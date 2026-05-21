namespace TdpTrans.DTOs
{
    public class ObservationResponse
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string GroupId { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        public int RiskScore { get; set; }
        public DateTime FirstDetectedAtUtc { get; set; }
        public DateTime LastDetectedAtUtc { get; set; }
    }
}
