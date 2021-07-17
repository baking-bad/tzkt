using System.Collections.Generic;
using Tzkt.Data.Models;

namespace Tzkt.Api
{
    static class OpTypes
    {
        public const string Endorsement = "endorsement";

        public const string Ballot = "ballot";
        public const string Proposal = "proposal";

        public const string Activation = "activation";
        public const string DoubleBaking = "double_baking";
        public const string DoubleEndorsing = "double_endorsing";
        public const string NonceRevelation = "nonce_revelation";

        public const string Delegation = "delegation";
        public const string Origination = "origination";
        public const string Transaction = "transaction";
        public const string Reveal = "reveal";

        public const string Migration = "migration";
        public const string RevelationPenalty = "revelation_penalty";
        public const string Baking = "baking";

        public static bool TryParse(IEnumerable<string> types, out Operations res)
        {
            res = Operations.None;
            foreach (var type in types)
            {
                switch (type)
                {
                    case Endorsement: res |= Operations.Endorsements; break;
                    case Ballot: res |= Operations.Ballots; break;
                    case Proposal: res |= Operations.Proposals; break;
                    case Activation: res |= Operations.Activations; break;
                    case DoubleBaking: res |= Operations.DoubleBakings; break;
                    case DoubleEndorsing: res |= Operations.DoubleEndorsings; break;
                    case NonceRevelation: res |= Operations.Revelations; break;
                    case Delegation: res |= Operations.Delegations; break;
                    case Origination: res |= Operations.Originations; break;
                    case Transaction: res |= Operations.Transactions; break;
                    case Reveal: res |= Operations.Reveals; break;
                    case Migration: res |= Operations.Migrations; break;
                    case RevelationPenalty: res |= Operations.RevelationPenalty; break;
                    case Baking: res |= Operations.Baking; break;
                    default: return false;
                }
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
            Migration,
            RevelationPenalty
        };
    }
}
