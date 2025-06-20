﻿using Microsoft.EntityFrameworkCore;

namespace Tzkt.Data.Models
{
    public class Software
    {
        public required int Id { get; set; }
        public required string ShortHash { get; set; }
        public required int FirstLevel { get; set; }
        public required int LastLevel { get; set; }

        public int BlocksCount { get; set; }
    }

    public static class SoftwareModel
    {
        public static void BuildSoftwareModel(this ModelBuilder modelBuilder)
        {
            #region keys
            modelBuilder.Entity<Software>()
                .HasKey(x => x.Id);
            #endregion

            #region props
            modelBuilder.Entity<Software>()
                .Property(x => x.ShortHash)
                .IsFixedLength(true)
                .HasMaxLength(8)
                .IsRequired();

            // shadow property
            modelBuilder.Entity<Software>()
                .Property<string>("Extras")
                .HasColumnType("jsonb");
            #endregion
        }
    }
}
