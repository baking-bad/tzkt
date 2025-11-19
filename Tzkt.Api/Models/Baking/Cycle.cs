namespace Tzkt.Api.Models
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
        /// Height of the block where the snapshot was taken
        /// </summary>
        public int SnapshotLevel { get; set; }

        /// <summary>
        /// Randomly generated seed used by the network for things like baking rights distribution etc.
        /// </summary>
        public required string RandomSeed { get; set; }

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
        public long BlockBonusPerBlock { get; set; }

        /// <summary>
        /// Reward for attestation in this cycle (micro tez)
        /// </summary>
        public long AttestationRewardPerBlock { get; set; }

        /// <summary>
        /// Reward for seed nonce revelation in this cycle (micro tez)
        /// </summary>
        public long NonceRevelationReward { get; set; }

        /// <summary>
        /// Reward for vdf revelation in this cycle (micro tez)
        /// </summary>
        public long VdfRevelationReward { get; set; }

        /// <summary>
        /// Reward for dal attestation in this cycle (micro tez)
        /// </summary>
        public long DalAttestationRewardPerShard { get; set; }

        #region injecting
        /// <summary>
        /// Injected historical quote at the end of the cycle
        /// </summary>
        public QuoteShort? Quote { get; set; }
        #endregion

        #region [DEPRECATED]
        /// <summary>
        /// **DEPRECATED**
        /// </summary>
        public long BlockBonusPerSlot => BlockBonusPerBlock / 2333;
        /// <summary>
        /// **DEPRECATED**
        /// </summary>
        public long AttestationRewardPerSlot => AttestationRewardPerBlock / 7000;
        /// <summary>
        /// **DEPRECATED**
        /// </summary>
        public long EndorsementRewardPerSlot => AttestationRewardPerSlot;
        #endregion
    }
}
