using Newtonsoft.Json.Linq;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto16
{
    partial class ProtoActivator : Proto15.ProtoActivator
    {
        public ProtoActivator(ProtocolHandler proto) : base(proto) { }

        protected override void SetParameters(Protocol protocol, JToken parameters)
        {
            base.SetParameters(protocol, parameters);
        }

        protected override void UpgradeParameters(Protocol protocol, Protocol prev)
        {

        }

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
