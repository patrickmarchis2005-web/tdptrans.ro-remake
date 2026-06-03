using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using TdpTrans.DTOs;
using TdpTrans.Models;
using TdpTrans.Repositories;

namespace TdpTrans.Services
{
    public class SecurityLabService : ISecurityLabService
    {
        private const string CacheVersionKey = "security-lab-cache-version";
        private static readonly string[] FirstNames = ["Andrei", "Maria", "Ioana", "Vlad", "Elena", "Radu", "Mihai", "Bianca", "Stefan", "Anca"];
        private static readonly string[] LastNames = ["Popescu", "Ionescu", "Stan", "Marin", "Dumitru", "Georgescu", "Diaconu", "Matei", "Ilie", "Petrescu"];
        private static readonly string[] CompanyPrefixes = ["Atlas", "Rapid", "Cargo", "Transit", "Vector", "Nova", "Core", "Prime"];
        private static readonly string[] CompanySuffixes = ["Logistics", "Transport", "Fleet", "Freight", "Supply", "Route", "Dispatch", "Mobility"];
        private static readonly string[] Cities = ["Cluj-Napoca", "Bucuresti", "Iasi", "Timisoara", "Brasov", "Craiova", "Sibiu", "Oradea"];
        private static readonly string[] Streets = ["Observatorului", "Unirii", "Memorandumului", "Victoriei", "Primaverii", "Lalelelor", "Republicii", "Independentei"];
        private readonly ApplicationDbContext _dbContext;
        private readonly IMemoryCache _cache;
        private readonly ISuspiciousActivityService _suspiciousActivityService;
        private readonly IAiSuspiciousActivityDetector _aiSuspiciousActivityDetector;

        public SecurityLabService(
            ApplicationDbContext dbContext,
            IMemoryCache cache,
            ISuspiciousActivityService suspiciousActivityService,
            IAiSuspiciousActivityDetector aiSuspiciousActivityDetector)
        {
            _dbContext = dbContext;
            _cache = cache;
            _suspiciousActivityService = suspiciousActivityService;
            _aiSuspiciousActivityDetector = aiSuspiciousActivityDetector;
        }

        public async Task<SecurityStatisticsResponse> GetSecurityStatistics(string mode, int lookbackHours)
        {
            var normalizedMode = string.Equals(mode, "naive", StringComparison.OrdinalIgnoreCase)
                ? "naive"
                : "optimized";
            var normalizedLookbackHours = Math.Clamp(lookbackHours, 1, 24 * 90);
            var cacheVersion = GetCacheVersion();

            if (normalizedMode == "optimized")
            {
                var cacheKey = $"security-statistics:{cacheVersion}:{normalizedLookbackHours}";
                if (_cache.TryGetValue(cacheKey, out SecurityStatisticsResponse? cachedResponse) && cachedResponse != null)
                {
                    return CloneStatistics(cachedResponse, true);
                }

                var computedResponse = await BuildOptimizedStatistics(normalizedLookbackHours);
                _cache.Set(cacheKey, computedResponse, TimeSpan.FromSeconds(45));
                return CloneStatistics(computedResponse, false);
            }

            return await BuildNaiveStatistics(normalizedLookbackHours);
        }

