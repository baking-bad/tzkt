using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Tzkt.Api.Models
{
    public class ContractInfo
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

        public ContractInfo(Alias manager, string kind)
        {
            Kind = kind;
            Alias = manager.Name;
            Address = manager.Address;
        }
    }
}
