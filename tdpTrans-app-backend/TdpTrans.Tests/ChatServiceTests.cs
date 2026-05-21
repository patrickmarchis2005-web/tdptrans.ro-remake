using Moq;
using TdpTrans.Models;
using TdpTrans.Repositories.Interfaces;
using TdpTrans.Services;

namespace TdpTrans.Tests
{
    public class ChatServiceTests
    {
        private readonly Mock<IChatMessageStore> _chatMessageStore;
        private readonly Mock<IUsersRepository> _usersRepository;
        private readonly Mock<IUserAccessService> _userAccessService;
        private readonly Mock<IActivityLogService> _activityLogService;
        private readonly ChatService _service;

        public ChatServiceTests()
        {
            _chatMessageStore = new Mock<IChatMessageStore>();
            _usersRepository = new Mock<IUsersRepository>();
            _userAccessService = new Mock<IUserAccessService>();
            _activityLogService = new Mock<IActivityLogService>();
            _service = new ChatService(
                _chatMessageStore.Object,
                _usersRepository.Object,
                _userAccessService.Object,
                _activityLogService.Object);
        }

        [Fact]
        public async Task GetContacts_ForAdmin_ReturnsOnlyStandardUsers()
        {
            var admin = BuildUser(1, "Administrator TDP", "admin@tdptrans.ro", RoleNames.Admin);
            var userOne = BuildUser(2, "Utilizator 1", "user1@tdptrans.ro", RoleNames.User);
            var userTwo = BuildUser(3, "Utilizator 2", "user2@tdptrans.ro", RoleNames.User);

            _userAccessService
                .Setup(service => service.EnsurePermission(1, PermissionNames.ChatUse, "Tried to access chat contacts without permission."))
                .ReturnsAsync(admin);
            _usersRepository
                .Setup(repository => repository.GetActiveUsersByRole(RoleNames.User))
                .ReturnsAsync(new List<AppUser> { userOne, userTwo });

            var result = await _service.GetContacts(admin.Id);

            Assert.Equal(2, result.Count);
            Assert.All(result, contact => Assert.Equal(RoleNames.User, contact.RoleName));
            Assert.Contains(result, contact => contact.Id == userOne.Id);
            Assert.Contains(result, contact => contact.Id == userTwo.Id);
        }

        [Fact]
        public async Task CreateMessage_FromUserToAdmin_PersistsPrivateConversation()
        {
            var user = BuildUser(2, "Sofer TDP", "sofer@tdptrans.ro", RoleNames.User);
            var admin = BuildUser(1, "Administrator TDP", "admin@tdptrans.ro", RoleNames.Admin);

            _userAccessService
                .Setup(service => service.EnsurePermission(2, PermissionNames.ChatUse, "Tried to send a chat message without permission."))
                .ReturnsAsync(user);
            _userAccessService
                .Setup(service => service.RequireUser(1))
                .ReturnsAsync(admin);
            _chatMessageStore
                .Setup(store => store.AppendMessage(It.Is<ChatMessageDocument>(message =>
                    message.ConversationKey == "1:2" &&
                    message.SenderUserId == user.Id &&
                    message.RecipientUserId == admin.Id &&
                    message.Message == "Salut!")))
                .ReturnsAsync((ChatMessageDocument message) => message);

            var response = await _service.CreateMessage(user.Id, admin.Id, " Salut! ");

            Assert.Equal("1:2", response.ConversationKey);
            Assert.Equal(user.Id, response.SenderUserId);
            Assert.Equal(admin.Id, response.RecipientUserId);
            Assert.Equal("Salut!", response.Message);

            _activityLogService.Verify(
                service => service.Log(
                    user.Id,
                    ActivityActionNames.ChatMessageSent,
                    It.Is<string>(value => value.Contains("Administrator TDP") && value.Contains("Salut!")),
                    true,
                    null),
                Times.Once);
        }

        [Fact]
        public async Task CreateMessage_BetweenTwoStandardUsers_ThrowsUnauthorizedAccessException()
        {
            var userOne = BuildUser(2, "Utilizator 1", "user1@tdptrans.ro", RoleNames.User);
            var userTwo = BuildUser(3, "Utilizator 2", "user2@tdptrans.ro", RoleNames.User);

            _userAccessService
                .Setup(service => service.EnsurePermission(2, PermissionNames.ChatUse, "Tried to send a chat message without permission."))
                .ReturnsAsync(userOne);
            _userAccessService
                .Setup(service => service.RequireUser(3))
                .ReturnsAsync(userTwo);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _service.CreateMessage(userOne.Id, userTwo.Id, "Salut"));

            _chatMessageStore.Verify(store => store.AppendMessage(It.IsAny<ChatMessageDocument>()), Times.Never);
        }

        private static AppUser BuildUser(int id, string fullName, string email, string roleName)
        {
            var role = new AppRole
            {
                Id = roleName == RoleNames.Admin ? 1 : 2,
                Name = roleName,
                Description = roleName
            };
            role.RolePermissions.Add(new RolePermission
            {
                RoleId = role.Id,
                Role = role,
                PermissionId = 2,
                Permission = new AppPermission
                {
                    Id = 2,
                    Name = PermissionNames.ChatUse,
                    Description = PermissionNames.ChatUse
                }
            });

            var user = new AppUser
            {
                Id = id,
                FullName = fullName,
                Email = email,
                PasswordHash = "hash",
                CreatedAtUtc = DateTime.UtcNow,
                IsActive = true
            };

            user.UserRoles.Add(new UserRole
            {
                UserId = user.Id,
                User = user,
                RoleId = role.Id,
                Role = role
            });

            return user;
        }
    }
}
