using Moq;
using TdpTrans.DTOs;
using TdpTrans.Models;
using TdpTrans.Repositories.Interfaces;
using TdpTrans.Services;

namespace TdpTrans.Tests
{
    public class AuthServiceTests
    {
        private readonly Mock<IUsersRepository> _usersRepository;
        private readonly Mock<IActivityLogService> _activityLogService;
        private readonly AuthService _service;

        public AuthServiceTests()
        {
            _usersRepository = new Mock<IUsersRepository>();
            _activityLogService = new Mock<IActivityLogService>();
            _service = new AuthService(_usersRepository.Object, _activityLogService.Object);
        }

        [Fact]
        public async Task Login_WithValidCredentials_ReturnsMappedUserAndLogsSuccess()
        {
            var user = BuildUser(
                1,
                "Administrator TDP",
                "admin@tdptrans.ro",
                "12345678",
                RoleNames.Admin,
                PermissionNames.ChatUse,
                PermissionNames.LogsView,
                PermissionNames.MissionsManage,
                PermissionNames.ObservationsView);

            _usersRepository
                .Setup(repository => repository.GetUserByEmail("admin@tdptrans.ro"))
                .ReturnsAsync(user);

            var response = await _service.Login(new LoginRequest
            {
                Email = " admin@tdptrans.ro ",
                Password = "12345678"
            });

            Assert.Equal(user.Id, response.Id);
            Assert.Equal(RoleNames.Admin, response.RoleName);
            Assert.Contains(PermissionNames.MissionsManage, response.Permissions);
            Assert.Contains(PermissionNames.ChatUse, response.Permissions);

            _activityLogService.Verify(
                service => service.Log(
                    user.Id,
                    ActivityActionNames.LoginSucceeded,
                    "User logged in successfully.",
                    true,
                    null),
                Times.Once);
        }

        [Fact]
        public async Task Login_WithUnknownUser_LogsFailureAndThrowsUnauthorizedAccessException()
        {
            _usersRepository
                .Setup(repository => repository.GetUserByEmail("missing@tdptrans.ro"))
                .ReturnsAsync((AppUser?)null);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _service.Login(new LoginRequest
                {
                    Email = " missing@tdptrans.ro ",
                    Password = "12345678"
                }));

            _activityLogService.Verify(
                service => service.Log(
                    null,
                    ActivityActionNames.LoginFailed,
                    It.Is<string>(value => value.Contains("missing@tdptrans.ro")),
                    false,
                    "anonymous"),
                Times.Once);
        }

        [Fact]
        public async Task Register_WithValidData_CreatesStandardUserAndLogsSignup()
        {
            var request = new SignupRequest
            {
                FullName = "Utilizator Nou",
                Email = " Nou@TdpTrans.Ro ",
                Password = "12345678"
            };

            var role = new AppRole
            {
                Id = 2,
                Name = RoleNames.User,
                Description = "Standard user"
            };
            role.RolePermissions.Add(new RolePermission
            {
                RoleId = role.Id,
                Role = role,
                PermissionId = 2,
                Permission = new AppPermission
                {
                    Id = 2,
                    Name = PermissionNames.ChatUse,
                    Description = "Chat permission"
                }
            });

            var createdUser = new AppUser
            {
                Id = 10,
                FullName = request.FullName,
                Email = "nou@tdptrans.ro",
                PasswordHash = CredentialHasher.HashPassword(request.Password),
                CreatedAtUtc = DateTime.UtcNow,
                IsActive = true
            };

            var registeredUser = BuildUser(
                createdUser.Id,
                request.FullName,
                "nou@tdptrans.ro",
                request.Password,
                RoleNames.User,
                PermissionNames.ChatUse);

            _usersRepository
                .Setup(repository => repository.GetUserByEmail("nou@tdptrans.ro"))
                .ReturnsAsync((AppUser?)null);
            _usersRepository
                .Setup(repository => repository.GetRoleByName(RoleNames.User))
                .ReturnsAsync(role);
            _usersRepository
                .Setup(repository => repository.AddUser(It.Is<AppUser>(user =>
                    user.FullName == request.FullName &&
                    user.Email == "nou@tdptrans.ro" &&
                    user.IsActive)))
                .ReturnsAsync(createdUser);
            _usersRepository
                .Setup(repository => repository.GetUserById(createdUser.Id))
                .ReturnsAsync(registeredUser);

            var response = await _service.Register(request);

            Assert.Equal(createdUser.Id, response.Id);
            Assert.Equal(RoleNames.User, response.RoleName);
            Assert.Single(response.Permissions);
            Assert.Contains(PermissionNames.ChatUse, response.Permissions);

            _usersRepository.Verify(
                repository => repository.AddUserRole(It.Is<UserRole>(userRole =>
                    userRole.UserId == createdUser.Id &&
                    userRole.RoleId == role.Id)),
                Times.Once);

            _activityLogService.Verify(
                service => service.Log(
                    createdUser.Id,
                    ActivityActionNames.SignupCreated,
                    "User self-registered through the signup screen.",
                    true,
                    RoleNames.User),
                Times.Once);
        }

        [Fact]
        public async Task Register_WithReservedAdminEmail_RejectsRegistration()
        {
            var request = new SignupRequest
            {
                FullName = "Pretins Administrator",
                Email = " ADMIN@TDPTRANS.RO ",
                Password = "12345678"
            };

            var exception = await Assert.ThrowsAsync<ArgumentException>(() => _service.Register(request));

            Assert.Equal("Adresa admin@tdptrans.ro este rezervata contului administrator.", exception.Message);
            _usersRepository.Verify(repository => repository.AddUser(It.IsAny<AppUser>()), Times.Never);
            _usersRepository.Verify(repository => repository.AddUserRole(It.IsAny<UserRole>()), Times.Never);
        }

        private static AppUser BuildUser(
            int id,
            string fullName,
            string email,
            string password,
            string roleName,
            params string[] permissions)
        {
            var role = new AppRole
            {
                Id = id,
                Name = roleName,
                Description = $"{roleName} role"
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
                FullName = fullName,
                Email = email,
                PasswordHash = CredentialHasher.HashPassword(password),
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

            return user;
        }
    }
}
