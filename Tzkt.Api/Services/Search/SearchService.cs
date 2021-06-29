using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Dapper;
using Tzkt.Api.Models;

namespace Tzkt.Api.Services
{
    public class SearchService : DbConnection
    {
       public List<Alias> Aliases { get; }

        public SearchService(IConfiguration config, ILogger<SearchService> logger) : base(config)
        {
            using var db = GetConnection();
            Aliases = db.Query<Alias>(@"
                SELECT ""Address"", ""Metadata""#>>'{alias}' AS ""Name""
                FROM ""Accounts""
                WHERE ""Metadata""@>'{}'")
                .ToList();

            logger.LogInformation("Loaded {1} search entries", Aliases.Count);
        }

        public IEnumerable<Alias> Find(string searchString)
        {
            var search = searchString.ToLower();
            var res = new List<(Alias, int)>();
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
            return res.OrderBy(x => x.Item2)
                .ThenBy(x => x.Item1.Name)
                .Select(x => x.Item1)
                .Take(10);
        }
    }
}
