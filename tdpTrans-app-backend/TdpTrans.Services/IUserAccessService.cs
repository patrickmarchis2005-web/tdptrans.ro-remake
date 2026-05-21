using TdpTrans.Models;

namespace TdpTrans.Services
{
    public interface IUserAccessService
    {
        Task<AppUser> RequireUser(int userId);
        Task<AppUser> EnsurePermission(int userId, string permissionName, string actionDescription);
    }
}
