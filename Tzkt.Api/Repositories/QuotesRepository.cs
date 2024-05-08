using Dapper;
using Npgsql;
using Tzkt.Api.Models;
using Tzkt.Api.Services.Cache;

namespace Tzkt.Api.Repositories
{
    public class QuotesRepository
    {
        readonly NpgsqlDataSource DataSource;
        readonly QuotesCache Quotes;
        readonly StateCache State;
        readonly TimeCache Time;

        public QuotesRepository(NpgsqlDataSource dataSource, StateCache state, TimeCache time, QuotesCache quotes)
        {
            DataSource = dataSource;
            Quotes = quotes;
            State = state;
            Time = time;
        }

        public int GetCount()
        {
            return State.Current.QuoteLevel + 1;
        }

        public Quote GetLast()
        {
            var state = State.Current;
            return new Quote
            {
                Level = state.Level,
                Timestamp = Time[state.Level],
                Btc = Quotes.Get(0),
                Eur = Quotes.Get(1),
                Usd = Quotes.Get(2),
                Cny = Quotes.Get(3),
                Jpy = Quotes.Get(4),
                Krw = Quotes.Get(5),
                Eth = Quotes.Get(6),
                Gbp = Quotes.Get(7)
            };
        }

        public async Task<IEnumerable<Quote>> Get(
            Int32Parameter level,
            DateTimeParameter timestamp,
            SortParameter sort,
            OffsetParameter offset,
            int limit)
        {
            var sql = new SqlBuilder(@"SELECT * FROM ""Quotes""")
                .Filter("Level", level)
                .Filter("Timestamp", timestamp)
                .Take(sort, offset, limit, x => x switch
                {
                    "level" => ("Level", "Level"),
                    _ => ("Id", "Id")
                });

            await using var db = await DataSource.OpenConnectionAsync();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            return rows.Select(row => new Quote
            {
                Level = row.Level,
                Timestamp = row.Timestamp,
                Btc = row.Btc,
                Eur = row.Eur,
                Usd = row.Usd,
                Cny = row.Cny,
                Jpy = row.Jpy,
                Krw = row.Krw,
                Eth = row.Eth,
                Gbp = row.Gbp
            });
        }


        public async Task<object[][]> Get(
            Int32Parameter level,
            DateTimeParameter timestamp,
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
                    case "level": columns.Add(@"""Level"""); break;
                    case "timestamp": columns.Add(@"""Timestamp"""); break;
                    case "btc": columns.Add(@"""Btc"""); break;
                    case "eur": columns.Add(@"""Eur"""); break;
                    case "usd": columns.Add(@"""Usd"""); break;
                    case "cny": columns.Add(@"""Cny"""); break;
                    case "jpy": columns.Add(@"""Jpy"""); break;
                    case "krw": columns.Add(@"""Krw"""); break;
                    case "eth": columns.Add(@"""Eth"""); break;
                    case "gbp": columns.Add(@"""Gbp"""); break;
                }
            }

            if (columns.Count == 0)
                return Array.Empty<object[]>();

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""Quotes""")
                .Filter("Level", level)
                .Filter("Timestamp", timestamp)
                .Take(sort, offset, limit, x => x switch
                {
                    "level" => ("Level", "Level"),
                    _ => ("Id", "Id")
                });

            await using var db = await DataSource.OpenConnectionAsync();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            var result = new object[rows.Count()][];
            for (int i = 0; i < result.Length; i++)
                result[i] = new object[fields.Length];

            for (int i = 0, j = 0; i < fields.Length; j = 0, i++)
            {
                switch (fields[i])
                {
                    case "level":
                        foreach (var row in rows)
                            result[j++][i] = row.Level;
                        break;
                    case "timestamp":
                        foreach (var row in rows)
                            result[j++][i] = row.Timestamp;
                        break;
                    case "btc":
                        foreach (var row in rows)
                            result[j++][i] = row.Btc;
                        break;
                    case "eur":
                        foreach (var row in rows)
                            result[j++][i] = row.Eur;
                        break;
                    case "usd":
                        foreach (var row in rows)
                            result[j++][i] = row.Usd;
                        break;
                    case "cny":
                        foreach (var row in rows)
                            result[j++][i] = row.Cny;
                        break;
                    case "jpy":
                        foreach (var row in rows)
                            result[j++][i] = row.Jpy;
                        break;
                    case "krw":
                        foreach (var row in rows)
                            result[j++][i] = row.Krw;
                        break;
                    case "eth":
                        foreach (var row in rows)
                            result[j++][i] = row.Eth;
                        break;
                    case "gbp":
                        foreach (var row in rows)
                            result[j++][i] = row.Gbp;
                        break;
                }
            }

            return result;
        }

        public async Task<object[]> Get(
            Int32Parameter level,
            DateTimeParameter timestamp,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            string field)
        {
            var columns = new HashSet<string>(1);
            switch (field)
            {
                case "level": columns.Add(@"""Level"""); break;
                case "timestamp": columns.Add(@"""Timestamp"""); break;
                case "btc": columns.Add(@"""Btc"""); break;
                case "eur": columns.Add(@"""Eur"""); break;
                case "usd": columns.Add(@"""Usd"""); break;
                case "cny": columns.Add(@"""Cny"""); break;
                case "jpy": columns.Add(@"""Jpy"""); break;
                case "krw": columns.Add(@"""Krw"""); break;
                case "eth": columns.Add(@"""Eth"""); break;
                case "gbp": columns.Add(@"""Gbp"""); break;
            }

            if (columns.Count == 0)
                return Array.Empty<object>();

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""Quotes""")
                .Filter("Level", level)
                .Filter("Timestamp", timestamp)
                .Take(sort, offset, limit, x => x switch
                {
                    "level" => ("Level", "Level"),
                    _ => ("Id", "Id")
                });

            await using var db = await DataSource.OpenConnectionAsync();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            //TODO: optimize memory allocation
            var result = new object[rows.Count()];
            var j = 0;

            switch (field)
            {
                case "level":
                    foreach (var row in rows)
                        result[j++] = row.Level;
                    break;
                case "timestamp":
                    foreach (var row in rows)
                        result[j++] = row.Timestamp;
                    break;
                case "btc":
                    foreach (var row in rows)
                        result[j++] = row.Btc;
                    break;
                case "eur":
                    foreach (var row in rows)
                        result[j++] = row.Eur;
                    break;
                case "usd":
                    foreach (var row in rows)
                        result[j++] = row.Usd;
                    break;
                case "cny":
                    foreach (var row in rows)
                        result[j++] = row.Cny;
                    break;
                case "jpy":
                    foreach (var row in rows)
                        result[j++] = row.Jpy;
                    break;
                case "krw":
                    foreach (var row in rows)
                        result[j++] = row.Krw;
                    break;
                case "eth":
                    foreach (var row in rows)
                        result[j++] = row.Eth;
                    break;
                case "gbp":
                    foreach (var row in rows)
                        result[j++] = row.Gbp;
                    break;
            }

            return result;
        }
    }
}
