using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Netezos.Encoding;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto18
{
    partial class ProtoActivator : Proto17.ProtoActivator
    {
        public ProtoActivator(ProtocolHandler proto) : base(proto) { }

        protected override async Task MigrateContext(AppState state)
        {
            await RemoveDeadRefutationGames(state);
            await RemoveBigmapKeys(state);
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

        async Task RemoveBigmapKeys(AppState state)
        {
            var keyHashes = new HashSet<string>
            {
                "exprtXBtxJxCDEDETueKAFLL7r7vZtNEo1MHajpHba1djtGKqJzWd3",
                "exprtbuRhaGDS942BgZ1qFdD7HAKeBjPEqzRxgLQyWQ6HWxcaiLC2c",
                "exprtePxSLgrhJmTPZEePyFBmESLhaBUN1WodvLYy9xYhEYE6dKPLe",
                "exprtx9GaYz5Fy5ytiuYgSfJqeYqkxGgobust8U6dpCLaeZUMiitmg",
                "expru28t4XoyB61WuRQnExk3Kq8ssGv1ejgdo9XHxpTXoQjXTGw1Dg",
                "expru2fZALknjB4vJjmQBPkrs3dJZ5ytuzfmE9A7ScUk5opJiZQyiJ",
                "expru2riAFKURjHJ1vNpvsZGGw6z4wtTvbstXVuwQPj1MaTqKPeQ6z",
                "expruHoZDr8ioVhaAs495crYTprAYyC87CruEJ6HaS7diYV6qLARqQ",
                "expruMie2gfy5smMd81NtcvvWm4jD7ThUebw9hpF3N3apKVtxkVG9M",
                "expruc3QW7cdxrGurDJQa6k9QqMZjGkRDJahy2XNtBt9WQzC1yavJK",
                "exprud86wYL7inFCVHkF1Jcz8uMXVY7dnbzxVupyyknZjtDVmwoQTJ",
                "exprufYzeBTGn9733Ga8xEEmU4SsrSyDrzEip8V8hTBAG253T5zZQx",
                "exprum9tuHNvisMa3c372AFmCa27rmkbCGrhzMSprrxgJjzXhrKAag",
                "expruokt7oQ6dDHRvL4sURKUzfwJirR8FPHvpXwjgUD4KHhPWhDGbv",
                "expruom5ds2hVgjdTB877Fx3ZuWT5WUnw1H6kUZavVHcJFbCkcgo3x",
                "exprv2DPd1pV3GVSN2CgW7PPrAQUTuZAdeJphwToQrTNrxiJcWzvtX",
                "exprv65Czv5TnKyEWgBHjDztkCkc1FAVEPxZ3V3ocgvGjfXwjPLo8M",
                "exprv6S2KAvqAC18jDLYjaj1w9oc4ESdDGJkUZ63EpkqSTAz88cSYB",
                "exprvNg3VDBnhtTHvc75krAEYzz6vUMr3iU5jtLdxs83FbgTbZ9nFT",
                "exprvS7wNDHYKYZ19nj3ZUo7AAVMCDpTK3NNERFhqe5SJGCBL4pwFA"
            };

            var valueType = new MichelinePrim
            {
                Prim = PrimType.pair,
                Args = new(2)
                {
                    new MichelinePrim
                    {
                        Prim = PrimType.timestamp
                    },
                    new MichelinePrim
                    {
                        Prim = PrimType.ticket,
                        Args = new(1)
                        {
                            new MichelinePrim
                            {
                                Prim = PrimType.@string,
                                Annots = new(1)
                                {
                                    new FieldAnnotation("data")
                                }
                            }
                        }
                    }
                }
            };

            var bigmap = await Db.BigMaps
                .FirstOrDefaultAsync(x => x.Ptr == 5696);

            if (bigmap?.ValueType.IsEqual(valueType.ToBytes()) != true)
                return;

            var block = await Cache.Blocks.CurrentAsync();
            
            var contract = await Cache.Accounts.GetAsync(bigmap.ContractId);
            Db.TryAttach(contract);

            var keys = await Db.BigMapKeys
                .Where(x => x.BigMapPtr == bigmap.Ptr && x.Active && keyHashes.Contains(x.KeyHash))
                .ToListAsync();

            foreach (var key in keys)
            {
                var value = Micheline.FromBytes(key.RawValue);
                if (((((value as MichelinePrim).Args[1] as MichelinePrim).Args[1] as MichelinePrim).Args[1] as MichelineInt).Value == BigInteger.Zero)
                {
                    var migration = new MigrationOperation
                    {
                        Id = Cache.AppState.NextOperationId(),
                        Block = block,
                        Account = contract,
                        AccountId = contract.Id,
                        BigMapUpdates = 1,
                        Kind = MigrationKind.RemoveBigMapKey,
                        Level = block.Level,
                        Timestamp = block.Timestamp
                    };
                    Db.MigrationOps.Add(migration);

                    block.Operations |= Operations.Migrations;
                    contract.MigrationsCount++;
                    state.MigrationOpsCount++;

                    var update = new BigMapUpdate
                    {
                        Id = Cache.AppState.NextBigMapUpdateId(),
                        Action = BigMapAction.RemoveKey,
                        BigMapKeyId = key.Id,
                        BigMapPtr = bigmap.Ptr,
                        JsonValue = key.JsonValue,
                        Level = block.Level,
                        MigrationId = migration.Id,
                        RawValue = key.RawValue
                    };
                    Db.BigMapUpdates.Add(update);

                    key.Active = false;
                    key.LastLevel = block.Level;
                    key.Updates++;

                    bigmap.ActiveKeys--;
                    bigmap.LastLevel = block.Level;
                    bigmap.Updates++;
                }
            }
        }
    }
}
