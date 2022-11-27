using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Tzkt.Api.Websocket.Hubs
{
    public abstract class BaseHub : Hub
    {
        static readonly object Crit = new ();
        static int Connections = 0;

        readonly ILogger Logger;
        readonly WebsocketConfig Config;

        protected BaseHub(ILogger logger, IConfiguration config)
        {
            Logger = logger;
            Config = config.GetWebsocketConfig();
        }

        public override Task OnConnectedAsync()
        {
            if (Connections >= Config.MaxConnections)
            {
                Logger.LogWarning("Connections limit exceeded. Client {id} dropped", Context.ConnectionId);
                throw new HubException("Connections limit exceeded");
            }

            if (string.IsNullOrEmpty(Context.ConnectionId))
            {
                Logger.LogCritical("Invalid connection ID: {id}", Context.ConnectionId);
                throw new HubException("Invalid connection ID");
            }

            lock (Crit)
            {
                Connections++;
                Logger.LogDebug("Client {id} connected. Total connections: {cnd}", Context.ConnectionId, Connections);
            }
            
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            lock (Crit)
            {
                Connections--;
                Logger.LogDebug("Client {id} disconnected: {ex}. Total connections: {cnt}",
                    Context.ConnectionId, exception?.Message ?? string.Empty, Connections);
            }

            return base.OnDisconnectedAsync(exception);
        }
    }
}