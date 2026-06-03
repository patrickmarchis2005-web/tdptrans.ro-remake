namespace TdpTrans.Services
{
    public class CredentialChangeEmailOptions
    {
        public string DeliveryMode { get; set; } = "smtp";
        public string FromAddress { get; set; } = "no-reply@tdptrans.local";
        public string FromDisplayName { get; set; } = "TDP Transport";
        public string Subject { get; set; } = "Cod de confirmare TDP Transport";
        public string SmtpHost { get; set; } = string.Empty;
        public int SmtpPort { get; set; } = 587;
        public bool EnableSsl { get; set; } = true;
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string PickupDirectory { get; set; } = string.Empty;
    }
}
