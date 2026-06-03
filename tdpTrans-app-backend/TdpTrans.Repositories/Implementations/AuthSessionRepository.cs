using Microsoft.EntityFrameworkCore;
using TdpTrans.Models;
using TdpTrans.Repositories.Interfaces;

namespace TdpTrans.Repositories.Implementations
{
    public class AuthSessionRepository : IAuthSessionRepository
    {
        private readonly ApplicationDbContext _context;

        public AuthSessionRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<AuthSession> Add(AuthSession session)
        {
            _context.AuthSessions.Add(session);
            await _context.SaveChangesAsync();
            return session;
        }

        public async Task<AuthSession?> GetActiveByTokenHash(string tokenHash)
        {
            return await _context.AuthSessions
                .Include(session => session.User)
                    .ThenInclude(user => user.UserRoles)
                        .ThenInclude(userRole => userRole.Role)
                            .ThenInclude(role => role.RolePermissions)
                                .ThenInclude(rolePermission => rolePermission.Permission)
                .FirstOrDefaultAsync(session =>
                    session.TokenHash == tokenHash &&
                    session.RevokedAtUtc == null);
        }

        public async Task<int> CountActiveSessions(int userId)
        {
            return await _context.AuthSessions
                .CountAsync(session => session.UserId == userId && session.RevokedAtUtc == null);
        }

        public async Task<int> CountDistinctRecentRemoteIpAddresses(int userId, DateTime sinceUtc)
        {
            return await _context.AuthSessions
                .Where(session =>
                    session.UserId == userId &&
                    session.RevokedAtUtc == null &&
                    session.CreatedAtUtc >= sinceUtc &&
                    !string.IsNullOrWhiteSpace(session.RemoteIpAddress))
                .Select(session => session.RemoteIpAddress)
                .Distinct()
                .CountAsync();
        }

        public async Task RevokeByTokenHash(string tokenHash, DateTime revokedAtUtc)
        {
            var sessions = await _context.AuthSessions
                .Where(session => session.TokenHash == tokenHash && session.RevokedAtUtc == null)
                .ToListAsync();

            foreach (var session in sessions)
            {
                session.RevokedAtUtc = revokedAtUtc;
            }

            await _context.SaveChangesAsync();
        }

        public async Task RevokeAllForUser(int userId, DateTime revokedAtUtc)
        {
            var sessions = await _context.AuthSessions
                .Where(session => session.UserId == userId && session.RevokedAtUtc == null)
                .ToListAsync();

            foreach (var session in sessions)
            {
                session.RevokedAtUtc = revokedAtUtc;
            }

            await _context.SaveChangesAsync();
        }

        public async Task SaveChanges()
        {
            await _context.SaveChangesAsync();
        }
    }
}
