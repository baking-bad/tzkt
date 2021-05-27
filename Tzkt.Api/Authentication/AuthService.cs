using System;
using System.Collections.Generic;
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
            Nonces = Config.Admins.ToDictionary(x => x.Username, x => long.MinValue);
        }

        public bool Authorized(AuthHeaders headers, List<Met>  body, out string error)
        {
            error = null;
            
            if(Config.Admins.All(x => x.Username != headers.User))
            {
                error = $"User {headers.User} doesn't exist";
                return false;
            }

            //TODO Nonce should be used just one time
            if (DateTime.UtcNow.AddSeconds(-Config.NonceLifetime) > DateTime.UnixEpoch.AddSeconds(headers.Nonce))
            {
                error = $"Nonce too old. Server time: {DateTime.UtcNow}. Request time: {DateTime.UnixEpoch.AddSeconds(headers.Nonce)}";
                return false;
            }
            
            if (Nonces[headers.User] >= headers.Nonce)
            {
                error = $"Nonce {headers.Nonce} has already used";
                return false;
            }

            
            /*if (!request.Body.CanSeek)
            {
                request.EnableBuffering();
            }
            request.Body.Position = 0;
            var reader = new StreamReader(request.Body, Encoding.UTF8);
            var body = reader.ReadToEnd();
            request.Body.Position = 0;*/
            var jso = new JsonSerializerOptions
            {
                Converters = { new DateTimeConverter()}
            };
            // jso.Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
            var json = JsonSerializer.Serialize(body, jso);
            var hash = Hex.Convert(Blake2b.GetDigest(Utf8.Parse(json)));
            
            var pubKey = PubKey.FromBase58(Config.Admins.FirstOrDefault(u => u.Username == headers.User)?.PubKey);
            if (!pubKey.Verify($"{headers.Nonce}{hash}", headers.Signature))
            {
                error = $"Invalid signature";
                return false;
            }
            
            Nonces[headers.User] = headers.Nonce;

            return true;
        }
    }
}