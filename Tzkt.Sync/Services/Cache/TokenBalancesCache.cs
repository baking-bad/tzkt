using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

using Tzkt.Data;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Services.Cache
{
    public class TokenBalancesCache
    {
        public const int MaxItems = 4 * 4096; //TODO: set limits in app settings

        static readonly Dictionary<(int, long), TokenBalance> Cached = new(MaxItems);

        readonly TzktContext Db;

        public TokenBalancesCache(TzktContext db)
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

        public void Add(TokenBalance tokenBalance)
        {
            Cached[(tokenBalance.AccountId, tokenBalance.TokenId)] = tokenBalance;
        }

        public void Remove(TokenBalance tokenBalance)
        {
            Cached.Remove((tokenBalance.AccountId, tokenBalance.TokenId));
        }

        public TokenBalance GetOrAdd(TokenBalance tokenBalance)
        {
            if (Cached.TryGetValue((tokenBalance.AccountId, tokenBalance.TokenId), out var res))
                return res;
            Add(tokenBalance);
            return tokenBalance;
        }

        public TokenBalance Get(int accountId, long tokenId)
        {
            if (!Cached.TryGetValue((accountId, tokenId), out var tokenBalance))
                throw new Exception($"TokenBalance ({accountId}, {tokenId}) doesn't exist");
            return tokenBalance;
        }

        public bool TryGet(int accountId, long tokenId, out TokenBalance tokenBalance)
        {
            return Cached.TryGetValue((accountId, tokenId), out tokenBalance);
        }

        public async Task Preload(IEnumerable<(int, long)> ids)
        {
            var missed = ids.Where(x => !Cached.ContainsKey(x)).ToHashSet();
            if (missed.Count > 0)
            {
                for (int i = 0, n = 2048; i < missed.Count; i += n)
                {
                    var corteges = string.Join(',', missed.Skip(i).Take(n).Select(x => $"({x.Item1}, {x.Item2})"));
                    var items = await Db.TokenBalances
                        .FromSqlRaw($@"
                            SELECT * FROM ""{nameof(TzktContext.TokenBalances)}""
                            WHERE (""{nameof(TokenBalance.AccountId)}"", ""{nameof(TokenBalance.TokenId)}"") IN ({corteges})")
                        .ToListAsync();

                    foreach (var item in items)
                        Add(item);
                }
            }
        }
    }
}
