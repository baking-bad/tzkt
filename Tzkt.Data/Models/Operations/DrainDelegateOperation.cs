﻿using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models.Base;

namespace Tzkt.Data.Models
{
    public class DrainDelegateOperation : BaseOperation
    {
        public required int DelegateId { get; set; }
        public required int TargetId { get; set; }

        public long Amount { get; set; }
        public long Fee { get; set; }
        public long AllocationFee { get; set; }
    }

    public static class DrainDelegateOperationModel
    {
        public static void BuildDrainDelegateOperationModel(this ModelBuilder modelBuilder)
        {
            #region keys
            modelBuilder.Entity<DrainDelegateOperation>()
                .HasKey(x => x.Id);
            #endregion
            
            #region props
            modelBuilder.Entity<DrainDelegateOperation>()
                .Property(x => x.OpHash)
                .IsFixedLength(true)
                .HasMaxLength(51)
                .IsRequired();
            #endregion

            #region indexes
            modelBuilder.Entity<DrainDelegateOperation>()
                .HasIndex(x => x.Level);

            modelBuilder.Entity<DrainDelegateOperation>()
                .HasIndex(x => x.OpHash);

            modelBuilder.Entity<DrainDelegateOperation>()
                .HasIndex(x => x.DelegateId);

            modelBuilder.Entity<DrainDelegateOperation>()
                .HasIndex(x => x.TargetId);
            #endregion
        }
    }
}
