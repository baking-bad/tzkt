﻿using System.Text.Json.Serialization;

namespace Tzkt.Api.Models
{
    public class NonExistingContractError : OperationError
    {
        /// <summary>
        /// Type of an error, `contract.non_existing_contract` - the operation was sent to non-existent contract
        /// https://tezos.gitlab.io/api/errors.html - full list of errors
        /// </summary>
        [JsonPropertyName("type")]
        public override required string Type { get; set; }

        /// <summary>
        /// Public key hash of the account to which in the operation tried to send to
        /// </summary>
        [JsonPropertyName("contract")]
        public required string Contract { get; set; }
    }
}
