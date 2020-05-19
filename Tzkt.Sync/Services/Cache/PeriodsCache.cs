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
        static VotingPeriod VotingPeriod = null;

        readonly TzktContext Db;

        public PeriodsCache(TzktContext db)
        {
            Db = db;
        }

        public void Reset()
        {
            VotingPeriod = null;
        }

        public void Add(VotingPeriod period)
        {
            VotingPeriod = period;
        }

        public async Task<VotingPeriod> CurrentAsync()
        {
            VotingPeriod ??= await Db.VotingPeriods.OrderByDescending(x => x.StartLevel).FirstOrDefaultAsync()
                ?? throw new Exception("Failed to get voting period");

            return VotingPeriod;
        }

        public void Remove()
        {
            VotingPeriod = null;
        }
    }
}
