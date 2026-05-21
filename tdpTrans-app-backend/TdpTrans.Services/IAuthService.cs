using TdpTrans.DTOs;

namespace TdpTrans.Services
{
    public interface IAuthService
    {
        Task<AuthenticatedUserResponse> Login(LoginRequest request);
        Task<AuthenticatedUserResponse> Register(SignupRequest request);
    }
}
