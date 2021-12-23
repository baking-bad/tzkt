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
        Dictionary<string, Dictionary<string, Dictionary<string, Access>>> Rights;
        Dictionary<string, AuthUser> Users;
        
        public PubKeyAuth(IConfiguration config)
        {
            Config = config.GetAuthConfig();
            Nonces = Config.Users.ToDictionary(x => x.Name, _ => long.MinValue );
            Rights = Config.Users.ToDictionary(x => x.Name, x => x.Rights
                .GroupBy(y => y.Table)
                .ToDictionary(z => z.Key, z => z
                    .ToDictionary(q => q.Section, q => q.Access)));
            Users = Config.Users.ToDictionary(x => x.Name, x => x);
        }

        public bool TryAuthenticate(AuthHeaders headers, AccessRights requestedRights, out string error)
        {
            error = null;

            if (!TryAuthenticateBase(headers, requestedRights, out error, out var credentials))
            {
                return false;
            }

            var key = PubKey.FromBase58(credentials.PubKey);
            if (!key.Verify($"{headers.Nonce}", headers.Signature))
            {
                error = $"Invalid signature";
                return false;
            }

            Nonces[headers.User] = (long)headers.Nonce;
            return true;
        }

        public bool TryAuthenticate(AuthHeaders headers, AccessRights requestedRights, string json, out string error)
        {
            error = null;

            if (!TryAuthenticateBase(headers, requestedRights, out error, out var credentials))
            {
                return false;
            }
            
            if (string.IsNullOrEmpty(json))
            {
                error = $"The body is empty";
                return false;
            }

            var hash = Hex.Convert(Blake2b.GetDigest(Utf8.Parse(json)));
            
            var key = PubKey.FromBase58(credentials.PubKey);
            if (!key.Verify($"{headers.Nonce}{hash}", headers.Signature))
            {
                error = $"Invalid signature";
                return false;
            }
            
            Nonces[headers.User] = (long)headers.Nonce;
            return true;
        }

        private bool TryAuthenticateBase(AuthHeaders headers, AccessRights requestedRights, out string error, out AuthUser credentials)
        {
            error = null;
            credentials = null;
            
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

            if (!Users.TryGetValue(headers.User, out credentials))
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

            if (!sections.TryGetValue(requestedRights.Section, out var access))
            {
                error = $"User {headers.User} doesn't have required permissions. {requestedRights.Section} required.";
                return false;
            }
            
            if (access < requestedRights.Access)
            {
                error = $"User {headers.User} doesn't have required permissions. {requestedRights.Access} required. {access} granted";
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

            return true;
        }
    }
}