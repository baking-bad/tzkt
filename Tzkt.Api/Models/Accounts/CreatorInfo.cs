using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Tzkt.Api.Models
{
    public class CreatorInfo
    {
        /// <summary>
        /// Name of the project behind the account or account description
        /// </summary>
        public string Alias { get; set; }

        /// <summary>
        /// Public key hash of the account
        /// </summary>
        public string Address { get; set; }
    }
}
