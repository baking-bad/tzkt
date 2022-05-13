﻿using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
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

        protected override async Task MigrateContext(AppState state)
        {
            var nextProto = await Cache.Protocols.GetAsync(state.NextProtocol);

            #region voting snapshots
            await Db.Database.ExecuteSqlRawAsync($@"
                DELETE FROM ""VotingSnapshots"" WHERE ""Period"" = {state.VotingPeriod}");

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
