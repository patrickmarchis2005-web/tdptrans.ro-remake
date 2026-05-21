namespace TdpTrans.Models
{
    public class UserObservation
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public AppUser User { get; set; } = null!;
        public string Reason { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        public int RiskScore { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime FirstDetectedAtUtc { get; set; }
        public DateTime LastDetectedAtUtc { get; set; }
    }
}
