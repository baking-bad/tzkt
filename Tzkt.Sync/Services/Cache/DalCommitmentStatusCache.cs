using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

using Tzkt.Data;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Services.Cache
{
    public class DalCommitmentStatusCache
    {
        static int CachedLevel = -1;
        static Dictionary<int, DalCommitmentStatus> CachedStatus = null;

        readonly TzktContext Db;

        public DalCommitmentStatusCache(TzktContext db)
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
                var status = await Db.DalCommitmentStatus
                    .AsNoTracking()
                    .Join(Db.DalPublishCommitmentOps, x => x.PublishmentId, x => x.Id,
                        (commitment, publishment) => new { commitment, publishment })
                    .Where(x => x.publishment.Level == level)
                    .ToListAsync();

                CachedStatus = status.ToDictionary(x => x.publishment.Slot, x => x.commitment);
                CachedLevel = level;
            }
        }
        
        public async Task<DalCommitmentStatus> GetOrDefaultAsync(int level, int slot)
        {
            await LoadCachedStatusAsync(level);
            return CachedStatus.TryGetValue(slot, out var res) ? res : null;
        }
        
        public async Task<List<DalCommitmentStatus>> GetOrDefaultAsync(int level)
        {
            await LoadCachedStatusAsync(level);
            return CachedStatus.Values.ToList();
        }
    }
}
