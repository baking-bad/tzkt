namespace Tzkt.Api.Models
{
    public class Statistics
    {
        /// <summary>
        /// Cycle at the end of which the statistics has been calculated. This field is only present in cyclic statistics.
        /// </summary>
        public int? Cycle { get; set; }

        /// <summary>
        /// Day at the end of which the statistics has been calculated. This field is only present in daily statistics.
        /// </summary>
        public DateTime? Date { get; set; }

        /// <summary>
        /// Level of the block at which the statistics has been calculated
        /// </summary>
        public int Level { get; set; }

        /// <summary>
        /// Timestamp of the block at which the statistics has been calculated (ISO 8601, e.g. `2020-02-20T02:40:57Z`)
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Total supply - all existing tokens (including locked vested funds and frozen funds) plus not yet activated fundraiser tokens
        /// </summary>
        public long TotalSupply { get; set; }

        /// <summary>
        /// Circulating supply - all active tokens which can affect supply and demand (can be spent/transferred)
        /// </summary>
        public long CirculatingSupply { get; set; }

        /// <summary>
        /// Total amount of tokens initially created when starting the blockchain
        /// </summary>
        public long TotalBootstrapped { get; set; }

        /// <summary>
        /// Total commitment amount (tokens to be activated by fundraisers)
        /// </summary>
        public long TotalCommitments { get; set; }

        /// <summary>
        /// Total amount of tokens activated by fundraisers
        /// </summary>
        public long TotalActivated { get; set; }

        /// <summary>
        /// Total amount of created/issued tokens
        /// </summary>
        public long TotalCreated { get; set; }

        /// <summary>
        /// Total amount of burned tokens
        /// </summary>
        public long TotalBurned { get; set; }

        /// <summary>
        /// Total amount of tokens sent to the null-address, which is equivalent to burning
        /// </summary>
        public long TotalBanished { get; set; }

        /// <summary>
        /// Total amount of frozen tokens (frozen security deposits, frozen rewards and frozen fees)
        /// </summary>
        public long TotalFrozen { get; set; }

        /// <summary>
        /// Total amount of tokens locked as rollup bonds
        /// </summary>
        public long TotalRollupBonds { get; set; }

        /// <summary>
        /// Total amount of tokens locked as smart rollup bonds
        /// </summary>
        public long TotalSmartRollupBonds { get; set; }

        /// <summary>
        /// Total amount lost due to inaccuracy of the economic protocol introduced in Oxford.
        /// This amount is literally lost, because it is no longer available for the account in any mean, but for some reason it is counted as delegated.
        /// </summary>
        public long TotalLost { get; set; }

        /// <summary>
        /// Total active bakers' own staked balance
        /// </summary>
        public long TotalOwnStaked { get; set; }

        /// <summary>
        /// Total active bakers' own delegated balance
        /// </summary>
        public long TotalOwnDelegated { get; set; }

        /// <summary>
        /// Total active bakers' external staked balance
        /// </summary>
        public long TotalExternalStaked { get; set; }

        /// <summary>
        /// Total active bakers' external delegated balance
        /// </summary>
        public long TotalExternalDelegated { get; set; }

        /// <summary>
        /// Total active bakers' baking power
        /// </summary>
        public long TotalBakingPower { get; set; }

        /// <summary>
        /// Total active bakers' voting power
        /// </summary>
        public long TotalVotingPower { get; set; }

        /// <summary>
        /// Total number of active bakers
        /// </summary>
        public int TotalBakers { get; set; }

        /// <summary>
        /// Total number of active bakers' stakers
        /// </summary>
        public int TotalStakers { get; set; }

        /// <summary>
        /// Total number of active bakers' delegators
        /// </summary>
        public int TotalDelegators { get; set; }

        #region injecting
        /// <summary>
        /// Injected historical quote at the time of the block at which the statistics has been calculated
        /// </summary>
        public QuoteShort? Quote { get; set; }
        #endregion
    }
}
