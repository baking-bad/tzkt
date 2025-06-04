﻿using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models.Base;

namespace Tzkt.Data.Models
{
    public class PreendorsementOperation : BaseOperation
    {
        public int DelegateId { get; set; }
        public int Slots { get; set; }

        public int? ResetDeactivation { get; set; }
    }

    public static class PreendorsementOperationModel
    {
        public static void BuildPreendorsementOperationModel(this ModelBuilder modelBuilder)
        {
            #region keys
            modelBuilder.Entity<PreendorsementOperation>()
                .HasKey(x => x.Id);
            #endregion
            
            #region props
            modelBuilder.Entity<PreendorsementOperation>()
                .Property(x => x.OpHash)
                .IsFixedLength(true)
                .HasMaxLength(51)
                .IsRequired();
            #endregion

            #region indexes
            modelBuilder.Entity<PreendorsementOperation>()
                .HasIndex(x => x.Level);

            modelBuilder.Entity<PreendorsementOperation>()
                .HasIndex(x => x.OpHash);

            modelBuilder.Entity<PreendorsementOperation>()
                .HasIndex(x => x.DelegateId);
            #endregion
        }
    }
}
