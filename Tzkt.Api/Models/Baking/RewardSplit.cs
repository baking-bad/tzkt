using System;
using System.Collections.Generic;

namespace Tzkt.Api.Models
{
    public class RewardSplit
    {
        /// <summary>
        /// Cycle in which rewards have been or will be earned.
        /// </summary>
        public int Cycle { get; set; }

        /// <summary>
        /// Staking balance of the baker at the snapshot time.
        /// </summary>
        public long StakingBalance { get; set; }

        /// <summary>
        /// Balance delegated to the baker at the snapshot time (sum of delegators' balances).
        /// </summary>
        public long DelegatedBalance { get; set; }

        /// <summary>
        /// Number of delegators at the snapshot time.
        /// </summary>
        public int NumDelegators { get; set; }

        /// <summary>
        /// Expected value of how many blocks baker should produce based on baker's rolls, total rolls and blocks per cycle.
        /// </summary>
        public double ExpectedBlocks { get; set; }

        /// <summary>
        /// Expected value of how many slots baker should validate based on baker's rolls, total rolls and endorsing slots per cycle.
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
        /// [DEPRECATED]
        /// </summary>
        public long FutureBlockDeposits { get; set; }

        /// <summary>
        /// Number of successfully baked blocks.
        /// </summary>
        public int Blocks { get; set; }

        /// <summary>
        /// Rewards received for baked blocks.
        /// </summary>
        public long BlockRewards { get; set; }

        /// <summary>
        /// [DEPRECATED]
        /// </summary>
        public int OwnBlocks { get; set; }

        /// <summary>
        /// [DEPRECATED]
        /// </summary>
        public long OwnBlockRewards { get; set; }

        /// <summary>
        /// [DEPRECATED]
        /// </summary>
        public int ExtraBlocks { get; set; }

        /// <summary>
        /// [DEPRECATED]
        /// </summary>
        public long ExtraBlockRewards { get; set; }

        /// <summary>
        /// Number of missed blocks.
        /// </summary>
        public int MissedBlocks { get; set; }

        /// <summary>
        /// Rewards which were not received due to missing blocks.
        /// </summary>
        public long MissedBlockRewards { get; set; }

        /// <summary>
        /// [DEPRECATED]
        /// </summary>
        public int MissedOwnBlocks { get; set; }

        /// <summary>
        /// [DEPRECATED]
        /// </summary>
        public long MissedOwnBlockRewards { get; set; }

        /// <summary>
        /// [DEPRECATED]
        /// </summary>
        public int MissedExtraBlocks { get; set; }

        /// <summary>
        /// [DEPRECATED]
        /// </summary>
        public long MissedExtraBlockRewards { get; set; }

        /// <summary>
        /// [DEPRECATED]
        /// </summary>
        public int UncoveredOwnBlocks { get; set; }

        /// <summary>
        /// [DEPRECATED]
        /// </summary>
        public long UncoveredOwnBlockRewards { get; set; }

        /// <summary>
        /// [DEPRECATED]
        /// </summary>
        public int UncoveredExtraBlocks { get; set; }

        /// <summary>
        /// [DEPRECATED]
        /// </summary>
        public long UncoveredExtraBlockRewards { get; set; }

        /// <summary>
        /// [DEPRECATED]
        /// </summary>
        public long BlockDeposits { get; set; }

        /// <summary>
        /// Number of slots which baker is allowed to validate in this cycle based on future endorsing rights.
        /// </summary>
        public int FutureEndorsements { get; set; }

        /// <summary>
        /// Estimated value of future endorsing rewards.
        /// </summary>
        public long FutureEndorsementRewards { get; set; }

        /// <summary>
        /// [DEPRECATED]
        /// </summary>
        public long FutureEndorsementDeposits { get; set; }

        /// <summary>
        /// Number of successfully endorsed slots.
        /// </summary>
        public int Endorsements { get; set; }

        /// <summary>
        /// Rewards received for endorsed slots.
        /// </summary>
        public long EndorsementRewards { get; set; }

