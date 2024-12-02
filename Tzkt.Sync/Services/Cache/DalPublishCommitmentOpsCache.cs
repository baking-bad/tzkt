using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Tzkt.Data;
using Tzkt.Data.Models;
using Tzkt.Data.Models.Base;

namespace Tzkt.Sync.Services.Cache
{
    public class DalPublishCommitmentOpsCache 
    {
        static int CachedLevel = -1;
        static Dictionary<int, DalPublishCommitmentOperation> CachedStatus = null;

        readonly TzktContext Db;

        public DalPublishCommitmentOpsCache(TzktContext db)
        {
            Db = db;
        }

        public void Reset()
        {
            CachedLevel = -1;
            CachedStatus = null;
        }

        private async Task LoadCachedStatusAsync(int level)
        {
            if (CachedLevel != level)
            {
                var status = await Db.DalPublishCommitmentOps
                    .AsNoTracking()
                    .Where(x => x.Level == level && x.Status == OperationStatus.Applied)
                    .ToListAsync();

                CachedStatus = status.ToDictionary(x => x.Slot,  x => x);
                CachedLevel = level;
            }
        }

        public async Task<DalPublishCommitmentOperation> GetOrDefaultAsync(int level, int slot)
        {
            await LoadCachedStatusAsync(level);
            return CachedStatus.GetValueOrDefault(slot, null);
        }

    }
}