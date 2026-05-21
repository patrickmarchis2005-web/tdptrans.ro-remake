using TdpTrans.DTOs;
using TdpTrans.Models;
using TdpTrans.Repositories.Interfaces;

namespace TdpTrans.Services
{
    public class SuspiciousActivityService : ISuspiciousActivityService
    {
        private readonly IActivityLogRepository _activityLogRepository;
        private readonly IObservationRepository _observationRepository;

        public SuspiciousActivityService(
            IActivityLogRepository activityLogRepository,
            IObservationRepository observationRepository)
        {
            _activityLogRepository = activityLogRepository;
            _observationRepository = observationRepository;
        }

        public async Task Evaluate(int userId)
        {
            var now = DateTime.UtcNow;

            var failedLoginCount = await _activityLogRepository.CountRecent(
                userId,
                ActivityActionNames.LoginFailed,
                now.AddMinutes(-15),
                false);

            if (failedLoginCount >= 3)
            {
                await UpsertObservation(
                    userId,
                    ObservationReasons.FailedLogins,
                    $"{failedLoginCount} tentative esuate de autentificare in ultimele 15 minute.",
                    85,
                    now);
            }

            var permissionDeniedCount = await _activityLogRepository.CountRecent(
                userId,
                ActivityActionNames.PermissionDenied,
                now.AddMinutes(-10),
                false);

            if (permissionDeniedCount >= 3)
            {
                await UpsertObservation(
                    userId,
                    ObservationReasons.PermissionDenials,
                    $"{permissionDeniedCount} accesari refuzate in ultimele 10 minute.",
                    70,
                    now);
            }

            var chatMessageCount = await _activityLogRepository.CountRecent(
                userId,
                ActivityActionNames.ChatMessageSent,
                now.AddMinutes(-2),
                true);

            if (chatMessageCount >= 20)
            {
                await UpsertObservation(
                    userId,
                    ObservationReasons.ChatSpam,
                    $"{chatMessageCount} mesaje trimise in mai putin de 2 minute.",
                    60,
                    now);
            }
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

        private async Task UpsertObservation(int userId, string reason, string details, int riskScore, DateTime detectedAtUtc)
        {
            var existingObservation = await _observationRepository.GetActiveByReason(userId, reason);
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
            existingObservation.RiskScore = Math.Max(existingObservation.RiskScore, riskScore);
            existingObservation.LastDetectedAtUtc = detectedAtUtc;
            existingObservation.IsActive = true;

            await _observationRepository.SaveChanges();
        }
    }
}
