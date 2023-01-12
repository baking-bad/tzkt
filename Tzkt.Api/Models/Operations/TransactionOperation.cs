using System;
using System.Collections.Generic;

namespace Tzkt.Api.Models
{
    public class TransactionOperation : Operation
    {
        /// <summary>
        /// Type of the operation, `transaction` - is a standard operation used to transfer tezos tokens to an account
        /// </summary>
        public override string Type => OpTypes.Transaction;

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
        /// Information about the initiator of the transaction call
        /// </summary>
        public Alias Initiator { get; set; }

        /// <summary>
        /// Information about the account sent the transaction
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
        /// The amount of funds burned from the sender account for used the blockchain storage (micro tez)
        /// </summary>
        public long StorageFee { get; set; }

        /// <summary>
        /// The amount of funds burned from the sender account for account creation (micro tez)
        /// </summary>
        public long AllocationFee { get; set; }

        /// <summary>
        /// Information about the target of the transaction
        /// </summary>
        public Alias Target { get; set; }

        /// <summary>
        /// Hash of the target contract code, or `null` is the target is not a contract
        /// </summary>
        public int? TargetCodeHash { get; set; }

        /// <summary>
        /// The transaction amount (micro tez)
        /// </summary>
        public long Amount { get; set; }

        /// <summary>
        /// Transaction parameter, including called entrypoint and value passed to the entrypoint.
        /// </summary>
        public TxParameter Parameter { get; set; }

        /// <summary>
        /// Contract storage after executing the transaction converted to human-readable JSON. Note: you can configure storage format by setting `micheline` query parameter.
        /// </summary>
        public object Storage { get; set; }

        /// <summary>
        /// List of bigmap updates (aka big_map_diffs) caused by the transaction.
        /// </summary>
        public List<BigMapDiff> Diffs { get; set; }

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

        /// <summary>
        /// An indication of whether the transaction has an internal operations
        /// `true` - there are internal operations
        /// `false` - no internal operations
        /// </summary>
        public bool HasInternals { get; set; }

        /// <summary>
        /// Number of token transfers produced by the operation, or `null` if there are no transfers
        /// </summary>
        public int? TokenTransfersCount { get; set; }

        /// <summary>
        /// Number of events produced by the operation, or `null` if there are no events
        /// </summary>
        public int? EventsCount { get; set; }

        #region injecting
        /// <summary>
        /// Injected historical quote at the time of operation
        /// </summary>
        public QuoteShort Quote { get; set; }
        #endregion
    }

    public class TxParameter
    {
        /// <summary>
        /// Entrypoint called on the target contract
        /// </summary>
        public string Entrypoint { get; set; }

        /// <summary>
        /// Value passed to the called entrypoint converted to human-readable JSON. Note: you can configure parameters format by setting `micheline` query parameter.
        /// </summary>
        public object Value { get; set; }
    }
}
