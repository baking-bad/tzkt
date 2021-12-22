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

        public PubKeyAuth(IConfiguration config)
        {
            Config = config.GetAuthConfig();
            Nonces = Config.Credentials.ToDictionary(x => x.User, _ => long.MinValue );
        }

        public bool TryAuthenticate(AuthHeaders headers, AccessRights access, out string error)
        {
            error = null;

            if (!TryAuthenticateBase(headers, access, out error))
            {
                return false;
            }

            var credentials = Config.Credentials.FirstOrDefault(x => x.User == headers.User);
            
            var key = PubKey.FromBase58(credentials.PubKey);
            if (!key.Verify($"{headers.Nonce}", headers.Signature))
            {
                error = $"Invalid signature";
                return false;
            }

            Nonces[headers.User] = (long)headers.Nonce;
            return true;
        }

        public bool TryAuthenticate(AuthHeaders headers, AccessRights access, string json, out string error)
        {
            error = null;

            if (!TryAuthenticateBase(headers, access, out error))
            {
                return false;
            }
            
            if (string.IsNullOrEmpty(json))
            {
                error = $"The body is empty";
                return false;
            }

            var credentials = Config.Credentials.FirstOrDefault(x => x.User == headers.User);
            
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

        private bool TryAuthenticateBase(AuthHeaders headers, AccessRights access, out string error)
        {
            error = null;
            
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