using Microsoft.EntityFrameworkCore;
using Mvkt.Data;
using Mvkt.Data.Models;

namespace Mvkt.Sync.Services.Cache
{
    public class TicketBalancesCache
    {
        public const int MaxItems = 4 * 4096; //TODO: set limits in app settings

        static readonly Dictionary<(int, long), TicketBalance> Cached = new(MaxItems);

        readonly MvktContext Db;

        public TicketBalancesCache(MvktContext db)
        {
            Db = db;
        }

        public void Reset()
        {
            Cached.Clear();
        }

        public void Trim()
        {
            if (Cached.Count > MaxItems * 0.9)
            {
                var toRemove = Cached.Values
                    .OrderBy(x => x.LastLevel)
                    .Take(MaxItems / 2)
                    .ToList();

                foreach (var item in toRemove)
                    Remove(item);
            }
        }

        public void Add(TicketBalance ticketBalance)
        {
            Cached[(ticketBalance.AccountId, ticketBalance.TicketId)] = ticketBalance;
        }

        public void Remove(TicketBalance ticketBalance)
        {
            Cached.Remove((ticketBalance.AccountId, ticketBalance.TicketId));
        }

        public TicketBalance GetOrAdd(TicketBalance ticketBalance)
        {
            if (Cached.TryGetValue((ticketBalance.AccountId, ticketBalance.TicketId), out var res))
                return res;
            Add(ticketBalance);
            return ticketBalance;
        }

        public TicketBalance Get(int accountId, long ticketId)
        {
            if (!Cached.TryGetValue((accountId, ticketId), out var ticketBalance))
                throw new Exception($"TicketBalance ({accountId}, {ticketId}) doesn't exist");
            return ticketBalance;
        }

        public bool TryGet(int accountId, long ticketId, out TicketBalance ticketBalance)
        {
            return Cached.TryGetValue((accountId, ticketId), out ticketBalance);
        }

        public async Task Preload(IEnumerable<(int, long)> ids)
        {
            var missed = ids.Where(x => !Cached.ContainsKey(x)).ToHashSet();
            if (missed.Count > 0)
            {
                for (int i = 0, n = 2048; i < missed.Count; i += n)
                {
                    var corteges = string.Join(',', missed.Skip(i).Take(n).Select(x => $"({x.Item1}, {x.Item2})"));
                    var items = await Db.TicketBalances
                        .FromSqlRaw($@"
                            SELECT * FROM ""{nameof(MvktContext.TicketBalances)}""
                            WHERE (""{nameof(TicketBalance.AccountId)}"", ""{nameof(TicketBalance.TicketId)}"") IN ({corteges})")
                        .ToListAsync();

                    foreach (var item in items)
                        Add(item);
                }
            }
        }
    }
}
