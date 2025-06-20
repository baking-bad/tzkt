﻿using System.Numerics;
using Microsoft.EntityFrameworkCore;

namespace Tzkt.Data.Models
{
    public class TicketTransfer
    {
        public required long Id { get; set; }
        public required long TicketId { get; set; }
        public required int TicketerId { get; set; }
        public required int Level { get; set; }
        public BigInteger Amount { get; set; }

        public int? FromId { get; set; }
        public int? ToId { get; set; }

        public long? TransactionId { get; set; }
        public long? TransferTicketId { get; set; }
        public long? SmartRollupExecuteId { get; set; }
    }

    public static class TicketTransferModel
    {
        public static void BuildTicketTransferModel(this ModelBuilder modelBuilder)
        {
            #region keys
            modelBuilder.Entity<TicketTransfer>()
                .HasKey(x => x.Id);
            #endregion

            #region indexes
            modelBuilder.Entity<TicketTransfer>()
                .HasIndex(x => new { x.Level, x.Id });

            modelBuilder.Entity<TicketTransfer>()
                .HasIndex(x => x.TicketerId);

            modelBuilder.Entity<TicketTransfer>()
                .HasIndex(x => x.TicketId);

            modelBuilder.Entity<TicketTransfer>()
                .HasIndex(x => x.FromId);

            modelBuilder.Entity<TicketTransfer>()
                .HasIndex(x => x.ToId);

            modelBuilder.Entity<TicketTransfer>()
                .HasIndex(x => x.TransactionId)
                .HasFilter($@"""{nameof(TicketTransfer.TransactionId)}"" IS NOT NULL");

            modelBuilder.Entity<TicketTransfer>()
                .HasIndex(x => x.TransferTicketId)
                .HasFilter($@"""{nameof(TicketTransfer.TransferTicketId)}"" IS NOT NULL");

            modelBuilder.Entity<TicketTransfer>()
                .HasIndex(x => x.SmartRollupExecuteId)
                .HasFilter($@"""{nameof(TicketTransfer.SmartRollupExecuteId)}"" IS NOT NULL");
            #endregion
        }
    }
}
