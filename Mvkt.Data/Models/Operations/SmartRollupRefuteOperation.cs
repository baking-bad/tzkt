using Microsoft.EntityFrameworkCore;
using Mvkt.Data.Models.Base;

namespace Mvkt.Data.Models
{
    public class SmartRollupRefuteOperation : ManagerOperation
    {
        public int? SmartRollupId { get; set; }
        public int? GameId { get; set; }
        public RefutationMove Move { get; set; }
        public RefutationGameStatus GameStatus { get; set; }
        public long? DissectionStart { get; set; }
        public long? DissectionEnd { get; set; }
        public int? DissectionSteps { get; set; }
    }

    public enum RefutationMove
    {
        Start,
        Dissection,
        Proof,
        Timeout
    }

    public enum RefutationGameStatus
    {
        None,
        Ongoing,
        Loser,
        Draw
    }

    public static class SmartRollupRefuteOperationModel
    {
        public static void BuildSmartRollupRefuteOperationModel(this ModelBuilder modelBuilder)
        {
            #region keys
            modelBuilder.Entity<SmartRollupRefuteOperation>()
                .HasKey(x => x.Id);
            #endregion
            
            #region props
            modelBuilder.Entity<SmartRollupRefuteOperation>()
                .Property(x => x.OpHash)
                .IsFixedLength(true)
                .HasMaxLength(51)
                .IsRequired();
            #endregion

            #region indexes
            modelBuilder.Entity<SmartRollupRefuteOperation>()
                .HasIndex(x => x.Level);

            modelBuilder.Entity<SmartRollupRefuteOperation>()
                .HasIndex(x => x.OpHash);

            modelBuilder.Entity<SmartRollupRefuteOperation>()
                .HasIndex(x => x.SenderId);

            modelBuilder.Entity<SmartRollupRefuteOperation>()
                .HasIndex(x => x.SmartRollupId);

            modelBuilder.Entity<SmartRollupRefuteOperation>()
                .HasIndex(x => new { x.GameId, x.Id });
            #endregion

            #region relations
            modelBuilder.Entity<SmartRollupRefuteOperation>()
                .HasOne(x => x.Block)
                .WithMany(x => x.SmartRollupRefuteOps)
                .HasForeignKey(x => x.Level)
                .HasPrincipalKey(x => x.Level);
            #endregion
        }
    }
}
