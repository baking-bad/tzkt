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
    public class ProtocolMetadataService : DbConnection
    {
        public List<ProtocolMetadataAlias> Aliases { get; private set; }

        readonly Dictionary<string, ProtocolMetadata> Metadata;
        readonly MetadataConfig Config;
        readonly ILogger Logger;

        public ProtocolMetadataService(IConfiguration config, ILogger<ProtocolMetadataService> logger) : base(config)
        {
            Config = config.GetMetadataConfig();
            Logger = logger;

            if (!File.Exists(Config.ProtocolsPath))
            {
                Aliases = new List<ProtocolMetadataAlias>();
                Metadata = new Dictionary<string, ProtocolMetadata>();
                Logger.LogInformation("Protocols metadata not found");
                return;
            }

            Logger.LogDebug("Loading protocols metadata...");

            var json = File.ReadAllText(Config.ProtocolsPath);
            var protocols = JsonSerializer.Deserialize<List<ProtocolMetadata>>(json);

            Metadata = protocols.ToDictionary(x => x.Hash);
            Aliases = new List<ProtocolMetadataAlias>(Metadata.Count);

            foreach (var meta in Metadata.Values)
            {
                Aliases.Add(new ProtocolMetadataAlias
                {
                    Hash = meta.Hash,
                    Alias = meta.Alias
                });

                meta.Hash = null;
            }

            Logger.LogDebug($"Loaded {Metadata.Count} protocols metadata");
        }

        public ProtocolMetadata this[string hash] => Metadata.TryGetValue(hash, out var meta) ? meta : null;
    }

    public static class ProtocolMetadataServiceExt
    {
        public static void AddProtocolMetadata(this IServiceCollection services)
        {
            services.AddSingleton<ProtocolMetadataService>();
        }
    }
}
