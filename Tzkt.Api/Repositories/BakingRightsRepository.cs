using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Dapper;

using Tzkt.Api.Models;
using Tzkt.Api.Services.Cache;

namespace Tzkt.Api.Repositories
{
    public class BakingRightsRepository : DbConnection
    {
        readonly AccountsCache Accounts;
        readonly TimeCache Time;
        readonly StateCache State;

        public BakingRightsRepository(AccountsCache accounts, TimeCache time, StateCache state, IConfiguration config) : base(config)
        {
            Accounts = accounts;
            Time = time;
            State = state;
        }

        public async Task<int> GetCount(
            BakingRightTypeParameter type,
            AccountParameter baker,
            Int32Parameter cycle,
            Int32Parameter level,
            Int32Parameter slots,
            Int32Parameter priority,
            BakingRightStatusParameter status)
        {
            var sql = new SqlBuilder(@"SELECT COUNT(*) FROM ""BakingRights""")
                .Filter("Cycle", cycle)
                .Filter("Level", level)
                .Filter("BakerId", baker)
                .Filter("Type", type)
                .Filter("Status", status)
                .Filter("Priority", priority)
                .Filter("Slots", slots);

            using var db = GetConnection();
            return await db.QueryFirstAsync<int>(sql.Query, sql.Params);
        }

        public async Task<IEnumerable<BakingRight>> Get(
            BakingRightTypeParameter type,
            AccountParameter baker,
            Int32Parameter cycle,
            Int32Parameter level,
            Int32Parameter slots,
            Int32Parameter priority,
            BakingRightStatusParameter status,
            SortParameter sort,
            OffsetParameter offset,
            int limit)
        {
            var sql = new SqlBuilder(@"SELECT * FROM ""BakingRights""")
                .Filter("Cycle", cycle)
                .Filter("Level", level)
                .Filter("BakerId", baker)
                .Filter("Type", type)
                .Filter("Status", status)
                .Filter("Priority", priority)
                .Filter("Slots", slots)
                .Take(sort ?? new SortParameter { Asc = "level" }, offset, limit, x => "Level");

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            var state = State.GetState();
            return rows.Select(row => new BakingRight
            {
                Type = TypeToString(row.Type),
                Cycle = row.Cycle,
                Level = row.Level,
                Timestamp = row.Status == 0 ? state.Timestamp.AddMinutes(row.Level - state.Level) : Time[row.Level],
                Baker = Accounts.GetAlias(row.BakerId),
                Priority = row.Priority,
                Slots = row.Slots,
                Status = StatusToString(row.Status)
            });
        }

