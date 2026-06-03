using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
namespace TdpTrans.Services
{
    public class CredentialChangeCodeService : ICredentialChangeCodeService
    {
        private static readonly TimeSpan CodeLifetime = TimeSpan.FromMinutes(5);
        private readonly ConcurrentDictionary<string, CredentialChangeCodeState> _issuedCodes = new();

        public CredentialChangeCodeIssuance IssueCode(string email, string clientKey)
        {
            var now = DateTime.UtcNow;
            RemoveExpiredCodes(now);

            var credentialChangeCode = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
            var expiresAtUtc = now.Add(CodeLifetime);

            _issuedCodes[BuildKey(email, clientKey)] = new CredentialChangeCodeState(
                HashCode(credentialChangeCode),
                expiresAtUtc);

            return new CredentialChangeCodeIssuance(credentialChangeCode, expiresAtUtc);
        }

        public bool VerifyCode(string email, string clientKey, string credentialChangeCode)
        {
            var key = BuildKey(email, clientKey);
            if (!_issuedCodes.TryGetValue(key, out var issuedCode))
            {
                return false;
            }

            if (issuedCode.ExpiresAtUtc <= DateTime.UtcNow)
            {
                _issuedCodes.TryRemove(key, out _);
                return false;
            }

            return CryptographicOperations.FixedTimeEquals(
                HashCode(credentialChangeCode),
                issuedCode.CodeHash);
        }

        public void ClearCode(string email, string clientKey)
        {
            _issuedCodes.TryRemove(BuildKey(email, clientKey), out _);
        }

        private void RemoveExpiredCodes(DateTime now)
        {
            foreach (var issuedCode in _issuedCodes)
            {
                if (issuedCode.Value.ExpiresAtUtc <= now)
                {
                    _issuedCodes.TryRemove(issuedCode.Key, out _);
                }
            }
        }

        private static string BuildKey(string email, string clientKey)
        {
            return $"{email}\n{clientKey}";
        }

        private static byte[] HashCode(string credentialChangeCode)
        {
            return SHA256.HashData(Encoding.UTF8.GetBytes($"tdp-credential-change::{credentialChangeCode}"));
        }

        private sealed record CredentialChangeCodeState(byte[] CodeHash, DateTime ExpiresAtUtc);
    }
}
