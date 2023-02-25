using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models;
using Tzkt.Data.Models.Base;

namespace Tzkt.Sync.Protocols.Proto16
{
    class SmartRollupTimeoutCommit : ProtocolCommit
    {
        public SmartRollupTimeoutCommit(ProtocolHandler protocol) : base(protocol) { }

        public virtual async Task Apply(Block block, JsonElement op, JsonElement content)
        {
            #region init
            var sender = await Cache.Accounts.GetAsync(content.RequiredString("source"));
            sender.Delegate ??= Cache.Accounts.GetDelegate(sender.DelegateId);
            var rollup = await Cache.Accounts.GetAsync(content.RequiredString("rollup")) as SmartRollup;

            var aliceId = await Cache.Accounts.GetIdOrDefaultAsync(content.Required("stakers").RequiredString("alice"));
            var bobId = await Cache.Accounts.GetIdOrDefaultAsync(content.Required("stakers").RequiredString("bob"));
            var game = await Cache.RefutationGames.GetOrDefaultAsync(rollup?.Id, aliceId, bobId);

            var result = content.Required("metadata").Required("operation_result");

            var operation = new SmartRollupRefuteOperation
            {
                Id = Cache.AppState.NextOperationId(),
                Block = block,
                Level = block.Level,
                Timestamp = block.Timestamp,
                OpHash = op.RequiredString("hash"),
                BakerFee = content.RequiredInt64("fee"),
                Counter = content.RequiredInt32("counter"),
                GasLimit = content.RequiredInt32("gas_limit"),
                StorageLimit = content.RequiredInt32("storage_limit"),
                SenderId = sender.Id,
                Sender = sender,
                SmartRollupId = rollup?.Id,
                GameId = game?.Id,
                Move = RefutationMove.Timeout,
                GameStatus = result.Optional("game_status") switch
                {
                    null => RefutationGameStatus.None,
                    JsonElement el when el.ValueKind == JsonValueKind.String => el.GetString() switch
                    {
                        "ongoing" => RefutationGameStatus.Ongoing,
                        _ => throw new NotImplementedException("Unknown refutation game status")
                    },
                    JsonElement el when el.ValueKind == JsonValueKind.Object => el.Required("result").RequiredString("kind") switch
                    {
                        "loser" => RefutationGameStatus.Loser,
                        "draw" => RefutationGameStatus.Draw,
                        _ => throw new NotImplementedException("Unknown refutation game result kind")
                    },
                    _ => throw new NotImplementedException("Unknown refutation game status")
                },
                Status = result.RequiredString("status") switch
                {
                    "applied" => OperationStatus.Applied,
                    "backtracked" => OperationStatus.Backtracked,
                    "failed" => OperationStatus.Failed,
                    "skipped" => OperationStatus.Skipped,
                    _ => throw new NotImplementedException()
                },
                Errors = result.TryGetProperty("errors", out var errors)
                    ? OperationErrors.Parse(content, errors)
                    : null,
                GasUsed = (int)(((result.OptionalInt64("consumed_milligas") ?? 0) + 999) / 1000),
                StorageUsed = 0,
                StorageFee = null,
                AllocationFee = null
            };
            #endregion

            #region entities
            var blockBaker = block.Proposer;
            var senderDelegate = sender.Delegate ?? sender as Data.Models.Delegate;

            Db.TryAttach(blockBaker);
            Db.TryAttach(sender);
            Db.TryAttach(senderDelegate);
            Db.TryAttach(rollup);
            Db.TryAttach(game);
            #endregion

            #region apply operation
            sender.Balance -= operation.BakerFee;
            if (senderDelegate != null)
            {
                senderDelegate.StakingBalance -= operation.BakerFee;
                if (senderDelegate.Id != sender.Id)
                    senderDelegate.DelegatedBalance -= operation.BakerFee;
            }
            blockBaker.Balance += operation.BakerFee;
            blockBaker.StakingBalance += operation.BakerFee;

            sender.SmartRollupRefuteCount++;
            if (rollup != null) rollup.SmartRollupRefuteCount++;

            block.Operations |= Operations.SmartRollupRefute;
            block.Fees += operation.BakerFee;

            sender.Counter = operation.Counter;

            if (game != null)
                game.LastLevel = operation.Level;

            Cache.AppState.Get().SmartRollupRefuteOpsCount++;
            #endregion

            #region apply result
            if (operation.Status == OperationStatus.Applied)
            {
                game.LastMoveId = operation.Id;

                var initiator = await Cache.Accounts.GetAsync(game.InitiatorId);
                var initiatorBaker = Cache.Accounts.GetDelegate(initiator.DelegateId) ?? (initiator as Data.Models.Delegate);

                var opponent = await Cache.Accounts.GetAsync(game.OpponentId);
                var opponentBaker = Cache.Accounts.GetDelegate(opponent.DelegateId) ?? (opponent as Data.Models.Delegate);

                Db.TryAttach(initiator);
                Db.TryAttach(initiatorBaker);

                Db.TryAttach(opponent);
                Db.TryAttach(opponentBaker);

                var updates = result.RequiredArray("balance_updates").EnumerateArray()
                    .Where(x => x.RequiredString("kind") == "freezer" || x.RequiredString("kind") == "contract");

                var initiatorUpdate = updates.FirstOrDefault(x => x.RequiredString("contract") == initiator.Address);
                var opponentUpdate = updates.FirstOrDefault(x => x.RequiredString("contract") == opponent.Address);

                var initiatorChange = initiatorUpdate.ValueKind != JsonValueKind.Undefined
                    ? initiatorUpdate.RequiredInt64("change")
                    : 0;
                var opponentChange = opponentUpdate.ValueKind != JsonValueKind.Undefined
                    ? opponentUpdate.RequiredInt64("change")
                    : 0;

                if (initiatorChange > 0)
                {
                    game.InitiatorReward = initiatorChange;
                }
                else
                {
                    game.InitiatorLoss = -initiatorChange;
                    initiator.SmartRollupBonds += initiatorChange;
                    rollup.SmartRollupBonds += initiatorChange;

                    var bondOp = block.SmartRollupPublishOps?
                        .FirstOrDefault(x => x.SmartRollupId == operation.SmartRollupId && x.BondStatus == SmartRollupBondStatus.Active && x.SenderId == initiator.Id)
                        ?? await Db.SmartRollupPublishOps.FirstAsync(x => x.SmartRollupId == operation.SmartRollupId && x.BondStatus == SmartRollupBondStatus.Active && x.SenderId == initiator.Id);
                    bondOp.BondStatus = SmartRollupBondStatus.Lost;
                }

                if (opponentChange > 0)
                {
                    game.OpponentReward = opponentChange;
                }
                else
                {
                    game.OpponentLoss = -opponentChange;
                    opponent.SmartRollupBonds += opponentChange;
                    rollup.SmartRollupBonds += opponentChange;

                    var bondOp = block.SmartRollupPublishOps?
                        .FirstOrDefault(x => x.SmartRollupId == operation.SmartRollupId && x.BondStatus == SmartRollupBondStatus.Active && x.SenderId == opponent.Id)
                        ?? await Db.SmartRollupPublishOps.FirstAsync(x => x.SmartRollupId == operation.SmartRollupId && x.BondStatus == SmartRollupBondStatus.Active && x.SenderId == opponent.Id);
                    bondOp.BondStatus = SmartRollupBondStatus.Lost;
                }

                initiator.Balance += initiatorChange;
                if (initiatorBaker != null)
                {
                    initiatorBaker.StakingBalance += initiatorChange;
                    if (initiatorBaker.Id != initiator.Id)
                        initiatorBaker.DelegatedBalance += initiatorChange;
                }

                opponent.Balance += opponentChange;
                if (opponentBaker != null)
                {
                    opponentBaker.StakingBalance += opponentChange;
                    if (opponentBaker.Id != opponent.Id)
                        opponentBaker.DelegatedBalance += opponentChange;
                }

                initiator.ActiveRefutationGamesCount--;
                opponent.ActiveRefutationGamesCount--;
                rollup.ActiveRefutationGamesCount--;
            }
            #endregion

            Proto.Manager.Set(operation.Sender);
            Db.SmartRollupRefuteOps.Add(operation);
        }
    }
}