        public async Task<SecuritySeedResponse> GenerateSecuritySeed(SecuritySeedRequest request)
        {
            var random = new Random();
            var normalized = new SecuritySeedRequest
            {
                UserCount = Math.Clamp(request.UserCount, 25, 1000),
                ClientCount = Math.Clamp(request.ClientCount, 25, 1000),
                TruckCount = Math.Clamp(request.TruckCount, 15, 400),
                MissionCount = Math.Clamp(request.MissionCount, 100, 12000),
                ActivityLogCount = Math.Clamp(request.ActivityLogCount, 200, 25000),
                SessionCount = Math.Clamp(request.SessionCount, 50, 5000),
            };

            var userRole = await _dbContext.Roles.SingleAsync(role => role.Name == RoleNames.User);
            var now = DateTime.UtcNow;

            var seededUsers = Enumerable.Range(0, normalized.UserCount)
                .Select(index =>
                {
                    var fullName = $"{Pick(FirstNames, random)} {Pick(LastNames, random)}";
                    var email = $"seed.user.{Guid.NewGuid():N}@tdptrans.local";
                    var securityCode = random.Next(100000, 999999).ToString();

                    return new AppUser
                    {
                        FullName = fullName,
                        Email = email,
                        PasswordHash = CredentialHasher.HashPassword("12345678"),
                        SecurityCodeHash = CredentialHasher.HashSecurityCode(securityCode),
                        AuthenticationPhraseHash = CredentialHasher.HashAuthenticationPhrase($"PHRASE-{random.Next(1000, 9999)}"),
                        CreatedAtUtc = now.AddMinutes(-random.Next(1, 60 * 24 * 45)),
                        IsActive = true
                    };
                })
                .ToList();

            _dbContext.Users.AddRange(seededUsers);
            await _dbContext.SaveChangesAsync();

            _dbContext.UserRoles.AddRange(seededUsers.Select(user => new UserRole
            {
                UserId = user.Id,
                RoleId = userRole.Id
            }));
            await _dbContext.SaveChangesAsync();

            var seededClients = Enumerable.Range(0, normalized.ClientCount)
                .Select(index => new Client
                {
                    Name = $"{Pick(CompanyPrefixes, random)} {Pick(CompanySuffixes, random)} {index + 1}",
                    Phone = $"07{random.Next(10000000, 99999999)}",
                    Email = $"client.{Guid.NewGuid():N}@seed.local"
                })
                .ToList();

            _dbContext.Clients.AddRange(seededClients);
            await _dbContext.SaveChangesAsync();

            var seededTrucks = Enumerable.Range(0, normalized.TruckCount)
                .Select(index => new Truck
                {
                    Id = GetAvailableTruckId(index, random),
                    LicensePlate = $"{Pick(["CJ", "B", "TM", "IS", "BV", "BH"], random)} {random.Next(10, 99)} {Pick(["LAB", "TDP", "OPS", "RUN"], random)}"
                })
                .ToList();

            _dbContext.Trucks.AddRange(seededTrucks);
            await _dbContext.SaveChangesAsync();

            var clientIds = seededClients.Select(client => client.Id).ToArray();
            var truckIds = seededTrucks.Select(truck => truck.Id).ToArray();

            var seededMissions = Enumerable.Range(0, normalized.MissionCount)
                .Select(index => new Mission
                {
                    ClientId = clientIds[random.Next(clientIds.Length)],
                    TruckId = truckIds[random.Next(truckIds.Length)],
                    Type = random.Next(100) < 55 ? MissionType.Transport : MissionType.Tractare,
                    Status = random.Next(100) switch
                    {
                        < 45 => MissionStatus.Finalizata,
                        < 75 => MissionStatus.Programata,
                        _ => MissionStatus.In_desfasurare,
                    },
                    Cost = decimal.Round((decimal)(250 + random.NextDouble() * 4750), 2),
                    Address = $"{Pick(Streets, random)} nr. {random.Next(1, 180)}, {Pick(Cities, random)}",
                    Date = now.AddDays(-random.Next(0, 365))
                })
                .ToList();

            _dbContext.Missions.AddRange(seededMissions);
            await _dbContext.SaveChangesAsync();

            var suspiciousUsers = seededUsers
                .OrderBy(_ => random.Next())
                .Take(Math.Max(6, normalized.UserCount / 20))
                .ToArray();

            var sessions = new List<AuthSession>();
            for (var index = 0; index < normalized.SessionCount; index++)
            {
                var user = seededUsers[random.Next(seededUsers.Count)];
                var createdAtUtc = now.AddMinutes(-random.Next(1, 60 * 24 * 10));
                sessions.Add(new AuthSession
                {
                    UserId = user.Id,
                    TokenHash = $"seed-session-{Guid.NewGuid():N}",
                    CreatedAtUtc = createdAtUtc,
                    LastActivityAtUtc = createdAtUtc.AddMinutes(random.Next(0, 120)),
                    ExpiresAtUtc = createdAtUtc.AddHours(12),
                    ClientKey = $"seed-client-{random.Next(1000, 9999)}",
                    UserAgent = index % 4 == 0 ? "Playwright/seed-bot" : "SeedBrowser/1.0",
                    RemoteIpAddress = suspiciousUsers.Contains(user)
                        ? $"10.0.{random.Next(1, 5)}.{random.Next(2, 250)}"
                        : $"192.168.0.{random.Next(2, 250)}"
                });
            }

            foreach (var suspiciousUser in suspiciousUsers)
            {
                sessions.Add(new AuthSession
                {
                    UserId = suspiciousUser.Id,
                    TokenHash = $"seed-session-{Guid.NewGuid():N}",
                    CreatedAtUtc = now.AddMinutes(-15),
                    LastActivityAtUtc = now.AddMinutes(-5),
                    ExpiresAtUtc = now.AddHours(12),
                    ClientKey = $"seed-client-{random.Next(1000, 9999)}",
                    UserAgent = "Playwright/seed-bot",
                    RemoteIpAddress = $"172.16.{random.Next(1, 10)}.{random.Next(2, 250)}"
                });
            }

            _dbContext.AuthSessions.AddRange(sessions);

            var logs = new List<ActivityLog>();
            var actionTypes = new[]
            {
                ActivityActionNames.MissionsViewed,
                ActivityActionNames.MissionCreated,
                ActivityActionNames.MissionUpdated,
                ActivityActionNames.StatisticsViewed,
                ActivityActionNames.ChatMessageSent,
                ActivityActionNames.LoginSucceeded,
            };

            for (var index = 0; index < normalized.ActivityLogCount; index++)
            {
                var user = seededUsers[random.Next(seededUsers.Count)];
                var actionType = Pick(actionTypes, random);
                var isSuccess = actionType != ActivityActionNames.LoginSucceeded || random.Next(100) >= 5;
                logs.Add(new ActivityLog
                {
                    UserId = user.Id,
                    GroupId = RoleNames.User,
                    ActionType = actionType,
                    ActionInformation = $"Seeded {actionType} event #{index + 1}.",
                    IsSuccess = isSuccess,
                    TimestampUtc = now.AddMinutes(-random.Next(0, 60 * 24 * 14))
                });
            }

            foreach (var suspiciousUser in suspiciousUsers)
            {
                for (var attempt = 0; attempt < 4; attempt++)
                {
                    logs.Add(new ActivityLog
                    {
                        UserId = suspiciousUser.Id,
                        GroupId = RoleNames.User,
                        ActionType = ActivityActionNames.LoginFailed,
                        ActionInformation = "Seeded failed login burst.",
                        IsSuccess = false,
                        TimestampUtc = now.AddMinutes(-10 + attempt)
                    });
                }

                for (var denial = 0; denial < 4; denial++)
                {
                    logs.Add(new ActivityLog
                    {
                        UserId = suspiciousUser.Id,
                        GroupId = RoleNames.User,
                        ActionType = ActivityActionNames.PermissionDenied,
                        ActionInformation = "Seeded permission-probing burst.",
                        IsSuccess = false,
                        TimestampUtc = now.AddMinutes(-6 + denial)
                    });
                }

                for (var chat = 0; chat < 22; chat++)
                {
                    logs.Add(new ActivityLog
                    {
                        UserId = suspiciousUser.Id,
                        GroupId = RoleNames.User,
                        ActionType = ActivityActionNames.ChatMessageSent,
                        ActionInformation = "Seeded chat flood burst.",
                        IsSuccess = true,
                        TimestampUtc = now.AddSeconds(-chat * 4)
                    });
                }
            }

            _dbContext.ActivityLogs.AddRange(logs);
            await _dbContext.SaveChangesAsync();

            foreach (var suspiciousUser in suspiciousUsers)
            {
                await _suspiciousActivityService.Evaluate(suspiciousUser.Id);
            }

            BumpCacheVersion();
            _aiSuspiciousActivityDetector.InvalidateModel();

            return new SecuritySeedResponse
            {
                CreatedUsers = seededUsers.Count,
                CreatedClients = seededClients.Count,
                CreatedTrucks = seededTrucks.Count,
                CreatedMissions = seededMissions.Count,
                CreatedActivityLogs = logs.Count,
                CreatedSessions = sessions.Count,
                SuspiciousProfilesSeeded = suspiciousUsers.Length
            };
        }

