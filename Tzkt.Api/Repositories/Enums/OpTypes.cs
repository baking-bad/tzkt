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

        public const string Delegation = "delegation";
        public const string Origination = "origination";
        public const string Transaction = "transaction";
        public const string Reveal = "reveal";
        public const string RegisterConstant = "register_constant";
        public const string SetDepositsLimit = "set_deposits_limit";

        public const string Migration = "migration";
        public const string RevelationPenalty = "revelation_penalty";
        public const string Baking = "baking";
        public const string EndorsingReward = "endorsing_reward";
        public const string TokenTransfer = "token_transfer";

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
            Delegation,
            Origination,
            Transaction,
            Reveal,
            RegisterConstant,
            SetDepositsLimit,
            Migration,
            RevelationPenalty
        };
    }
}
