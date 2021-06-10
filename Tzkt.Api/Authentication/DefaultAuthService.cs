namespace Tzkt.Api.Authentication
{
    public class DefaultAuthService : IAuthService
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