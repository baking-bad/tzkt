using Microsoft.EntityFrameworkCore;
using Tzkt.Data;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Services.Cache
{
    public class BlocksCache(CacheService cache, TzktContext db)
    {
        #region static
        static int SoftCap = 0;
        static int TargetCap = 0;
        static Dictionary<int, Block> Cached = [];

        public static void Configure(CacheSize? size)
        {
            SoftCap = size?.SoftCap ?? 64_000;
            TargetCap = size?.TargetCap ?? 32_000;
            Cached = new(SoftCap + 256);
        }
        #endregion

        readonly CacheService Cache = cache;
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
                    .OrderBy(x => x.Level)
                    .Take(Cached.Count - TargetCap)
                    .ToList();

                foreach (var item in toRemove)
                    Remove(item);
            }
        }

        public void Add(Block block)
        {
            Cached[block.Level] = block;
        }

        public void Remove(Block block)
        {
            Cached.Remove(block.Level);
        }

        public async Task Preload(IEnumerable<int> levels)
        {
            var missed = levels.Where(x => !Cached.ContainsKey(x)).ToHashSet();
            if (missed.Count != 0)
            {
                var items = await Db.Blocks
                    .Where(x => missed.Contains(x.Level))
                    .ToListAsync();

                foreach (var item in items)
                    Add(item);
            }
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
            if (!Cached.TryGetValue(level, out var block))
            {
                block = Db.Blocks.FirstOrDefault(x => x.Level == level)
                    ?? throw new Exception($"Block #{level} doesn't exist");

                Add(block);
            }

            return block;
        }

        public async Task<Block> GetAsync(int level)
        {
            if (!Cached.TryGetValue(level, out var block))
            {
                block = await Db.Blocks.FirstOrDefaultAsync(x => x.Level == level)
                    ?? throw new Exception($"Block #{level} doesn't exist");

                Add(block);
            }

            return block;
        }

        public Block GetCached(int level)
        {
            if (!Cached.TryGetValue(level, out var block))
                throw new Exception($"Block #{level} is not cached");

            return block;
        }
    }
}
