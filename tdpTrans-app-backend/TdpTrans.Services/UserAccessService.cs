using TdpTrans.Models;
using TdpTrans.Repositories.Interfaces;

namespace TdpTrans.Services
{
    public class UserAccessService : IUserAccessService
    {
        private readonly IUsersRepository _usersRepository;
        private readonly IActivityLogService _activityLogService;

        public UserAccessService(IUsersRepository usersRepository, IActivityLogService activityLogService)
        {
            _usersRepository = usersRepository;
            _activityLogService = activityLogService;
        }

        public async Task<AppUser> EnsurePermission(int userId, string permissionName, string actionDescription)
        {
            var user = await RequireUser(userId);

            var hasPermission = user.UserRoles
                .SelectMany(userRole => userRole.Role.RolePermissions)
                .Select(rolePermission => rolePermission.Permission.Name)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Contains(permissionName, StringComparer.OrdinalIgnoreCase);

            if (!hasPermission)
            {
                await _activityLogService.Log(userId, ActivityActionNames.PermissionDenied, actionDescription, false);
                throw new UnauthorizedAccessException("Utilizatorul nu are permisiunea necesara.");
            }

            return user;
        }

        public async Task<AppUser> RequireUser(int userId)
        {
            var user = await _usersRepository.GetUserById(userId);
            if (user == null || !user.IsActive)
            {
                throw new UnauthorizedAccessException("Utilizatorul autentificat nu este valid.");
            }

            return user;
        }
    }
}
