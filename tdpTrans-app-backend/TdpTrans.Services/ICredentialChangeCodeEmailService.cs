namespace TdpTrans.Services
{
    public interface ICredentialChangeCodeEmailService
    {
        Task SendCredentialChangeCode(string email, string credentialChangeCode, DateTime expiresAtUtc);
    }
}
