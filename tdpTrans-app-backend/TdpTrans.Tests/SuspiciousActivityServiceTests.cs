using Moq;
using TdpTrans.Models;
using TdpTrans.Repositories.Interfaces;
using TdpTrans.Services;

namespace TdpTrans.Tests
{
    public class SuspiciousActivityServiceTests
    {
        [Fact]
        public async Task Evaluate_WhenAiDetectorFlagsRisk_AddsAiObservation()
        {
            var activityLogRepository = new Mock<IActivityLogRepository>();
            var observationRepository = new Mock<IObservationRepository>();
            var authSessionRepository = new Mock<IAuthSessionRepository>();
            var aiSuspiciousActivityDetector = new Mock<IAiSuspiciousActivityDetector>();

            activityLogRepository
                .Setup(repository => repository.CountRecent(7, ActivityActionNames.LoginFailed, It.IsAny<DateTime>(), false))
                .ReturnsAsync(0);
            activityLogRepository
                .Setup(repository => repository.CountRecent(7, ActivityActionNames.PermissionDenied, It.IsAny<DateTime>(), false))
                .ReturnsAsync(0);
            activityLogRepository
                .Setup(repository => repository.CountRecent(7, ActivityActionNames.ChatMessageSent, It.IsAny<DateTime>(), true))
                .ReturnsAsync(0);

            observationRepository
                .Setup(repository => repository.GetActiveByReason(7, It.IsAny<string>()))
                .ReturnsAsync((UserObservation?)null);

            authSessionRepository
                .Setup(repository => repository.CountActiveSessions(7))
                .ReturnsAsync(1);
            authSessionRepository
                .Setup(repository => repository.CountDistinctRecentRemoteIpAddresses(7, It.IsAny<DateTime>()))
                .ReturnsAsync(1);

            aiSuspiciousActivityDetector
                .Setup(detector => detector.Assess(7))
                .ReturnsAsync(new AiSuspiciousActivityAssessment
                {
                    ShouldFlag = true,
                    Probability = 0.87d,
                    RiskScore = 87,
                    Details = "Scor AI 87%. Semnale dominante: 4 login-uri esuate/15m."
                });

            var service = new SuspiciousActivityService(
                activityLogRepository.Object,
                observationRepository.Object,
                authSessionRepository.Object,
                aiSuspiciousActivityDetector.Object);

            await service.Evaluate(7);

            observationRepository.Verify(
                repository => repository.Add(It.Is<UserObservation>(observation =>
                    observation.UserId == 7 &&
                    observation.Reason == ObservationReasons.AiAnomaly &&
                    observation.RiskScore == 87 &&
                    observation.Details.Contains("Scor AI 87%"))),
                Times.Once);
        }
    }
}
