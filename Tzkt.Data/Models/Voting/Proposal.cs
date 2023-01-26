using Microsoft.EntityFrameworkCore;

namespace Tzkt.Data.Models
{
    public class Proposal
    {
        public int Id { get; set; }
        public string Hash { get; set; }
        public int InitiatorId { get; set; }
        public int FirstPeriod { get; set; }
        public int LastPeriod { get; set; }
        public int Epoch { get; set; }
        
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
