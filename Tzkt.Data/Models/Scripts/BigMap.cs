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
        None                = 0b_0000,
        TokenMetadata       = 0b_0001, // tzip-12
        Metadata            = 0b_0010, // tzip-16
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
