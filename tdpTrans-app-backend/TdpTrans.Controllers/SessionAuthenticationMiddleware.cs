using TdpTrans.Services;

namespace TdpTrans.Controllers
{
    public class SessionAuthenticationMiddleware
    {
        private readonly RequestDelegate _next;

        public SessionAuthenticationMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, IAuthSessionService authSessionService)
        {
            var token = ExtractBearerToken(context.Request.Headers.Authorization);
            if (!string.IsNullOrWhiteSpace(token))
            {
                var actor = await authSessionService.Authenticate(token);
                if (actor != null)
                {
                    context.Items[ActorHeaderReader.HttpContextItemKey] = actor;
                }
            }

            await _next(context);
        }

        private static string? ExtractBearerToken(string? authorizationHeader)
        {
            if (string.IsNullOrWhiteSpace(authorizationHeader))
            {
                return null;
            }

            const string prefix = "Bearer ";
            return authorizationHeader.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                ? authorizationHeader[prefix.Length..].Trim()
                : null;
        }
    }
}
