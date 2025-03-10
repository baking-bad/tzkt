using Tzkt.Data.Models;
using Tzkt.Data.Models.Base;

namespace Tzkt.Sync.Protocols
{
    public class BlockContext
    {
        public Block Block { get; set; } = null!;
        public Data.Models.Delegate Proposer { get; set; } = null!;
        public Protocol Protocol { get; set; } = null!;

        #region operations
        public List<EndorsementOperation> EndorsementOps { get; set; } = new();
        public List<PreendorsementOperation> PreendorsementOps { get; set; } = new();

        public List<ProposalOperation> ProposalOps { get; set; } = new();
        public List<BallotOperation> BallotOps { get; set; } = new();

        public List<ActivationOperation> ActivationOps { get; set; } = new();
        public List<DalEntrapmentEvidenceOperation> DalEntrapmentEvidenceOps { get; set; } = new();
        public List<DoubleBakingOperation> DoubleBakingOps { get; set; } = new();
        public List<DoubleEndorsingOperation> DoubleEndorsingOps { get; set; } = new();
        public List<DoublePreendorsingOperation> DoublePreendorsingOps { get; set; } = new();
        public List<NonceRevelationOperation> NonceRevelationOps { get; set; } = new();
        public List<VdfRevelationOperation> VdfRevelationOps { get; set; } = new();
        public List<DrainDelegateOperation> DrainDelegateOps { get; set; } = new();

        public List<DelegationOperation> DelegationOps { get; set; } = new();
        public List<OriginationOperation> OriginationOps { get; set; } = new();
        public List<TransactionOperation> TransactionOps { get; set; } = new();
        public List<RevealOperation> RevealOps { get; set; } = new();
        public List<RegisterConstantOperation> RegisterConstantOps { get; set; } = new();
        public List<SetDepositsLimitOperation> SetDepositsLimitOps { get; set; } = new();
        public List<IncreasePaidStorageOperation> IncreasePaidStorageOps { get; set; } = new();
        public List<UpdateConsensusKeyOperation> UpdateConsensusKeyOps { get; set; } = new();
        public List<TransferTicketOperation> TransferTicketOps { get; set; } = new();
        public List<SetDelegateParametersOperation> SetDelegateParametersOps { get; set; } = new();
        public List<DalPublishCommitmentOperation> DalPublishCommitmentOps { get; set; } = new();
        public List<StakingOperation> StakingOps { get; set; } = new();

        public List<TxRollupOriginationOperation> TxRollupOriginationOps { get; set; } = new();
        public List<TxRollupSubmitBatchOperation> TxRollupSubmitBatchOps { get; set; } = new();
        public List<TxRollupCommitOperation> TxRollupCommitOps { get; set; } = new();
        public List<TxRollupFinalizeCommitmentOperation> TxRollupFinalizeCommitmentOps { get; set; } = new();
        public List<TxRollupRemoveCommitmentOperation> TxRollupRemoveCommitmentOps { get; set; } = new();
        public List<TxRollupReturnBondOperation> TxRollupReturnBondOps { get; set; } = new();
        public List<TxRollupRejectionOperation> TxRollupRejectionOps { get; set; } = new();
        public List<TxRollupDispatchTicketsOperation> TxRollupDispatchTicketsOps { get; set; } = new();

        public List<SmartRollupAddMessagesOperation> SmartRollupAddMessagesOps { get; set; } = new();
        public List<SmartRollupCementOperation> SmartRollupCementOps { get; set; } = new();
        public List<SmartRollupExecuteOperation> SmartRollupExecuteOps { get; set; } = new();
        public List<SmartRollupOriginateOperation> SmartRollupOriginateOps { get; set; } = new();
        public List<SmartRollupPublishOperation> SmartRollupPublishOps { get; set; } = new();
        public List<SmartRollupRecoverBondOperation> SmartRollupRecoverBondOps { get; set; } = new();
        public List<SmartRollupRefuteOperation> SmartRollupRefuteOps { get; set; } = new();
        #endregion

        #region fictive operations
        public List<MigrationOperation> MigrationOps { get; set; } = new();
        public List<RevelationPenaltyOperation> RevelationPenaltyOps { get; set; } = new();
        public List<EndorsingRewardOperation> EndorsingRewardOps { get; set; } = new();
        public List<DalAttestationRewardOperation> DalAttestationRewardOps { get; set; } = new();
        public List<AutostakingOperation> AutostakingOps { get; set; } = new();
        #endregion

