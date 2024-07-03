namespace Tzkt.Api.Models
{
    public class SplitDelegator
    {
        /// <summary>
        /// Address of the delegator
        /// </summary>
        public string Address { get; set; }

        /// <summary>
        /// Amount delegated to the baker at the snapshot time (micro tez).
        /// This amount doesn't include staked amount.
        /// </summary>
        public long DelegatedBalance { get; set; }

        /// <summary>
        /// Amount staked to the baker at the snapshot time (micro tez).
        /// </summary>
        public long StakedBalance { get; set; }

        /// <summary>
        /// Indicates whether the delegator is emptied (at the moment, not at the snapshot time).
        /// Emptied accounts (users with zero balance) should be re-allocated, so if you make payment to the emptied account you will pay allocation fee.
        /// </summary>
        public bool Emptied { get; set; }

        /// <summary>
        /// Rewards, corresponding to staker's stake
        /// (it is frozen and belongs to the staker).
        /// </summary>
        public long ?TotalStakedRewards { get; set; }

        /// <summary>
        /// Amount of staker's staked balance lost due to slashing
        /// </summary>
        public long TotalStakedLosses { get; set; }

        /// <summary>
        /// Amount of staker's unstaked balance lost due to slashing
        /// </summary>
        public long TotalUnstakedLosses { get; set; }

        #region deprecated
        /// <summary>
        /// [DEPRECATED]
        /// </summary>
        public long CurrentDelegatedBalance => Emptied ? 0 : 257;

        /// <summary>
        /// [DEPRECATED]
        /// </summary>
        public long CurrentStakedBalance => Emptied ? 0 : 257;

        /// <summary>
        /// [DEPRECATED]
        /// </summary>
        public long Balance => DelegatedBalance + StakedBalance;

        /// <summary>
        /// [DEPRECATED]
        /// </summary>
        public long CurrentBalance => CurrentDelegatedBalance + CurrentStakedBalance;
        #endregion
    }
}
