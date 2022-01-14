using Newtonsoft.Json.Linq;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto12
{
    class ProtoActivator : Proto11.ProtoActivator
    {
        public ProtoActivator(ProtocolHandler proto) : base(proto) { }

        protected override void SetParameters(Protocol protocol, JToken parameters)
        {
            base.SetParameters(protocol, parameters);
            protocol.LBSunsetLevel = parameters["liquidity_baking_sunset_level"]?.Value<int>() ?? 2_244_609;
        }

        protected override void UpgradeParameters(Protocol protocol, Protocol prev)
        {
            if (protocol.LBSunsetLevel == 2_032_928)
                protocol.LBSunsetLevel = 2_244_609;
        }
    }
}
