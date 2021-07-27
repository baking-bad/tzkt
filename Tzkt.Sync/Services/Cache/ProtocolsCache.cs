using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

using Tzkt.Data;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Services.Cache
{
    public class ProtocolsCache
    {
        public const int MaxProtocols = 16; //TODO: set limits in app settings

        static readonly Dictionary<int, Protocol> CachedByCode = new(17);
        static readonly Dictionary<string, Protocol> CachedByHash = new(17);

        readonly TzktContext Db;

        public ProtocolsCache(TzktContext db)
        {
            Db = db;
        }

        public void Reset()
        {
            CachedByCode.Clear();
            CachedByHash.Clear();
        }

        public void Add(Protocol protocol)
        {
            CheckSpace();
            CachedByCode[protocol.Code] = protocol;
            CachedByHash[protocol.Hash] = protocol;
        }

        public async Task<Protocol> GetAsync(int code)
        {
            if (!CachedByCode.TryGetValue(code, out var protocol))
            {
                protocol = await Db.Protocols.FirstOrDefaultAsync(x => x.Code == code)
                    ?? throw new Exception($"Protocol #{code} doesn't exist");

                Add(protocol);
            }

            return protocol;
        }

        public async Task<Protocol> GetAsync(string hash)
        {
            if (!CachedByHash.TryGetValue(hash, out var protocol))
            {
                protocol = await Db.Protocols.FirstOrDefaultAsync(x => x.Hash == hash)
                    ?? throw new Exception($"Protocol {hash} doesn't exist");

                Add(protocol);
            }

            return protocol;
        }

        public async Task<Protocol> FindByCycleAsync(int cycle)
        {
            var protocol = CachedByCode.Values
                .OrderByDescending(x => x.Code)
                .FirstOrDefault(x => x.FirstCycle <= cycle);

            if (protocol == null)
            {
                protocol = await Db.Protocols
                    .OrderByDescending(x => x.Code)
                    .FirstOrDefaultAsync(x => x.FirstCycle <= cycle)
                        ?? throw new Exception($"Protocol for cycle {cycle} doesn't exist");

                Add(protocol);
            }

            return protocol;
        }

        public async Task<Protocol> FindByLevelAsync(int level)
        {
            var protocol = CachedByCode.Values
                .OrderByDescending(x => x.Code)
                .FirstOrDefault(x => x.FirstLevel <= level);

            if (protocol == null)
            {
                protocol = await Db.Protocols
                    .OrderByDescending(x => x.Code)
                    .FirstOrDefaultAsync(x => x.FirstLevel <= level)
                        ?? throw new Exception($"Protocol for level {level} doesn't exist");

                Add(protocol);
            }

            return protocol;
        }

        public void Remove(Protocol protocol)
        {
            CachedByCode.Remove(protocol.Code);
            CachedByHash.Remove(protocol.Hash);
        }

        void CheckSpace()
        {
            if (CachedByCode.Count >= MaxProtocols)
            {
                var oldest = CachedByCode.Values
                    .Take(MaxProtocols / 4);

                foreach (var code in oldest.Select(x => x.Code).ToList())
                    CachedByCode.Remove(code);

                foreach (var hash in oldest.Select(x => x.Hash).ToList())
                    CachedByHash.Remove(hash);
            }
        }
    }
}
