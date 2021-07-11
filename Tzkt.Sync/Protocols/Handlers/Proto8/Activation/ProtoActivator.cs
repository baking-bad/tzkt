using System.Linq;
using System.Threading.Tasks;
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
            protocol.BlocksPerVoting = 20_480;
        }

        protected override Task MigrateContext(AppState state)
        {
            var prevPeriod = Db.ChangeTracker
                .Entries()
                .First(x => x.Entity is VotingPeriod p && p.Index == state.VotingPeriod - 1)
                .Entity as VotingPeriod;

            var newPeriod = Db.ChangeTracker
                .Entries()
                .First(x => x.Entity is VotingPeriod p && p.Index == state.VotingPeriod)
                .Entity as VotingPeriod;

            prevPeriod.LastLevel -= 1;
            newPeriod.FirstLevel -= 1;
            newPeriod.LastLevel = newPeriod.FirstLevel + 20_479;
            return Task.CompletedTask;
        }
    }
}
