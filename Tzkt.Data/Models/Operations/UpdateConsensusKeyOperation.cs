using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models.Base;

namespace Tzkt.Data.Models
{
    public class UpdateConsensusKeyOperation : ManagerOperation
    {
        public int ActivationCycle { get; set; }
        public string PublicKey { get; set; }
        public string PublicKeyHash { get; set; }
    }

    public static class UpdateConsensusKeyOperationModel
    {
        public static void BuildUpdateConsensusKeyOperationModel(this ModelBuilder modelBuilder)
        {
            #region keys
            modelBuilder.Entity<UpdateConsensusKeyOperation>()
                .HasKey(x => x.Id);
            #endregion
            
            #region props
            modelBuilder.Entity<UpdateConsensusKeyOperation>()
                .Property(x => x.OpHash)
                .IsFixedLength(true)
                .HasMaxLength(51)
                .IsRequired();
            #endregion

            #region indexes
            modelBuilder.Entity<UpdateConsensusKeyOperation>()
                .HasIndex(x => x.Level);

            modelBuilder.Entity<UpdateConsensusKeyOperation>()
                .HasIndex(x => x.OpHash);

            modelBuilder.Entity<UpdateConsensusKeyOperation>()
                .HasIndex(x => x.SenderId);
            #endregion

            #region relations
            modelBuilder.Entity<UpdateConsensusKeyOperation>()
                .HasOne(x => x.Block)
                .WithMany(x => x.UpdateConsensusKeyOps)
                .HasForeignKey(x => x.Level)
                .HasPrincipalKey(x => x.Level);
            #endregion
        }
    }
}
