using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using TdpTrans.DTOs;
using TdpTrans.Models;

namespace TdpTrans.Services
{
    public class ChatWebSocketEndpoint
    {
        private sealed class ChatInboundMessage
        {
            public int RecipientUserId { get; set; }
            public string Message { get; set; } = string.Empty;
        }

        private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ChatConnectionManager _connectionManager;

        public ChatWebSocketEndpoint(IServiceScopeFactory serviceScopeFactory, ChatConnectionManager connectionManager)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _connectionManager = connectionManager;
        }

        public async Task HandleAsync(HttpContext context)
        {
            if (!context.WebSockets.IsWebSocketRequest)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            var accessToken = context.Request.Query["accessToken"].ToString();
            if (string.IsNullOrWhiteSpace(accessToken))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }

            SessionActor? actor;
            using (var authScope = _serviceScopeFactory.CreateScope())
            {
                var authSessionService = authScope.ServiceProvider.GetRequiredService<IAuthSessionService>();
                actor = await authSessionService.Authenticate(accessToken);
            }

            if (actor == null)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }

            var userId = actor.UserId;

            var clientKey = context.Request.Query["clientKey"].ToString();
            if (string.IsNullOrWhiteSpace(clientKey))
            {
                clientKey = Guid.NewGuid().ToString("N");
            }

            using (var permissionScope = _serviceScopeFactory.CreateScope())
            {
                var userAccessService = permissionScope.ServiceProvider.GetRequiredService<IUserAccessService>();
                await userAccessService.EnsurePermission(userId, PermissionNames.ChatUse, "Tried to connect to chat without permission.");
            }

            using var socket = await context.WebSockets.AcceptWebSocketAsync();
            var connectionId = await _connectionManager.Add(userId, clientKey, socket);

            using (var connectScope = _serviceScopeFactory.CreateScope())
            {
                var activityLogService = connectScope.ServiceProvider.GetRequiredService<IActivityLogService>();
                await activityLogService.Log(userId, ActivityActionNames.ChatConnected, "Connected to the real-time chat.");
            }

            try
            {
                while (socket.State == WebSocketState.Open)
                {
                    var payload = await ReceiveMessage(socket);
                    if (payload == null)
                    {
                        break;
                    }

                    if (string.IsNullOrWhiteSpace(payload.Message))
                    {
                        continue;
                    }

                using var messageScope = _serviceScopeFactory.CreateScope();
                    var authSessionService = messageScope.ServiceProvider.GetRequiredService<IAuthSessionService>();
                    var refreshedActor = await authSessionService.Authenticate(accessToken);
                    if (refreshedActor == null)
                    {
                        break;
                    }

                    var chatService = messageScope.ServiceProvider.GetRequiredService<IChatService>();
                    var createdMessage = await chatService.CreateMessage(userId, payload.RecipientUserId, payload.Message);
                    await _connectionManager.BroadcastToUsers(
                        new[] { createdMessage.SenderUserId, createdMessage.RecipientUserId },
                        createdMessage);
                }
            }
            finally
            {
                using var disconnectScope = _serviceScopeFactory.CreateScope();
                var activityLogService = disconnectScope.ServiceProvider.GetRequiredService<IActivityLogService>();
                await activityLogService.Log(userId, ActivityActionNames.ChatDisconnected, "Disconnected from the real-time chat.");
                await _connectionManager.Remove(connectionId);
            }
        }

        private static async Task<ChatInboundMessage?> ReceiveMessage(WebSocket socket)
        {
            var buffer = new byte[4096];
            using var memoryStream = new MemoryStream();

            while (true)
            {
                var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    return null;
                }

                memoryStream.Write(buffer, 0, result.Count);

                if (result.EndOfMessage)
                {
                    break;
                }
            }

            var jsonPayload = Encoding.UTF8.GetString(memoryStream.ToArray());
            return JsonSerializer.Deserialize<ChatInboundMessage>(jsonPayload, SerializerOptions);
        }
    }
}
