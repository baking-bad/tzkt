using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models.Base;

namespace Tzkt.Data.Models
{
    public class ProposalOperation : BaseOperation
    {
        public int PeriodId { get; set; }
        public int ProposalId { get; set; }
        public int SenderId { get; set; }

        #region relations
        [ForeignKey(nameof(PeriodId))]
        public VotingPeriod Period { get; set; }

        [ForeignKey(nameof(ProposalId))]
        public Proposal Proposal { get; set; }

        [ForeignKey(nameof(SenderId))]
        public Delegate Sender { get; set; }
        #endregion
    }

    public static class ProposalOperationModel
    {
        public static void BuildProposalOperationModel(this ModelBuilder modelBuilder)
        {
            #region indexes
            modelBuilder.Entity<ProposalOperation>()
                .HasIndex(x => x.Level);

            modelBuilder.Entity<ProposalOperation>()
                .HasIndex(x => x.OpHash);

            modelBuilder.Entity<ProposalOperation>()
                .HasIndex(x => x.SenderId);
            #endregion
            
            #region keys
            modelBuilder.Entity<ProposalOperation>()
                .HasKey(x => x.Id);
            #endregion
            
            #region props
            modelBuilder.Entity<ProposalOperation>()
                .Property(x => x.OpHash)
                .IsFixedLength(true)
                .HasMaxLength(51)
                .IsRequired();
            #endregion
            
            #region relations
            modelBuilder.Entity<ProposalOperation>()
                .HasOne(x => x.Block)
                .WithMany(x => x.Proposals)
                .HasForeignKey(x => x.Level)
                .HasPrincipalKey(x => x.Level);

            modelBuilder.Entity<ProposalOperation>()
                .HasOne(x => x.Sender)
                .WithMany(x => x.Proposals)
                .HasForeignKey(x => x.SenderId);

            modelBuilder.Entity<ProposalOperation>()
                .HasOne(x => x.Proposal)
                .WithMany(x => x.Proposings)
                .HasForeignKey(x => x.ProposalId);

            modelBuilder.Entity<ProposalOperation>()
                .HasOne(x => x.Period)
                .WithMany(x => x.Proposals)
                .HasForeignKey(x => x.PeriodId);
            #endregion
        }
    }
}
