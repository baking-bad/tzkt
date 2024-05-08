using System.Runtime.Serialization;
using NJsonSchema.Converters;
using Newtonsoft.Json;

namespace Tzkt.Api.Models
{
    [JsonConverter(typeof(OperationJsonInheritanceConverter), "type")]
    [KnownType(typeof(EndorsementOperation))]
    [KnownType(typeof(PreendorsementOperation))]
    [KnownType(typeof(BallotOperation))]
    [KnownType(typeof(ProposalOperation))]
    [KnownType(typeof(ActivationOperation))]
    [KnownType(typeof(DoubleBakingOperation))]
    [KnownType(typeof(DoubleEndorsingOperation))]
    [KnownType(typeof(DoublePreendorsingOperation))]
    [KnownType(typeof(NonceRevelationOperation))]
    [KnownType(typeof(VdfRevelationOperation))]
    [KnownType(typeof(DrainDelegateOperation))]
    [KnownType(typeof(DelegationOperation))]
    [KnownType(typeof(OriginationOperation))]
    [KnownType(typeof(TransactionOperation))]
    [KnownType(typeof(RevealOperation))]
    [KnownType(typeof(RegisterConstantOperation))]
    [KnownType(typeof(SetDepositsLimitOperation))]
    [KnownType(typeof(MigrationOperation))]
    [KnownType(typeof(RevelationPenaltyOperation))]
    [KnownType(typeof(BakingOperation))]
    [KnownType(typeof(EndorsingRewardOperation))]
    [KnownType(typeof(TransferTicketOperation))]
    [KnownType(typeof(TxRollupCommitOperation))]
    [KnownType(typeof(TxRollupDispatchTicketsOperation))]
    [KnownType(typeof(TxRollupFinalizeCommitmentOperation))]
    [KnownType(typeof(TxRollupOriginationOperation))]
    [KnownType(typeof(TxRollupRejectionOperation))]
    [KnownType(typeof(TxRollupRemoveCommitmentOperation))]
    [KnownType(typeof(TxRollupReturnBondOperation))]
    [KnownType(typeof(TxRollupSubmitBatchOperation))]
    [KnownType(typeof(IncreasePaidStorageOperation))]
    [KnownType(typeof(UpdateConsensusKeyOperation))]
    [KnownType(typeof(SmartRollupAddMessagesOperation))]
    [KnownType(typeof(SmartRollupCementOperation))]
    [KnownType(typeof(SmartRollupExecuteOperation))]
    [KnownType(typeof(SmartRollupOriginateOperation))]
    [KnownType(typeof(SmartRollupPublishOperation))]
    [KnownType(typeof(SmartRollupRecoverBondOperation))]
    [KnownType(typeof(SmartRollupRefuteOperation))]
    [KnownType(typeof(AutostakingOperation))]
    [KnownType(typeof(StakingOperation))]
    [KnownType(typeof(SetDelegateParametersOperation))]
    [KnownType(typeof(DalPublishCommitmentOperation))]
    public abstract class Operation
    {
        /// <summary>
        /// Type of the operation
        /// </summary>
        public abstract string Type { get; }

        /// <summary>
        /// Unique ID of the operation, stored in the TzKT indexer database
        /// </summary>
        public abstract long Id { get; set; }
    }

    public class OperationJsonInheritanceConverter : JsonInheritanceConverter
    {
        public OperationJsonInheritanceConverter(string name) : base(name) { }

