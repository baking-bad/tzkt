﻿using System;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Mvkt.Data.Models
{
    public class Protocol
    {
        // TODO: generate ID in app state
        public int Id { get; set; }
        public int Code { get; set; }
        public string Hash { get; set; }
        public int FirstLevel { get; set; }
        public int LastLevel { get; set; }
        public int FirstCycle { get; set; }
        public int FirstCycleLevel { get; set; }

        public int RampUpCycles { get; set; }
        public int NoRewardCycles { get; set; }

        public int PreservedCycles { get; set; }

        public int BlocksPerCycle { get; set; }
        public int BlocksPerCommitment { get; set; }
        public int BlocksPerSnapshot { get; set; }
        public int BlocksPerVoting { get; set; }

        public int TimeBetweenBlocks { get; set; }

        public int EndorsersPerBlock { get; set; }
        public int HardOperationGasLimit { get; set; }
        public int HardOperationStorageLimit { get; set; }
        public int HardBlockGasLimit { get; set; }

        public long MinimalStake { get; set; }
        public long MinimalFrozenStake { get; set; }

        public long BlockDeposit { get; set; }
        public long BlockReward0 { get; set; }
        public long BlockReward1 { get; set; }
        public long MaxBakingReward { get; set; }

        public long EndorsementDeposit { get; set; }
        public long EndorsementReward0 { get; set; }
        public long EndorsementReward1 { get; set; }
        public long MaxEndorsingReward { get; set; }

        public int OriginationSize { get; set; }
        public int ByteCost { get; set; }

        public int ProposalQuorum { get; set; }
        public int BallotQuorumMin { get; set; }
        public int BallotQuorumMax { get; set; }

        /// <summary>
        /// 1/2 window size of 2000 blocks with precision of 1000000 for integer computation
        /// </summary>
        public int LBToggleThreshold { get; set; }

        public int ConsensusThreshold { get; set; }
        public int MinParticipationNumerator { get; set; }
        public int MinParticipationDenominator { get; set; }
        public int MaxSlashingPeriod { get; set; }
        public int MaxDelegatedOverFrozenRatio { get; set; }
        public int MaxExternalOverOwnStakeRatio { get; set; }
        public int StakePowerMultiplier { get; set; }

        public int SmartRollupOriginationSize { get; set; }
        public long SmartRollupStakeAmount { get; set; }
        public int SmartRollupChallengeWindow { get; set; }
        public int SmartRollupCommitmentPeriod { get; set; }
        public int SmartRollupTimeoutPeriod { get; set; }

        public string Dictator { get; set; }

        public long BaseIssuedPerMinute { get; set; }
        public int BlockRewardWeight { get; set; }
        public int BlockBonusWeight { get; set; }
        public int EndorsingRewardWeight { get; set; }
        public int NonceRevelationRewardWeight { get; set; }
        public int VdfRevelationRewardWeight { get; set; }
        public int LBSubsidyWeight { get; set; }

        public int DoubleBakingSlashedPercentage { get; set; }
        public int DoubleEndorsingSlashedPercentage { get; set; }

        #region helpers
        public int GetCycleStart(int cycle)
        {
            if (cycle < FirstCycle)
                throw new Exception("Cycle doesn't match the protocol");

            return FirstCycleLevel + (cycle - FirstCycle) * BlocksPerCycle;
        }
        public int GetCycleEnd(int cycle)
        {
            if (cycle < FirstCycle)
                throw new Exception("Cycle doesn't match the protocol");

            return GetCycleStart(cycle) + BlocksPerCycle - 1;
        }
        public int GetCycle(int level)
        {
            if (level < FirstLevel)
                throw new Exception("Level doesn't match the protocol");

            if (level < FirstCycleLevel)
                return FirstCycle - 1;

            return FirstCycle + (level - FirstCycleLevel) / BlocksPerCycle;
        }
        public bool IsCycleStart(int level)
        {
            if (level < FirstLevel)
                throw new Exception("Level doesn't match the protocol");

            return (level - FirstCycleLevel) % BlocksPerCycle == 0;
        }
        public bool IsCycleEnd(int level)
        {
            if (level < FirstLevel)
                throw new Exception("Level doesn't match the protocol");

            return (level + 1 - FirstCycleLevel) % BlocksPerCycle == 0;
        }

        [NotMapped]
        public int SnapshotsPerCycle => BlocksPerCycle / BlocksPerSnapshot;

        [NotMapped]
        public bool HasDictator => Dictator != null;
        #endregion
    }

    public static class ProtocolModel
    {
        public static void BuildProtocolModel(this ModelBuilder modelBuilder)
        {
            #region keys
            modelBuilder.Entity<Protocol>()
                .HasKey(x => x.Id);

            modelBuilder.Entity<Protocol>()
                .HasAlternateKey(x => x.Code);
            #endregion

            #region props
            modelBuilder.Entity<Protocol>()
                .Property(x => x.Hash)
                .IsFixedLength(true)
                .HasMaxLength(51)
                .IsRequired();

            // shadow property
            modelBuilder.Entity<Protocol>()
                .Property<string>("Extras")
                .HasColumnType("jsonb");
            #endregion
        }
    }
}
