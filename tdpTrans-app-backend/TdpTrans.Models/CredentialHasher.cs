using System.Security.Cryptography;
using System.Text;

namespace TdpTrans.Models
{
    public static class CredentialHasher
    {
        private const int DefaultIterations = 120_000;
        private const int SaltLength = 16;
        private const int KeyLength = 32;
        private const string HashPrefix = "pbkdf2";

        public static string HashPassword(string password)
        {
            return HashSecret(password, "password");
        }

        public static bool VerifyPassword(string password, string passwordHash)
        {
            if (passwordHash.StartsWith($"{HashPrefix}$", StringComparison.Ordinal))
            {
                return VerifySecret(password, passwordHash, "password");
            }

            return VerifyLegacyPassword(password, passwordHash);
        }

        public static bool NeedsPasswordRehash(string passwordHash)
        {
            return !passwordHash.StartsWith($"{HashPrefix}$", StringComparison.Ordinal);
        }

        public static string HashSecurityCode(string securityCode)
        {
            return HashSecret(securityCode, "security-code");
        }

        public static bool VerifySecurityCode(string securityCode, string securityCodeHash)
        {
            return VerifySecret(securityCode, securityCodeHash, "security-code");
        }

        public static string HashAuthenticationPhrase(string authenticationPhrase)
        {
            return HashSecret(authenticationPhrase, "authentication-phrase");
        }

        public static bool VerifyAuthenticationPhrase(string authenticationPhrase, string authenticationPhraseHash)
        {
            return VerifySecret(authenticationPhrase, authenticationPhraseHash, "authentication-phrase");
        }

        private static string HashSecret(string value, string purpose)
        {
            var salt = RandomNumberGenerator.GetBytes(SaltLength);
            var hash = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes($"{purpose}::{value}"),
                salt,
                DefaultIterations,
                HashAlgorithmName.SHA256,
                KeyLength);

            return $"{HashPrefix}${DefaultIterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
        }

        private static bool VerifySecret(string value, string storedHash, string purpose)
        {
            var parts = storedHash.Split('$', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 4 || !string.Equals(parts[0], HashPrefix, StringComparison.Ordinal))
            {
                return false;
            }

            if (!int.TryParse(parts[1], out var iterations))
            {
                return false;
            }

            try
            {
                var salt = Convert.FromBase64String(parts[2]);
                var expectedHash = Convert.FromBase64String(parts[3]);
                var actualHash = Rfc2898DeriveBytes.Pbkdf2(
                    Encoding.UTF8.GetBytes($"{purpose}::{value}"),
                    salt,
                    iterations,
                    HashAlgorithmName.SHA256,
                    expectedHash.Length);

                return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
            }
            catch (FormatException)
            {
                return false;
            }
        }

        private static bool VerifyLegacyPassword(string password, string passwordHash)
        {
            var bytes = Encoding.UTF8.GetBytes($"tdptrans::{password}");
            var legacyHash = Convert.ToHexString(SHA256.HashData(bytes));
            return string.Equals(legacyHash, passwordHash, StringComparison.OrdinalIgnoreCase);
        }
    }
}
