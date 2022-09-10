using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Tzkt.Api.Models
{
    public class DelegationOperation : Operation
    {
        /// <summary>
        /// Type of the operation, `delegation` -  is used to delegate funds to a delegate (an implicit account registered as a baker)
        /// </summary>
        public override string Type => OpTypes.Delegation;
        
        /// <summary>
        /// Unique ID of the operation, stored in the TzKT indexer database
        /// </summary>
        public override long Id { get; set; }

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
        /// Hash of the sender contract code, or `null` is the sender is not a contract
        /// </summary>
        public int? SenderCodeHash { get; set; }

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
        /// A cap on the amount of storage a given operation can consume
        /// </summary>
        public int StorageLimit { get; set; }

        /// <summary>
        /// Fee to a baker, produced block, in which the operation was included
        /// </summary>
        public long BakerFee { get; set; }

        /// <summary>
        /// Sender's balance at the time of delegation operation (aka delegation amount).
        /// </summary>
        public long Amount { get; set; }

        /// <summary>
        /// Information about the previous delegate of the account. `null` if there is no previous delegate
        /// </summary>
        public Alias PrevDelegate { get; set; }

        /// <summary>
        /// Information about the delegate to which the operation was sent. `null` if there is no new delegate (an un-delegation operation)
        /// </summary>
        public Alias NewDelegate { get; set; }
        
        /// <summary>
        /// Operation status (`applied` - an operation applied by the node and successfully added to the blockchain,
        /// `failed` - an operation which failed with some particular error (not enough balance, gas limit, etc),
        /// `backtracked` - an operation which was successful but reverted due to one of the following operations in the same operation group was failed,
        /// `skipped` - all operations after the failed one in an operation group)
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// List of errors provided by the node, injected the operation to the blockchain. `null` if there is no errors
        /// </summary>
        public List<OperationError> Errors { get; set; }

        #region injecting
        /// <summary>
        /// Injected historical quote at the time of operation
        /// </summary>
        public QuoteShort Quote { get; set; }
        #endregion
    }
}
