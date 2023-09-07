﻿using Newtonsoft.Json.Linq;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto12
{
    partial class ProtoActivator : Proto11.ProtoActivator
    {
        protected override async Task<List<Account>> BootstrapAccounts(Protocol protocol, JToken parameters)
        {
            var accounts = await base.BootstrapAccounts(protocol, parameters);

            foreach (var account in accounts.Where(x => x.Type == AccountType.Delegate))
            {
                var baker = account as Data.Models.Delegate;
                baker.FrozenDeposit = baker.StakingBalance >= protocol.MinimalStake
                    ? baker.StakingBalance / (protocol.MaxDelegatedOverFrozenRatio + 1)
                    : 0;
            }

            Cache.Statistics.Current.TotalFrozen = accounts.Sum(x => (x as Data.Models.Delegate)?.FrozenDeposit ?? 0);

            return accounts;
        }
    }
}
