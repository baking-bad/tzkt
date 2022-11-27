using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Dapper;

namespace Tzkt.Api.Services.Cache
{
    public class BigMapsCache : DbConnection
    {
        const int MaxPtrs = 4096;
        readonly Dictionary<string, RawBigMap> Cached = new(MaxPtrs);
        readonly SemaphoreSlim Sema = new(1);
        int LastUpdate;

        readonly StateCache State;
        readonly ILogger Logger;

        public BigMapsCache(StateCache state, IConfiguration config, ILogger<BigMapsCache> logger) : base(config)
        {
            State = state;
            Logger = logger;
            LastUpdate = state.Current.Level;
        }

        public async Task UpdateAsync()
        {
            Logger.LogDebug("Updating bigmaps cache...");
            var from = Math.Min(LastUpdate, State.ValidLevel);
            try
            {
                await Sema.WaitAsync();
                #region check reorg
                if (State.Reorganized)
                {
                    List<string> corrupted;
                    corrupted = Cached
                        .Where(x => x.Value.LastLevel > from)
                        .Select(x => x.Key)
                        .ToList();

                    foreach (var key in corrupted)
                        Cached.Remove(key);

                    Logger.LogDebug("Removed {cnt} corrupted BigMaps", corrupted.Count);
                }
                #endregion

                var sql = @"
                    SELECT  ""Ptr"", ""LastLevel"", ""Active""
                    FROM    ""BigMaps""
                    WHERE   ""LastLevel"" > @from";

                using var db = GetConnection();
                var rows = await db.QueryAsync(sql, new { from });
                if (!rows.Any()) return;

                var removed = rows.Where(x => !x.Active).Select(x => (int)x.Ptr).ToHashSet();
                var keys = Cached.Where(x => removed.Contains(x.Value.Ptr)).Select(x => x.Key).ToList();
                foreach (var key in keys)
                    Cached.Remove(key);

                var updated = rows.Where(x => x.Active).ToDictionary(x => (int)x.Ptr, x => (int)x.LastLevel);
                foreach (var item in Cached.Values)
                    if (updated.TryGetValue(item.Ptr, out var lastLevel))
                        item.LastLevel = lastLevel;

                LastUpdate = State.Current.Level;
                Logger.LogDebug("Updated {cnd} bigmaps since block {level}", removed.Count + updated.Count, from);
            }
            finally
            {
                Sema.Release();
            }
        }

        public async Task<int?> GetPtrAsync(int contractId, string path)
        {
            var key = $"{contractId}~{path}";
            if (!Cached.TryGetValue(key, out var item))
            {
                try
                {
                    await Sema.WaitAsync();
                    if (!Cached.TryGetValue(key, out item))
                    {
                        var sql = @"
                        SELECT  ""Ptr"", ""StoragePath"", ""LastLevel""
                        FROM    ""BigMaps""
                        WHERE   ""ContractId"" = @id
                        AND 	""Active"" = true
                        AND     ""StoragePath"" LIKE @name";

                        using var db = GetConnection();
                        var rows = await db.QueryAsync(sql, new { id = contractId, name = $"%{path}" });
                        if (!rows.Any()) return null;

                        var row = rows.FirstOrDefault(x => x.StoragePath == path) ?? rows.First();
                        item = new RawBigMap
                        {
                            Ptr = row.Ptr,
                            LastLevel = row.LastLevel
                        };
                        CheckSpace();
                        Cached.Add(key, item);
                    }
                }
                finally
                {
                    Sema.Release();
                }
            }
            return item.Ptr;
        }

        void CheckSpace()
        {
            if (Cached.Count >= MaxPtrs)
            {
                foreach (var key in Cached.Keys.Take(MaxPtrs / 4).ToList())
                    Cached.Remove(key);
            }
        }
    }
}
