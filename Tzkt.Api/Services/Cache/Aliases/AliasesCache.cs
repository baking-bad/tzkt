using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Dapper;
using Tzkt.Api.Models;

namespace Tzkt.Api.Services.Cache
{
    public class AliasesCache : DbConnection
    {
        #region static
        const string SelectQuery = @"
        SELECT ""Address"", ""Metadata""#>>'{alias}' AS ""Name""
        FROM   ""Accounts""";
        #endregion

        public List<Alias> Aliases { get; }

        public AliasesCache(IConfiguration config, ILogger<AliasesCache> logger) : base(config)
        {
            using var db = GetConnection();
            Aliases = db.Query<Alias>(
                $@"{SelectQuery} WHERE ""Metadata""@>'{{}}' AND ""Metadata""#>>'{{alias}}' IS NOT NULL")
                .ToList();
            logger.LogInformation("Loaded {1} aliases", Aliases.Count);
        }

        public void UpdateMetadata(string address, string json)
        {
            string name = null;
            if (json != null)
            {
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("alias", out var alias))
                    name = alias.GetString();
            }

            lock (this)
            {
                if (name != null)
                {
                    var alias = Aliases.FirstOrDefault(x => x.Address == address);
                    if (alias == null)
                        Aliases.Add(new Alias { Address = address, Name = name });
                    else
                        alias.Name = name;
                }
                else
                {
                    Aliases.RemoveAll(x => x.Address == address);
                }
            }
        }

        public IEnumerable<Alias> Search(string str)
        {
            var search = str.ToLower();
            var res = new List<(Alias alias, int priority)>();

            lock (this)
            {
                foreach (var item in Aliases)
                {
                    var alias = item.Name.ToLower();

                    if (alias == search)
                        res.Add((item, 0));
                    else if (alias.StartsWith(search))
                        res.Add((item, 1));
                    else if (alias.Contains(search))
                        res.Add((item, 2));
                }
            }

            return res.OrderBy(x => x.priority)
                .ThenBy(x => x.alias.Name)
                .Select(x => x.alias)
                .Take(10);
        }
    }
}
