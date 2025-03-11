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
        public List<EndorsementOperation> EndorsementOps { get; set; } = [];
        public List<PreendorsementOperation> PreendorsementOps { get; set; } = [];

        public List<ProposalOperation> ProposalOps { get; set; } = [];
        public List<BallotOperation> BallotOps { get; set; } = [];

        public List<ActivationOperation> ActivationOps { get; set; } = [];
        public List<DalEntrapmentEvidenceOperation> DalEntrapmentEvidenceOps { get; set; } = [];
        public List<DoubleBakingOperation> DoubleBakingOps { get; set; } = [];
        public List<DoubleEndorsingOperation> DoubleEndorsingOps { get; set; } = [];
        public List<DoublePreendorsingOperation> DoublePreendorsingOps { get; set; } = [];
        public List<NonceRevelationOperation> NonceRevelationOps { get; set; } = [];
        public List<VdfRevelationOperation> VdfRevelationOps { get; set; } = [];
        public List<DrainDelegateOperation> DrainDelegateOps { get; set; } = [];

        public List<DelegationOperation> DelegationOps { get; set; } = [];
        public List<OriginationOperation> OriginationOps { get; set; } = [];
        public List<TransactionOperation> TransactionOps { get; set; } = [];
        public List<RevealOperation> RevealOps { get; set; } = [];
        public List<RegisterConstantOperation> RegisterConstantOps { get; set; } = [];
        public List<SetDepositsLimitOperation> SetDepositsLimitOps { get; set; } = [];
        public List<IncreasePaidStorageOperation> IncreasePaidStorageOps { get; set; } = [];
        public List<UpdateConsensusKeyOperation> UpdateConsensusKeyOps { get; set; } = [];
        public List<TransferTicketOperation> TransferTicketOps { get; set; } = [];
        public List<SetDelegateParametersOperation> SetDelegateParametersOps { get; set; } = [];
        public List<DalPublishCommitmentOperation> DalPublishCommitmentOps { get; set; } = [];
        public List<StakingOperation> StakingOps { get; set; } = [];

        public List<TxRollupOriginationOperation> TxRollupOriginationOps { get; set; } = [];
        public List<TxRollupSubmitBatchOperation> TxRollupSubmitBatchOps { get; set; } = [];
        public List<TxRollupCommitOperation> TxRollupCommitOps { get; set; } = [];
        public List<TxRollupFinalizeCommitmentOperation> TxRollupFinalizeCommitmentOps { get; set; } = [];
        public List<TxRollupRemoveCommitmentOperation> TxRollupRemoveCommitmentOps { get; set; } = [];
        public List<TxRollupReturnBondOperation> TxRollupReturnBondOps { get; set; } = [];
        public List<TxRollupRejectionOperation> TxRollupRejectionOps { get; set; } = [];
        public List<TxRollupDispatchTicketsOperation> TxRollupDispatchTicketsOps { get; set; } = [];

        public List<SmartRollupAddMessagesOperation> SmartRollupAddMessagesOps { get; set; } = [];
        public List<SmartRollupCementOperation> SmartRollupCementOps { get; set; } = [];
        public List<SmartRollupExecuteOperation> SmartRollupExecuteOps { get; set; } = [];
        public List<SmartRollupOriginateOperation> SmartRollupOriginateOps { get; set; } = [];
        public List<SmartRollupPublishOperation> SmartRollupPublishOps { get; set; } = [];
        public List<SmartRollupRecoverBondOperation> SmartRollupRecoverBondOps { get; set; } = [];
        public List<SmartRollupRefuteOperation> SmartRollupRefuteOps { get; set; } = [];
        #endregion

        #region fictive operations
        public List<MigrationOperation> MigrationOps { get; set; } = [];
        public List<RevelationPenaltyOperation> RevelationPenaltyOps { get; set; } = [];
        public List<EndorsingRewardOperation> EndorsingRewardOps { get; set; } = [];
        public List<DalAttestationRewardOperation> DalAttestationRewardOps { get; set; } = [];
        public List<AutostakingOperation> AutostakingOps { get; set; } = [];
        #endregion

        public IEnumerable<IOperation> EnumerateOps()
        {
            var ops = Enumerable.Empty<IOperation>();

            if (EndorsementOps.Count != 0) ops = ops.Concat(EndorsementOps);
            if (PreendorsementOps.Count != 0) ops = ops.Concat(PreendorsementOps);

            if (BallotOps.Count != 0) ops = ops.Concat(BallotOps);
            if (ProposalOps.Count != 0) ops = ops.Concat(ProposalOps);

            if (ActivationOps.Count != 0) ops = ops.Concat(ActivationOps);
            if (DalEntrapmentEvidenceOps.Count != 0) ops = ops.Concat(DalEntrapmentEvidenceOps);
            if (DoubleBakingOps.Count != 0) ops = ops.Concat(DoubleBakingOps);
            if (DoubleEndorsingOps.Count != 0) ops = ops.Concat(DoubleEndorsingOps);
            if (DoublePreendorsingOps.Count != 0) ops = ops.Concat(DoublePreendorsingOps);
            if (NonceRevelationOps.Count != 0) ops = ops.Concat(NonceRevelationOps);
            if (VdfRevelationOps.Count != 0) ops = ops.Concat(VdfRevelationOps);
            if (DrainDelegateOps.Count != 0) ops = ops.Concat(DrainDelegateOps);

            if (DelegationOps.Count != 0) ops = ops.Concat(DelegationOps);
            if (OriginationOps.Count != 0) ops = ops.Concat(OriginationOps);
            if (TransactionOps.Count != 0) ops = ops.Concat(TransactionOps);
            if (RevealOps.Count != 0) ops = ops.Concat(RevealOps);
            if (RegisterConstantOps.Count != 0) ops = ops.Concat(RegisterConstantOps);
            if (SetDepositsLimitOps.Count != 0) ops = ops.Concat(SetDepositsLimitOps);
            if (IncreasePaidStorageOps.Count != 0) ops = ops.Concat(IncreasePaidStorageOps);
            if (UpdateConsensusKeyOps.Count != 0) ops = ops.Concat(UpdateConsensusKeyOps);
            if (TransferTicketOps.Count != 0) ops = ops.Concat(TransferTicketOps);
            if (SetDelegateParametersOps.Count != 0) ops = ops.Concat(SetDelegateParametersOps);
            if (DalPublishCommitmentOps.Count != 0) ops = ops.Concat(DalPublishCommitmentOps);
            if (StakingOps.Count != 0) ops = ops.Concat(StakingOps);

            if (TxRollupOriginationOps.Count != 0) ops = ops.Concat(TxRollupOriginationOps);
            if (TxRollupSubmitBatchOps.Count != 0) ops = ops.Concat(TxRollupSubmitBatchOps);
            if (TxRollupCommitOps.Count != 0) ops = ops.Concat(TxRollupCommitOps);
            if (TxRollupFinalizeCommitmentOps.Count != 0) ops = ops.Concat(TxRollupFinalizeCommitmentOps);
            if (TxRollupRemoveCommitmentOps.Count != 0) ops = ops.Concat(TxRollupRemoveCommitmentOps);
            if (TxRollupReturnBondOps.Count != 0) ops = ops.Concat(TxRollupReturnBondOps);
            if (TxRollupRejectionOps.Count != 0) ops = ops.Concat(TxRollupRejectionOps);
            if (TxRollupDispatchTicketsOps.Count != 0) ops = ops.Concat(TxRollupDispatchTicketsOps);

            if (SmartRollupAddMessagesOps.Count != 0) ops = ops.Concat(SmartRollupAddMessagesOps);
            if (SmartRollupCementOps.Count != 0) ops = ops.Concat(SmartRollupCementOps);
            if (SmartRollupExecuteOps.Count != 0) ops = ops.Concat(SmartRollupExecuteOps);
            if (SmartRollupOriginateOps.Count != 0) ops = ops.Concat(SmartRollupOriginateOps);
            if (SmartRollupPublishOps.Count != 0) ops = ops.Concat(SmartRollupPublishOps);
            if (SmartRollupRecoverBondOps.Count != 0) ops = ops.Concat(SmartRollupRecoverBondOps);
            if (SmartRollupRefuteOps.Count != 0) ops = ops.Concat(SmartRollupRefuteOps);

            return ops;
        }
    }
}
