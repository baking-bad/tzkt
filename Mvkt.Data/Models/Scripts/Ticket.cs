﻿using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Mvkt.Data.Models
{
    public class Ticket
    {
        public long Id { get; set; }
        public int TicketerId { get; set; }

        public int FirstMinterId { get; set; }
        public int FirstLevel { get; set; }
        public int LastLevel { get; set; }

        public int TransfersCount { get; set; }
        public int BalancesCount { get; set; }
        public int HoldersCount { get; set; }

        public BigInteger TotalMinted { get; set; }
        public BigInteger TotalBurned { get; set; }
        public BigInteger TotalSupply { get; set; }

        public int TypeHash { get; set; }
        public int ContentHash { get; set; }
        
        public byte[] RawType { get; set; }
        public byte[] RawContent { get; set; }
        public string JsonContent { get; set; }
    }
    
    public static class TicketModel
    {
        public static void BuildTicketModel(this ModelBuilder modelBuilder)
        {
            #region keys
            modelBuilder.Entity<Ticket>()
                .HasKey(x => x.Id);
            #endregion

            #region props
            modelBuilder.Entity<Ticket>()
                .Property(x => x.JsonContent)
                .HasColumnType("jsonb");

            // TODO: switch to `numeric` type after migration to .NET 6
            var converter = new ValueConverter<BigInteger, string>(
                x => x.ToString(),
                x => BigInteger.Parse(x));
            
            modelBuilder.Entity<Ticket>()
                .Property(x => x.TotalMinted)
                .HasConversion(converter);

            modelBuilder.Entity<Ticket>()
                .Property(x => x.TotalBurned)
                .HasConversion(converter);

            modelBuilder.Entity<Ticket>()
                .Property(x => x.TotalSupply)
                .HasConversion(converter);
            #endregion

            #region indexes
            modelBuilder.Entity<Ticket>()
                .HasIndex(x => x.Id)
                .IsUnique();

            modelBuilder.Entity<Ticket>()
                .HasIndex(x => x.TicketerId);

            modelBuilder.Entity<Ticket>()
                .HasIndex(x => x.FirstMinterId);

            modelBuilder.Entity<Ticket>()
                .HasIndex(x => x.FirstLevel);

            modelBuilder.Entity<Ticket>()
                .HasIndex(x => x.LastLevel);

            modelBuilder.Entity<Ticket>()
                .HasIndex(x => x.TypeHash);

            modelBuilder.Entity<Ticket>()
                .HasIndex(x => x.ContentHash);

            modelBuilder.Entity<Ticket>()
                .HasIndex(x => new { x.TicketerId, x.TypeHash, x.ContentHash });

            modelBuilder.Entity<Ticket>()
                .HasIndex(x => x.JsonContent)
                .HasMethod("gin")
                .HasOperators("jsonb_path_ops");
            #endregion
        }
    }
}
