using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Initiator
{
    class CycleCommit : ProtocolCommit
    {
        public Block Block { get; private set; }
        public List<Account> BootstrapedAccounts { get; private set; }
        public Dictionary<int, DelegateSnapshot> Snapshots { get; private set; }

        CycleCommit(ProtocolHandler protocol) : base(protocol) { }

        public override async Task Apply()
        {
            Snapshots = BootstrapedAccounts
                .Where(x => x.Type == AccountType.Delegate)
                .Select(x => x as Data.Models.Delegate)
                .ToDictionary(x => x.Id, x => new DelegateSnapshot
                {
                    StakingBalance = x.StakingBalance,
                    DelegatedBalance = x.StakingBalance - x.Balance, //nothing is frozen yet
                    DelegatorsCount = x.DelegatorsCount
                });

            var totalRolls = Snapshots.Values.Sum(x => (int)(x.StakingBalance / Block.Protocol.TokensPerRoll));
            var totalStake = Snapshots.Values.Sum(x => x.StakingBalance);
            var totalDelegated = Snapshots.Values.Sum(x => x.DelegatedBalance);
            var totalDelegators = Snapshots.Values.Sum(x => x.DelegatorsCount);
            var totalBakers = Snapshots.Count;

            for (int cycle = 0; cycle <= Block.Protocol.PreservedCycles; cycle++)
            {
                var rawCycle = await Proto.Rpc.GetCycleAsync(1, cycle);
                Db.Cycles.Add(new Cycle
                {
                    Index = cycle,
                    SnapshotIndex = 0,
                    SnapshotLevel = 1,
                    TotalRolls = totalRolls,
                    TotalStaking = totalStake,
                    TotalDelegated = totalDelegated,
                    TotalDelegators = totalDelegators,
                    TotalBakers = totalBakers,
                    Seed = rawCycle.RequiredString("random_seed")
                });
            }
        }

        public override async Task Revert()
        {
            await Db.Database.ExecuteSqlRawAsync(@"DELETE FROM ""Cycles""");
        }

        #region static
        public static async Task<CycleCommit> Apply(ProtocolHandler proto, Block block, List<Account> accounts)
        {
            var commit = new CycleCommit(proto) { Block = block, BootstrapedAccounts = accounts };
            await commit.Apply();

            return commit;
        }

        public static async Task<CycleCommit> Revert(ProtocolHandler proto)
        {
            var commit = new CycleCommit(proto);
            await commit.Revert();

            return commit;
        }
        #endregion

        public class DelegateSnapshot
        {
            public long StakingBalance { get; set; }
            public long DelegatedBalance { get; set; }
            public int DelegatorsCount { get; set; }
        }
    }
}
