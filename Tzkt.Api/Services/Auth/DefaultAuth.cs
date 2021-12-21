namespace Tzkt.Api.Services.Auth
{
    public class DefaultAuth : IAuthService
    {
        public bool TryAuthenticate(AuthHeaders headers, AuthRights requiredRights, out string error)
        {
            error = null;
            return true;
        }

        public bool TryAuthenticate(AuthHeaders headers, AuthRights requiredRights, string json, out string error)
        {
            error = null;
            return true;
        }
    }
}