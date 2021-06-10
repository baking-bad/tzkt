using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Netezos.Encoding;
using Netezos.Keys;
using Netezos.Utils;
using Tzkt.Api.Utils;

namespace Tzkt.Api.Authentication
{
    public class PubKeyAuthService : IAuthService
    {
        private readonly AuthConfig Config;

        private readonly Dictionary<string, long> Nonces;

        public PubKeyAuthService(IConfiguration config)
        {
            Config = config.GetAuthConfig();
            Nonces = Config.Admins.ToDictionary(x => x.Username, x => long.MinValue );
        }
        
        public bool Authorized(AuthHeaders headers, string json, out string error)
        {
            error = null;

            
            if (string.IsNullOrWhiteSpace(headers.User))
            {
                error = $"The X-TZKT-USER header is required";
                return false;
            }
            
            if (headers.Nonce == null)
            {
                error = $"The X-TZKT-NONCE header is required";
                return false;
            }
            
            if (string.IsNullOrWhiteSpace(headers.Signature))
            {
                error = $"The X-TZKT-SIGNATURE header is required";
                return false;
            }
                
            var nonce = (long) headers.Nonce;
            
            if(Config.Admins.All(x => x.Username != headers.User))
            {
                error = $"User {headers.User} doesn't exist";
                return false;
            }

            if (DateTime.UtcNow.AddSeconds(-Config.NonceLifetime) > DateTime.UnixEpoch.AddMilliseconds(nonce))
            {
                error = $"Nonce too old. Server time: {DateTime.UtcNow}. Request time: {DateTime.UnixEpoch.AddMilliseconds(nonce)}";
                return false;
            }
            
            if (Nonces[headers.User] >= nonce)
            {
                error = $"Nonce {nonce} has already used";
                return false;
            }

            var hash = Hex.Convert(Blake2b.GetDigest(Utf8.Parse(json)));
            
            var pubKey = PubKey.FromBase58(Config.Admins.FirstOrDefault(u => u.Username == headers.User)?.PubKey);
            if (!pubKey.Verify($"{headers.Nonce}{hash}", headers.Signature))
            {
                error = $"Invalid signature";
                return false;
            }
            
            Nonces[headers.User] = nonce;

            return true;
        }
        
        public bool Authorized(AuthHeaders headers, out string error)
        {
            error = null;

            if (string.IsNullOrWhiteSpace(headers.User))
            {
                error = $"The X-TZKT-USER header is required";
                return false;
            }
            
            if (headers.Nonce == null)
            {
                error = $"The X-TZKT-NONCE header is required";
                return false;
            }
            
            if (string.IsNullOrWhiteSpace(headers.Signature))
            {
                error = $"The X-TZKT-SIGNATURE header is required";
                return false;
            }
            
            var nonce = (long) headers.Nonce;
            
            if(Config.Admins.All(x => x.Username != headers.User))
            {
                error = $"User {headers.User} doesn't exist";
                return false;
            }

            if (DateTime.UtcNow.AddSeconds(-Config.NonceLifetime) > DateTime.UnixEpoch.AddMilliseconds(nonce))
            {
                error = $"Nonce too old. Server time: {DateTime.UtcNow}. Request time: {DateTime.UnixEpoch.AddMilliseconds(nonce)}";
                return false;
            }
            
            if (Nonces[headers.User] >= nonce)
            {
                error = $"Nonce {nonce} has already used";
                return false;
            }

            var pubKey = PubKey.FromBase58(Config.Admins.FirstOrDefault(u => u.Username == headers.User)?.PubKey);
            if (!pubKey.Verify($"{headers.Nonce}", headers.Signature))
            {
                error = $"Invalid signature";
                return false;
            }
            
            Nonces[headers.User] = nonce;

            return true;
        }
    }
}