using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Tzkt.Data;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Services.Cache
{
    public class TicketBalancesCache(TzktContext db)
    {
        const int MaxItems = 4 * 4096; //TODO: set limits in app settings
        static readonly Dictionary<(int, long), TicketBalance> Cached = new(MaxItems);

        readonly TzktContext Db = db;

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

        public bool TryGet(int accountId, long ticketId, [NotNullWhen(true)] out TicketBalance? ticketBalance)
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
#pragma warning disable EF1002 // Risk of vulnerability to SQL injection.
                    var items = await Db.TicketBalances
                        .FromSqlRaw($"""
                            SELECT *
                            FROM "TicketBalances"
                            WHERE ("AccountId", "TicketId") IN ({corteges})
                            """)
                        .ToListAsync();
#pragma warning restore EF1002 // Risk of vulnerability to SQL injection.

                    foreach (var item in items)
                        Add(item);
                }
            }
        }
    }
}