        private async Task<SecurityStatisticsResponse> BuildNaiveStatistics(int lookbackHours)
        {
            var stopwatch = Stopwatch.StartNew();
            var now = DateTime.UtcNow;
            var sinceUtc = now.AddHours(-lookbackHours);

            var users = await _dbContext.Users
                .AsNoTracking()
                .Include(user => user.UserRoles)
                    .ThenInclude(userRole => userRole.Role)
                        .ThenInclude(role => role.RolePermissions)
                            .ThenInclude(rolePermission => rolePermission.Permission)
                .ToListAsync();

            var logs = await _dbContext.ActivityLogs
                .AsNoTracking()
                .Where(activityLog => activityLog.TimestampUtc >= sinceUtc && activityLog.UserId != null)
                .ToListAsync();

            var observations = await _dbContext.UserObservations
                .AsNoTracking()
                .Where(observation => observation.IsActive)
                .Include(observation => observation.User)
                    .ThenInclude(user => user.UserRoles)
                        .ThenInclude(userRole => userRole.Role)
                .ToListAsync();

            var logSummariesByUser = logs
                .GroupBy(log => log.UserId!.Value)
                .ToDictionary(
                    group => group.Key,
                    group => new UserLogSummary(
                        group.Count(log => log.IsSuccess),
                        group.Count(log => !log.IsSuccess),
                        group.Count(log => log.ActionType == ActivityActionNames.LoginFailed && !log.IsSuccess),
                        group.Count(log => log.ActionType == ActivityActionNames.PermissionDenied && !log.IsSuccess),
                        group.Count(log => log.ActionType == ActivityActionNames.ChatMessageSent && log.IsSuccess)));

            var permissionStats = users
                .SelectMany(user => user.UserRoles.SelectMany(userRole =>
                    userRole.Role.RolePermissions.Select(rolePermission => new
                    {
                        UserId = user.Id,
                        RoleId = userRole.RoleId,
                        PermissionName = rolePermission.Permission.Name,
                    })))
                .GroupBy(edge => edge.PermissionName)
                .Select(group =>
                {
                    var userIds = group.Select(edge => edge.UserId).Distinct().ToArray();
                    return new PermissionLoadStatisticResponse
                    {
                        PermissionName = group.Key,
                        RoleCount = group.Select(edge => edge.RoleId).Distinct().Count(),
                        UserCount = userIds.Length,
                        AssignmentEdges = group.Count(),
                        SuccessfulActionCount = userIds.Sum(userId => logSummariesByUser.TryGetValue(userId, out var summary) ? summary.SuccessCount : 0),
                        FailedActionCount = userIds.Sum(userId => logSummariesByUser.TryGetValue(userId, out var summary) ? summary.FailureCount : 0),
                        ObservedRiskUsers = observations.Select(observation => observation.UserId).Distinct().Count(userIds.Contains),
                    };
                })
                .OrderByDescending(statistic => statistic.UserCount)
                .ThenBy(statistic => statistic.PermissionName)
                .ToArray();

            stopwatch.Stop();
            return BuildStatisticsResponse(
                "naive",
                false,
                lookbackHours,
                stopwatch.ElapsedMilliseconds,
                now,
                users.Count,
                users.Sum(user => user.UserRoles.Count),
                permissionStats.Sum(statistic => statistic.AssignmentEdges),
                logs.Count,
                permissionStats,
                BuildTopRiskUsers(observations, logSummariesByUser, new Dictionary<int, int>()));
        }

