using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Initiator
{
    class DelegatorCycleCommit : ProtocolCommit
    {
        public Block Block { get; private set; }
        public List<Account> BootstrapedAccounts { get; private set; }

        DelegatorCycleCommit(ProtocolHandler protocol) : base(protocol) { }

        public override Task Apply()
        {
            for (int cycle = 0; cycle <= Block.Protocol.PreservedCycles; cycle++)
            {
                Db.DelegatorCycles.AddRange(BootstrapedAccounts
                    .Where(x => x.Staked && x.Type != AccountType.Delegate)
                    .Select(x => new DelegatorCycle
                    {
                        Cycle = cycle,
                        BakerId = (int)x.DelegateId,
                        Balance = x.Balance,
                        DelegatorId = x.Id
                    }));
            }

            return Task.CompletedTask;
        }

        public override async Task Revert()
        {
            await Db.Database.ExecuteSqlRawAsync(@"DELETE FROM ""DelegatorCycles""");
        }

        #region static
        public static async Task<DelegatorCycleCommit> Apply(ProtocolHandler proto, Block block, List<Account> accounts)
        {
            var commit = new DelegatorCycleCommit(proto) { Block = block, BootstrapedAccounts = accounts };
            await commit.Apply();
            return commit;
        }

        public static async Task<DelegatorCycleCommit> Revert(ProtocolHandler proto)
        {
            var commit = new DelegatorCycleCommit(proto);
            await commit.Revert();
            return commit;
        }
        #endregion
    }
}
