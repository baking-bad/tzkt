using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto13
{
    class CycleCommit : ProtocolCommit
    {
        public Cycle FutureCycle { get; protected set; }
        public List<SnapshotBalance> Snapshots { get; protected set; }
        public Dictionary<int, long> SelectedStakes { get; protected set; }

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

            var vdfSolution = await GetVdfSolution(block);

            var futureSeed = Seed.GetNextSeed(lastSeed, nonces, vdfSolution);
            var snapshotIndex = 0;
            var snapshotLevel = 1;

            if (block.Cycle >= 1)
            {
                var snapshotProto = await Cache.Protocols.FindByCycleAsync(block.Cycle - 1);
                snapshotIndex = Seed.GetSnapshotIndex(futureSeed, snapshotProto.SnapshotsPerCycle, true);
                snapshotLevel = snapshotProto.GetCycleStart(block.Cycle - 1) - 1 + (snapshotIndex + 1) * snapshotProto.BlocksPerSnapshot;
            }

            Snapshots = await Db.SnapshotBalances
                .AsNoTracking()
                .Where(x => x.Level == snapshotLevel && x.DelegateId == null)
                .ToListAsync();

            var endorsingRewards = await Db.BakerCycles
                .AsNoTracking()
                .Where(x => x.Cycle == block.Cycle - 1 && x.EndorsementRewards > 0)
                .ToDictionaryAsync(x => x.BakerId, x => x.EndorsementRewards);

            SelectedStakes = Snapshots
                .Where(x => x.StakingBalance >= block.Protocol.TokensPerRoll)
                .ToDictionary(x => x.AccountId, x =>
                {
                    var baker = Cache.Accounts.GetDelegate(x.AccountId);

                    var lastBalance = baker.Balance;
                    if (endorsingRewards.TryGetValue(baker.Id, out var reward))
                        lastBalance -= reward;
                    if (block.ProposerId == baker.Id)
                        lastBalance -= block.Reward;
                    if (block.ProducerId == baker.Id)
                        lastBalance -= block.Bonus;

                    var depositCap = Math.Min(lastBalance, baker.FrozenDepositLimit ?? (long.MaxValue / 100));
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
                SelectedBakers = SelectedStakes.Count,
                SelectedStake = SelectedStakes.Values.Sum(),
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

        protected virtual Task<byte[]> GetVdfSolution(Block block) => Task.FromResult<byte[]>(null);
    }
}
