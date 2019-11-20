using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Tzkt.Api.Models
{
    public class Delegate : IAccount
    {
        public string Type => "delegate";

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

        public int DelegatorsCount { get; set; }

        public long StakingBalance { get; set; }

        public int FirstActivity { get; set; }

        public int LastActivity { get; set; }
    }
}
