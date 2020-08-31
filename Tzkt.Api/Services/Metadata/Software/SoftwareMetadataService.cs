using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Tzkt.Api.Models;
using Tzkt.Api.Services.Cache;

namespace Tzkt.Api.Services.Metadata
{
    public class SoftwareMetadataService : DbConnection
    {
        readonly Dictionary<int, SoftwareAlias> Aliases;
        readonly TimeCache Time;
        readonly ILogger Logger;

        public SoftwareMetadataService(TimeCache time, IConfiguration config, ILogger<SoftwareMetadataService> logger) : base(config)
        {
            Logger = logger;
            Time = time;

            Logger.LogDebug("Loading software metadata...");

            using var db = GetConnection();
            var rows = db.Query(@"SELECT ""Id"", ""FirstLevel"", ""Version"", ""CommitDate"" FROM ""Software""");

            Aliases = rows.ToDictionary(row => (int)row.Id, row => new SoftwareAlias
            {
                Version = row.Version,
                Date = row.CommitDate ?? Time[row.FirstLevel]
            });

            Logger.LogDebug($"Loaded {Aliases.Count} software metadata");
        }

        public SoftwareAlias this[int id]
        {
            get
            {
                lock (Aliases)
                {
                    if (!Aliases.TryGetValue(id, out var alias))
                    {
                        using var db = GetConnection();
                        var row = db.QueryFirst($@"
                            SELECT ""Id"", ""FirstLevel"", ""Version"", ""CommitDate""
                            FROM ""Software""
                            WHERE ""Id"" = {id}");

                        alias = new SoftwareAlias
                        {
                            Version = row.Version,
                            Date = row.CommitDate ?? Time[row.FirstLevel]
                        };

                        Aliases.Add(id, alias);
                    }

                    return alias;
                }
            }
        }

        public async Task Refresh()
        {
            using var db = GetConnection();
            var rows = await db.QueryAsync(@"SELECT ""Id"", ""FirstLevel"", ""Version"", ""CommitDate"" FROM ""Software""");

            foreach (var row in rows)
            {
                if (Aliases.TryGetValue((int)row.Id, out var alias) && alias.Version != row.Version)
                {
                    alias.Version = row.Version;
                    alias.Date = row.CommitDate ?? Time[row.FirstLevel];
                }
            }
        }
    }

    public static class SoftwareMetadataServiceExt
    {
        public static void AddSoftwareMetadata(this IServiceCollection services)
        {
            services.AddSingleton<SoftwareMetadataService>();
        }
    }
}
