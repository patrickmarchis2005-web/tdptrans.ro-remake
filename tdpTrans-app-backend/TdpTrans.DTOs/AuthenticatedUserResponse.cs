namespace TdpTrans.DTOs
{
    public class AuthenticatedUserResponse
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public IReadOnlyList<string> Permissions { get; set; } = Array.Empty<string>();
    }
}
