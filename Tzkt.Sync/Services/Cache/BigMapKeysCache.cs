using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

using Tzkt.Data;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Services.Cache
{
    public class BigMapKeysCache
    {
        public const int MaxItems = 4 * 4096; //TODO: set limits in app settings

        static readonly Dictionary<(int, string), BigMapKey> Cached = new(MaxItems);

        readonly TzktContext Db;

        public BigMapKeysCache(TzktContext db)
        {
            Db = db;
        }

        public void Reset()
        {
            Cached.Clear();
        }

        public async Task Prefetch(IEnumerable<(int ptr, string hash)> keys)
        {
            var missed = keys.Where(x => !Cached.ContainsKey((x.ptr, x.hash))).ToList();
            if (missed.Count > 0)
            {
                #region check space
                if (Cached.Count + missed.Count > MaxItems)
                {
                    var pinned = keys.ToHashSet();
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

                for (int i = 0, n = 2048; i < missed.Count; i += n)
                {
                    var ptrHashes = string.Join(',', missed.Skip(i).Take(n).Select(x => $"({x.ptr}, '{x.hash}')")); // TODO: use parameters
                    var loaded = await Db.BigMapKeys
                        .FromSqlRaw($@"
                        SELECT * FROM ""{nameof(TzktContext.BigMapKeys)}""
                        WHERE (""{nameof(BigMapKey.BigMapPtr)}"", ""{nameof(BigMapKey.KeyHash)}"") IN ({ptrHashes})")
                        .ToListAsync();

                    foreach (var item in loaded)
                        Cached.TryAdd((item.BigMapPtr, item.KeyHash), item);
                }
            }
        }

        public bool TryGet(int ptr, string hash, out BigMapKey key)
        {
            return Cached.TryGetValue((ptr, hash), out key);
        }

        public void Cache(BigMapKey key)
        {
            Cached[(key.BigMapPtr, key.KeyHash)] = key;
        }

        public void Cache(IEnumerable<BigMapKey> keys)
        {
            foreach (var key in keys)
                Cached[(key.BigMapPtr, key.KeyHash)] = key;
        }

        public void Remove(BigMapKey key)
        {
            Cached.Remove((key.BigMapPtr, key.KeyHash));
        }
    }
}
