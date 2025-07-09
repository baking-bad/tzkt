using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Tzkt.Data.Models
{
    public class Protocol
    {
        // TODO: generate ID in app state
        public required int Id { get; set; }
        public required int Code { get; set; }
        public required string Hash { get; set; }
        public required int Version { get; set; }
        public required int FirstLevel { get; set; }
        public required int LastLevel { get; set; }
        public required int FirstCycle { get; set; }
        public required int FirstCycleLevel { get; set; }

        public int RampUpCycles { get; set; }
        public int NoRewardCycles { get; set; }

        public int ConsensusRightsDelay { get; set; }
        public int DelegateParametersActivationDelay { get; set; }

        public int BlocksPerCycle { get; set; }
        public int BlocksPerCommitment { get; set; }
        public int BlocksPerSnapshot { get; set; }
        public int BlocksPerVoting { get; set; }

        public int TimeBetweenBlocks { get; set; }

        public int AttestersPerBlock { get; set; }
        public int HardOperationGasLimit { get; set; }
        public int HardOperationStorageLimit { get; set; }
        public int HardBlockGasLimit { get; set; }

        public long MinimalStake { get; set; }
        public long MinimalFrozenStake { get; set; }

        public long BlockDeposit { get; set; }
        public long BlockReward0 { get; set; }
        public long BlockReward1 { get; set; }
        public long MaxBakingReward { get; set; }

        public long AttestationDeposit { get; set; }
        public long AttestationReward0 { get; set; }
        public long AttestationReward1 { get; set; }
        public long MaxAttestationReward { get; set; }

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
        public int DenunciationPeriod { get; set; }
        public int SlashingDelay { get; set; }
        public int MaxDelegatedOverFrozenRatio { get; set; }
        public int MaxExternalOverOwnStakeRatio { get; set; }
        public int StakePowerMultiplier { get; set; }

        public int SmartRollupOriginationSize { get; set; }
        public long SmartRollupStakeAmount { get; set; }
        public int SmartRollupChallengeWindow { get; set; }
        public int SmartRollupCommitmentPeriod { get; set; }
        public int SmartRollupTimeoutPeriod { get; set; }

        public string? Dictator { get; set; }

        public int DoubleBakingSlashedPercentage { get; set; }
        public int DoubleConsensusSlashedPercentage { get; set; }

        public int NumberOfShards { get; set; }
        public int ToleratedInactivityPeriod { get; set; }

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

            #region indexes
            modelBuilder.Entity<Protocol>()
                .HasIndex(x => x.Code)
                .IsUnique();

            modelBuilder.Entity<Protocol>()
                .HasIndex(x => x.Hash)
                .IsUnique();
            #endregion
        }
    }
}