        private async Task<SecurityStatisticsResponse> BuildOptimizedStatistics(int lookbackHours)
        {
            var stopwatch = Stopwatch.StartNew();
            var now = DateTime.UtcNow;
            var sinceUtc = now.AddHours(-lookbackHours);

            var assignmentEdges = await (
                from userRole in _dbContext.UserRoles.AsNoTracking()
                join rolePermission in _dbContext.RolePermissions.AsNoTracking() on userRole.RoleId equals rolePermission.RoleId
                join permission in _dbContext.Permissions.AsNoTracking() on rolePermission.PermissionId equals permission.Id
                select new
                {
                    userRole.UserId,
                    userRole.RoleId,
                    PermissionName = permission.Name,
                })
                .ToListAsync();

            var logSummariesByUser = await _dbContext.ActivityLogs
                .AsNoTracking()
                .Where(activityLog => activityLog.UserId != null && activityLog.TimestampUtc >= sinceUtc)
                .GroupBy(activityLog => activityLog.UserId!.Value)
                .Select(group => new
                {
                    UserId = group.Key,
                    SuccessCount = group.Count(activityLog => activityLog.IsSuccess),
                    FailureCount = group.Count(activityLog => !activityLog.IsSuccess),
                    FailedLogins = group.Count(activityLog => activityLog.ActionType == ActivityActionNames.LoginFailed && !activityLog.IsSuccess),
                    PermissionDenials = group.Count(activityLog => activityLog.ActionType == ActivityActionNames.PermissionDenied && !activityLog.IsSuccess),
                    ChatMessages = group.Count(activityLog => activityLog.ActionType == ActivityActionNames.ChatMessageSent && activityLog.IsSuccess),
                })
                .ToDictionaryAsync(
                    row => row.UserId,
                    row => new UserLogSummary(
                        row.SuccessCount,
                        row.FailureCount,
                        row.FailedLogins,
                        row.PermissionDenials,
                        row.ChatMessages));

            var observationRows = await _dbContext.UserObservations
                .AsNoTracking()
                .Where(observation => observation.IsActive)
                .Include(observation => observation.User)
                    .ThenInclude(user => user.UserRoles)
                        .ThenInclude(userRole => userRole.Role)
                .ToListAsync();

            var distinctIpCounts = await _dbContext.AuthSessions
                .AsNoTracking()
                .Where(session => session.RevokedAtUtc == null && session.CreatedAtUtc >= sinceUtc && !string.IsNullOrWhiteSpace(session.RemoteIpAddress))
                .GroupBy(session => session.UserId)
                .Select(group => new
                {
                    UserId = group.Key,
                    DistinctIpCount = group.Select(session => session.RemoteIpAddress).Distinct().Count(),
                })
                .ToDictionaryAsync(row => row.UserId, row => row.DistinctIpCount);

            var permissionStats = assignmentEdges
                .GroupBy(edge => edge.PermissionName)
                .Select(group =>
                {
                    var userIds = group.Select(edge => edge.UserId).Distinct().ToArray();
                    return new PermissionLoadStatisticResponse
                    {
                        PermissionName = group.Key,
                        RoleCount = group.Select(edge => edge.RoleId).Distinct().Count(),
                        UserCount = userIds.Length,
                        AssignmentEdges = group.Count(),
                        SuccessfulActionCount = userIds.Sum(userId => logSummariesByUser.TryGetValue(userId, out var summary) ? summary.SuccessCount : 0),
                        FailedActionCount = userIds.Sum(userId => logSummariesByUser.TryGetValue(userId, out var summary) ? summary.FailureCount : 0),
                        ObservedRiskUsers = observationRows.Select(observation => observation.UserId).Distinct().Count(userIds.Contains),
                    };
                })
                .OrderByDescending(statistic => statistic.UserCount)
                .ThenBy(statistic => statistic.PermissionName)
                .ToArray();

            var totalUsers = await _dbContext.Users.AsNoTracking().CountAsync();
            var totalRoleAssignments = await _dbContext.UserRoles.AsNoTracking().CountAsync();
            var totalActivityLogs = await _dbContext.ActivityLogs.AsNoTracking().CountAsync(activityLog => activityLog.TimestampUtc >= sinceUtc);

            stopwatch.Stop();
            return BuildStatisticsResponse(
                "optimized",
                false,
                lookbackHours,
                stopwatch.ElapsedMilliseconds,
                now,
                totalUsers,
                totalRoleAssignments,
                assignmentEdges.Count,
                totalActivityLogs,
                permissionStats,
                BuildTopRiskUsers(observationRows, logSummariesByUser, distinctIpCounts));
        }

