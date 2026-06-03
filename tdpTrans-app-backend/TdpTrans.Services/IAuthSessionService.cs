using TdpTrans.DTOs;
using TdpTrans.Models;

namespace TdpTrans.Services
{
    public interface IAuthSessionService
    {
        Task<AuthenticatedUserResponse> StartSession(
            AppUser user,
            string? clientKey,
            string? userAgent,
            string? remoteIpAddress);

        Task<SessionActor?> Authenticate(string token, bool touchActivity = true);
        Task EndSession(string token);
        Task EndAllUserSessions(int userId);
    }
}
