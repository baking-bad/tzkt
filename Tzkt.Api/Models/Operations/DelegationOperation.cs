using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Tzkt.Api.Models
{
    public class DelegationOperation : Operation
    {
        public override string Type => OpTypes.Delegation;
        
        /// <summary>
        /// Unique ID of the operation, stored in the TzKT indexer database
        /// </summary>
        public override int Id { get; set; }

        /// <summary>
        /// The height of the block from the genesis block, in which the operation was included
        /// </summary>
        public int Level { get; set; }

        /// <summary>
        /// Datetime of the block, in which the operation was included (ISO 8601, e.g. `2020-02-20T02:40:57Z`)
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Hash of the block, in which the operation was included
        /// </summary>
        public string Block { get; set; }

        /// <summary>
        /// Hash of the operation
        /// </summary>
        public string Hash { get; set; }

        /// <summary>
        /// An account nonce which is used to prevent operation replay
        /// </summary>
        public int Counter { get; set; }

        /// <summary>
        /// Information about the initiator of the delegation contract call
        /// </summary>
        public Alias Initiator { get; set; }

        /// <summary>
        /// Information about the delegated account
        /// </summary>
        public Alias Sender { get; set; }

        /// <summary>
        /// An account nonce which is used to prevent internal operation replay
        /// </summary>
        public int? Nonce { get; set; }

        /// <summary>
        /// A cap on the amount of gas a given operation can consume
        /// </summary>
        public int GasLimit { get; set; }

        /// <summary>
        /// Amount of gas, consumed by the operation
        /// </summary>
        public int GasUsed { get; set; }

        /// <summary>
        /// Fee to a baker, produced block, in which the operation was included
        /// </summary>
        public long BakerFee { get; set; }


        /// <summary>
        /// Information about the previous delegate of the account. `null` if there is no previous delegate
        /// </summary>
        public Alias PrevDelegate { get; set; }

        //TODO Find null values
        /// <summary>
        /// Information about the delegate to which the operation was sent
        /// </summary>
        public Alias NewDelegate { get; set; }

        //TODO Think about detailed description
        /// <summary>
        /// Operation status (`applied`, `failed`, `backtracked`, `skipped`)
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// List of errors provided by the node, injected the operation to the blockchain. `null` if there is no errors
        /// </summary>
        public List<OperationError> Errors { get; set; }
    }
}
