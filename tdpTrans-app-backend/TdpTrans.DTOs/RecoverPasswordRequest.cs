namespace TdpTrans.DTOs
{
    public class RecoverPasswordRequest
    {
        public string Email { get; set; } = string.Empty;
        public string CredentialChangeCode { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
        public string NewSecurityCode { get; set; } = string.Empty;
        public string NewAuthenticationPhrase { get; set; } = string.Empty;
    }
}
