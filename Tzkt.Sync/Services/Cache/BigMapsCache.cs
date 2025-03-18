using Microsoft.EntityFrameworkCore;
using Tzkt.Data;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Services.Cache
{
    public class BigMapsCache(TzktContext db)
    {
        const int MaxCount = 65713; //TODO: set limits in app settings
        const int TargetCount = MaxCount * 3 / 4;

        static readonly Dictionary<int, BigMap> Cached = new(MaxCount);

        readonly TzktContext Db = db;

        public void Reset()
        {
            Cached.Clear();
        }

        public void Trim()
        {
            if (Cached.Count > MaxCount)
            {
                var toRemove = Cached.Values
                    .OrderBy(x => x.LastLevel)
                    .Take(Cached.Count - TargetCount)
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
