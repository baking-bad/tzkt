using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto23
{
    partial class ProtoActivator(ProtocolHandler proto) : Proto22.ProtoActivator(proto)
    {
        protected override void SetParameters(Protocol protocol, JToken parameters)
        {
            base.SetParameters(protocol, parameters);
        }

        protected override void UpgradeParameters(Protocol protocol, Protocol prev)
        {
            if (prev.BlocksPerCycle == 10800 && prev.BlocksPerCommitment == 240)
                protocol.BlocksPerCommitment = 84;
        }

        protected override async Task ActivateContext(AppState state)
        {
            await base.ActivateContext(state);
            Cache.AppState.Get().AiActivationLevel = 1;
            UpdateBakersPower();
        }

        protected override async Task MigrateContext(AppState state)
        {
            var prevProto = await Cache.Protocols.GetAsync(state.Protocol);
            var nextProto = await Cache.Protocols.GetAsync(state.NextProtocol);

            #region unreveal tz4
            foreach (var account in await Db.Users.Where(x => x.Revealed && x.Address.StartsWith("tz4")).ToListAsync())
            {
                Cache.Accounts.Add(account);
                Db.TryAttach(account);
                account.Revealed = false;
            }
            #endregion

            #region update revelation rewards
            if (prevProto.BlocksPerCommitment != nextProto.BlocksPerCommitment)
            {
                foreach (var cycle in await Db.Cycles.Where(x => x.Index > state.Cycle).ToListAsync())
                {
                    cycle.NonceRevelationReward = cycle.NonceRevelationReward * nextProto.BlocksPerCommitment / prevProto.BlocksPerCommitment;
                    cycle.VdfRevelationReward= cycle.VdfRevelationReward * nextProto.BlocksPerCommitment / prevProto.BlocksPerCommitment;
                }
            }
            #endregion
        }

        protected override Task RevertContext(AppState state) => throw new NotImplementedException();
    }
}
