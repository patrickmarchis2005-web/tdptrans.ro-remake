using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using TdpTrans.Models;
using TdpTrans.Repositories;

namespace TdpTrans.Services
{
    public class AiSuspiciousActivityDetector : IAiSuspiciousActivityDetector
    {
        private const string ModelCacheKey = "ai-suspicious-activity-model";
        private const double RiskThreshold = 0.78d;
        private static readonly string[] RuleObservationReasons =
        [
            ObservationReasons.FailedLogins,
            ObservationReasons.PermissionProbe,
            ObservationReasons.ChatBurst,
            ObservationReasons.MultiSessionIpDrift
        ];

        private static readonly FeatureDefinition[] FeatureDefinitions =
        [
            new("FailedLogins15m", snapshot => snapshot.FailedLogins15m, snapshot => $"{snapshot.FailedLogins15m} login-uri esuate/15m"),
            new("PermissionDenials10m", snapshot => snapshot.PermissionDenials10m, snapshot => $"{snapshot.PermissionDenials10m} accesari refuzate/10m"),
            new("ChatMessages2m", snapshot => snapshot.ChatMessages2m, snapshot => $"{snapshot.ChatMessages2m} mesaje/2m"),
            new("ActiveSessions", snapshot => snapshot.ActiveSessions, snapshot => $"{snapshot.ActiveSessions} sesiuni active"),
            new("DistinctRecentIps12h", snapshot => snapshot.DistinctRecentIps12h, snapshot => $"{snapshot.DistinctRecentIps12h} IP-uri/12h"),
            new("SuccessActions24h", snapshot => snapshot.SuccessActions24h, snapshot => $"{snapshot.SuccessActions24h} actiuni reusite/24h"),
            new("FailedActions24h", snapshot => snapshot.FailedActions24h, snapshot => $"{snapshot.FailedActions24h} actiuni esuate/24h")
        ];

        private readonly ApplicationDbContext _dbContext;
        private readonly IMemoryCache _cache;

        public AiSuspiciousActivityDetector(ApplicationDbContext dbContext, IMemoryCache cache)
        {
            _dbContext = dbContext;
            _cache = cache;
        }

        public async Task<AiSuspiciousActivityAssessment?> Assess(int userId)
        {
            var snapshot = await BuildSnapshot(userId, DateTime.UtcNow);
            if (snapshot == null || !snapshot.HasMeaningfulActivity)
            {
                return null;
            }

            var model = await GetOrTrainModel();
            var probability = model.Predict(snapshot);
            var shouldFlag = probability >= RiskThreshold;

            return new AiSuspiciousActivityAssessment
            {
                ShouldFlag = shouldFlag,
                Probability = probability,
                RiskScore = Math.Clamp((int)Math.Round(probability * 100d), 1, 99),
                Details = BuildDetails(model, snapshot, probability)
            };
        }

        public void InvalidateModel()
        {
            _cache.Remove(ModelCacheKey);
        }

        private async Task<AiModel> GetOrTrainModel()
        {
            if (_cache.TryGetValue(ModelCacheKey, out AiModel? cachedModel) && cachedModel != null)
            {
                return cachedModel;
            }

            var trainedModel = await TrainModel(DateTime.UtcNow);
            _cache.Set(ModelCacheKey, trainedModel, TimeSpan.FromMinutes(2));
            return trainedModel;
        }

        private async Task<AiModel> TrainModel(DateTime now)
        {
            var samples = GetBaselineTrainingSamples();
            var liveSamples = await BuildLiveTrainingSamples(now);
            samples.AddRange(liveSamples);

            return AiModel.Train(samples);
        }

        private async Task<List<TrainingSample>> BuildLiveTrainingSamples(DateTime now)
        {
            var snapshotsByUser = await BuildSnapshotsForAllUsers(now);
            var positiveUserIds = await _dbContext.UserObservations
                .AsNoTracking()
                .Where(observation => observation.IsActive && RuleObservationReasons.Contains(observation.Reason))
                .Select(observation => observation.UserId)
                .Distinct()
                .ToListAsync();

            var positiveSet = positiveUserIds.ToHashSet();
            var candidateSnapshots = snapshotsByUser.Values
                .Where(snapshot => snapshot.HasMeaningfulActivity || positiveSet.Contains(snapshot.UserId))
                .ToList();

            var positiveSamples = candidateSnapshots
                .Where(snapshot => positiveSet.Contains(snapshot.UserId))
                .Select(snapshot => new TrainingSample(snapshot, true))
                .ToList();

            var negativeSamples = candidateSnapshots
                .Where(snapshot => !positiveSet.Contains(snapshot.UserId) && IsLikelyBenign(snapshot))
                .OrderByDescending(snapshot => snapshot.ActivityWeight)
                .Take(Math.Max(12, positiveSamples.Count * 3))
                .Select(snapshot => new TrainingSample(snapshot, false))
                .ToList();

            positiveSamples.AddRange(negativeSamples);
            return positiveSamples;
        }

