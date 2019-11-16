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

using Tzkt.Api.Models;

namespace Tzkt.Api.Services
{
    public class AliasService : DbConnection
    {
        const string Path = "Services\\Aliases\\Aliases.json";

        readonly Dictionary<int, Alias> Aliases;
        readonly ILogger Logger;

        public AliasService(IConfiguration config, ILogger<AliasService> logger) : base(config)
        {
            Logger = logger;

            #region known aliases
            logger.LogDebug("Loading known aliases...");
            
            var known = new Dictionary<string, string>();
            if (File.Exists(Path))
            {
                var json = File.ReadAllText(Path);
                var aliases = JsonSerializer.Deserialize<List<Alias>>(json);
                known = aliases.ToDictionary(x => x.Address, x => x.Name);

                logger.LogDebug($"Loaded {known.Count} items");
            }
            else
            {
                logger.LogInformation("Known aliases not found");
            }
            #endregion

            #region all addresses
            logger.LogDebug("Loading all addresses...");

            var sql = @"
                SELECT  ""Id"", ""Address""
                FROM    ""Accounts""";

            using var db = GetConnection();
            var items = db.Query(sql);

            Aliases = new Dictionary<int, Alias>((int)(items.Count() * 1.1));
            foreach (var item in items)
                Aliases.Add(item.Id, new Alias
                {
                    Address = item.Address,
                    Name = known.TryGetValue(item.Address, out string name) ? name : null
                });

            logger.LogDebug($"Loaded {Aliases.Count} items");
            #endregion
        }

        public Alias this[int id] => Aliases[id];

        public void Update(IEnumerable<(int Id, string Address)> accounts)
        {
            var added = 0;
            foreach (var account in accounts)
            {
                if (!Aliases.ContainsKey(account.Id))
                {
                    Aliases.Add(account.Id, new Alias { Address = account.Address, });
                    added++;
                }
            }
            Logger.LogDebug($"Added {added} aliases");
        }
    }

    public static class AliasServiceExt
    {
        public static void AddAliases(this IServiceCollection services)
        {
            services.AddSingleton<AliasService>();
        }
    }
}
