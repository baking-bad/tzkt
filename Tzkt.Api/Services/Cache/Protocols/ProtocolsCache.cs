using Dapper;
using Npgsql;
using Tzkt.Data.Models;

namespace Tzkt.Api.Services.Cache
{
    public class ProtocolsCache
    {
        public Protocol Current => Protocols[^1];

        List<Protocol> Protocols;
        readonly NpgsqlDataSource DataSource;
        readonly StateCache State;
        readonly ILogger Logger;

        public ProtocolsCache(NpgsqlDataSource dataSource, StateCache state, ILogger<ProtocolsCache> logger)
        {
            DataSource = dataSource;
            State = state;
            Logger = logger;

            Logger.LogDebug("Initializing protocols cache...");
            InitCache();
            Logger.LogInformation("Loaded {cnt} of {total} protocols", Protocols!.Count, state.Current.ProtocolsCount);
        }

        public Task UpdateAsync()
        {
            Logger.LogDebug("Updating protocols cache...");
            if (State.Reorganized && Protocols.Any(x => x.FirstLevel > State.ValidLevel) || State.Current.ProtocolsCount != Protocols.Count)
            {
                InitCache();
                Logger.LogDebug("{cnt} protocols updated", Protocols.Count);
            }
            else
            {
                Logger.LogDebug("No changes");
            }
            return Task.CompletedTask;
        }

        public Protocol FindByCycle(int cycle) => Protocols.Last(x => x.FirstCycle <= cycle);
        public Protocol FindByLevel(int level) => Protocols.Last(x => x.FirstLevel <= level);

        void InitCache()
        {
            using var db = DataSource.OpenConnection();
            Protocols = db.Query<Protocol>(@"SELECT * FROM ""Protocols"" ORDER BY ""Code""").ToList();
        }
    }
}
