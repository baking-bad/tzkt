using Microsoft.EntityFrameworkCore;
using Tzkt.Data;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Services.Cache
{
    public class BigMapsCache(TzktContext db)
    {
        #region static
        static int SoftCap = 0;
        static int TargetCap = 0;
        static Dictionary<int, BigMap> Cached = [];

        public static void Configure(CacheSize? size)
        {
            SoftCap = size?.SoftCap ?? 30_000;
            TargetCap = size?.TargetCap ?? 20_000;
            Cached = new(SoftCap + 1024);
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
                var toRemove = Cached.Values
                    .OrderBy(x => x.LastLevel)
                    .Take(Cached.Count - TargetCap)
                    .ToList();

                foreach (var item in toRemove)
                    Remove(item);
            }
        }

        public void Add(BigMap bigmap)
        {
            Cached[bigmap.Ptr] = bigmap;
        }

        public void Remove(BigMap bigmap)
        {
            Cached.Remove(bigmap.Ptr);
        }

        public async Task Prefetch(IEnumerable<int> ptrs)
        {
            var missed = ptrs.Where(x => !Cached.ContainsKey(x)).ToHashSet();
            if (missed.Count != 0)
            {
                var items = await Db.BigMaps
                    .Where(x => missed.Contains(x.Ptr))
                    .ToListAsync();

                foreach (var item in items)
                    Cached.Add(item.Ptr, item);
            }
        }

        public BigMap Get(int ptr)
        {
            if (!Cached.TryGetValue(ptr, out var bigMap))
                throw new Exception($"BigMap #{ptr} doesn't exist");

            return bigMap;
        }
    }
}
