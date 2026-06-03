namespace TdpTrans.DTOs
{
    public class SignupRequest
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string SecurityCode { get; set; } = string.Empty;
        public string AuthenticationPhrase { get; set; } = string.Empty;
    }
}
