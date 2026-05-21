using Moq;
using TdpTrans.Models;
using TdpTrans.Repositories.Interfaces;
using TdpTrans.Services;

namespace TdpTrans.Tests
{
    public class UserAccessServiceTests
    {
        private readonly Mock<IUsersRepository> _usersRepository;
        private readonly Mock<IActivityLogService> _activityLogService;
        private readonly UserAccessService _service;

        public UserAccessServiceTests()
        {
            _usersRepository = new Mock<IUsersRepository>();
            _activityLogService = new Mock<IActivityLogService>();
            _service = new UserAccessService(_usersRepository.Object, _activityLogService.Object);
        }

        [Fact]
        public async Task EnsurePermission_WithExistingPermission_ReturnsUser()
        {
            var user = BuildUser(1, true, PermissionNames.MissionsManage, PermissionNames.ChatUse);

            _usersRepository
                .Setup(repository => repository.GetUserById(user.Id))
                .ReturnsAsync(user);

            var result = await _service.EnsurePermission(
                user.Id,
                PermissionNames.MissionsManage,
                "Attempted missions management.");

            Assert.Same(user, result);
            _activityLogService.Verify(
                service => service.Log(
                    It.IsAny<int?>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<bool>(),
                    It.IsAny<string?>()),
                Times.Never);
        }

        [Fact]
        public async Task EnsurePermission_WithoutPermission_LogsFailureAndThrowsUnauthorizedAccessException()
        {
            var user = BuildUser(2, true, PermissionNames.ChatUse);

            _usersRepository
                .Setup(repository => repository.GetUserById(user.Id))
                .ReturnsAsync(user);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _service.EnsurePermission(
                    user.Id,
                    PermissionNames.MissionsManage,
                    "Attempted missions management."));

            _activityLogService.Verify(
                service => service.Log(
                    user.Id,
                    ActivityActionNames.PermissionDenied,
                    "Attempted missions management.",
                    false,
                    null),
                Times.Once);
        }

        [Fact]
        public async Task RequireUser_WithInactiveUser_ThrowsUnauthorizedAccessException()
        {
            var user = BuildUser(3, false, PermissionNames.ChatUse);

            _usersRepository
                .Setup(repository => repository.GetUserById(user.Id))
                .ReturnsAsync(user);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _service.RequireUser(user.Id));
        }

        private static AppUser BuildUser(int id, bool isActive, params string[] permissions)
        {
            var role = new AppRole
            {
                Id = id,
                Name = RoleNames.User,
                Description = "Standard user"
            };

            for (var index = 0; index < permissions.Length; index++)
            {
                role.RolePermissions.Add(new RolePermission
                {
                    RoleId = role.Id,
                    Role = role,
                    PermissionId = index + 1,
                    Permission = new AppPermission
                    {
                        Id = index + 1,
                        Name = permissions[index],
                        Description = permissions[index]
                    }
                });
            }

            var user = new AppUser
            {
                Id = id,
                FullName = $"User {id}",
                Email = $"user{id}@tdptrans.ro",
                PasswordHash = "hash",
                CreatedAtUtc = DateTime.UtcNow,
                IsActive = isActive
            };

            user.UserRoles.Add(new UserRole
            {
                UserId = user.Id,
                User = user,
                RoleId = role.Id,
                Role = role
            });

            return user;
        }
    }
}
