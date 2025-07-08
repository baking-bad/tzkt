using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto13
{
    class CycleCommit(ProtocolHandler protocol) : ProtocolCommit(protocol)
    {
        public Cycle? FutureCycle { get; protected set; }
        public List<SnapshotBalance>? Snapshots { get; protected set; }
        public Dictionary<int, long>? SelectedStakes { get; protected set; }

        public virtual async Task Apply(Block block)
        {
            if (!block.Events.HasFlag(BlockEvents.CycleBegin))
                return;

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

            var vdfSolution = await GetVdfSolution(block);

            var futureSeed = Seed.GetNextSeed(lastSeed, nonces, vdfSolution);
            var snapshotLevel = 1;

            if (block.Cycle >= 1)
            {
                var snapshotProto = await Cache.Protocols.FindByCycleAsync(block.Cycle - 1);
                var snapshotIndex = Seed.GetSnapshotIndex(futureSeed, snapshotProto.SnapshotsPerCycle, true);
                snapshotLevel = snapshotProto.GetCycleStart(block.Cycle - 1) - 1 + (snapshotIndex + 1) * snapshotProto.BlocksPerSnapshot;
            }

            Snapshots = await Db.SnapshotBalances
                .AsNoTracking()
                .Where(x => x.Level == snapshotLevel && x.BakerId == x.AccountId)
                .ToListAsync();

            SelectedStakes = await GetSelectedStakes(block, Context.Protocol, Snapshots);

            FutureCycle = new Cycle
            {
                Id = 0,
                Index = futureCycle,
                FirstLevel = Context.Protocol.GetCycleStart(futureCycle),
                LastLevel = Context.Protocol.GetCycleEnd(futureCycle),
                SnapshotLevel = snapshotLevel,
                TotalBakers = SelectedStakes.Count,
                TotalBakingPower = SelectedStakes.Values.Sum(),
                Seed = futureSeed
            };

            Db.Cycles.Add(FutureCycle);
        }

        public virtual async Task Revert(Block block)
        {
            if (!block.Events.HasFlag(BlockEvents.CycleBegin))
                return;

            var futureCycle = block.Cycle + Context.Protocol.ConsensusRightsDelay;

            await Db.Database.ExecuteSqlRawAsync("""
                DELETE FROM "Cycles"
                WHERE "Index" = {0}
                """, futureCycle);
        }

        protected virtual Task<byte[]?> GetVdfSolution(Block block) => Task.FromResult<byte[]?>(null);

        protected virtual async Task<Dictionary<int, long>> GetSelectedStakes(Block block, Protocol protocol, List<SnapshotBalance> snapshots)
        {
            var attestationRewards = await Db.BakerCycles
                .AsNoTracking()
                .Where(x => x.Cycle == block.Cycle - 1 && x.AttestationRewardsDelegated > 0)
                .ToDictionaryAsync(x => x.BakerId, x => x.AttestationRewardsDelegated);

            return snapshots
                .Where(x => x.StakingBalance >= protocol.MinimalStake)
                .ToDictionary(x => x.AccountId, x =>
                {
                    var baker = Cache.Accounts.GetDelegate(x.AccountId);

                    var lastBalance = baker.Balance;
                    if (attestationRewards.TryGetValue(baker.Id, out var reward))
                        lastBalance -= reward;
                    if (block.ProposerId == baker.Id)
                        lastBalance -= block.RewardDelegated;
                    if (block.ProducerId == baker.Id)
                        lastBalance -= block.BonusDelegated;

                    var depositCap = Math.Min(lastBalance, baker.FrozenDepositLimit ?? (long.MaxValue / 100));
                    return Math.Min((long)x.StakingBalance, depositCap * (protocol.MaxDelegatedOverFrozenRatio + 1));
                });
        }
    }
}
