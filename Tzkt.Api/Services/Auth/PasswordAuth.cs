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

        public bool TryAuthenticate(AuthHeaders headers, out string error)
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

            if (!Config.Credentials.TryGetValue(headers.User, out var password))
            {
                error = $"User {headers.User} doesn't exist";
                return false;
            }

            if (headers.Password != password)
            {
                error = $"Invalid password";
                return false;
            }

            return true;
        }

        public bool TryAuthenticate(AuthHeaders headers, string json, out string error)
        {
            return TryAuthenticate(headers, out error);
        }
    }
}