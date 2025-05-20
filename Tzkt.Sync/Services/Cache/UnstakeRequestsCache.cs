using Microsoft.EntityFrameworkCore;
using Tzkt.Data;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Services.Cache
{
    public class UnstakeRequestsCache(TzktContext db)
    {
        #region static
        static int SoftCap = 0;
        static int TargetCap = 0;
        static Dictionary<(int, int?, int), UnstakeRequest> Cached = [];

        public static void Configure(CacheSize? size)
        {
            SoftCap = size?.SoftCap ?? 4000;
            TargetCap = size?.TargetCap ?? 3000;
            Cached = new(SoftCap + 256);
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

        public void Add(UnstakeRequest item)
        {
            Cached[(item.BakerId, item.StakerId, item.Cycle)] = item;
        }

        public void Remove(UnstakeRequest item)
        {
            Cached.Remove((item.BakerId, item.StakerId, item.Cycle));
        }

        public async Task<UnstakeRequest?> GetOrDefaultAsync(int bakerId, int? stakerId, int cycle)
        {
            if (!Cached.TryGetValue((bakerId, stakerId, cycle), out var item))
            {
                item = await Db.UnstakeRequests
                    .FirstOrDefaultAsync(x => x.BakerId == bakerId && x.StakerId == stakerId && x.Cycle == cycle);

                if (item != null)
                    Add(item);
            }
            return item;
        }

        public async Task<UnstakeRequest> GetAsync(int bakerId, int? stakerId, int cycle)
        {
            if (!Cached.TryGetValue((bakerId, stakerId, cycle), out var item))
            {
                item = await Db.UnstakeRequests
                    .FirstOrDefaultAsync(x => x.BakerId == bakerId && x.StakerId == stakerId && x.Cycle == cycle)
                        ?? throw new Exception($"UnstakeRequest #({bakerId}, {stakerId}, {cycle}) doesn't exist");

                Add(item);
            }
            return item;
        }
    }
}
