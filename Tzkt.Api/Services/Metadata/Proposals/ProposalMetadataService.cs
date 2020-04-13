using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Tzkt.Api.Services.Metadata
{
    public class ProposalMetadataService : DbConnection
    {
        public List<ProposalMetadataAlias> Aliases { get; private set; }

        readonly Dictionary<string, ProposalMetadata> Metadata;
        readonly MetadataConfig Config;
        readonly ILogger Logger;

        public ProposalMetadataService(IConfiguration config, ILogger<ProposalMetadataService> logger) : base(config)
        {
            Config = config.GetMetadataConfig();
            Logger = logger;

            if (!File.Exists(Config.ProposalsPath))
            {
                Aliases = new List<ProposalMetadataAlias>();
                Metadata = new Dictionary<string, ProposalMetadata>();
                Logger.LogInformation("Proposals metadata not found");
                return;
            }

            Logger.LogDebug("Loading proposals metadata...");

            var json = File.ReadAllText(Config.ProposalsPath);
            var proposals = JsonSerializer.Deserialize<List<ProposalMetadata>>(json);

            Metadata = proposals.ToDictionary(x => x.Hash);
            Aliases = new List<ProposalMetadataAlias>(Metadata.Count);

            foreach (var meta in Metadata.Values)
            {
                Aliases.Add(new ProposalMetadataAlias
                {
                    Hash = meta.Hash,
                    Alias = meta.Alias
                });

                meta.Hash = null;
            }

            Logger.LogDebug($"Loaded {Metadata.Count} proposals metadata");
        }

        public ProposalMetadata this[string hash] => Metadata.TryGetValue(hash, out var meta) ? meta : null;
    }

    public static class ProposalMetadataServiceExt
    {
        public static void AddProposalMetadata(this IServiceCollection services)
        {
            services.AddSingleton<ProposalMetadataService>();
        }
    }
}
