using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Dapper;
using Tzkt.Api.Services.Auth;
using Tzkt.Api.Utils;

namespace Tzkt.Api.Repositories
{
    public class MetadataRepository : DbConnection
    {
        public MetadataRepository(IConfiguration config) : base(config) {}

        #region get
        public Task<RawJson> GetStateMetadata(string section = null)
            => Get("AppState", "Id", "integer", -1, section);

        public Task<RawJson> GetAccountMetadata(string address, string section = null)
            => Get("Accounts", "Address", "character(36)", address, section);

        public Task<RawJson> GetProposalMetadata(string hash, string section = null)
            => Get("Proposals", "Hash", "character(51)", hash, section);

        public Task<RawJson> GetProtocolMetadata(string hash, string section = null)
            => Get("Protocols", "Hash", "character(51)", hash, section);

        public Task<RawJson> GetSoftwareMetadata(string shortHash, string section = null)
            => Get("Software", "ShortHash", "character(8)", shortHash, section);

        public Task<RawJson> GetConstantMetadata(string address, string section = null)
            => Get("RegisterConstantOps", "Address", "character(54)", address, section);

        public Task<RawJson> GetBlockMetadata(int level, string section = null)
            => Get("Blocks", "Level", "integer", level, section);

        public Task<RawJson> GetTokenMetadata(int id, string section = null)
            => Get("Tokens", "Id", "integer", id, section);

        async Task<RawJson> Get<T>(string table, string keyColumn, string keyType, T key, string section)
        {
            var path = section != null
                ? " -> @section::text"
                : string.Empty;

            using var db = GetConnection();
            var row = await db.QueryFirstOrDefaultAsync($@"
                SELECT ""Metadata""{path} as metadata
                FROM ""{table}""
                WHERE ""{keyColumn}"" = @key::{keyType}
                LIMIT 1",
                new { section, key });

            return row?.metadata;
        }
        #endregion

        #region get all
        public Task<IEnumerable<MetadataUpdate<string>>> GetAccountMetadata(JsonParameter metadata, int offset, int limit, string section = null)
            => Get<string>("Accounts", "Address", metadata, offset, limit, section);

        public Task<IEnumerable<MetadataUpdate<string>>> GetProposalMetadata(JsonParameter metadata, int offset, int limit, string section = null)
            => Get<string>("Proposals", "Hash", metadata, offset, limit, section);

        public Task<IEnumerable<MetadataUpdate<string>>> GetProtocolMetadata(JsonParameter metadata, int offset, int limit, string section = null)
            => Get<string>("Protocols", "Hash", metadata, offset, limit, section);

        public Task<IEnumerable<MetadataUpdate<string>>> GetSoftwareMetadata(JsonParameter metadata, int offset, int limit, string section = null)
            => Get<string>("Software", "ShortHash", metadata, offset, limit, section);

        public Task<IEnumerable<MetadataUpdate<string>>> GetConstantMetadata(JsonParameter metadata, int offset, int limit, string section = null)
            => Get<string>("RegisterConstantOps", "Address", metadata, offset, limit, section);

        public Task<IEnumerable<MetadataUpdate<int>>> GetBlockMetadata(JsonParameter metadata, int offset, int limit, string section = null)
            => Get<int>("Blocks", "Level", metadata, offset, limit, section);

        public Task<IEnumerable<MetadataUpdate<int>>> GetTokenMetadata(JsonParameter metadata, int offset, int limit, string section = null)
            => Get<int>("Tokens", "Id", metadata, offset, limit, section);