        private static SecurityStatisticsResponse BuildStatisticsResponse(
            string mode,
            bool isCached,
            int lookbackHours,
            long durationMs,
            DateTime generatedAtUtc,
            int totalUsers,
            int totalRoleAssignments,
            int totalPermissionAssignments,
            int totalActivityLogs,
            IReadOnlyList<PermissionLoadStatisticResponse> permissionStatistics,
            IReadOnlyList<SecurityRiskUserResponse> topRiskUsers)
        {
            return new SecurityStatisticsResponse
            {
                Mode = mode,
                IsCached = isCached,
                LookbackHours = lookbackHours,
                DurationMs = durationMs,
                GeneratedAtUtc = generatedAtUtc,
                TotalUsers = totalUsers,
                TotalRoleAssignments = totalRoleAssignments,
                TotalPermissionAssignments = totalPermissionAssignments,
                TotalActivityLogs = totalActivityLogs,
                PermissionStatistics = permissionStatistics,
                TopRiskUsers = topRiskUsers,
            };
        }

        private static IReadOnlyList<SecurityRiskUserResponse> BuildTopRiskUsers(
            IReadOnlyList<UserObservation> observations,
            IReadOnlyDictionary<int, UserLogSummary> logSummariesByUser,
            IReadOnlyDictionary<int, int> distinctIpCounts)
        {
            return observations
                .GroupBy(observation => observation.UserId)
                .Select(group =>
                {
                    var latestObservation = group.OrderByDescending(observation => observation.LastDetectedAtUtc).First();
                    logSummariesByUser.TryGetValue(group.Key, out var summary);
                    distinctIpCounts.TryGetValue(group.Key, out var distinctIpCount);

                    return new SecurityRiskUserResponse
                    {
                        UserId = latestObservation.UserId,
                        FullName = latestObservation.User.FullName,
                        Email = latestObservation.User.Email,
                        RoleName = latestObservation.User.UserRoles.Select(userRole => userRole.Role.Name).FirstOrDefault() ?? RoleNames.User,
                        RiskScore = group.Max(observation => observation.RiskScore),
                        FailedLogins = summary?.FailedLogins ?? 0,
                        PermissionDenials = summary?.PermissionDenials ?? 0,
                        ChatMessagesLastTwoMinutes = summary?.ChatMessages ?? 0,
                        DistinctRecentIpCount = distinctIpCount,
                    };
                })
                .OrderByDescending(user => user.RiskScore)
                .ThenByDescending(user => user.FailedLogins)
                .Take(6)
                .ToArray();
        }

