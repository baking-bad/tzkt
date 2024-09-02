using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

using Tzkt.Data;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Services.Cache
{
    public class DalAttestationsCache
    {
        static int CachedLevel = -1;
        static List<DalAttestationStatus> CachedStatus = new();

        readonly TzktContext Db;

        public DalAttestationsCache(TzktContext db)
        {
            Db = db;
        }

        public static void Reset()
        {
            CachedLevel = -1;
            CachedStatus.Clear();
        }

        public static void Add(int level, IEnumerable<DalAttestationStatus> entry)
        {
            if (CachedLevel != level)
            {
                Reset();
                CachedLevel = level;
            }
            CachedStatus.AddRange(entry);
        }
        
        public List<DalAttestationStatus> GetCached(int level)
        {
            if (CachedLevel == level && CachedStatus is not null)
                return CachedStatus;
            return new List<DalAttestationStatus>();
        }
   }
}
