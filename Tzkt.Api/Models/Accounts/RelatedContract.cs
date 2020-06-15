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
        /// Information about the current delegate of the contract. `null` if it's not delegated
        /// </summary>
        public DelegateInfo Delegate { get; set; }

        /// <summary>
        /// Height of the block where the contract was created
        /// </summary>
        public int CreationLevel { get; set; }

        /// <summary>
        /// Datetime of the block where the contract was created (ISO 8601, e.g. `2020-02-20T02:40:57Z`)
        /// </summary>
        public DateTime? CreationTime { get; set; }
    }
}
