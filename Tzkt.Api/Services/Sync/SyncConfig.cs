using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace Tzkt.Api.Services.Sync
{
    public class SyncConfig
    {
        public int CheckInterval { get; set; } = 20;
        public int UpdateInterval { get; set; } = 1;
    }

    public static class SyncConfigExt
    {
        public static SyncConfig GetSyncConfig(this IConfiguration config)
        {
            return config.GetSection("Sync")?.Get<SyncConfig>() ?? new SyncConfig();
        }
    }
}
