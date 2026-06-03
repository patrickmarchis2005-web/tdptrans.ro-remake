namespace TdpTrans.Models
{
    public class AuthSession
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public AppUser User { get; set; } = null!;
        public string TokenHash { get; set; } = string.Empty;
        public DateTime CreatedAtUtc { get; set; }
        public DateTime LastActivityAtUtc { get; set; }
        public DateTime ExpiresAtUtc { get; set; }
        public DateTime? RevokedAtUtc { get; set; }
        public string ClientKey { get; set; } = string.Empty;
        public string UserAgent { get; set; } = string.Empty;
        public string RemoteIpAddress { get; set; } = string.Empty;
    }
}
