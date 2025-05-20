using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto1
{
    class CycleCommit(ProtocolHandler protocol) : ProtocolCommit(protocol)
    {
        public Cycle? FutureCycle { get; protected set; }
        public List<SnapshotBalance>? BakerSnapshots { get; protected set; }

        public virtual async Task Apply(Block block)
        {
            if (block.Events.HasFlag(BlockEvents.CycleBegin))
            {
                var futureCycle = block.Cycle + Context.Protocol.ConsensusRightsDelay;
                
                var lastSeed = await Db.Cycles
                    .AsNoTracking()
                    .Where(x => x.Index == futureCycle - 1)
                    .Select(x => x.Seed)
                    .FirstOrDefaultAsync()
                    ?? throw new Exception($"Seed for cycle {futureCycle - 1} is missed");
                
                var nonces = block.Cycle < 2 ? [] : await Db.NonceRevelationOps
                    .AsNoTracking()
                    .Where(x => x.RevealedCycle == block.Cycle - 2)
                    .OrderByDescending(x => x.RevealedLevel)
                    .Select(x => x.Nonce)
                    .ToListAsync();

                var futureSeed = Seed.GetNextSeed(lastSeed, nonces, null);
                var snapshotIndex = 0;
                var snapshotLevel = 1;
                var snapshotProto = await Cache.Protocols.FindByCycleAsync(Math.Max(block.Cycle - 2, 0));

                if (block.Cycle >= 2)
                {
                    snapshotIndex = Seed.GetSnapshotIndex(futureSeed, snapshotProto.SnapshotsPerCycle);
                    snapshotLevel = snapshotProto.GetCycleStart(block.Cycle - 2) - 1 + (snapshotIndex + 1) * snapshotProto.BlocksPerSnapshot;
                }

                BakerSnapshots = await Db.SnapshotBalances
                    .AsNoTracking()
                    .Where(x => x.Level == snapshotLevel && x.AccountId == x.BakerId)
                    .ToListAsync();

                FutureCycle = new Cycle
                {
                    Id = 0,
                    Index = futureCycle,
                    FirstLevel = Context.Protocol.GetCycleStart(futureCycle),
                    LastLevel = Context.Protocol.GetCycleEnd(futureCycle),
                    SnapshotLevel = snapshotLevel,
                    TotalBakers = BakerSnapshots.Count(x => x.StakingBalance >= snapshotProto.MinimalStake),
                    TotalBakingPower = BakerSnapshots.Sum(x => x.StakingBalance - x.StakingBalance % snapshotProto.MinimalStake),
                    Seed = futureSeed
                };

                Db.Cycles.Add(FutureCycle);
            }
        }

        public virtual async Task Revert(Block block)
        {
            if (block.Events.HasFlag(BlockEvents.CycleBegin))
            {
                var futureCycle = block.Cycle + Context.Protocol.ConsensusRightsDelay;

                await Db.Database.ExecuteSqlRawAsync("""
                    DELETE FROM "Cycles"
                    WHERE "Index" = {0}
                    """, futureCycle);
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
