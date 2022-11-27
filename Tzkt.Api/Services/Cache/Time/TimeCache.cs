using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Dapper;

namespace Tzkt.Api.Services.Cache
{
    public class TimeCache : DbConnection
    {
        readonly List<DateTime> Times;

        readonly StateCache State;
        readonly ProtocolsCache Protocols;
        readonly ILogger Logger;

        public TimeCache(StateCache state, ProtocolsCache protocols, IConfiguration config, ILogger<TimeCache> logger) : base(config)
        {
            logger.LogDebug("Initializing timestamps cache...");

            State = state;
            Protocols = protocols;
            Logger = logger;

            using var db = GetConnection();
            var times = db.Query<DateTime>(@"SELECT ""Timestamp"" FROM ""Blocks"" ORDER BY ""Level""");

            Times = new List<DateTime>(times.Count() + 130_000);
            Times.AddRange(times);

            logger.LogInformation("Loaded {cnt} timestamps", Times.Count);
        }

        public async Task UpdateAsync()
        {
            Logger.LogDebug("Updating timestamps cache...");
            var sql = @"SELECT ""Level"", ""Timestamp"" FROM ""Blocks"" WHERE ""Level"" > @fromLevel ORDER BY ""Level""";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { fromLevel = Math.Min(Times.Count - 1, State.ValidLevel) });

            foreach (var row in rows)
            {
                if (row.Level < Times.Count)
                    Times[row.Level] = row.Timestamp;
                else
                    Times.Add(row.Timestamp);
            }
            Logger.LogDebug("{cnt} timestamps updated", rows.Count());
        }

        public DateTime this[int level]
        {
            get
            {
                if (level >= Times.Count)
                    return Times[^1].AddSeconds((level - Times.Count + 1) * Protocols.Current.TimeBetweenBlocks);
                
                return Times[level];
            }
        }

        public int FindLevel(DateTime datetime, SearchMode mode)
        {
            if (Times.Count == 0)
                return -1;

            if (datetime > Times[^1])
                return mode == SearchMode.ExactOrLower ? Times.Count - 1 : -1;

            if (datetime < Times[0])
                return mode == SearchMode.ExactOrHigher ? 0 : -1;

            #region binary search
            var from = 0;
            var mid = 0;
            var to = Times.Count - 1;

            while (from <= to)
            {
                mid = from + (to - from) / 2;

                if (datetime > Times[mid])
                {
                    from = mid + 1;
                }
                else if (datetime < Times[mid])
                {
                    to = mid - 1;
                }
                else
                {
                    return mid;
                }
            }

            return mode == SearchMode.Exact ? -1 :
                   mode == SearchMode.ExactOrHigher ? from : to;
            #endregion
        }
    }

    public enum SearchMode
    {
        Exact,
        ExactOrLower,
        ExactOrHigher
    }
}
