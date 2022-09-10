using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Tzkt.Data.Models
{
    public class Block
    {
        public long Id { get; set; }
        public int Cycle { get; set; }
        public int Level { get; set; }
        public string Hash { get; set; }
        public DateTime Timestamp { get; set; }
        public int ProtoCode { get; set; }
        public int? SoftwareId { get; set; }

        public int PayloadRound { get; set; }
        public int BlockRound { get; set; }
        public int Validations { get; set; }
        public BlockEvents Events { get; set; }
        public Operations Operations { get; set; }

        public long Deposit { get; set; }
        public long Reward { get; set; }
        public long Bonus { get; set; }
        public long Fees { get; set; }

        public int? ProposerId { get; set; }
        public int? ProducerId { get; set; }
        public long? RevelationId { get; set; }
        public int? ResetBakerDeactivation { get; set; }
        public int? ResetProposerDeactivation { get; set; }

        public bool? LBToggle { get; set; }
        public int LBToggleEma { get; set; }

        #region relations
        [ForeignKey(nameof(ProtoCode))]
        public Protocol Protocol { get; set; }

        [ForeignKey(nameof(ProposerId))]
        public Delegate Proposer { get; set; }

        [ForeignKey(nameof(RevelationId))]
        public NonceRevelationOperation Revelation { get; set; }

        [ForeignKey(nameof(SoftwareId))]
        public Software Software { get; set; }
        #endregion

        #region indirect relations
        public List<Account> CreatedAccounts { get; set; }

        public List<EndorsementOperation> Endorsements { get; set; }
        public List<PreendorsementOperation> Preendorsements { get; set; }

        public List<BallotOperation> Ballots { get; set; }
        public List<ProposalOperation> Proposals { get; set; }

        public List<ActivationOperation> Activations { get; set; }
        public List<DoubleBakingOperation> DoubleBakings { get; set; }
        public List<DoubleEndorsingOperation> DoubleEndorsings { get; set; }
        public List<DoublePreendorsingOperation> DoublePreendorsings { get; set; }
        public List<NonceRevelationOperation> Revelations { get; set; }

        public List<DelegationOperation> Delegations { get; set; }
        public List<OriginationOperation> Originations { get; set; }
        public List<TransactionOperation> Transactions { get; set; }
        public List<RevealOperation> Reveals { get; set; }
        public List<RegisterConstantOperation> RegisterConstants { get; set; }
        public List<SetDepositsLimitOperation> SetDepositsLimits { get; set; }

        public List<TxRollupOriginationOperation> TxRollupOriginationOps { get; set; }
        public List<TxRollupSubmitBatchOperation> TxRollupSubmitBatchOps { get; set; }
        public List<TxRollupCommitOperation> TxRollupCommitOps { get; set; }
        public List<TxRollupFinalizeCommitmentOperation> TxRollupFinalizeCommitmentOps { get; set; }
        public List<TxRollupRemoveCommitmentOperation> TxRollupRemoveCommitmentOps { get; set; }
        public List<TxRollupReturnBondOperation> TxRollupReturnBondOps { get; set; }
        public List<TxRollupRejectionOperation> TxRollupRejectionOps { get; set; }
        public List<TxRollupDispatchTicketsOperation> TxRollupDispatchTicketsOps { get; set; }
        public List<TransferTicketOperation> TransferTicketOps { get; set; }

        public List<IncreasePaidStorageOperation> IncreasePaidStorageOps { get; set; }
        public List<VdfRevelationOperation> VdfRevelationOps { get; set; }

        public List<MigrationOperation> Migrations { get; set; }
        public List<RevelationPenaltyOperation> RevelationPenalties { get; set; }
        #endregion
    }

    public static class BlockModel
    {
        public static void BuildBlockModel(this ModelBuilder modelBuilder)
        {
            #region keys
            modelBuilder.Entity<Block>()
                .HasKey(x => x.Id);

            modelBuilder.Entity<Block>()
                .HasAlternateKey(x => x.Level);
            #endregion

            #region props
            modelBuilder.Entity<Block>()
                .Property(x => x.Hash)
                .IsFixedLength(true)
                .HasMaxLength(51)
                .IsRequired();

            // shadow property
            modelBuilder.Entity<Block>()
                .Property<string>("Metadata")
                .HasColumnType("jsonb");
            #endregion

            #region indexes
            modelBuilder.Entity<Block>()
                .HasIndex(x => x.Level)
                .IsUnique();

            modelBuilder.Entity<Block>()
                .HasIndex(x => x.Hash)
                .IsUnique();

            modelBuilder.Entity<Block>()
                .HasIndex(x => x.ProposerId);

            modelBuilder.Entity<Block>()
                .HasIndex(x => x.ProducerId);
            #endregion

            #region relations
            modelBuilder.Entity<Block>()
                .HasOne(x => x.Protocol)
                .WithMany()
                .HasForeignKey(x => x.ProtoCode)
                .HasPrincipalKey(x => x.Code);

            modelBuilder.Entity<Block>()
                .HasOne(x => x.Revelation)
                .WithOne(x => x.RevealedBlock)
                .HasForeignKey<Block>(x => x.RevelationId);

            modelBuilder.Entity<Block>()
                .HasOne(x => x.Software)
                .WithMany()
                .HasForeignKey(x => x.SoftwareId)
                .HasPrincipalKey(x => x.Id);
            #endregion
        }
    }
}
