namespace Tzkt.Api.Services.Auth
{
    public class DefaultAuth : IAuthService
    {
        public bool TryAuthenticate(AuthHeaders headers, AccessRights requestedRights, out string error)
        {
            error = null;
            return true;
        }

        public bool TryAuthenticate(AuthHeaders headers, AccessRights requestedRights, string json, out string error)
        {
            error = null;
            return true;
        }
    }
}