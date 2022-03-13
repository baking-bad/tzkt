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
        readonly ProtocolsCache Protocols;
        readonly TimeCache Time;
        readonly StateCache State;

        public BakingRightsRepository(AccountsCache accounts, ProtocolsCache protocols, TimeCache time, StateCache state, IConfiguration config) : base(config)
        {
            Accounts = accounts;
            Protocols = protocols;
            Time = time;
            State = state;
        }

        public async Task<int> GetCount(
            BakingRightTypeParameter type,
            AccountParameter baker,
            Int32Parameter cycle,
            Int32Parameter level,
            Int32NullParameter slots,
            Int32NullParameter round,
            BakingRightStatusParameter status)
        {
            var sql = new SqlBuilder(@"SELECT COUNT(*) FROM ""BakingRights""")
                .Filter("Cycle", cycle)
                .Filter("Level", level)
                .Filter("BakerId", baker)
                .Filter("Type", type)
                .Filter("Status", status)
                .Filter("Round", round)
                .Filter("Slots", slots);

            using var db = GetConnection();
            return await db.QueryFirstAsync<int>(sql.Query, sql.Params);
        }

        public async Task<IEnumerable<BakingRight>> Get(
            BakingRightTypeParameter type,
            AccountParameter baker,
            Int32Parameter cycle,
            Int32Parameter level,
            Int32NullParameter slots,
            Int32NullParameter round,
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
                .Filter("Round", round)
                .Filter("Slots", slots)
                .Take(sort ?? new SortParameter { Asc = "level" }, offset, limit, x => ("Level", "Level"));

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            return rows.Select(row => new BakingRight
            {
                Type = BakingRightTypes.ToString(row.Type),
                Cycle = row.Cycle,
                Level = row.Level,
                Timestamp = Time[row.Level],
                Baker = Accounts.GetAlias(row.BakerId),
                Round = row.Round,
                Slots = row.Slots,
                Status = BakingRightStatuses.ToString(row.Status)
            });
        }

        public async Task<object[][]> Get(
            BakingRightTypeParameter type,
            AccountParameter baker,
            Int32Parameter cycle,
            Int32Parameter level,
            Int32NullParameter slots,
            Int32NullParameter round,
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
                    case "round": columns.Add(@"""Round"""); break;
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
                .Filter("Round", round)
                .Filter("Slots", slots)
                .Take(sort ?? new SortParameter { Asc = "level" }, offset, limit, x => ("Level", "Level"));

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
                            result[j++][i] = BakingRightTypes.ToString(row.Type);
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
                        foreach (var row in rows)
                            result[j++][i] = Time[row.Level];
                        break;
                    case "baker":
                        foreach (var row in rows)
                            result[j++][i] = await Accounts.GetAliasAsync(row.BakerId);
                        break;
                    case "round":
                        foreach (var row in rows)
                            result[j++][i] = row.Round;
                        break;
                    case "slots":
                        foreach (var row in rows)
                            result[j++][i] = row.Slots;
                        break;
                    case "status":
                        foreach (var row in rows)
                            result[j++][i] = BakingRightStatuses.ToString(row.Status);
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
            Int32NullParameter slots,
            Int32NullParameter round,
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
                case "round": columns.Add(@"""Round"""); break;
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
                .Filter("Round", round)
                .Filter("Slots", slots)
                .Take(sort ?? new SortParameter { Asc = "level" }, offset, limit, x => ("Level", "Level"));

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            //TODO: optimize memory allocation
            var result = new object[rows.Count()];
            var j = 0;

            switch (field)
            {
                case "type":
                    foreach (var row in rows)
                        result[j++] = BakingRightTypes.ToString(row.Type);
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
                    foreach (var row in rows)
                        result[j++] = Time[row.Level];
                    break;
                case "baker":
                    foreach (var row in rows)
                        result[j++] = await Accounts.GetAliasAsync(row.BakerId);
                    break;
                case "round":
                    foreach (var row in rows)
                        result[j++] = row.Round;
                    break;
                case "slots":
                    foreach (var row in rows)
                        result[j++] = row.Slots;
                    break;
                case "status":
                    foreach (var row in rows)
                        result[j++] = BakingRightStatuses.ToString(row.Status);
                    break;
            }

            return result;
        }

        public async Task<IEnumerable<BakingInterval>> GetSchedule(string address, DateTime from, DateTime to, int maxRound)
        {
            var state = State.Current;
            var proto = Protocols.Current;

            var rawAccount = await Accounts.GetAsync(address);

            var fromLevel = from > state.Timestamp
                ? state.Level + (int)(from - state.Timestamp).TotalSeconds / proto.TimeBetweenBlocks
                : Time.FindLevel(from, SearchMode.ExactOrHigher);

            var toLevel = to > state.Timestamp
                ? state.Level + (int)(to - state.Timestamp).TotalSeconds / proto.TimeBetweenBlocks
                : Time.FindLevel(to, SearchMode.ExactOrLower);

            if (!(rawAccount is RawDelegate) || fromLevel == -1 || toLevel == -1)
                return Enumerable.Empty<BakingInterval>();

            var fromCycle = fromLevel < 2 ? 0 : Protocols.FindByLevel(fromLevel).GetCycle(fromLevel);
            var toCycle = toLevel < 2 ? 0 : Protocols.FindByLevel(toLevel).GetCycle(toLevel);

            var sql = $@"
                SELECT ""Level"", ""Slots"", ""Status"" FROM ""BakingRights""
                WHERE ""BakerId"" = {rawAccount.Id}
                AND   ""Cycle"" >= {fromCycle} AND ""Cycle"" <= {toCycle}
                AND   ""Level"" >= {fromLevel} AND ""Level"" <= {toLevel}
                AND   NOT(""Status"" = 0 AND ""Round"" IS NOT NULL AND ""Round"" > {maxRound})";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql);

            var count = (int)Math.Ceiling((to - from).TotalHours);
            var intervals = new List<BakingInterval>(count);
            
            for (int i = 0; i < count; i++)
            {
                var interval = new BakingInterval();
                interval.StartTime = from.AddHours(i);
                interval.EndTime = interval.StartTime.AddSeconds(3599);

                intervals.Add(interval);
            }

            foreach (var row in rows)
            {
                var i = (int)(Time[row.Level] - from).TotalHours;
                if (i >= intervals.Count) continue;

                if (intervals[i].LastLevel == null || row.Level > intervals[i].LastLevel)
                    intervals[i].LastLevel = row.Level;

                if (intervals[i].FirstLevel == null || row.Level < intervals[i].FirstLevel)
                    intervals[i].FirstLevel = row.Level;

                if (intervals[i].Status == null || row.Status > intervals[i].Status)
                    intervals[i].Status = row.Status;

                if (row.Slots == null)
                    intervals[i].Blocks++;
                else
                    intervals[i].Slots += row.Slots;
            }

            return intervals;
        }
    }
}
