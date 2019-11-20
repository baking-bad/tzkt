using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tzkt.Api.Services.Cache
{
    public class RawDelegate : RawUser
    {
        public int ActivationLevel { get; set; }
        public int DeactivationLevel { get; set; }

        public long FrozenDeposits { get; set; }
        public long FrozenRewards { get; set; }
        public long FrozenFees { get; set; }

        public int Delegators { get; set; }
        public long StakingBalance { get; set; }
    }
}
