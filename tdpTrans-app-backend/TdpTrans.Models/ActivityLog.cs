namespace TdpTrans.Models
{
    public class ActivityLog
    {
        public int Id { get; set; }
        public int? UserId { get; set; }
        public AppUser? User { get; set; }
        public string GroupId { get; set; } = string.Empty;
        public string ActionType { get; set; } = string.Empty;
        public string ActionInformation { get; set; } = string.Empty;
        public bool IsSuccess { get; set; }
        public DateTime TimestampUtc { get; set; }
    }
}
