﻿using Microsoft.EntityFrameworkCore;

namespace Tzkt.Data.Models
{
    public class AppState
    {
        public const int SubIdBits = 20;

        public required int Id { get; set; }
        public required string Chain { get; set; }
        public required string ChainId { get; set; }
        public int KnownHead { get; set; }
        public DateTime LastSync { get; set; }

        #region head
        public int Cycle { get; set; }
        public int Level { get; set; }
        public DateTime Timestamp { get; set; }
        public required string Protocol { get; set; }
        public required string NextProtocol { get; set; }
        public required string Hash { get; set; }
        public int VotingEpoch { get; set; }
        public int VotingPeriod { get; set; }
        #endregion

        #region state
        public int PendingDelegateParameters { get; set; }
        #endregion

        #region counters
        public int AccountCounter { get; set; }
        public long OperationCounter { get; set; }
        public int ManagerCounter { get; set; }
        public int BigMapCounter { get; set; }
        public int BigMapKeyCounter { get; set; }
        public int BigMapUpdateCounter { get; set; }
        public int StorageCounter { get; set; }
        public int ScriptCounter { get; set; }
        public int EventCounter { get; set; }
        public int SmartRollupCommitmentCounter { get; set; }
        public int RefutationGameCounter { get; set; }
        public int InboxMessageCounter { get; set; }
        public int ProposalCounter { get; set; }
        public int SoftwareCounter { get; set; }
        #endregion

        #region entities count
        public int CommitmentsCount { get; set; }

        public int BlocksCount { get; set; }
        public int ProtocolsCount { get; set; }

        public int ActivationOpsCount { get; set; }
        public int BallotOpsCount { get; set; }
        public int DelegationOpsCount { get; set; }
        public int DalEntrapmentEvidenceOpsCount { get; set; }
        public int DoubleBakingOpsCount { get; set; }
        public int DoubleEndorsingOpsCount { get; set; }
        public int DoublePreendorsingOpsCount { get; set; }
        public int EndorsementOpsCount { get; set; }
        public int PreendorsementOpsCount { get; set; }
        public int NonceRevelationOpsCount { get; set; }
        public int VdfRevelationOpsCount { get; set; }
        public int OriginationOpsCount { get; set; }
        public int ProposalOpsCount { get; set; }
        public int RevealOpsCount { get; set; }
        public int StakingOpsCount { get; set; }
        public int SetDelegateParametersOpsCount { get; set; }
        public int TransactionOpsCount { get; set; }
        public int RegisterConstantOpsCount { get; set; }
        public int EndorsingRewardOpsCount { get; set; }
        public int DalAttestationRewardOpsCount { get; set; }
        public int SetDepositsLimitOpsCount { get; set; }

        public int TxRollupOriginationOpsCount { get; set; }
        public int TxRollupSubmitBatchOpsCount { get; set; }
        public int TxRollupCommitOpsCount { get; set; }
        public int TxRollupFinalizeCommitmentOpsCount { get; set; }
        public int TxRollupRemoveCommitmentOpsCount { get; set; }
        public int TxRollupReturnBondOpsCount { get; set; }
        public int TxRollupRejectionOpsCount { get; set; }
        public int TxRollupDispatchTicketsOpsCount { get; set; }
        public int TransferTicketOpsCount { get; set; }

        public int IncreasePaidStorageOpsCount { get; set; }
        public int UpdateConsensusKeyOpsCount { get; set; }
        public int DrainDelegateOpsCount { get; set; }

        public int MigrationOpsCount { get; set; }
        public int RevelationPenaltyOpsCount { get; set; }
        public int AutostakingOpsCount { get; set; }

        public int SmartRollupAddMessagesOpsCount { get; set; }
        public int SmartRollupCementOpsCount { get; set; }
        public int SmartRollupExecuteOpsCount { get; set; }
        public int SmartRollupOriginateOpsCount { get; set; }
        public int SmartRollupPublishOpsCount { get; set; }
        public int SmartRollupRecoverBondOpsCount { get; set; }
        public int SmartRollupRefuteOpsCount { get; set; }

        public int DalPublishCommitmentOpsCount { get; set; }

        public int CyclesCount { get; set; }
        public int ConstantsCount { get; set; }

        public int TokensCount { get; set; }
        public int TokenBalancesCount { get; set; }
        public int TokenTransfersCount { get; set; }
        
        public int TicketsCount { get; set; }
        public int TicketBalancesCount { get; set; }
        public int TicketTransfersCount { get; set; }

        public int EventsCount { get; set; }
        public int StakingUpdatesCount { get; set; }
        public int UnstakeRequestsCount { get; set; }
        #endregion

        #region plugins
        public int QuoteLevel { get; set; }
        public double QuoteBtc { get; set; }
        public double QuoteEur { get; set; }
        public double QuoteUsd { get; set; }
        public double QuoteCny { get; set; }
        public double QuoteJpy { get; set; }
        public double QuoteKrw { get; set; }
        public double QuoteEth { get; set; }
        public double QuoteGbp { get; set; }

        public required string DomainsNameRegistry { get; set; }
        public int DomainsLevel { get; set; }
        #endregion 
    }

    public static class AppStateModel
    {
        public static void BuildAppStateModel(this ModelBuilder modelBuilder)
        {
            #region keys
            modelBuilder.Entity<AppState>()
                .HasKey(x => x.Id);
            #endregion

            #region props
            // shadow property
            modelBuilder.Entity<AppState>()
                .Property<string>("Extras")
                .HasColumnType("jsonb");
            #endregion

            #region seed
            modelBuilder.Entity<AppState>().HasData(
                new AppState
                {
                    Id = -1,
                    Chain = string.Empty,
                    ChainId = string.Empty,
                    Cycle = -1,
                    Level = -1,
                    Timestamp = DateTimeOffset.MinValue.UtcDateTime,
                    Protocol = string.Empty,
                    NextProtocol = string.Empty,
                    Hash = string.Empty,
                    VotingEpoch = -1,
                    VotingPeriod = -1,
                    QuoteLevel = -1,
                    DomainsNameRegistry = string.Empty,
                });
            #endregion
        }
    }
}