        public IEnumerable<IOperation> EnumerateOps()
        {
            var ops = Enumerable.Empty<IOperation>();

            if (EndorsementOps.Any()) ops = ops.Concat(EndorsementOps);
            if (PreendorsementOps.Any()) ops = ops.Concat(PreendorsementOps);

            if (BallotOps.Any()) ops = ops.Concat(BallotOps);
            if (ProposalOps.Any()) ops = ops.Concat(ProposalOps);

            if (ActivationOps.Any()) ops = ops.Concat(ActivationOps);
            if (DalEntrapmentEvidenceOps.Any()) ops = ops.Concat(DalEntrapmentEvidenceOps);
            if (DoubleBakingOps.Any()) ops = ops.Concat(DoubleBakingOps);
            if (DoubleEndorsingOps.Any()) ops = ops.Concat(DoubleEndorsingOps);
            if (DoublePreendorsingOps.Any()) ops = ops.Concat(DoublePreendorsingOps);
            if (NonceRevelationOps.Any()) ops = ops.Concat(NonceRevelationOps);
            if (VdfRevelationOps.Any()) ops = ops.Concat(VdfRevelationOps);
            if (DrainDelegateOps.Any()) ops = ops.Concat(DrainDelegateOps);

            if (DelegationOps.Any()) ops = ops.Concat(DelegationOps);
            if (OriginationOps.Any()) ops = ops.Concat(OriginationOps);
            if (TransactionOps.Any()) ops = ops.Concat(TransactionOps);
            if (RevealOps.Any()) ops = ops.Concat(RevealOps);
            if (RegisterConstantOps.Any()) ops = ops.Concat(RegisterConstantOps);
            if (SetDepositsLimitOps.Any()) ops = ops.Concat(SetDepositsLimitOps);
            if (IncreasePaidStorageOps.Any()) ops = ops.Concat(IncreasePaidStorageOps);
            if (UpdateConsensusKeyOps.Any()) ops = ops.Concat(UpdateConsensusKeyOps);
            if (TransferTicketOps.Any()) ops = ops.Concat(TransferTicketOps);
            if (SetDelegateParametersOps.Any()) ops = ops.Concat(SetDelegateParametersOps);
            if (DalPublishCommitmentOps.Any()) ops = ops.Concat(DalPublishCommitmentOps);
            if (StakingOps.Any()) ops = ops.Concat(StakingOps);

            if (TxRollupOriginationOps.Any()) ops = ops.Concat(TxRollupOriginationOps);
            if (TxRollupSubmitBatchOps.Any()) ops = ops.Concat(TxRollupSubmitBatchOps);
            if (TxRollupCommitOps.Any()) ops = ops.Concat(TxRollupCommitOps);
            if (TxRollupFinalizeCommitmentOps.Any()) ops = ops.Concat(TxRollupFinalizeCommitmentOps);
            if (TxRollupRemoveCommitmentOps.Any()) ops = ops.Concat(TxRollupRemoveCommitmentOps);
            if (TxRollupReturnBondOps.Any()) ops = ops.Concat(TxRollupReturnBondOps);
            if (TxRollupRejectionOps.Any()) ops = ops.Concat(TxRollupRejectionOps);
            if (TxRollupDispatchTicketsOps.Any()) ops = ops.Concat(TxRollupDispatchTicketsOps);

            if (SmartRollupAddMessagesOps.Any()) ops = ops.Concat(SmartRollupAddMessagesOps);
            if (SmartRollupCementOps.Any()) ops = ops.Concat(SmartRollupCementOps);
            if (SmartRollupExecuteOps.Any()) ops = ops.Concat(SmartRollupExecuteOps);
            if (SmartRollupOriginateOps.Any()) ops = ops.Concat(SmartRollupOriginateOps);
            if (SmartRollupPublishOps.Any()) ops = ops.Concat(SmartRollupPublishOps);
            if (SmartRollupRecoverBondOps.Any()) ops = ops.Concat(SmartRollupRecoverBondOps);
            if (SmartRollupRefuteOps.Any()) ops = ops.Concat(SmartRollupRefuteOps);

            return ops;
        }
    }
}
