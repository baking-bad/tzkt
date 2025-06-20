﻿using Microsoft.EntityFrameworkCore;
using System.Diagnostics.CodeAnalysis;
using Tzkt.Data;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Services.Cache
{
    public class TicketsCache(TzktContext db)
    {
        #region static
        static int SoftCap = 0;
        static int TargetCap = 0;
        static Dictionary<long, Ticket> CachedById = [];
        static Dictionary<(int TicketerId, HashableBytes RawType, HashableBytes RawContent), Ticket> CachedByKey = [];

        public static void Configure(CacheSize? size)
        {
            SoftCap = size?.SoftCap ?? 4000;
            TargetCap = size?.TargetCap ?? 3000;
            CachedById = new(SoftCap + 256);
            CachedByKey = new(SoftCap + 256);
        }
        #endregion

        readonly TzktContext Db = db;

        public void Reset()
        {
            CachedById.Clear();
            CachedByKey.Clear();
        }

        public void Trim()
        {
            if (CachedById.Count > SoftCap)
            {
                var toRemove = CachedById.Values
                    .OrderBy(x => x.LastLevel)
                    .Take(CachedById.Count - TargetCap)
                    .ToList();

                foreach (var item in toRemove)
                    Remove(item);
            }
        }

        public void Add(Ticket ticket)
        {
            CachedById[ticket.Id] = ticket;
            CachedByKey[(ticket.TicketerId, ticket.RawType, ticket.RawContent)] = ticket;
        }

        public void Remove(Ticket ticket)
        {
            CachedById.Remove(ticket.Id);
            CachedByKey.Remove((ticket.TicketerId, ticket.RawType, ticket.RawContent));
        }

        public Ticket GetCached(long id)
        {
            if (!CachedById.TryGetValue(id, out var token))
                throw new Exception($"Ticket #{id} doesn't exist in the cache");
            return token;
        }

        public bool TryGetCached(int ticketerId, byte[] rawType, byte[] rawContent, [NotNullWhen(true)] out Ticket? token)
        {
            return CachedByKey.TryGetValue((ticketerId, rawType, rawContent), out token);
        }

        public async Task Preload(IEnumerable<long> ids)
        {
            var missed = ids.Where(x => !CachedById.ContainsKey(x)).ToHashSet();
            if (missed.Count != 0)
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
            if (missed.Count != 0)
            {
                for (int i = 0, n = 2048; i < missed.Count; i += n)
                {
                    var corteges = string.Join(',', missed.Skip(i).Take(n).Select(x => $"({x.Item1}, '{x.Item3}', '{x.Item5}')"));
#pragma warning disable EF1002 // Risk of vulnerability to SQL injection.
                    var items = await Db.Tickets
                        .FromSqlRaw($"""
                            SELECT *
                            FROM "Tickets"
                            WHERE ("TicketerId", "TypeHash", "ContentHash") IN ({corteges})
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
