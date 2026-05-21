using LiteDB;
using TdpTrans.Models;
using TdpTrans.Repositories.Interfaces;

namespace TdpTrans.Repositories.Implementations
{
    public class LiteDbChatMessageStore : IChatMessageStore, IDisposable
    {
        private readonly LiteDatabase _database;
        private readonly ILiteCollection<ChatMessageDocument> _messages;

        public LiteDbChatMessageStore(string databasePath)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(databasePath)!);
            _database = new LiteDatabase(databasePath);
            _messages = _database.GetCollection<ChatMessageDocument>("chat_messages");
            _messages.EnsureIndex(message => message.ConversationKey);
            _messages.EnsureIndex(message => message.TimestampUtc);
        }

        public Task<ChatMessageDocument> AppendMessage(ChatMessageDocument message)
        {
            _messages.Insert(message);
            return Task.FromResult(message);
        }

        public void Dispose()
        {
            _database.Dispose();
        }

        public Task<IReadOnlyList<ChatMessageDocument>> GetRecentMessages(string conversationKey, int take)
        {
            var messages = _messages
                .Query()
                .Where(message => message.ConversationKey == conversationKey)
                .OrderByDescending(message => message.TimestampUtc)
                .Limit(take)
                .ToList()
                .OrderBy(message => message.TimestampUtc)
                .ToList();

            return Task.FromResult<IReadOnlyList<ChatMessageDocument>>(messages);
        }
    }
}
