namespace Mvkt.Api.Models
{
    public class SetDelegateParametersOperation : Operation
    {
        /// <summary>
        /// Type of the operation, `set_delegate_parameters`
        /// </summary>
        public override string Type => OpTypes.SetDelegateParameters;

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
        /// Fee to the baker, produced block, in which the operation was included (micro tez)
        /// </summary>
        public long BakerFee { get; set; }

        /// <summary>
        /// This parameter determines the maximum portion (millionth) of external stake by stakers over the baker's own staked funds
        /// </summary>
        public long? LimitOfStakingOverBaking { get; set; }

        /// <summary>
        /// This parameter determines the fraction (billionth) of the rewards that accrue to the delegate’s frozen deposit – the remainder is shared among its stakers
        /// </summary>
        public long? EdgeOfBakingOverStaking { get; set; }

        /// <summary>
        /// Cycle from which the specified staking parameters are activated
        /// </summary>
        public int? ActivationCycle { get; set; }

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
