using Microsoft.EntityFrameworkCore;
using TdpTrans.Models;
using TdpTrans.Repositories.Interfaces;

namespace TdpTrans.Repositories.Implementations
{
    public class ActivityLogRepository : IActivityLogRepository
    {
        private readonly ApplicationDbContext _context;

        public ActivityLogRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ActivityLog> Add(ActivityLog activityLog)
        {
            _context.ActivityLogs.Add(activityLog);
            await _context.SaveChangesAsync();
            return activityLog;
        }

        public async Task<int> CountRecent(int userId, string actionType, DateTime sinceUtc, bool? isSuccess = null)
        {
            var query = _context.ActivityLogs
                .Where(activityLog =>
                    activityLog.UserId == userId &&
                    activityLog.ActionType == actionType &&
                    activityLog.TimestampUtc >= sinceUtc);

            if (isSuccess.HasValue)
            {
                query = query.Where(activityLog => activityLog.IsSuccess == isSuccess.Value);
            }

            return await query.CountAsync();
        }

        public async Task<IReadOnlyList<ActivityLog>> GetRecent(int take)
        {
            return await _context.ActivityLogs
                .Include(activityLog => activityLog.User)
                    .ThenInclude(user => user!.UserRoles)
                        .ThenInclude(userRole => userRole.Role)
                .OrderByDescending(activityLog => activityLog.TimestampUtc)
                .Take(take)
                .ToListAsync();
        }
    }
}
