using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace TdpTrans.Services
{
    public class ChatConnectionManager
    {
        private sealed class ChatConnection
        {
            public required int UserId { get; init; }
            public required string ClientKey { get; init; }
            public required WebSocket Socket { get; init; }
        }

        private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
        private readonly ConcurrentDictionary<string, ChatConnection> _connections = new();
        private readonly ConcurrentDictionary<string, string> _clientConnections = new();

        public async Task<string> Add(int userId, string clientKey, WebSocket socket)
        {
            var clientConnectionKey = $"{userId}:{clientKey}";

            if (_clientConnections.TryRemove(clientConnectionKey, out var existingConnectionId))
            {
                await Remove(existingConnectionId);
            }

            var connectionId = Guid.NewGuid().ToString("N");
            _connections[connectionId] = new ChatConnection
            {
                UserId = userId,
                ClientKey = clientKey,
                Socket = socket
            };
            _clientConnections[clientConnectionKey] = connectionId;
            return connectionId;
        }

        public async Task BroadcastToUsers<T>(IEnumerable<int> userIds, T payload, CancellationToken cancellationToken = default)
        {
            var recipientSet = userIds.Distinct().ToHashSet();
            var buffer = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload, SerializerOptions));
            var segment = new ArraySegment<byte>(buffer);
            var disconnectedIds = new List<string>();

            foreach (var connection in _connections)
            {
                if (!recipientSet.Contains(connection.Value.UserId))
                {
                    continue;
                }

                if (connection.Value.Socket.State != WebSocketState.Open)
                {
                    disconnectedIds.Add(connection.Key);
                    continue;
                }

                try
                {
                    await connection.Value.Socket.SendAsync(segment, WebSocketMessageType.Text, true, cancellationToken);
                }
                catch
                {
                    disconnectedIds.Add(connection.Key);
                }
            }

            foreach (var connectionId in disconnectedIds)
            {
                _connections.TryRemove(connectionId, out _);
            }
        }

        public async Task Remove(string connectionId)
        {
            if (_connections.TryRemove(connectionId, out var connection))
            {
                _clientConnections.TryRemove($"{connection.UserId}:{connection.ClientKey}", out _);

                if (connection.Socket.State == WebSocketState.Open)
                {
                    await connection.Socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed", CancellationToken.None);
                }
            }
        }
    }
}
