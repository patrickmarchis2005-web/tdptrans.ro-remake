using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using TdpTrans.Controllers;
using TdpTrans.DTOs;
using TdpTrans.Models;
using TdpTrans.Services;

namespace TdpTrans.Tests
{
    public class AuthControllerTests
    {
        private const string SessionActorItemKey = "tdptrans.session-actor";
        private readonly Mock<IAuthService> _authService;
        private readonly Mock<IAuthSessionService> _authSessionService;
        private readonly Mock<IActivityLogService> _activityLogService;

        public AuthControllerTests()
        {
            _authService = new Mock<IAuthService>();
            _authSessionService = new Mock<IAuthSessionService>();
            _activityLogService = new Mock<IActivityLogService>();
        }

        [Fact]
        public async Task Login_WithValidRequest_ReturnsOkAndPassesRequestMetadata()
        {
            var request = new LoginRequest
            {
                Email = "admin@tdptrans.ro",
                Password = "12345678",
                SecurityCode = "246810",
                AuthenticationPhrase = "TDP-ADMIN"
            };
            var expectedResponse = BuildAuthenticatedResponse();
            _authService
                .Setup(service => service.Login(request, "client-1", "Unit Test Agent", "127.0.0.1"))
                .ReturnsAsync(expectedResponse);

            var controller = CreateController(CreateHttpContext());

            var result = await controller.Login(request);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var payload = Assert.IsType<AuthenticatedUserResponse>(okResult.Value);

            Assert.Same(expectedResponse, payload);
            _authService.Verify(service => service.Login(request, "client-1", "Unit Test Agent", "127.0.0.1"), Times.Once);
        }

        [Fact]
        public async Task Login_WhenUnauthorizedAccessExceptionIsThrown_ReturnsUnauthorized()
        {
            var request = new LoginRequest
            {
                Email = "admin@tdptrans.ro",
                Password = "12345678",
                SecurityCode = "000000",
                AuthenticationPhrase = "TDP-ADMIN"
            };
            _authService
                .Setup(service => service.Login(request, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new UnauthorizedAccessException("Codul de securitate este incorect."));

            var controller = CreateController(CreateHttpContext());

            var result = await controller.Login(request);

            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result.Result);
            Assert.Equal("Codul de securitate este incorect.", unauthorizedResult.Value);
        }

        [Fact]
        public async Task Signup_WithValidRequest_ReturnsCreatedAndPassesRequestMetadata()
        {
            var request = new SignupRequest
            {
                FullName = "Utilizator Nou",
                Email = "nou@tdptrans.ro",
                Password = "12345678",
                SecurityCode = "246810",
                AuthenticationPhrase = "FRAZA-NOUA"
            };
            var expectedResponse = BuildAuthenticatedResponse();
            _authService
                .Setup(service => service.Register(request, "client-1", "Unit Test Agent", "127.0.0.1"))
                .ReturnsAsync(expectedResponse);

            var controller = CreateController(CreateHttpContext());

            var result = await controller.Signup(request);

            var createdResult = Assert.IsType<CreatedResult>(result.Result);
            var payload = Assert.IsType<AuthenticatedUserResponse>(createdResult.Value);

            Assert.Equal(nameof(AuthController.Signup), createdResult.Location);
            Assert.Same(expectedResponse, payload);
            _authService.Verify(service => service.Register(request, "client-1", "Unit Test Agent", "127.0.0.1"), Times.Once);
        }

