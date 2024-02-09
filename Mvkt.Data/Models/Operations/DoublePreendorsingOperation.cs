﻿using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Mvkt.Data.Models.Base;

namespace Mvkt.Data.Models
{
    public class DoublePreendorsingOperation : BaseOperation
    {
        public int AccusedLevel { get; set; }
        public int SlashedLevel { get; set; }

        public int AccuserId { get; set; }
        public int OffenderId { get; set; }
        
        public long Reward { get; set; }
        public long LostStaked { get; set; }
        public long LostUnstaked { get; set; }
        public long LostExternalStaked { get; set; }
        public long LostExternalUnstaked { get; set; }

        // it's needed to handle negligent Oxford implementation
        public long RoundingLoss { get; set; }

        #region relations
        [ForeignKey(nameof(AccuserId))]
        public Delegate Accuser { get; set; }

        [ForeignKey(nameof(OffenderId))]
        public Delegate Offender { get; set; }
        #endregion
    }

    public static class DoublePreendorsingOperationModel
    {
        public static void BuildDoublePreendorsingOperationModel(this ModelBuilder modelBuilder)
        {
            #region keys
            modelBuilder.Entity<DoublePreendorsingOperation>()
                .HasKey(x => x.Id);
            #endregion
            
            #region props
            modelBuilder.Entity<DoublePreendorsingOperation>()
                .Property(x => x.OpHash)
                .IsFixedLength(true)
                .HasMaxLength(51)
                .IsRequired();
            #endregion

            #region indexes
            modelBuilder.Entity<DoublePreendorsingOperation>()
                .HasIndex(x => x.Level);

            modelBuilder.Entity<DoublePreendorsingOperation>()
                .HasIndex(x => x.OpHash);

            modelBuilder.Entity<DoublePreendorsingOperation>()
                .HasIndex(x => x.AccuserId);

            modelBuilder.Entity<DoublePreendorsingOperation>()
                .HasIndex(x => x.OffenderId);
            #endregion

            #region relations
            modelBuilder.Entity<DoublePreendorsingOperation>()
                .HasOne(x => x.Block)
                .WithMany(x => x.DoublePreendorsings)
                .HasForeignKey(x => x.Level)
                .HasPrincipalKey(x => x.Level);
            #endregion
        }
    }
}
