using TdpTrans.DTOs;
using TdpTrans.Models;
using TdpTrans.Repositories.Interfaces;

namespace TdpTrans.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUsersRepository _usersRepository;
        private readonly IActivityLogService _activityLogService;

        public AuthService(IUsersRepository usersRepository, IActivityLogService activityLogService)
        {
            _usersRepository = usersRepository;
            _activityLogService = activityLogService;
        }

        public async Task<AuthenticatedUserResponse> Login(LoginRequest request)
        {
            var email = NormalizeEmail(request.Email);
            var user = await _usersRepository.GetUserByEmail(email);

            if (user == null || !CredentialHasher.VerifyPassword(request.Password, user.PasswordHash))
            {
                await _activityLogService.Log(
                    user?.Id,
                    ActivityActionNames.LoginFailed,
                    $"Failed login attempt for {email}.",
                    false,
                    user == null ? "anonymous" : null);

                throw new UnauthorizedAccessException("Email sau parola sunt incorecte.");
            }

            if (!user.IsActive)
            {
                throw new UnauthorizedAccessException("Contul este inactiv.");
            }

            await _activityLogService.Log(user.Id, ActivityActionNames.LoginSucceeded, "User logged in successfully.");
            return MapUser(user);
        }

        public async Task<AuthenticatedUserResponse> Register(SignupRequest request)
        {
            var email = NormalizeEmail(request.Email);
            var fullName = request.FullName.Trim();

            if (string.IsNullOrWhiteSpace(fullName))
            {
                throw new ArgumentException("Numele complet este obligatoriu.");
            }

            if (string.IsNullOrWhiteSpace(email))
            {
                throw new ArgumentException("Email-ul este obligatoriu.");
            }

            if (request.Password.Trim().Length < 6)
            {
                throw new ArgumentException("Parola trebuie sa aiba cel putin 6 caractere.");
            }

            if (string.Equals(email, ReservedAccountEmails.PrimaryAdmin, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Adresa admin@tdptrans.ro este rezervata contului administrator.");
            }

            var existingUser = await _usersRepository.GetUserByEmail(email);
            if (existingUser != null)
            {
                throw new ArgumentException("Exista deja un cont cu acest email.");
            }

            var role = await _usersRepository.GetRoleByName(RoleNames.User)
                ?? throw new InvalidOperationException("Rolul implicit pentru utilizator nu exista.");

            var createdUser = await _usersRepository.AddUser(new AppUser
            {
                FullName = fullName,
                Email = email,
                PasswordHash = CredentialHasher.HashPassword(request.Password),
                CreatedAtUtc = DateTime.UtcNow,
                IsActive = true
            });

            await _usersRepository.AddUserRole(new UserRole
            {
                UserId = createdUser.Id,
                RoleId = role.Id
            });

            var registeredUser = await _usersRepository.GetUserById(createdUser.Id)
                ?? throw new InvalidOperationException("Utilizatorul nou creat nu a putut fi reincarcat.");

            await _activityLogService.Log(
                registeredUser.Id,
                ActivityActionNames.SignupCreated,
                "User self-registered through the signup screen.",
                true,
                RoleNames.User);

            return MapUser(registeredUser);
        }

        private static string NormalizeEmail(string email)
        {
            return email.Trim().ToLowerInvariant();
        }

        private static AuthenticatedUserResponse MapUser(AppUser user)
        {
            var roleName = user.UserRoles
                .Select(userRole => userRole.Role.Name)
                .FirstOrDefault() ?? RoleNames.User;

            var permissions = user.UserRoles
                .SelectMany(userRole => userRole.Role.RolePermissions)
                .Select(rolePermission => rolePermission.Permission.Name)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(name => name)
                .ToArray();

            return new AuthenticatedUserResponse
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                RoleName = roleName,
                Permissions = permissions
            };
        }
    }
}
