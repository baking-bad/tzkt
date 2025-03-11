using Microsoft.EntityFrameworkCore;
using Tzkt.Data;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Services.Cache
{
    public class BakerCyclesCache(TzktContext db)
    {
        static int CachedCycle = -1;
        static Dictionary<int, BakerCycle> CachedBakerCycles = [];

        readonly TzktContext Db = db;

        public void Reset()
        {
            CachedCycle = -1;
            CachedBakerCycles = [];
        }

        public async Task<bool> ExistsAsync(int cycle, int bakerId)
        {
            if (CachedCycle != cycle)
            {
                var bakerCycles = await Db.BakerCycles
                    .Where(x => x.Cycle == cycle)
                    .ToListAsync();

                CachedBakerCycles = bakerCycles.ToDictionary(x => x.BakerId);
                CachedCycle = cycle;
            }

            return CachedBakerCycles.ContainsKey(bakerId);
        }

        public async Task<Dictionary<int, BakerCycle>> GetAsync(int cycle)
        {
            if (CachedCycle != cycle)
            {
                var bakerCycles = await Db.BakerCycles
                    .Where(x => x.Cycle == cycle)
                    .ToListAsync();

                CachedBakerCycles = bakerCycles.ToDictionary(x => x.BakerId);
                CachedCycle = cycle;
            }

            return CachedBakerCycles;
        }

        public async Task<BakerCycle> GetAsync(int cycle, int bakerId)
        {
            if (CachedCycle != cycle)
            {
                var bakerCycles = await Db.BakerCycles
                    .Where(x => x.Cycle == cycle)
                    .ToListAsync();

                CachedBakerCycles = bakerCycles.ToDictionary(x => x.BakerId);
                CachedCycle = cycle;
            }

            return CachedBakerCycles[bakerId];
        }

        public async Task<BakerCycle?> GetOrDefaultAsync(int cycle, int bakerId)
        {
            if (CachedCycle != cycle)
            {
                var bakerCycles = await Db.BakerCycles
                    .Where(x => x.Cycle == cycle)
                    .ToListAsync();

                CachedBakerCycles = bakerCycles.ToDictionary(x => x.BakerId);
                CachedCycle = cycle;
            }

            return CachedBakerCycles.TryGetValue(bakerId, out var res) ? res : null;
        }

        public void Add(BakerCycle bc)
        {
            if (CachedCycle != bc.Cycle)
                throw new InvalidOperationException();

            CachedBakerCycles[bc.BakerId] = bc;
        }
    }
}
