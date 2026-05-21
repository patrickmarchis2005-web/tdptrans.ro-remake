using TdpTrans.DTOs;

namespace TdpTrans.Services
{
    public interface IActivityLogService
    {
        Task Log(int? userId, string actionType, string actionInformation, bool isSuccess = true, string? fallbackGroupId = null);
        Task<IReadOnlyList<ActivityLogResponse>> GetRecentLogs(int take);
    }
}
