using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto12
{
    class CycleCommit : ProtocolCommit
    {
        public Cycle FutureCycle { get; protected set; }
        public List<SnapshotBalance> Snapshots { get; protected set; }

        public CycleCommit(ProtocolHandler protocol) : base(protocol) { }

        public virtual async Task Apply(Block block)
        {
            if (!block.Events.HasFlag(BlockEvents.CycleBegin))
                return;
            
            var futureCycle = block.Cycle + block.Protocol.PreservedCycles;

            var lastSeed = await Db.Cycles
                .AsNoTracking()
                .Where(x => x.Index == futureCycle - 1)
                .Select(x => x.Seed)
                .FirstOrDefaultAsync()
                ?? throw new Exception($"Seed for cycle {futureCycle - 1} is missed");

            var nonces = block.Cycle < 2 ? Enumerable.Empty<byte[]>() : await Db.NonceRevelationOps
                .AsNoTracking()
                .Where(x => x.RevealedCycle == block.Cycle - 2)
                .OrderByDescending(x => x.RevealedLevel)
                .Select(x => x.Nonce)
                .ToListAsync();

            var futureSeed = Seed.GetNextSeed(lastSeed, nonces);
            var snapshotIndex = 0;
            var snapshotLevel = 1;

            if (block.Cycle >= 2)
            {
                if (block.Cycle == block.Protocol.FirstCycle)
                {
                    snapshotLevel = block.Protocol.FirstLevel - 1;
                }
                else
                {
                    var snapshotProto = await Cache.Protocols.FindByCycleAsync(block.Cycle - 1);
                    snapshotIndex = Seed.GetSnapshotIndex(futureSeed, true);
                    #region WTF?!
                    if (Cache.AppState.Get().Chain == "mainnet" && block.Cycle == 469)
                        snapshotIndex = 15;
                    #endregion
                    snapshotLevel = snapshotProto.GetCycleStart(block.Cycle - 1) - 1 + (snapshotIndex + 1) * snapshotProto.BlocksPerSnapshot;
                }
            }

            Snapshots = await Db.SnapshotBalances
                .AsNoTracking()
                .Where(x => x.Level == snapshotLevel && x.DelegateId == null)
                .ToListAsync();

            var selectedStakes = Snapshots
                .Where(x => x.StakingBalance >= block.Protocol.TokensPerRoll)
                .Select(x =>
                {
                    var baker = Cache.Accounts.GetDelegate(x.AccountId);
                    var depositCap = Math.Min(x.Balance, baker.FrozenDepositLimit ?? (long.MaxValue / 100));
                    return Math.Min((long)x.StakingBalance, depositCap * 100 / block.Protocol.FrozenDepositsPercentage);
                });

            FutureCycle = new Cycle
            {
                Index = futureCycle,
                FirstLevel = block.Protocol.GetCycleStart(futureCycle),
                LastLevel = block.Protocol.GetCycleEnd(futureCycle),
                SnapshotIndex = snapshotIndex,
                SnapshotLevel = snapshotLevel,
                TotalStaking = Snapshots.Sum(x => (long)x.StakingBalance),
                TotalDelegated = Snapshots.Sum(x => (long)x.DelegatedBalance),
                TotalDelegators = Snapshots.Sum(x => (int)x.DelegatorsCount),
                SelectedBakers = selectedStakes.Count(),
                SelectedStake = selectedStakes.Sum(),
                TotalBakers = Snapshots.Count,
                Seed = futureSeed
            };

            Db.Cycles.Add(FutureCycle);
        }

        public virtual async Task Revert(Block block)
        {
            if (!block.Events.HasFlag(BlockEvents.CycleBegin))
                return;

            block.Protocol ??= await Cache.Protocols.GetAsync(block.ProtoCode);
            var futureCycle = block.Cycle + block.Protocol.PreservedCycles;

            await Db.Database.ExecuteSqlRawAsync($@"
                DELETE  FROM ""Cycles""
                WHERE   ""Index"" = {futureCycle}");
        }
    }
}
