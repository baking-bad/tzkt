using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

using Tzkt.Data;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Services
{
    public class ProtocolsCache
    {
        #region cache
        const int MaxSize = 16;
        static readonly Dictionary<string, Protocol> Protocols = new Dictionary<string, Protocol>(MaxSize);
        #endregion

        readonly TzktContext Db;

        public ProtocolsCache(TzktContext db)
        {
            Db = db;
        }

        public async Task<Protocol> GetProtocolAsync(string hash)
        {
            if (!Protocols.ContainsKey(hash))
            {
                if (Protocols.Count >= MaxSize)
                    foreach (var key in Protocols.Keys.Take(MaxSize / 4).ToList())
                        Protocols.Remove(key);

                Protocols[hash] = await Db.Protocols.FirstOrDefaultAsync(x => x.Hash == hash)
                    ?? new Protocol { Hash = hash, Code = await Db.Protocols.CountAsync() - 1 };
            }

            return Protocols[hash];
        }

        public void ProtocolUp(Protocol protocol)
        {
            protocol.Weight++;

            if (Db.Entry(protocol).State != EntityState.Added)
                Db.Update(protocol);
        }

        public void ProtocolDown(Protocol protocol)
        {
            if (--protocol.Weight == 0)
            {
                Db.Protocols.Remove(protocol);
                Protocols.Remove(protocol.Hash);
            }
            else
            {
                Db.Update(protocol);
            }
        }

        public void Clear() => Protocols.Clear();
    }
}
