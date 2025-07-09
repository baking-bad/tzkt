using Tzkt.Data.Models;

namespace Tzkt.Api
{
    public static class ActivityTypes
    {
        public const string Activation = "activation";
        public const string Autostaking = "autostaking";
        public const string Baking = "baking";
        public const string Ballot = "ballot";
        public const string DalAttestationReward = "dal_attestation_reward";
        public const string DalEntrapmentEvidence = "dal_entrapment_evidence";
        public const string DalPublishCommitment = "dal_publish_commitment";
        public const string Delegation = "delegation";
        public const string DoubleBaking = "double_baking";
        public const string DoubleConsensus = "double_consensus";
        public const string DrainDelegate = "drain_delegate";
        public const string Attestation = "attestation";
        public const string AttestationReward = "attestation_reward";
        public const string IncreasePaidStorage = "increase_paid_storage";
        public const string Migration = "migration";
        public const string NonceRevelation = "nonce_revelation";
        public const string Origination = "origination";
        public const string Preattestation = "preattestation";
        public const string Proposal = "proposal";
        public const string RegisterConstant = "register_constant";
        public const string Reveal = "reveal";
        public const string RevelationPenalty = "revelation_penalty";
        public const string SetDelegateParameters = "set_delegate_parameters";
        public const string SetDepositsLimit = "set_deposits_limit";
        public const string SmartRollupAddMessages = "sr_add_messages";
        public const string SmartRollupCement = "sr_cement";
        public const string SmartRollupExecute = "sr_execute";
        public const string SmartRollupOriginate = "sr_originate";
        public const string SmartRollupPublish = "sr_publish";
        public const string SmartRollupRecoverBond = "sr_recover_bond";
        public const string SmartRollupRefute = "sr_refute";
        public const string Staking = "staking";
        public const string Transaction = "transaction";
        public const string TransferTicket = "transfer_ticket";
        public const string TxRollupCommit = "tx_rollup_commit";
        public const string TxRollupDispatchTickets = "tx_rollup_dispatch_tickets";
        public const string TxRollupFinalizeCommitment = "tx_rollup_finalize_commitment";
        public const string TxRollupOrigination = "tx_rollup_origination";
        public const string TxRollupRejection = "tx_rollup_rejection";
        public const string TxRollupRemoveCommitment = "tx_rollup_remove_commitment";
        public const string TxRollupReturnBond = "tx_rollup_return_bond";
        public const string TxRollupSubmitBatch = "tx_rollup_submit_batch";
        public const string UpdateSecondaryKey = "update_secondary_key";
        public const string VdfRevelation = "vdf_revelation";
        public const string TicketTransfer = "ticket_transfer";
        public const string TokenTransfer = "token_transfer";

        public static bool IsValid(string value) => value switch
        {
            Activation => true,
            Autostaking => true,
            Baking => true,
            Ballot => true,
            DalAttestationReward => true,
            DalEntrapmentEvidence => true,
            DalPublishCommitment => true,
            Delegation => true,
            DoubleBaking => true,
            DoubleConsensus => true,
            DrainDelegate => true,
            Attestation => true,
            AttestationReward => true,
            IncreasePaidStorage => true,
            Migration => true,
            NonceRevelation => true,
            Origination => true,
            Preattestation => true,
            Proposal => true,
            RegisterConstant => true,
            Reveal => true,
            RevelationPenalty => true,
            SetDelegateParameters => true,
            SetDepositsLimit => true,
            SmartRollupAddMessages => true,
            SmartRollupCement => true,
            SmartRollupExecute => true,
            SmartRollupOriginate => true,
            SmartRollupPublish => true,
            SmartRollupRecoverBond => true,
            SmartRollupRefute => true,
            Staking => true,
            Transaction => true,
            TransferTicket => true,
            TxRollupCommit => true,
            TxRollupDispatchTickets => true,
            TxRollupFinalizeCommitment => true,
            TxRollupOrigination => true,
            TxRollupRejection => true,
            TxRollupRemoveCommitment => true,
            TxRollupReturnBond => true,
            TxRollupSubmitBatch => true,
            UpdateSecondaryKey => true,
            VdfRevelation => true,
            TicketTransfer => true,
            TokenTransfer => true,
            _ => false
        };