        [Fact]
        public async Task Signup_WhenArgumentExceptionIsThrown_ReturnsBadRequest()
        {
            var request = new SignupRequest
            {
                FullName = "Utilizator Nou",
                Email = "admin@tdptrans.ro",
                Password = "12345678",
                SecurityCode = "246810",
                AuthenticationPhrase = "FRAZA-NOUA"
            };
            _authService
                .Setup(service => service.Register(request, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new ArgumentException("Adresa admin@tdptrans.ro este rezervata contului administrator."));

            var controller = CreateController(CreateHttpContext());

            var result = await controller.Signup(request);

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal("Adresa admin@tdptrans.ro este rezervata contului administrator.", badRequestResult.Value);
        }

        [Fact]
        public async Task RecoverPassword_WithValidRequest_ReturnsOkAndPassesRequestMetadata()
        {
            var request = new RecoverPasswordRequest
            {
                Email = "recover@tdptrans.ro",
                CredentialChangeCode = "482915",
                NewPassword = "12345678",
                NewSecurityCode = "112233",
                NewAuthenticationPhrase = "PHRASE-RESET"
            };
            var expectedResponse = BuildAuthenticatedResponse();
            _authService
                .Setup(service => service.RecoverPassword(request, "client-1", "Unit Test Agent", "127.0.0.1"))
                .ReturnsAsync(expectedResponse);

            var controller = CreateController(CreateHttpContext());

            var result = await controller.RecoverPassword(request);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var payload = Assert.IsType<AuthenticatedUserResponse>(okResult.Value);

            Assert.Same(expectedResponse, payload);
            _authService.Verify(service => service.RecoverPassword(request, "client-1", "Unit Test Agent", "127.0.0.1"), Times.Once);
        }

        [Fact]
        public async Task RequestCredentialChangeCode_WithValidRequest_ReturnsOkAndPassesRequestMetadata()
        {
            var request = new CredentialChangeCodeRequest
            {
                Email = "recover@tdptrans.ro"
            };
            var expectedResponse = new CredentialChangeCodeResponse
            {
                CredentialChangeCode = "482915",
                ExpiresAtUtc = DateTime.UtcNow.AddMinutes(5)
            };
            _authService
                .Setup(service => service.RequestCredentialChangeCode(request, "client-1", "Unit Test Agent", "127.0.0.1"))
                .ReturnsAsync(expectedResponse);

            var controller = CreateController(CreateHttpContext());

            var result = await controller.RequestCredentialChangeCode(request);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var payload = Assert.IsType<CredentialChangeCodeResponse>(okResult.Value);

            Assert.Same(expectedResponse, payload);
            _authService.Verify(service => service.RequestCredentialChangeCode(request, "client-1", "Unit Test Agent", "127.0.0.1"), Times.Once);
        }

        [Fact]
        public async Task Logout_WithBearerToken_LogsActivityAndEndsSession()
        {
            var httpContext = CreateHttpContext();
            httpContext.Request.Headers.Authorization = "Bearer access-token";
            httpContext.Items[SessionActorItemKey] = new SessionActor
            {
                UserId = 42,
                Email = "admin@tdptrans.ro",
                FullName = "Administrator TDP",
                RoleName = RoleNames.Admin,
                Permissions = Array.Empty<string>(),
                SessionExpiresAtUtc = DateTime.UtcNow.AddHours(1),
                SessionIdleTimeoutSeconds = 900
            };

            var controller = CreateController(httpContext);

            var result = await controller.Logout(_authSessionService.Object, _activityLogService.Object);

            Assert.IsType<NoContentResult>(result);
            _activityLogService.Verify(
                service => service.Log(42, ActivityActionNames.LogoutSucceeded, "User logged out successfully.", true, null),
                Times.Once);
            _authSessionService.Verify(service => service.EndSession("access-token"), Times.Once);
        }

        [Fact]
        public async Task Logout_WithoutBearerToken_ReturnsNoContentWithoutTouchingServices()
        {
            var controller = CreateController(CreateHttpContext());

            var result = await controller.Logout(_authSessionService.Object, _activityLogService.Object);

            Assert.IsType<NoContentResult>(result);
            _activityLogService.Verify(
                service => service.Log(It.IsAny<int?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string?>()),
                Times.Never);
            _authSessionService.Verify(service => service.EndSession(It.IsAny<string>()), Times.Never);
        }

        private AuthController CreateController(DefaultHttpContext httpContext)
        {
            return new AuthController(_authService.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = httpContext
                }
            };
        }

        private static DefaultHttpContext CreateHttpContext()
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers.UserAgent = "Unit Test Agent";
            httpContext.Request.Headers["X-Client-Key"] = "client-1";
            httpContext.Connection.RemoteIpAddress = IPAddress.Parse("127.0.0.1");
            return httpContext;
        }

        private static AuthenticatedUserResponse BuildAuthenticatedResponse()
        {
            return new AuthenticatedUserResponse
            {
                Id = 1,
                FullName = "Administrator TDP",
                Email = "admin@tdptrans.ro",
                RoleName = RoleNames.Admin,
                Permissions = new[] { PermissionNames.ChatUse },
                AccessToken = "test-access-token",
                SessionExpiresAtUtc = DateTime.UtcNow.AddHours(12),
                SessionIdleTimeoutSeconds = 900
            };
        }
    }
}
