using TdpTrans.DTOs;
using TdpTrans.Models;
using TdpTrans.Repositories.Interfaces;

namespace TdpTrans.Services
{
    public class ChatService : IChatService
    {
        private readonly IChatMessageStore _chatMessageStore;
        private readonly IUsersRepository _usersRepository;
        private readonly IUserAccessService _userAccessService;
        private readonly IActivityLogService _activityLogService;

        public ChatService(
            IChatMessageStore chatMessageStore,
            IUsersRepository usersRepository,
            IUserAccessService userAccessService,
            IActivityLogService activityLogService)
        {
            _chatMessageStore = chatMessageStore;
            _usersRepository = usersRepository;
            _userAccessService = userAccessService;
            _activityLogService = activityLogService;
        }

        public async Task<IReadOnlyList<ChatContactResponse>> GetContacts(int userId)
        {
            var user = await _userAccessService.EnsurePermission(userId, PermissionNames.ChatUse, "Tried to access chat contacts without permission.");

            if (IsAdmin(user))
            {
                var users = await _usersRepository.GetActiveUsersByRole(RoleNames.User);
                return users
                    .Where(contact => contact.Id != user.Id)
                    .Select(MapContact)
                    .ToArray();
            }

            var adminUser = await _usersRepository.GetUserByEmail(ReservedAccountEmails.PrimaryAdmin)
                ?? throw new InvalidOperationException("Contul administrator nu este disponibil pentru chat.");

            if (!adminUser.IsActive)
            {
                throw new InvalidOperationException("Contul administrator nu este activ pentru chat.");
            }

            return new[] { MapContact(adminUser) };
        }

        public async Task<ChatMessageResponse> CreateMessage(int userId, int recipientUserId, string message)
        {
            var (user, recipient) = await EnsureConversation(
                userId,
                recipientUserId,
                "Tried to send a chat message without permission.");

            var cleanedMessage = message.Trim();
            if (string.IsNullOrWhiteSpace(cleanedMessage))
            {
                throw new ArgumentException("Mesajul nu poate fi gol.");
            }

            if (cleanedMessage.Length > 500)
            {
                throw new ArgumentException("Mesajul este prea lung.");
            }

            var senderRoleName = GetRoleName(user);
            var recipientRoleName = GetRoleName(recipient);
            var conversationKey = BuildConversationKey(user.Id, recipient.Id);
            var storedMessage = await _chatMessageStore.AppendMessage(new ChatMessageDocument
            {
                ConversationKey = conversationKey,
                SenderUserId = user.Id,
                SenderName = user.FullName,
                SenderRole = senderRoleName,
                RecipientUserId = recipient.Id,
                RecipientName = recipient.FullName,
                RecipientRole = recipientRoleName,
                Message = cleanedMessage,
                TimestampUtc = DateTime.UtcNow
            });

            await _activityLogService.Log(
                user.Id,
                ActivityActionNames.ChatMessageSent,
                $"Sent private chat message to {recipient.FullName}: {cleanedMessage}");

            return MapMessage(storedMessage);
        }

        public async Task<IReadOnlyList<ChatMessageResponse>> GetRecentMessages(int userId, int withUserId, int take = 40)
        {
            var (user, partner) = await EnsureConversation(
                userId,
                withUserId,
                "Tried to access chat history without permission.");

            var messages = await _chatMessageStore.GetRecentMessages(
                BuildConversationKey(user.Id, partner.Id),
                Math.Clamp(take, 10, 100));

            await _activityLogService.Log(
                userId,
                ActivityActionNames.ChatHistoryViewed,
                $"Viewed private chat history with {partner.FullName}.");

            return messages.Select(MapMessage).ToArray();
        }

        private async Task<(AppUser User, AppUser Partner)> EnsureConversation(
            int userId,
            int partnerUserId,
            string actionDescription)
        {
            var user = await _userAccessService.EnsurePermission(userId, PermissionNames.ChatUse, actionDescription);
            var partner = await _userAccessService.RequireUser(partnerUserId);

            if (user.Id == partner.Id)
            {
                throw new ArgumentException("Convorbirea privata trebuie deschisa cu alt utilizator.");
            }

            var userIsAdmin = IsAdmin(user);
            var partnerIsAdmin = IsAdmin(partner);

            if (userIsAdmin && !partnerIsAdmin)
            {
                return (user, partner);
            }

            if (!userIsAdmin && partnerIsAdmin && string.Equals(partner.Email, ReservedAccountEmails.PrimaryAdmin, StringComparison.OrdinalIgnoreCase))
            {
                return (user, partner);
            }

            throw new UnauthorizedAccessException("Chat-ul privat este disponibil doar intre administrator si cate un utilizator.");
        }

        private static ChatContactResponse MapContact(AppUser user)
        {
            return new ChatContactResponse
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                RoleName = GetRoleName(user)
            };
        }

        private static ChatMessageResponse MapMessage(ChatMessageDocument message)
        {
            return new ChatMessageResponse
            {
                Id = message.Id,
                ConversationKey = message.ConversationKey,
                SenderUserId = message.SenderUserId,
                SenderName = message.SenderName,
                SenderRole = message.SenderRole,
                RecipientUserId = message.RecipientUserId,
                RecipientName = message.RecipientName,
                RecipientRole = message.RecipientRole,
                Message = message.Message,
                TimestampUtc = message.TimestampUtc
            };
        }

        private static string BuildConversationKey(int firstUserId, int secondUserId)
        {
            var orderedUserIds = new[] { firstUserId, secondUserId }.OrderBy(userId => userId).ToArray();
            return $"{orderedUserIds[0]}:{orderedUserIds[1]}";
        }

        private static string GetRoleName(AppUser user)
        {
            return user.UserRoles.Select(userRole => userRole.Role.Name).FirstOrDefault() ?? RoleNames.User;
        }

        private static bool IsAdmin(AppUser user)
        {
            return string.Equals(GetRoleName(user), RoleNames.Admin, StringComparison.OrdinalIgnoreCase);
        }
    }
}
