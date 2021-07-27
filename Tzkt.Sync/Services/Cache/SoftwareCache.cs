using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

using Tzkt.Data;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Services.Cache
{
    public class SoftwareCache
    {
        public const int MaxItems = 32; //TODO: set limits in app settings

        static readonly Dictionary<int, Software> CachedById = new(37);
        static readonly Dictionary<string, Software> CachedByHash = new(37);

        readonly TzktContext Db;

        public SoftwareCache(TzktContext db)
        {
            Db = db;
        }

        public void Reset()
        {
            CachedById.Clear();
            CachedByHash.Clear();
        }

        public void Add(Software item)
        {
            CheckSpace();
            CachedById[item.Id] = item;
            CachedByHash[item.ShortHash] = item;
        }

        public async Task<Software> GetAsync(int? id)
        {
            if (id == null) return null;

            if (!CachedById.TryGetValue((int)id, out var item))
            {
                item = await Db.Software.FirstOrDefaultAsync(x => x.Id == id)
                    ?? throw new Exception($"Software #{id} doesn't exist");

                Add(item);
            }

            return item;
        }

        public async Task<Software> GetOrCreateAsync(string hash, Func<Software> create)
        {
            if (!CachedByHash.TryGetValue(hash, out var item))
            {
                item = await Db.Software.FirstOrDefaultAsync(x => x.ShortHash == hash)
                    ?? create();

                // just created software has id = 0 so CachedById will contain wrong key, but it's fine
                Add(item);
            }

            return item;
        }

        public void Remove(Software item)
        {
            CachedById.Remove(item.Id);
            CachedByHash.Remove(item.ShortHash);
        }

        void CheckSpace()
        {
            if (CachedByHash.Count >= MaxItems)
            {
                var oldest = CachedByHash.Values
                    .Take(MaxItems / 4);

                foreach (var key in oldest.Select(x => x.Id).ToList())
                    CachedById.Remove(key);

                foreach (var key in oldest.Select(x => x.ShortHash).ToList())
                    CachedByHash.Remove(key);
            }
        }
    }
}
