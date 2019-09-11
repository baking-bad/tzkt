using System;
using Microsoft.EntityFrameworkCore;

using Tzkt.Data.Models;

namespace Tzkt.Data
{
    public class TzktContext : DbContext
    {
        public DbSet<AppState> AppState { get; set; }

        public DbSet<Block> Blocks { get; set; }
        public DbSet<Contract> Contracts { get; set; }
        public DbSet<BalanceSnapshot> BalanceSnapshots { get; set; }

        public DbSet<BakingRight> BakingRights { get; set; }
        public DbSet<EndorsingRight> EndorsingRights { get; set; }

        public DbSet<BakerCycle> BakerCycles { get; set; }
        public DbSet<DelegatorSnapshot> DelegatorSnapshots { get; set; }
        public DbSet<Cycle> Cycles { get; set; }

        public DbSet<VotingEpoch> VotingEpoches { get; set; }
        public DbSet<VotingPeriod> VotingPeriods { get; set; }
        public DbSet<ExplorationPeriod> ExplorationPeriods { get; set; }
        public DbSet<PromotionPeriod> PromotionPeriods { get; set; }
        public DbSet<ProposalPeriod> ProposalPeriods { get; set; }
        public DbSet<TestingPeriod> TestingPeriods { get; set; }
        public DbSet<Proposal> Proposals { get; set; }

        public DbSet<Protocol> Protocols { get; set; }

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

