using TdpTrans.DTOs;

namespace TdpTrans.Controllers
{
    internal static class ActorHeaderReader
    {
        public const string HttpContextItemKey = "tdptrans.session-actor";

        public static bool TryRead(HttpRequest request, out int userId)
        {
            userId = 0;

            if (request.HttpContext.Items.TryGetValue(HttpContextItemKey, out var actor) &&
                actor is SessionActor sessionActor)
            {
                userId = sessionActor.UserId;
                return true;
            }

            return false;
        }

        public static bool TryReadActor(HttpContext context, out SessionActor? actor)
        {
            actor = context.Items.TryGetValue(HttpContextItemKey, out var value)
                ? value as SessionActor
                : null;

            return actor != null;
        }
    }
}
