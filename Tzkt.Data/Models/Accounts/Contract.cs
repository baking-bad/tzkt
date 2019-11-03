using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Tzkt.Data.Models
{
    public class Contract : Account
    {
        public int? ManagerId { get; set; }
        public int? WeirdDelegateId { get; set; }

        #region relations
        [ForeignKey(nameof(ManagerId))]
        public Account Manager { get; set; }

        [ForeignKey(nameof(WeirdDelegateId))]
        public User WeirdDelegate { get; set; }
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
                .WithMany(x => x.OriginatedContracts)
                .HasForeignKey(x => x.ManagerId);
            #endregion
        }
    }
}
