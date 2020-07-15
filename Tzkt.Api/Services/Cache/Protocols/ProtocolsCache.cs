using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Dapper;

using Tzkt.Data.Models;

namespace Tzkt.Api.Services.Cache
{
    public class ProtocolsCache : DbConnection
    {
        List<Protocol> Protocols;
        readonly ILogger Logger;

        public ProtocolsCache(IConfiguration config, ILogger<ProtocolsCache> logger) : base(config)
        {
            Logger = logger;

            Logger.LogDebug("Initializing protocols cache...");

            using var db = GetConnection();
            Protocols = db.Query<Protocol>(@"SELECT * FROM ""Protocols"" ORDER BY ""Code""").ToList();

            Logger.LogDebug($"Protocols cache initialized with {Protocols.Count} items");
        }

        public Protocol Current => Protocols[^1];

        public async Task UpdateAsync(RawState state)
        {
            if (state.Protocol != Current.Hash)
            {
                using var db = GetConnection();
                Protocols = (await db.QueryAsync<Protocol>(@"SELECT * FROM ""Protocols"" ORDER BY ""Code""")).ToList();
            }
        }
    }

    public static class ProtocolsCacheExt
    {
        public static void AddProtocolsCache(this IServiceCollection services)
        {
            services.AddSingleton<ProtocolsCache>();
        }
    }
}
