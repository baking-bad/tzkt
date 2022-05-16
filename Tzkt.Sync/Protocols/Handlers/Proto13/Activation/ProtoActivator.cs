using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Netezos.Encoding;
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
            protocol.BlocksPerVoting = (parameters["cycles_per_voting_period"]?.Value<int>() ?? 5) * protocol.BlocksPerCycle;
        }

        protected override void UpgradeParameters(Protocol protocol, Protocol prev)
        {
            protocol.LBToggleThreshold = 1_000_000_000;
        }

        protected override long GetVotingPower(Delegate baker, Protocol protocol)
        {
            return baker.StakingBalance;
        }

        protected override Sampler GetSampler(IEnumerable<(int id, long stake)> selection)
        {
            var sorted = selection.OrderByDescending(x =>
                Base58.Parse(Cache.Accounts.GetDelegate(x.id).Address), new BytesComparer());

            return new Sampler(sorted.Select(x => x.id).ToArray(), sorted.Select(x => x.stake).ToArray());
        }

        protected override async Task MigrateContext(AppState state)
        {
            var nextProto = await Cache.Protocols.GetAsync(state.NextProtocol);

            #region voting snapshots
            await Db.Database.ExecuteSqlRawAsync($@"
                DELETE FROM ""VotingSnapshots"" WHERE ""Period"" = {state.VotingPeriod}");

            Db.VotingSnapshots.RemoveRange(Db.ChangeTracker.Entries()
                .Where(x => x.Entity is VotingSnapshot)
                .Select(x => x.Entity as VotingSnapshot));

            var snapshots = Cache.Accounts.GetDelegates()
                .Where(x => x.Staked && x.StakingBalance >= nextProto.TokensPerRoll)
                .Select(x => new VotingSnapshot
                {
                    Level = state.Level,
                    Period = state.VotingPeriod,
                    BakerId = x.Id,
                    VotingPower = x.StakingBalance,
                    Status = VoterStatus.None
                });

            var period = await Cache.Periods.GetAsync(state.VotingPeriod);
            Db.TryAttach(period);

            period.TotalBakers = snapshots.Count();
            period.TotalVotingPower = snapshots.Sum(x => x.VotingPower);

            Db.VotingSnapshots.AddRange(snapshots);
            #endregion
        }
        protected override Task RevertContext(AppState state) => Task.CompletedTask;
    }
}
