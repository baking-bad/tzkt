using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Tzkt.Data.Models
{
    public class Delegate : User
    {
        public int ActivationLevel { get; set; }
        public int DeactivationLevel { get; set; }

        public long? FrozenDepositLimit { get; set; }
        public long FrozenDeposit { get; set; }
        public long StakingBalance { get; set; }
        public long DelegatedBalance { get; set; }
        public int DelegatorsCount { get; set; }

        public int BlocksCount { get; set; }
        public int EndorsementsCount { get; set; }
        public int PreendorsementsCount { get; set; }
        public int BallotsCount { get; set; }
        public int ProposalsCount { get; set; }
        public int DoubleBakingCount { get; set; }
        public int DoubleEndorsingCount { get; set; }
        public int DoublePreendorsingCount { get; set; }
        public int NonceRevelationsCount { get; set; }
        public int VdfRevelationsCount { get; set; }
        public int RevelationPenaltiesCount { get; set; }
        public int EndorsingRewardsCount { get; set; }

        public int? SoftwareId { get; set; }

        #region relations
        [ForeignKey(nameof(SoftwareId))]
        public Software Software { get; set; }
        #endregion

        #region indirect relations
        public List<Account> DelegatedAccounts { get; set; }
        #endregion
    }

    public static class DelegateModel
    {
        public static void BuildDelegateModel(this ModelBuilder modelBuilder)
        {
            #region indexes
            modelBuilder.Entity<Delegate>()
                .HasIndex(x => new { x.Type, x.Staked })
                .HasFilter(@"""Type"" = 1");
            #endregion

            #region relations
            modelBuilder.Entity<Delegate>()
                .HasOne(x => x.Software)
                .WithMany()
                .HasForeignKey(x => x.SoftwareId)
                .HasPrincipalKey(x => x.Id);
            #endregion
        }
    }
}
