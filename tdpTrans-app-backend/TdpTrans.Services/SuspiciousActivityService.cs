using TdpTrans.DTOs;
using TdpTrans.Models;
using TdpTrans.Repositories.Interfaces;

namespace TdpTrans.Services
{
    public class SuspiciousActivityService : ISuspiciousActivityService
    {
        private readonly IActivityLogRepository _activityLogRepository;
        private readonly IObservationRepository _observationRepository;
        private readonly IAuthSessionRepository _authSessionRepository;
        private readonly IAiSuspiciousActivityDetector _aiSuspiciousActivityDetector;

        public SuspiciousActivityService(
            IActivityLogRepository activityLogRepository,
            IObservationRepository observationRepository,
            IAuthSessionRepository authSessionRepository,
            IAiSuspiciousActivityDetector aiSuspiciousActivityDetector)
        {
            _activityLogRepository = activityLogRepository;
            _observationRepository = observationRepository;
            _authSessionRepository = authSessionRepository;
            _aiSuspiciousActivityDetector = aiSuspiciousActivityDetector;
        }

        public async Task Evaluate(int userId)
        {
            var now = DateTime.UtcNow;

            var failedLoginCount = await _activityLogRepository.CountRecent(
                userId,
                ActivityActionNames.LoginFailed,
                now.AddMinutes(-15),
                false);

            await UpsertOrResolveObservation(
                userId,
                ObservationReasons.FailedLogins,
                failedLoginCount >= 3,
                $"{failedLoginCount} tentative esuate de autentificare in ultimele 15 minute.",
                Math.Min(95, 45 + failedLoginCount * 10),
                now);

            var permissionDeniedCount = await _activityLogRepository.CountRecent(
                userId,
                ActivityActionNames.PermissionDenied,
                now.AddMinutes(-10),
                false);

            await UpsertOrResolveObservation(
                userId,
                ObservationReasons.PermissionProbe,
                permissionDeniedCount >= 3,
                $"{permissionDeniedCount} accesari refuzate in ultimele 10 minute.",
                Math.Min(90, 35 + permissionDeniedCount * 8),
                now);

            var chatMessageCount = await _activityLogRepository.CountRecent(
                userId,
                ActivityActionNames.ChatMessageSent,
                now.AddMinutes(-2),
                true);

            await UpsertOrResolveObservation(
                userId,
                ObservationReasons.ChatBurst,
                chatMessageCount >= 20,
                $"{chatMessageCount} mesaje trimise in mai putin de 2 minute.",
                Math.Min(80, 25 + chatMessageCount * 2),
                now);

            var activeSessionCount = await _authSessionRepository.CountActiveSessions(userId);
            var distinctRecentIpCount = await _authSessionRepository.CountDistinctRecentRemoteIpAddresses(userId, now.AddHours(-12));

            await UpsertOrResolveObservation(
                userId,
                ObservationReasons.MultiSessionIpDrift,
                activeSessionCount >= 3 && distinctRecentIpCount >= 2,
                $"{activeSessionCount} sesiuni active provenite din {distinctRecentIpCount} IP-uri diferite in ultimele 12 ore.",
                Math.Min(92, 40 + activeSessionCount * 9 + distinctRecentIpCount * 7),
                now);

            var aiAssessment = await _aiSuspiciousActivityDetector.Assess(userId);
            await UpsertOrResolveObservation(
                userId,
                ObservationReasons.AiAnomaly,
                aiAssessment?.ShouldFlag == true,
                aiAssessment?.Details ?? "Scorul AI nu indica un risc crescut pentru acest utilizator.",
                aiAssessment?.RiskScore ?? 0,
                now);
        }

        public async Task<IReadOnlyList<ObservationResponse>> GetActiveObservations()
        {
            var observations = await _observationRepository.GetActive();
            return observations
                .Select(observation => new ObservationResponse
                {
                    Id = observation.Id,
                    UserId = observation.UserId,
                    UserName = observation.User.FullName,
                    Email = observation.User.Email,
                    GroupId = observation.User.UserRoles.Select(userRole => userRole.Role.Name).FirstOrDefault() ?? RoleNames.User,
                    Reason = observation.Reason,
                    Details = observation.Details,
                    RiskScore = observation.RiskScore,
                    FirstDetectedAtUtc = observation.FirstDetectedAtUtc,
                    LastDetectedAtUtc = observation.LastDetectedAtUtc
                })
                .ToArray();
        }

        private async Task UpsertOrResolveObservation(
            int userId,
            string reason,
            bool shouldBeActive,
            string details,
            int riskScore,
            DateTime detectedAtUtc)
        {
            var existingObservation = await _observationRepository.GetActiveByReason(userId, reason);
            if (!shouldBeActive)
            {
                if (existingObservation != null)
                {
                    existingObservation.IsActive = false;
                    existingObservation.LastDetectedAtUtc = detectedAtUtc;
                    await _observationRepository.SaveChanges();
                }

                return;
            }

            if (existingObservation == null)
            {
                await _observationRepository.Add(new UserObservation
                {
                    UserId = userId,
                    Reason = reason,
                    Details = details,
                    RiskScore = riskScore,
                    FirstDetectedAtUtc = detectedAtUtc,
                    LastDetectedAtUtc = detectedAtUtc,
                    IsActive = true
                });
                return;
            }

            existingObservation.Details = details;
            existingObservation.RiskScore = riskScore;
            existingObservation.LastDetectedAtUtc = detectedAtUtc;
            existingObservation.IsActive = true;

            await _observationRepository.SaveChanges();
        }
    }
}
