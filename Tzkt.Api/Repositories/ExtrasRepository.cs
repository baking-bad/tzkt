using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Dapper;
using Tzkt.Api.Services.Auth;
using Tzkt.Api.Utils;

namespace Tzkt.Api.Repositories
{
    public class ExtrasRepository : DbConnection
    {
        public ExtrasRepository(IConfiguration config) : base(config) {}

        #region get
        public Task<RawJson> GetStateExtras(string section = null)
            => Get("AppState", "Id", "integer", -1, section);

        public Task<RawJson> GetAccountExtras(string address, string section = null)
            => Get("Accounts", "Address", "varchar(37)", address, section);

        public Task<RawJson> GetProposalExtras(string hash, string section = null)
            => Get("Proposals", "Hash", "character(51)", hash, section);

        public Task<RawJson> GetProtocolExtras(string hash, string section = null)
            => Get("Protocols", "Hash", "character(51)", hash, section);

        public Task<RawJson> GetSoftwareExtras(string shortHash, string section = null)
            => Get("Software", "ShortHash", "character(8)", shortHash, section);

        public Task<RawJson> GetConstantExtras(string address, string section = null)
            => Get("RegisterConstantOps", "Address", "character(54)", address, section);

        public Task<RawJson> GetBlockExtras(int level, string section = null)
            => Get("Blocks", "Level", "integer", level, section);

        async Task<RawJson> Get<T>(string table, string keyColumn, string keyType, T key, string section)
        {
            var path = section != null
                ? " -> @section::text"
                : string.Empty;

            using var db = GetConnection();
            var row = await db.QueryFirstOrDefaultAsync($@"
                SELECT ""Extras""{path} as extras
                FROM ""{table}""
                WHERE ""{keyColumn}"" = @key::{keyType}
                LIMIT 1",
                new { section, key });

            return row?.extras;
        }
        #endregion

        #region get all
        public Task<IEnumerable<ExtrasUpdate<string>>> GetAccountExtras(JsonParameter extras, int offset, int limit, string section = null)
            => Get<string>("Accounts", "Address", extras, offset, limit, section);

        public Task<IEnumerable<ExtrasUpdate<string>>> GetProposalExtras(JsonParameter extras, int offset, int limit, string section = null)
            => Get<string>("Proposals", "Hash", extras, offset, limit, section);

        public Task<IEnumerable<ExtrasUpdate<string>>> GetProtocolExtras(JsonParameter extras, int offset, int limit, string section = null)
            => Get<string>("Protocols", "Hash", extras, offset, limit, section);

        public Task<IEnumerable<ExtrasUpdate<string>>> GetSoftwareExtras(JsonParameter extras, int offset, int limit, string section = null)
            => Get<string>("Software", "ShortHash", extras, offset, limit, section);

        public Task<IEnumerable<ExtrasUpdate<string>>> GetConstantExtras(JsonParameter extras, int offset, int limit, string section = null)
            => Get<string>("RegisterConstantOps", "Address", extras, offset, limit, section);

        public Task<IEnumerable<ExtrasUpdate<int>>> GetBlockExtras(JsonParameter extras, int offset, int limit, string section = null)
            => Get<int>("Blocks", "Level", extras, offset, limit, section);

        async Task<IEnumerable<ExtrasUpdate<T>>> Get<T>(string table, string keyColumn, JsonParameter extras, int offset, int limit, string section)
        {
            var path = section != null
                ? " -> @section::text"
                : string.Empty;

            var sql = new SqlBuilder($@"
                SELECT ""{keyColumn}"" AS key, ""Extras""{path} as extras
                FROM ""{table}""");

            sql.Filter(@"""Extras"" IS NOT NULL");
            if (section != null)
            {
                sql.Filter(@"""Extras"" @> @sectionObj::jsonb");
                sql.Params.Add("@sectionObj", $@"{{ ""{section}"": {{}} }}");
                sql.Params.Add("@section", section);
            }

            if (extras != null && section != null)
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

                PrependSection(extras.As);
                PrependSection(extras.Eq);
                PrependSection(extras.Ge);
                PrependSection(extras.Gt);
                PrependSection(extras.In);
                PrependSection(extras.Le);
                PrependSection(extras.Lt);
                PrependSection(extras.Ne);
                PrependSection(extras.Ni);
                PrependSection(extras.Null);
                PrependSection(extras.Un);
            }

            sql.Filter("Extras", extras);
            sql.Take(offset, limit);

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            return rows.Select(row => new ExtrasUpdate<T>
            {
                Key = row.key,
                Extras = row.extras
            });
        }
        #endregion

        #region update
        public async Task<ExtrasUpdate> UpdateStateExtras(ExtrasUpdate extras, string section)
            => (await Update("AppState", "Id", "integer", new List<ExtrasUpdate<int>>
            {
                new()
                {
                    Key = -1,
                    Extras = extras.Extras
                }
            }, section))
            .Select(x => new ExtrasUpdate
            {
                Extras = x.Extras
            })
            .FirstOrDefault();

        public Task<List<ExtrasUpdate<string>>> UpdateAccountExtras(List<ExtrasUpdate<string>> extras, string section)
            => Update("Accounts", "Address", "varchar(37)", extras, section);

        public Task<List<ExtrasUpdate<string>>> UpdateProposalExtras(List<ExtrasUpdate<string>> extras, string section)
            => Update("Proposals", "Hash", "character(51)", extras, section);

        public Task<List<ExtrasUpdate<string>>> UpdateProtocolExtras(List<ExtrasUpdate<string>> extras, string section)
            => Update("Protocols", "Hash", "character(51)", extras, section);

        public Task<List<ExtrasUpdate<string>>> UpdateSoftwareExtras(List<ExtrasUpdate<string>> extras, string section)
            => Update("Software", "ShortHash", "character(8)", extras, section);

        public Task<List<ExtrasUpdate<string>>> UpdateConstantExtras(List<ExtrasUpdate<string>> extras, string section)
            => Update("RegisterConstantOps", "Address", "character(54)", extras, section);

        public Task<List<ExtrasUpdate<int>>> UpdateBlockExtras(List<ExtrasUpdate<int>> extras, string section)
            => Update("Blocks", "Level", "integer", extras, section);

        async Task<List<ExtrasUpdate<T>>> Update<T>(string table, string keyColumn, string keyType, List<ExtrasUpdate<T>> extras, string section)
        {
            var res = new List<ExtrasUpdate<T>>(extras.Count);
            using var db = GetConnection();
            foreach (var update in extras)
            {
                var value = (section == null, update.Extras == null) switch
                {
                    (false, false) => @"jsonb_set(COALESCE(""Extras"", '{}'), @section::text[], @extras::jsonb)", // set section
                    (false, true) => @"NULLIF(COALESCE(""Extras"", '{}') #- @section::text[], '{}')", // remove section
                    (true, false) => "@extras::jsonb", // set root
                    (true, true) => "NULL", // remove root
                };
                
                var rows = await db.ExecuteAsync($@"
                    UPDATE ""{table}""
                    SET ""Extras"" = {value}
                    WHERE ""{keyColumn}"" = @key::{keyType}",
                    new { key = update.Key, extras = (string)update.Extras, section = new[] { section } });

                if (rows > 0)
                    res.Add(update);
            }
            return res;
        }
        #endregion
    }
}