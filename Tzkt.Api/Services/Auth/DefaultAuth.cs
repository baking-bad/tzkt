namespace Tzkt.Api.Services.Auth
{
    public class DefaultAuth : IAuthService
    {
        public bool Authorized(AuthHeaders headers, string json, out string error)
        {
            error = null;
            return true;
        }

        public bool Authorized(AuthHeaders headers, out string error)
        {
            error = null;
            return true;
        }
    }
}