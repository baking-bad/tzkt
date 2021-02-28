using Newtonsoft.Json.Linq;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto8
{
    class ProtoActivator : Proto7.ProtoActivator
    {
        public ProtoActivator(ProtocolHandler proto) : base(proto) { }

        protected override void SetParameters(Protocol protocol, JToken parameters)
        {
            base.SetParameters(protocol, parameters);
            protocol.BlocksPerVoting = parameters["blocks_per_voting_period"]?.Value<int>() ?? 20_480;
        }

        protected override void UpgradeParameters(Protocol protocol, Protocol prev)
        {
            base.UpgradeParameters(protocol, prev);
            protocol.BlocksPerVoting = 20_480;
        }
    }
}
