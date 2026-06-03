namespace TdpTrans.DTOs
{
    public class CredentialChangeCodeResponse
    {
        public string CredentialChangeCode { get; set; } = string.Empty;
        public DateTime ExpiresAtUtc { get; set; }
    }
}
