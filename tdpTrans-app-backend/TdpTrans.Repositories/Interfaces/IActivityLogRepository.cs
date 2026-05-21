using TdpTrans.Models;

namespace TdpTrans.Repositories.Interfaces
{
    public interface IActivityLogRepository
    {
        Task<ActivityLog> Add(ActivityLog activityLog);
        Task<IReadOnlyList<ActivityLog>> GetRecent(int take);
        Task<int> CountRecent(int userId, string actionType, DateTime sinceUtc, bool? isSuccess = null);
    }
}
