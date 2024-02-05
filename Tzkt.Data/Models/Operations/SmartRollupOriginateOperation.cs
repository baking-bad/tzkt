using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models.Base;

namespace Tzkt.Data.Models
{
    public class SmartRollupOriginateOperation : ManagerOperation
    {
        public PvmKind PvmKind { get; set; }
        public byte[] Kernel { get; set; }
        public byte[] ParameterType { get; set; }
        public string GenesisCommitment { get; set; }
        public int? SmartRollupId { get; set; }
    }

    public static class SmartRollupOriginateOperationModel
    {
        public static void BuildSmartRollupOriginateOperationModel(this ModelBuilder modelBuilder)
        {
            #region keys
            modelBuilder.Entity<SmartRollupOriginateOperation>()
                .HasKey(x => x.Id);
            #endregion
            
            #region props
            modelBuilder.Entity<SmartRollupOriginateOperation>()
                .Property(x => x.OpHash)
                .IsFixedLength(true)
                .HasMaxLength(51)
                .IsRequired();
            #endregion

            #region indexes
            modelBuilder.Entity<SmartRollupOriginateOperation>()
                .HasIndex(x => x.Level);

            modelBuilder.Entity<SmartRollupOriginateOperation>()
                .HasIndex(x => x.OpHash);

            modelBuilder.Entity<SmartRollupOriginateOperation>()
                .HasIndex(x => x.SenderId);

            modelBuilder.Entity<SmartRollupOriginateOperation>()
                .HasIndex(x => x.SmartRollupId);
            #endregion

            #region relations
            modelBuilder.Entity<SmartRollupOriginateOperation>()
                .HasOne(x => x.Block)
                .WithMany(x => x.SmartRollupOriginateOps)
                .HasForeignKey(x => x.Level)
                .HasPrincipalKey(x => x.Level);
            #endregion
        }
    }
}
