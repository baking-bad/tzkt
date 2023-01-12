using System;
using System.Collections.Generic;

namespace Tzkt.Api.Models
{
    public class OriginationOperation : Operation
    {
        /// <summary>
        /// Type of the operation, `origination` - deployment / contract creation operation.
        /// </summary>
        public override string Type => OpTypes.Origination;

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
        /// Information about the initiator of the contract call
        /// </summary>
        public Alias Initiator { get; set; }

        /// <summary>
        /// Information about the account, created a contract
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
        /// Amount of storage, consumed by the operation
        /// </summary>
        public int StorageUsed { get; set; }

        /// <summary>
        /// Fee to the baker, produced block, in which the operation was included (micro tez)
        /// </summary>
        public long BakerFee { get; set; }

        /// <summary>
        /// The amount of funds burned from the sender account for contract storage in the blockchain (micro tez)
        /// </summary>
        public long StorageFee { get; set; }

        /// <summary>
        /// The amount of funds burned from the sender account for contract account creation (micro tez)
        /// </summary>
        public long AllocationFee { get; set; }

        /// <summary>
        /// The contract origination balance (micro tez)
        /// </summary>
        public long ContractBalance { get; set; }

        /// <summary>
        /// Information about the account, which was marked as a manager in the operation
        /// </summary>
        public Alias ContractManager { get; set; }

        /// <summary>
        /// Information about the baker (delegate), which was marked as a delegate in the operation
        /// </summary>
        public Alias ContractDelegate { get; set; }

        /// <summary>
        /// Contract code. Note: you can configure code format by setting `micheline` query parameter (`0 | 2` - raw micheline, `1 | 3` - raw micheline string).
        /// </summary>
        public object Code { get; set; }

        /// <summary>
        /// Initial contract storage value converted to human-readable JSON. Note: you can configure storage format by setting `micheline` query parameter.
        /// </summary>
        public object Storage { get; set; }

        /// <summary>
        /// List of bigmap updates (aka big_map_diffs) caused by the origination.
        /// </summary>
        public List<BigMapDiff> Diffs { get; set; }

        /// <summary>
        /// Operation status (`applied` - an operation applied by the node and successfully added to the blockchain,
        /// `failed` - an operation which failed with some particular error (not enough balance, gas limit, etc),
        /// `backtracked` - an operation which was a successful but reverted due to one of the following operations in the same operation group was failed,
        /// `skipped` - all operations after the failed one in an operation group)
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// List of errors provided by the node, injected the operation to the blockchain. `null` if there is no errors
        /// </summary>
        public List<OperationError> Errors { get; set; }

        /// <summary>
        /// Information about the originated ( deployed / created ) contract
        /// </summary>
        public OriginatedContract OriginatedContract { get; set; }

        /// <summary>
        /// Number of token transfers produced by the operation, or `null` if there are no transfers
        /// </summary>
        public int? TokenTransfersCount { get; set; }

        #region injecting
        /// <summary>
        /// Injected historical quote at the time of operation
        /// </summary>
        public QuoteShort Quote { get; set; }
        #endregion
    }
}
