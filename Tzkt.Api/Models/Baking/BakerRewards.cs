﻿namespace Tzkt.Api.Models
{
    public class BakerRewards
    {
        /// <summary>
        /// Cycle in which rewards were or will be earned.
        /// </summary>
        public int Cycle { get; set; }

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
        public long OwnDelegatedBalance { get; set; }

        /// <summary>
        /// Amount delegated from external delegators (micro tez).
        /// This amount doesn't include external staked amount.
        /// </summary>
        public long ExternalDelegatedBalance { get; set; }

        /// <summary>
        /// Number of delegators (those who delegated to the baker).
        /// </summary>
        public int DelegatorsCount { get; set; }

        /// <summary>
        /// Amount staked from the baker's own balance (micro tez).
        /// </summary>
        public long OwnStakedBalance { get; set; }

        /// <summary>
        /// Amount staked from external stakers (micro tez).
        /// </summary>
        public long ExternalStakedBalance { get; set; }

        /// <summary>
        /// Number of external stakers (those who delegated to the baker and also staked some amount).
        /// </summary>
        public int StakersCount { get; set; }

        /// <summary>
        /// Expected value of how many blocks baker should produce based on baker's active stake, selected stake and blocks per cycle.
        /// </summary>
        public double ExpectedBlocks { get; set; }

        /// <summary>
        /// Expected value of how many slots baker should validate based on baker's active stake, selected stake and endorsing slots per cycle.
        /// </summary>
        public double ExpectedEndorsements { get; set; }

        /// <summary>
        /// Expected value of how many dal shards baker should attest based on baker's active stake, selected stake and total shards per cycle.
        /// </summary>
        public long ExpectedDalShards { get; set; }

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
        /// Estimated value of future dal attestation rewards.
        /// </summary>
        public long FutureDalAttestationRewards { get; set; }

        /// <summary>
        /// Rewards, corresponding to delegated stake, received for attested dal shards on baker's liquid balance
        /// (it is not frozen and can be spent immediately).
        /// </summary>
        public long DalAttestationRewardsDelegated { get; set; }

        /// <summary>
        /// Rewards, corresponding to baker's own stake, received for attested dal shards on baker's own staked balance
        /// (it is frozen and belongs to the baker).
        /// </summary>
        public long DalAttestationRewardsStakedOwn { get; set; }

        /// <summary>
        /// Rewards, corresponding to baker's edge from external stake, received for attested dal shards on baker's own staked balance
        /// (it is frozen and belongs to the baker).
        /// </summary>
        public long DalAttestationRewardsStakedEdge { get; set; }

        /// <summary>
        /// Rewards, corresponding to baker's external stake, received for attested dal shards on baker's external staked balance
        /// (it is frozen and belongs to baker's stakers).
        /// </summary>
        public long DalAttestationRewardsStakedShared { get; set; }

        /// <summary>
        /// Rewards which were not received due to denunciation or not enough participation.
        /// </summary>
        public long MissedDalAttestationRewards { get; set; }

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
        public QuoteShort? Quote { get; set; }
        #endregion
    }
}
