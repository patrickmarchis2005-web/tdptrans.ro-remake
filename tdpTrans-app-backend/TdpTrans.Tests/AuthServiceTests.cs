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
        private readonly Mock<IAuthSessionService> _authSessionService;
        private readonly Mock<ICredentialChangeCodeService> _credentialChangeCodeService;
        private readonly AuthService _service;

        public AuthServiceTests()
        {
            _usersRepository = new Mock<IUsersRepository>();
            _activityLogService = new Mock<IActivityLogService>();
            _authSessionService = new Mock<IAuthSessionService>();
            _credentialChangeCodeService = new Mock<ICredentialChangeCodeService>();
            _service = new AuthService(
                _usersRepository.Object,
                _activityLogService.Object,
                _authSessionService.Object,
                _credentialChangeCodeService.Object);
        }

        [Fact]
        public async Task Login_WithValidCredentials_ReturnsMappedUserAndLogsSuccess()
        {
            var user = BuildUser(
                1,
                "Administrator TDP",
                "admin@tdptrans.ro",
                "12345678",
                "246810",
                "TDP-ADMIN",
                RoleNames.Admin,
                PermissionNames.ChatUse,
                PermissionNames.LogsView,
                PermissionNames.MissionsManage);

            _usersRepository
                .Setup(repository => repository.GetUserByEmail("admin@tdptrans.ro"))
                .ReturnsAsync(user);
            _authSessionService
                .Setup(service => service.StartSession(user, "client-1", "Unit Test Agent", "127.0.0.1"))
                .ReturnsAsync(BuildAuthenticatedResponse(user));

            var response = await _service.Login(
                new LoginRequest
                {
                    Email = " Admin@TdpTrans.Ro ",
                    Password = "12345678",
                    SecurityCode = "246810",
                    AuthenticationPhrase = " tdp-admin "
                },
                "client-1",
                "Unit Test Agent",
                "127.0.0.1");

            Assert.Equal(user.Id, response.Id);
            Assert.Equal("test-access-token", response.AccessToken);

            _activityLogService.Verify(
                service => service.Log(user.Id, ActivityActionNames.LoginSucceeded, "User logged in successfully.", true, null),
                Times.Once);
        }

        [Fact]
        public async Task Login_WithUnknownUser_LogsFailureAndThrowsUnauthorizedAccessException()
        {
            _usersRepository
                .Setup(repository => repository.GetUserByEmail("missing@tdptrans.ro"))
                .ReturnsAsync((AppUser?)null);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _service.Login(
                new LoginRequest
                {
                    Email = "missing@tdptrans.ro",
                    Password = "12345678",
                    SecurityCode = "246810",
                    AuthenticationPhrase = "TDP-USER"
                },
                "client-1",
                "Unit Test Agent",
                "127.0.0.1"));

            _activityLogService.Verify(
                service => service.Log(null, ActivityActionNames.LoginFailed, "Failed login attempt for missing@tdptrans.ro.", false, "anonymous"),
                Times.Once);
        }

        [Fact]
        public async Task Login_WithInvalidSecurityCode_LogsFailureAndThrowsUnauthorizedAccessException()
        {
            var user = BuildUser(
                2,
                "Sofer Test",
                "sofer@tdptrans.ro",
                "12345678",
                "135790",
                "TDP-USER",
                RoleNames.User,
                PermissionNames.ChatUse);

            _usersRepository
                .Setup(repository => repository.GetUserByEmail("sofer@tdptrans.ro"))
                .ReturnsAsync(user);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _service.Login(
                new LoginRequest
                {
                    Email = "sofer@tdptrans.ro",
                    Password = "12345678",
                    SecurityCode = "000000",
                    AuthenticationPhrase = "TDP-USER"
                },
                "client-1",
                "Unit Test Agent",
                "127.0.0.1"));

            _activityLogService.Verify(
                service => service.Log(user.Id, ActivityActionNames.LoginFailed, "Failed security code challenge for sofer@tdptrans.ro.", false, null),
                Times.Once);
        }

        [Fact]
        public async Task Login_WithInvalidAuthenticationPhrase_LogsFailureAndThrowsUnauthorizedAccessException()
        {
            var user = BuildUser(
                3,
                "Sofer Test",
                "sofer@tdptrans.ro",
                "12345678",
                "135790",
                "TDP-USER",
                RoleNames.User,
                PermissionNames.ChatUse);

            _usersRepository
                .Setup(repository => repository.GetUserByEmail("sofer@tdptrans.ro"))
                .ReturnsAsync(user);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _service.Login(
                new LoginRequest
                {
                    Email = "sofer@tdptrans.ro",
                    Password = "12345678",
                    SecurityCode = "135790",
                    AuthenticationPhrase = "alt phrase"
                },
                "client-1",
                "Unit Test Agent",
                "127.0.0.1"));

            _activityLogService.Verify(
                service => service.Log(user.Id, ActivityActionNames.LoginFailed, "Failed authentication phrase challenge for sofer@tdptrans.ro.", false, null),
                Times.Once);
        }

        [Fact]
        public async Task Register_WithValidData_CreatesStandardUserAndLogsSignup()
        {
            var role = new AppRole
            {
                Id = 7,
                Name = RoleNames.User
            };
            AppUser? addedUser = null;

            _usersRepository
                .Setup(repository => repository.GetUserByEmail("nou@tdptrans.ro"))
                .ReturnsAsync((AppUser?)null);
            _usersRepository
                .Setup(repository => repository.GetRoleByName(RoleNames.User))
                .ReturnsAsync(role);
            _usersRepository
                .Setup(repository => repository.AddUser(It.IsAny<AppUser>()))
                .ReturnsAsync((AppUser user) =>
                {
                    user.Id = 15;
                    addedUser = user;
                    return user;
                });
            _usersRepository
                .Setup(repository => repository.AddUserRole(It.IsAny<UserRole>()))
                .Callback<UserRole>(assignment =>
                {
                    if (addedUser == null)
                    {
                        return;
                    }

                    addedUser.UserRoles.Add(new UserRole
                    {
                        UserId = assignment.UserId,
                        RoleId = assignment.RoleId,
                        Role = role
                    });
                })
                .Returns(Task.CompletedTask);
            _usersRepository
                .Setup(repository => repository.GetUserById(15))
                .ReturnsAsync(() => addedUser);
            _authSessionService
                .Setup(service => service.StartSession(It.IsAny<AppUser>(), "client-1", "Unit Test Agent", "127.0.0.1"))
                .ReturnsAsync((AppUser user, string? _, string? _, string? _) => BuildAuthenticatedResponse(user));

            var request = new SignupRequest
            {
                FullName = " Utilizator Nou ",
                Email = " Nou@TdpTrans.Ro ",
                Password = "12345678",
                SecurityCode = "246810",
                AuthenticationPhrase = "  FRAZA-NOUA  "
            };

            var response = await _service.Register(request, "client-1", "Unit Test Agent", "127.0.0.1");

            Assert.Equal(15, response.Id);
            Assert.NotNull(addedUser);
            Assert.Equal("Utilizator Nou", addedUser!.FullName);
            Assert.Equal("nou@tdptrans.ro", addedUser.Email);
            Assert.True(CredentialHasher.VerifyPassword("12345678", addedUser.PasswordHash));
            Assert.True(CredentialHasher.VerifySecurityCode("246810", addedUser.SecurityCodeHash));
            Assert.True(CredentialHasher.VerifyAuthenticationPhrase("FRAZA-NOUA", addedUser.AuthenticationPhraseHash));

            _usersRepository.Verify(
                repository => repository.AddUserRole(It.Is<UserRole>(assignment => assignment.UserId == 15 && assignment.RoleId == role.Id)),
                Times.Once);
            _activityLogService.Verify(
                service => service.Log(15, ActivityActionNames.SignupCreated, "User self-registered through the signup screen.", true, RoleNames.User),
                Times.Once);
        }

        [Fact]
        public async Task Register_WithReservedAdminEmail_RejectsRegistration()
        {
            var request = new SignupRequest
            {
                FullName = "Admin",
                Email = "admin@tdptrans.ro",
                Password = "12345678",
                SecurityCode = "246810",
                AuthenticationPhrase = "FRAZA-ADMIN"
            };

            var exception = await Assert.ThrowsAsync<ArgumentException>(() => _service.Register(
                request,
                "client-1",
                "Unit Test Agent",
                "127.0.0.1"));

            Assert.Equal("Adresa admin@tdptrans.ro este rezervata contului administrator.", exception.Message);
        }

        [Fact]
        public async Task RequestCredentialChangeCode_WithKnownUser_ReturnsGeneratedCode()
        {
            var user = BuildUser(
                4,
                "Recuperare Test",
                "recover@tdptrans.ro",
                "12345678",
                "246810",
                "PHRASE-OLD",
                RoleNames.User,
                PermissionNames.ChatUse);
            var issuedCode = new CredentialChangeCodeIssuance("482915", DateTime.UtcNow.AddMinutes(5));

            _usersRepository
                .Setup(repository => repository.GetUserByEmail("recover@tdptrans.ro"))
                .ReturnsAsync(user);
            _credentialChangeCodeService
                .Setup(service => service.IssueCode("recover@tdptrans.ro", "client-1"))
                .Returns(issuedCode);

            var response = await _service.RequestCredentialChangeCode(
                new CredentialChangeCodeRequest
                {
                    Email = " Recover@TdpTrans.Ro "
                },
                "client-1",
                "Unit Test Agent",
                "127.0.0.1");

            Assert.Equal("482915", response.CredentialChangeCode);
            Assert.Equal(issuedCode.ExpiresAtUtc, response.ExpiresAtUtc);
        }

        [Fact]
        public async Task RecoverPassword_WithInvalidCredentialChangeCode_ThrowsUnauthorizedAccessException()
        {
            var user = BuildUser(
                4,
                "Recuperare Test",
                "recover@tdptrans.ro",
                "12345678",
                "246810",
                "PHRASE-OLD",
                RoleNames.User,
                PermissionNames.ChatUse);

            _usersRepository
                .Setup(repository => repository.GetUserByEmail("recover@tdptrans.ro"))
                .ReturnsAsync(user);
            _credentialChangeCodeService
                .Setup(service => service.VerifyCode("recover@tdptrans.ro", "client-1", "999999"))
                .Returns(false);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _service.RecoverPassword(
                new RecoverPasswordRequest
                {
                    Email = "recover@tdptrans.ro",
                    CredentialChangeCode = "999999",
                    NewPassword = "parola_noua",
                    NewSecurityCode = "112233",
                    NewAuthenticationPhrase = "PHRASE-RESET"
                },
                "client-1",
                "Unit Test Agent",
                "127.0.0.1"));

            _activityLogService.Verify(
                service => service.Log(user.Id, ActivityActionNames.PasswordRecoveryFailed, "Failed credential change confirmation challenge for recover@tdptrans.ro.", false, null),
                Times.Once);
        }

        [Fact]
        public async Task RecoverPassword_WithValidCredentialChangeCode_UpdatesCredentialsAndStartsFreshSession()
        {
            var user = BuildUser(
                5,
                "Recuperare Test",
                "recover@tdptrans.ro",
                "12345678",
                "246810",
                "PHRASE-OLD",
                RoleNames.User,
                PermissionNames.ChatUse);

            _usersRepository
                .Setup(repository => repository.GetUserByEmail("recover@tdptrans.ro"))
                .ReturnsAsync(user);
            _usersRepository
                .Setup(repository => repository.SaveChanges())
                .Returns(Task.CompletedTask);
            _credentialChangeCodeService
                .Setup(service => service.VerifyCode("recover@tdptrans.ro", "client-1", "482915"))
                .Returns(true);
            _authSessionService
                .Setup(service => service.StartSession(user, "client-1", "Unit Test Agent", "127.0.0.1"))
                .ReturnsAsync(BuildAuthenticatedResponse(user));

            var response = await _service.RecoverPassword(
                new RecoverPasswordRequest
                {
                    Email = " Recover@TdpTrans.Ro ",
                    CredentialChangeCode = " 482915 ",
                    NewPassword = "parola_noua",
                    NewSecurityCode = "112233",
                    NewAuthenticationPhrase = "  phrase-reset  "
                },
                "client-1",
                "Unit Test Agent",
                "127.0.0.1");

            Assert.Equal(user.Id, response.Id);
            Assert.True(CredentialHasher.VerifyPassword("parola_noua", user.PasswordHash));
            Assert.True(CredentialHasher.VerifySecurityCode("112233", user.SecurityCodeHash));
            Assert.True(CredentialHasher.VerifyAuthenticationPhrase("PHRASE-RESET", user.AuthenticationPhraseHash));

            _credentialChangeCodeService.Verify(service => service.ClearCode("recover@tdptrans.ro", "client-1"), Times.Once);
            _authSessionService.Verify(service => service.EndAllUserSessions(user.Id), Times.Once);
            _activityLogService.Verify(
                service => service.Log(user.Id, ActivityActionNames.PasswordRecoverySucceeded, "Updated account credentials using the on-screen confirmation code.", true, null),
                Times.Once);
        }

        private static AppUser BuildUser(
            int id,
            string fullName,
            string email,
            string password,
            string securityCode,
            string authenticationPhrase,
            string roleName,
            params string[] permissions)
        {
            var role = new AppRole
            {
                Id = id + 100,
                Name = roleName,
                RolePermissions = permissions
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Select((permission, index) => new RolePermission
                    {
                        RoleId = id + 100,
                        PermissionId = index + 1,
                        Permission = new AppPermission
                        {
                            Id = index + 1,
                            Name = permission
                        }
                    })
                    .ToList()
            };

            return new AppUser
            {
                Id = id,
                FullName = fullName,
                Email = email,
                PasswordHash = CredentialHasher.HashPassword(password),
                SecurityCodeHash = CredentialHasher.HashSecurityCode(securityCode),
                AuthenticationPhraseHash = CredentialHasher.HashAuthenticationPhrase(authenticationPhrase),
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow.AddDays(-2),
                UserRoles =
                {
                    new UserRole
                    {
                        UserId = id,
                        RoleId = role.Id,
                        Role = role
                    }
                }
            };
        }

        private static AuthenticatedUserResponse BuildAuthenticatedResponse(AppUser user)
        {
            return new AuthenticatedUserResponse
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                RoleName = user.UserRoles.Select(userRole => userRole.Role.Name).First(),
                Permissions = user.UserRoles
                    .SelectMany(userRole => userRole.Role.RolePermissions)
                    .Select(rolePermission => rolePermission.Permission.Name)
                    .ToArray(),
                AccessToken = "test-access-token",
                SessionExpiresAtUtc = DateTime.UtcNow.AddHours(12),
                SessionIdleTimeoutSeconds = 900
            };
        }
    }
}
