using Microsoft.EntityFrameworkCore;
using Tzkt.Data;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Services.Cache
{
    public class PeriodsCache(TzktContext db)
    {
        const int MaxCount = 16; //TODO: set limits in app settings
        const int TargetCount = MaxCount * 3 / 4;

        static readonly Dictionary<int, VotingPeriod> Cached = new(MaxCount);

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
                    .OrderBy(x => x.LastLevel)
                    .Take(Cached.Count - TargetCount)
                    .ToList();

                foreach (var item in toRemove)
                    Remove(item);
            }
        }

        public void Add(VotingPeriod period)
        {
            Cached[period.Index] = period;
        }

        public void Remove(VotingPeriod period)
        {
            Cached.Remove(period.Index);
        }

        public async Task<VotingPeriod> GetAsync(int index)
        {
            if (!Cached.TryGetValue(index, out var period))
            {
                period = await Db.VotingPeriods.FirstOrDefaultAsync(x => x.Index == index)
                    ?? throw new Exception($"Voting period #{index} not found");

                Add(period);
            }

            return period;
        }
    }
}
