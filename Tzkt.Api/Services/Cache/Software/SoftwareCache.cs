using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Dapper;
using Tzkt.Api.Models;

namespace Tzkt.Api.Services.Cache
{
    public class SoftwareCache : DbConnection
    {
        #region static
        const string SelectQuery = @"
        SELECT  ""Id"", ""FirstLevel"",
                ""Metadata""->>'version' as ""Version"",
                ""Metadata""->>'commitDate' as ""CommitDate""
        FROM    ""Software""";
        #endregion

        public SoftwareAlias this[int id]
        {
            get
            {
                lock (this)
                {
                    if (!Software.TryGetValue(id, out var software))
                    {
                        using var db = GetConnection();
                        var row = db.QueryFirst($@"{SelectQuery} WHERE ""Id"" = {id}");
                        software = Parse(row);
                        Software.Add(id, software);
                    }

                    return software;
                }
            }
        }

        readonly Dictionary<int, SoftwareAlias> Software;
        readonly TimeCache Time;
        readonly ILogger Logger;

        public SoftwareCache(TimeCache time, IConfiguration config, ILogger<SoftwareCache> logger) : base(config)
        {
            Logger = logger;
            Time = time;

            Logger.LogDebug("Initializing software cache...");

            using var db = GetConnection();
            var rows = db.Query(SelectQuery);

            Software = rows.ToDictionary(row => (int)row.Id, row => (SoftwareAlias)Parse(row));

            Logger.LogDebug("Loaded {0} software", Software.Count);
        }

        public async Task Reload(List<string> hashes)
        {
            using var db = GetConnection();
            var rows = await db.QueryAsync($@"{SelectQuery} WHERE ""ShortHash"" = ANY(@hashes::character(8)[])", new { hashes });

            lock (this)
            {
                foreach (var row in rows)
                    Software[(int)row.Id] = Parse(row);
            }
        }

        SoftwareAlias Parse(dynamic row) => new()
        {
            Version = row.Version,
            Date = DateTimeOffset.TryParse(row.CommitDate, out DateTimeOffset dt) ? dt.DateTime : Time[row.FirstLevel]
        };
    }
}
