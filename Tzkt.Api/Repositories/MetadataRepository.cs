using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Tzkt.Api.Authentication;
using Tzkt.Api.Controllers;

namespace Tzkt.Api.Repositories
{
    public class MetadataRepository : DbConnection
    {
        public MetadataRepository(IConfiguration config) : base(config) {}
        
        
        public async Task<IEnumerable<dynamic>> Update(string table, string key, List<Met> metadatas)
        {
            using var db = GetConnection();

            foreach (var metadata in metadatas)
            {
                //TODO Return amount of affected rows?
                var upd = $@"UPDATE ""{table}"" SET ""Metadata"" = @metadata::jsonb WHERE ""{key}"" = @key";
                var rows = await db.ExecuteAsync(upd, new {metadata = metadata.Metadata.Json, key = metadata.Key});
            }
            
            var sql = $@"SELECT ""{key}"", ""Metadata"" FROM ""{table}"" WHERE ""{key}"" IN ('{string.Join("', '", metadatas.Select(x => x.Key))}')";
            return await db.QueryAsync(sql);
        }

        public async Task<IEnumerable<Met>> GetMetadata(string table, string key, int limit, OffsetParameter offset)
        {
            using var db = GetConnection();

            var sql = new SqlBuilder($@"SELECT ""{key}"", ""Metadata"" FROM ""{table}"" WHERE ""Metadata"" IS NOT NULL")
                .Take(offset, limit);
            var res = await db.QueryAsync(sql.Query, sql.Params);
            return res.Select(row => new Met
            {
                Key = row.ShortHash,
                Metadata = new RawJson(row.Metadata)
            });

        }
    }
}