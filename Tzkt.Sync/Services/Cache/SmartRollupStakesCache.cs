using Microsoft.EntityFrameworkCore;
using Tzkt.Data;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Services.Cache
{
    public class SmartRollupStakesCache
    {
        public const int MaxItems = 4096; //TODO: set limits in app settings

        static readonly Dictionary<int, Dictionary<int, int>> Cache = new(4097);

        readonly TzktContext Db;

        public SmartRollupStakesCache(TzktContext db)
        {
            Db = db;
        }

        public void Add(SmartRollupCommitment commitment)
        {
            Cache.Add(commitment.Id, new() { { commitment.InitiatorId, 1 } });
        }

        public void Remove(int commitmentId)
        {
            Cache.Remove(commitmentId);
        }

        public void Remove(int commitmentId, int stakerId)
        {
            if (Cache.TryGetValue(commitmentId, out var stakers))
                stakers.Remove(stakerId);
        }

        public void Trim()
        {
            if (Cache.Count > MaxItems)
            {
                var toRemove = Cache.Keys
                    .OrderBy(x => x)
                    .Take(Cache.Count - (int)(MaxItems * 0.75))
                    .ToList();

                foreach (var item in toRemove)
                    Remove(item);
            }
        }

        public void Reset()
        {
            Cache.Clear();
        }

        public async Task<int?> GetAsync(SmartRollupCommitment commitment, int stakerId)
        {
            if (!Cache.TryGetValue(commitment.Id, out var stakers))
            {
                stakers = await GetStakers(commitment);
                Cache.Add(commitment.Id, stakers);
            }
            return stakers.TryGetValue(stakerId, out var stake) ? stake : null;
        }

        public async Task SetAsync(SmartRollupCommitment commitment, int stakerId, int stake)
        {
            if (!Cache.TryGetValue(commitment.Id, out var stakers))
            {
                stakers = await GetStakers(commitment);
                Cache.Add(commitment.Id, stakers);
            }
            stakers[stakerId] = stake;
        }

        public void Set(int commitmentId, int stakerId, int stake)
        {
            if (Cache.TryGetValue(commitmentId, out var stakers))
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
