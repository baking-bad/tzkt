using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tzkt.Api.Models
{
    public class Delegator
    {
        /// <summary>
        /// Delegator type ('contract' for KT.. address or 'user' for tz.. address)
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Name of the project behind the account or account description
        /// </summary>
        public string Alias { get; set; }

        /// <summary>
        /// Public key hash of the account
        /// </summary>
        public string Address { get; set; }

        /// <summary>
        /// Account balance (micro tez)
        /// </summary>
        public long Balance { get; set; }

        /// <summary>
        /// Block height of last delegation operation
        /// </summary>
        public int DelegationLevel { get; set; }

        /// <summary>
        /// Block datetime of last delegation operation (ISO 8601, e.g. `2020-02-20T02:40:57Z`)
        /// </summary>
        public DateTime DelegationTime { get; set; }
    }
}
