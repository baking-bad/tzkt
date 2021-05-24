using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Netezos.Encoding;
using Netezos.Keys;
using Netezos.Utils;

namespace Tzkt.Api.Utils
{
    public class TzktAuthenticationOptions
        : AuthenticationSchemeOptions
    { }

    public class TzktAuthenticationHandler : AuthenticationHandler<TzktAuthenticationOptions>
    {
        public TzktAuthenticationHandler(IOptionsMonitor<TzktAuthenticationOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock) : base(options, logger, encoder, clock) {}
    
        private const string UserHeader = "X-TZKT-USER";
        private const string NonceHeader = "X-TZKT-NONCE";
        private const string SignatureHeader = "X-TZKT-SIGNATURE";

        //TODO To config
        private Dictionary<string, string> Users = new()
        {
            {"vadim", "edpktxafnWbAESCnir4T8E22nPTTg9SQmvCGSorwCKWA8zDVubKisk"}
        };

        private static readonly List<string> Keys = new() {UserHeader, NonceHeader, SignatureHeader};


        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if(!AuthenticationHeaderValue.TryParse(Request.Headers[UserHeader], out AuthenticationHeaderValue userHeaderValue)
            || !AuthenticationHeaderValue.TryParse(Request.Headers[NonceHeader], out AuthenticationHeaderValue nonceHeaderValue)
            || !AuthenticationHeaderValue.TryParse(Request.Headers[SignatureHeader], out AuthenticationHeaderValue signHeaderValue))
            {
                //Invalid Authorization header
                return AuthenticateResult.NoResult();
            }

            if(!Users.TryGetValue(userHeaderValue.Scheme, out var pubKeyString))
            {
                return AuthenticateResult.Fail("User doesn't exist");
            }

            if (DateTime.UtcNow.AddSeconds(-100) > DateTime.UnixEpoch.AddSeconds(long.Parse(nonceHeaderValue.Scheme)))
            {
                return AuthenticateResult.Fail("Outdated nonce");
            }
            
            if (!Request.Body.CanSeek)
            {
                // We only do this if the stream isn't *already* seekable,
                // as EnableBuffering will create a new stream instance
                // each time it's called
                Request.EnableBuffering();
            }
            Request.Body.Position = 0;
            var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var body = await reader.ReadToEndAsync().ConfigureAwait(false);
            Request.Body.Position = 0;

            var hash = Hex.Convert(Blake2b.GetDigest(Utf8.Parse(body)));
            
            var pubKey = PubKey.FromBase58(pubKeyString);
            if(!pubKey.Verify($"{nonceHeaderValue.Scheme}{hash}", signHeaderValue.Scheme))
                return AuthenticateResult.Fail("Invalid signature");

            var claims = new[] { new Claim(ClaimTypes.Name, userHeaderValue.Scheme) };
            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);
            return AuthenticateResult.Success(ticket);
        }
    }
}