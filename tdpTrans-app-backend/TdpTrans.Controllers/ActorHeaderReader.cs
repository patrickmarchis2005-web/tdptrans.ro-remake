namespace TdpTrans.Controllers
{
    internal static class ActorHeaderReader
    {
        public const string HeaderName = "X-User-Id";

        public static bool TryRead(HttpRequest request, out int userId)
        {
            return int.TryParse(request.Headers[HeaderName].FirstOrDefault(), out userId);
        }
    }
}
