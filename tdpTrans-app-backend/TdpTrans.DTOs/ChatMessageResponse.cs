namespace TdpTrans.DTOs
{
    public class ChatMessageResponse
    {
        public string Id { get; set; } = string.Empty;
        public string ConversationKey { get; set; } = string.Empty;
        public int SenderUserId { get; set; }
        public string SenderName { get; set; } = string.Empty;
        public string SenderRole { get; set; } = string.Empty;
        public int RecipientUserId { get; set; }
        public string RecipientName { get; set; } = string.Empty;
        public string RecipientRole { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime TimestampUtc { get; set; }
    }
}
