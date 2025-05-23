﻿using Tzkt.Data.Models;

namespace Tzkt.Api
{
    static class OpTypes
    {
        public const string Endorsement = "endorsement";
        public const string Preendorsement = "preendorsement";

        public const string Ballot = "ballot";
        public const string Proposal = "proposal";

        public const string Activation = "activation";
        public const string DoubleBaking = "double_baking";
        public const string DoubleEndorsing = "double_endorsing";
        public const string DoublePreendorsing = "double_preendorsing";
        public const string NonceRevelation = "nonce_revelation";
        public const string VdfRevelation = "vdf_revelation";
        public const string DrainDelegate = "drain_delegate";

        public const string Delegation = "delegation";
        public const string Origination = "origination";
        public const string Transaction = "transaction";
        public const string Reveal = "reveal";
        public const string RegisterConstant = "register_constant";
        public const string SetDepositsLimit = "set_deposits_limit";
        public const string Staking = "staking";

        public const string TxRollupOrigination = "tx_rollup_origination";
        public const string TxRollupSubmitBatch = "tx_rollup_submit_batch";
        public const string TxRollupCommit = "tx_rollup_commit";
        public const string TxRollupReturnBond = "tx_rollup_return_bond";
        public const string TxRollupFinalizeCommitment = "tx_rollup_finalize_commitment";
        public const string TxRollupRemoveCommitment = "tx_rollup_remove_commitment";
        public const string TxRollupRejection = "tx_rollup_rejection";
        public const string TxRollupDispatchTickets = "tx_rollup_dispatch_tickets";
        public const string TransferTicket = "transfer_ticket";

        public const string IncreasePaidStorage = "increase_paid_storage";
        public const string UpdateConsensusKey = "update_consensus_key";

        public const string SmartRollupAddMessages = "sr_add_messages";
        public const string SmartRollupCement = "sr_cement";
        public const string SmartRollupExecute = "sr_execute";
        public const string SmartRollupOriginate = "sr_originate";
        public const string SmartRollupPublish = "sr_publish";
        public const string SmartRollupRecoverBond = "sr_recover_bond";
        public const string SmartRollupRefute = "sr_refute";

        public const string Migration = "migration";
        public const string RevelationPenalty = "revelation_penalty";
        public const string Baking = "baking";
        public const string EndorsingReward = "endorsing_reward";
        public const string Autostaking = "autostaking";

        public const string SetDelegateParameters = "set_delegate_parameters";
        public const string DalPublishCommitment = "dal_publish_commitment";

        public const string DalEntrapmentEvidence = "dal_entrapment_evidence";
        public const string DalAttestationReward = "dal_attestation_reward";

        public static bool TryParse(string type, out Operations res)
        {
            res = Operations.None;
            switch (type)
            {
                case Endorsement: res = Operations.Endorsements; break;
                case Preendorsement: res = Operations.Preendorsements; break;
                case Ballot: res = Operations.Ballots; break;
                case Proposal: res = Operations.Proposals; break;
                case Activation: res = Operations.Activations; break;
                case DoubleBaking: res = Operations.DoubleBakings; break;
                case DoubleEndorsing: res = Operations.DoubleEndorsings; break;
                case DoublePreendorsing: res = Operations.DoublePreendorsings; break;
                case NonceRevelation: res = Operations.Revelations; break;
                case VdfRevelation: res = Operations.VdfRevelation; break;
                case DrainDelegate: res = Operations.DrainDelegate; break;
                case Delegation: res = Operations.Delegations; break;
                case Origination: res = Operations.Originations; break;
                case Transaction: res = Operations.Transactions; break;
                case Reveal: res = Operations.Reveals; break;
                case RegisterConstant: res = Operations.RegisterConstant; break;
                case SetDepositsLimit: res = Operations.SetDepositsLimits; break;
                case Staking: res = Operations.Staking; break;
                case Migration: res = Operations.Migrations; break;
                case RevelationPenalty: res = Operations.RevelationPenalty; break;
                case Baking: res = Operations.Baking; break;
                case EndorsingReward: res = Operations.EndorsingRewards; break;
                case TxRollupOrigination: res = Operations.TxRollupOrigination; break;
                case TxRollupSubmitBatch: res = Operations.TxRollupSubmitBatch; break;
                case TxRollupCommit: res = Operations.TxRollupCommit; break;
                case TxRollupReturnBond: res = Operations.TxRollupReturnBond; break;
                case TxRollupFinalizeCommitment: res = Operations.TxRollupFinalizeCommitment; break;
                case TxRollupRemoveCommitment: res = Operations.TxRollupRemoveCommitment; break;
                case TxRollupRejection: res = Operations.TxRollupRejection; break;
                case TxRollupDispatchTickets: res = Operations.TxRollupDispatchTickets; break;
                case TransferTicket: res = Operations.TransferTicket; break;
                case IncreasePaidStorage: res = Operations.IncreasePaidStorage; break;
                case UpdateConsensusKey: res = Operations.UpdateConsensusKey; break;
                case SmartRollupAddMessages: res = Operations.SmartRollupAddMessages; break;
                case SmartRollupCement: res = Operations.SmartRollupCement; break;
                case SmartRollupExecute: res = Operations.SmartRollupExecute; break;
                case SmartRollupOriginate: res = Operations.SmartRollupOriginate; break;
                case SmartRollupPublish: res = Operations.SmartRollupPublish; break;
                case SmartRollupRecoverBond: res = Operations.SmartRollupRecoverBond; break;
                case SmartRollupRefute: res = Operations.SmartRollupRefute; break;
                case Autostaking: res = Operations.Autostaking; break;
                case SetDelegateParameters: res = Operations.SetDelegateParameters; break;
                case DalPublishCommitment: res = Operations.DalPublishCommitment; break;
                case DalEntrapmentEvidence: res = Operations.DalEntrapmentEvidence; break;
                case DalAttestationReward: res = Operations.DalAttestationReward; break;
                default: return false;
            }
            return true;
        }

        public static readonly HashSet<string> DefaultSet = new()
        {
            Ballot,
            Proposal,
            Activation,
            DoubleBaking,
            DoubleEndorsing,
            NonceRevelation,
            VdfRevelation,
            DrainDelegate,
            Delegation,
            Origination,
            Transaction,
            Reveal,
            RegisterConstant,
            SetDepositsLimit,
            Staking,
            Migration,
            RevelationPenalty,
            TransferTicket,
            TxRollupCommit,
            TxRollupDispatchTickets,
            TxRollupFinalizeCommitment,
            TxRollupOrigination,
            TxRollupRejection,
            TxRollupRemoveCommitment,
            TxRollupReturnBond,
            TxRollupSubmitBatch,
            IncreasePaidStorage,
            UpdateConsensusKey,
            SmartRollupAddMessages,
            SmartRollupCement,
            SmartRollupExecute,
            SmartRollupOriginate,
            SmartRollupPublish,
            SmartRollupRecoverBond,
            SmartRollupRefute,
            SetDelegateParameters,
            DalPublishCommitment,
            DalEntrapmentEvidence,
            DalAttestationReward,
        };
    }
}
