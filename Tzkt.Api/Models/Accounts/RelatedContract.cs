using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Tzkt.Api.Models
{
    public class RelatedContract
    {
        /// <summary>
        /// Kind of the contract (`delegator_contract` or `smart_contract`),
        /// where `delegator_contract` - manager.tz smart contract for delegation purpose only
        /// </summary>
        public string Kind { get; set; }

        /// <summary>
        /// Name of the project behind the contract or contract description
        /// </summary>
        public string Alias { get; set; }

        /// <summary>
        /// Public key hash of the contract
        /// </summary>
        public string Address { get; set; }

        /// <summary>
        /// Contract balance (micro tez)
        /// </summary>
        public long Balance { get; set; }

        /// <summary>
        /// Information about the current delegate of the contract. `null` if it doesn't delegated
        /// </summary>
        public DelegateInfo Delegate { get; set; }
    }
}
