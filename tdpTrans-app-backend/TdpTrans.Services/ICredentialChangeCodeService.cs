namespace TdpTrans.Services
{
    public interface ICredentialChangeCodeService
    {
        CredentialChangeCodeIssuance IssueCode(string email, string clientKey);
        bool VerifyCode(string email, string clientKey, string credentialChangeCode);
        void ClearCode(string email, string clientKey);
    }
}
