using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto13
{
    partial class ProtoActivator : Proto12.ProtoActivator
    {
        public ProtoActivator(ProtocolHandler proto) : base(proto) { }

        protected override void SetParameters(Protocol protocol, JToken parameters)
        {
            base.SetParameters(protocol, parameters);
            protocol.LBToggleThreshold = parameters["liquidity_baking_toggle_ema_threshold"]?.Value<int>() ?? 1_000_000_000;
        }

        protected override void UpgradeParameters(Protocol protocol, Protocol prev)
        {
            protocol.LBToggleThreshold = 1_000_000_000;
        }

        protected override long GetVotingPower(Delegate baker, Protocol protocol)
        {
            return baker.StakingBalance;
        }

        protected override Task MigrateContext(AppState state) => Task.CompletedTask;
        protected override Task RevertContext(AppState state) => Task.CompletedTask;
    }
}
