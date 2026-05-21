using Microsoft.AspNetCore.Mvc;
using TdpTrans.DTOs;
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
                var user = await _authService.Login(request);
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
                var user = await _authService.Register(request);
                return Created(nameof(Signup), user);
            }
            catch (ArgumentException exception)
            {
                return BadRequest(exception.Message);
            }
        }
    }
}
