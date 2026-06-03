using TdpTrans.DTOs;

namespace TdpTrans.Services
{
    public interface IAuthService
    {
        Task<AuthenticatedUserResponse> Login(LoginRequest request, string? clientKey, string? userAgent, string? remoteIpAddress);
        Task<AuthenticatedUserResponse> Register(SignupRequest request, string? clientKey, string? userAgent, string? remoteIpAddress);
        Task<CredentialChangeCodeResponse> RequestCredentialChangeCode(CredentialChangeCodeRequest request, string? clientKey, string? userAgent, string? remoteIpAddress);
        Task<AuthenticatedUserResponse> RecoverPassword(RecoverPasswordRequest request, string? clientKey, string? userAgent, string? remoteIpAddress);
    }
}
