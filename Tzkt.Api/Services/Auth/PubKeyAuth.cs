using System;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Netezos.Encoding;
using Netezos.Keys;
using Netezos.Utils;

namespace Tzkt.Api.Services.Auth
{
    public class PubKeyAuth : IAuthService
    {
        readonly AuthConfig Config;
        readonly Dictionary<string, long> Nonces;
        readonly Dictionary<string, Dictionary<string, (Access access, Dictionary<string, Access> sections)>> Rights;
        readonly Dictionary<string, PubKey> PubKeys;
        
        public PubKeyAuth(IConfiguration config)
        {
            var cfg = config.GetAuthConfig();
            Config = cfg;
            Nonces = cfg.Users.ToDictionary(x => x.Name, _ => long.MinValue );
            Rights = cfg.Users.ToDictionary(x => x.Name, x => x.Rights?
                .GroupBy(y => y.Table)
                .ToDictionary(z => z.Key, z => (z
                    .Where(k => k.Section == null).Select(a => a.Access).DefaultIfEmpty(Access.None).Max() , z
                    .Where(p => p.Section != null)
                    .ToDictionary(q => q.Section, q => q.Access))));
            PubKeys = cfg.Users.ToDictionary(x => x.Name, x => PubKey.FromBase58(x.PubKey));
        }

        public bool TryAuthenticate(AuthHeaders headers, AccessRights requestedRights, out string error)
        {
            error = null;

            if (!TryAuthenticateBase(headers, requestedRights, out error, out var pubKey))
            {
                return false;
            }

            if (!pubKey.Verify($"{headers.Nonce}", headers.Signature))
            {
                error = $"Invalid signature";
                return false;
            }

            Nonces[headers.User] = (long) headers.Nonce;
            return true;
        }

        public bool TryAuthenticate(AuthHeaders headers, AccessRights requestedRights, string json, out string error)
        {
            error = null;

            if (!TryAuthenticateBase(headers, requestedRights, out error, out var pubKey))
            {
                return false;
            }
            
            if (string.IsNullOrEmpty(json))
            {
                error = $"The body is empty";
                return false;
            }

            var hash = Hex.Convert(Blake2b.GetDigest(Utf8.Parse(json)));
            
            if (!pubKey.Verify($"{headers.Nonce}{hash}", headers.Signature))
            {
                error = $"Invalid signature";
                return false;
            }
            
            Nonces[headers.User] = (long)headers.Nonce;
            return true;
        }

        private bool TryAuthenticateBase(AuthHeaders headers, AccessRights requestedRights, out string error, out PubKey pubKey)
        {
            error = null;
            pubKey = null;
            
            if (string.IsNullOrEmpty(headers?.User))
            {
                error = "The X-TZKT-USER header is required";
                return false;
            }

            if (headers.Nonce == null)
            {
                error = "The X-TZKT-NONCE header is required";
                return false;
            }

            if (string.IsNullOrEmpty(headers.Signature))
            {
                error = "The X-TZKT-SIGNATURE header is required";
                return false;
            }

            if (!PubKeys.TryGetValue(headers.User, out pubKey))
            {
                error = $"User {headers.User} doesn't exist";
                return false;
            }
            
            var nonce = (long)headers.Nonce;
            var nonceTime = DateTime.UnixEpoch.AddMilliseconds(nonce);

            if (nonceTime < DateTime.UtcNow.AddSeconds(-Config.NonceLifetime))
            {
                error = $"Nonce too old. Server time: {DateTime.UtcNow}, nonce: {nonceTime}";
                return false;
            }

            if (nonce <= Nonces[headers.User])
            {
                error = $"Nonce {nonce} has already been used";
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
    }
}