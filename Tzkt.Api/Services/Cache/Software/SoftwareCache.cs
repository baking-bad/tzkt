using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Dapper;
using Tzkt.Api.Models;

namespace Tzkt.Api.Services.Cache
{
    public class SoftwareCache : DbConnection
    {
        public SoftwareAlias this[int id]
        {
            get
            {
                lock (this)
                {
                    if (!Aliases.TryGetValue(id, out var alias))
                    {
                        using var db = GetConnection();
                        var row = db.QueryFirst($@"
                            SELECT  ""Id"", ""FirstLevel"",
                                    ""Metadata""->>'version' as ""Version"",
                                    ""Metadata""->>'commitDate' as ""CommitDate""
                            FROM ""Software""
                            WHERE ""Id"" = {id}");

                        alias = new SoftwareAlias
                        {
                            Version = row.Version,
                            Date = DateTime.TryParse(row.CommitDate, out DateTime dt) ? dt : Time[row.FirstLevel]
                        };

                        Aliases.Add(id, alias);
                    }

                    return alias;
                }
            }
        }

        Dictionary<int, SoftwareAlias> Aliases;
        readonly TimeCache Time;
        readonly ILogger Logger;

        public SoftwareCache(TimeCache time, IConfiguration config, ILogger<SoftwareCache> logger) : base(config)
        {
            Logger = logger;
            Time = time;

            Logger.LogDebug("Initializing software cache...");
            Initialize();
            Logger.LogDebug("Loaded {0} software", Aliases.Count);
        }

        public void Initialize()
        {
            lock (this)
            {
                using var db = GetConnection();
                var rows = db.Query(@"
                SELECT  ""Id"", ""FirstLevel"",
                        ""Metadata""->>'version' as ""Version"",
                        ""Metadata""->>'commitDate' as ""CommitDate""
                FROM ""Software""");

                Aliases = rows.ToDictionary(row => (int)row.Id, row => new SoftwareAlias
                {
                    Version = row.Version,
                    Date = DateTimeOffset.TryParse(row.CommitDate, out DateTimeOffset dt) ? dt.DateTime : Time[row.FirstLevel]
                });
            }
        }
    }

    public static class SoftwareCacheExt
    {
        public static void AddSoftwareCache(this IServiceCollection services)
        {
            services.AddSingleton<SoftwareCache>();
        }
    }
}
