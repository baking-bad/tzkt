using Microsoft.EntityFrameworkCore;
using Tzkt.Data;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Services.Cache
{
    public class SmartRollupCommitmentCache
    {
        public const int MaxItems = 4096; //TODO: set limits in app settings

        static readonly Dictionary<int, SmartRollupCommitment> CachedById = new(4097);
        static readonly Dictionary<(string, int), SmartRollupCommitment> CachedByKey = new(4097);

        readonly TzktContext Db;

        public SmartRollupCommitmentCache(TzktContext db)
        {
            Db = db;
        }

        public void Add(SmartRollupCommitment item)
        {
            CachedById[item.Id] = item;
            CachedByKey[(item.Hash, item.SmartRollupId)] = item;
        }

        public void Remove(SmartRollupCommitment item)
        {
            CachedById.Remove(item.Id);
            CachedByKey.Remove((item.Hash, item.SmartRollupId));
        }

        public void Trim()
        {
            if (CachedByKey.Count > MaxItems)
            {
                var toRemove = CachedByKey.Values
                    .OrderBy(x => x.LastLevel)
                    .Take(MaxItems / 2)
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

        public async Task<SmartRollupCommitment> GetAsync(int id)
        {
            if (!CachedById.TryGetValue(id, out var item))
            {
                item = await Db.SmartRollupCommitments.SingleOrDefaultAsync(x => x.Id == id)
                    ?? throw new Exception($"Smart rollup commitment #{id} doesn't exist");
                Add(item);
            }

            return item;
        }

        public async Task<SmartRollupCommitment> GetAsync(string hash, int smartRollupId)
        {
            if (!CachedByKey.TryGetValue((hash, smartRollupId), out var item))
            {
                // throw if there are more than one, because not sure how to choose then
                item = await Db.SmartRollupCommitments.SingleOrDefaultAsync(x => x.Hash == hash && x.SmartRollupId == smartRollupId)
                    ?? throw new Exception($"Smart rollup commitment ({hash}, {smartRollupId}) doesn't exist");
                Add(item);
            }

            return item;
        }

        public async Task<SmartRollupCommitment> GetOrDefaultAsync(string hash, int smartRollupId)
        {
            if (!CachedByKey.TryGetValue((hash, smartRollupId), out var item))
            {
                // throw if there are more than one, because not sure how to choose then
                item = await Db.SmartRollupCommitments.SingleOrDefaultAsync(x => x.Hash == hash && x.SmartRollupId == smartRollupId);
                if (item != null) Add(item);
            }

            return item;
        }
    }
}
