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
        private int CachedLevel = -1;
        private Dictionary<(int, Data.Models.Delegate), DalAttestation> CachedStatus = new();

        private readonly TzktContext Db;

        public DalAttestationsCache(TzktContext db)
        {
            Db = db;
        }

        public void Reset()
        {
            CachedLevel = -1;
            CachedStatus.Clear();
        }

        public void Add(int level, int slot, Data.Models.Delegate @delegate, DalAttestation attestation)
        {
            if (CachedLevel != level)
            {
                Reset();
                CachedLevel = level;
            }
            CachedStatus.Add((slot, @delegate), attestation);
        }

        public DalAttestation GetOrDefault(int level, int slot, Data.Models.Delegate @delegate)
        {
            if (CachedLevel != level)
                return null;
            return CachedStatus.TryGetValue((slot, @delegate), out var res) ? res : null;
        }
    }
}
