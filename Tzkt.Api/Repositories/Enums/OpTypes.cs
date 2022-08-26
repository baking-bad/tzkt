using System.Collections.Generic;
using Tzkt.Data.Models;

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

        public const string Delegation = "delegation";
        public const string Origination = "origination";
        public const string Transaction = "transaction";
        public const string Reveal = "reveal";
        public const string RegisterConstant = "register_constant";
        public const string SetDepositsLimit = "set_deposits_limit";

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

        public const string Migration = "migration";
        public const string RevelationPenalty = "revelation_penalty";
        public const string Baking = "baking";
        public const string EndorsingReward = "endorsing_reward";

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
                case Delegation: res = Operations.Delegations; break;
                case Origination: res = Operations.Originations; break;
                case Transaction: res = Operations.Transactions; break;
                case Reveal: res = Operations.Reveals; break;
                case RegisterConstant: res = Operations.RegisterConstant; break;
                case SetDepositsLimit: res = Operations.SetDepositsLimits; break;
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
            Delegation,
            Origination,
            Transaction,
            Reveal,
            RegisterConstant,
            SetDepositsLimit,
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
            IncreasePaidStorage
        };
    }
}
