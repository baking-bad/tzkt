﻿namespace Tzkt.Api.Models
{
    public class ProtocolConstants
    {
        /// <summary>
        /// The number of cycles where security deposit is ramping up
        /// </summary>
        public int RampUpCycles { get; set; }

        /// <summary>
        /// The number of cycles with no baking rewards
        /// </summary>
        public int NoRewardCycles { get; set; }

        /// <summary>
        /// Delay in cycles after which baking rights are assigned
        /// </summary>
        public int ConsensusRightsDelay { get; set; }

        /// <summary>
        /// Delay in cycles after which the parameters from `set_delegate_parameters` operations take effect
        /// </summary>
        public int DelegateParametersActivationDelay { get; set; }

        /// <summary>
        /// A number of blocks the cycle contains
        /// </summary>
        public int BlocksPerCycle { get; set; }
        
        /// <summary>
        /// A number of blocks that indicates how often seed nonce hash is included in a block. Seed nonce hash presents in only one out of `blocksPerCommitment`
        /// </summary>
        public int BlocksPerCommitment { get; set; }
        
        /// <summary>
        /// A number of blocks that indicates how often a snapshot (snapshots are records of the state of stake distributions) is taken
        /// </summary>
        public int BlocksPerSnapshot { get; set; }
        
        /// <summary>
        /// A number of block that indicates how long a voting period takes
        /// </summary>
        public int BlocksPerVoting { get; set; }

        /// <summary>
        /// Minimum amount of seconds between blocks
        /// </summary>
        public int TimeBetweenBlocks { get; set; }

        /// <summary>
        /// Number of bakers that assigned to endorse a block
        /// </summary>
        public int EndorsersPerBlock { get; set; }
        
        /// <summary>
        /// Maximum amount of gas that one operation can consume
        /// </summary>
        public int HardOperationGasLimit { get; set; }
        
        /// <summary>
        /// Maximum amount of storage that one operation can consume
        /// </summary>
        public int HardOperationStorageLimit { get; set; }
        
        /// <summary>
        /// Maximum amount of total gas usage of a single block
        /// </summary>
        public int HardBlockGasLimit { get; set; }

        /// <summary>
        /// Required number of tokens to get 1 roll (micro tez)
        /// </summary>
        public long MinimalStake { get; set; }

        /// <summary>
        /// Required number of tokens to be frozen by bakers (micro tez)
        /// </summary>
        public long MinimalFrozenStake { get; set; }

        /// <summary>
        /// Security deposit for baking (producing) a block (micro tez)
        /// </summary>
        public long BlockDeposit { get; set; }

        //TODO Think about it
        /// <summary>
        /// Reward for baking (producing) a block (micro tez)
        /// </summary>
        public List<long> BlockReward { get; set; }

        /// <summary>
        /// Security deposit for sending an endorsement operation (micro tez)
        /// </summary>
        public long EndorsementDeposit { get; set; }
        
        /// <summary>
        /// Reward for sending an endorsement operation (micro tez)
        /// </summary>
        public List<long> EndorsementReward { get; set; }

        /// <summary>
        /// Initial storage size of an originated (created) account (bytes)
        /// </summary>
        public int OriginationSize { get; set; }
        
        /// <summary>
        /// Cost of one storage byte in the blockchain (micro tez)
        /// </summary>
        public int ByteCost { get; set; }

        /// <summary>
        /// Percentage of the total number of voting power required to select a proposal on the proposal period
        /// </summary>
        public double ProposalQuorum { get; set; }
        
        /// <summary>
        /// The minimum value of quorum percentage on the exploration and promotion periods
        /// </summary>
        public double BallotQuorumMin { get; set; }

        /// <summary>
        /// The maximum value of quorum percentage on the exploration and promotion periods
        /// </summary>
        public double BallotQuorumMax { get; set; }

        /// <summary>
        /// 1/2 window size of 2000 blocks with precision of 1000000 for integer computation
        /// </summary>
        public int LBToggleThreshold { get; set; }

        /// <summary>
        /// Endorsement quorum
        /// </summary>
        public int ConsensusThreshold { get; set; }

        /// <summary>
        /// Number of endorsed slots needed to receive endorsing rewards
        /// </summary>
        public int MinParticipationNumerator { get; set; }

        /// <summary>
        /// Number of endorsed slots needed to receive endorsing rewards
        /// </summary>
        public int MinParticipationDenominator { get; set; }

        /// <summary>
        /// Number of cycles after double baking/(pre)endorsing where an accusation operation can be injected
        /// </summary>
        public int DenunciationPeriod { get; set; }

        /// <summary>
        /// Number of cycles after double baking/(pre)endorsing evidence where slashing happens
        /// </summary>
        public int SlashingDelay { get; set; }

        /// <summary>
        /// The ratio of delegated tez over the baker’s frozen stake
        /// </summary>
        public int MaxDelegatedOverFrozenRatio { get; set; }

        /// <summary>
        /// The ratio of external staked balance over the baker’s own staked balance
        /// </summary>
        public int MaxExternalOverOwnStakeRatio { get; set; }

        /// <summary>
        /// Initial storage size of an originated (created) smart rollup (bytes)
        /// </summary>
        public int SmartRollupOriginationSize { get; set; }

        /// <summary>
        /// Smart rollup commitment bond (mutez)
        /// </summary>
        public long SmartRollupStakeAmount { get; set; }

        /// <summary>
        /// Window (in blocks) when it's possible to refute pending commitment
        /// </summary>
        public int SmartRollupChallengeWindow { get; set; }

        /// <summary>
        /// Period (in blocks) for publishing commitments
        /// </summary>
        public int SmartRollupCommitmentPeriod { get; set; }

        /// <summary>
        /// Period (in blocks) when a refutation game player must make a turn
        /// </summary>
        public int SmartRollupTimeoutPeriod { get; set; }

        /// <summary>
        /// Number of DAL Shards
        /// </summary>
        public int DalNumberOfShards { get; set; }

        /// <summary>
        /// Governance dictator
        /// </summary>
        public string Dictator { get; set; }

        #region deprecated
        /// <summary>
        /// [DEPRECATED]
        /// </summary>
        public int MaxSlashingPeriod => DenunciationPeriod + SlashingDelay;

        /// <summary>
        /// [DEPRECATED]
        /// </summary>
        public int PreservedCycles => ConsensusRightsDelay;

        /// <summary>
        /// [DEPRECATED]
        /// </summary>
        public int LBEscapeThreshold => LBToggleThreshold;

        /// <summary>
        /// [DEPRECATED]
        /// </summary>
        public int LBSunsetLevel => 3_063_809;

        /// <summary>
        /// [DEPRECATED]
        /// </summary>
        public int FrozenDepositsPercentage => 10;

        /// <summary>
        /// [DEPRECATED]
        /// </summary>
        public int TxRollupOriginationSize => 4_000;

        /// <summary>
        /// [DEPRECATED]
        /// </summary>
        public long TxRollupCommitmentBond => 10_000_000_000;

        /// <summary>
        /// [DEPRECATED]
        /// </summary>
        public long DoubleBakingPunishment => 640_000_000;

        /// <summary>
        /// [DEPRECATED]
        /// </summary>
        public int DoubleEndorsingPunishmentNumerator => 1;

        /// <summary>
        /// [DEPRECATED]
        /// </summary>
        public int DoubleEndorsingPunishmentDenominator => 2;

        /// <summary>
        /// [DEPRECATED]
        /// </summary>
        public long RevelationReward => 125_000;

        /// <summary>
        /// [DEPRECATED]
        /// </summary>
        public int LBSubsidy => 5_000_000 * TimeBetweenBlocks / 60;

        /// <summary>
        /// [DEPRECATED]
        /// </summary>
        public long TokensPerRoll => MinimalStake;
        #endregion
    }
}
