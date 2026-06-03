using System.Security.Cryptography;
using System.Text;
using TdpTrans.DTOs;
using TdpTrans.Models;
using TdpTrans.Repositories.Interfaces;

namespace TdpTrans.Services
{
    public class AuthSessionService : IAuthSessionService
    {
        private const int SessionIdleTimeoutSeconds = 15 * 60;
        private static readonly TimeSpan SessionLifetime = TimeSpan.FromHours(12);
        private static readonly TimeSpan ActivityWriteDebounce = TimeSpan.FromMinutes(1);
        private readonly IAuthSessionRepository _authSessionRepository;
        private readonly ISuspiciousActivityService _suspiciousActivityService;

        public AuthSessionService(IAuthSessionRepository authSessionRepository, ISuspiciousActivityService suspiciousActivityService)
        {
            _authSessionRepository = authSessionRepository;
            _suspiciousActivityService = suspiciousActivityService;
        }

        public async Task<AuthenticatedUserResponse> StartSession(
            AppUser user,
            string? clientKey,
            string? userAgent,
            string? remoteIpAddress)
        {
            var now = DateTime.UtcNow;
            var accessToken = CreateAccessToken();

            await _authSessionRepository.Add(new AuthSession
            {
                UserId = user.Id,
                TokenHash = HashToken(accessToken),
                CreatedAtUtc = now,
                LastActivityAtUtc = now,
                ExpiresAtUtc = now.Add(SessionLifetime),
                ClientKey = clientKey?.Trim() ?? string.Empty,
                UserAgent = userAgent?.Trim() ?? string.Empty,
                RemoteIpAddress = remoteIpAddress?.Trim() ?? string.Empty
            });

            await _suspiciousActivityService.Evaluate(user.Id);

            return new AuthenticatedUserResponse
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                RoleName = user.UserRoles.Select(userRole => userRole.Role.Name).FirstOrDefault() ?? RoleNames.User,
                Permissions = user.UserRoles
                    .SelectMany(userRole => userRole.Role.RolePermissions)
                    .Select(rolePermission => rolePermission.Permission.Name)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(permission => permission)
                    .ToArray(),
                AccessToken = accessToken,
                SessionExpiresAtUtc = now.Add(SessionLifetime),
                SessionIdleTimeoutSeconds = SessionIdleTimeoutSeconds
            };
        }

        public async Task<SessionActor?> Authenticate(string token, bool touchActivity = true)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return null;
            }

            var now = DateTime.UtcNow;
            var session = await _authSessionRepository.GetActiveByTokenHash(HashToken(token));
            if (session == null)
            {
                return null;
            }

            var idleDeadlineUtc = session.LastActivityAtUtc.AddSeconds(SessionIdleTimeoutSeconds);
            var isExpired = session.ExpiresAtUtc <= now || idleDeadlineUtc <= now || !session.User.IsActive;
            if (isExpired)
            {
                session.RevokedAtUtc = now;
                await _authSessionRepository.SaveChanges();
                return null;
            }

            if (touchActivity && now - session.LastActivityAtUtc >= ActivityWriteDebounce)
            {
                session.LastActivityAtUtc = now;
                await _authSessionRepository.SaveChanges();
            }

            return new SessionActor
            {
                UserId = session.UserId,
                Email = session.User.Email,
                FullName = session.User.FullName,
                RoleName = session.User.UserRoles.Select(userRole => userRole.Role.Name).FirstOrDefault() ?? RoleNames.User,
                Permissions = session.User.UserRoles
                    .SelectMany(userRole => userRole.Role.RolePermissions)
                    .Select(rolePermission => rolePermission.Permission.Name)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(permission => permission)
                    .ToArray(),
                SessionExpiresAtUtc = session.ExpiresAtUtc,
                SessionIdleTimeoutSeconds = SessionIdleTimeoutSeconds
            };
        }

        public async Task EndSession(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return;
            }

            await _authSessionRepository.RevokeByTokenHash(HashToken(token), DateTime.UtcNow);
        }

        public async Task EndAllUserSessions(int userId)
        {
            await _authSessionRepository.RevokeAllForUser(userId, DateTime.UtcNow);
        }

        private static string CreateAccessToken()
        {
            return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }

        private static string HashToken(string token)
        {
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"tdp-session::{token}")));
        }
    }
}
