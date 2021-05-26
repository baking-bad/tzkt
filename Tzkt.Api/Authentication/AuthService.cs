using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection.Metadata;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Netezos.Encoding;
using Netezos.Keys;
using Netezos.Utils;
using Tzkt.Api.Models;
using Tzkt.Api.Utils;
using TzKT_Client;

namespace Tzkt.Api.Authentication
{
    public class AuthService
    {
        private readonly AuthConfig Config;
        private const string UserHeader = "X-TZKT-USER";
        private const string NonceHeader = "X-TZKT-NONCE";
        private const string SignatureHeader = "X-TZKT-SIGNATURE";

        private Dictionary<string, long> Nonces;

        public AuthService(IConfiguration config)
        {
            Config = config.GetAuthConfig();
            Nonces = Config.Admins.ToDictionary(x => x.Username, x => long.MinValue);
        }

        public bool Authorized(IHeaderDictionary headers, List<Met>  body, out string error)
        {
            error = null;
            
            //TODO Get Rid of that
            if(!AuthenticationHeaderValue.TryParse(headers[UserHeader], out var userHeaderValue)
               || !AuthenticationHeaderValue.TryParse(headers[NonceHeader], out var nonceHeaderValue)
               || !AuthenticationHeaderValue.TryParse(headers[SignatureHeader], out var signHeaderValue)
               || !long.TryParse(nonceHeaderValue.Scheme, out var nonce))
            {
                error = $"{UserHeader}, {NonceHeader}, and {SignatureHeader} should be provided for the Authorization";
                return false;
            }

            if(Config.Admins.All(x => x.Username != userHeaderValue.Scheme))
            {
                error = $"User {userHeaderValue.Scheme} doesn't exist";
                return false;
            }

            //TODO Nonce should be used just one time
            if (DateTime.UtcNow.AddSeconds(-Config.NonceLifetime) > DateTime.UnixEpoch.AddSeconds(nonce))
            {
                error = $"Nonce too old";
                return false;
            }
            
            if (Nonces[userHeaderValue.Scheme] >= nonce)
            {
                error = $"Nonce {nonce} already used";
                return false;
            }

            Nonces[userHeaderValue.Scheme] = nonce;
            
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
            
            var pubKey = PubKey.FromBase58(Config.Admins.FirstOrDefault(u => u.Username == userHeaderValue.Scheme)?.PubKey);
            if (!pubKey.Verify($"{nonce}{hash}", signHeaderValue.Scheme))
            {
                error = $"Invalid signature";
                return false;
            }

            return true;
        }
    }
}