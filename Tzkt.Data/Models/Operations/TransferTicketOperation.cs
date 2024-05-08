﻿using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models.Base;

namespace Tzkt.Data.Models
{
    public class TransferTicketOperation : ManagerOperation
    {
        public int? TargetId { get; set; }
        public int? TicketerId { get; set; }
        public BigInteger Amount { get; set; }

        public byte[] RawType { get; set; }
        public byte[] RawContent { get; set; }
        public string JsonContent { get; set; }
        public string Entrypoint { get; set; }

        public int? TicketTransfers { get; set; }
        public int? SubIds { get; set; }
    }

    public static class TransferTicketOperationModel
    {
        public static void BuildTransferTicketOperationModel(this ModelBuilder modelBuilder)
        {
            #region keys
            modelBuilder.Entity<TransferTicketOperation>()
                .HasKey(x => x.Id);
            #endregion
            
            #region props
            modelBuilder.Entity<TransferTicketOperation>()
                .Property(x => x.OpHash)
                .IsFixedLength(true)
                .HasMaxLength(51)
                .IsRequired();
            #endregion

            #region indexes
            modelBuilder.Entity<TransferTicketOperation>()
                .HasIndex(x => x.Level);

            modelBuilder.Entity<TransferTicketOperation>()
                .HasIndex(x => x.OpHash);

            modelBuilder.Entity<TransferTicketOperation>()
                .HasIndex(x => x.SenderId);

            modelBuilder.Entity<TransferTicketOperation>()
                .HasIndex(x => x.TargetId);

            modelBuilder.Entity<TransferTicketOperation>()
                .HasIndex(x => x.TicketerId);
            #endregion

            #region relations
            modelBuilder.Entity<TransferTicketOperation>()
                .HasOne(x => x.Block)
                .WithMany(x => x.TransferTicketOps)
                .HasForeignKey(x => x.Level)
                .HasPrincipalKey(x => x.Level);
            #endregion
        }
    }
}
