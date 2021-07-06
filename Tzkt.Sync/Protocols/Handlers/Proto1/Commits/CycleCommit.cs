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
        public Cycle FutureCycle { get; protected set; }
        public Dictionary<int, DelegateSnapshot> Snapshots { get; protected set; }

        public CycleCommit(ProtocolHandler protocol) : base(protocol) { }

        public virtual async Task Apply(Block block)
        {
            if (block.Events.HasFlag(BlockEvents.CycleBegin))
            {
                var futureCycle = block.Cycle + block.Protocol.PreservedCycles;

                var rawCycle = await Proto.Rpc.GetCycleAsync(block.Level, futureCycle);

                var snapshotIndex = rawCycle.RequiredInt32("roll_snapshot");
                var snapshotLevel = 1;
                if (block.Cycle >= 2)
                {
                    var snapshotProto = await Cache.Protocols.FindByCycleAsync(block.Cycle - 2);
                    snapshotLevel = snapshotProto.GetCycleStart(block.Cycle - 2) - 1 + (snapshotIndex + 1) * snapshotProto.BlocksPerSnapshot;
                }
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
                    FirstLevel = block.Protocol.GetCycleStart(futureCycle),
                    LastLevel = block.Protocol.GetCycleEnd(futureCycle),
                    SnapshotIndex = snapshotIndex,
                    SnapshotLevel = snapshotLevel,
                    TotalRolls = Snapshots.Values.Sum(x => (int)(x.StakingBalance / block.Protocol.TokensPerRoll)),
                    TotalStaking = Snapshots.Values.Sum(x => x.StakingBalance),
                    TotalDelegated = Snapshots.Values.Sum(x => x.DelegatedBalance),
                    TotalDelegators = Snapshots.Values.Sum(x => x.DelegatorsCount),
                    TotalBakers = Snapshots.Count,
                    Seed = rawCycle.RequiredString("random_seed")
                };

                Db.Cycles.Add(FutureCycle);
            }
        }

        public virtual async Task Revert(Block block)
        {
            if (block.Events.HasFlag(BlockEvents.CycleBegin))
            {
                block.Protocol ??= await Cache.Protocols.GetAsync(block.ProtoCode);
                var futureCycle = block.Cycle + block.Protocol.PreservedCycles;

                await Db.Database.ExecuteSqlRawAsync($@"
                    DELETE  FROM ""Cycles""
                    WHERE   ""Index"" = {futureCycle}");
            }
        }

        public class DelegateSnapshot
        {
            public long StakingBalance { get; set; }
            public long DelegatedBalance { get; set; }
            public int DelegatorsCount { get; set; }
        }
    }
}
