using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

using Tezzycat.Services.Protocols;

namespace Tezzycat.Services
{
    public class TezosProtocols
    {
        private readonly Dictionary<string, IProtocolHandler> Handlers;

        public TezosProtocols(TezosNode node)
        {
            Handlers = new Dictionary<string, IProtocolHandler>
            {
                { "PrihK96nBAFSxVL1GLJTVhu9YnzkMFiBeuJRPA8NwuZVZCE1L6i", new GenesisHandler() },
                { "Ps9mPmXaRzmzk35gbAYNCAw6UXdE2qoABTHbN2oEEc1qM7CwT9P", new ProtoInitHandler() },
                { "PtCJ7pwoxe8JasnHY8YonnLYjcVHmhiARPJvqcC6VfHT5s8k8sY", new Proto1Handler() },
                { "PsYLVpVvgbLhAhoqAkMFUo6gudkJ9weNXhUYCiLDzcUpFpkk8Wt", new Proto2Handler() },
                { "PsddFKi32cMJ2qPjf43Qv5GDWLDPZb3T3bF6fLKiF5HtvHNU7aP", new Proto3Handler() }
            };
        }

        public IProtocolHandler GetProtocolHandler(string protocol)
            => Handlers.ContainsKey(protocol) ? Handlers[protocol] : Handlers.Last().Value;
    }

    public static class TezosProtocolsExt
    {
        public static void AddTezosProtocols(this IServiceCollection services)
        {
            services.AddSingleton<TezosProtocols>();
        }
    }
}
