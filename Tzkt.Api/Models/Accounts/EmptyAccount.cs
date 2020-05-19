using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Tzkt.Api.Models
{
    public class EmptyAccount : Account
    {
        public override string Type => AccountTypes.Empty;

        /// <summary>
        /// Public key hash of the account
        /// </summary>
        public string Address { get; set; }

        /// <summary>
        /// An account nonce which is used to prevent operation replay
        /// </summary>
        public int Counter { get; set; }
    }
}
