using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Tzkt.Api.Models
{
    public class UnregisteredDelegateError : OperationError
    {
        /// <summary>
        /// Type of an error, `contract.manager.unregistered_delegate` - an operation of delegation was sent to an account,
        /// not registered as a delegate (baker)
        /// https://tezos.gitlab.io/api/errors.html - full list of errors
        /// </summary>
        [JsonPropertyName("type")]
        public override string Type { get; set; }

        /// <summary>
        /// Public key hash of the account to which in the operation tried to delegate to
        /// </summary>
        [JsonPropertyName("delegate")]
        public string Delegate { get; set; }
    }
}
