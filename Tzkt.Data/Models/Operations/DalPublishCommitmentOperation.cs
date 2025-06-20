﻿using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models.Base;

namespace Tzkt.Data.Models
{
    public class DalPublishCommitmentOperation : ManagerOperation
    {
        public int Slot { get; set; }
        public required string Commitment {  get; set; }
    }

    public static class DalPublishCommitmentOperationModel
    {
        public static void BuildDalPublishCommitmentOperationModel(this ModelBuilder modelBuilder)
        {
            #region keys
            modelBuilder.Entity<DalPublishCommitmentOperation>()
                .HasKey(x => x.Id);
            #endregion
            
            #region props
            modelBuilder.Entity<DalPublishCommitmentOperation>()
                .Property(x => x.OpHash)
                .IsFixedLength(true)
                .HasMaxLength(51)
                .IsRequired();
            #endregion

            #region indexes
            modelBuilder.Entity<DalPublishCommitmentOperation>()
                .HasIndex(x => x.Level);

            modelBuilder.Entity<DalPublishCommitmentOperation>()
                .HasIndex(x => x.OpHash);

            modelBuilder.Entity<DalPublishCommitmentOperation>()
                .HasIndex(x => x.SenderId);
            #endregion
        }
    }
}
