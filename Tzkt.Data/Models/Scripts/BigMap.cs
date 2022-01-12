using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Netezos.Contracts;
using Netezos.Encoding;

namespace Tzkt.Data.Models
{
    public class BigMap
    {
        public int Id { get; set; }
        public int Ptr { get; set; }
        public int ContractId { get; set; }
        public string StoragePath { get; set; }
        public bool Active { get; set; }

        public byte[] KeyType { get; set; }
        public byte[] ValueType { get; set; }

        public int FirstLevel { get; set; }
        public int LastLevel { get; set; }
        public int TotalKeys { get; set; }
        public int ActiveKeys { get; set; }
        public int Updates { get; set; }

        public BigMapTag Tags { get; set; }

        #region schema
        BigMapSchema _Schema = null;
        public BigMapSchema Schema
        {
            get
            {
                _Schema ??= new BigMapSchema(new MichelinePrim
                {
                    Prim = PrimType.big_map,
                    Args = new List<IMicheline>
                    {
                        Micheline.FromBytes(KeyType),
                        Micheline.FromBytes(ValueType)
                    }
                });
                return _Schema;
            }
        }
        #endregion
    }

    [Flags]
    public enum BigMapTag
    {
        None            = 0b_0000_0000_0000_0000_0000,
        Persistent      = 0b_0000_0000_0000_0000_0001, // is not a list/set/map item
        Metadata        = 0b_0000_0000_0000_0000_0010, // big_map %metadata string bytes
        TokenMetadata   = 0b_0000_0000_0000_0000_0100, // big_map %token_metadata nat (pair nat (map string bytes))

        Ledger          = 0b_0000_0000_0000_0001_0000, // ledger
        Ledger1         = 0b_0000_0000_0000_0011_0000, // big_map address nat
        Ledger2         = 0b_0000_0000_0000_0101_0000, // big_map nat address
        Ledger3         = 0b_0000_0000_0000_1001_0000, // big_map (pair address nat) nat
        Ledger4         = 0b_0000_0000_0001_0001_0000, // big_map (pair nat address) nat
        Ledger5         = 0b_0000_0000_0010_0001_0000, // big_map address (pair nat (map address nat))
        Ledger6         = 0b_0000_0000_0100_0001_0000, // big_map address (pair (map address nat) nat)
        Ledger7         = 0b_0000_0000_1000_0001_0000, // big_map bytes bytes (tzBTC)
        Ledger8         = 0b_0000_0001_0000_0001_0000, // big_map address { ... balance ... }
        Ledger9         = 0b_0000_0010_0000_0001_0000, // big_map (pair address nat) { ... balance ... }
        Ledger10        = 0b_0000_0100_0000_0001_0000, // big_map (pair nat address) { ... balance ... }
        Ledger11        = 0b_0000_1000_0000_0001_0000, // big_map address (pair (map nat nat) (set address)) (QUIPU)
        Ledger12        = 0b_0001_0000_0000_0001_0000, // big_map bytes (pair ...) (Tezos Domains)
        LedgerTypes     = 0b_0001_1111_1111_1110_0000,
        LedgerMask      = 0b_0001_1111_1111_1111_0000,
        LedgerNft       = 0b_0001_0000_0000_0100_0000,
    }

    public static class BigMapModel
    {
        public static void BuildBigMapModel(this ModelBuilder modelBuilder)
        {
            #region keys
            modelBuilder.Entity<BigMap>()
                .HasKey(x => x.Id);

            modelBuilder.Entity<BigMap>()
                .HasAlternateKey(x => x.Ptr);
            #endregion

            #region indexes
            modelBuilder.Entity<BigMap>()
                .HasIndex(x => x.Id)
                .IsUnique();

            modelBuilder.Entity<BigMap>()
                .HasIndex(x => x.Ptr)
                .IsUnique();

            modelBuilder.Entity<BigMap>()
                .HasIndex(x => x.ContractId);

            modelBuilder.Entity<BigMapKey>()
                .HasIndex(x => x.LastLevel);
            #endregion
        }
    }
}
