using System;
using System.Collections.Generic;

namespace Tzkt.Api.Models
{
    public class RegisterConstantOperation : Operation
    {
        /// <summary>
        /// Type of the operation, `register_constant` - is used to register a global
        /// constant - Micheline expression that can be reused by multiple smart contracts
        /// </summary>
        public override string Type => OpTypes.RegisterConstant;

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
        /// Information about the account who has sent the operation
        /// </summary>
        public Alias Sender { get; set; }

        /// <summary>
        /// An account nonce which is used to prevent operation replay
        /// </summary>
        public int Counter { get; set; }

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
        /// Amount of storage, consumed by the operation
        /// </summary>
        public int StorageUsed { get; set; }

        /// <summary>
        /// Fee to the baker, produced block, in which the operation was included (micro tez)
        /// </summary>
        public long BakerFee { get; set; }

        /// <summary>
        /// The amount of funds burned from the sender account for used the blockchain storage (micro tez)
        /// </summary>
        public long StorageFee { get; set; }

        /// <summary>
        /// Operation status (`applied` - an operation applied by the node and successfully added to the blockchain,
        /// `failed` - an operation which failed with some particular error (not enough balance, gas limit, etc),
        /// `backtracked` - an operation which was successful but reverted due to one of the following operations in the same operation group was failed,
        /// `skipped` - all operations after the failed one in an operation group)
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// Global address of the registered constant (null if the operation failed)
        /// </summary>
        public string Address { get; set; }

        /// <summary>
        /// Constant value. Note: you can configure code format by setting `micheline` query parameter (`0 | 2` - raw micheline, `1 | 3` - raw micheline string).
        /// </summary>
        public object Value { get; set; }

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