        public override string GetDiscriminatorValue(Type type)
        {
            if (type == typeof(EndorsementOperation))
                return OpTypes.Endorsement;

            if (type == typeof(PreendorsementOperation))
                return OpTypes.Preendorsement;

            if (type == typeof(BallotOperation))
                return OpTypes.Ballot;

            if (type == typeof(ProposalOperation))
                return OpTypes.Proposal;

            if (type == typeof(ActivationOperation))
                return OpTypes.Activation;

            if (type == typeof(DoubleBakingOperation))
                return OpTypes.DoubleBaking;

            if (type == typeof(DoubleEndorsingOperation))
                return OpTypes.DoubleEndorsing;

            if (type == typeof(DoublePreendorsingOperation))
                return OpTypes.DoublePreendorsing;

            if (type == typeof(NonceRevelationOperation))
                return OpTypes.NonceRevelation;

            if (type == typeof(VdfRevelationOperation))
                return OpTypes.VdfRevelation;

            if (type == typeof(DrainDelegateOperation))
                return OpTypes.DrainDelegate;

            if (type == typeof(DelegationOperation))
                return OpTypes.Delegation;

            if (type == typeof(OriginationOperation))
                return OpTypes.Origination;

            if (type == typeof(TransactionOperation))
                return OpTypes.Transaction;

            if (type == typeof(RevealOperation))
                return OpTypes.Reveal;

            if (type == typeof(RegisterConstantOperation))
                return OpTypes.RegisterConstant;

            if (type == typeof(SetDepositsLimitOperation))
                return OpTypes.SetDepositsLimit;

            if (type == typeof(MigrationOperation))
                return OpTypes.Migration;

            if (type == typeof(RevelationPenaltyOperation))
                return OpTypes.RevelationPenalty;

            if (type == typeof(BakingOperation))
                return OpTypes.Baking;

            if (type == typeof(EndorsingRewardOperation))
                return OpTypes.EndorsingReward;

            if (type == typeof(TransferTicketOperation))
                return OpTypes.TransferTicket;

            if (type == typeof(TxRollupCommitOperation))
                return OpTypes.TxRollupCommit;

            if (type == typeof(TxRollupDispatchTicketsOperation))
                return OpTypes.TxRollupDispatchTickets;

            if (type == typeof(TxRollupFinalizeCommitmentOperation))
                return OpTypes.TxRollupFinalizeCommitment;

            if (type == typeof(TxRollupOriginationOperation))
                return OpTypes.TxRollupOrigination;

            if (type == typeof(TxRollupRejectionOperation))
                return OpTypes.TxRollupRejection;

            if (type == typeof(TxRollupRemoveCommitmentOperation))
                return OpTypes.TxRollupRemoveCommitment;

            if (type == typeof(TxRollupReturnBondOperation))
                return OpTypes.TxRollupReturnBond;

            if (type == typeof(TxRollupSubmitBatchOperation))
                return OpTypes.TxRollupSubmitBatch;

            if (type == typeof(IncreasePaidStorageOperation))
                return OpTypes.IncreasePaidStorage;

            if (type == typeof(UpdateConsensusKeyOperation))
                return OpTypes.UpdateConsensusKey;

            if (type == typeof(SmartRollupAddMessagesOperation))
                return OpTypes.SmartRollupAddMessages;

            if (type == typeof(SmartRollupCementOperation))
                return OpTypes.SmartRollupCement;

            if (type == typeof(SmartRollupExecuteOperation))
                return OpTypes.SmartRollupExecute;

            if (type == typeof(SmartRollupOriginateOperation))
                return OpTypes.SmartRollupOriginate;

            if (type == typeof(SmartRollupPublishOperation))
                return OpTypes.SmartRollupPublish;

            if (type == typeof(SmartRollupRecoverBondOperation))
                return OpTypes.SmartRollupRecoverBond;

            if (type == typeof(SmartRollupRefuteOperation))
                return OpTypes.SmartRollupRefute;

            if (type == typeof(AutostakingOperation))
                return OpTypes.Autostaking;

            if (type == typeof(StakingOperation))
                return OpTypes.Staking;

            if (type == typeof(SetDelegateParametersOperation))
                return OpTypes.SetDelegateParameters;

            if (type == typeof(DalPublishCommitmentOperation))
                return OpTypes.DalPublishCommitment;

            return base.GetDiscriminatorValue(type);
        }
    }
}
