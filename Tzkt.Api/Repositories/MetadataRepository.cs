using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Configuration;
using Tzkt.Api.Authentication;

namespace Tzkt.Api.Repositories
{
    public class MetadataRepository : DbConnection
    {
        public MetadataRepository(IConfiguration config) : base(config) {}
        
        
        public async Task<IEnumerable<Meta>> Update(string table, string key, List<Meta> metadata)
        {
            using var db = GetConnection();

            foreach (var meta in metadata)
            {
                var upd = $@"UPDATE ""{table}"" SET ""Metadata"" = @metadata::jsonb WHERE ""{key}"" = @key::character(36)";
                await db.ExecuteAsync(upd, new {metadata = meta.Metadata.Json, key = meta.Key});
            }

            var sql = $@"SELECT ""{key}"" AS key, ""Metadata"" FROM ""{table}"" WHERE ""{key}"" IN ('{string.Join("', '", metadata.Select(x => x.Key))}')";
            var res = await db.QueryAsync(sql);
            
            return res.Select(row => new Meta
            {
                Key = row.key,
                Metadata = new RawJson(row.Metadata)
            });
        }

        public async Task<IEnumerable<Meta>> GetMetadata(string table, string key, int limit, OffsetParameter offset)
        {
            using var db = GetConnection();

            var sql = new SqlBuilder($@"SELECT ""{key}"" AS key, ""Metadata"" FROM ""{table}"" WHERE ""Metadata"" IS NOT NULL")
                .Take(offset, limit);
            var res = await db.QueryAsync(sql.Query, sql.Params);
            return res.Select(row => new Meta
            {
                Key = row.key,
                Metadata = new RawJson(row.Metadata)
            });
        }

        public async Task<IEnumerable<Meta>> GetAccounts(string value, int limit, OffsetParameter offset)
        {
            using var db = GetConnection();
            
            var sql = new SqlBuilder($@"SELECT ""Address"" AS key, ""Metadata"" FROM ""Accounts"" WHERE ""Metadata"" ->> 'alias' ILIKE '%{value}%'")
                .Take(offset, limit);
            var res = await db.QueryAsync(sql.Query, sql.Params);
            return res.Select(row => new Meta
            {
                Key = row.key,
                Metadata = new RawJson(row.Metadata)
            });
        }
    }
}