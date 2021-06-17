using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Dapper;
using Tzkt.Api.Services.Auth;

namespace Tzkt.Api.Repositories
{
    public class MetadataRepository : DbConnection
    {
        public MetadataRepository(IConfiguration config) : base(config) {}

        #region get
        public Task<IEnumerable<ObjectMetadata>> GetAccountMetadata(int offset, int limit)
            => Get("Accounts", "Address", offset, limit);

        public Task<IEnumerable<ObjectMetadata>> GetProposalMetadata(int offset, int limit)
            => Get("Proposals", "Hash", offset, limit);

        public Task<IEnumerable<ObjectMetadata>> GetProtocolMetadata(int offset, int limit)
            => Get("Protocols", "Hash", offset, limit);

        public Task<IEnumerable<ObjectMetadata>> GetSoftwareMetadata(int offset, int limit)
            => Get("Software", "ShortHash", offset, limit);

        async Task<IEnumerable<ObjectMetadata>> Get(string table, string key, int offset, int limit)
        {
            using var db = GetConnection();
            var rows = await db.QueryAsync($@"
                SELECT ""{key}"" AS key, ""Metadata"" as metadata
                FROM ""{table}""
                WHERE ""Metadata"" @> '{{}}'
                ORDER BY ""Id""
                OFFSET {offset}
                LIMIT {limit}");

            return rows.Select(row => new ObjectMetadata
            {
                Key = row.key,
                Metadata = row.metadata
            });
        }
        #endregion

        #region update
        public Task<List<ObjectMetadata>> UpdateAccountMetadata(List<ObjectMetadata> metadata)
            => Update("Accounts", "Address", "character(36)", metadata);

        public Task<List<ObjectMetadata>> UpdatProposalMetadata(List<ObjectMetadata> metadata)
            => Update("Proposals", "Hash", "character(51)", metadata);

        public Task<List<ObjectMetadata>> UpdatProtocolMetadata(List<ObjectMetadata> metadata)
            => Update("Protocols", "Hash", "character(51)", metadata);

        public Task<List<ObjectMetadata>> UpdateSoftwareMetadata(List<ObjectMetadata> metadata)
            => Update("Software", "ShortHash", "character(8)", metadata);

        async Task<List<ObjectMetadata>> Update(string table, string key, string keyType, List<ObjectMetadata> metadata)
        {
            var res = new List<ObjectMetadata>(metadata.Count);
            using var db = GetConnection();
            foreach (var meta in metadata)
            {
                var rows = await db.ExecuteAsync(
                    $@"UPDATE ""{table}"" SET ""Metadata"" = @metadata::jsonb WHERE ""{key}"" = @key::{keyType}",
                    new { key = meta.Key, metadata = meta.Metadata.Json });

                if (rows == 1)
                    res.Add(meta);
            }
            return res;
        }
        #endregion
    }
}