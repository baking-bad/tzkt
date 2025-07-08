using System.Text.Json;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto1
{
    partial class ProtoActivator(ProtocolHandler protocol) : ProtocolCommit(protocol)
    {
        public async Task Activate(AppState state, JsonElement rawBlock)
        {
            if (state.Level == 1) // bootstrap
            {
                var (protocol, parameters) = BootstrapProtocol(rawBlock);

                var accounts = await BootstrapAccounts(protocol, parameters);
                var cycles = BootstrapCycles(protocol, accounts, parameters);
                var (bakingRights, attestationRights) = await BootstrapBakingRights(protocol, accounts, cycles);
                BootstrapDelegationSnapshots(accounts);
                BootstrapSnapshotBalances(accounts);
                BootstrapBakerCycles(protocol, accounts, cycles, bakingRights, attestationRights);
                BootstrapStakerCycles(protocol, accounts);
                BootstrapDelegatorCycles(protocol, accounts);
                BootstrapVoting(protocol, accounts);
                BootstrapCommitments(parameters);
                await ActivateContext(state);
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
                await DeactivateContext(state);
                await ClearCommitments();
                await ClearVoting();
                await ClearSnapshotBalances();
                await ClearDelegationSnapshots();
                await ClearBakerCycles();
                await ClearStakerCycles();
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

        protected virtual Task ActivateContext(AppState state) => Task.CompletedTask;
        protected virtual Task DeactivateContext(AppState state) => Task.CompletedTask;
        protected virtual Task MigrateContext(AppState state) => Task.CompletedTask;
        protected virtual Task RevertContext(AppState state) => Task.CompletedTask;
    }
}
