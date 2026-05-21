using Microsoft.AspNetCore.Mvc;
using TdpTrans.DTOs;
using TdpTrans.Services;

namespace TdpTrans.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly IChatService _chatService;
        private readonly ChatConnectionManager _chatConnectionManager;

        public sealed class SendChatMessageRequest
        {
            public int RecipientUserId { get; set; }
            public string Message { get; set; } = string.Empty;
        }

        public ChatController(IChatService chatService, ChatConnectionManager chatConnectionManager)
        {
            _chatService = chatService;
            _chatConnectionManager = chatConnectionManager;
        }

        [HttpGet("contacts")]
        public async Task<ActionResult<IReadOnlyList<ChatContactResponse>>> GetContacts()
        {
            if (!ActorHeaderReader.TryRead(Request, out var actorUserId))
            {
                return Unauthorized("Este necesar un utilizator autentificat.");
            }

            try
            {
                var contacts = await _chatService.GetContacts(actorUserId);
                return Ok(contacts);
            }
            catch (UnauthorizedAccessException exception)
            {
                return StatusCode(StatusCodes.Status403Forbidden, exception.Message);
            }
            catch (InvalidOperationException exception)
            {
                return BadRequest(exception.Message);
            }
        }

        [HttpGet("history")]
        public async Task<ActionResult<IReadOnlyList<ChatMessageResponse>>> GetHistory([FromQuery] int withUserId, [FromQuery] int take = 40)
        {
            if (!ActorHeaderReader.TryRead(Request, out var actorUserId))
            {
                return Unauthorized("Este necesar un utilizator autentificat.");
            }

            try
            {
                var messages = await _chatService.GetRecentMessages(actorUserId, withUserId, take);
                return Ok(messages);
            }
            catch (ArgumentException exception)
            {
                return BadRequest(exception.Message);
            }
            catch (UnauthorizedAccessException exception)
            {
                return StatusCode(StatusCodes.Status403Forbidden, exception.Message);
            }
        }

        [HttpPost("messages")]
        public async Task<ActionResult<ChatMessageResponse>> SendMessage([FromBody] SendChatMessageRequest request)
        {
            if (!ActorHeaderReader.TryRead(Request, out var actorUserId))
            {
                return Unauthorized("Este necesar un utilizator autentificat.");
            }

            try
            {
                var createdMessage = await _chatService.CreateMessage(actorUserId, request.RecipientUserId, request.Message);
                await _chatConnectionManager.BroadcastToUsers(
                    new[] { createdMessage.SenderUserId, createdMessage.RecipientUserId },
                    createdMessage);

                return Ok(createdMessage);
            }
            catch (ArgumentException exception)
            {
                return BadRequest(exception.Message);
            }
            catch (UnauthorizedAccessException exception)
            {
                return StatusCode(StatusCodes.Status403Forbidden, exception.Message);
            }
        }
    }
}
