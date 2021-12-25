using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

using Tzkt.Data;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Services.Cache
{
    public class TokensCache
    {
        public const int MaxItems = 216091; //TODO: set limits in app settings

        static readonly Dictionary<int, Token> CachedById = new(MaxItems);
        static readonly Dictionary<(int, BigInteger), Token> CachedByContract = new(MaxItems);

        readonly TzktContext Db;

        public TokensCache(TzktContext db)
        {
            Db = db;
        }

        public void Reset()
        {
            CachedById.Clear();
            CachedByContract.Clear();
        }

        public void Vacuum()
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
            CachedByContract[(token.ContractId, token.TokenId)] = token;
        }

        public void Remove(Token token)
        {
            CachedById.Remove(token.Id);
            CachedByContract.Remove((token.ContractId, token.TokenId));
        }

        public bool Has(int contractId, BigInteger tokenId)
        {
            return CachedByContract.ContainsKey((contractId, tokenId));
        }

        public Token GetOrAdd(Token token)
        {
            if (CachedById.TryGetValue(token.Id, out var res))
                return res;
            Add(token);
            return token;
        }

        public Token Get(int id)
        {
            if (!CachedById.TryGetValue(id, out var token))
                throw new Exception($"Token #{id} doesn't exist");
            return token;
        }

        public Token Get(int contractId, BigInteger tokenId)
        {
            if (!CachedByContract.TryGetValue((contractId, tokenId), out var token))
                throw new Exception($"Token ({contractId}, {tokenId}) doesn't exist");
            return token;
        }

        public bool TryGet(int contractId, BigInteger tokenId, out Token token)
        {
            return CachedByContract.TryGetValue((contractId, tokenId), out token);
        }

        public async Task Preload(IEnumerable<int> ids)
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
            var missed = ids.Where(x => !CachedByContract.ContainsKey(x)).ToHashSet();
            if (missed.Count > 0)
            {
                var corteges = string.Join(',', missed.Select(x => $"({x.Item1}, '{x.Item2}')"));
                var items = await Db.Tokens
                    .FromSqlRaw($@"
                        SELECT * FROM ""{nameof(TzktContext.Tokens)}""
                        WHERE (""{nameof(Token.ContractId)}"", ""{nameof(Token.TokenId)}"") IN ({corteges})")
                    .ToListAsync();

                foreach (var item in items)
                    Add(item);
            }
        }
    }
}
