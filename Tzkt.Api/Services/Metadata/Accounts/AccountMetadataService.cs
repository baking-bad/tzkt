using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Dapper;

namespace Tzkt.Api.Services.Metadata
{
    public class AccountMetadataService : DbConnection
    {
        public List<AccountMetadataAlias> Aliases { get; private set; }

        readonly Dictionary<int, AccountMetadata> Metadata;
        readonly MetadataConfig Config;
        readonly ILogger Logger;

        public AccountMetadataService(IConfiguration config, ILogger<AccountMetadataService> logger) : base(config)
        {
            Config = config.GetMetadataConfig();
            Logger = logger;

            if (!File.Exists(Config.AccountsPath))
            {
                Aliases = new List<AccountMetadataAlias>();
                Metadata = new Dictionary<int, AccountMetadata>();
                Logger.LogInformation("Accounts metadata not found");
                return;
            }

            Logger.LogDebug("Loading accounts metadata...");

            var json = File.ReadAllText(Config.AccountsPath);
            var accounts = JsonSerializer.Deserialize<List<AccountMetadata>>(json);

            var sql = @"
                    SELECT  ""Id"", ""Address""
                    FROM    ""Accounts""
                    WHERE   ""Address"" = ANY (@addresses)";

            using var db = GetConnection();
            var links = db.Query<(int Id, string Address)>(sql, new { addresses = accounts.Select(x => x.Address).ToArray() });

            Metadata = accounts.ToDictionary(x => links.First(l => l.Address == x.Address).Id);
            Aliases = new List<AccountMetadataAlias>(Metadata.Count);

            foreach (var meta in Metadata.Values)
            {
                Aliases.Add(new AccountMetadataAlias
                {
                    Address = meta.Address,
                    Alias = meta.Alias,
                    Logo = meta.Logo
                });

                meta.Address = null;
            }

            Logger.LogDebug($"Loaded {Metadata.Count} accounts metadata");
        }

        public AccountMetadata this[int id] => Metadata.TryGetValue(id, out var meta) ? meta : null;
    }

    public static class AccountMetadataServiceExt
    {
        public static void AddAccountMetadata (this IServiceCollection services)
        {
            services.AddSingleton<AccountMetadataService>();
        }
    }
}
