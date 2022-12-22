using System.Threading.Tasks;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto11
{
    class ProtoActivator : Proto10.ProtoActivator
    {
        public ProtoActivator(ProtocolHandler proto) : base(proto) { }

        protected override void UpgradeParameters(Protocol protocol, Protocol prev) { }
        protected override Task MigrateContext(AppState state) => Task.CompletedTask;
        protected override Task RevertContext(AppState state) => Task.CompletedTask;
    }
}
