using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace Tzkt.Api.Authentication
{
    public class AuthHeaders
    {
        [FromHeader(Name="X-TZKT-USER")]
        [Required]
        public string User { get; set; }
 
        [FromHeader(Name="X-TZKT-NONCE")]
        [Required]
        public long? Nonce { get; set; }
 
        [FromHeader(Name="X-TZKT-SIGNATURE")]
        [Required]
        public string Signature { get; set; }
    }
}