        public static bool TryParseOperation(string type, out Operations res)
        {
            res = Operations.None;
            switch (type)
            {
                case Activation: res = Operations.Activations; break;
                case Autostaking: res = Operations.Autostaking; break;
                case Baking: res = Operations.Baking; break;
                case Ballot: res = Operations.Ballots; break;
                case DalAttestationReward: res = Operations.DalAttestationReward; break;
                case DalEntrapmentEvidence: res = Operations.DalEntrapmentEvidence; break;
                case DalPublishCommitment: res = Operations.DalPublishCommitment; break;
                case Delegation: res = Operations.Delegations; break;
                case DoubleBaking: res = Operations.DoubleBakings; break;
                case DoubleConsensus: res = Operations.DoubleConsensus; break;
                case DrainDelegate: res = Operations.DrainDelegate; break;
                case Attestation: res = Operations.Attestations; break;
                case AttestationReward: res = Operations.AttestationRewards; break;
                case IncreasePaidStorage: res = Operations.IncreasePaidStorage; break;
                case Migration: res = Operations.Migrations; break;
                case NonceRevelation: res = Operations.Revelations; break;
                case Origination: res = Operations.Originations; break;
                case Preattestation: res = Operations.Preattestations; break;
                case Proposal: res = Operations.Proposals; break;
                case RegisterConstant: res = Operations.RegisterConstant; break;
                case Reveal: res = Operations.Reveals; break;
                case RevelationPenalty: res = Operations.RevelationPenalty; break;
                case SetDelegateParameters: res = Operations.SetDelegateParameters; break;
                case SetDepositsLimit: res = Operations.SetDepositsLimits; break;
                case SmartRollupAddMessages: res = Operations.SmartRollupAddMessages; break;
                case SmartRollupCement: res = Operations.SmartRollupCement; break;
                case SmartRollupExecute: res = Operations.SmartRollupExecute; break;
                case SmartRollupOriginate: res = Operations.SmartRollupOriginate; break;
                case SmartRollupPublish: res = Operations.SmartRollupPublish; break;
                case SmartRollupRecoverBond: res = Operations.SmartRollupRecoverBond; break;
                case SmartRollupRefute: res = Operations.SmartRollupRefute; break;
                case Staking: res = Operations.Staking; break;
                case Transaction: res = Operations.Transactions; break;
                case TransferTicket: res = Operations.TransferTicket; break;
                case TxRollupCommit: res = Operations.TxRollupCommit; break;
                case TxRollupDispatchTickets: res = Operations.TxRollupDispatchTickets; break;
                case TxRollupFinalizeCommitment: res = Operations.TxRollupFinalizeCommitment; break;
                case TxRollupOrigination: res = Operations.TxRollupOrigination; break;
                case TxRollupRejection: res = Operations.TxRollupRejection; break;
                case TxRollupRemoveCommitment: res = Operations.TxRollupRemoveCommitment; break;
                case TxRollupReturnBond: res = Operations.TxRollupReturnBond; break;
                case TxRollupSubmitBatch: res = Operations.TxRollupSubmitBatch; break;
                case UpdateSecondaryKey: res = Operations.UpdateSecondaryKey; break;
                case VdfRevelation: res = Operations.VdfRevelation; break;
                default: return false;
            }
            return true;
        }

        public static readonly HashSet<string> Default =
        [
            Activation,
            //Autostaking,
            Baking,
            Ballot,
            DalAttestationReward,
            DalEntrapmentEvidence,
            DalPublishCommitment,
            Delegation,
            DoubleBaking,
            DoubleConsensus,
            DrainDelegate,
            //Attestation,
            AttestationReward,
            IncreasePaidStorage,
            Migration,
            NonceRevelation,
            Origination,
            //Preattestation,
            Proposal,
            RegisterConstant,
            Reveal,
            //RevelationPenalty,
            SetDelegateParameters,
            //SetDepositsLimit,
            SmartRollupAddMessages,
            SmartRollupCement,
            SmartRollupExecute,
            SmartRollupOriginate,
            SmartRollupPublish,
            SmartRollupRecoverBond,
            SmartRollupRefute,
            Staking,
            Transaction,
            TransferTicket,
            //TxRollupCommit,
            //TxRollupDispatchTickets,
            //TxRollupFinalizeCommitment,
            //TxRollupOrigination,
            //TxRollupRejection,
            //TxRollupRemoveCommitment,
            //TxRollupReturnBond,
            //TxRollupSubmitBatch,
            UpdateSecondaryKey,
            VdfRevelation,
            TicketTransfer,
            TokenTransfer,
        ];
    }
}
