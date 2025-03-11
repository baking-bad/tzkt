using Microsoft.EntityFrameworkCore;
using Tzkt.Data;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Services.Cache
{
    public class RefutationGameCache(TzktContext db)
    {
        const int MaxItems = 257; //TODO: set limits in app settings
        static readonly Dictionary<int, RefutationGame> CachedById = new(257);
        static readonly Dictionary<(int, int, int), RefutationGame> CachedByKey = new(257);

        readonly TzktContext Db = db;

        public void Add(RefutationGame item)
        {
            var (aliceId, bobId) = Order(item.InitiatorId, item.OpponentId);
            CachedById[item.Id] = item;
            CachedByKey[(item.SmartRollupId, aliceId, bobId)] = item;
        }

        public void Remove(RefutationGame item)
        {
            var (aliceId, bobId) = Order(item.InitiatorId, item.OpponentId);
            CachedById.Remove(item.Id);
            CachedByKey.Remove((item.SmartRollupId, aliceId, bobId));
        }

        public void Trim()
        {
            if (CachedByKey.Count > MaxItems)
            {
                var toRemove = CachedByKey.Values
                    .OrderBy(x => x.LastLevel)
                    .Take(CachedByKey.Count - (int)(MaxItems * 0.75))
                    .ToList();

                foreach (var item in toRemove)
                    Remove(item);
            }
        }

        public void Reset()
        {
            CachedById.Clear();
            CachedByKey.Clear();
        }

        public async Task<RefutationGame> GetAsync(int id)
        {
            if (!CachedById.TryGetValue(id, out var item))
            {
                item = await Db.RefutationGames.SingleOrDefaultAsync(x => x.Id == id)
                    ?? throw new Exception($"Refutation game #{id} doesn't exist");
                Add(item);
            }
            return item;
        }

        public async Task<RefutationGame> GetAsync(int rollupId, int initiatorId, int opponentId)
        {
            var (aliceId, bobId) = Order(initiatorId, opponentId);
            if (!CachedByKey.TryGetValue((rollupId, aliceId, bobId), out var item))
            {
                item = await Db.RefutationGames
                    .Where(x =>
                        x.SmartRollupId == rollupId &&
                        (x.InitiatorId == initiatorId && x.OpponentId == opponentId || x.InitiatorId == opponentId && x.OpponentId == initiatorId))
                    .OrderByDescending(x => x.Id)
                    .FirstOrDefaultAsync()
                    ?? throw new Exception($"Refutation game ({rollupId}, {initiatorId}, {opponentId}) doesn't exist");
                Add(item);
            }
            return item;
        }

        public async Task<RefutationGame?> GetOrDefaultAsync(int? rollupId, int? initiatorId, int? opponentId)
        {
            if (rollupId is not int _rollupId ||
                initiatorId is not int _initiatorId ||
                opponentId is not int _opponentId)
                return null;

            var (aliceId, bobId) = Order(_initiatorId, _opponentId);
            if (!CachedByKey.TryGetValue((_rollupId, aliceId, bobId), out var item))
            {
                item = await Db.RefutationGames
                    .Where(x =>
                        x.SmartRollupId == _rollupId &&
                        (x.InitiatorId == _initiatorId && x.OpponentId == _opponentId || x.InitiatorId == _opponentId && x.OpponentId == _initiatorId))
                    .OrderByDescending(x => x.Id)
                    .FirstOrDefaultAsync();
                if (item != null) Add(item);
            }
            return item;
        }

        static (int, int) Order(int id1, int id2) => id1 < id2 ? (id1, id2) : (id2, id1);
    }
}
