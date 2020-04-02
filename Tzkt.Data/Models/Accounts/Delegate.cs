using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Tzkt.Data.Models
{
    public class Delegate : User
    {
        public int ActivationLevel { get; set; }
        public int DeactivationLevel { get; set; }

        public long FrozenDeposits { get; set; }
        public long FrozenRewards { get; set; }
        public long FrozenFees { get; set; }

        public int DelegatorsCount { get; set; }
        public long StakingBalance { get; set; }

        public int BlocksCount { get; set; }
        public int EndorsementsCount { get; set; }
        public int BallotsCount { get; set; }
        public int ProposalsCount { get; set; }
        public int DoubleBakingCount { get; set; }
        public int DoubleEndorsingCount { get; set; }
        public int NonceRevelationsCount { get; set; }
        public int RevelationPenaltiesCount { get; set; }

        #region indirect relations
        public List<Account> DelegatedAccounts { get; set; }
        #endregion
    }

    public static class DelegateModel
    {
        public static void BuildDelegateModel(this ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Contract>()
                .HasIndex(x => new { x.Type, x.Staked })
                .HasFilter(@"""Type"" = 1");
        }
    }
}
