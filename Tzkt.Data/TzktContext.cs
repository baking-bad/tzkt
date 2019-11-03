﻿using Microsoft.EntityFrameworkCore;

using Tzkt.Data.Models;

namespace Tzkt.Data
{
    public class TzktContext : DbContext
    {
        #region app state
        public DbSet<AppState> AppState { get; set; }
        #endregion

        #region accounts
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
        #endregion

        #region voting
        public DbSet<Proposal> Proposals { get; set; }
        public DbSet<VotingEpoch> VotingEpoches { get; set; }
        public DbSet<VotingPeriod> VotingPeriods { get; set; }
        public DbSet<ProposalPeriod> ProposalPeriods { get; set; }
        public DbSet<ExplorationPeriod> ExplorationPeriods { get; set; }
        public DbSet<TestingPeriod> TestingPeriods { get; set; }
        public DbSet<PromotionPeriod> PromotionPeriods { get; set; }
        #endregion

        //public DbSet<Cycle> Cycles { get; set; }
        //public DbSet<BalanceSnapshot> BalanceSnapshots { get; set; }
        //public DbSet<BakingRight> BakingRights { get; set; }
        //public DbSet<EndorsingRight> EndorsingRights { get; set; }
        //public DbSet<BakingCycle> BakerCycles { get; set; }
        //public DbSet<DelegatorSnapshot> DelegatorSnapshots { get; set; }

        public TzktContext(DbContextOptions options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            #region app state
            modelBuilder.BuildAppStateModel();
            #endregion

            #region accounts
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
            #endregion

            #region voting
            modelBuilder.BuildProposalModel();
            modelBuilder.BuildVotingEpochModel();
            modelBuilder.BuildVotingPeriodModel();
            modelBuilder.BuildProposalPeriodModel();
            modelBuilder.BuildExplorationPeriodModel();
            modelBuilder.BuildTestingPeriodModel();
            modelBuilder.BuildPromotionPeriodModel();
            #endregion
        }
    }
}
