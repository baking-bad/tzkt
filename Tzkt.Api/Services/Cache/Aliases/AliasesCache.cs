using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
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
        SELECT ""Address"", ""Extras""#>>'{profile,alias}' AS ""Name""
        FROM   ""Accounts""";
        #endregion

        public List<Alias> Aliases { get; }
        public Dictionary<string, List<Alias>> Dictionary = new();

        public AliasesCache(IConfiguration config, ILogger<AliasesCache> logger) : base(config)
        {
            using var db = GetConnection();
            Aliases = db.Query<Alias>(
                $@"{SelectQuery} WHERE ""Extras""@>'{{""profile"":{{}}}}' AND ""Extras""#>>'{{profile,alias}}' IS NOT NULL")
                .ToList();

            logger.LogInformation("Loaded {cnt} aliases", Aliases.Count);

            foreach (var alias in Aliases)
                AddTrigrams(alias);
        }

        public void OnExtrasUpdate(string address, string json)
        {
            string name = null;
            if (json != null)
            {
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("profile", out var profile) && 
                    profile.TryGetProperty("alias", out var alias))
                    name = alias.GetString();
            }

            lock (this)
            {
                if (name != null)
                {
                    var alias = Aliases.FirstOrDefault(x => x.Address == address);
                    if (alias == null)
                    {
                        var newAlias = new Alias { Address = address, Name = name };
                        Aliases.Add(newAlias);
                        AddTrigrams(newAlias);
                    }
                    else
                    {
                        RemoveTrigrams(alias);
                        alias.Name = name;
                        AddTrigrams(alias);
                    }
                }
                else
                {
                    foreach (var alias in Aliases.Where(x => x.Address == address))
                        RemoveTrigrams(alias);

                    Aliases.RemoveAll(x => x.Address == address);
                }
            }
        }

        public IEnumerable<Alias> Search(string search, int limit)
        {
            search = search.Trim().ToLower();

            if (search.Length < 3)
                return SimpleSearch(search, limit);

            var candidates = new Dictionary<Alias, int>();

            lock (this)
            {
                foreach (var trigram in GetTrigrams(search))
                {
                    if (Dictionary.TryGetValue(trigram, out var aliases))
                    {
                        foreach (var alias in aliases)
                        {
                            if (!candidates.TryAdd(alias, 1))
                                candidates[alias] += 1;
                        }
                    }
                }

                if (Dictionary.TryGetValue($" {search[..2]}", out var aliases2))
                {
                    foreach (var alias in aliases2)
                    {
                        if (alias.Name.ToLower() == search)
                            candidates[alias] += 1000;
                    }
                }
            }

            return candidates.OrderByDescending(x => x.Value)
                .ThenBy(x => x.Key.Name)
                .Select(x => x.Key)
                .Take(limit);
        }

        public IEnumerable<Alias> SimpleSearch(string search, int limit)
        {
            var res = new List<(Alias alias, int priority)>();

            lock (this)
            {
                foreach (var item in Aliases)
                {
                    var name = item.Name.ToLower();

                    if (name == search)
                        res.Add((item, 0));
                    else if (name.StartsWith(search))
                        res.Add((item, 1));
                    else if (name.Contains(search))
                        res.Add((item, 2));
                }
            }

            return res.OrderBy(x => x.priority)
                .ThenBy(x => x.alias.Name)
                .Select(x => x.alias)
                .Take(limit);
        }

        void AddTrigrams(Alias alias)
        {
            foreach (var trigram in GetTrigrams(alias.Name.ToLower()))
            {
                if (!Dictionary.TryGetValue(trigram, out var list))
                {
                    list = new();
                    Dictionary.Add(trigram, list);
                }
                list.Add(alias);
            }
        }

        void RemoveTrigrams(Alias alias)
        {
            foreach (var trigram in GetTrigrams(alias.Name.ToLower()))
            {
                if (Dictionary.TryGetValue(trigram, out var list))
                {
                    if (list.Remove(alias) && list.Count == 0)
                        Dictionary.Remove(trigram);
                }
            }
        }

        static HashSet<string> GetTrigrams(string s)
        {
            var res = new HashSet<string>();
            for (int i = 0; i < s.Length - 2; i++)
                res.Add($"{s[i]}{s[i + 1]}{s[i + 2]}");
            res.Add($" {s[0]}{s[1]}");
            res.Add($"{s[^2]}{s[^1]} ");
            return res;
        }
    }
}
