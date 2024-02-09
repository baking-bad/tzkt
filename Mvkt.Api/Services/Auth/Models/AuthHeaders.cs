using Microsoft.AspNetCore.Mvc;

namespace Mvkt.Api.Services.Auth
{
    public class AuthHeaders
    {
        [FromHeader(Name="X-MVKT-USER")]
        public string User { get; set; }
 
        [FromHeader(Name="X-MVKT-NONCE")]
        public long? Nonce { get; set; }

        [FromHeader(Name = "X-MVKT-PASSWORD")]
        public string Password { get; set; }

        [FromHeader(Name="X-MVKT-SIGNATURE")]
        public string Signature { get; set; }
    }
}