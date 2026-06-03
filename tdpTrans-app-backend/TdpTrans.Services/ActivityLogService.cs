using TdpTrans.DTOs;
using TdpTrans.Models;
using TdpTrans.Repositories.Interfaces;

namespace TdpTrans.Services
{
    public class ActivityLogService : IActivityLogService
    {
        private readonly IActivityLogRepository _activityLogRepository;
        private readonly IUsersRepository _usersRepository;
        private readonly ISuspiciousActivityService _suspiciousActivityService;

        public ActivityLogService(
            IActivityLogRepository activityLogRepository,
            IUsersRepository usersRepository,
            ISuspiciousActivityService suspiciousActivityService)
        {
            _activityLogRepository = activityLogRepository;
            _usersRepository = usersRepository;
            _suspiciousActivityService = suspiciousActivityService;
        }

        public async Task<IReadOnlyList<ActivityLogResponse>> GetRecentLogs(int take)
        {
            var logs = await _activityLogRepository.GetRecent(take);
            return logs
                .Select(log => new ActivityLogResponse
                {
                    Id = log.Id,
                    UserId = log.UserId,
                    UserName = log.User?.FullName ?? "Anonim",
                    GroupId = log.GroupId,
                    ActionType = log.ActionType,
                    ActionInformation = log.ActionInformation,
                    IsSuccess = log.IsSuccess,
                    TimestampUtc = log.TimestampUtc
                })
                .ToArray();
        }

        public async Task Log(int? userId, string actionType, string actionInformation, bool isSuccess = true, string? fallbackGroupId = null)
        {
            AppUser? user = null;
            if (userId.HasValue)
            {
                user = await _usersRepository.GetUserById(userId.Value);
            }

            var groupId = user?.UserRoles.Select(userRole => userRole.Role.Name).FirstOrDefault()
                ?? fallbackGroupId
                ?? "anonymous";

            await _activityLogRepository.Add(new ActivityLog
            {
                UserId = user?.Id,
                GroupId = groupId,
                ActionType = actionType,
                ActionInformation = actionInformation,
                IsSuccess = isSuccess,
                TimestampUtc = DateTime.UtcNow
            });

            if (user?.Id != null && ShouldEvaluateSuspiciousActivity(actionType))
            {
                await _suspiciousActivityService.Evaluate(user.Id);
            }
        }

        private static bool ShouldEvaluateSuspiciousActivity(string actionType)
        {
            return actionType == ActivityActionNames.LoginFailed
                || actionType == ActivityActionNames.PermissionDenied
                || actionType == ActivityActionNames.ChatMessageSent;
        }
    }
}
