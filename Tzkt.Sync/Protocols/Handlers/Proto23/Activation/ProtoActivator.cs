using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto23
{
    partial class ProtoActivator(ProtocolHandler proto) : Proto21.ProtoActivator(proto)
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

        protected override async Task MigrateContext(AppState state)
        {
            #region unreveal tz4
            foreach (var account in await Db.Users.Where(x => x.Revealed && x.Address.StartsWith("tz4")).ToListAsync())
            {
                Cache.Accounts.Add(account);
                Db.TryAttach(account);
                account.Revealed = false;
            }
            #endregion
        }

        protected override Task RevertContext(AppState state) => Task.CompletedTask;
    }
}
