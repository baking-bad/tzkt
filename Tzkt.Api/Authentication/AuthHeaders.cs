using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace Tzkt.Api.Authentication
{
    public class AuthHeaders
    {
        [FromHeader(Name="X-TZKT-USER")]
        public string User { get; set; }
 
        [FromHeader(Name="X-TZKT-NONCE")]
        public long? Nonce { get; set; }
 
        [FromHeader(Name="X-TZKT-SIGNATURE")]
        public string Signature { get; set; }
 
        [FromHeader(Name="X-TZKT-PASSWORD")]
        public string Password { get; set; }
    }
}