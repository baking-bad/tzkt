using System;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models.Base;

namespace Tzkt.Data.Models
{
    public class MigrationOperation : IOperation
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
    }

    public enum MigrationKind
    {
        Bootstrap,
        ActivateDelegate,
        AirDrop,
        ProposalInvoice,
        CodeChange,
        Origination,
        Subsidy,
        RemoveBigMapKey
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
        }
    }
}