        public TzktContext(DbContextOptions options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            #region app state
            modelBuilder.Entity<AppState>().HasData(
                new AppState
                {
                    Id = -1,
                    Level = -1,
                    Timestamp = DateTime.MinValue,
                    Protocol = "",
                    Hash = "",
                });
            #endregion

            #region block
            #region indexes
            modelBuilder.Entity<Block>()
                .HasIndex(x => x.Level)
                .IsUnique();

            modelBuilder.Entity<Block>()
                .HasIndex(x => x.Hash)
                .IsUnique();
            #endregion
            #region keys
            modelBuilder.Entity<Block>()
                .HasKey(x => x.Id);

            modelBuilder.Entity<Block>()
                .HasAlternateKey(x => x.Level);
            #endregion
            #region props
            modelBuilder.Entity<Block>()
                .Property(x => x.Hash)
                .HasMaxLength(51)
                .IsFixedLength()
                .IsRequired();
            #endregion
            #region relations
            modelBuilder.Entity<Block>()
                .HasOne(x => x.Baker)
                .WithMany(x => x.BakedBlocks)
                .HasForeignKey(x => x.BakerId);

            modelBuilder.Entity<Block>()
                .HasOne(x => x.Protocol)
                .WithMany(x => x.Blocks)
                .HasForeignKey(x => x.ProtocolId);

            modelBuilder.Entity<Block>()
                .HasOne(x => x.Revelation)
                .WithOne(x => x.RevelationBlock)
                .HasForeignKey<Block>(x => x.RevelationId)
                .HasPrincipalKey<NonceRevelationOperation>(x => x.RevelationLevel);
            #endregion
            #endregion

            #region contracts
            #region indexes
            modelBuilder.Entity<Contract>()
                .HasIndex(x => x.Id)
                .IsUnique();

            modelBuilder.Entity<Contract>()
                .HasIndex(x => x.Address)
                .IsUnique();

            modelBuilder.Entity<Contract>()
                .HasIndex(x => x.DelegateId);

            modelBuilder.Entity<Contract>()
                .HasIndex(x => x.ManagerId);
            #endregion
            #region keys
            modelBuilder.Entity<Contract>()
                .HasKey(x => x.Id);

            modelBuilder.Entity<Contract>()
                .HasAlternateKey(x => x.Address);
            #endregion
            #region props
            modelBuilder.Entity<Contract>()
                .Property(x => x.Address)
                .HasMaxLength(36)
                .IsFixedLength()
                .IsRequired();

            modelBuilder.Entity<Contract>()
                .Property(x => x.PublicKey)
                .HasMaxLength(65);
            #endregion
            #region relations
            modelBuilder.Entity<Contract>()
                .HasOne(x => x.Delegate)
                .WithMany(x => x.DelegatedContracts)
                .HasForeignKey(x => x.DelegateId);

            modelBuilder.Entity<Contract>()
                .HasOne(x => x.Manager)
                .WithMany(x => x.OriginatedContracts)
                .HasForeignKey(x => x.ManagerId);
            #endregion
            #endregion

            #region activations
            #region indexes
            modelBuilder.Entity<ActivationOperation>()
                .HasIndex(x => x.AccountId)
                .IsUnique();

            modelBuilder.Entity<ActivationOperation>()
                .HasIndex(x => x.Level);

            modelBuilder.Entity<ActivationOperation>()
                .HasIndex(x => x.OpHash);
            #endregion
            #region keys
            modelBuilder.Entity<ActivationOperation>()
                .HasKey(x => x.Id);
            #endregion
            #region props
            modelBuilder.Entity<ActivationOperation>()
                .Property(x => x.OpHash)
                .HasMaxLength(51)
                .IsFixedLength()
                .IsRequired();
            #endregion
            #region relations
            modelBuilder.Entity<ActivationOperation>()
                .HasOne(x => x.Block)
                .WithMany(x => x.Activations)
                .HasForeignKey(x => x.Level)
                .HasPrincipalKey(x => x.Level);

            modelBuilder.Entity<ActivationOperation>()
                .HasOne(x => x.Account)
                .WithOne(x => x.Activation)
                .HasForeignKey<ActivationOperation>(x => x.AccountId);
            #endregion
            #endregion

            #region ballots
            #region indexes
            modelBuilder.Entity<BallotOperation>()
                .HasIndex(x => x.Level);

            modelBuilder.Entity<BallotOperation>()
                .HasIndex(x => x.OpHash);

            modelBuilder.Entity<BallotOperation>()
                .HasIndex(x => x.SenderId);
            #endregion
            #region keys
            modelBuilder.Entity<BallotOperation>()
                .HasKey(x => x.Id);
            #endregion
            #region props
            modelBuilder.Entity<BallotOperation>()
                .Property(x => x.OpHash)
                .HasMaxLength(51)
                .IsFixedLength()
                .IsRequired();
            #endregion
            #region relations
            modelBuilder.Entity<BallotOperation>()
                .HasOne(x => x.Block)
                .WithMany(x => x.Ballots)
                .HasForeignKey(x => x.Level)
                .HasPrincipalKey(x => x.Level);

            modelBuilder.Entity<BallotOperation>()
                .HasOne(x => x.Sender)
                .WithMany(x => x.Ballots)
                .HasForeignKey(x => x.SenderId);

            modelBuilder.Entity<BallotOperation>()
                .HasOne(x => x.Proposal)
                .WithMany(x => x.Ballots)
                .HasForeignKey(x => x.ProposalId);

            modelBuilder.Entity<BallotOperation>()
                .HasOne(x => x.Period)
                .WithMany(x => x.Ballots)
                .HasForeignKey(x => x.PeriodId);
            #endregion
            #endregion

            #region delegations
            #region indexes
            modelBuilder.Entity<DelegationOperation>()
                .HasIndex(x => x.Level);

            modelBuilder.Entity<DelegationOperation>()
                .HasIndex(x => x.OpHash);

            modelBuilder.Entity<DelegationOperation>()
                .HasIndex(x => x.DelegateId);

            modelBuilder.Entity<DelegationOperation>()
                .HasIndex(x => x.SenderId);
            #endregion
            #region keys
            modelBuilder.Entity<DelegationOperation>()
                .HasKey(x => x.Id);
            #endregion
            #region props
            modelBuilder.Entity<DelegationOperation>()
                .Property(x => x.OpHash)
                .HasMaxLength(51)
                .IsFixedLength()
                .IsRequired();
            #endregion
            #region relations
            modelBuilder.Entity<DelegationOperation>()
                .HasOne(x => x.Block)
                .WithMany(x => x.Delegations)
                .HasForeignKey(x => x.Level)
                .HasPrincipalKey(x => x.Level);

            modelBuilder.Entity<DelegationOperation>()
                .HasOne(x => x.Sender)
                .WithMany(x => x.Delegations)
                .HasForeignKey(x => x.SenderId);

            modelBuilder.Entity<DelegationOperation>()
                .HasOne(x => x.Delegate)
                .WithMany(x => x.IncomingDelegations)
                .HasForeignKey(x => x.DelegateId);

            modelBuilder.Entity<DelegationOperation>()
                .HasOne(x => x.Parent)
                .WithMany(x => x.InternalDelegations)
                .HasForeignKey(x => x.ParentId);
            #endregion
            #endregion

            #region double bakings
            #region indexes
            modelBuilder.Entity<DoubleBakingOperation>()
                .HasIndex(x => x.Level);

            modelBuilder.Entity<DoubleBakingOperation>()
                .HasIndex(x => x.OpHash);

            modelBuilder.Entity<DoubleBakingOperation>()
                .HasIndex(x => x.AccuserId);

            modelBuilder.Entity<DoubleBakingOperation>()
                .HasIndex(x => x.OffenderId);
            #endregion
            #region keys
            modelBuilder.Entity<DoubleBakingOperation>()
                .HasKey(x => x.Id);
            #endregion
            #region props
            modelBuilder.Entity<DoubleBakingOperation>()
                .Property(x => x.OpHash)
                .HasMaxLength(51)
                .IsFixedLength()
                .IsRequired();
            #endregion
            #region relations
            modelBuilder.Entity<DoubleBakingOperation>()
                .HasOne(x => x.Block)
                .WithMany(x => x.DoubleBakings)
                .HasForeignKey(x => x.Level)
                .HasPrincipalKey(x => x.Level);

            modelBuilder.Entity<DoubleBakingOperation>()
                .HasOne(x => x.Accuser)
                .WithMany(x => x.DoubleBakingAccusations)
                .HasForeignKey(x => x.AccuserId);

            modelBuilder.Entity<DoubleBakingOperation>()
                .HasOne(x => x.Offender)
                .WithMany(x => x.DoubleBakings)
                .HasForeignKey(x => x.OffenderId);
            #endregion
            #endregion

            #region double endorsements
            #region indexes
            modelBuilder.Entity<DoubleEndorsingOperation>()
                .HasIndex(x => x.Level);

            modelBuilder.Entity<DoubleEndorsingOperation>()
                .HasIndex(x => x.OpHash);

            modelBuilder.Entity<DoubleEndorsingOperation>()
                .HasIndex(x => x.AccuserId);

            modelBuilder.Entity<DoubleEndorsingOperation>()
                .HasIndex(x => x.OffenderId);
            #endregion
            #region keys
            modelBuilder.Entity<DoubleEndorsingOperation>()
                .HasKey(x => x.Id);
            #endregion
            #region props
            modelBuilder.Entity<DoubleEndorsingOperation>()
                .Property(x => x.OpHash)
                .HasMaxLength(51)
                .IsFixedLength()
                .IsRequired();
            #endregion
            #region relations
            modelBuilder.Entity<DoubleEndorsingOperation>()
                .HasOne(x => x.Block)
                .WithMany(x => x.DoubleEndorsings)
                .HasForeignKey(x => x.Level)
                .HasPrincipalKey(x => x.Level);

            modelBuilder.Entity<DoubleEndorsingOperation>()
                .HasOne(x => x.Accuser)
                .WithMany(x => x.DoubleEndorsingAccusations)
                .HasForeignKey(x => x.AccuserId);

            modelBuilder.Entity<DoubleEndorsingOperation>()
                .HasOne(x => x.Offender)
                .WithMany(x => x.DoubleEndorsings)
                .HasForeignKey(x => x.OffenderId);
            #endregion
            #endregion

            #region endorsements
            #region indexes
            modelBuilder.Entity<EndorsementOperation>()
                .HasIndex(x => x.Level);

            modelBuilder.Entity<EndorsementOperation>()
                .HasIndex(x => x.OpHash);

            modelBuilder.Entity<EndorsementOperation>()
                .HasIndex(x => x.DelegateId);
            #endregion
            #region keys
            modelBuilder.Entity<EndorsementOperation>()
                .HasKey(x => x.Id);
            #endregion
            #region props
            modelBuilder.Entity<EndorsementOperation>()
                .Property(x => x.OpHash)
                .HasMaxLength(51)
                .IsFixedLength()
                .IsRequired();
            #endregion
            #region relations
            modelBuilder.Entity<EndorsementOperation>()
                .HasOne(x => x.Block)
                .WithMany(x => x.Endorsements)
                .HasForeignKey(x => x.Level)
                .HasPrincipalKey(x => x.Level);

            modelBuilder.Entity<EndorsementOperation>()
                .HasOne(x => x.Delegate)
                .WithMany(x => x.Endorsements)
                .HasForeignKey(x => x.DelegateId);
            #endregion
            #endregion

            #region nonce revelations
            #region indexes
            modelBuilder.Entity<NonceRevelationOperation>()
                .HasIndex(x => x.Level);

            modelBuilder.Entity<NonceRevelationOperation>()
                .HasIndex(x => x.OpHash);

            modelBuilder.Entity<NonceRevelationOperation>()
                .HasIndex(x => x.BakerId);

            modelBuilder.Entity<NonceRevelationOperation>()
                .HasIndex(x => x.RevelationLevel);
            #endregion
            #region keys
            modelBuilder.Entity<NonceRevelationOperation>()
                .HasKey(x => x.Id);
            modelBuilder.Entity<NonceRevelationOperation>()
                .HasAlternateKey(x => x.RevelationLevel);
            #endregion
            #region props
            modelBuilder.Entity<NonceRevelationOperation>()
                .Property(x => x.OpHash)
                .HasMaxLength(51)
                .IsFixedLength()
                .IsRequired();
            #endregion
            #region relations
            modelBuilder.Entity<NonceRevelationOperation>()
                .HasOne(x => x.Block)
                .WithMany(x => x.Revelations)
                .HasForeignKey(x => x.Level)
                .HasPrincipalKey(x => x.Level);

            modelBuilder.Entity<NonceRevelationOperation>()
                .HasOne(x => x.Baker)
                .WithMany(x => x.Revelations)
                .HasForeignKey(x => x.BakerId);
            #endregion
            #endregion

            #region originations
            #region indexes
            modelBuilder.Entity<OriginationOperation>()
                .HasIndex(x => x.Level);

            modelBuilder.Entity<OriginationOperation>()
                .HasIndex(x => x.OpHash);

            modelBuilder.Entity<OriginationOperation>()
                .HasIndex(x => x.ContractId);

            modelBuilder.Entity<OriginationOperation>()
                .HasIndex(x => x.DelegateId);

            modelBuilder.Entity<OriginationOperation>()
                .HasIndex(x => x.ManagerId);

            modelBuilder.Entity<OriginationOperation>()
                .HasIndex(x => x.SenderId);
            #endregion
            #region keys
            modelBuilder.Entity<OriginationOperation>()
                .HasKey(x => x.Id);
            #endregion
            #region props
            modelBuilder.Entity<OriginationOperation>()
                .Property(x => x.OpHash)
                .HasMaxLength(51)
                .IsFixedLength()
                .IsRequired();
            #endregion
            #region relations
            modelBuilder.Entity<OriginationOperation>()
                .HasOne(x => x.Block)
                .WithMany(x => x.Originations)
                .HasForeignKey(x => x.Level)
                .HasPrincipalKey(x => x.Level);

            modelBuilder.Entity<OriginationOperation>()
                .HasOne(x => x.Contract)
                .WithOne(x => x.Origination)
                .HasForeignKey<OriginationOperation>(x => x.ContractId);

            modelBuilder.Entity<OriginationOperation>()
                .HasOne(x => x.Delegate)
                .WithMany(x => x.DelegatedOriginations)
                .HasForeignKey(x => x.DelegateId);

            modelBuilder.Entity<OriginationOperation>()
                .HasOne(x => x.Manager)
                .WithMany(x => x.ManagedOriginations)
                .HasForeignKey(x => x.ManagerId);

            modelBuilder.Entity<OriginationOperation>()
                .HasOne(x => x.Sender)
                .WithMany(x => x.Originations)
                .HasForeignKey(x => x.SenderId);

            modelBuilder.Entity<OriginationOperation>()
                .HasOne(x => x.Parent)
                .WithMany(x => x.InternalOriginations)
                .HasForeignKey(x => x.ParentId);
            #endregion
            #endregion

            #region proposal ops
            #region indexes
            modelBuilder.Entity<ProposalOperation>()
                .HasIndex(x => x.Level);

            modelBuilder.Entity<ProposalOperation>()
                .HasIndex(x => x.OpHash);

            modelBuilder.Entity<ProposalOperation>()
                .HasIndex(x => x.SenderId);
            #endregion
            #region keys
            modelBuilder.Entity<ProposalOperation>()
                .HasKey(x => x.Id);
            #endregion
            #region props
            modelBuilder.Entity<ProposalOperation>()
                .Property(x => x.OpHash)
                .HasMaxLength(51)
                .IsFixedLength()
                .IsRequired();
            #endregion
            #region relations
            modelBuilder.Entity<ProposalOperation>()
                .HasOne(x => x.Block)
                .WithMany(x => x.Proposals)
                .HasForeignKey(x => x.Level)
                .HasPrincipalKey(x => x.Level);

            modelBuilder.Entity<ProposalOperation>()
                .HasOne(x => x.Sender)
                .WithMany(x => x.Proposals)
                .HasForeignKey(x => x.SenderId);

            modelBuilder.Entity<ProposalOperation>()
                .HasOne(x => x.Proposal)
                .WithMany(x => x.Proposals)
                .HasForeignKey(x => x.ProposalId);

            modelBuilder.Entity<ProposalOperation>()
                .HasOne(x => x.Period)
                .WithMany(x => x.Proposals)
                .HasForeignKey(x => x.PeriodId);
            #endregion
            #endregion

            #region reveals
            #region indexes
            modelBuilder.Entity<RevealOperation>()
                .HasIndex(x => x.Level);

            modelBuilder.Entity<RevealOperation>()
                .HasIndex(x => x.OpHash);

            modelBuilder.Entity<RevealOperation>()
                .HasIndex(x => x.SenderId);
            #endregion
            #region keys
            modelBuilder.Entity<RevealOperation>()
                .HasKey(x => x.Id);
            #endregion
            #region props
            modelBuilder.Entity<RevealOperation>()
                .Property(x => x.OpHash)
                .HasMaxLength(51)
                .IsFixedLength()
                .IsRequired();

            modelBuilder.Entity<RevealOperation>()
                .Property(x => x.PublicKey)
                .HasMaxLength(65)
                .IsRequired();
            #endregion
            #region relations
            modelBuilder.Entity<RevealOperation>()
                .HasOne(x => x.Block)
                .WithMany(x => x.Reveals)
                .HasForeignKey(x => x.Level)
                .HasPrincipalKey(x => x.Level);

            modelBuilder.Entity<RevealOperation>()
                .HasOne(x => x.Sender)
                .WithMany(x => x.Reveals)
                .HasForeignKey(x => x.SenderId);

            modelBuilder.Entity<RevealOperation>()
                .HasOne(x => x.Parent)
                .WithMany(x => x.InternalReveals)
                .HasForeignKey(x => x.ParentId);
            #endregion
            #endregion

            #region transactions
            #region indexes
            modelBuilder.Entity<TransactionOperation>()
                .HasIndex(x => x.Level);

            modelBuilder.Entity<TransactionOperation>()
                .HasIndex(x => x.OpHash);

            modelBuilder.Entity<TransactionOperation>()
                .HasIndex(x => x.SenderId);

            modelBuilder.Entity<TransactionOperation>()
                .HasIndex(x => x.TargetId);
            #endregion
            #region keys
            modelBuilder.Entity<TransactionOperation>()
                .HasKey(x => x.Id);
            #endregion
            #region props
            modelBuilder.Entity<TransactionOperation>()
                .Property(x => x.OpHash)
                .HasMaxLength(51)
                .IsFixedLength()
                .IsRequired();
            #endregion
            #region relations
            modelBuilder.Entity<TransactionOperation>()
                .HasOne(x => x.Block)
                .WithMany(x => x.Transactions)
                .HasForeignKey(x => x.Level)
                .HasPrincipalKey(x => x.Level);

            modelBuilder.Entity<TransactionOperation>()
                .HasOne(x => x.Parent)
                .WithMany(x => x.InternalTransactions)
                .HasForeignKey(x => x.ParentId);

            modelBuilder.Entity<TransactionOperation>()
                .HasOne(x => x.Sender)
                .WithMany(x => x.OutgoingTransactions)
                .HasForeignKey(x => x.SenderId);

            modelBuilder.Entity<TransactionOperation>()
                .HasOne(x => x.Target)
                .WithMany(x => x.IncomingTransactions)
                .HasForeignKey(x => x.TargetId);
            #endregion
            #endregion
        }
    }
}