        public async Task<object[][]> Get(
            BakingRightTypeParameter type,
            AccountParameter baker,
            Int32Parameter cycle,
            Int32Parameter level,
            Int32Parameter slots,
            Int32Parameter priority,
            BakingRightStatusParameter status,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            string[] fields)
        {
            var columns = new HashSet<string>(fields.Length);
            foreach (var field in fields)
            {
                switch (field)
                {
                    case "type": columns.Add(@"""Type"""); break;
                    case "cycle": columns.Add(@"""Cycle"""); break;
                    case "level": columns.Add(@"""Level"""); break;
                    case "timestamp": columns.Add(@"""Level"""); columns.Add(@"""Status"""); break;
                    case "baker": columns.Add(@"""BakerId"""); break;
                    case "priority": columns.Add(@"""Priority"""); break;
                    case "slots": columns.Add(@"""Slots"""); break;
                    case "status": columns.Add(@"""Status"""); break;
                }
            }

            if (columns.Count == 0)
                return Array.Empty<object[]>();

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""BakingRights""")
                .Filter("Cycle", cycle)
                .Filter("Level", level)
                .Filter("BakerId", baker)
                .Filter("Type", type)
                .Filter("Status", status)
                .Filter("Priority", priority)
                .Filter("Slots", slots)
                .Take(sort ?? new SortParameter { Asc = "level" }, offset, limit, x => "Level");

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            var result = new object[rows.Count()][];
            for (int i = 0; i < result.Length; i++)
                result[i] = new object[fields.Length];

            for (int i = 0, j = 0; i < fields.Length; j = 0, i++)
            {
                switch (fields[i])
                {
                    case "type":
                        foreach (var row in rows)
                            result[j++][i] = TypeToString(row.Type);
                        break;
                    case "cycle":
                        foreach (var row in rows)
                            result[j++][i] = row.Cycle;
                        break;
                    case "level":
                        foreach (var row in rows)
                            result[j++][i] = row.Level;
                        break;
                    case "timestamp":
                        var state = State.GetState();
                        foreach (var row in rows)
                            result[j++][i] = row.Status == 0 ? state.Timestamp.AddMinutes(row.Level - state.Level) : Time[row.Level];
                        break;
                    case "baker":
                        foreach (var row in rows)
                            result[j++][i] = await Accounts.GetAliasAsync(row.BakerId);
                        break;
                    case "priority":
                        foreach (var row in rows)
                            result[j++][i] = row.Priority;
                        break;
                    case "slots":
                        foreach (var row in rows)
                            result[j++][i] = row.Slots;
                        break;
                    case "status":
                        foreach (var row in rows)
                            result[j++][i] = StatusToString(row.Status);
                        break;
                }
            }

            return result;
        }

        public async Task<object[]> Get(
            BakingRightTypeParameter type,
            AccountParameter baker,
            Int32Parameter cycle,
            Int32Parameter level,
            Int32Parameter slots,
            Int32Parameter priority,
            BakingRightStatusParameter status,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            string field)
        {
            var columns = new HashSet<string>(1);
            switch (field)
            {
                case "type": columns.Add(@"""Type"""); break;
                case "cycle": columns.Add(@"""Cycle"""); break;
                case "level": columns.Add(@"""Level"""); break;
                case "timestamp": columns.Add(@"""Level"""); columns.Add(@"""Status"""); break;
                case "baker": columns.Add(@"""BakerId"""); break;
                case "priority": columns.Add(@"""Priority"""); break;
                case "slots": columns.Add(@"""Slots"""); break;
                case "status": columns.Add(@"""Status"""); break;
            }

            if (columns.Count == 0)
                return Array.Empty<object>();

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""BakingRights""")
                .Filter("Cycle", cycle)
                .Filter("Level", level)
                .Filter("BakerId", baker)
                .Filter("Type", type)
                .Filter("Status", status)
                .Filter("Priority", priority)
                .Filter("Slots", slots)
                .Take(sort ?? new SortParameter { Asc = "level" }, offset, limit, x => "Level");

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            //TODO: optimize memory allocation
            var result = new object[rows.Count()];
            var j = 0;

            switch (field)
            {
                case "type":
                    foreach (var row in rows)
                        result[j++] = TypeToString(row.Type);
                    break;
                case "cycle":
                    foreach (var row in rows)
                        result[j++] = row.Cycle;
                    break;
                case "level":
                    foreach (var row in rows)
                        result[j++] = row.Level;
                    break;
                case "timestamp":
                    var state = State.GetState();
                    foreach (var row in rows)
                        result[j++] = row.Status == 0 ? state.Timestamp.AddMinutes(row.Level - state.Level) : Time[row.Level];
                    break;
                case "baker":
                    foreach (var row in rows)
                        result[j++] = await Accounts.GetAliasAsync(row.BakerId);
                    break;
                case "priority":
                    foreach (var row in rows)
                        result[j++] = row.Priority;
                    break;
                case "slots":
                    foreach (var row in rows)
                        result[j++] = row.Slots;
                    break;
                case "status":
                    foreach (var row in rows)
                        result[j++] = StatusToString(row.Status);
                    break;
            }

            return result;
        }

        string TypeToString(int type) => type switch
        {
            0 => "baking",
            1 => "endorsing",
            _ => "unknown"
        };

        string StatusToString(int status) => status switch
        {
            0 => "future",
            1 => "realized",
            2 => "uncovered",
            3 => "missed",
            _ => "unknown"
        };
    }
}
