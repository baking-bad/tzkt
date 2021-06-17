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
        public Task<IEnumerable<MetadataRecord>> GetAccountMetadata(int offset, int limit)
            => Get("Accounts", "Address", offset, limit);

        public Task<IEnumerable<MetadataRecord>> GetProposalMetadata(int offset, int limit)
            => Get("Proposals", "Hash", offset, limit);

        public Task<IEnumerable<MetadataRecord>> GetProtocolMetadata(int offset, int limit)
            => Get("Protocols", "Hash", offset, limit);

        public Task<IEnumerable<MetadataRecord>> GetSoftwareMetadata(int offset, int limit)
            => Get("Softwares", "ShortHash", offset, limit);

        async Task<IEnumerable<MetadataRecord>> Get(string table, string key, int offset, int limit)
        {
            using var db = GetConnection();
            var res = await db.QueryAsync($@"
                SELECT ""{key}"" AS key, ""Metadata"" as metadata
                FROM ""{table}""
                WHERE ""Metadata"" @> '{{}}'
                ORDER BY ""Id""
                OFFSET {offset}
                LIMIT {limit}");

            return res.Select(row => new MetadataRecord
            {
                Key = row.key,
                Metadata = row.metadata
            });
        }
        #endregion

        #region update
        public Task<List<MetadataRecord>> UpdateAccountMetadata(List<MetadataRecord> metadata)
            => Update("Accounts", "Address", "character(36)", metadata);

        public Task<List<MetadataRecord>> UpdatProposalMetadata(List<MetadataRecord> metadata)
            => Update("Proposals", "Hash", "character(51)", metadata);

        public Task<List<MetadataRecord>> UpdatProtocolMetadata(List<MetadataRecord> metadata)
            => Update("Protocols", "Hash", "character(51)", metadata);

        public Task<List<MetadataRecord>> UpdateSoftwareMetadata(List<MetadataRecord> metadata)
            => Update("Software", "ShortHash", "character(8)", metadata);

        async Task<List<MetadataRecord>> Update(string table, string key, string keyType, List<MetadataRecord> metadata)
        {
            var res = new List<MetadataRecord>(metadata.Count);
            using var db = GetConnection();
            foreach (var meta in metadata)
            {
                var rows = await db.ExecuteAsync(
                    $@"UPDATE ""{table}"" SET ""Metadata"" = @metadata::jsonb WHERE ""{key}"" = @key::{keyType}",
                    new { metadata = meta.Metadata.Json, key = meta.Key });

                if (rows == 1)
                    res.Add(meta);
            }
            return res;
        }
        #endregion
    }
}