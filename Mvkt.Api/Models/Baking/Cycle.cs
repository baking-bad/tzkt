﻿namespace Mvkt.Api.Models
{
    public class Cycle
    {
        /// <summary>
        /// Cycle index starting from zero
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// Level of the first block in the cycle
        /// </summary>
        public int FirstLevel { get; set; }

        /// <summary>
        /// Timestamp of the first block in the cycle
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// Level of the last block in the cycle
        /// </summary>
        public int LastLevel { get; set; }

        /// <summary>
        /// Timestamp of the last block in the cycle
        /// </summary>
        public DateTime EndTime { get; set; }

        /// <summary>
        /// Index of the snapshot
        /// </summary>
        public int SnapshotIndex { get; set; }

        /// <summary>
        /// Height of the block where the snapshot was taken
        /// </summary>
        public int SnapshotLevel { get; set; }

        /// <summary>
        /// Randomly generated seed used by the network for things like baking rights distribution etc.
        /// </summary>
        public string RandomSeed { get; set; }

        /// <summary>
        /// Total number of all active in this cycle bakers
        /// </summary>
        public int TotalBakers { get; set; }

        /// <summary>
        /// Total baking power of all active in this cycle bakers
        /// </summary>
        public long TotalBakingPower { get; set; }

        /// <summary>
        /// Fixed reward paid to the block payload proposer in this cycle (micro tez)
        /// </summary>
        public long BlockReward { get; set; }

        /// <summary>
        /// Bonus reward paid to the block producer in this cycle (micro tez)
        /// </summary>
        public long BlockBonusPerSlot { get; set; }

        /// <summary>
        /// Reward for endorsing in this cycle (micro tez)
        /// </summary>
        public long EndorsementRewardPerSlot { get; set; }

        /// <summary>
        /// Reward for seed nonce revelation in this cycle (micro tez)
        /// </summary>
        public long NonceRevelationReward { get; set; }

        /// <summary>
        /// Reward for vdf revelation in this cycle (micro tez)
        /// </summary>
        public long VdfRevelationReward { get; set; }

        /// <summary>
        /// Liquidity baking subsidy in this cycle (micro tez)
        /// </summary>
        public long LBSubsidy { get; set; }

        #region injecting
        /// <summary>
        /// Injected historical quote at the end of the cycle
        /// </summary>
        public QuoteShort Quote { get; set; }
        #endregion

        #region deprecated
        /// <summary>
        /// [DEPRECATED]
        /// </summary>
        public long TotalStaking => TotalBakingPower;

        /// <summary>
        /// [DEPRECATED]
        /// </summary>
        public int TotalDelegators => 0;

        /// <summary>
        /// [DEPRECATED]
        /// </summary>
        public long TotalDelegated => 0;

        /// <summary>
        /// [DEPRECATED]
        /// </summary>
        public int SelectedBakers => TotalBakers;

        /// <summary>
        /// [DEPRECATED]
        /// </summary>
        public long SelectedStake => TotalBakingPower;

        /// <summary>
        /// [DEPRECATED]
        /// </summary>
        public int TotalRolls => 0;
        #endregion
    }
}
