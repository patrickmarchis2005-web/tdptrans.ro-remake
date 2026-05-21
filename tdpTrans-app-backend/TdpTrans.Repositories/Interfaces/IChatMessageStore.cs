using TdpTrans.Models;

namespace TdpTrans.Repositories.Interfaces
{
    public interface IChatMessageStore
    {
        Task<IReadOnlyList<ChatMessageDocument>> GetRecentMessages(string conversationKey, int take);
        Task<ChatMessageDocument> AppendMessage(ChatMessageDocument message);
    }
}
