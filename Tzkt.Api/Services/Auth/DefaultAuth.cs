namespace Tzkt.Api.Services.Auth
{
    public class DefaultAuth : IAuthService
    {
        public bool TryAuthenticate(AuthHeaders headers, AccessRights access, out string error)
        {
            error = null;
            return true;
        }

        public bool TryAuthenticate(AuthHeaders headers, AccessRights access, string json, out string error)
        {
            error = null;
            return true;
        }
    }
}