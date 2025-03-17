using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Tzkt.Data;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Services.Cache
{
    public class TokensCache(TzktContext db)
    {
        const int MaxItems = 4 * 4096; //TODO: set limits in app settings
        static readonly Dictionary<long, Token> CachedById = new(MaxItems);
        static readonly Dictionary<(int, BigInteger), Token> CachedByKey = new(MaxItems);

        readonly TzktContext Db = db;

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

        public void Add(Token token)
        {
            CachedById[token.Id] = token;
            CachedByKey[(token.ContractId, token.TokenId)] = token;
        }

        public void Remove(Token token)
        {
            CachedById.Remove(token.Id);
            CachedByKey.Remove((token.ContractId, token.TokenId));
        }

        public bool Has(int contractId, BigInteger tokenId)
        {
            return CachedByKey.ContainsKey((contractId, tokenId));
        }

        public Token GetOrAdd(Token token)
        {
            if (CachedById.TryGetValue(token.Id, out var res))
                return res;
            Add(token);
            return token;
        }

        public Token Get(long id)
        {
            if (!CachedById.TryGetValue(id, out var token))
                throw new Exception($"Token #{id} doesn't exist");
            return token;
        }

        public Token Get(int contractId, BigInteger tokenId)
        {
            if (!CachedByKey.TryGetValue((contractId, tokenId), out var token))
                throw new Exception($"Token ({contractId}, {tokenId}) doesn't exist");
            return token;
        }

        public bool TryGet(int contractId, BigInteger tokenId, [NotNullWhen(true)] out Token? token)
        {
            return CachedByKey.TryGetValue((contractId, tokenId), out token);
        }

        public async Task Preload(IEnumerable<long> ids)
        {
            var missed = ids.Where(x => !CachedById.ContainsKey(x)).ToHashSet();
            if (missed.Count > 0)
            {
                var items = await Db.Tokens
                    .Where(x => missed.Contains(x.Id))
                    .ToListAsync();

                foreach (var item in items)
                    Add(item);
            }
        }

        public async Task Preload(IEnumerable<(int, BigInteger)> ids)
        {
            var missed = ids.Where(x => !CachedByKey.ContainsKey(x)).ToHashSet();
            if (missed.Count > 0)
            {
                for (int i = 0, n = 2048; i < missed.Count; i += n)
                {
                    var corteges = string.Join(',', missed.Skip(i).Take(n).Select(x => $"({x.Item1}, '{x.Item2}')"));
#pragma warning disable EF1002 // Risk of vulnerability to SQL injection.
                    var items = await Db.Tokens
                        .FromSqlRaw($"""
                            SELECT *
                            FROM "Tokens"
                            WHERE ("ContractId", "TokenId") IN ({corteges})
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
