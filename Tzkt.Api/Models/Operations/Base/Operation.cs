﻿using System.Runtime.Serialization;
using NJsonSchema.Converters;
using Newtonsoft.Json;

namespace Tzkt.Api.Models
{
    [JsonConverter(typeof(OperationJsonInheritanceConverter), "type")]
    [KnownType(typeof(ActivationOperation))]
    [KnownType(typeof(AutostakingOperation))]
    [KnownType(typeof(BakingOperation))]
    [KnownType(typeof(BallotOperation))]
    [KnownType(typeof(DalAttestationRewardOperation))]
    [KnownType(typeof(DalEntrapmentEvidenceOperation))]
    [KnownType(typeof(DalPublishCommitmentOperation))]
    [KnownType(typeof(DelegationOperation))]
    [KnownType(typeof(DoubleBakingOperation))]
    [KnownType(typeof(DoubleEndorsingOperation))]
    [KnownType(typeof(DoublePreendorsingOperation))]
    [KnownType(typeof(DrainDelegateOperation))]
    [KnownType(typeof(EndorsementOperation))]
    [KnownType(typeof(EndorsingRewardOperation))]
    [KnownType(typeof(IncreasePaidStorageOperation))]
    [KnownType(typeof(MigrationOperation))]
    [KnownType(typeof(NonceRevelationOperation))]
    [KnownType(typeof(OriginationOperation))]
    [KnownType(typeof(PreendorsementOperation))]
    [KnownType(typeof(ProposalOperation))]
    [KnownType(typeof(RegisterConstantOperation))]
    [KnownType(typeof(RevealOperation))]
    [KnownType(typeof(RevelationPenaltyOperation))]
    [KnownType(typeof(SetDelegateParametersOperation))]
    [KnownType(typeof(SetDepositsLimitOperation))]
    [KnownType(typeof(SmartRollupAddMessagesOperation))]
    [KnownType(typeof(SmartRollupCementOperation))]
    [KnownType(typeof(SmartRollupExecuteOperation))]
    [KnownType(typeof(SmartRollupOriginateOperation))]
    [KnownType(typeof(SmartRollupPublishOperation))]
    [KnownType(typeof(SmartRollupRecoverBondOperation))]
    [KnownType(typeof(SmartRollupRefuteOperation))]
    [KnownType(typeof(StakingOperation))]
    [KnownType(typeof(TransactionOperation))]
    [KnownType(typeof(TransferTicketOperation))]
    [KnownType(typeof(TxRollupCommitOperation))]
    [KnownType(typeof(TxRollupDispatchTicketsOperation))]
    [KnownType(typeof(TxRollupFinalizeCommitmentOperation))]
    [KnownType(typeof(TxRollupOriginationOperation))]
    [KnownType(typeof(TxRollupRejectionOperation))]
    [KnownType(typeof(TxRollupRemoveCommitmentOperation))]
    [KnownType(typeof(TxRollupReturnBondOperation))]
    [KnownType(typeof(TxRollupSubmitBatchOperation))]
    [KnownType(typeof(UpdateConsensusKeyOperation))]
    [KnownType(typeof(VdfRevelationOperation))]
    public abstract class Operation : Activity
    {
        /// <summary>
        /// Type of the operation
        /// </summary>
        public override abstract string Type { get; }

        /// <summary>
        /// Internal ID of the operation
        /// </summary>
        public override abstract long Id { get; set; }
    }

