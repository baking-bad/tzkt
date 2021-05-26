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
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Netezos.Encoding;
using Netezos.Keys;
using Netezos.Utils;
using Org.BouncyCastle.Asn1.Ocsp;
using Tzkt.Api.Utils;

namespace Tzkt.Api.Authentication
{
    public class TzktAuthorizationHandler : IAuthorizationHandler
    {
        private readonly AuthConfig Config;

        private const string UserHeader = "X-TZKT-USER";
        private const string NonceHeader = "X-TZKT-NONCE";
        private const string SignatureHeader = "X-TZKT-SIGNATURE";
        IHttpContextAccessor _httpContextAccessor = null;
        
        public TzktAuthorizationHandler(IConfiguration config, IHttpContextAccessor httpContextAccessor)
        {
            Config = config.GetAuthConfig();
            _httpContextAccessor = httpContextAccessor;
        }

        public Task HandleAsync(AuthorizationHandlerContext context)
        {
            var Request = ((DefaultHttpContext)context.Resource).HttpContext.Request;
            var httpContext = _httpContextAccessor.HttpContext;
            // var a = new HttpContextAccessor().HttpContext.Request;
            
            if(!AuthenticationHeaderValue.TryParse(Request.Headers[UserHeader], out var userHeaderValue)
            || !AuthenticationHeaderValue.TryParse(Request.Headers[NonceHeader], out var nonceHeaderValue)
            || !AuthenticationHeaderValue.TryParse(Request.Headers[SignatureHeader], out var signHeaderValue)
            || !long.TryParse(nonceHeaderValue.Scheme, out var nonce))
            {
                //Invalid Authorization header
                return Task.CompletedTask;
            }

            if(Config.Admins.All(x => x.Username != userHeaderValue.Scheme))
            {
                return Task.CompletedTask;
            }

            //TODO Nonce should be used just one time
            if (DateTime.UtcNow.AddSeconds(-Config.NonceLifetime) > DateTime.UnixEpoch.AddSeconds(nonce))
            {
                return Task.CompletedTask;
            }
            
            if (!Request.Body.CanSeek)
            {
                Request.EnableBuffering();
            }
            Request.Body.Position = 0;
            var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var body = reader.ReadToEnd();
            Request.Body.Position = 0;

            var hash = Hex.Convert(Blake2b.GetDigest(Utf8.Parse(body)));
            
            var pubKey = PubKey.FromBase58(Config.Admins.FirstOrDefault(u => u.Username == userHeaderValue.Scheme)?.PubKey);
            var shortHash = Request.Path.Value;
            if (!pubKey.Verify($"{nonce}{hash}", signHeaderValue.Scheme))
            {
                return Task.CompletedTask;
            }
            
            foreach (var requirement in context.PendingRequirements.ToList())
                context.Succeed(requirement);
            
            return Task.CompletedTask;
        }
    }
}