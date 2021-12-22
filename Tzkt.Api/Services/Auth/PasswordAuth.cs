using System.Linq;
using Microsoft.Extensions.Configuration;

namespace Tzkt.Api.Services.Auth
{
    public class PasswordAuth : IAuthService
    {
        readonly AuthConfig Config;

        public PasswordAuth(IConfiguration config)
        {
            Config = config.GetAuthConfig();
        }

        public bool TryAuthenticate(AuthHeaders headers, AccessRights access, out string error)
        {
            error = null;

            if (string.IsNullOrEmpty(headers?.User))
            {
                error = $"The X-TZKT-USER header is required";
                return false;
            }

            if (string.IsNullOrEmpty(headers.Password))
            {
                error = $"The X-TZKT-PASSWORD header is required";
                return false;
            }

            var credentials = Config.Credentials.FirstOrDefault(x => x.User == headers.User);

            if (credentials == null)
            {
                error = $"User {headers.User} doesn't exist";
                return false;
            }
            
            if (credentials.Access < access)
            {
                error = $"User {headers.User} doesn't have required permissions. {access} required. {credentials.Access} granted";
                return false;
            }

            if (headers.Password != credentials.Password)
            {
                error = $"Invalid password";
                return false;
            }

            return true;
        }

        public bool TryAuthenticate(AuthHeaders headers, AccessRights access, string json, out string error)
        {
            if (string.IsNullOrEmpty(json))
            {
                error = $"The body is empty";
                return false;
            }
            
            return TryAuthenticate(headers, access, out error);
        }
    }
}