    public class OperationJsonInheritanceConverter(string name) : JsonInheritanceConverter<Operation>(name)
    {
        public override string GetDiscriminatorValue(Type type)
        {
            if (type == typeof(ActivationOperation)) return ActivityTypes.Activation;
            if (type == typeof(AutostakingOperation)) return ActivityTypes.Autostaking;
            if (type == typeof(BakingOperation)) return ActivityTypes.Baking;
            if (type == typeof(BallotOperation)) return ActivityTypes.Ballot;
            if (type == typeof(DalAttestationRewardOperation)) return ActivityTypes.DalAttestationReward;
            if (type == typeof(DalEntrapmentEvidenceOperation)) return ActivityTypes.DalEntrapmentEvidence;
            if (type == typeof(DalPublishCommitmentOperation)) return ActivityTypes.DalPublishCommitment;
            if (type == typeof(DelegationOperation)) return ActivityTypes.Delegation;
            if (type == typeof(DoubleBakingOperation)) return ActivityTypes.DoubleBaking;
            if (type == typeof(DoubleEndorsingOperation)) return ActivityTypes.DoubleEndorsing;
            if (type == typeof(DoublePreendorsingOperation)) return ActivityTypes.DoublePreendorsing;
            if (type == typeof(DrainDelegateOperation)) return ActivityTypes.DrainDelegate;
            if (type == typeof(EndorsementOperation)) return ActivityTypes.Endorsement;
            if (type == typeof(EndorsingRewardOperation)) return ActivityTypes.EndorsingReward;
            if (type == typeof(IncreasePaidStorageOperation)) return ActivityTypes.IncreasePaidStorage;
            if (type == typeof(MigrationOperation)) return ActivityTypes.Migration;
            if (type == typeof(NonceRevelationOperation)) return ActivityTypes.NonceRevelation;
            if (type == typeof(OriginationOperation)) return ActivityTypes.Origination;
            if (type == typeof(PreendorsementOperation)) return ActivityTypes.Preendorsement;
            if (type == typeof(ProposalOperation)) return ActivityTypes.Proposal;
            if (type == typeof(RegisterConstantOperation)) return ActivityTypes.RegisterConstant;
            if (type == typeof(RevealOperation)) return ActivityTypes.Reveal;
            if (type == typeof(RevelationPenaltyOperation)) return ActivityTypes.RevelationPenalty;
            if (type == typeof(SetDelegateParametersOperation)) return ActivityTypes.SetDelegateParameters;
            if (type == typeof(SetDepositsLimitOperation)) return ActivityTypes.SetDepositsLimit;
            if (type == typeof(SmartRollupAddMessagesOperation)) return ActivityTypes.SmartRollupAddMessages;
            if (type == typeof(SmartRollupCementOperation)) return ActivityTypes.SmartRollupCement;
            if (type == typeof(SmartRollupExecuteOperation)) return ActivityTypes.SmartRollupExecute;
            if (type == typeof(SmartRollupOriginateOperation)) return ActivityTypes.SmartRollupOriginate;
            if (type == typeof(SmartRollupPublishOperation)) return ActivityTypes.SmartRollupPublish;
            if (type == typeof(SmartRollupRecoverBondOperation)) return ActivityTypes.SmartRollupRecoverBond;
            if (type == typeof(SmartRollupRefuteOperation)) return ActivityTypes.SmartRollupRefute;
            if (type == typeof(StakingOperation)) return ActivityTypes.Staking;
            if (type == typeof(TransactionOperation)) return ActivityTypes.Transaction;
            if (type == typeof(TransferTicketOperation)) return ActivityTypes.TransferTicket;
            if (type == typeof(TxRollupCommitOperation)) return ActivityTypes.TxRollupCommit;
            if (type == typeof(TxRollupDispatchTicketsOperation)) return ActivityTypes.TxRollupDispatchTickets;
            if (type == typeof(TxRollupFinalizeCommitmentOperation)) return ActivityTypes.TxRollupFinalizeCommitment;
            if (type == typeof(TxRollupOriginationOperation)) return ActivityTypes.TxRollupOrigination;
            if (type == typeof(TxRollupRejectionOperation)) return ActivityTypes.TxRollupRejection;
            if (type == typeof(TxRollupRemoveCommitmentOperation)) return ActivityTypes.TxRollupRemoveCommitment;
            if (type == typeof(TxRollupReturnBondOperation)) return ActivityTypes.TxRollupReturnBond;
            if (type == typeof(TxRollupSubmitBatchOperation)) return ActivityTypes.TxRollupSubmitBatch;
            if (type == typeof(UpdateConsensusKeyOperation)) return ActivityTypes.UpdateConsensusKey;
            if (type == typeof(VdfRevelationOperation)) return ActivityTypes.VdfRevelation;

            return base.GetDiscriminatorValue(type);
        }
    }
}
