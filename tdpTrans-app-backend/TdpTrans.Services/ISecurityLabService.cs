using TdpTrans.DTOs;

namespace TdpTrans.Services
{
    public interface ISecurityLabService
    {
        Task<SecurityStatisticsResponse> GetSecurityStatistics(string mode, int lookbackHours);
        Task<SecuritySeedResponse> GenerateSecuritySeed(SecuritySeedRequest request);
    }
}
