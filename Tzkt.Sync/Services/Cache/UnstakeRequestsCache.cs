using Microsoft.EntityFrameworkCore;
using Tzkt.Data;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Services.Cache
{
    public class UnstakeRequestsCache
    {
        public const int SoftCap = 4096; //TODO: set limits in app settings

        static readonly Dictionary<(int, int?, int), UnstakeRequest> Cached = new((int)(SoftCap * 1.1));

        readonly TzktContext Db;

        public UnstakeRequestsCache(TzktContext db)
        {
            Db = db;
        }

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
                    .Take(SoftCap / 2)
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

        public async Task<UnstakeRequest> GetOrDefaultAsync(int bakerId, int? stakerId, int cycle)
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
