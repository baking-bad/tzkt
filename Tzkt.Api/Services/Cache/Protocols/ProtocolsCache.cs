using System.Collections.Generic;
using System.Linq;
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

        readonly StateCache State;
        readonly ILogger Logger;

        public ProtocolsCache(StateCache state, IConfiguration config, ILogger<ProtocolsCache> logger) : base(config)
        {
            logger.LogDebug("Initializing protocols cache...");

            State = state;
            Logger = logger;

            using var db = GetConnection();
            Protocols = db.Query<Protocol>(@"SELECT * FROM ""Protocols"" ORDER BY ""Code""").ToList();

            logger.LogInformation("Loaded {1} of {2} protocols", Protocols.Count, state.Current.ProtocolsCount);
        }

        public Protocol Current => Protocols[^1];

        public async Task UpdateAsync()
        {
            Logger.LogDebug("Updating protocols cache...");
            if (State.Reorganized && Protocols.Any(x => x.FirstLevel > State.ValidLevel) || State.Current.ProtocolsCount != Protocols.Count)
            {
                using var db = GetConnection();
                Protocols = (await db.QueryAsync<Protocol>(@"SELECT * FROM ""Protocols"" ORDER BY ""Code""")).ToList();
                Logger.LogDebug("{1} protocols updated", Protocols.Count);
            }
            else
            {
                Logger.LogDebug("No changes");
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
