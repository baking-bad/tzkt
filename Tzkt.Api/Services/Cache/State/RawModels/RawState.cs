namespace Tzkt.Api.Services.Cache
{
    public class RawState
    {
        public required string Chain { get; set; }
        public required string ChainId { get; set; }
        public int KnownHead { get; set; }
        public DateTime LastSync { get; set; }
        public int Cycle { get; set; }
        public int Level { get; set; }
        public required string Hash { get; set; }
        public required string Protocol { get; set; }
        public required string NextProtocol { get; set; }
        public DateTime Timestamp { get; set; }
        public int VotingEpoch { get; set; }
        public int VotingPeriod { get; set; }
        public int AccountCounter { get; set; }
        public int ManagerCounter { get; set; }
        public long OperationCounter { get; set; }
        public int EventCounter { get; set; }
        public int SmartRollupCommitmentCounter { get; set; }
        public int RefutationGameCounter { get; set; }
        public int InboxMessageCounter { get; set; }
        public int BigMapUpdateCounter { get; set; }
        public int ProposalCounter { get; set; }

        #region entities count
        public int CommitmentsCount { get; set; }

        public int BlocksCount { get; set; }
        public int ProtocolsCount { get; set; }
        public int CyclesCount { get; set; }
        public int ConstantsCount { get; set; }

        public int ActivationOpsCount { get; set; }
        public int BallotOpsCount { get; set; }
        public int DalEntrapmentEvidenceOpsCount { get; set; }
        public int DelegationOpsCount { get; set; }
        public int DoubleBakingOpsCount { get; set; }
        public int DoubleConsensusOpsCount { get; set; }
        public int AttestationOpsCount { get; set; }
        public int PreattestationOpsCount { get; set; }
        public int NonceRevelationOpsCount { get; set; }
        public int VdfRevelationOpsCount { get; set; }
        public int OriginationOpsCount { get; set; }
        public int ProposalOpsCount { get; set; }
        public int RevealOpsCount { get; set; }
        public int StakingOpsCount { get; set; }
        public int SetDelegateParametersOpsCount { get; set; }
        public int DalPublishCommitmentOpsCount { get; set; }
        public int RegisterConstantOpsCount { get; set; }
        public int SetDepositsLimitOpsCount { get; set; }
        public int TransactionOpsCount { get; set; }
        public int MigrationOpsCount { get; set; }
        public int RevelationPenaltyOpsCount { get; set; }
        public int AttestationRewardOpsCount { get; set; }
        public int DalAttestationRewardOpsCount { get; set; }
        public int AutostakingOpsCount { get; set; }

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
        public int UpdateSecondaryKeyOpsCount { get; set; }
        public int DrainDelegateOpsCount { get; set; }

        public int SmartRollupAddMessagesOpsCount { get; set; }
        public int SmartRollupCementOpsCount { get; set; }
        public int SmartRollupExecuteOpsCount { get; set; }
        public int SmartRollupOriginateOpsCount { get; set; }
        public int SmartRollupPublishOpsCount { get; set; }
        public int SmartRollupRecoverBondOpsCount { get; set; }
        public int SmartRollupRefuteOpsCount { get; set; }

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
