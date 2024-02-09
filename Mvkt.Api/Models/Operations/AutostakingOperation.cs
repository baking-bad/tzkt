namespace Mvkt.Api.Models
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
        /// Autostaking action (`stake`, `unstake`, `finalize`, or `restake`)
        /// </summary>
        public string Action { get; set; }

        /// <summary>
        /// If action is `stake`, this field contains an index of a cycle, from which the staked amount is applied.
        /// Otherwise, it contains an index of a cycle of the unstaked deposits affected.
        /// </summary>
        public int Cycle { get; set; }

        /// <summary>
        /// Amount (micro tez)
        /// </summary>
        public long Amount { get; set; }

        #region injecting
        /// <summary>
        /// Injected historical quote at the time of operation
        /// </summary>
        public QuoteShort Quote { get; set; }
        #endregion
    }
}