        private static bool IsLikelyBenign(FeatureSnapshot snapshot)
        {
            if (CountSuspiciousSignals(snapshot) >= 2)
            {
                return false;
            }

            if (snapshot.FailedLogins15m >= 2 ||
                snapshot.PermissionDenials10m >= 2 ||
                snapshot.ChatMessages2m >= 12 ||
                snapshot.DistinctRecentIps12h >= 3 ||
                snapshot.FailedActions24h >= 6)
            {
                return false;
            }

            return true;
        }

        private static int CountSuspiciousSignals(FeatureSnapshot snapshot)
        {
            var suspiciousSignals = 0;

            if (snapshot.FailedLogins15m >= 2)
            {
                suspiciousSignals++;
            }

            if (snapshot.PermissionDenials10m >= 2)
            {
                suspiciousSignals++;
            }

            if (snapshot.ChatMessages2m >= 12)
            {
                suspiciousSignals++;
            }

            if (snapshot.ActiveSessions >= 3)
            {
                suspiciousSignals++;
            }

            if (snapshot.DistinctRecentIps12h >= 3)
            {
                suspiciousSignals++;
            }

            return suspiciousSignals;
        }

        private async Task<FeatureSnapshot?> BuildSnapshot(int userId, DateTime now)
        {
            var snapshotsByUser = await BuildSnapshotsForAllUsers(now, userId);
            return snapshotsByUser.GetValueOrDefault(userId);
        }

        private async Task<Dictionary<int, FeatureSnapshot>> BuildSnapshotsForAllUsers(DateTime now, int? limitToUserId = null)
        {
            var window24HoursUtc = now.AddHours(-24);
            var window15MinutesUtc = now.AddMinutes(-15);
            var window10MinutesUtc = now.AddMinutes(-10);
            var window2MinutesUtc = now.AddMinutes(-2);
            var window12HoursUtc = now.AddHours(-12);

            var baseLogsQuery = _dbContext.ActivityLogs
                .AsNoTracking()
                .Where(activityLog => activityLog.UserId != null);
            var baseSessionsQuery = _dbContext.AuthSessions
                .AsNoTracking()
                .Where(session => session.RevokedAtUtc == null);

            if (limitToUserId.HasValue)
            {
                var userId = limitToUserId.Value;
                baseLogsQuery = baseLogsQuery.Where(activityLog => activityLog.UserId == userId);
                baseSessionsQuery = baseSessionsQuery.Where(session => session.UserId == userId);
            }

            var recentOutcomesByUser = await baseLogsQuery
                .Where(activityLog => activityLog.TimestampUtc >= window24HoursUtc)
                .GroupBy(activityLog => activityLog.UserId!.Value)
                .Select(group => new
                {
                    UserId = group.Key,
                    SuccessCount = group.Count(activityLog => activityLog.IsSuccess),
                    FailureCount = group.Count(activityLog => !activityLog.IsSuccess)
                })
                .ToDictionaryAsync(
                    row => row.UserId,
                    row => new OutcomeSummary(row.SuccessCount, row.FailureCount));

            var failedLoginsByUser = await CountGroupedLogs(
                baseLogsQuery,
                window15MinutesUtc,
                ActivityActionNames.LoginFailed,
                false);
            var permissionDenialsByUser = await CountGroupedLogs(
                baseLogsQuery,
                window10MinutesUtc,
                ActivityActionNames.PermissionDenied,
                false);
            var chatMessagesByUser = await CountGroupedLogs(
                baseLogsQuery,
                window2MinutesUtc,
                ActivityActionNames.ChatMessageSent,
                true);

            var activeSessionsByUser = await baseSessionsQuery
                .GroupBy(session => session.UserId)
                .Select(group => new
                {
                    UserId = group.Key,
                    Count = group.Count()
                })
                .ToDictionaryAsync(row => row.UserId, row => row.Count);

            var distinctRecentIpsByUser = await baseSessionsQuery
                .Where(session => session.CreatedAtUtc >= window12HoursUtc && !string.IsNullOrWhiteSpace(session.RemoteIpAddress))
                .GroupBy(session => session.UserId)
                .Select(group => new
                {
                    UserId = group.Key,
                    Count = group.Select(session => session.RemoteIpAddress).Distinct().Count()
                })
                .ToDictionaryAsync(row => row.UserId, row => row.Count);

            var candidateUserIds = recentOutcomesByUser.Keys
                .Concat(failedLoginsByUser.Keys)
                .Concat(permissionDenialsByUser.Keys)
                .Concat(chatMessagesByUser.Keys)
                .Concat(activeSessionsByUser.Keys)
                .Concat(distinctRecentIpsByUser.Keys)
                .Distinct()
                .ToArray();

            return candidateUserIds.ToDictionary(
                userId => userId,
                userId =>
                {
                    var outcomes = recentOutcomesByUser.GetValueOrDefault(userId) ?? OutcomeSummary.Empty;
                    failedLoginsByUser.TryGetValue(userId, out var failedLogins);
                    permissionDenialsByUser.TryGetValue(userId, out var permissionDenials);
                    chatMessagesByUser.TryGetValue(userId, out var chatMessages);
                    activeSessionsByUser.TryGetValue(userId, out var activeSessions);
                    distinctRecentIpsByUser.TryGetValue(userId, out var distinctRecentIps);

                    return new FeatureSnapshot(
                        userId,
                        failedLogins,
                        permissionDenials,
                        chatMessages,
                        activeSessions,
                        distinctRecentIps,
                        outcomes.SuccessCount,
                        outcomes.FailureCount);
                });
        }

