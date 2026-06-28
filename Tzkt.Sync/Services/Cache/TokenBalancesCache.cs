using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Tzkt.Data;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Services.Cache
{
    public class TokenBalancesCache(TzktContext db)
    {
        #region static
        static int SoftCap = 0;
        static int TargetCap = 0;
        static Dictionary<(int, HashableBytes?, long), TokenBalance> Cached = [];

        public static void Configure(CacheSize? size)
        {
            SoftCap = size?.SoftCap ?? 120_000;
            TargetCap = size?.TargetCap ?? 100_000;
            Cached = new(SoftCap + 4096);
        }
        #endregion

        readonly TzktContext Db = db;

        public void Reset()
        {
            Cached.Clear();
        }

        public void Trim()
        {
            if (Cached.Count > SoftCap)
            {
                var toRemove = Cached.Values
                    .OrderBy(x => x.LastLevel)
                    .Take(Cached.Count - TargetCap)
                    .ToList();

                foreach (var item in toRemove)
                    Remove(item);
            }
        }

        public void Add(TokenBalance tokenBalance)
        {
            Cached[(tokenBalance.AccountId, HashableBytes.From(tokenBalance.Entrypoint), tokenBalance.TokenId)] = tokenBalance;
        }

        public void Remove(TokenBalance tokenBalance)
        {
            Cached.Remove((tokenBalance.AccountId, HashableBytes.From(tokenBalance.Entrypoint), tokenBalance.TokenId));
        }

        public TokenBalance GetOrAdd(TokenBalance tokenBalance)
        {
            if (Cached.TryGetValue((tokenBalance.AccountId, HashableBytes.From(tokenBalance.Entrypoint), tokenBalance.TokenId), out var res))
                return res;
            Add(tokenBalance);
            return tokenBalance;
        }

        public TokenBalance Get(int accountId, byte[]? entrypoint, long tokenId)
        {
            var _entrypoint = HashableBytes.From(entrypoint);
            if (!Cached.TryGetValue((accountId, _entrypoint, tokenId), out var tokenBalance))
                throw new Exception($"TokenBalance ({accountId}, {_entrypoint}, {tokenId}) doesn't exist");
            return tokenBalance;
        }

        public bool TryGet(int accountId, byte[]? entrypoint, long tokenId, [NotNullWhen(true)] out TokenBalance? tokenBalance)
        {
            return Cached.TryGetValue((accountId, HashableBytes.From(entrypoint), tokenId), out tokenBalance);
        }

        public async Task Preload(IEnumerable<(int AccountId, HashableBytes? Entrypoint, long TokenId)> ids)
        {
            var missed = ids.Where(x => !Cached.ContainsKey(x)).ToHashSet();
            if (missed.Count != 0)
            {
                for (int i = 0, n = 2048; i < missed.Count; i += n)
                {
                    var corteges1 = string.Join(',', missed.Skip(i).Take(n).Where(x => x.Entrypoint == null).Select(x => $"({x.AccountId}, {x.TokenId})"));
                    var corteges2 = string.Join(',', missed.Skip(i).Take(n).Where(x => x.Entrypoint != null).Select(x => $"({x.AccountId}, '\\x{x.Entrypoint}', {x.TokenId})"));
                    string query;

                    if (corteges1.Length != 0)
                    {
                        if (corteges2.Length != 0)
                        {
                            query = $"""
                                SELECT *
                                FROM "TokenBalances"
                                WHERE ("AccountId", "TokenId") IN ({corteges1}) AND "Entrypoint" IS NULL
                                    
                                UNION ALL
                                    
                                SELECT *
                                FROM "TokenBalances"
                                WHERE ("AccountId", "Entrypoint", "TokenId") IN ({corteges2})
                                """;
                        }
                        else
                        {
                            query = $"""
                                SELECT *
                                FROM "TokenBalances"
                                WHERE ("AccountId", "TokenId") IN ({corteges1}) AND "Entrypoint" IS NULL
                                """;
                        }
                    }
                    else
                    {
                        query = $"""
                            SELECT *
                            FROM "TokenBalances"
                            WHERE ("AccountId", "Entrypoint", "TokenId") IN ({corteges2})
                            """;
                    }

#pragma warning disable EF1002 // Risk of vulnerability to SQL injection.
                    var items = await Db.TokenBalances
                        .FromSqlRaw(query)
                        .ToListAsync();
#pragma warning restore EF1002 // Risk of vulnerability to SQL injection.

                    foreach (var item in items)
                        Add(item);
                }
            }
        }
    }
}
