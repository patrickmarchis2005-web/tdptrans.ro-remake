using TdpTrans.DTOs;
using TdpTrans.Models;
using TdpTrans.Repositories.Interfaces;

namespace TdpTrans.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUsersRepository _usersRepository;
        private readonly IActivityLogService _activityLogService;
        private readonly IAuthSessionService _authSessionService;
        private readonly ICredentialChangeCodeService _credentialChangeCodeService;

        public AuthService(
            IUsersRepository usersRepository,
            IActivityLogService activityLogService,
            IAuthSessionService authSessionService,
            ICredentialChangeCodeService credentialChangeCodeService)
        {
            _usersRepository = usersRepository;
            _activityLogService = activityLogService;
            _authSessionService = authSessionService;
            _credentialChangeCodeService = credentialChangeCodeService;
        }

        public async Task<AuthenticatedUserResponse> Login(LoginRequest request, string? clientKey, string? userAgent, string? remoteIpAddress)
        {
            var email = NormalizeEmail(request.Email);
            var securityCode = NormalizeSecurityCode(request.SecurityCode);
            var authenticationPhrase = NormalizeAuthenticationPhrase(request.AuthenticationPhrase);
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

            if (!IsValidSecurityCode(securityCode) || !CredentialHasher.VerifySecurityCode(securityCode, user.SecurityCodeHash))
            {
                await _activityLogService.Log(
                    user.Id,
                    ActivityActionNames.LoginFailed,
                    $"Failed security code challenge for {email}.",
                    false);

                throw new UnauthorizedAccessException("Codul de securitate este incorect.");
            }

            if (!IsValidAuthenticationPhrase(authenticationPhrase) || !CredentialHasher.VerifyAuthenticationPhrase(authenticationPhrase, user.AuthenticationPhraseHash))
            {
                await _activityLogService.Log(
                    user.Id,
                    ActivityActionNames.LoginFailed,
                    $"Failed authentication phrase challenge for {email}.",
                    false);

                throw new UnauthorizedAccessException("Fraza de autentificare este incorecta.");
            }

            if (!user.IsActive)
            {
                throw new UnauthorizedAccessException("Contul este inactiv.");
            }

            if (CredentialHasher.NeedsPasswordRehash(user.PasswordHash))
            {
                user.PasswordHash = CredentialHasher.HashPassword(request.Password);
                await _usersRepository.SaveChanges();
            }

            await _activityLogService.Log(user.Id, ActivityActionNames.LoginSucceeded, "User logged in successfully.");
            return await _authSessionService.StartSession(user, clientKey, userAgent, remoteIpAddress);
        }

        public async Task<AuthenticatedUserResponse> Register(SignupRequest request, string? clientKey, string? userAgent, string? remoteIpAddress)
        {
            var email = NormalizeEmail(request.Email);
            var fullName = request.FullName.Trim();
            var securityCode = NormalizeSecurityCode(request.SecurityCode);
            var authenticationPhrase = NormalizeAuthenticationPhrase(request.AuthenticationPhrase);

            if (string.IsNullOrWhiteSpace(fullName))
            {
                throw new ArgumentException("Numele complet este obligatoriu.");
            }

            if (string.IsNullOrWhiteSpace(email))
            {
                throw new ArgumentException("Email-ul este obligatoriu.");
            }

            if (request.Password.Trim().Length < 8)
            {
                throw new ArgumentException("Parola trebuie sa aiba cel putin 8 caractere.");
            }

            if (!IsValidSecurityCode(securityCode))
            {
                throw new ArgumentException("Codul de securitate trebuie sa contina exact 6 cifre.");
            }

            if (!IsValidAuthenticationPhrase(authenticationPhrase))
            {
                throw new ArgumentException("Fraza de autentificare trebuie sa aiba intre 6 si 64 de caractere.");
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
                SecurityCodeHash = CredentialHasher.HashSecurityCode(securityCode),
                AuthenticationPhraseHash = CredentialHasher.HashAuthenticationPhrase(authenticationPhrase),
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

            return await _authSessionService.StartSession(registeredUser, clientKey, userAgent, remoteIpAddress);
        }

        public async Task<CredentialChangeCodeResponse> RequestCredentialChangeCode(CredentialChangeCodeRequest request, string? clientKey, string? userAgent, string? remoteIpAddress)
        {
            var email = NormalizeEmail(request.Email);
            if (string.IsNullOrWhiteSpace(email))
            {
                throw new ArgumentException("Email-ul este obligatoriu.");
            }

            var user = await _usersRepository.GetUserByEmail(email);
            if (user == null)
            {
                throw new UnauthorizedAccessException("Nu exista niciun cont asociat cu acest email.");
            }

            var issuance = _credentialChangeCodeService.IssueCode(email, NormalizeClientKey(clientKey));

            return new CredentialChangeCodeResponse
            {
                CredentialChangeCode = issuance.CredentialChangeCode,
                ExpiresAtUtc = issuance.ExpiresAtUtc
            };
        }

        public async Task<AuthenticatedUserResponse> RecoverPassword(RecoverPasswordRequest request, string? clientKey, string? userAgent, string? remoteIpAddress)
        {
            var email = NormalizeEmail(request.Email);
            var clientKeyValue = NormalizeClientKey(clientKey);
            var credentialChangeCode = NormalizeCredentialChangeCode(request.CredentialChangeCode);
            var newSecurityCode = NormalizeSecurityCode(request.NewSecurityCode);
            var newAuthenticationPhrase = NormalizeAuthenticationPhrase(request.NewAuthenticationPhrase);
            var user = await _usersRepository.GetUserByEmail(email);
            if (user == null)
            {
                throw new UnauthorizedAccessException("Nu exista niciun cont asociat cu acest email.");
            }

            if (request.NewPassword.Trim().Length < 8)
            {
                throw new ArgumentException("Parola noua trebuie sa aiba cel putin 8 caractere.");
            }

            if (!IsValidSecurityCode(newSecurityCode))
            {
                throw new ArgumentException("Noul cod de securitate trebuie sa contina exact 6 cifre.");
            }

            if (!IsValidAuthenticationPhrase(newAuthenticationPhrase))
            {
                throw new ArgumentException("Noua fraza de autentificare trebuie sa aiba intre 6 si 64 de caractere.");
            }

            if (!IsValidCredentialChangeCode(credentialChangeCode) || !_credentialChangeCodeService.VerifyCode(email, clientKeyValue, credentialChangeCode))
            {
                await _activityLogService.Log(
                    user.Id,
                    ActivityActionNames.PasswordRecoveryFailed,
                    $"Failed credential change confirmation challenge for {email}.",
                    false);

                throw new UnauthorizedAccessException("Codul de confirmare pentru schimbarea credentialelor este invalid sau a expirat.");
            }

            user.PasswordHash = CredentialHasher.HashPassword(request.NewPassword);
            user.SecurityCodeHash = CredentialHasher.HashSecurityCode(newSecurityCode);
            user.AuthenticationPhraseHash = CredentialHasher.HashAuthenticationPhrase(newAuthenticationPhrase);

            await _usersRepository.SaveChanges();
            _credentialChangeCodeService.ClearCode(email, clientKeyValue);
            await _authSessionService.EndAllUserSessions(user.Id);

            await _activityLogService.Log(
                user.Id,
                ActivityActionNames.PasswordRecoverySucceeded,
                "Updated account credentials using the on-screen confirmation code.");

            return await _authSessionService.StartSession(user, clientKey, userAgent, remoteIpAddress);
        }

        private static string NormalizeEmail(string email)
        {
            return email.Trim().ToLowerInvariant();
        }

        private static string NormalizeClientKey(string? clientKey)
        {
            return clientKey?.Trim() ?? string.Empty;
        }

        private static bool IsValidSecurityCode(string securityCode)
        {
            return !string.IsNullOrWhiteSpace(securityCode)
                && securityCode.Length == 6
                && securityCode.All(char.IsDigit);
        }

        private static bool IsValidCredentialChangeCode(string credentialChangeCode)
        {
            return !string.IsNullOrWhiteSpace(credentialChangeCode)
                && credentialChangeCode.Length == 6
                && credentialChangeCode.All(char.IsDigit);
        }

        private static bool IsValidAuthenticationPhrase(string authenticationPhrase)
        {
            return !string.IsNullOrWhiteSpace(authenticationPhrase)
                && authenticationPhrase.Length >= 6
                && authenticationPhrase.Length <= 64;
        }

        private static string NormalizeSecurityCode(string securityCode)
        {
            return securityCode.Trim();
        }

        private static string NormalizeCredentialChangeCode(string credentialChangeCode)
        {
            return credentialChangeCode.Trim();
        }

        private static string NormalizeAuthenticationPhrase(string authenticationPhrase)
        {
            return authenticationPhrase.Trim().ToUpperInvariant();
        }

    }
}
