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
    public class AuthService
    {
        private readonly AuthConfig Config;
        private const string UserHeader = "X-TZKT-USER";
        private const string NonceHeader = "X-TZKT-NONCE";
        private const string SignatureHeader = "X-TZKT-SIGNATURE";

        private readonly Dictionary<string, long> Nonces;

        public AuthService(IConfiguration config)
        {
            Config = config.GetAuthConfig();
            Nonces = Config.Admins.ToDictionary(x => x.Username, x => long.MinValue );
        }
        
        //TODO Method overload for GET
        public bool Authorized(AuthHeaders headers, string json, out string error)
        {
            error = null;
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

            //TODO Check unicode chars one more time
            // var json = JsonSerializer.Serialize(body, jso);
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