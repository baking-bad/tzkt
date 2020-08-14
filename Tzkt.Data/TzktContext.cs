using Microsoft.EntityFrameworkCore;

using Tzkt.Data.Models;

namespace Tzkt.Data
{
    public class TzktContext : DbContext
    {
        #region app state
        public DbSet<AppState> AppState { get; set; }
        #endregion

        #region accounts
        public DbSet<Commitment> Commitments { get; set; }
        public DbSet<Account> Accounts { get; set; }
        public DbSet<Contract> Contracts { get; set; }
        public DbSet<Delegate> Delegates { get; set; }
        public DbSet<User> Users { get; set; }
        #endregion

        #region blocks
        public DbSet<Block> Blocks { get; set; }
        public DbSet<Protocol> Protocols { get; set; }
        #endregion

        #region operations
        public DbSet<ActivationOperation> ActivationOps { get; set; }
        public DbSet<BallotOperation> BallotOps { get; set; }
        public DbSet<DelegationOperation> DelegationOps { get; set; }
        public DbSet<DoubleBakingOperation> DoubleBakingOps { get; set; }
        public DbSet<DoubleEndorsingOperation> DoubleEndorsingOps { get; set; }
        public DbSet<EndorsementOperation> EndorsementOps { get; set; }
        public DbSet<NonceRevelationOperation> NonceRevelationOps { get; set; }
        public DbSet<OriginationOperation> OriginationOps { get; set; }
        public DbSet<ProposalOperation> ProposalOps { get; set; }
        public DbSet<RevealOperation> RevealOps { get; set; }
        public DbSet<TransactionOperation> TransactionOps { get; set; }

        public DbSet<MigrationOperation> MigrationOps { get; set; }
        public DbSet<RevelationPenaltyOperation> RevelationPenaltyOps { get; set; }
        #endregion

        #region voting
        public DbSet<Proposal> Proposals { get; set; }
        public DbSet<VotingEpoch> VotingEpoches { get; set; }
        public DbSet<VotingPeriod> VotingPeriods { get; set; }
        public DbSet<ProposalPeriod> ProposalPeriods { get; set; }
        public DbSet<ExplorationPeriod> ExplorationPeriods { get; set; }
        public DbSet<TestingPeriod> TestingPeriods { get; set; }
        public DbSet<PromotionPeriod> PromotionPeriods { get; set; }
        public DbSet<VotingSnapshot> VotingSnapshots { get; set; }
        #endregion

        #region baking
        public DbSet<Cycle> Cycles { get; set; }
        public DbSet<BakerCycle> BakerCycles { get; set; }
        public DbSet<DelegatorCycle> DelegatorCycles { get; set; }
        public DbSet<BakingRight> BakingRights { get; set; }
        public DbSet<SnapshotBalance> SnapshotBalances { get; set; }
        #endregion

        #region quotes
        public DbSet<Quote> Quotes { get; set; }
        #endregion

        public TzktContext(DbContextOptions options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            #region app state
            modelBuilder.BuildAppStateModel();
            #endregion

            #region accounts
            modelBuilder.BuildCommitmentModel();
            modelBuilder.BuildAccountModel();
            modelBuilder.BuildContractModel();
            modelBuilder.BuildDelegateModel();
            modelBuilder.BuildUserModel();
            #endregion

            #region block
            modelBuilder.BuildBlockModel();
            modelBuilder.BuildProtocolModel();
            #endregion

            #region operations
            modelBuilder.BuildActivationOperationModel();
            modelBuilder.BuildBallotOperationModel();
            modelBuilder.BuildDelegationOperationModel();
            modelBuilder.BuildDoubleBakingOperationModel();
            modelBuilder.BuildDoubleEndorsingOperationModel();
            modelBuilder.BuildEndorsementOperationModel();
            modelBuilder.BuildNonceRevelationOperationModel();
            modelBuilder.BuildOriginationOperationModel();
            modelBuilder.BuildProposalOperationModel();
            modelBuilder.BuildRevealOperationModel();
            modelBuilder.BuildTransactionOperationModel();

            modelBuilder.BuildMigrationOperationModel();
            modelBuilder.BuildRevelationPenaltyOperationModel();
            #endregion

            #region voting
            modelBuilder.BuildProposalModel();
            modelBuilder.BuildVotingEpochModel();
            modelBuilder.BuildVotingPeriodModel();
            modelBuilder.BuildProposalPeriodModel();
            modelBuilder.BuildExplorationPeriodModel();
            modelBuilder.BuildTestingPeriodModel();
            modelBuilder.BuildPromotionPeriodModel();
            modelBuilder.BuildVotingSnapshotModel();
            #endregion

            #region baking
            modelBuilder.BuildCycleModel();
            modelBuilder.BuildBakerCycleModel();
            modelBuilder.BuildDelegatorCycleModel();
            modelBuilder.BuildBakingRightModel();
            modelBuilder.BuildSnapshotBalanceModel();
            #endregion

            #region quotes
            modelBuilder.BuildQuoteModel();
            #endregion
        }
    }
}
