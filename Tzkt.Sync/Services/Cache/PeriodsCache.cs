using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

using Tzkt.Data;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Services.Cache
{
    public class PeriodsCache
    {
        public const int MaxPeriods = 16; //TODO: set limits in app settings

        static readonly Dictionary<int, VotingPeriod> Cached = new(17);

        readonly TzktContext Db;

        public PeriodsCache(TzktContext db)
        {
            Db = db;
        }

        public void Reset()
        {
            Cached.Clear();
        }

        public void Add(VotingPeriod period)
        {
            CheckSpace();
            Cached[period.Index] = period;
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

        public void Remove(VotingPeriod period)
        {
            Cached.Remove(period.Index);
        }

        void CheckSpace()
        {
            if (Cached.Count >= MaxPeriods)
            {
                var oldest = Cached.Values
                    .Take(MaxPeriods / 4);

                foreach (var index in oldest.Select(x => x.Index).ToList())
                    Cached.Remove(index);
            }
        }
    }
}
