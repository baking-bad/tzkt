using System;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Tzkt.Data.Models
{
    public class Contract : Account
    {
        public ContractKind Kind { get; set; }
        public int TypeHash { get; set; }
        public int CodeHash { get; set; }
        public ContractTags Tags { get; set; }

        public bool? Spendable { get; set; }

        public int? CreatorId { get; set; }
        public int? ManagerId { get; set; }
        public int? WeirdDelegateId { get; set; }

        #region relations
        [ForeignKey(nameof(CreatorId))]
        public Account Creator { get; set; }

        [ForeignKey(nameof(ManagerId))]
        public User Manager { get; set; }

        [ForeignKey(nameof(WeirdDelegateId))]
        public User WeirdDelegate { get; set; }
        #endregion
    }

    public enum ContractKind : byte
    {
        DelegatorContract,
        SmartContract,
        Asset
    }

    [Flags]
    public enum ContractTags
    {
        None        = 0b_0000,
        FA1         = 0b_0001, // tzip-5
        FA12        = 0b_0011, // tzip-7
        FA2         = 0b_0100, // tzip-12
        Constants   = 0b_1000, // refers at least one global constant
    }

    public static class ContractModel
    { 
        public static void BuildContractModel(this ModelBuilder modelBuilder)
        {
            #region indexes
            modelBuilder.Entity<Contract>()
                .HasIndex(x => new { x.Type, x.Kind })
                .HasFilter(@"""Type"" = 2");

            modelBuilder.Entity<Contract>()
                .HasIndex(x => x.CreatorId);

            modelBuilder.Entity<Contract>()
                .HasIndex(x => x.ManagerId);

            modelBuilder.Entity<Contract>()
                .HasIndex(x => x.TypeHash);

            modelBuilder.Entity<Contract>()
                .HasIndex(x => x.CodeHash);
            #endregion
        }
    }
}
