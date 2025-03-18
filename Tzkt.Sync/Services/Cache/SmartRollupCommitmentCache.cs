using Microsoft.EntityFrameworkCore;
using Tzkt.Data;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Services.Cache
{
    public class SmartRollupCommitmentCache(TzktContext db)
    {
        #region static
        static int SoftCap = 0;
        static int TargetCap = 0;
        static Dictionary<int, SmartRollupCommitment> CachedById = [];
        static Dictionary<(string, int), SmartRollupCommitment> CachedByKey = [];

        public static void Configure(CacheSize? size)
        {
            SoftCap = size?.SoftCap ?? 10_000;
            TargetCap = size?.TargetCap ?? 5000;
            CachedById = new(SoftCap + 512);
            CachedByKey = new(SoftCap + 512);
        }
        #endregion

        readonly TzktContext Db = db;

        public void Reset()
        {
            CachedById.Clear();
            CachedByKey.Clear();
        }

        public void Trim()
        {
            if (CachedByKey.Count > SoftCap)
            {
                var toRemove = CachedByKey.Values
                    .OrderBy(x => x.LastLevel)
                    .Take(CachedByKey.Count - TargetCap)
                    .ToList();

                foreach (var item in toRemove)
                    Remove(item);
            }
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

        public async Task<SmartRollupCommitment> GetAsync(string hash, int rollupId)
        {
            if (!CachedByKey.TryGetValue((hash, rollupId), out var item))
            {
                item = await Db.SmartRollupCommitments
                    .Where(x => x.Hash == hash && x.SmartRollupId == rollupId)
                    .OrderByDescending(x => x.Id)
                    .FirstOrDefaultAsync()
                    ?? throw new Exception($"Smart rollup commitment ({hash}, {rollupId}) doesn't exist");
                Add(item);
            }
            return item;
        }

        public async Task<SmartRollupCommitment?> GetOrDefaultAsync(int? id)
        {
            if (id is not int _id)
                return null;

            if (!CachedById.TryGetValue(_id, out var item))
            {
                item = await Db.SmartRollupCommitments.SingleOrDefaultAsync(x => x.Id == _id)
                    ?? throw new Exception($"Smart rollup commitment #{_id} doesn't exist");
                Add(item);
            }
            return item;
        }

        public async Task<SmartRollupCommitment?> GetOrDefaultAsync(string? hash, int? rollupId)
        {
            if (hash is not string _hash || rollupId is not int _rollupId)
                return null;

            if (!CachedByKey.TryGetValue((_hash, _rollupId), out var item))
            {
                item = await Db.SmartRollupCommitments
                    .Where(x => x.Hash == _hash && x.SmartRollupId == _rollupId)
                    .OrderByDescending(x => x.Id)
                    .FirstOrDefaultAsync();
                if (item != null) Add(item);
            }
            
            return item;
        }
    }
}
