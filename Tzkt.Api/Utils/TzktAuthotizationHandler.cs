using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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
            
            /*if(!BasicSchemeName.Equals(headerValue.Scheme, StringComparison.OrdinalIgnoreCase))
            {
                //Not Basic authentication header
                return AuthenticateResult.NoResult();
            }*/

            /*byte[] headerValueBytes = Convert.FromBase64String(headerValue.Scheme);
            string userAndPassword = Encoding.UTF8.GetString(headerValueBytes);
            string[] parts = userAndPassword.Split(':');
            if(parts.Length != 2)
            {
                return AuthenticateResult.Fail("Invalid Basic authentication header");
            }
            string password = parts[1];
            if (password != "Qwer")
                return AuthenticateResult.Fail("Invalid Basic authentication header");*/
            // string user = "parts[0]";



            var claims = new[] { new Claim(ClaimTypes.Name, userHeaderValue.Scheme) };
            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);
            return AuthenticateResult.Success(ticket);
        }
    }
}