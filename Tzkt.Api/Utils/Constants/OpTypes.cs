using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

        public static readonly HashSet<string> DefaultSet = new HashSet<string>
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
