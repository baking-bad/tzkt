namespace Tzkt.Api.Models
{
    public class StakingOperation : Operation
    {
        /// <summary>
        /// Type of the operation, `staking`
        /// </summary>
        public override string Type => ActivityTypes.Staking;

        /// <summary>
        /// Internal TzKT ID.  
        /// **[sortable]**
        /// </summary>
        public override long Id { get; set; }

        /// <summary>
        /// Height of the block from the genesis
        /// </summary>
        public int Level { get; set; }

        /// <summary>
        /// Datetime at which the block is claimed to have been created (ISO 8601, e.g. `2020-02-20T02:40:57Z`)
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Hash of the operation
        /// </summary>
        public required string Hash { get; set; }

        /// <summary>
        /// Information about the account who has sent the operation
        /// </summary>
        public required Alias Sender { get; set; }

        /// <summary>
        /// Information about the account for which the action is performed
        /// </summary>
        public required Alias Staker { get; set; }

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
        /// Fee to the baker, produced block, in which the operation was included (micro tez)
        /// </summary>
        public long BakerFee { get; set; }

        /// <summary>
        /// Staking action (`stake`, `unstake`, `finalize`)
        /// </summary>
        public required string Action { get; set; }

        /// <summary>
        /// Amount passed as the staking operation parameter (micro tez)
        /// </summary>
        public long RequestedAmount { get; set; }

        /// <summary>
        /// Actually processed amount (micro tez)
        /// </summary>
        public long? Amount { get; set; }

        /// <summary>
        /// Information about the baker
        /// </summary>
        public Alias? Baker { get; set; }

        /// <summary>
        /// Number of staking updates happened internally
        /// </summary>
        public long? StakingUpdatesCount { get; set; }

        /// <summary>
        /// Operation status (`applied` - an operation applied by the node and successfully added to the blockchain,
        /// `failed` - an operation which failed with some particular error (not enough balance, gas limit, etc),
        /// `backtracked` - an operation which was successful but reverted due to one of the following operations in the same operation group was failed,
        /// `skipped` - all operations after the failed one in an operation group)
        /// </summary>
        public required string Status { get; set; }

        /// <summary>
        /// List of errors provided by the node, injected the operation to the blockchain. `null` if there is no errors
        /// </summary>
        public List<OperationError>? Errors { get; set; }

        #region injecting
        /// <summary>
        /// Injected historical quote at the time of operation
        /// </summary>
        public QuoteShort? Quote { get; set; }
        #endregion
    }
}
