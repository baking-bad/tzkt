using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto1
{
    class CycleCommit : ProtocolCommit
    {
        public Block Block { get; private set; }
        public Cycle FutureCycle { get; private set; }
        public Dictionary<int, DelegateSnapshot> Snapshots { get; private set; }

        CycleCommit(ProtocolHandler protocol) : base(protocol) { }

        public override async Task Apply()
        {
            if (Block.Events.HasFlag(BlockEvents.CycleBegin))
            {
                var currentCycle = (Block.Level - 1) / Block.Protocol.BlocksPerCycle;
                var futureCycle = currentCycle + Block.Protocol.PreservedCycles;

                var cycleStream = await Proto.Node.GetCycleAsync(Block.Level, futureCycle);
                var rawCycle = await (Proto.Serializer as Serializer).DeserializeCycle(cycleStream);

                var snapshotLevel = Math.Max(1, (currentCycle - 2) * Block.Protocol.BlocksPerCycle + (rawCycle.RollSnapshot + 1) * Block.Protocol.BlocksPerSnapshot);
                var snapshotBalances = await Db.SnapshotBalances.AsNoTracking().Where(x => x.Level == snapshotLevel).ToListAsync();

                Snapshots = new Dictionary<int, DelegateSnapshot>(512);
                foreach (var s in snapshotBalances)
                {
                    if (s.DelegateId == null)
                    {
                        if (!Snapshots.TryGetValue(s.AccountId, out var snapshot))
                        {
                            snapshot = new DelegateSnapshot();
                            Snapshots.Add(s.AccountId, snapshot);
                        }

                        snapshot.StakingBalance += s.Balance;
                    }
                    else
                    {
                        if (!Snapshots.TryGetValue((int)s.DelegateId, out var snapshot))
                        {
                            snapshot = new DelegateSnapshot();
                            Snapshots.Add((int)s.DelegateId, snapshot);
                        }

                        snapshot.StakingBalance += s.Balance;
                        snapshot.DelegatedBalance += s.Balance;
                        snapshot.DelegatorsCount++;
                    }
                }

                FutureCycle = new Cycle
                {
                    Index = futureCycle,
                    SnapshotLevel = snapshotLevel,
                    TotalRolls = Snapshots.Values.Sum(x => (int)(x.StakingBalance / Block.Protocol.TokensPerRoll)),
                    TotalStaking = Snapshots.Values.Sum(x => x.StakingBalance),
                    TotalDelegated = Snapshots.Values.Sum(x => x.DelegatedBalance),
                    TotalDelegators = Snapshots.Values.Sum(x => x.DelegatorsCount),
                    TotalBakers = Snapshots.Count,
                    Seed = rawCycle.RandomSeed
                };

                Db.Cycles.Add(FutureCycle);
            }
        }

        public override async Task Revert()
        {
            if (Block.Events.HasFlag(BlockEvents.CycleBegin))
            {
                Block.Protocol ??= await Cache.Protocols.GetAsync(Block.ProtoCode);
                var futureCycle = (Block.Level - 1) / Block.Protocol.BlocksPerCycle + Block.Protocol.PreservedCycles;

                await Db.Database.ExecuteSqlRawAsync($@"
                    DELETE  FROM ""Cycles""
                    WHERE   ""Index"" = {futureCycle}");
            }
        }

        #region static
        public static async Task<CycleCommit> Apply(ProtocolHandler proto, Block block)
        {
            var commit = new CycleCommit(proto) { Block = block };
            await commit.Apply();
            return commit;
        }

        public static async Task<CycleCommit> Revert(ProtocolHandler proto, Block block)
        {
            var commit = new CycleCommit(proto) { Block = block };
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
