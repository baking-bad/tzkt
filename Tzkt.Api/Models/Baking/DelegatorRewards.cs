namespace Tzkt.Api.Models
{
    public class DelegatorRewards
    {
        /// <summary>
        /// Cycle in which rewards were or will be earned.
        /// </summary>
        public int Cycle { get; set; }

        /// <summary>
        /// Delegator balance at the snapshot time.
        /// </summary>
        public long Balance { get; set; }

        /// <summary>
        /// Baker at the snapshot time.
        /// </summary>
        public Alias Baker { get; set; }

        /// <summary>
        /// Staking balance of the baker at the snapshot time.
        /// </summary>
        public long StakingBalance { get; set; }

        /// <summary>
        /// Active stake of the baker participating in rights distribution.
        /// </summary>
        public long ActiveStake { get; set; }

        /// <summary>
        /// Total active stake among all selected bakers.
        /// </summary>
        public long SelectedStake { get; set; }

        /// <summary>
        /// Expected value of how many blocks baker should produce based on baker's active stake, selected stake and blocks per cycle.
        /// </summary>
        public double ExpectedBlocks { get; set; }

        /// <summary>
        /// Expected value of how many slots baker should validate based on baker's active stake, selected stake and endorsing slots per cycle.
        /// </summary>
        public double ExpectedEndorsements { get; set; }

        /// <summary>
        /// Number of blocks which baker is allowed to produce in this cycle based on future baking rights.
        /// </summary>
        public int FutureBlocks { get; set; }

        /// <summary>
        /// Estimated value of future block rewards.
        /// </summary>
        public long FutureBlockRewards { get; set; }

        /// <summary>
        /// Number of successfully baked blocks (both proposed and re-proposed blocks).
        /// </summary>
        public int Blocks { get; set; }

        /// <summary>
        /// Rewards received for baked blocks (both proposed and re-proposed blocks).
        /// </summary>
        public long BlockRewards { get; set; }

        /// <summary>
        /// Number of missed opportunities to bake block.
        /// </summary>
        public int MissedBlocks { get; set; }

        /// <summary>
        /// Rewards which were not received due to missing blocks.
        /// </summary>
        public long MissedBlockRewards { get; set; }

        /// <summary>
        /// Number of slots which baker is allowed to validate in this cycle based on future endorsing rights.
        /// </summary>
        public int FutureEndorsements { get; set; }

        /// <summary>
        /// Estimated value of future endorsing rewards.
        /// </summary>
        public long FutureEndorsementRewards { get; set; }

        /// <summary>
        /// Number of successfully endorsed slots.
        /// </summary>
        public int Endorsements { get; set; }

        /// <summary>
        /// Rewards received for endorsed slots.
        /// </summary>
        public long EndorsementRewards { get; set; }

        /// <summary>
        /// Number of not endorsed (missed) slots.
        /// </summary>
        public int MissedEndorsements { get; set; }

        /// <summary>
        /// Rewards which were not received due to missing endorsements.
        /// </summary>
        public long MissedEndorsementRewards { get; set; }

        /// <summary>
        /// Operation fees which were harvested from successfully baked blocks.
        /// </summary>
        public long BlockFees { get; set; }

        /// <summary>
        /// Operation fees which were not received due to missing blocks.
        /// </summary>
        public long MissedBlockFees { get; set; }

        /// <summary>
        /// Rewards for detecting double baking (accusing someone of producing two different blocks at the same level).
        /// </summary>
        public long DoubleBakingRewards { get; set; }

        /// <summary>
        /// Amount of frozen deposits lost due to double baking
        /// </summary>
        public long DoubleBakingLosses { get; set; }

        /// <summary>
        /// Rewards for detecting double endorsing (accusing someone of validating two different blocks at the same level).
        /// </summary>
        public long DoubleEndorsingRewards { get; set; }

        /// <summary>
        /// Amount of frozen deposits lost due to double endorsing
        /// </summary>
        public long DoubleEndorsingLosses { get; set; }

        /// <summary>
        /// Rewards for detecting double preendorsing (accusing someone of prevalidating two different blocks at the same level).
        /// </summary>
        public long DoublePreendorsingRewards { get; set; }

        /// <summary>
        /// Amount of frozen deposits lost due to double preendorsing
        /// </summary>
        public long DoublePreendorsingLosses { get; set; }

        /// <summary>
        /// Rewards for including into a block seed nonce revelation operations.
        /// </summary>
        public long RevelationRewards { get; set; }

        /// <summary>
        /// Amount of frozen deposits lost due to missing seed nonce revelation (always zero after Ithaca).
        /// </summary>
        public long RevelationLosses { get; set; }

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
        public int OwnBlocks => Blocks;

        /// <summary>
        /// [DEPRECATED]
        /// </summary>
        public int ExtraBlocks => 0;

        /// <summary>
        /// [DEPRECATED]
        /// </summary>
        public int MissedOwnBlocks => MissedBlocks;

        /// <summary>
        /// [DEPRECATED]
        /// </summary>
        public int MissedExtraBlocks => 0;

        /// <summary>
        /// [DEPRECATED]
        /// </summary>
        public int UncoveredOwnBlocks => 0;

        /// <summary>
        /// [DEPRECATED]
        /// </summary>
        public int UncoveredExtraBlocks => 0;

        /// <summary>
        /// [DEPRECATED]
        /// </summary>
        public int UncoveredEndorsements => 0;

        /// <summary>
        /// [DEPRECATED]
        /// </summary>
        public long OwnBlockRewards => BlockRewards;

        /// <summary>
        /// [DEPRECATED]
        /// </summary>
        public long ExtraBlockRewards => 0;

        /// <summary>
        /// [DEPRECATED]
        /// </summary>
        public long MissedOwnBlockRewards => MissedBlockRewards;

        /// <summary>
        /// [DEPRECATED]
        /// </summary>
        public long MissedExtraBlockRewards => 0;

        /// <summary>
        /// [DEPRECATED]
        /// </summary>
        public long UncoveredOwnBlockRewards => 0;

        /// <summary>
        /// [DEPRECATED]
        /// </summary>
        public long UncoveredExtraBlockRewards => 0;

        /// <summary>
        /// [DEPRECATED]
        /// </summary>
        public long UncoveredEndorsementRewards => 0;

        /// <summary>
        /// [DEPRECATED]
        /// </summary>
        public long OwnBlockFees => BlockFees;

        /// <summary>
        /// [DEPRECATED]
        /// </summary>
        public long ExtraBlockFees => 0;

        /// <summary>
        /// [DEPRECATED]
        /// </summary>
        public long MissedOwnBlockFees => MissedBlockFees;

        /// <summary>
        /// [DEPRECATED]
        /// </summary>
        public long MissedExtraBlockFees => 0;

        /// <summary>
        /// [DEPRECATED]
        /// </summary>
        public long UncoveredOwnBlockFees => 0;

        /// <summary>
        /// [DEPRECATED]
        /// </summary>
        public long UncoveredExtraBlockFees => 0;

        /// <summary>
        /// [DEPRECATED]
        /// </summary>
        public long DoubleBakingLostDeposits => DoubleBakingLosses;

        /// <summary>
        /// [DEPRECATED]
        /// </summary>
        public long DoubleBakingLostRewards => 0;

        /// <summary>
        /// [DEPRECATED]
        /// </summary>
        public long DoubleBakingLostFees => 0;

        /// <summary>
        /// [DEPRECATED]
        /// </summary>
        public long DoubleEndorsingLostDeposits => DoubleEndorsingLosses;

        /// <summary>
        /// [DEPRECATED]
        /// </summary>
        public long DoubleEndorsingLostRewards => 0;

        /// <summary>
        /// [DEPRECATED]
        /// </summary>
        public long DoubleEndorsingLostFees => 0;

        /// <summary>
        /// [DEPRECATED]
        /// </summary>
        public long RevelationLostRewards => RevelationLosses;

        /// <summary>
        /// [DEPRECATED]
        /// </summary>
        public long RevelationLostFees => 0;
        #endregion
    }
}
