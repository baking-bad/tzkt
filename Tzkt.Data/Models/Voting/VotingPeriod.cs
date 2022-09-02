using Microsoft.EntityFrameworkCore;

namespace Tzkt.Data.Models
{
    public class VotingPeriod
    {
        public int Id { get; set; }
        public int Index { get; set; }
        public int Epoch { get; set; }
        public int FirstLevel { get; set; }
        public int LastLevel { get; set; }

        public PeriodKind Kind { get; set; }
        public PeriodStatus Status { get; set; }
        public DictatorStatus Dictator { get; set; }

        public int? TotalBakers { get; set; }
        public long? TotalVotingPower { get; set; }

        #region proposal
        public int? UpvotesQuorum { get; set; }

        public int? ProposalsCount { get; set; }
        public int? TopUpvotes { get; set; }
        public long? TopVotingPower { get; set; }
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

            modelBuilder.Entity<VotingPeriod>()
                .HasAlternateKey(x => x.Index);
            #endregion

            #region indexes
            modelBuilder.Entity<VotingPeriod>()
                .HasIndex(x => x.Id)
                .IsUnique();

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
