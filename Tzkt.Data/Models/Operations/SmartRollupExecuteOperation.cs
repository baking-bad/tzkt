﻿using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models.Base;

namespace Tzkt.Data.Models
{
    public class SmartRollupExecuteOperation : ManagerOperation
    {
        public int? SmartRollupId { get; set; }
        public int? CommitmentId { get; set; }

        public int? TicketTransfers { get; set; }
        public int? SubIds { get; set; }
    }

    public static class SmartRollupExecuteOperationModel
    {
        public static void BuildSmartRollupExecuteOperationModel(this ModelBuilder modelBuilder)
        {
            #region keys
            modelBuilder.Entity<SmartRollupExecuteOperation>()
                .HasKey(x => x.Id);
            #endregion
            
            #region props
            modelBuilder.Entity<SmartRollupExecuteOperation>()
                .Property(x => x.OpHash)
                .IsFixedLength(true)
                .HasMaxLength(51)
                .IsRequired();
            #endregion

            #region indexes
            modelBuilder.Entity<SmartRollupExecuteOperation>()
                .HasIndex(x => x.Level);

            modelBuilder.Entity<SmartRollupExecuteOperation>()
                .HasIndex(x => x.OpHash);

            modelBuilder.Entity<SmartRollupExecuteOperation>()
                .HasIndex(x => x.SenderId);

            modelBuilder.Entity<SmartRollupExecuteOperation>()
                .HasIndex(x => x.SmartRollupId);

            modelBuilder.Entity<SmartRollupExecuteOperation>()
                .HasIndex(x => new { x.CommitmentId, x.Id });
            #endregion
        }
    }
}
