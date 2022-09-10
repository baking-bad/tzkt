using System;

namespace Tzkt.Api.Services.Cache
{
    public class RawState
    {
        public string Chain { get; set; }
        
        public string ChainId { get; set; }

        public int KnownHead { get; set; }

        public DateTime LastSync { get; set; }

        public int Cycle { get; set; }

        public int Level { get; set; }

        public string Hash { get; set; }

        public string Protocol { get; set; }

        public string NextProtocol { get; set; }

        public DateTime Timestamp { get; set; }

        public int VotingEpoch { get; set; }

        public int VotingPeriod { get; set; }

        public int ManagerCounter { get; set; }
        
        public long OperationCounter { get; set; }

        public int EventCounter { get; set; }

        #region entities count
        public int CommitmentsCount { get; set; }
        public int AccountsCount { get; set; }

        public int BlocksCount { get; set; }
        public int ProtocolsCount { get; set; }
        public int ProposalsCount { get; set; }
        public int CyclesCount { get; set; }
        public int ConstantsCount { get; set; }

        public int ActivationOpsCount { get; set; }
        public int BallotOpsCount { get; set; }
        public int DelegationOpsCount { get; set; }
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
        public int RegisterConstantOpsCount { get; set; }
        public int SetDepositsLimitOpsCount { get; set; }
        public int TransactionOpsCount { get; set; }
        public int MigrationOpsCount { get; set; }
        public int RevelationPenaltyOpsCount { get; set; }
        public int EndorsingRewardOpsCount { get; set; }

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

        public int TokensCount { get; set; }
        public int TokenBalancesCount { get; set; }
        public int TokenTransfersCount { get; set; }
        public int EventsCount { get; set; }
        #endregion

        #region quotes
        public int QuoteLevel { get; set; }
        public double QuoteBtc { get; set; }
        public double QuoteEur { get; set; }
        public double QuoteUsd { get; set; }
        public double QuoteCny { get; set; }
        public double QuoteJpy { get; set; }
        public double QuoteKrw { get; set; }
        public double QuoteEth { get; set; }
        public double QuoteGbp { get; set; }
        #endregion
    }
}
