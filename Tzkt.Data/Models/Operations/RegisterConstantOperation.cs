﻿using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models.Base;

namespace Tzkt.Data.Models
{
    public class RegisterConstantOperation : ManagerOperation
    {
        public string? Address { get; set; }
        public byte[]? Value { get; set; }
        public int? Refs { get; set; }
    }

    public static class RegisterConstantOperationModel
    {
        public static void BuildRegisterConstantOperationModel(this ModelBuilder modelBuilder)
        {
            #region keys
            modelBuilder.Entity<RegisterConstantOperation>()
                .HasKey(x => x.Id);
            #endregion

            #region props
            modelBuilder.Entity<RegisterConstantOperation>()
                .Property(x => x.OpHash)
                .IsFixedLength(true)
                .HasMaxLength(51)
                .IsRequired();

            modelBuilder.Entity<RegisterConstantOperation>()
                .Property(x => x.Address)
                .HasMaxLength(54); // expr

            // shadow property
            modelBuilder.Entity<RegisterConstantOperation>()
                .Property<string>("Extras")
                .HasColumnType("jsonb");
            #endregion

            #region indexes
            modelBuilder.Entity<RegisterConstantOperation>()
                .HasIndex(x => x.Level);

            modelBuilder.Entity<RegisterConstantOperation>()
                .HasIndex(x => x.OpHash);

            modelBuilder.Entity<RegisterConstantOperation>()
                .HasIndex(x => x.SenderId);

            modelBuilder.Entity<RegisterConstantOperation>()
                .HasIndex(x => x.Address)
                .HasFilter($@"""{nameof(RegisterConstantOperation.Address)}"" IS NOT NULL");
            #endregion
        }
    }
}
