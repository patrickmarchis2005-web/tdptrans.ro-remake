using Microsoft.EntityFrameworkCore;
using TdpTrans.Models;
using TdpTrans.Repositories.Interfaces;

namespace TdpTrans.Repositories.Implementations
{
    public class EntityFrameworkChatMessageStore : IChatMessageStore
    {
        private readonly ApplicationDbContext _dbContext;

        public EntityFrameworkChatMessageStore(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<ChatMessageDocument> AppendMessage(ChatMessageDocument message)
        {
            _dbContext.ChatMessages.Add(message);
            await _dbContext.SaveChangesAsync();
            return message;
        }

        public async Task<IReadOnlyList<ChatMessageDocument>> GetRecentMessages(string conversationKey, int take)
        {
            var recentMessages = await _dbContext.ChatMessages
                .AsNoTracking()
                .Where(message => message.ConversationKey == conversationKey)
                .OrderByDescending(message => message.TimestampUtc)
                .Take(take)
                .ToListAsync();

            return recentMessages
                .OrderBy(message => message.TimestampUtc)
                .ToList();
        }
    }
}
