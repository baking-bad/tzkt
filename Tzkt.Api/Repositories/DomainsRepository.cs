using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Dapper;

using Tzkt.Api.Models;
using Tzkt.Api.Services.Cache;

namespace Tzkt.Api.Repositories
{
    public class DomainsRepository : DbConnection
    {
        readonly AccountsCache Accounts;
        readonly TimeCache Times;

        public DomainsRepository(AccountsCache accounts, TimeCache times, IConfiguration config) : base(config)
        {
            Accounts = accounts;
            Times = times;
        }

        async Task<IEnumerable<dynamic>> QueryAsync(DomainFilter filter, Pagination pagination, List<SelectionField> fields = null)
        {
            var select = "*";
            if (fields != null)
            {
                var counter = 0;
                var columns = new HashSet<string>(fields.Count);
                foreach (var field in fields)
                {
                    switch (field.Field)
                    {
                        case "id": columns.Add(@"""Id"""); break;
                        case "level": columns.Add(@"""Level"""); break;
                        case "name": columns.Add(@"""Name"""); break;
                        case "owner": columns.Add(@"""Owner"""); break;
                        case "address": columns.Add(@"""Address"""); break;
                        case "reverse": columns.Add(@"""Reverse"""); break;
                        case "expiration": columns.Add(@"""Expiration"""); break;
                        case "data":
                            if (field.Path == null)
                            {
                                columns.Add(@"""Data""");
                            }
                            else
                            {
                                field.Column = $"c{counter++}";
                                columns.Add($@"""Data"" #> '{{{field.PathString}}}' as {field.Column}");
                            }
                            break;
                        case "firstLevel": columns.Add(@"""FirstLevel"""); break;
                        case "firstTime": columns.Add(@"""FirstLevel"""); break;
                        case "lastLevel": columns.Add(@"""LastLevel"""); break;
                        case "lastTime": columns.Add(@"""LastLevel"""); break;
                    }
                }

                if (columns.Count == 0)
                    return Enumerable.Empty<dynamic>();

                select = string.Join(',', columns);
            }

            var sql = new SqlBuilder($@"SELECT {select} FROM ""Domains""")
                .Filter("Id", filter.id)
                .Filter("Level", filter.level)
                .Filter("Name", filter.name)
                .Filter("Owner", filter.owner)
                .Filter("Address", filter.address)
                .Filter("Reverse", filter.reverse)
                .Filter("Expiration", filter.expiration)
                .Filter("Data", filter.data)
                .Filter("FirstLevel", filter.firstLevel)
                .Filter("FirstLevel", filter.firstTime)
                .Filter("LastLevel", filter.lastLevel)
                .Filter("LastLevel", filter.lastTime)
                .Take(pagination, x => x switch
                {
                    "name" => (@"""Name""", @"""Name"""),
                    "firstLevel" => (@"""FirstLevel""", @"""FirstLevel"""),
                    "lastLevel" => (@"""LastLevel""", @"""LastLevel"""),
                    _ => (@"""Id""", @"""Id""")
                });

            using var db = GetConnection();
            return await db.QueryAsync(sql.Query, sql.Params);
        }

        public async Task<int> GetCount(DomainFilter filter)
        {
            var sql = new SqlBuilder(@"SELECT COUNT(*) FROM ""Domains""")
                .Filter("Id", filter.id)
                .Filter("Level", filter.level)
                .Filter("Name", filter.name)
                .Filter("Owner", filter.owner)
                .Filter("Address", filter.address)
                .Filter("Reverse", filter.reverse)
                .Filter("Expiration", filter.expiration)
                .Filter("Data", filter.data)
                .Filter("FirstLevel", filter.firstLevel)
                .Filter("FirstLevel", filter.firstTime)
                .Filter("LastLevel", filter.lastLevel)
                .Filter("LastLevel", filter.lastTime);

            using var db = GetConnection();
            return await db.QueryFirstAsync<int>(sql.Query, sql.Params);
        }

        public async Task<Domain> Get(string name)
        {
            var sql = @"
                SELECT  *
                FROM    ""Domains""
                WHERE   ""Name"" = @name
                LIMIT   1";

            using var db = GetConnection();
            var row = await db.QueryFirstOrDefaultAsync(sql, new { name });
            if (row == null) return null;

            return new Domain
            {
                Id = row.Id,
                Level = row.Level,
                Name = row.Name,
                Owner = row.Owner == null ? null : Accounts.GetAlias(row.Owner),
                Address = row.Address == null ? null : Accounts.GetAlias(row.Address),
                Reverse = row.Reverse,
                Expiration = row.Expiration,
                Data = row.Data == null ? null : (RawJson)row.Data,
                FirstLevel = row.FirstLevel,
                FirstTime = Times[row.FirstLevel],
                LastLevel = row.LastLevel,
                LastTime = Times[row.LastLevel]
            };
        }

        public async Task<IEnumerable<Domain>> Get(DomainFilter filter, Pagination pagination)
        {
            var rows = await QueryAsync(filter, pagination);
            return rows.Select(row => new Domain
            {
                Id = row.Id,
                Level = row.Level,
                Name = row.Name,
                Owner = row.Owner == null ? null : Accounts.GetAlias(row.Owner),
                Address = row.Address == null ? null : Accounts.GetAlias(row.Address),
                Reverse = row.Reverse,
                Expiration = row.Expiration,
                Data = row.Data == null ? null : (RawJson)row.Data,
                FirstLevel = row.FirstLevel,
                FirstTime = Times[row.FirstLevel],
                LastLevel = row.LastLevel,
                LastTime = Times[row.LastLevel]
            });
        }

        public async Task<object[][]> Get(DomainFilter filter, Pagination pagination, List<SelectionField> fields)
        {
            var rows = await QueryAsync(filter, pagination, fields);

            var result = new object[rows.Count()][];
            for (int i = 0; i < result.Length; i++)
                result[i] = new object[fields.Count];

            for (int i = 0, j = 0; i < fields.Count; j = 0, i++)
            {
                switch (fields[i].Full)
                {
                    case "id":
                        foreach (var row in rows)
                            result[j++][i] = row.Id;
                        break;
                    case "level":
                        foreach (var row in rows)
                            result[j++][i] = row.Level;
                        break;
                    case "name":
                        foreach (var row in rows)
                            result[j++][i] = row.Name;
                        break;
                    case "owner":
                        foreach (var row in rows)
                            result[j++][i] = row.Owner == null ? null : Accounts.GetAlias(row.Owner);
                        break;
                    case "owner.alias":
                        foreach (var row in rows)
                            result[j++][i] = row.Owner == null ? null : Accounts.GetAlias(row.Owner).Name;
                        break;
                    case "owner.address":
                        foreach (var row in rows)
                            result[j++][i] = row.Owner == null ? null : Accounts.GetAlias(row.Owner).Address;
                        break;
                    case "address":
                        foreach (var row in rows)
                            result[j++][i] = row.Address == null ? null : Accounts.GetAlias(row.Address);
                        break;
                    case "address.alias":
                        foreach (var row in rows)
                            result[j++][i] = row.Address == null ? null : Accounts.GetAlias(row.Address).Name;
                        break;
                    case "address.address":
                        foreach (var row in rows)
                            result[j++][i] = row.Address == null ? null : Accounts.GetAlias(row.Address).Address;
                        break;
                    case "reverse":
                        foreach (var row in rows)
                            result[j++][i] = row.Reverse;
                        break;
                    case "expiration":
                        foreach (var row in rows)
                            result[j++][i] = row.Expiration;
                        break;
                    case "data":
                        foreach (var row in rows)
                            result[j++][i] = (RawJson)row.Data;
                        break;
                    case "firstLevel":
                        foreach (var row in rows)
                            result[j++][i] = row.FirstLevel;
                        break;
                    case "firstTime":
                        foreach (var row in rows)
                            result[j++][i] = Times[row.FirstLevel];
                        break;
                    case "lastLevel":
                        foreach (var row in rows)
                            result[j++][i] = row.LastLevel;
                        break;
                    case "lastTime":
                        foreach (var row in rows)
                            result[j++][i] = Times[row.LastLevel];
                        break;
                    default:
                        if (fields[i].Field == "data")
                            foreach (var row in rows)
                                result[j++][i] = (RawJson)((row as IDictionary<string, object>)[fields[i].Column] as string);
                        break;
                }
            }

            return result;
        }
    }
}
