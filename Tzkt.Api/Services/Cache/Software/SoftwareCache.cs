using System;
using System.Collections.Generic;
using System.Linq;
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
                ""Extras""->>'version' as ""Version"",
                ""Extras""->>'commitDate' as ""CommitDate""
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

            Logger.LogDebug("Loaded {cnt} software", Software.Count);
        }

        public void OnExtrasUpdate(string shortHash)
        {
            lock (this)
            {
                using var db = GetConnection();
                var row = db.QueryFirstOrDefault($@"{SelectQuery} WHERE ""ShortHash"" = @shortHash::character(8)", new { shortHash });
                if (row != null) Software[(int)row.Id] = Parse(row);
            }
        }

        SoftwareAlias Parse(dynamic row) => new()
        {
            Version = row.Version,
            Date = DateTimeOffset.TryParse(row.CommitDate, out DateTimeOffset dt) ? dt.UtcDateTime : Time[row.FirstLevel]
        };
    }
}
