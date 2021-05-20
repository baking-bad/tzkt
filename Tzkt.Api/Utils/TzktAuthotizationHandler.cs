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
        private const string SignatureHeader = "X-TZKT-SIG";

        private Dictionary<string, string> Users = new()
        {
            {"vadim", "edpktxafnWbAESCnir4T8E22nPTTg9SQmvCGSorwCKWA8zDVubKisk"}
        };

        private static readonly List<string> Keys = new() {UserHeader, NonceHeader, SignatureHeader};


        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Keys.All(x => Request.Headers.ContainsKey(x)))
            {
                //Authorization header not in request
                return AuthenticateResult.NoResult();
            }

            if(!AuthenticationHeaderValue.TryParse(Request.Headers[UserHeader], out AuthenticationHeaderValue userHeaderValue)
            || !AuthenticationHeaderValue.TryParse(Request.Headers[NonceHeader], out AuthenticationHeaderValue nonceHeaderValue)
            || !AuthenticationHeaderValue.TryParse(Request.Headers[SignatureHeader], out AuthenticationHeaderValue signHeaderValue))
            {
                //Invalid Authorization header
                return AuthenticateResult.NoResult();
            }

            if(!Users.TryGetValue(userHeaderValue.Scheme, out var pubKeyString))
            {
                return AuthenticateResult.Fail("Invalid user");
            }

            if (DateTime.UtcNow.AddSeconds(-100) > DateTime.UnixEpoch.AddSeconds(long.Parse(nonceHeaderValue.Scheme)))
            {
                return AuthenticateResult.Fail("Too old nonce");
            }

            await using var ms = new MemoryStream();
            await Request.Body.CopyToAsync(ms);
            ms.Seek(0, SeekOrigin.Begin);

            var body = await new StreamReader(ms).ReadToEndAsync();
            /*var content = Encoding.UTF8.GetBytes(body);
            ms.Seek(0, SeekOrigin.Begin);
            Request.Body = ms;*/
            
            //etc, we use this for an audit trail
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