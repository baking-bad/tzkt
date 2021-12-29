using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;

namespace Tzkt.Api.Services.Auth
{
    public class PasswordAuth : IAuthService
    {
        readonly Dictionary<string, Dictionary<string, (Access access, Dictionary<string, Access> sections)>> Rights;
        readonly Dictionary<string, AuthUser> Users;

        public PasswordAuth(IConfiguration config)
        {
            var cfg = config.GetAuthConfig();
            Rights = cfg.Users.ToDictionary(x => x.Name, x => x.Rights?
                                      .GroupBy(y => y.Table)
                                 .ToDictionary(z => z.Key, z => (z.FirstOrDefault(k => k.Section == null)?.Access ?? Access.None , z
                                 .Where(p => p.Section != null)
                                 .ToDictionary(q => q.Section, q => q.Access))));
            Users = cfg.Users.ToDictionary(x => x.Name, x => x);
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

            if (!Rights.TryGetValue(headers.User, out var user))
            {
                error = $"User {headers.User} doesn't exist";
                return false;
            }

            if (user == null)
            {
                return true;
            }
            
            if (!user.TryGetValue(requestedRights.Table, out var sections))
            {
                error = $"User {headers.User} doesn't have required permissions. {requestedRights.Table} required.";
                return false;
            }

            if (sections.access >= requestedRights.Access)
            {
                return true;
            }

            if (requestedRights.Section == null)
            {
                error = $"User {headers.User} doesn't have required permissions. {requestedRights.Access} required.";
                return false;
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
            if (!string.IsNullOrEmpty(json)) return TryAuthenticate(headers, requestedRights, out error);
            
            error = $"The body is empty";
            return false;

        }
    }
}