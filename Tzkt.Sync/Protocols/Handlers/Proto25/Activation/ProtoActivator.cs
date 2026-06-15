using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto25
{
    partial class ProtoActivator(ProtocolHandler proto) : Proto24.ProtoActivator(proto)
    {
        protected override void UpgradeParameters(Protocol protocol, Protocol prev) { }

        protected override Task MigrateContext(AppState state) => Task.CompletedTask;

        protected override Task RevertContext(AppState state) => Task.CompletedTask;
    }
}
