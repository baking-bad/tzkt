﻿using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace Tzkt.Data.Models
{
    public class Domain
    {
        public required int Id { get; set; }
        public required int Level { get; set; }
        public required string Name { get; set; }
        public required string Owner { get; set; }
        public string? Address { get; set; }

        public bool Reverse { get; set; }
        public DateTime Expiration { get; set; }
        public JsonElement? Data { get; set; }

        public required int FirstLevel { get; set; }
        public required int LastLevel { get; set; }
    }

    public static class DomainModel
    {
        public static void BuildDomainModel(this ModelBuilder modelBuilder)
        {
            #region keys
            modelBuilder.Entity<Domain>()
                .HasKey(x => x.Id);
            #endregion

            #region indexes
            modelBuilder.Entity<Domain>()
                .HasIndex(x => x.Name);

            modelBuilder.Entity<Domain>()
                .HasIndex(x => x.Address);

            modelBuilder.Entity<Domain>()
                .HasIndex(x => x.Owner);

            modelBuilder.Entity<Domain>()
                .HasIndex(x => x.Level);

            modelBuilder.Entity<Domain>()
                .HasIndex(x => x.FirstLevel);

            modelBuilder.Entity<Domain>()
                .HasIndex(x => x.LastLevel);
            #endregion
        }
    }
}
