using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

using Tzkt.Data;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Services.Cache
{
    public class BigMapsCache
    {
        public const int MaxItems = 65713; //TODO: set limits in app settings

        static readonly Dictionary<int, BigMap> Cached = new(MaxItems);

        readonly TzktContext Db;

        public BigMapsCache(TzktContext db)
        {
            Db = db;
        }

        public void Reset()
        {
            Cached.Clear();
        }

        public async Task Prefetch(IEnumerable<int> ptrs)
        {
            var missed = ptrs.Where(x => !Cached.ContainsKey(x)).ToHashSet();
            if (missed.Count > 0)
            {
                #region check space
                if (Cached.Count + missed.Count > MaxItems)
                {
                    var pinned = ptrs.ToHashSet();
                    var toRemove = Cached
                        .Where(kv => !pinned.Contains(kv.Key))
                        .OrderBy(x => x.Value.LastLevel)
                        .Select(x => x.Key)
                        .Take(Math.Max(MaxItems / 4, Cached.Count - MaxItems * 3 / 4))
                        .ToList();

                    foreach (var key in toRemove)
                        Cached.Remove(key);
                }
                #endregion

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

        public void Cache(BigMap bigmap)
        {
            Cached[bigmap.Ptr] = bigmap;
        }

        public void Remove(BigMap bigmap)
        {
            Cached.Remove(bigmap.Ptr);
        }
    }
}
