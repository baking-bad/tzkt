﻿using System.Numerics;
using Microsoft.EntityFrameworkCore;

namespace Tzkt.Data.Models
{
    public class Delegate : User
    {
        public int ActivationLevel { get; set; }
        public int DeactivationLevel { get; set; }

        public long StakingBalance { get; set; }
        public long DelegatedBalance { get; set; }
        public int DelegatorsCount { get; set; }

        public long OwnStakedBalance { get; set; }
        public long ExternalStakedBalance { get; set; }
        public BigInteger? IssuedPseudotokens { get; set; }
        public int StakersCount { get; set; }

        public long ExternalUnstakedBalance { get; set; }
        public long RoundingError { get; set; }

        public long? FrozenDepositLimit { get; set; }
        public long? LimitOfStakingOverBaking { get; set; }
        public long? EdgeOfBakingOverStaking { get; set; }

        public int BlocksCount { get; set; }
        public int EndorsementsCount { get; set; }
        public int PreendorsementsCount { get; set; }
        public int BallotsCount { get; set; }
        public int ProposalsCount { get; set; }
        public int DalEntrapmentEvidenceOpsCount { get; set; }
        public int DoubleBakingCount { get; set; }
        public int DoubleEndorsingCount { get; set; }
        public int DoublePreendorsingCount { get; set; }
        public int NonceRevelationsCount { get; set; }
        public int VdfRevelationsCount { get; set; }
        public int RevelationPenaltiesCount { get; set; }
        public int EndorsingRewardsCount { get; set; }
        public int DalAttestationRewardsCount { get; set; }
        public int AutostakingOpsCount { get; set; }

        public int? SoftwareId { get; set; }
    }

    public static class DelegateModel
    {
        public static void BuildDelegateModel(this ModelBuilder modelBuilder)
        {
            #region indexes
            modelBuilder.Entity<Delegate>()
                .HasIndex(x => x.Staked, $"IX_{nameof(TzktContext.Accounts)}_{nameof(Delegate.Staked)}_Partial")
                .HasFilter($@"""{nameof(Account.Type)}"" = {(int)AccountType.Delegate}");

            modelBuilder.Entity<Delegate>()
                .HasIndex(x => x.DeactivationLevel, $"IX_{nameof(TzktContext.Accounts)}_{nameof(Delegate.DeactivationLevel)}_Partial")
                .HasFilter($@"""{nameof(Account.Type)}"" = {(int)AccountType.Delegate}");
            #endregion
        }
    }
}
