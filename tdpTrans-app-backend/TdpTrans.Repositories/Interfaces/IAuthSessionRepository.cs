using TdpTrans.Models;

namespace TdpTrans.Repositories.Interfaces
{
    public interface IAuthSessionRepository
    {
        Task<AuthSession> Add(AuthSession session);
        Task<AuthSession?> GetActiveByTokenHash(string tokenHash);
        Task<int> CountActiveSessions(int userId);
        Task<int> CountDistinctRecentRemoteIpAddresses(int userId, DateTime sinceUtc);
        Task RevokeByTokenHash(string tokenHash, DateTime revokedAtUtc);
        Task RevokeAllForUser(int userId, DateTime revokedAtUtc);
        Task SaveChanges();
    }
}
