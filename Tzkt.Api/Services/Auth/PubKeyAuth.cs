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
                .GroupBy(g => g.Table)
                .ToDictionary(g => g.Key, g =>
                (
                    g.FirstOrDefault(r => r.Section == null)?.Access ?? Access.None,
                    g.Where(r => r.Section != null).ToDictionary(r => r.Section, r => r.Access)
                )));
            PubKeys = cfg.Users.ToDictionary(x => x.Name, x => PubKey.FromBase58(x.PubKey));
        }

        public bool TryAuthenticate(AuthHeaders headers, AccessRights requestedRights, out string error)
        {
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
            if (!TryAuthenticateBase(headers, requestedRights, out error, out var pubKey))
            {
                return false;
            }
            
            if (string.IsNullOrEmpty(json))
            {
                error = $"Request body is empty";
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
            
            if (nonce is < 0 or >= 253402300800000)
            {
                error = $"Nonce out of range.";
                return false;
            }
            
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
                error = $"User {headers.User} doesn't have required permissions. {requestedRights.Access} required for section {requestedRights.Section}. {sectionAccess} granted";
                return false;
            }

            return true;
        }
    }
}