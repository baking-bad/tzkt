using System.Text.Json;
using System.Threading.Tasks;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto1
{
    partial class ProtoActivator : ProtocolCommit
    {
        public ProtoActivator(ProtocolHandler protocol) : base(protocol) { }

        public async Task Activate(AppState state, JsonElement rawBlock)
        {
            if (state.Level == 1) // bootstrap
            {
                var (protocol, parameters) = BootstrapProtocol(rawBlock);

                var accounts = await BootstrapAccounts(protocol);
                var (bakingRights, endorsingRights) = await BootstrapBakingRights(protocol, accounts);
                await BootstrapCycles(protocol, accounts);
                BootstrapDelegatorCycles(protocol, accounts);
                BootstrapBakerCycles(protocol, accounts, bakingRights, endorsingRights);
                BootstrapSnapshotBalances(accounts);
                BootstrapVoting(protocol);
                await BootstrapCommitments(parameters);
            }
            else // upgrade
            {
                await UpgradeProtocol(state);
                await MigrateContext(state);
            }
        }

        public async Task Deactivate(AppState state)
        {
            if (state.Level == 1) // clear
            {
                await ClearCommitments();
                await ClearVoting();
                await ClearSnapshotBalances();
                await ClearBakerCycles();
                await ClearDelegatorCycles();
                await ClearCycles();
                await ClearBakingRights();
                await ClearAccounts();
                await ClearProtocol();
            }
            else // downgrade
            {
                await RevertContext(state);
                await DowngradeProtocol(state);
            }
        }

        protected virtual Task MigrateContext(AppState state) => Task.CompletedTask;
        protected virtual Task RevertContext(AppState state) => Task.CompletedTask;

        public override Task Apply() => Task.CompletedTask;
        public override Task Revert() => Task.CompletedTask;
    }
}