        async Task<IEnumerable<MetadataUpdate<T>>> Get<T>(string table, string keyColumn, JsonParameter metadata, int offset, int limit, string section)
        {
            var path = section != null
                ? " -> @section::text"
                : string.Empty;

            var sql = new SqlBuilder($@"
                SELECT ""{keyColumn}"" AS key, ""Metadata""{path} as metadata
                FROM ""{table}""");

            sql.Filter(@"""Metadata"" IS NOT NULL");
            if (section != null)
            {
                sql.Filter(@"""Metadata"" @> @sectionObj::jsonb");
                sql.Params.Add("@sectionObj", $@"{{ ""{section}"": {{}} }}");
                sql.Params.Add("@section", section);
            }

            if (metadata != null && section != null)
            {
                var sectionPath = new JsonPath(section);
                void PrependSection<TValue>(List<(JsonPath[], TValue)> list)
                {
                    if (list == null) return;

                    var items = list
                        .Select(x => (new[] { sectionPath }.Concat(x.Item1).ToArray(), x.Item2))
                        .ToList();

                    list.Clear();
                    list.AddRange(items);
                }

                PrependSection(metadata.As);
                PrependSection(metadata.Eq);
                PrependSection(metadata.Ge);
                PrependSection(metadata.Gt);
                PrependSection(metadata.In);
                PrependSection(metadata.Le);
                PrependSection(metadata.Lt);
                PrependSection(metadata.Ne);
                PrependSection(metadata.Ni);
                PrependSection(metadata.Null);
                PrependSection(metadata.Un);
            }

            sql.Filter("Metadata", metadata);
            sql.Take(offset, limit);

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            return rows.Select(row => new MetadataUpdate<T>
            {
                Key = row.key,
                Section = section,
                Metadata = row.metadata
            });
        }
        #endregion

        #region update
        public async Task<MetadataUpdate> UpdateStateMetadata(MetadataUpdate metadata)
            => (await Update("AppState", "Id", "integer", new List<MetadataUpdate<int>>
            {
                new()
                {
                    Key = -1,
                    Section = metadata.Section,
                    Metadata = metadata.Metadata
                }
            }))
            .Select(x => new MetadataUpdate
            {
                Section = x.Section,
                Metadata = x.Metadata
            })
            .FirstOrDefault();

        public Task<List<MetadataUpdate<string>>> UpdateAccountMetadata(List<MetadataUpdate<string>> metadata)
            => Update("Accounts", "Address", "character(36)", metadata);

        public Task<List<MetadataUpdate<string>>> UpdatProposalMetadata(List<MetadataUpdate<string>> metadata)
            => Update("Proposals", "Hash", "character(51)", metadata);

        public Task<List<MetadataUpdate<string>>> UpdatProtocolMetadata(List<MetadataUpdate<string>> metadata)
            => Update("Protocols", "Hash", "character(51)", metadata);

        public Task<List<MetadataUpdate<string>>> UpdateSoftwareMetadata(List<MetadataUpdate<string>> metadata)
            => Update("Software", "ShortHash", "character(8)", metadata);

        public Task<List<MetadataUpdate<string>>> UpdateConstantMetadata(List<MetadataUpdate<string>> metadata)
            => Update("RegisterConstantOps", "Address", "character(54)", metadata);

        public Task<List<MetadataUpdate<int>>> UpdateBlockMetadata(List<MetadataUpdate<int>> metadata)
            => Update("Blocks", "Level", "integer", metadata);

        public Task<List<MetadataUpdate<int>>> UpdateTokenMetadata(List<MetadataUpdate<int>> metadata)
            => Update("Tokens", "Id", "integer", metadata);

        async Task<List<MetadataUpdate<T>>> Update<T>(string table, string keyColumn, string keyType, List<MetadataUpdate<T>> metadata)
        {
            var res = new List<MetadataUpdate<T>>(metadata.Count);
            using var db = GetConnection();
            foreach (var meta in metadata)
            {
                var value = (meta.Section == null, meta.Metadata == null) switch
                {
                    (false, false) => @"jsonb_set(COALESCE(""Metadata"", '{}'), @section::text[], @metadata::jsonb)", // set section
                    (false, true) => @"NULLIF(COALESCE(""Metadata"", '{}') #- @section::text[], '{}')", // remove section
                    (true, false) => "@metadata::jsonb", // set root
                    (true, true) => "NULL", // remove root
                };
                
                var rows = await db.ExecuteAsync($@"
                    UPDATE ""{table}""
                    SET ""Metadata"" = {value}
                    WHERE ""{keyColumn}"" = @key::{keyType}",
                    new { key = meta.Key, metadata = (string)meta.Metadata, section = new[] { meta.Section } });

                if (rows > 0)
                    res.Add(meta);
            }
            return res;
        }
        #endregion
    }
}