        private static async Task<Dictionary<int, int>> CountGroupedLogs(
            IQueryable<ActivityLog> logsQuery,
            DateTime sinceUtc,
            string actionType,
            bool isSuccess)
        {
            return await logsQuery
                .Where(activityLog =>
                    activityLog.TimestampUtc >= sinceUtc &&
                    activityLog.ActionType == actionType &&
                    activityLog.IsSuccess == isSuccess)
                .GroupBy(activityLog => activityLog.UserId!.Value)
                .Select(group => new
                {
                    UserId = group.Key,
                    Count = group.Count()
                })
                .ToDictionaryAsync(row => row.UserId, row => row.Count);
        }

        private static string BuildDetails(AiModel model, FeatureSnapshot snapshot, double probability)
        {
            var topSignals = model.GetTopSignals(snapshot)
                .Where(signal => signal.Contribution > 0d)
                .Take(3)
                .Select(signal => signal.Description)
                .ToArray();

            var score = Math.Round(probability * 100d);
            var signalSummary = topSignals.Length > 0
                ? string.Join(", ", topSignals)
                : "profil combinat de activitate neobisnuit";

            return $"Scor AI {score:0}%. Semnale dominante: {signalSummary}.";
        }

        private static List<TrainingSample> GetBaselineTrainingSamples()
        {
            return
            [
                new TrainingSample(new FeatureSnapshot(0, 4, 0, 0, 1, 1, 1, 6), true),
                new TrainingSample(new FeatureSnapshot(0, 5, 1, 0, 2, 2, 1, 8), true),
                new TrainingSample(new FeatureSnapshot(0, 0, 4, 0, 1, 1, 2, 6), true),
                new TrainingSample(new FeatureSnapshot(0, 1, 3, 2, 2, 2, 4, 7), true),
                new TrainingSample(new FeatureSnapshot(0, 0, 0, 24, 1, 1, 10, 3), true),
                new TrainingSample(new FeatureSnapshot(0, 1, 0, 18, 2, 2, 9, 4), true),
                new TrainingSample(new FeatureSnapshot(0, 0, 1, 4, 4, 3, 5, 3), true),
                new TrainingSample(new FeatureSnapshot(0, 2, 2, 14, 3, 3, 6, 8), true),
                new TrainingSample(new FeatureSnapshot(0, 0, 0, 0, 1, 1, 8, 0), false),
                new TrainingSample(new FeatureSnapshot(0, 0, 0, 3, 1, 1, 10, 1), false),
                new TrainingSample(new FeatureSnapshot(0, 1, 0, 6, 1, 1, 12, 2), false),
                new TrainingSample(new FeatureSnapshot(0, 0, 1, 4, 1, 1, 9, 1), false),
                new TrainingSample(new FeatureSnapshot(0, 0, 0, 0, 2, 1, 6, 0), false),
                new TrainingSample(new FeatureSnapshot(0, 0, 0, 8, 1, 1, 14, 2), false),
                new TrainingSample(new FeatureSnapshot(0, 1, 0, 10, 2, 1, 16, 2), false),
                new TrainingSample(new FeatureSnapshot(0, 0, 1, 1, 2, 1, 7, 1), false)
            ];
        }

        private sealed record FeatureDefinition(
            string Name,
            Func<FeatureSnapshot, double> GetValue,
            Func<FeatureSnapshot, string> Describe);

        private sealed record TrainingSample(FeatureSnapshot Snapshot, bool Label);

        private sealed record OutcomeSummary(int SuccessCount, int FailureCount)
        {
            public static readonly OutcomeSummary Empty = new(0, 0);
        }

