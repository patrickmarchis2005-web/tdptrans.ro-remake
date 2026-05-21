using Moq;
using TdpTrans.Models;
using TdpTrans.Repositories.Interfaces;
using TdpTrans.Services;

namespace TdpTrans.Tests
{
    public class SuspiciousActivityServiceTests
    {
        private readonly Mock<IActivityLogRepository> _activityLogRepository;
        private readonly Mock<IObservationRepository> _observationRepository;
        private readonly SuspiciousActivityService _service;

        public SuspiciousActivityServiceTests()
        {
            _activityLogRepository = new Mock<IActivityLogRepository>();
            _observationRepository = new Mock<IObservationRepository>();
            _service = new SuspiciousActivityService(_activityLogRepository.Object, _observationRepository.Object);
        }

        [Fact]
        public async Task Evaluate_WithRepeatedFailedLogins_CreatesObservation()
        {
            const int userId = 7;

            _activityLogRepository
                .Setup(repository => repository.CountRecent(
                    userId,
                    ActivityActionNames.LoginFailed,
                    It.IsAny<DateTime>(),
                    false))
                .ReturnsAsync(3);
            _activityLogRepository
                .Setup(repository => repository.CountRecent(
                    userId,
                    ActivityActionNames.PermissionDenied,
                    It.IsAny<DateTime>(),
                    false))
                .ReturnsAsync(0);
            _activityLogRepository
                .Setup(repository => repository.CountRecent(
                    userId,
                    ActivityActionNames.ChatMessageSent,
                    It.IsAny<DateTime>(),
                    true))
                .ReturnsAsync(0);
            _observationRepository
                .Setup(repository => repository.GetActiveByReason(userId, ObservationReasons.FailedLogins))
                .ReturnsAsync((UserObservation?)null);

            await _service.Evaluate(userId);

            _observationRepository.Verify(
                repository => repository.Add(It.Is<UserObservation>(observation =>
                    observation.UserId == userId &&
                    observation.Reason == ObservationReasons.FailedLogins &&
                    observation.RiskScore == 85 &&
                    observation.IsActive &&
                    observation.Details.Contains("3 tentative esuate"))),
                Times.Once);
        }

        [Fact]
        public async Task Evaluate_WithExistingPermissionObservation_RefreshesExistingRecord()
        {
            const int userId = 9;
            var observation = new UserObservation
            {
                Id = 4,
                UserId = userId,
                Reason = ObservationReasons.PermissionDenials,
                Details = "Old details",
                RiskScore = 50,
                IsActive = true,
                FirstDetectedAtUtc = DateTime.UtcNow.AddHours(-1),
                LastDetectedAtUtc = DateTime.UtcNow.AddHours(-1),
                User = new AppUser()
            };

            _activityLogRepository
                .Setup(repository => repository.CountRecent(
                    userId,
                    ActivityActionNames.LoginFailed,
                    It.IsAny<DateTime>(),
                    false))
                .ReturnsAsync(0);
            _activityLogRepository
                .Setup(repository => repository.CountRecent(
                    userId,
                    ActivityActionNames.PermissionDenied,
                    It.IsAny<DateTime>(),
                    false))
                .ReturnsAsync(3);
            _activityLogRepository
                .Setup(repository => repository.CountRecent(
                    userId,
                    ActivityActionNames.ChatMessageSent,
                    It.IsAny<DateTime>(),
                    true))
                .ReturnsAsync(0);
            _observationRepository
                .Setup(repository => repository.GetActiveByReason(userId, ObservationReasons.PermissionDenials))
                .ReturnsAsync(observation);

            await _service.Evaluate(userId);

            Assert.Equal(70, observation.RiskScore);
            Assert.Equal("3 accesari refuzate in ultimele 10 minute.", observation.Details);
            Assert.True(observation.IsActive);

            _observationRepository.Verify(repository => repository.SaveChanges(), Times.Once);
            _observationRepository.Verify(repository => repository.Add(It.IsAny<UserObservation>()), Times.Never);
        }

        [Fact]
        public async Task GetActiveObservations_MapsRoleAndUserDetails()
        {
            var role = new AppRole
            {
                Id = 1,
                Name = RoleNames.Admin,
                Description = "Administrator"
            };
            var user = new AppUser
            {
                Id = 5,
                FullName = "Administrator TDP",
                Email = "admin@tdptrans.ro",
                PasswordHash = "hash",
                CreatedAtUtc = DateTime.UtcNow,
                IsActive = true
            };
            user.UserRoles.Add(new UserRole
            {
                UserId = user.Id,
                User = user,
                RoleId = role.Id,
                Role = role
            });

            var observations = new List<UserObservation>
            {
                new UserObservation
                {
                    Id = 1,
                    UserId = user.Id,
                    User = user,
                    Reason = ObservationReasons.ChatSpam,
                    Details = "20 mesaje trimise in mai putin de 2 minute.",
                    RiskScore = 60,
                    FirstDetectedAtUtc = DateTime.UtcNow.AddMinutes(-3),
                    LastDetectedAtUtc = DateTime.UtcNow.AddMinutes(-1),
                    IsActive = true
                }
            };

            _observationRepository
                .Setup(repository => repository.GetActive())
                .ReturnsAsync(observations);

            var result = await _service.GetActiveObservations();

            Assert.Single(result);
            Assert.Equal(user.Id, result[0].UserId);
            Assert.Equal(user.FullName, result[0].UserName);
            Assert.Equal(RoleNames.Admin, result[0].GroupId);
            Assert.Equal(ObservationReasons.ChatSpam, result[0].Reason);
        }
    }
}
