using System.Security.Cryptography;
using System.Text;

namespace TdpTrans.Models
{
    public static class CredentialHasher
    {
        public static string HashPassword(string password)
        {
            var bytes = Encoding.UTF8.GetBytes($"tdptrans::{password}");
            return Convert.ToHexString(SHA256.HashData(bytes));
        }

        public static bool VerifyPassword(string password, string passwordHash)
        {
            return HashPassword(password) == passwordHash;
        }
    }
}
