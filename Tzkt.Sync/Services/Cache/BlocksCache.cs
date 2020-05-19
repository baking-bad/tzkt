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
        public const int MaxBlocks = 3 * 4096; //TODO: set limits in app settings

        static readonly Dictionary<int, Block> CachedBlocks = new Dictionary<int, Block>(13331);

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

        public Task<Block> CurrentAsync()
        {
            return GetAsync(Cache.AppState.GetLevel());
        }

        public Task<Block> PreviousAsync()
        {
            return GetAsync(Cache.AppState.GetLevel() - 1);
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

        public void Remove(Block block)
        {
            CachedBlocks.Remove(block.Level);
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
