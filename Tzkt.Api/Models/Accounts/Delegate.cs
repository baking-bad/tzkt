using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Tzkt.Api.Models
{
    public class Delegate : Account
    {
        public override string Type => AccountTypes.Delegate;

        public bool Active { get; set; }

        public string Alias { get; set; }

        public string Address { get; set; }

        public string PublicKey { get; set; }

        public long Balance { get; set; }

        public long FrozenDeposits { get; set; }

        public long FrozenRewards { get; set; }

        public long FrozenFees { get; set; }

        public int Counter { get; set; }

        public int ActivationLevel { get; set; }

        public int? DeactivationLevel { get; set; }

        public long StakingBalance { get; set; }

        public int NumContracts { get; set; }

        public int NumDelegators { get; set; }

        public int NumEndorsements { get; set; }

        public int NumBallots { get; set; }

        public int NumProposals { get; set; }

        public int NumActivations { get; set; }

        public int NumDoubleBaking { get; set; }

        public int NumDoubleEndorsing { get; set; }

        public int NumNonceRevelations { get; set; }

        public int NumDelegations { get; set; }

        public int NumOriginations { get; set; }

        public int NumTransactions { get; set; }

        public int NumReveals { get; set; }

        public int NumSystem { get; set; }

        public int FirstActivity { get; set; }

        public int LastActivity { get; set; }

        public IEnumerable<RelatedContract> Contracts { get; set; }

        public IEnumerable<Delegator> Delegators { get; set; }

        public IEnumerable<Operation> Operations { get; set; }
    }
}
