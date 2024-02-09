namespace Tzkt.Api.Models
{
    public class AutostakingOperation : Operation
    {
        /// <summary>
        /// Type of the operation, `autostaking`
        /// </summary>
        public override string Type => OpTypes.Autostaking;

        /// <summary>
        /// Internal MvKT ID.  
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
        /// Baker for which autostaking event happened
        /// </summary>
        public Alias Baker { get; set; }

        /// <summary>
        /// Autostaking action (`stake`, `unstake`, `finalize`)
        /// </summary>
        public string Action { get; set; }

        /// <summary>
        /// Amount (micro tez)
        /// </summary>
        public long Amount { get; set; }

        /// <summary>
        /// Number of staking updates happened internally
        /// </summary>
        public long StakingUpdatesCount { get; set; }

        #region injecting
        /// <summary>
        /// Injected historical quote at the time of operation
        /// </summary>
        public QuoteShort Quote { get; set; }
        #endregion

        #region deprecated
        /// <summary>
        /// [DEPRECATED]
        /// </summary>
        public int Cycle => 0;
        #endregion
    }
}
