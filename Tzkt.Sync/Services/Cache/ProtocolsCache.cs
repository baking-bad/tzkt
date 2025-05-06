using Microsoft.EntityFrameworkCore;
using Tzkt.Data;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Services.Cache
{
    public class ProtocolsCache(TzktContext db)
    {
        #region static
        static readonly Dictionary<int, Protocol> CachedByCode = new(37);
        static readonly Dictionary<string, Protocol> CachedByHash = new(37);
        #endregion

        readonly TzktContext Db = db;

        public async Task ResetAsync()
        {
            CachedByCode.Clear();
            CachedByHash.Clear();

            foreach (var protocol in await Db.Protocols.ToListAsync())
                Add(protocol);
        }

        public void Add(Protocol protocol)
        {
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

        public Protocol FindByCycle(int cycle)
        {
            return CachedByCode.Values
                .OrderByDescending(x => x.Code)
                .FirstOrDefault(x => x.FirstCycle <= cycle)
                    ?? throw new Exception($"Protocol for cycle {cycle} doesn't exist");
        }

        public int GetCycleStart(int cycle)
        {
            return FindByCycle(cycle).GetCycleStart(cycle);
        }

        public int GetCycleEnd(int cycle)
        {
            return FindByCycle(cycle).GetCycleEnd(cycle);
        }
    }
}
