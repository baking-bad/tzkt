using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto24
{
    partial class ProtoActivator(ProtocolHandler proto) : Proto23.ProtoActivator(proto)
    {
        protected override void SetParameters(Protocol protocol, JToken parameters)
        {
            base.SetParameters(protocol, parameters);
        }

        protected override void UpgradeParameters(Protocol protocol, Protocol prev)
        {
            base.UpgradeParameters(protocol, prev);
        }

        protected override async Task MigrateContext(AppState state)
        {
            await base.MigrateContext(state);
        }

        protected override Task RevertContext(AppState state) => base.RevertContext(state);
    }
}