﻿using Microsoft.EntityFrameworkCore;

namespace Tzkt.Data.Models
{
    public class VotingPeriod
    {
        public required int Id { get; set; }
        public required int Index { get; set; }
        public required int Epoch { get; set; }
        public required int FirstLevel { get; set; }
        public required int LastLevel { get; set; }

        public required PeriodKind Kind { get; set; }
        public PeriodStatus Status { get; set; }
        public DictatorStatus Dictator { get; set; }

        public int TotalBakers { get; set; }
        public long TotalVotingPower { get; set; }

        #region proposal
        public int? UpvotesQuorum { get; set; }

        public int? ProposalsCount { get; set; }
        public int? TopUpvotes { get; set; }
        public long? TopVotingPower { get; set; }

        public bool? SingleWinner { get; set; }
        #endregion

        #region ballot
        public int? ParticipationEma { get; set; }
        public int? BallotsQuorum { get; set; }
        public int? Supermajority { get; set; }
        
        public int? YayBallots { get; set; }
        public int? NayBallots { get; set; }
        public int? PassBallots { get; set; }
        public long? YayVotingPower { get; set; }
        public long? NayVotingPower { get; set; }
        public long? PassVotingPower { get; set; }
        #endregion
    }

    public static class VotingPeriodModel
    {
        public static void BuildVotingPeriodModel(this ModelBuilder modelBuilder)
        {
            #region keys
            modelBuilder.Entity<VotingPeriod>()
                .HasKey(x => x.Id);
            #endregion

            #region indexes
            modelBuilder.Entity<VotingPeriod>()
                .HasIndex(x => x.Index)
                .IsUnique();

            modelBuilder.Entity<VotingPeriod>()
                .HasIndex(x => x.Epoch);
            #endregion
        }
    }

    public enum PeriodKind
    {
        Proposal,
        Exploration,
        Testing,
        Promotion,
        Adoption
    }

    public enum PeriodStatus
    {
        Active,
        NoProposals,
        NoQuorum,
        NoSupermajority,
        NoSingleWinner,
        Success
    }

    public enum DictatorStatus
    {
        None,
        Abort,
        Reset,
        Submit
    }
}
