using Microsoft.AspNetCore.Mvc;
using TdpTrans.DTOs;
using TdpTrans.Models;
using TdpTrans.Services;

namespace TdpTrans.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("login")]
        public async Task<ActionResult<AuthenticatedUserResponse>> Login([FromBody] LoginRequest request)
        {
            try
            {
                var user = await _authService.Login(request, GetClientKey(), Request.Headers.UserAgent.ToString(), HttpContext.Connection.RemoteIpAddress?.ToString());
                return Ok(user);
            }
            catch (ArgumentException exception)
            {
                return BadRequest(exception.Message);
            }
            catch (UnauthorizedAccessException exception)
            {
                return Unauthorized(exception.Message);
            }
        }

        [HttpPost("signup")]
        public async Task<ActionResult<AuthenticatedUserResponse>> Signup([FromBody] SignupRequest request)
        {
            try
            {
                var user = await _authService.Register(request, GetClientKey(), Request.Headers.UserAgent.ToString(), HttpContext.Connection.RemoteIpAddress?.ToString());
                return Created(nameof(Signup), user);
            }
            catch (ArgumentException exception)
            {
                return BadRequest(exception.Message);
            }
        }

        [HttpPost("recover-password")]
        public async Task<ActionResult<AuthenticatedUserResponse>> RecoverPassword([FromBody] RecoverPasswordRequest request)
        {
            try
            {
                var user = await _authService.RecoverPassword(request, GetClientKey(), Request.Headers.UserAgent.ToString(), HttpContext.Connection.RemoteIpAddress?.ToString());
                return Ok(user);
            }
            catch (ArgumentException exception)
            {
                return BadRequest(exception.Message);
            }
            catch (UnauthorizedAccessException exception)
            {
                return Unauthorized(exception.Message);
            }
        }

        [HttpPost("request-credential-change-code")]
        public async Task<ActionResult<CredentialChangeCodeResponse>> RequestCredentialChangeCode([FromBody] CredentialChangeCodeRequest request)
        {
            try
            {
                var response = await _authService.RequestCredentialChangeCode(request, GetClientKey(), Request.Headers.UserAgent.ToString(), HttpContext.Connection.RemoteIpAddress?.ToString());
                return Ok(response);
            }
            catch (ArgumentException exception)
            {
                return BadRequest(exception.Message);
            }
            catch (InvalidOperationException exception)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, exception.Message);
            }
            catch (UnauthorizedAccessException exception)
            {
                return Unauthorized(exception.Message);
            }
        }

        [HttpPost("logout")]
        public async Task<ActionResult> Logout(
            [FromServices] IAuthSessionService authSessionService,
            [FromServices] IActivityLogService activityLogService)
        {
            var token = ExtractBearerToken(Request.Headers.Authorization);
            if (string.IsNullOrWhiteSpace(token))
            {
                return NoContent();
            }

            if (ActorHeaderReader.TryRead(Request, out var actorUserId))
            {
                await activityLogService.Log(actorUserId, ActivityActionNames.LogoutSucceeded, "User logged out successfully.");
            }

            await authSessionService.EndSession(token);
            return NoContent();
        }

        private string GetClientKey()
        {
            return Request.Headers["X-Client-Key"].FirstOrDefault()?.Trim() ?? string.Empty;
        }

        private static string? ExtractBearerToken(string? authorizationHeader)
        {
            if (string.IsNullOrWhiteSpace(authorizationHeader))
            {
                return null;
            }

            const string prefix = "Bearer ";
            return authorizationHeader.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                ? authorizationHeader[prefix.Length..].Trim()
                : null;
        }
    }
}
