﻿using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using Mvkt.Data.Models;

namespace Mvkt.Sync.Protocols.Proto12
{
    partial class ProtoActivator : Proto11.ProtoActivator
    {
        public override List<Cycle> BootstrapCycles(Protocol protocol, List<Account> accounts, JToken parameters)
        {
            var cycles = base.BootstrapCycles(protocol, accounts, parameters);
            
            var delegates = accounts
                .Where(x => x is Data.Models.Delegate d && d.StakingBalance >= protocol.MinimalStake)
                .Select(x => x as Data.Models.Delegate);

            foreach (var cycle in cycles)
            {
                cycle.TotalBakingPower = delegates.Sum(x => Math.Min(x.StakingBalance, x.Balance * (protocol.MaxDelegatedOverFrozenRatio + 1)));
                cycle.TotalBakers = delegates.Count();
            }

            return cycles;
        }
    }
}
