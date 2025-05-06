using Microsoft.EntityFrameworkCore;
using Tzkt.Data;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Services.Cache
{
    public class SmartRollupStakesCache(TzktContext db)
    {
        #region static
        static int SoftCap = 0;
        static int TargetCap = 0;
        static Dictionary<int, Dictionary<int, int>> Cached = [];

        public static void Configure(CacheSize? size)
        {
            SoftCap = size?.SoftCap ?? 10_000;
            TargetCap = size?.TargetCap ?? 5000;
            Cached = new(SoftCap + 512);
        }
        #endregion

        readonly TzktContext Db = db;

        public void Reset()
        {
            Cached.Clear();
        }

        public void Trim()
        {
            if (Cached.Count > SoftCap)
            {
                var toRemove = Cached.Keys
                    .OrderBy(x => x)
                    .Take(Cached.Count - TargetCap)
                    .ToList();

                foreach (var item in toRemove)
                    Remove(item);
            }
        }

        public void Add(SmartRollupCommitment commitment)
        {
            Cached.Add(commitment.Id, new() { { commitment.InitiatorId, 1 } });
        }

        public void Remove(int commitmentId)
        {
            Cached.Remove(commitmentId);
        }

        public void Remove(int commitmentId, int stakerId)
        {
            if (Cached.TryGetValue(commitmentId, out var stakers))
                stakers.Remove(stakerId);
        }

        public async Task<int?> GetAsync(SmartRollupCommitment commitment, int stakerId)
        {
            if (!Cached.TryGetValue(commitment.Id, out var stakers))
            {
                stakers = await GetStakers(commitment);
                Cached.Add(commitment.Id, stakers);
            }
            return stakers.TryGetValue(stakerId, out var stake) ? stake : null;
        }

        public async Task SetAsync(SmartRollupCommitment commitment, int stakerId, int stake)
        {
            if (!Cached.TryGetValue(commitment.Id, out var stakers))
            {
                stakers = await GetStakers(commitment);
                Cached.Add(commitment.Id, stakers);
            }
            stakers[stakerId] = stake;
        }

        public void Set(int commitmentId, int stakerId, int stake)
        {
            if (Cached.TryGetValue(commitmentId, out var stakers))
                stakers[stakerId] = stake;
        }

        async Task<Dictionary<int, int>> GetStakers(SmartRollupCommitment commitment)
        {
            if (commitment.Stakers == 1)
                return new() { { commitment.InitiatorId, commitment.ActiveStakers } };
            
            if (commitment.Stakers == commitment.ActiveStakers)
                return await Db.SmartRollupPublishOps
                    .AsNoTracking()
                    .Where(x => x.CommitmentId == commitment.Id && x.Flags.HasFlag(SmartRollupPublishFlags.AddStaker))
                    .Select(x => x.SenderId)
                    .ToDictionaryAsync(x => x, x => 1);
            
            var stakers = new Dictionary<int, int>();
            var changes = new List<(long Id, int StakerId, SmartRollupPublishFlags? Change)>();

            foreach (var op in await Db.SmartRollupPublishOps.AsNoTracking()
                .Where(x => x.CommitmentId == commitment.Id && x.Flags != SmartRollupPublishFlags.None)
                .ToListAsync())
                changes.Add((op.Id, op.SenderId, op.Flags));

            foreach (var game in await Db.RefutationGames.AsNoTracking()
                .Where(x => x.InitiatorCommitmentId == commitment.Id && x.InitiatorLoss < 0)
                .ToListAsync())
                changes.Add((game.LastMoveId, game.InitiatorId, null));

            foreach (var game in await Db.RefutationGames.AsNoTracking()
                .Where(x => x.OpponentCommitmentId == commitment.Id && x.OpponentLoss < 0)
                .ToListAsync())
                changes.Add((game.LastMoveId, game.OpponentId, null));

            foreach (var (_, id, change) in changes.OrderBy(x => x.Id))
                stakers[id] = change != null ? 1 : 0;
                
            return stakers;
        }
    }
}
