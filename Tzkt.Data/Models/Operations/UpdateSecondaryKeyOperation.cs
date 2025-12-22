using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models.Base;

namespace Tzkt.Data.Models
{
    public class UpdateSecondaryKeyOperation : ManagerOperation
    {
        public SecondaryKeyType KeyType { get; set; }
        public int ActivationCycle { get; set; }
        public required string PublicKey { get; set; }
        public required string PublicKeyHash { get; set; }
    }

    public enum SecondaryKeyType
    {
        Consensus,
        Companion
    }

    public static class UpdateSecondaryKeyOperationModel
    {
        public static void BuildUpdateSecondaryKeyOperationModel(this ModelBuilder modelBuilder)
        {
            #region keys
            modelBuilder.Entity<UpdateSecondaryKeyOperation>()
                .HasKey(x => x.Id);
            #endregion
            
            #region props
            modelBuilder.Entity<UpdateSecondaryKeyOperation>()
                .Property(x => x.OpHash)
                .IsFixedLength(true)
                .HasMaxLength(51)
                .IsRequired();
            #endregion

            #region indexes
            modelBuilder.Entity<UpdateSecondaryKeyOperation>()
                .HasIndex(x => x.Level);

            modelBuilder.Entity<UpdateSecondaryKeyOperation>()
                .HasIndex(x => x.OpHash);

            modelBuilder.Entity<UpdateSecondaryKeyOperation>()
                .HasIndex(x => new { x.SenderId, x.Id });

            modelBuilder.Entity<UpdateSecondaryKeyOperation>()
                .HasIndex(x => x.ActivationCycle);
            #endregion
        }
    }
}
