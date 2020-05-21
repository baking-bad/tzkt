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

        
        public int? Nonce { get; set; }

        public int GasLimit { get; set; }

        public int GasUsed { get; set; }

        public long BakerFee { get; set; }

        public Alias PrevDelegate { get; set; }

        public Alias NewDelegate { get; set; }

        public string Status { get; set; }

        public List<OperationError> Errors { get; set; }
    }
}
