using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Tzkt.Data;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Services.Cache
{
    public class BigMapKeysCache(TzktContext db)
    {
        #region static
        static int SoftCap = 0;
        static int TargetCap = 0;
        static Dictionary<(int, string), BigMapKey> Cached = [];

        public static void Configure(CacheSize? size)
        {
            SoftCap = size?.SoftCap ?? 120_000;
            TargetCap = size?.TargetCap ?? 100_000;
            Cached = new(SoftCap + 16_384);
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

        public void Add(BigMapKey key)
        {
            Cached[(key.BigMapPtr, key.KeyHash)] = key;
        }

        public void Add(IEnumerable<BigMapKey> keys)
        {
            foreach (var key in keys)
                Cached[(key.BigMapPtr, key.KeyHash)] = key;
        }

        public void Remove(BigMapKey key)
        {
            Cached.Remove((key.BigMapPtr, key.KeyHash));
        }

        public async Task Prefetch(IEnumerable<(int ptr, string hash)> keys)
        {
            var missed = keys.Where(x => !Cached.ContainsKey((x.ptr, x.hash))).ToList();
            if (missed.Count != 0)
            {
                for (int i = 0, n = 2048; i < missed.Count; i += n)
                {
                    var ptrHashes = string.Join(',', missed.Skip(i).Take(n).Select(x => $"({x.ptr}, '{x.hash}')")); // TODO: use parameters
#pragma warning disable EF1002 // Risk of vulnerability to SQL injection.
                    var loaded = await Db.BigMapKeys
                        .FromSqlRaw($"""
                            SELECT *
                            FROM "BigMapKeys"
                            WHERE ("BigMapPtr", "KeyHash") IN ({ptrHashes})
                            """)
                        .ToListAsync();
#pragma warning restore EF1002 // Risk of vulnerability to SQL injection.

                    foreach (var item in loaded)
                        Cached.Add((item.BigMapPtr, item.KeyHash), item);
                }
            }
        }

        public bool TryGet(int ptr, string hash, [NotNullWhen(true)] out BigMapKey? key)
        {
            return Cached.TryGetValue((ptr, hash), out key);
        }
    }
}
