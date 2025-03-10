using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models.Base;

namespace Tzkt.Data.Models
{
    public class ProposalOperation : BaseOperation
    {
        public int Epoch { get; set; }
        public int Period { get; set; }
        public int ProposalId { get; set; }
        public int SenderId { get; set; }
        public long VotingPower { get; set; }

        public bool Duplicated { get; set; }
    }

    public static class ProposalOperationModel
    {
        public static void BuildProposalOperationModel(this ModelBuilder modelBuilder)
        {
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

            #region indexes
            modelBuilder.Entity<ProposalOperation>()
                .HasIndex(x => x.Epoch);

            modelBuilder.Entity<ProposalOperation>()
                .HasIndex(x => x.Period);

            modelBuilder.Entity<ProposalOperation>()
                .HasIndex(x => x.Level);

            modelBuilder.Entity<ProposalOperation>()
                .HasIndex(x => x.OpHash);

            modelBuilder.Entity<ProposalOperation>()
                .HasIndex(x => x.SenderId);

            modelBuilder.Entity<ProposalOperation>()
                .HasIndex(x => x.ProposalId);
            #endregion
        }
    }
}
