namespace Tzkt.Api.Services.Auth
{
    public class DefaultAuth : IAuthService
    {
        public bool TryAuthorize(AuthHeaders headers, out string error)
        {
            error = null;
            return true;
        }

        public bool TryAuthorize(AuthHeaders headers, string json, out string error)
        {
            error = null;
            return true;
        }
    }
}