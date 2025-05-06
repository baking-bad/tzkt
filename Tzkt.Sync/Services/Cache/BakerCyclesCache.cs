using Microsoft.EntityFrameworkCore;
using Tzkt.Data;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Services.Cache
{
    public class BakerCyclesCache(TzktContext db)
    {
        #region static
        static int CachedCycle = -1;
        static Dictionary<int, BakerCycle> Cached = [];
        #endregion

        readonly TzktContext Db = db;

        public void Reset()
        {
            CachedCycle = -1;
            Cached = [];
        }

        public void Add(BakerCycle bc)
        {
            if (CachedCycle != bc.Cycle)
                throw new InvalidOperationException();

            Cached[bc.BakerId] = bc;
        }

        async Task EnsureCachedCycle(int cycle)
        {
            if (CachedCycle != cycle)
            {
                var bakerCycles = await Db.BakerCycles
                    .Where(x => x.Cycle == cycle)
                    .ToListAsync();

                Cached = bakerCycles.ToDictionary(x => x.BakerId);
                CachedCycle = cycle;
            }
        }

        public async Task<Dictionary<int, BakerCycle>> GetAsync(int cycle)
        {
            await EnsureCachedCycle(cycle);
            return Cached;
        }

        public async Task<BakerCycle> GetAsync(int cycle, int bakerId)
        {
            await EnsureCachedCycle(cycle);
            return Cached[bakerId];
        }

        public async Task<BakerCycle?> GetOrDefaultAsync(int cycle, int bakerId)
        {
            await EnsureCachedCycle(cycle);
            return Cached.TryGetValue(bakerId, out var res) ? res : null;
        }
    }
}
