using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Dapper;
using Tzkt.Data.Models;

namespace Tzkt.Api.Services.Cache
{
    public class ProtocolsCache : DbConnection
    {
        public Protocol Current => Protocols[^1];

        List<Protocol> Protocols;
        readonly StateCache State;
        readonly ILogger Logger;

        public ProtocolsCache(StateCache state, IConfiguration config, ILogger<ProtocolsCache> logger) : base(config)
        {
            State = state;
            Logger = logger;

            Logger.LogDebug("Initializing protocols cache...");
            InitCache();
            Logger.LogInformation("Loaded {1} of {2} protocols", Protocols.Count, state.Current.ProtocolsCount);
        }

        public Task UpdateAsync()
        {
            Logger.LogDebug("Updating protocols cache...");
            if (State.Reorganized && Protocols.Any(x => x.FirstLevel > State.ValidLevel) || State.Current.ProtocolsCount != Protocols.Count)
            {
                InitCache();
                Logger.LogDebug("{1} protocols updated", Protocols.Count);
            }
            else
            {
                Logger.LogDebug("No changes");
            }
            return Task.CompletedTask;
        }

        void InitCache()
        {
            using var db = GetConnection();
            Protocols = db.Query<Protocol>(@"SELECT * FROM ""Protocols"" ORDER BY ""Code""").ToList();
        }
    }
}
