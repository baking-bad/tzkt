using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

using Tzkt.Data;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Services.Cache
{
    public class BlocksCache
    {
        public const int MaxBlocks = 3 * 8192; //TODO: set limits in app settings

        static readonly Dictionary<int, Block> CachedBlocks = new(MaxBlocks);

        readonly CacheService Cache;
        readonly TzktContext Db;

        public BlocksCache(CacheService cache, TzktContext db)
        {
            Cache = cache;
            Db = db;
        }

        public void Reset()
        {
            CachedBlocks.Clear();
        }

        public void Add(Block block)
        {
            CheckSpace();
            CachedBlocks[block.Level] = block;
        }

        public Block Current()
        {
            return Get(Cache.AppState.GetLevel());
        }

        public Task<Block> CurrentAsync()
        {
            return GetAsync(Cache.AppState.GetLevel());
        }

        public Task<Block> PreviousAsync()
        {
            return GetAsync(Cache.AppState.GetLevel() - 1);
        }

        public Block Get(int level)
        {
            if (!CachedBlocks.TryGetValue(level, out var block))
            {
                block = Db.Blocks.FirstOrDefault(x => x.Level == level)
                    ?? throw new Exception($"Block #{level} doesn't exist");

                Add(block);
            }

            return block;
        }

        public async Task<Block> GetAsync(int level)
        {
            if (!CachedBlocks.TryGetValue(level, out var block))
            {
                block = await Db.Blocks.FirstOrDefaultAsync(x => x.Level == level)
                    ?? throw new Exception($"Block #{level} doesn't exist");

                Add(block);
            }

            return block;
        }

        public Block GetCached(int level)
        {
            if (!CachedBlocks.TryGetValue(level, out var block))
                throw new Exception($"Block #{level} is not cached");
            return block;
        }

        public void Remove(Block block)
        {
            CachedBlocks.Remove(block.Level);
        }

        public async Task Preload(IEnumerable<int> levels)
        {
            var missed = levels.Where(x => !CachedBlocks.ContainsKey(x)).ToHashSet();
            if (missed.Count > 0)
            {
                var items = await Db.Blocks
                    .Where(x => missed.Contains(x.Level))
                    .ToListAsync();

                foreach (var item in items)
                    Add(item);
            }
        }

        void CheckSpace()
        {
            if (CachedBlocks.Count >= MaxBlocks)
            {
                var oldest = CachedBlocks.Keys
                    .OrderBy(x => x)
                    .TakeLast(MaxBlocks / 3)
                    .ToList();

                foreach (var level in oldest)
                    CachedBlocks.Remove(level);
            }
        }
    }
}
