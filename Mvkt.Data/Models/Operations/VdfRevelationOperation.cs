﻿using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Mvkt.Data.Models.Base;

namespace Mvkt.Data.Models
{
    public class VdfRevelationOperation : BaseOperation
    {
        public int Cycle { get; set; }
        public int BakerId { get; set; }
        public long RewardLiquid { get; set; }
        public long RewardStakedOwn { get; set; }
        public long RewardStakedShared { get; set; }
        public byte[] Solution { get; set; }
        public byte[] Proof { get; set; }

        #region relations
        [ForeignKey(nameof(BakerId))]
        public Delegate Baker { get; set; }
        #endregion
    }

    public static class VdfRevelationOperationModel
    {
        public static void BuildVdfRevelationOperationModel(this ModelBuilder modelBuilder)
        {
            #region keys
            modelBuilder.Entity<VdfRevelationOperation>()
                .HasKey(x => x.Id);
            #endregion
            
            #region props
            modelBuilder.Entity<VdfRevelationOperation>()
                .Property(x => x.OpHash)
                .IsFixedLength(true)
                .HasMaxLength(51)
                .IsRequired();
            #endregion

            #region indexes
            modelBuilder.Entity<VdfRevelationOperation>()
                .HasIndex(x => x.Level);

            modelBuilder.Entity<VdfRevelationOperation>()
                .HasIndex(x => x.OpHash);

            modelBuilder.Entity<VdfRevelationOperation>()
                .HasIndex(x => x.BakerId);

            modelBuilder.Entity<VdfRevelationOperation>()
                .HasIndex(x => x.Cycle);
            #endregion

            #region relations
            modelBuilder.Entity<VdfRevelationOperation>()
                .HasOne(x => x.Block)
                .WithMany(x => x.VdfRevelationOps)
                .HasForeignKey(x => x.Level)
                .HasPrincipalKey(x => x.Level);
            #endregion
        }
    }
}
