using TdpTrans.Models;

namespace TdpTrans.Repositories.Interfaces
{
    public interface IUsersRepository
    {
        Task<AppUser?> GetUserByEmail(string email);
        Task<AppUser?> GetUserById(int userId);
        Task<IReadOnlyList<AppUser>> GetActiveUsersByRole(string roleName);
        Task<AppRole?> GetRoleByName(string roleName);
        Task<AppUser> AddUser(AppUser user);
        Task AddUserRole(UserRole userRole);
        Task SaveChanges();
    }
}
