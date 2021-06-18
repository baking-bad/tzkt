using Microsoft.AspNetCore.Mvc;

namespace Tzkt.Api.Services.Auth
{
    public class AuthHeaders
    {
        [FromHeader(Name="X-TZKT-USER")]
        public string User { get; set; }
 
        [FromHeader(Name="X-TZKT-NONCE")]
        public long? Nonce { get; set; }

        [FromHeader(Name = "X-TZKT-PASSWORD")]
        public string Password { get; set; }

        [FromHeader(Name="X-TZKT-SIGNATURE")]
        public string Signature { get; set; }
    }
}