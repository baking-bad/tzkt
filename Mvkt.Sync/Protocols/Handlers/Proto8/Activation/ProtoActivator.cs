using Newtonsoft.Json.Linq;
using Mvkt.Data.Models;

namespace Mvkt.Sync.Protocols.Proto8
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
            protocol.BlocksPerVoting = 20_480;
        }

        protected override async Task MigrateContext(AppState state)
        {

            var prevPeriod = await Cache.Periods.GetAsync(state.VotingPeriod - 1);
            Db.TryAttach(prevPeriod);
            prevPeriod.LastLevel -= 1;

            var newPeriod = await Cache.Periods.GetAsync(state.VotingPeriod);
            Db.TryAttach(newPeriod);
            newPeriod.FirstLevel -= 1;
            newPeriod.LastLevel = newPeriod.FirstLevel + 20_479;
        }
    }
}
