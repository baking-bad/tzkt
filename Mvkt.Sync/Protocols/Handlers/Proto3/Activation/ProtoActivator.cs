using Newtonsoft.Json.Linq;
using Mvkt.Data.Models;

namespace Mvkt.Sync.Protocols.Proto3
{
    class ProtoActivator : Proto1.ProtoActivator
    {
        public ProtoActivator(ProtocolHandler proto) : base(proto) { }

        protected override void SetParameters(Protocol protocol, JToken parameters)
        {
            base.SetParameters(protocol, parameters);
            protocol.OriginationSize = parameters["origination_size"]?.Value<int>() ?? 257;
        }
    }
}
