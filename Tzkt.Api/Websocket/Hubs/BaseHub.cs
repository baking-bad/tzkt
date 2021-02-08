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
                Logger.LogWarning("Connections limit exceeded. Client {0} dropped", Context.ConnectionId);
                throw new HubException("Connections limit exceeded");
            }

            if (string.IsNullOrEmpty(Context.ConnectionId))
            {
                Logger.LogCritical("Invalid connection ID: {0}", Context.ConnectionId);
                throw new HubException("Invalid connection ID");
            }

            lock (Crit)
            {
                Connections++;
                Logger.LogDebug("Client {0} connected. Total connections: {1}", Context.ConnectionId, Connections);
            }
            
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            lock (Crit)
            {
                Connections--;
                Logger.LogDebug("Client {0} disconnected: {1}. Total connections: {2}",
                    Context.ConnectionId, exception?.Message ?? string.Empty, Connections);
            }

            return base.OnDisconnectedAsync(exception);
        }
    }
}