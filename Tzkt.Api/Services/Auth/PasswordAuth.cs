using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;

namespace Tzkt.Api.Services.Auth
{
    public class PasswordAuth : IAuthService
    {
        readonly AuthConfig Config;
        Dictionary<string, Dictionary<string, (Access access, Dictionary<string, Access> sections)>> Rights;
        Dictionary<string, AuthUser> Users;

        public PasswordAuth(IConfiguration config)
        {
            Config = config.GetAuthConfig();
            Rights = Config.Users.ToDictionary(x => x.Name, x => x.Rights
                                      .GroupBy(y => y.Table)
                                 .ToDictionary(z => z.Key, z => (z.Where(k => k.Section == null).FirstOrDefault()?.Access ?? Access.None , z
                                 .Where(p => p.Section != null)
                                 .ToDictionary(q => q.Section, q => q.Access))));
            Users = Config.Users.ToDictionary(x => x.Name, x => x);
        }

        public bool TryAuthenticate(AuthHeaders headers, AccessRights requestedRights, out string error)
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

            if (!Users.TryGetValue(headers.User, out var credentials))
            {
                error = $"User {headers.User} doesn't exist";
                return false;
            }
            
            if (headers.Password != credentials.Password)
            {
                error = $"Invalid password";
                return false;
            }

            if (!Rights.GetValueOrDefault(headers.User).TryGetValue(requestedRights.Table, out var sections))
            {
                error = $"User {headers.User} doesn't have required permissions. {requestedRights.Table} required.";
                return false;
            }

            if (sections.access != Access.None)
            {
                return true;
            }

            if (!sections.sections.TryGetValue(requestedRights.Section, out var access))
            {
                error = $"User {headers.User} doesn't have required permissions. {requestedRights.Section} required.";
                return false;
            }
            
            if (access < requestedRights.Access)
            {
                error = $"User {headers.User} doesn't have required permissions. {requestedRights.Access} required. {access} granted";
                return false;
            }

            return true;
        }

        public bool TryAuthenticate(AuthHeaders headers, AccessRights requestedRights, string json, out string error)
        {
            if (string.IsNullOrEmpty(json))
            {
                error = $"The body is empty";
                return false;
            }
            
            return TryAuthenticate(headers, requestedRights, out error);
        }
    }
}