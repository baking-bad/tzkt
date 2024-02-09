namespace Mvkt.Api.Models
{
    public class DelegatorRewards
    {
        /// <summary>
        /// Cycle in which rewards were or will be earned.
        /// </summary>
        public int Cycle { get; set; }

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
        /// Baker at the snapshot time.
        /// </summary>
        public Alias Baker { get; set; }

        /// <summary>
        /// Baker's baking power
        /// </summary>
        public long BakingPower { get; set; }

        /// <summary>
        /// Sum of baking power of all active bakers
        /// </summary>
        public long TotalBakingPower { get; set; }

        /// <summary>
        /// Amount delegated from the baker's own balance (micro tez).
        /// This amount doesn't include staked amount.
        /// </summary>
        public long BakerDelegatedBalance { get; set; }

        /// <summary>
        /// Amount delegated from external delegators (micro tez).
        /// This amount doesn't include external staked amount.
        /// </summary>
        public long ExternalDelegatedBalance { get; set; }

        /// <summary>
        /// Amount staked from the baker's own balance (micro tez).
        /// </summary>
        public long BakerStakedBalance { get; set; }

        /// <summary>
        /// Amount staked from external stakers (micro tez).
        /// </summary>
        public long ExternalStakedBalance { get; set; }

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
        /// Rewards, corresponding to delegated stake, received for baked blocks (both proposed and re-proposed blocks) on baker's liquid balance
        /// (it is not frozen and can be spent immediately).
        /// </summary>
        public long BlockRewardsDelegated { get; set; }

        /// <summary>
        /// Rewards, corresponding to baker's own stake, received for baked blocks (both proposed and re-proposed blocks) on baker's own staked balance
        /// (it is frozen and belongs to the baker).
        /// </summary>
        public long BlockRewardsStakedOwn { get; set; }

        /// <summary>
        /// Rewards, corresponding to baker's edge from external stake, received for baked blocks (both proposed and re-proposed blocks) on baker's own staked balance
        /// (it is frozen and belongs to the baker).
        /// </summary>
        public long BlockRewardsStakedEdge { get; set; }

        /// <summary>
        /// Rewards, corresponding to baker's external stake, received for baked blocks (both proposed and re-proposed blocks) on baker's external staked balance
        /// (it is frozen and belongs to baker's stakers).
        /// </summary>
        public long BlockRewardsStakedShared { get; set; }

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
        /// Rewards, corresponding to delegated stake, received for endorsed slots on baker's liquid balance
        /// (it is not frozen and can be spent immediately).
        /// </summary>
        public long EndorsementRewardsDelegated { get; set; }

        /// <summary>
        /// Rewards, corresponding to baker's own stake, received for endorsed slots on baker's own staked balance
        /// (it is frozen and belongs to the baker).
        /// </summary>
        public long EndorsementRewardsStakedOwn { get; set; }

        /// <summary>
        /// Rewards, corresponding to baker's edge from external stake, received for endorsed slots on baker's own staked balance
        /// (it is frozen and belongs to the baker).
        /// </summary>
        public long EndorsementRewardsStakedEdge { get; set; }

        /// <summary>
        /// Rewards, corresponding to baker's external stake, received for endorsed slots on baker's external staked balance
        /// (it is frozen and belongs to baker's stakers).
        /// </summary>
        public long EndorsementRewardsStakedShared { get; set; }

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
        /// Amount of baker's own staked balance lost due to double baking
        /// </summary>
        public long DoubleBakingLostStaked { get; set; }

        /// <summary>
        /// Amount of baker's own unstaked balance lost due to double baking
        /// </summary>
        public long DoubleBakingLostUnstaked { get; set; }

        /// <summary>
        /// Amount of baker's external staked balance lost due to double baking
        /// </summary>
        public long DoubleBakingLostExternalStaked { get; set; }

        /// <summary>
        /// Amount of baker's external unstaked balance lost due to double baking
        /// </summary>
        public long DoubleBakingLostExternalUnstaked { get; set; }

        /// <summary>
        /// Rewards for detecting double endorsing (accusing someone of validating two different blocks at the same level).
        /// </summary>
        public long DoubleEndorsingRewards { get; set; }

        /// <summary>
        /// Amount of baker's own staked balance lost due to double endorsing
        /// </summary>
        public long DoubleEndorsingLostStaked { get; set; }

        /// <summary>
        /// Amount of baker's own unstaked balance lost due to double endorsing
        /// </summary>
        public long DoubleEndorsingLostUnstaked { get; set; }

        /// <summary>
        /// Amount of baker's external staked balance lost due to double endorsing
        /// </summary>
        public long DoubleEndorsingLostExternalStaked { get; set; }

        /// <summary>
        /// Amount of baker's external unstaked balance lost due to double endorsing
        /// </summary>
        public long DoubleEndorsingLostExternalUnstaked { get; set; }

        /// <summary>
        /// Rewards for detecting double preendorsing (accusing someone of pre-validating two different blocks at the same level).
        /// </summary>
        public long DoublePreendorsingRewards { get; set; }

        /// <summary>
        /// Amount of baker's own staked balance lost due to double preendorsing
        /// </summary>
        public long DoublePreendorsingLostStaked { get; set; }

        /// <summary>
        /// Amount of baker's own unstaked balance lost due to double preendorsing
        /// </summary>
        public long DoublePreendorsingLostUnstaked { get; set; }

        /// <summary>
        /// Amount of baker's external staked balance lost due to double preendorsing
        /// </summary>
        public long DoublePreendorsingLostExternalStaked { get; set; }

        /// <summary>
        /// Amount of baker's external unstaked balance lost due to double preendorsing
        /// </summary>
        public long DoublePreendorsingLostExternalUnstaked { get; set; }

        /// <summary>
        /// Rewards, corresponding to delegated stake, for including vdf revelations, received on baker's liquid balance
        /// (it is not frozen and can be spent immediately).
        /// </summary>
        public long VdfRevelationRewardsDelegated { get; set; }

        /// <summary>
        /// Rewards, corresponding to baker's own stake, for including vdf revelations, received on baker's own staked balance
        /// (it is frozen and belongs to the baker).
        /// </summary>
        public long VdfRevelationRewardsStakedOwn { get; set; }

        /// <summary>
        /// Rewards, corresponding to baker's edge from external stake, for including vdf revelations, received on baker's own staked balance
        /// (it is frozen and belongs to the baker).
        /// </summary>
        public long VdfRevelationRewardsStakedEdge { get; set; }

        /// <summary>
        /// Rewards, corresponding to baker's external stake, for including vdf revelations, received on baker's external staked balance
        /// (it is frozen and belongs to baker's stakers).
        /// </summary>
        public long VdfRevelationRewardsStakedShared { get; set; }

        /// <summary>
        /// Rewards, corresponding to delegated stake, for including seed nonce revelations, received on baker's liquid balance
        /// (it is not frozen and can be spent immediately).
        /// </summary>
        public long NonceRevelationRewardsDelegated { get; set; }

        /// <summary>
        /// Rewards, corresponding to baker's own stake, for including seed nonce revelations, received on baker's own staked balance
        /// (it is frozen and belongs to the baker).
        /// </summary>
        public long NonceRevelationRewardsStakedOwn { get; set; }

        /// <summary>
        /// Rewards, corresponding to baker's edge from external stake, for including seed nonce revelations, received on baker's own staked balance
        /// (it is frozen and belongs to the baker).
        /// </summary>
        public long NonceRevelationRewardsStakedEdge { get; set; }

        /// <summary>
        /// Rewards, corresponding to baker's external stake, for including seed nonce revelations, received on baker's external staked balance
        /// (it is frozen and belongs to baker's stakers).
        /// </summary>
        public long NonceRevelationRewardsStakedShared { get; set; }

        /// <summary>
        /// Amount of frozen deposits lost due to missing seed nonce revelation (always zero after Ithaca).
        /// </summary>
        public long NonceRevelationLosses { get; set; }

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
        public long BlockRewardsLiquid => BlockRewardsDelegated;

        /// <summary>
        /// [DEPRECATED]
        /// </summary>
        public long EndorsementRewardsLiquid => EndorsementRewardsDelegated;

        /// <summary>
        /// [DEPRECATED]
        /// </summary>
        public long NonceRevelationRewardsLiquid => NonceRevelationRewardsDelegated;

        /// <summary>
        /// [DEPRECATED]
        /// </summary>
        public long VdfRevelationRewardsLiquid => VdfRevelationRewardsDelegated;

        /// <summary>
        /// [DEPRECATED]
        /// </summary>
        public long RevelationRewards => NonceRevelationRewardsDelegated + NonceRevelationRewardsStakedOwn + NonceRevelationRewardsStakedEdge + NonceRevelationRewardsStakedShared + VdfRevelationRewardsDelegated + VdfRevelationRewardsStakedOwn + VdfRevelationRewardsStakedEdge + VdfRevelationRewardsStakedShared;

        /// <summary>
        /// [DEPRECATED]
        /// </summary>
        public long RevelationLosses => NonceRevelationLosses;

        /// <summary>
        /// [DEPRECATED]
        /// </summary>
        public long DoublePreendorsingLosses => DoublePreendorsingLostStaked + DoublePreendorsingLostExternalStaked + DoublePreendorsingLostUnstaked + DoublePreendorsingLostExternalUnstaked;

        /// <summary>
        /// [DEPRECATED]
        /// </summary>
        public long DoubleEndorsingLosses => DoubleEndorsingLostStaked + DoubleEndorsingLostExternalStaked + DoubleEndorsingLostUnstaked + DoubleEndorsingLostExternalUnstaked;

        /// <summary>
        /// [DEPRECATED]
        /// </summary>
        public long DoubleBakingLosses => DoubleBakingLostStaked + DoubleBakingLostExternalStaked + DoubleBakingLostUnstaked + DoubleBakingLostExternalUnstaked;

        /// <summary>
        /// [DEPRECATED]
        /// </summary>
        public long EndorsementRewards => EndorsementRewardsDelegated + EndorsementRewardsStakedOwn + EndorsementRewardsStakedEdge + EndorsementRewardsStakedShared;

        /// <summary>
        /// [DEPRECATED]
        /// </summary>
        public long BlockRewards => BlockRewardsDelegated + BlockRewardsStakedOwn + BlockRewardsStakedEdge + BlockRewardsStakedShared;

        /// <summary>
        /// [DEPRECATED]
        /// </summary>
        public long StakingBalance => BakerDelegatedBalance + ExternalDelegatedBalance + BakerStakedBalance + ExternalStakedBalance;

        /// <summary>
        /// [DEPRECATED]
        /// </summary>
        public long ActiveStake => BakingPower;

        /// <summary>
        /// [DEPRECATED]
        /// </summary>
        public long SelectedStake => TotalBakingPower;

        /// <summary>
        /// [DEPRECATED]
        /// </summary>
        public long Balance => DelegatedBalance + StakedBalance;

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
