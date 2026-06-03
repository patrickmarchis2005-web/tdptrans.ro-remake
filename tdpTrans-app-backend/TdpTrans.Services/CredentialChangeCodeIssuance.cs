namespace TdpTrans.Services
{
    public sealed record CredentialChangeCodeIssuance(string CredentialChangeCode, DateTime ExpiresAtUtc);
}
