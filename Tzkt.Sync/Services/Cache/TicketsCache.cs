using Microsoft.EntityFrameworkCore;
using Tzkt.Data;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Services.Cache
{
    public class TicketsCache
    {
        public const int MaxItems = 4 * 4096; //TODO: set limits in app settings

        static readonly Dictionary<long, Ticket> CachedById = new(MaxItems);
        static readonly Dictionary<(int ContractId, HashableBytes RawContent, HashableBytes RawType), Ticket> CachedByKey = new(MaxItems);

        readonly TzktContext Db;

        public TicketsCache(TzktContext db)
        {
            Db = db;
        }

        public void Reset()
        {
            CachedById.Clear();
            CachedByKey.Clear();
        }

        public void Trim()
        {
            if (CachedById.Count > MaxItems * 0.9)
            {
                var toRemove = CachedById.Values
                    .OrderBy(x => x.LastLevel)
                    .Take(MaxItems / 2)
                    .ToList();

                foreach (var item in toRemove)
                    Remove(item);
            }
        }

        public void Add(Ticket ticket)
        {
            CachedById[ticket.Id] = ticket;
            CachedByKey[(ticket.TicketerId, ticket.RawContent, ticket.RawType)] = ticket;
        }

        public void Remove(Ticket ticket)
        {
            CachedById.Remove(ticket.Id);
            CachedByKey.Remove((ticket.TicketerId, ticket.RawContent, ticket.RawType));
        }

        public Ticket GetCached(long id)
        {
            if (!CachedById.TryGetValue(id, out var token))
                throw new Exception($"Ticket #{id} doesn't exist in the cache");
            return token;
        }

        public bool TryGetCached(int contractId, byte[] rawContent, byte[] rawType, out Ticket token)
        {
            return CachedByKey.TryGetValue((contractId, rawContent, rawType), out token);
        }

        public async Task Preload(IEnumerable<long> ids)
        {
            var missed = ids.Where(x => !CachedById.ContainsKey(x)).ToHashSet();
            if (missed.Count > 0)
            {
                var items = await Db.Tickets
                    .Where(x => missed.Contains(x.Id))
                    .ToListAsync();

                foreach (var item in items)
                    Add(item);
            }
        }

        public async Task Preload(IEnumerable<(int, byte[], int, byte[], int)> keys)
        {
            var missed = keys.Where(x => !CachedByKey.ContainsKey((x.Item1, x.Item2, x.Item4))).ToHashSet();
            if (missed.Count > 0)
            {
                for (int i = 0, n = 2048; i < missed.Count; i += n)
                {
                    var corteges = string.Join(',', missed.Skip(i).Take(n).Select(x => $"({x.Item1}, '{x.Item3}', '{x.Item5}')"));
                    var items = await Db.Tickets
                        .FromSqlRaw($@"
                            SELECT * FROM ""{nameof(TzktContext.Tickets)}""
                            WHERE (""{nameof(Ticket.TicketerId)}"", ""{nameof(Ticket.ContentHash)}"", ""{nameof(Ticket.TypeHash)}"") IN ({corteges})")
                        .ToListAsync();

                    foreach (var item in items)
                        Add(item);
                }
            }
        }
    }
}
