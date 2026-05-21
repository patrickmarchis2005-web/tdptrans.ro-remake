using TdpTrans.DTOs;

namespace TdpTrans.Services
{
    public interface IChatService
    {
        Task<IReadOnlyList<ChatContactResponse>> GetContacts(int userId);
        Task<IReadOnlyList<ChatMessageResponse>> GetRecentMessages(int userId, int withUserId, int take = 40);
        Task<ChatMessageResponse> CreateMessage(int userId, int recipientUserId, string message);
    }
}
