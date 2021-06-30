using System.Threading.Tasks;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto10
{
    class ProtoActivator : Proto9.ProtoActivator
    {
        public ProtoActivator(ProtocolHandler proto) : base(proto) { }

        protected override Task MigrateContext(AppState state)
        {
            return Task.CompletedTask;
        }

        protected override Task RevertContext(AppState state)
        {
            return Task.CompletedTask;
        }
    }
}
