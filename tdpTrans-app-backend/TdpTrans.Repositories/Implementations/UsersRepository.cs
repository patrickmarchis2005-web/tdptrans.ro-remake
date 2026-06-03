using Microsoft.EntityFrameworkCore;
using TdpTrans.Models;
using TdpTrans.Repositories.Interfaces;

namespace TdpTrans.Repositories.Implementations
{
    public class UsersRepository : IUsersRepository
    {
        private readonly ApplicationDbContext _context;

        public UsersRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<AppUser> AddUser(AppUser user)
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }

        public async Task AddUserRole(UserRole userRole)
        {
            _context.UserRoles.Add(userRole);
            await _context.SaveChangesAsync();
        }

        public async Task<AppRole?> GetRoleByName(string roleName)
        {
            return await _context.Roles.FirstOrDefaultAsync(role => role.Name == roleName);
        }

        public async Task<AppUser?> GetUserByEmail(string email)
        {
            return await _context.Users
                .Include(user => user.UserRoles)
                    .ThenInclude(userRole => userRole.Role)
                        .ThenInclude(role => role.RolePermissions)
                            .ThenInclude(rolePermission => rolePermission.Permission)
                .FirstOrDefaultAsync(user => user.Email == email);
        }

        public async Task<AppUser?> GetUserById(int userId)
        {
            return await _context.Users
                .Include(user => user.UserRoles)
                    .ThenInclude(userRole => userRole.Role)
                        .ThenInclude(role => role.RolePermissions)
                            .ThenInclude(rolePermission => rolePermission.Permission)
                .FirstOrDefaultAsync(user => user.Id == userId);
        }

        public async Task<IReadOnlyList<AppUser>> GetActiveUsersByRole(string roleName)
        {
            return await _context.Users
                .Include(user => user.UserRoles)
                    .ThenInclude(userRole => userRole.Role)
                .Where(user =>
                    user.IsActive &&
                    user.UserRoles.Any(userRole => userRole.Role.Name == roleName))
                .OrderBy(user => user.FullName)
                .ThenBy(user => user.Email)
                .ToListAsync();
        }

        public async Task SaveChanges()
        {
            await _context.SaveChangesAsync();
        }
    }
}
