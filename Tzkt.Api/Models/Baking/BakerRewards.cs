using System;

namespace Tzkt.Api.Models
{
    public class BakerRewards
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
        /// Estimated value of bonds which will be locked as security deposit for future blocks.
        /// </summary>
        public long FutureBlockDeposits { get; set; }

        /// <summary>
        /// Number of successfully baked blocks with priority `0`.
        /// </summary>
        public int OwnBlocks { get; set; }

        /// <summary>
        /// Rewards received for blocks baked with priority `0`.
        /// </summary>
        public long OwnBlockRewards { get; set; }

        /// <summary>
        /// Number of successfully baked blocks with priority `1+`.
        /// </summary>
        public int ExtraBlocks { get; set; }

        /// <summary>
        /// Rewards received for blocks baked with priority `1+`.
        /// </summary>
        public long ExtraBlockRewards { get; set; }

        /// <summary>
        /// Number of blocks which were missed at priority `0` for no apparent reason (usually due to issues with network or node).
        /// </summary>
        public int MissedOwnBlocks { get; set; }

        /// <summary>
        /// Rewards which were not received due to missing own blocks.
        /// </summary>
        public long MissedOwnBlockRewards { get; set; }

        /// <summary>
        /// Number of blocks which were missed at priority `1+` for no apparent reason (usually due to issues with network or node).
        /// </summary>
        public int MissedExtraBlocks { get; set; }

        /// <summary>
        /// Rewards which were not received due to missing extra blocks.
        /// </summary>
        public long MissedExtraBlockRewards { get; set; }

        /// <summary>
        /// Number of blocks which were missed at priority `0` due to lack of bonds (for example, when a baker is overdelegated).
        /// </summary>
        public int UncoveredOwnBlocks { get; set; }

        /// <summary>
        /// Rewards which were not received due to missing own blocks due to lack of bonds.
        /// </summary>
        public long UncoveredOwnBlockRewards { get; set; }

        /// <summary>
        /// Number of blocks which were missed at priority `1+` due to lack of bonds (for example, when a baker is overdelegated).
        /// </summary>
        public int UncoveredExtraBlocks { get; set; }

        /// <summary>
        /// Rewards which were not received due to missing extra blocks due to lack of bonds.
        /// </summary>
        public long UncoveredExtraBlockRewards { get; set; }

        /// <summary>
        /// Bonds which were locked as a security deposit for baking own and extra blocks.
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
        /// Estimated value of bonds which will be locked as security deposit for future endorsements.
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
        /// Number of endorsing slots which were missed due to lack of bonds (for example, when a baker is overdelegated).
        /// </summary>
        public int UncoveredEndorsements { get; set; }

        /// <summary>
        /// Rewards which were not received due to missing endorsements due to lack of bonds.
        /// </summary>
        public long UncoveredEndorsementRewards { get; set; }

        /// <summary>
        /// Bonds which were locked as a security deposit for endorsed slots.
        /// </summary>
        public long EndorsementDeposits { get; set; }

        /// <summary>
        /// Operation fees which were harvested from successfully baked blocks with priority `0`.
        /// </summary>
        public long OwnBlockFees { get; set; }

        /// <summary>
        /// Operation fees which were harvested from successfully baked blocks with priority `1+`.
        /// </summary>
        public long ExtraBlockFees { get; set; }

        /// <summary>
        /// Operation fees which were not received due to missing own blocks.
        /// </summary>
        public long MissedOwnBlockFees { get; set; }

        /// <summary>
        /// Operation fees which were not received due to missing extra blocks.
        /// </summary>
        public long MissedExtraBlockFees { get; set; }

        /// <summary>
        /// Operation fees which were not received due to missing own blocks (due to lack of bonds).
        /// </summary>
        public long UncoveredOwnBlockFees { get; set; }

        /// <summary>
        /// Operation fees which were not received due to missing extra blocks (due to lack of bonds).
        /// </summary>
        public long UncoveredExtraBlockFees { get; set; }

        /// <summary>
        /// Rewards for detecting double baking (accusing someone of producing two different blocks at the same level).
        /// </summary>
        public long DoubleBakingRewards { get; set; }

        /// <summary>
        /// Bonds lost due to double baking
        /// </summary>
        public long DoubleBakingLostDeposits { get; set; }

        /// <summary>
        /// Rewards lost due to double baking
        /// </summary>
        public long DoubleBakingLostRewards { get; set; }

        /// <summary>
        /// Fees lost due to double baking
        /// </summary>
        public long DoubleBakingLostFees { get; set; }

        /// <summary>
        /// Rewards for detecting double endorsing (accusing someone of validating two different blocks at the same level).
        /// </summary>
        public long DoubleEndorsingRewards { get; set; }

        /// <summary>
        /// Bonds lost due to double endorsing
        /// </summary>
        public long DoubleEndorsingLostDeposits { get; set; }

        /// <summary>
        /// Rewards lost due to double endorsing
        /// </summary>
        public long DoubleEndorsingLostRewards { get; set; }

        /// <summary>
        /// Fees lost due to double endorsing
        /// </summary>
        public long DoubleEndorsingLostFees { get; set; }

        /// <summary>
        /// Rewards for including into a block seed nonce revelation operations.
        /// </summary>
        public long RevelationRewards { get; set; }

        /// <summary>
        /// Rewards lost due to missing seed nonce revelation.
        /// </summary>
        public long RevelationLostRewards { get; set; }

        /// <summary>
        /// Fees lost due to missing seed nonce revelation.
        /// </summary>
        public long RevelationLostFees { get; set; }

        #region injecting
        /// <summary>
        /// Injected historical quote at the end of the cycle
        /// </summary>
        public QuoteShort Quote { get; set; }
        #endregion
    }
}
