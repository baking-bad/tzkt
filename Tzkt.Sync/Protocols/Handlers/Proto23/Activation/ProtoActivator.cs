using Newtonsoft.Json.Linq;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto23
{
    partial class ProtoActivator(ProtocolHandler proto) : Proto21.ProtoActivator(proto)
    {
        protected override void SetParameters(Protocol protocol, JToken parameters)
        {
            base.SetParameters(protocol, parameters);
        }

        protected override void UpgradeParameters(Protocol protocol, Protocol prev)
        {
        }

        protected override async Task MigrateContext(AppState state)
        {
        }

        protected override async Task RevertContext(AppState state)
        {
        }
    }
}
