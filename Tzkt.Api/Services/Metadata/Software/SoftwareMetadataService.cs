using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Dapper;
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
            var rows = db.Query(@"
                SELECT ""Id"", ""FirstLevel"", ""Metadata""->>'version' as ""Version"", ""Metadata""->>'commitDate' as ""CommitDate""
                FROM ""Software""");

            Aliases = rows.ToDictionary(row => (int)row.Id, row => new SoftwareAlias
            {
                Version = row.Version,
                Date = DateTimeOffset.TryParse(row.CommitDate, out DateTimeOffset dt) ? dt.DateTime : Time[row.FirstLevel]
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
                            SELECT ""Id"", ""FirstLevel"", ""Metadata""->>'version' as ""Version"", ""Metadata""->>'commitDate' as ""CommitDate""
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
    }

    public static class SoftwareMetadataServiceExt
    {
        public static void AddSoftwareMetadata(this IServiceCollection services)
        {
            services.AddSingleton<SoftwareMetadataService>();
        }
    }
}
