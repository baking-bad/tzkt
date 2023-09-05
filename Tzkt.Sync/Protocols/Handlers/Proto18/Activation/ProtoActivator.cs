using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto18
{
    partial class ProtoActivator : Proto17.ProtoActivator
    {
        public ProtoActivator(ProtocolHandler proto) : base(proto) { }

        protected override async Task MigrateContext(AppState state)
        {
            await RemoveDeadRefutationGames(state);
        }

        protected async Task RemoveDeadRefutationGames(AppState state)
        {
            var activeGames = await Db.RefutationGames
                .AsNoTracking()
                .Where(x =>
                    x.InitiatorReward == null &&
                    x.InitiatorLoss == null &&
                    x.OpponentReward == null &&
                    x.OpponentLoss == null)
                .ToListAsync();

            foreach (var game in activeGames)
            {
                var initiatorBond = await Db.SmartRollupPublishOps
                    .AsNoTracking()
                    .Where(x =>
                        x.SmartRollupId == game.SmartRollupId &&
                        x.BondStatus == SmartRollupBondStatus.Active &&
                        x.SenderId == game.InitiatorId)
                    .FirstOrDefaultAsync();

                if (initiatorBond != null)
                    continue;

                var opponentBond = await Db.SmartRollupPublishOps
                    .AsNoTracking()
                    .Where(x =>
                        x.SmartRollupId == game.SmartRollupId &&
                        x.BondStatus == SmartRollupBondStatus.Active &&
                        x.SenderId == game.OpponentId)
                    .FirstOrDefaultAsync();

                if (opponentBond != null)
                    continue;

                Db.TryAttach(game);
                game.LastLevel = state.Level;
                game.InitiatorReward = 0;
                game.InitiatorLoss = 0;
                game.OpponentReward = 0;
                game.OpponentLoss = 0;

                var initiator = await Cache.Accounts.GetAsync(game.InitiatorId);
                Db.TryAttach(initiator);
                initiator.ActiveRefutationGamesCount--;

                var opponent = await Cache.Accounts.GetAsync(game.OpponentId);
                Db.TryAttach(opponent);
                opponent.ActiveRefutationGamesCount--;

                var rollup = await Cache.Accounts.GetAsync(game.SmartRollupId);
                Db.TryAttach(rollup);
                rollup.ActiveRefutationGamesCount--;
            }
        }
    }
}
