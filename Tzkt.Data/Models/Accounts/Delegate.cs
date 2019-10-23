using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Tzkt.Data.Models
{
    public class Delegate : User
    {
        public int? ActivationLevel { get; set; }
        public int? DeactivationLevel { get; set; }

        public long FrozenDeposits { get; set; }
        public long FrozenRewards { get; set; }
        public long FrozenFees { get; set; }

        public int Delegators { get; set; }
        public long StakingBalance { get; set; }

        #region relations
        [ForeignKey(nameof(ActivationLevel))]
        public Block ActivationBlock { get; set; }

        [ForeignKey(nameof(DeactivationLevel))]
        public Block DeactivationBlock { get; set; }
        #endregion

        #region indirect relations
        public List<Account> DelegatedAccounts { get; set; }

        public List<Block> BakedBlocks { get; set; }
        public List<Proposal> PushedProposals { get; set; }

        //public List<BakingRight> BakingRights { get; set; }
        //public List<EndorsingRight> EndorsingRights { get; set; }
        //public List<BakingCycle> BakingCycles { get; set; }
        //public List<DelegatorSnapshot> DelegatorsSnapshots { get; set; }

        public List<EndorsementOperation> Endorsements { get; set; }

        public List<BallotOperation> Ballots { get; set; }
        public List<ProposalOperation> Proposals { get; set; }

        public List<DoubleBakingOperation> SentDoubleBakingAccusations { get; set; }
        public List<DoubleBakingOperation> ReceivedDoubleBakingAccusations { get; set; }
        public List<DoubleEndorsingOperation> SentDoubleEndorsingAccusations { get; set; }
        public List<DoubleEndorsingOperation> ReceivedDoubleEndorsingAccusations { get; set; }
        public List<NonceRevelationOperation> SentRevelations { get; set; }

        public List<DelegationOperation> ReceivedDelegations { get; set; }
        public List<OriginationOperation> DelegatedOriginations { get; set; }
        #endregion
    }

    public static class DelegateModel
    {
        public static void BuildDelegateModel(this ModelBuilder modelBuilder)
        {
            #region relations
            modelBuilder.Entity<Delegate>()
                .HasOne(x => x.ActivationBlock)
                .WithMany(x => x.ActivatedDelegates)
                .HasForeignKey(x => x.ActivationLevel)
                .HasPrincipalKey(x => x.Level)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Delegate>()
                .HasOne(x => x.DeactivationBlock)
                .WithMany(x => x.DeactivatedDelegates)
                .HasForeignKey(x => x.DeactivationLevel)
                .HasPrincipalKey(x => x.Level)
                .OnDelete(DeleteBehavior.SetNull);
            #endregion
        }
    }
}
