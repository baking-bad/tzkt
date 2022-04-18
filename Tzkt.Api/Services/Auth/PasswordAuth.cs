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
                .GroupBy(r => r.Table)
                .ToDictionary(g => g.Key, g => 
                (
                    g.FirstOrDefault(r => r.Section == null)?.Access ?? Access.None,
                    g.Where(r => r.Section != null).ToDictionary(r => r.Section, r => r.Access)
                )));
            Users = cfg.Users.ToDictionary(x => x.Name);
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

            if (!Users.TryGetValue(headers.User, out var user))
            {
                error = $"User {headers.User} doesn't exist";
                return false;
            }
            
            if (headers.Password != user.Password)
            {
                error = $"Invalid password";
                return false;
            }

            if (!Rights.TryGetValue(headers.User, out var rights))
            {
                error = $"User {headers.User} doesn't exist";
                return false;
            }

            if (rights == null)
            {
                return true;
            }
            
            if (!rights.TryGetValue(requestedRights.Table, out var tableRights))
            {
                error = $"User {headers.User} doesn't have required permissions. {requestedRights.Table} required.";
                return false;
            }

            if (tableRights.access >= requestedRights.Access)
            {
                return true;
            }

            if (requestedRights.Section == null)
            {
                error = $"User {headers.User} doesn't have required permissions. {requestedRights.Access} required.";
                return false;
            }

            if (!tableRights.sections.TryGetValue(requestedRights.Section, out var sectionAccess))
            {
                error = $"User {headers.User} doesn't have required permissions. {requestedRights.Section} required.";
                return false;
            }
            
            if (sectionAccess < requestedRights.Access)
            {
                error = $"User {headers.User} doesn't have required permissions. {requestedRights.Access} required. {sectionAccess} granted";
                return false;
            }

            return true;
        }

        public bool TryAuthenticate(AuthHeaders headers, AccessRights requestedRights, string json, out string error)
        {
            if (string.IsNullOrEmpty(json))
            {
                error = "Request body is empty";
                return false;
            }

            return TryAuthenticate(headers, requestedRights, out error);
        }
    }
}