        private sealed record FeatureSnapshot(
            int UserId,
            int FailedLogins15m,
            int PermissionDenials10m,
            int ChatMessages2m,
            int ActiveSessions,
            int DistinctRecentIps12h,
            int SuccessActions24h,
            int FailedActions24h)
        {
            public bool HasMeaningfulActivity =>
                FailedLogins15m > 0 ||
                PermissionDenials10m > 0 ||
                ChatMessages2m > 0 ||
                ActiveSessions > 0 ||
                DistinctRecentIps12h > 0 ||
                SuccessActions24h > 0 ||
                FailedActions24h > 0;

            public int ActivityWeight =>
                FailedLogins15m * 4 +
                PermissionDenials10m * 4 +
                ChatMessages2m * 2 +
                ActiveSessions * 2 +
                DistinctRecentIps12h * 3 +
                SuccessActions24h +
                FailedActions24h * 2;

            public double[] ToVector()
            {
                return
                [
                    FailedLogins15m,
                    PermissionDenials10m,
                    ChatMessages2m,
                    ActiveSessions,
                    DistinctRecentIps12h,
                    SuccessActions24h,
                    FailedActions24h
                ];
            }
        }

        private sealed class AiModel
        {
            private readonly double[] _means;
            private readonly double[] _standardDeviations;
            private readonly double[] _weights;
            private readonly double _bias;

            private AiModel(double[] means, double[] standardDeviations, double[] weights, double bias)
            {
                _means = means;
                _standardDeviations = standardDeviations;
                _weights = weights;
                _bias = bias;
            }

            public static AiModel Train(IReadOnlyList<TrainingSample> samples)
            {
                var featureCount = FeatureDefinitions.Length;
                var means = new double[featureCount];
                var standardDeviations = new double[featureCount];

                for (var featureIndex = 0; featureIndex < featureCount; featureIndex++)
                {
                    means[featureIndex] = samples.Average(sample => sample.Snapshot.ToVector()[featureIndex]);
                    var variance = samples
                        .Select(sample => sample.Snapshot.ToVector()[featureIndex] - means[featureIndex])
                        .Average(value => value * value);
                    standardDeviations[featureIndex] = variance > 0d ? Math.Sqrt(variance) : 1d;
                }

                var normalizedSamples = samples
                    .Select(sample => new
                    {
                        Label = sample.Label ? 1d : 0d,
                        Features = Normalize(sample.Snapshot.ToVector(), means, standardDeviations)
                    })
                    .ToArray();

                var weights = new double[featureCount];
                var bias = 0d;
                const double learningRate = 0.08d;
                const double regularization = 0.001d;

                for (var epoch = 0; epoch < 500; epoch++)
                {
                    foreach (var sample in normalizedSamples)
                    {
                        var score = bias + Dot(weights, sample.Features);
                        var probability = Sigmoid(score);
                        var error = probability - sample.Label;

                        bias -= learningRate * error;
                        for (var featureIndex = 0; featureIndex < featureCount; featureIndex++)
                        {
                            weights[featureIndex] -= learningRate * ((error * sample.Features[featureIndex]) + (regularization * weights[featureIndex]));
                        }
                    }
                }

                return new AiModel(means, standardDeviations, weights, bias);
            }

            public double Predict(FeatureSnapshot snapshot)
            {
                var normalized = Normalize(snapshot.ToVector(), _means, _standardDeviations);
                return Sigmoid(_bias + Dot(_weights, normalized));
            }

            public IReadOnlyList<SignalContribution> GetTopSignals(FeatureSnapshot snapshot)
            {
                var rawVector = snapshot.ToVector();
                var normalized = Normalize(rawVector, _means, _standardDeviations);

                return FeatureDefinitions
                    .Select((definition, index) => new SignalContribution(
                        definition.Name,
                        definition.Describe(snapshot),
                        normalized[index] * _weights[index]))
                    .OrderByDescending(signal => signal.Contribution)
                    .ToArray();
            }

            private static double[] Normalize(double[] values, double[] means, double[] standardDeviations)
            {
                var normalized = new double[values.Length];
                for (var index = 0; index < values.Length; index++)
                {
                    normalized[index] = (values[index] - means[index]) / standardDeviations[index];
                }

                return normalized;
            }

            private static double Dot(IReadOnlyList<double> weights, IReadOnlyList<double> features)
            {
                var sum = 0d;
                for (var index = 0; index < weights.Count; index++)
                {
                    sum += weights[index] * features[index];
                }

                return sum;
            }

            private static double Sigmoid(double value)
            {
                var clamped = Math.Clamp(value, -30d, 30d);
                return 1d / (1d + Math.Exp(-clamped));
            }
        }

        private sealed record SignalContribution(string Name, string Description, double Contribution);
    }
}
