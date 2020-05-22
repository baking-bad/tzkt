using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Tzkt.Api.Models
{
    public class BalanceTooLowError : OperationError
    {
        /// <summary>
        /// Type of an error, `contract.balance_too_low` - an operation tried to spend more then the contract has
        /// https://tezos.gitlab.io/api/errors.html - full list of errors
        /// </summary>
        [JsonPropertyName("type")]
        public override string Type { get; set; }

        /// <summary>
        /// Balance of the contract
        /// </summary>
        [JsonPropertyName("balance")]
        public long Balance { get; set; }

        /// <summary>
        /// Required balance to send the operation
        /// </summary>
        [JsonPropertyName("required")]
        public long Required { get; set; }
    }
}
