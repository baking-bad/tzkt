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
using Microsoft.Extensions.Configuration;
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
        private readonly AuthConfig Config;
        public TzktAuthenticationHandler(IConfiguration config, IOptionsMonitor< TzktAuthenticationOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock) : base(options, logger, encoder, clock)
        {
            Config = config.GetAuthConfig();
        }
    
        private const string UserHeader = "X-TZKT-USER";
        private const string NonceHeader = "X-TZKT-NONCE";
        private const string SignatureHeader = "X-TZKT-SIGNATURE";

        private static readonly List<string> Keys = new() {UserHeader, NonceHeader, SignatureHeader};


        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if(!AuthenticationHeaderValue.TryParse(Request.Headers[UserHeader], out AuthenticationHeaderValue userHeaderValue)
            || !AuthenticationHeaderValue.TryParse(Request.Headers[NonceHeader], out AuthenticationHeaderValue nonceHeaderValue)
            || !AuthenticationHeaderValue.TryParse(Request.Headers[SignatureHeader], out AuthenticationHeaderValue signHeaderValue)
            || !long.TryParse(nonceHeaderValue.Scheme, out var nonce))
            {
                //Invalid Authorization header
                return AuthenticateResult.NoResult();
            }

            if(Config.Admins.All(x => x.Username != userHeaderValue.Scheme))
            {
                return AuthenticateResult.Fail("User doesn't exist");
            }

            //TODO Nonce should be used just one time
            if (DateTime.UtcNow.AddSeconds(-Config.NonceLifetime) > DateTime.UnixEpoch.AddSeconds(nonce))
            {
                return AuthenticateResult.Fail("Outdated nonce");
            }
            
            if (!Request.Body.CanSeek)
            {
                Request.EnableBuffering();
            }
            Request.Body.Position = 0;
            var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var body = await reader.ReadToEndAsync().ConfigureAwait(false);
            Request.Body.Position = 0;

            var hash = Hex.Convert(Blake2b.GetDigest(Utf8.Parse(body)));
            
            var pubKey = PubKey.FromBase58(Config.Admins.FirstOrDefault(u => u.Username == userHeaderValue.Scheme)?.PubKey);
            var shortHash = Request.Path.Value;
            if(!pubKey.Verify($"{nonce}{hash}", signHeaderValue.Scheme))
                return AuthenticateResult.Fail("Invalid signature");

            var claims = new[] { new Claim(ClaimTypes.Name, userHeaderValue.Scheme) };
            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);
            return AuthenticateResult.Success(ticket);
        }
    }
}