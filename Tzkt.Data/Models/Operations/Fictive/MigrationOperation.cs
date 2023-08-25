using System;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Tzkt.Data.Models
{
    public class MigrationOperation
    {
        public long Id { get; set; }
        public int Level { get; set; }
        public DateTime Timestamp { get; set; }

        public int AccountId { get; set; }
        public MigrationKind Kind { get; set; }
        public long BalanceChange { get; set; }

        public int? ScriptId { get; set; }
        public int? StorageId { get; set; }
        public int? BigMapUpdates { get; set; }
        public int? TokenTransfers { get; set; }

        public int? SubIds { get; set; }

        #region relations
        [ForeignKey(nameof(Level))]
        public Block Block { get; set; }

        [ForeignKey(nameof(AccountId))]
        public Account Account { get; set; }

        [ForeignKey(nameof(ScriptId))]
        public Script Script { get; set; }

        [ForeignKey(nameof(StorageId))]
        public Storage Storage { get; set; }
        #endregion
    }

    public enum MigrationKind
    {
        Bootstrap,
        ActivateDelegate,
        AirDrop,
        ProposalInvoice,
        CodeChange,
        Origination,
        Subsidy
    }

    public static class MigrationOperationModel
    {
        public static void BuildMigrationOperationModel(this ModelBuilder modelBuilder)
        {
            #region keys
            modelBuilder.Entity<MigrationOperation>()
                .HasKey(x => x.Id);
            #endregion

            #region indexes
            modelBuilder.Entity<MigrationOperation>()
                .HasIndex(x => x.Level);

            modelBuilder.Entity<MigrationOperation>()
                .HasIndex(x => x.AccountId);
            #endregion

            #region relations
            modelBuilder.Entity<MigrationOperation>()
                .HasOne(x => x.Block)
                .WithMany(x => x.Migrations)
                .HasForeignKey(x => x.Level)
                .HasPrincipalKey(x => x.Level);
            #endregion
        }
    }
}
