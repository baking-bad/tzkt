using System;
using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models.Base;

namespace Tzkt.Data.Models
{
    public class SmartRollupPublishOperation : ManagerOperation
    {
        public int? SmartRollupId { get; set; }
        public int? CommitmentId { get; set; }
        public long Bond { get; set; }
        public SmartRollupBondStatus? BondStatus { get; set; }
        public SmartRollupPublishFlags Flags { get; set; }
    }

    public enum SmartRollupBondStatus
    {
        Active,
        Returned,
        Lost
    }

    [Flags]
    public enum SmartRollupPublishFlags
    {
        None                = 0b_0000,
        AddStaker           = 0b_0001,
        ReactivateStaker    = 0b_0010,
        ReactivateBranch    = 0b_0100
    }

    public static class SmartRollupPublishOperationModel
    {
        public static void BuildSmartRollupPublishOperationModel(this ModelBuilder modelBuilder)
        {
            #region keys
            modelBuilder.Entity<SmartRollupPublishOperation>()
                .HasKey(x => x.Id);
            #endregion
            
            #region props
            modelBuilder.Entity<SmartRollupPublishOperation>()
                .Property(x => x.OpHash)
                .IsFixedLength(true)
                .HasMaxLength(51)
                .IsRequired();
            #endregion

            #region indexes
            modelBuilder.Entity<SmartRollupPublishOperation>()
                .HasIndex(x => x.Level);

            modelBuilder.Entity<SmartRollupPublishOperation>()
                .HasIndex(x => x.OpHash);

            modelBuilder.Entity<SmartRollupPublishOperation>()
                .HasIndex(x => x.SenderId);

            modelBuilder.Entity<SmartRollupPublishOperation>()
                .HasIndex(x => x.SmartRollupId);

            modelBuilder.Entity<SmartRollupPublishOperation>()
                .HasIndex(x => x.CommitmentId);

            modelBuilder.Entity<SmartRollupPublishOperation>()
                .HasIndex(x => new { x.SmartRollupId, x.BondStatus, x.SenderId })
                .HasFilter($@"""{nameof(SmartRollupPublishOperation.BondStatus)}"" IS NOT NULL");
            #endregion

            #region relations
            modelBuilder.Entity<SmartRollupPublishOperation>()
                .HasOne(x => x.Block)
                .WithMany(x => x.SmartRollupPublishOps)
                .HasForeignKey(x => x.Level)
                .HasPrincipalKey(x => x.Level);
            #endregion
        }
    }
}
