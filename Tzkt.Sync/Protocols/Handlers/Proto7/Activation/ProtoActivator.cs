using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto7
{
    class ProtoActivator : Proto6.ProtoActivator
    {
        public ProtoActivator(ProtocolHandler proto) : base(proto) { }

        protected override void SetParameters(Protocol protocol, JToken parameters)
        {
            base.SetParameters(protocol, parameters);
            protocol.ByteCost = parameters["cost_per_byte"]?.Value<int>() ?? 250;
        }

        protected override void UpgradeParameters(Protocol protocol, Protocol prev)
        {
            protocol.ByteCost = 250;
        }

        protected override Task MigrateContext(AppState state) => Task.CompletedTask;
        protected override Task RevertContext(AppState state) => Task.CompletedTask;
    }
}
