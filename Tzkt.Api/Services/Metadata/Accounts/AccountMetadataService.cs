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

        // temporary
        readonly FileSystemWatcher Watcher;

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

            Metadata = links.ToDictionary(x => x.Id, x => accounts.First(a => a.Address == x.Address));
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

            // temporary
            var fi = new FileInfo(Config.AccountsPath);
            Watcher = new FileSystemWatcher(fi.Directory.FullName, fi.Name);
            Watcher.NotifyFilter = NotifyFilters.Size;
            Watcher.Changed += Watcher_Changed;
            Watcher.Error += Watcher_Error;
            Watcher.EnableRaisingEvents = true;
        }

        public AccountMetadata this[int id] => Metadata.TryGetValue(id, out var meta) ? meta : null;

        // temporary
        private void Watcher_Changed(object sender, FileSystemEventArgs e)
        {
            try
            {
                Logger.LogWarning("Updating account aliases");
                var json = File.ReadAllText(Config.AccountsPath);
                var accounts = JsonSerializer.Deserialize<List<AccountMetadata>>(json);

                var dic1 = Metadata.Values.ToDictionary(x => Aliases.First(a => a.Alias == x.Alias).Address);
                var changed = new List<AccountMetadata>();

                foreach (var acc2 in accounts)
                    if (!dic1.TryGetValue(acc2.Address, out var acc1) ||
                        acc2.Alias != acc1.Alias ||
                        acc2.Description != acc1.Description ||
                        acc2.Discord != acc1.Discord ||
                        acc2.Email != acc1.Email ||
                        acc2.Facebook != acc1.Facebook ||
                        acc2.Github != acc1.Github ||
                        acc2.Instagram != acc1.Instagram ||
                        acc2.Kind != acc1.Kind ||
                        acc2.Logo != acc1.Logo ||
                        acc2.Owner != acc1.Owner ||
                        acc2.Reddit != acc1.Reddit ||
                        acc2.Riot != acc1.Riot ||
                        acc2.Site != acc1.Site ||
                        acc2.Slack != acc1.Slack ||
                        acc2.Support != acc1.Support ||
                        acc2.Telegram != acc1.Telegram ||
                        acc2.Twitter != acc1.Twitter)
                        changed.Add(acc2);

                Logger.LogWarning($"{changed.Count} changes detected");
                if (changed.Count > 0)
                {
                    using var db = GetConnection();
                    var links = db.Query<(int Id, string Address)>(
                        @"
                            SELECT  ""Id"", ""Address""
                            FROM    ""Accounts""
                            WHERE   ""Address"" = ANY (@addresses)
                        ",
                        new { addresses = changed.Select(x => x.Address).ToArray() });

                    foreach (var (Id, Address) in links)
                    {
                        var acc = changed.First(x => x.Address == Address);
                        if (!Metadata.ContainsKey(Id))
                        {
                            Aliases.Add(new AccountMetadataAlias
                            {
                                Address = acc.Address,
                                Alias = acc.Alias,
                                Logo = acc.Logo
                            });
                            Metadata.Add(Id, acc);
                        }
                        else
                        {
                            var alias = Aliases.First(x => x.Address == Address);
                            alias.Alias = acc.Alias;
                            alias.Logo = acc.Logo;
                            Metadata[Id] = acc;
                        }
                    }
                    Logger.LogWarning($"{links.Count()} changes applied");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to refresh account aliases: {ex.Message}");
            }
        }

        // temporary
        private void Watcher_Error(object sender, ErrorEventArgs e)
        {
            Logger.LogError($"File watcher error: {e?.GetException()?.Message}");
        }
    }

    public static class AccountMetadataServiceExt
    {
        public static void AddAccountMetadata (this IServiceCollection services)
        {
            services.AddSingleton<AccountMetadataService>();
        }
    }
}
