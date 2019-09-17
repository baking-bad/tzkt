using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Tzkt.Data.Models
{
    public abstract class VotingPeriod
    {
        public int Id { get; set; }
        public int EpochId { get; set; }
        public VotingPeriods Kind { get; set; }
        public int StartLevel { get; set; }
        public int EndLevel { get; set; }

        #region relations
        [ForeignKey(nameof(EpochId))]
        public VotingEpoch Epoch { get; set; }
        #endregion

        #region indirect relations
        public List<BallotOperation> Ballots { get; set; }
        public List<ProposalOperation> Proposals { get; set; }
        #endregion
    }

    public static class VotingPeriodModel
    {
        public static void BuildVotingPeriodModel(this ModelBuilder modelBuilder)
        {
            #region keys
            modelBuilder.Entity<VotingPeriod>()
                .HasKey(x => x.Id);
            #endregion

            #region props
            modelBuilder.Entity<VotingPeriod>()
                .HasDiscriminator<VotingPeriods>(nameof(VotingPeriod.Kind))
                .HasValue<ProposalPeriod>(VotingPeriods.Proposal)
                .HasValue<ExplorationPeriod>(VotingPeriods.Exploration)
                .HasValue<TestingPeriod>(VotingPeriods.Testing)
                .HasValue<PromotionPeriod>(VotingPeriods.Promotion);
            #endregion

            #region relations
            modelBuilder.Entity<VotingPeriod>()
                .HasOne(x => x.Epoch)
                .WithMany(x => x.Periods)
                .HasForeignKey(x => x.EpochId);
            #endregion
        }
    }

    public enum VotingPeriods
    {
        Proposal,
        Exploration,
        Testing,
        Promotion
    }
}
