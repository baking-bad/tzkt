using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Tzkt.Data.Models
{
    public class Proposal
    {
        public int Id { get; set; }
        public string Hash { get; set; }
        public ProposalStatus Status { get; set; }

        public int InitiatorId { get; set; }

        public int ProposalPeriodId { get; set; }
        public int? ExplorationPeriodId { get; set; }
        public int? TestingPeriodId { get; set; }
        public int? PromotionPeriodId { get; set; }

        #region relations
        [ForeignKey(nameof(InitiatorId))]
        public Delegate Initiator { get; set; }

        [ForeignKey(nameof(ProposalPeriodId))]
        public ProposalPeriod ProposalPeriod { get; set; }

        [ForeignKey(nameof(ExplorationPeriodId))]
        public ExplorationPeriod ExplorationPeriod { get; set; }

        [ForeignKey(nameof(TestingPeriodId))]
        public TestingPeriod TestingPeriod { get; set; }

        [ForeignKey(nameof(PromotionPeriodId))]
        public PromotionPeriod PromotionPeriod { get; set; }
        #endregion

        #region indirect relations
        public List<BallotOperation> Ballots { get; set; }
        public List<ProposalOperation> Proposings { get; set; }
        #endregion
    }

    public static class ProposalModel
    {
        public static void BuildProposalModel(this ModelBuilder modelBuilder)
        {
            #region keys
            modelBuilder.Entity<Proposal>()
                .HasKey(x => x.Id);
            #endregion

            #region props
            modelBuilder.Entity<Proposal>()
                .Property(nameof(Proposal.Hash))
                .IsFixedLength(true)
                .HasMaxLength(51);
            #endregion

            #region relations
            modelBuilder.Entity<Proposal>()
                .HasOne(x => x.Initiator)
                .WithMany(x => x.PushedProposals)
                .HasForeignKey(x => x.InitiatorId);

            modelBuilder.Entity<Proposal>()
                .HasOne(x => x.ProposalPeriod)
                .WithMany(x => x.Candidates)
                .HasForeignKey(x => x.ProposalPeriodId);

            modelBuilder.Entity<Proposal>()
                .HasOne(x => x.ExplorationPeriod)
                .WithOne(x => x.Proposal)
                .HasForeignKey<Proposal>(x => x.ExplorationPeriodId);

            modelBuilder.Entity<Proposal>()
                .HasOne(x => x.TestingPeriod)
                .WithOne(x => x.Proposal)
                .HasForeignKey<Proposal>(x => x.TestingPeriodId);

            modelBuilder.Entity<Proposal>()
                .HasOne(x => x.PromotionPeriod)
                .WithOne(x => x.Proposal)
                .HasForeignKey<Proposal>(x => x.PromotionPeriodId);
            #endregion
        }
    }

    public enum ProposalStatus
    {
        Active,
        Applied,
        Declined
    }
}
