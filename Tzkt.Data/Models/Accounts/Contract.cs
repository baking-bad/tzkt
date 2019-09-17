using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Tzkt.Data.Models
{
    public class Contract : Account
    {
        public int? ManagerId { get; set; }
        public int? OriginatorId { get; set; }

        public bool Delegatable { get; set; }
        public bool Spendable { get; set; }

        #region relations
        [ForeignKey(nameof(ManagerId))]
        public User Manager { get; set; }

        [ForeignKey(nameof(OriginatorId))]
        public Account Originator { get; set; }
        #endregion

        #region indirect relations
        public OriginationOperation Origination { get; set; }
        #endregion
    }

    public static class ContractModel
    { 
        public static void BuildContractModel(this ModelBuilder modelBuilder)
        {
            #region relations
            modelBuilder.Entity<Contract>()
                .HasOne(x => x.Manager)
                .WithMany(x => x.ManagedContracts)
                .HasForeignKey(x => x.ManagerId);

            modelBuilder.Entity<Contract>()
                .HasOne(x => x.Originator)
                .WithMany(x => x.OriginatedContracts)
                .HasForeignKey(x => x.OriginatorId);
            #endregion
        }
    }
}