        private SecurityStatisticsResponse CloneStatistics(SecurityStatisticsResponse response, bool isCached)
        {
            return new SecurityStatisticsResponse
            {
                Mode = response.Mode,
                IsCached = isCached,
                LookbackHours = response.LookbackHours,
                DurationMs = response.DurationMs,
                GeneratedAtUtc = response.GeneratedAtUtc,
                TotalUsers = response.TotalUsers,
                TotalRoleAssignments = response.TotalRoleAssignments,
                TotalPermissionAssignments = response.TotalPermissionAssignments,
                TotalActivityLogs = response.TotalActivityLogs,
                PermissionStatistics = response.PermissionStatistics
                    .Select(statistic => new PermissionLoadStatisticResponse
                    {
                        PermissionName = statistic.PermissionName,
                        RoleCount = statistic.RoleCount,
                        UserCount = statistic.UserCount,
                        AssignmentEdges = statistic.AssignmentEdges,
                        SuccessfulActionCount = statistic.SuccessfulActionCount,
                        FailedActionCount = statistic.FailedActionCount,
                        ObservedRiskUsers = statistic.ObservedRiskUsers,
                    })
                    .ToArray(),
                TopRiskUsers = response.TopRiskUsers
                    .Select(user => new SecurityRiskUserResponse
                    {
                        UserId = user.UserId,
                        FullName = user.FullName,
                        Email = user.Email,
                        RoleName = user.RoleName,
                        RiskScore = user.RiskScore,
                        FailedLogins = user.FailedLogins,
                        PermissionDenials = user.PermissionDenials,
                        ChatMessagesLastTwoMinutes = user.ChatMessagesLastTwoMinutes,
                        DistinctRecentIpCount = user.DistinctRecentIpCount,
                    })
                    .ToArray(),
            };
        }

        private string GetCacheVersion()
        {
            if (_cache.TryGetValue(CacheVersionKey, out string? value) && !string.IsNullOrWhiteSpace(value))
            {
                return value;
            }

            value = Guid.NewGuid().ToString("N");
            _cache.Set(CacheVersionKey, value);
            return value;
        }

        private void BumpCacheVersion()
        {
            _cache.Set(CacheVersionKey, Guid.NewGuid().ToString("N"));
        }

        private static T Pick<T>(IReadOnlyList<T> values, Random random)
        {
            return values[random.Next(values.Count)];
        }

        private int GetAvailableTruckId(int index, Random random)
        {
            var baseId = 700000 + index;
            while (_dbContext.Trucks.Any(truck => truck.Id == baseId))
            {
                baseId += random.Next(1, 1000);
            }

            return baseId;
        }

        private sealed record UserLogSummary(
            int SuccessCount,
            int FailureCount,
            int FailedLogins,
            int PermissionDenials,
            int ChatMessages);
    }
}
