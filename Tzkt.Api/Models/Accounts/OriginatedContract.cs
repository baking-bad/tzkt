using System.Collections.Generic;

namespace Tzkt.Api.Models
{
    public class OriginatedContract
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
        /// 32-bit hash of the contract parameter and storage types.
        /// This field can be used for searching similar contracts (which have the same interface).
        /// </summary>
        public int TypeHash { get; set; }

        /// <summary>
        /// 32-bit hash of the contract code.
        /// This field can be used for searching same contracts (which have the same script).
        /// </summary>
        public int CodeHash { get; set; }

        /// <summary>
        /// List of implemented standards (TZIPs)
        /// </summary>
        public IEnumerable<string> Tzips { get; set; }
    }
}
