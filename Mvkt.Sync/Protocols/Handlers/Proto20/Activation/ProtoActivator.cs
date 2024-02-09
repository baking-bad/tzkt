using Mvkt.Data.Models;

namespace Mvkt.Sync.Protocols.Proto20
{
    partial class ProtoActivator : Proto19.ProtoActivator
    {
        public ProtoActivator(ProtocolHandler proto) : base(proto) { }

        protected override void UpgradeParameters(Protocol protocol, Protocol prev)
        {
            // nothing to upgrade
        }

        protected override Task MigrateContext(AppState state)
        {
            // nothing to migrate
            return Task.CompletedTask;
        }

        protected override Task RevertContext(AppState state)
        {
            // nothing to revert
            return Task.CompletedTask;
        }
    }
}
