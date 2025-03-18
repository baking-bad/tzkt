using Microsoft.EntityFrameworkCore;
using Tzkt.Data;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Services.Cache
{
    public class BlocksCache(CacheService cache, TzktContext db)
    {
        const int MaxCount = 3 * 8192; //TODO: set limits in app settings
        const int TargetCount = MaxCount * 3 / 4;

        static readonly Dictionary<int, Block> Cached = new(MaxCount);

        readonly CacheService Cache = cache;
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
                    .OrderBy(x => x.Level)
                    .Take(Cached.Count - TargetCount)
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