        /// <summary>
        /// Number of endorsing slots which were missed for no apparent reason (usually due to issues with network or node).
        /// </summary>
        public int MissedEndorsements { get; set; }

        /// <summary>
        /// Rewards which were not received due to missing endorsements.
        /// </summary>
        public long MissedEndorsementRewards { get; set; }

        /// <summary>
        /// [DEPRECATED]
        /// </summary>
        public int UncoveredEndorsements { get; set; }

        /// <summary>
        /// [DEPRECATED]
        /// </summary>
        public long UncoveredEndorsementRewards { get; set; }

        /// <summary>
        /// [DEPRECATED]
        /// </summary>
        public long EndorsementDeposits { get; set; }

        /// <summary>
        /// Operation fees which were harvested from successfully baked blocks.
        /// </summary>
        public long BlockFees { get; set; }

        /// <summary>
        /// Operation fees which were not received due to missing blocks.
        /// </summary>
        public long MissedBlockFees { get; set; }

        /// <summary>
        /// [DEPRECATED]
        /// </summary>
        public long OwnBlockFees { get; set; }

        /// <summary>
        /// [DEPRECATED]
        /// </summary>
        public long ExtraBlockFees { get; set; }

        /// <summary>
        /// [DEPRECATED]
        /// </summary>
        public long MissedOwnBlockFees { get; set; }

        /// <summary>
        /// [DEPRECATED]
        /// </summary>
        public long MissedExtraBlockFees { get; set; }

        /// <summary>
        /// [DEPRECATED]
        /// </summary>
        public long UncoveredOwnBlockFees { get; set; }

        /// <summary>
        /// [DEPRECATED]
        /// </summary>
        public long UncoveredExtraBlockFees { get; set; }

        /// <summary>
        /// Rewards for detecting double baking (accusing someone of producing two different blocks at the same level).
        /// </summary>
        public long DoubleBakingRewards { get; set; }

        /// <summary>
        /// Bonds, rewards and gathered fees lost due to double baking
        /// </summary>
        public long DoubleBakingLosses { get; set; }

        /// <summary>
        /// [DEPRECATED]
        /// </summary>
        public long DoubleBakingLostDeposits { get; set; }

        /// <summary>
        /// [DEPRECATED]
        /// </summary>
        public long DoubleBakingLostRewards { get; set; }

        /// <summary>
        /// [DEPRECATED]
        /// </summary>
        public long DoubleBakingLostFees { get; set; }

        /// <summary>
        /// Rewards for detecting double endorsing (accusing someone of validating two different blocks at the same level).
        /// </summary>
        public long DoubleEndorsingRewards { get; set; }

        /// <summary>
        /// Bonds, rewards and gathered fees lost due to double endorsing
        /// </summary>
        public long DoubleEndorsingLosses { get; set; }

        /// <summary>
        /// [DEPRECATED]
        /// </summary>
        public long DoubleEndorsingLostDeposits { get; set; }

        /// <summary>
        /// [DEPRECATED]
        /// </summary>
        public long DoubleEndorsingLostRewards { get; set; }

        /// <summary>
        /// [DEPRECATED]
        /// </summary>
        public long DoubleEndorsingLostFees { get; set; }

        /// <summary>
        /// Rewards for including into a block seed nonce revelation operations.
        /// </summary>
        public long RevelationRewards { get; set; }

        /// <summary>
        /// Rewards and gathered fees lost due to missing seed nonce revelation.
        /// </summary>
        public long RevelationLosses { get; set; }

        /// <summary>
        /// [DEPRECATED]
        /// </summary>
        public long RevelationLostRewards { get; set; }

        /// <summary>
        /// [DEPRECATED]
        /// </summary>
        public long RevelationLostFees { get; set; }

        /// <summary>
        /// List of delegators at the snapshot time
        /// </summary>
        public IEnumerable<SplitDelegator> Delegators { get; set; }
    }
}
