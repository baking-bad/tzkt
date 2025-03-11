using Microsoft.EntityFrameworkCore;

namespace Tzkt.Data.Models
{
    public class Proposal
    {
        public required int Id { get; set; }
        public required string Hash { get; set; }
        public required int InitiatorId { get; set; }
        public required int FirstPeriod { get; set; }
        public required int LastPeriod { get; set; }
        public required int Epoch { get; set; }
        
        public int Upvotes { get; set; }
        public long VotingPower { get; set; }
        public ProposalStatus Status { get; set; }
    }

    public static class ProposalModel
    {
        public static void BuildProposalModel(this ModelBuilder modelBuilder)
        {
            #region keys
            modelBuilder.Entity<Proposal>()
                .HasKey(x => x.Id);
            #endregion

            #region props
            modelBuilder.Entity<Proposal>()
                .Property(nameof(Proposal.Hash))
                .IsFixedLength(true)
                .HasMaxLength(51);

            // shadow property
            modelBuilder.Entity<Proposal>()
                .Property<string>("Extras")
                .HasColumnType("jsonb");
            #endregion

            #region indexes
            modelBuilder.Entity<Proposal>()
                .HasIndex(x => x.Epoch);

            modelBuilder.Entity<Proposal>()
                .HasIndex(x => x.Hash);

            modelBuilder.Entity<Proposal>()
                .HasIndex(x => x.LastPeriod);

            modelBuilder.Entity<Proposal>()
                .HasIndex(x => x.FirstPeriod);

            modelBuilder.Entity<Proposal>()
                .HasIndex(x => x.Status, $"IX_{nameof(TzktContext.Proposals)}_{nameof(Proposal.Status)}_Partial")
                .HasFilter($@"""{nameof(Proposal.Status)}"" = {(int)ProposalStatus.Active}");
            #endregion
        }
    }

    public enum ProposalStatus
    {
        Active,
        Accepted,
        Rejected,
        Skipped
    }
}
