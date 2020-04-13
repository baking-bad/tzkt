using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace Tzkt.Api.Services
{
    public class MetadataConfig
    {
        public string AccountsPath { get; set; }
        public string ProposalsPath { get; set; }
        public string ProtocolsPath { get; set; }
    }

    public static class MetadataConfigExt
    {
        public static MetadataConfig GetMetadataConfig(this IConfiguration config)
        {
            return config.GetSection("Metadata")?.Get<MetadataConfig>() ?? new MetadataConfig();
        }
    }
}
