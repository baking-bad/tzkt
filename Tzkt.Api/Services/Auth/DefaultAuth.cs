namespace Tzkt.Api.Services.Auth
{
    public class DefaultAuth : IAuthService
    {
        public bool TryAuthenticate(AuthHeaders headers, out string error)
        {
            error = null;
            return true;
        }

        public bool TryAuthenticate(AuthHeaders headers, string json, out string error)
        {
            error = null;
            return true;
        }
    }
}