using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace Tzkt.Api.Websocket
{
    static class IClientProxyExtension
    {
        public static Task SendState(this IClientProxy client, string method, object state)
            => client.SendAsync(method, new StateMessage(state));

        public static Task SendData(this IClientProxy client, string method, object data, object state)
            => client.SendAsync(method, new DataMessage(data, state));

        public static Task SendReorg(this IClientProxy client, string method, object state)
            => client.SendAsync(method, new ReorgMessage(state));
    }
}