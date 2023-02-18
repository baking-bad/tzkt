using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models;
using Tzkt.Data.Models.Base;

namespace Tzkt.Sync.Protocols.Proto16
{
    class SmartRollupRefuteCommit : ProtocolCommit
    {
        public SmartRollupRefuteCommit(ProtocolHandler protocol) : base(protocol) { }

        public virtual async Task Apply(Block block, JsonElement op, JsonElement content)
        {
            #region init
            var sender = await Cache.Accounts.GetAsync(content.RequiredString("source"));
            sender.Delegate ??= Cache.Accounts.GetDelegate(sender.DelegateId);
            var rollup = await Cache.Accounts.GetAsync(content.RequiredString("rollup")) as SmartRollup;

            var refutation = content.Required("refutation");
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
                Move = refutation.RequiredString("refutation_kind") switch
                {
                    "start" => RefutationMove.Start,
                    "move" when refutation.Required("step").ValueKind == JsonValueKind.Array => RefutationMove.Dissection,
                    "move" when refutation.Required("step").ValueKind == JsonValueKind.Object => RefutationMove.Proof,
                    _ => throw new NotImplementedException("Unknown refutation kind")
                },
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

            Cache.AppState.Get().SmartRollupRefuteOpsCount++;
            #endregion

            #region apply result
            if (operation.Status == OperationStatus.Applied)
            {
            }
            #endregion

            #region apply game
            if (operation.Move == RefutationMove.Start)
            {
                if (operation.Status == OperationStatus.Applied)
                {
                    var opponent = await Cache.Accounts.GetAsync(content.RequiredString("opponent"));
                    var initiatorCommitmentHash = refutation.RequiredString("player_commitment_hash");
                    var opponentCommitmentHash = refutation.RequiredString("opponent_commitment_hash");

                    var initiatorCommitment = await Cache.SmartRollupCommitments.GetAsync(initiatorCommitmentHash, rollup.Id);
                    var opponentCommitment = await Cache.SmartRollupCommitments.GetAsync(opponentCommitmentHash, rollup.Id);

                    Db.TryAttach(initiatorCommitment);
                    Db.TryAttach(opponentCommitment);
                    Db.TryAttach(opponent);

                    var game = new RefutationGame
                    {
                        Id = Cache.AppState.NextRefutationGameId(),
                        SmartRollupId = rollup.Id,
                        InitiatorId = sender.Id,
                        OpponentId = opponent.Id,
                        InitiatorCommitmentId = initiatorCommitment.Id,
                        OpponentCommitmentId = opponentCommitment.Id,
                        LastMoveId = operation.Id,
                        FirstLevel = operation.Level,
                        LastLevel = operation.Level,
                        InitiatorReward = null,
                        InitiatorLoss = null,
                        OpponentReward = null,
                        OpponentLoss = null
                    };
                    Cache.RefutationGames.Add(game);
                    Db.RefutationGames.Add(game);

                    operation.GameId = game.Id;

                    sender.RefutationGamesCount++;
                    opponent.RefutationGamesCount++;
                    rollup.RefutationGamesCount++;

                    rollup.ActiveGames++;

                    initiatorCommitment.ActiveGames = (initiatorCommitment.ActiveGames ?? 0) + 1;
                    opponentCommitment.ActiveGames = (opponentCommitment.ActiveGames ?? 0) + 1;
                }
            }
            else
            {
                var _opponent = await Cache.Accounts.GetAsync(content.RequiredString("opponent"));
                if (operation.Status == OperationStatus.Applied)
                {
                    var game = await Cache.RefutationGames.GetAsync(rollup.Id, sender.Id, _opponent.Id);

                    Db.TryAttach(game);
                    game.LastLevel = operation.Level;
                    game.LastMoveId = operation.Id;

                    if (operation.GameStatus != RefutationGameStatus.Ongoing)
                    {
                        var initiator = await Cache.Accounts.GetAsync(game.InitiatorId);
                        var initiatorBaker = Cache.Accounts.GetDelegate(initiator.DelegateId) ?? (initiator as Data.Models.Delegate);
                        var initiatorCommitment = await Cache.SmartRollupCommitments.GetAsync(game.InitiatorCommitmentId);
                        
                        var opponent = await Cache.Accounts.GetAsync(game.OpponentId);
                        var opponentBaker = Cache.Accounts.GetDelegate(opponent.DelegateId) ?? (opponent as Data.Models.Delegate);
                        var opponentCommitment = await Cache.SmartRollupCommitments.GetAsync(game.OpponentCommitmentId);

                        Db.TryAttach(initiator);
                        Db.TryAttach(initiatorBaker);
                        Db.TryAttach(initiatorCommitment);
                        
                        Db.TryAttach(opponent);
                        Db.TryAttach(opponentBaker);
                        Db.TryAttach(opponentCommitment);

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
                            initiatorCommitment.WonGames = (initiatorCommitment.WonGames ?? 0) + 1;
                        }
                        else
                        {
                            game.InitiatorLoss = -initiatorChange;
                            initiator.SmartRollupBonds += initiatorChange;
                            initiatorCommitment.LostGames = (initiatorCommitment.LostGames ?? 0) + 1;
                        }

                        if (opponentChange > 0)
                        {
                            game.OpponentReward = opponentChange;
                            opponentCommitment.WonGames = (opponentCommitment.WonGames ?? 0) + 1;
                        }
                        else
                        {
                            game.OpponentLoss = -opponentChange;
                            opponent.SmartRollupBonds += opponentChange;
                            opponentCommitment.LostGames = (opponentCommitment.LostGames ?? 0) + 1;
                        }

                        initiator.Balance += initiatorChange;
                        initiatorBaker.StakingBalance += initiatorChange;
                        if (initiatorBaker.Id != initiator.Id)
                            initiatorBaker.DelegatedBalance += initiatorChange;

                        opponent.Balance += opponentChange;
                        opponentBaker.StakingBalance += opponentChange;
                        if (opponentBaker.Id != opponent.Id)
                            opponentBaker.DelegatedBalance += opponentChange;

                        initiatorCommitment.ActiveGames--;
                        opponentCommitment.ActiveGames--;

                        rollup.ActiveGames--;
                    }

                    operation.GameId = game.Id;
                }
                else if (rollup != null)
                {
                    var game = await Cache.RefutationGames.GetOrDefaultAsync(rollup.Id, sender.Id, _opponent.Id);
                    if (game != null)
                    {
                        Db.TryAttach(game);
                        game.LastLevel = operation.Level;

                        operation.GameId = game.Id;
                    }
                }
            }
            #endregion

            Proto.Manager.Set(operation.Sender);
            Db.SmartRollupRefuteOps.Add(operation);
        }

        public virtual async Task Revert(Block block, SmartRollupRefuteOperation operation)
        {
            #region init
            operation.Block ??= block;
            operation.Block.Protocol ??= await Cache.Protocols.GetAsync(block.ProtoCode);
            operation.Block.Proposer ??= Cache.Accounts.GetDelegate(block.ProposerId);

            operation.Sender ??= await Cache.Accounts.GetAsync(operation.SenderId);
            operation.Sender.Delegate ??= Cache.Accounts.GetDelegate(operation.Sender.DelegateId);
            #endregion

            #region entities
            var blockBaker = block.Proposer;
            var sender = operation.Sender;
            var senderDelegate = sender.Delegate ?? sender as Data.Models.Delegate;
            var rollup = await Cache.Accounts.GetAsync(operation.SmartRollupId) as SmartRollup;

            Db.TryAttach(blockBaker);
            Db.TryAttach(sender);
            Db.TryAttach(senderDelegate);
            Db.TryAttach(rollup);
            #endregion

            #region revert game
            if (operation.Move == RefutationMove.Start)
            {
                if (operation.Status == OperationStatus.Applied)
                {
                    var game = await Cache.RefutationGames.GetAsync((int)operation.GameId);
                    var opponent = await Cache.Accounts.GetAsync(game.OpponentId);
                    var initiatorCommitment = await Cache.SmartRollupCommitments.GetAsync(game.InitiatorCommitmentId);
                    var opponentCommitment = await Cache.SmartRollupCommitments.GetAsync(game.OpponentCommitmentId);

                    Db.TryAttach(initiatorCommitment);
                    Db.TryAttach(opponentCommitment);
                    Db.TryAttach(opponent);

                    Cache.AppState.ReleaseRefutationGameId();
                    Cache.RefutationGames.Remove(game);
                    Db.RefutationGames.Remove(game);

                    sender.RefutationGamesCount--;
                    opponent.RefutationGamesCount--;
                    rollup.RefutationGamesCount--;

                    rollup.ActiveGames--;

                    initiatorCommitment.ActiveGames--;
                    opponentCommitment.ActiveGames--;
                }
            }
            else
            {
                if (operation.Status == OperationStatus.Applied)
                {
                    var game = await Cache.RefutationGames.GetAsync((int)operation.GameId);
                    var prevOp = await Db.SmartRollupRefuteOps
                        .AsNoTracking()
                        .Where(x => x.Id < operation.Id && x.Status == OperationStatus.Applied)
                        .OrderByDescending(x => x.Id)
                        .FirstAsync();

                    Db.TryAttach(game);
                    game.LastLevel = prevOp.Level;
                    game.LastMoveId = prevOp.Id;

                    if (operation.GameStatus != RefutationGameStatus.Ongoing)
                    {
                        var initiator = await Cache.Accounts.GetAsync(game.InitiatorId);
                        var initiatorBaker = Cache.Accounts.GetDelegate(initiator.DelegateId) ?? (initiator as Data.Models.Delegate);
                        var initiatorCommitment = await Cache.SmartRollupCommitments.GetAsync(game.InitiatorCommitmentId);

                        var opponent = await Cache.Accounts.GetAsync(game.OpponentId);
                        var opponentBaker = Cache.Accounts.GetDelegate(opponent.DelegateId) ?? (opponent as Data.Models.Delegate);
                        var opponentCommitment = await Cache.SmartRollupCommitments.GetAsync(game.OpponentCommitmentId);

                        Db.TryAttach(initiator);
                        Db.TryAttach(initiatorBaker);
                        Db.TryAttach(initiatorCommitment);

                        Db.TryAttach(opponent);
                        Db.TryAttach(opponentBaker);
                        Db.TryAttach(opponentCommitment);

                        var initiatorChange = game.InitiatorReward ?? -game.InitiatorLoss ?? 0;
                        var opponentChange = game.OpponentReward ?? -game.OpponentLoss ?? 0;

                        if (initiatorChange > 0)
                        {
                            game.InitiatorReward = null;
                            initiatorCommitment.WonGames--;
                        }
                        else
                        {
                            game.InitiatorLoss = null;
                            initiator.SmartRollupBonds -= initiatorChange;
                            initiatorCommitment.LostGames--;
                        }

                        if (opponentChange > 0)
                        {
                            game.OpponentReward = null;
                            opponentCommitment.WonGames--;
                        }
                        else
                        {
                            game.OpponentLoss = null;
                            opponent.SmartRollupBonds -= opponentChange;
                            opponentCommitment.LostGames--;
                        }

                        initiator.Balance -= initiatorChange;
                        initiatorBaker.StakingBalance -= initiatorChange;
                        if (initiatorBaker.Id != initiator.Id)
                            initiatorBaker.DelegatedBalance -= initiatorChange;

                        opponent.Balance -= opponentChange;
                        opponentBaker.StakingBalance -= opponentChange;
                        if (opponentBaker.Id != opponent.Id)
                            opponentBaker.DelegatedBalance -= opponentChange;

                        initiatorCommitment.ActiveGames++;
                        opponentCommitment.ActiveGames++;

                        rollup.ActiveGames++;
                    }
                }
                else if (rollup != null)
                {
                    if (operation.GameId != null)
                    {
                        var game = await Cache.RefutationGames.GetAsync((int)operation.GameId);
                        var prevOp = await Db.SmartRollupRefuteOps
                            .AsNoTracking()
                            .Where(x => x.Id < operation.Id && x.Status == OperationStatus.Applied)
                            .OrderByDescending(x => x.Id)
                            .FirstAsync();

                        Db.TryAttach(game);
                        game.LastLevel = prevOp.Level;
                    }
                }
            }
            #endregion

            #region revert result
            if (operation.Status == OperationStatus.Applied)
            {
            }
            #endregion

            #region revert operation
            sender.Balance += operation.BakerFee;
            if (senderDelegate != null)
            {
                senderDelegate.StakingBalance += operation.BakerFee;
                if (senderDelegate.Id != sender.Id)
                    senderDelegate.DelegatedBalance += operation.BakerFee;
            }
            blockBaker.Balance -= operation.BakerFee;
            blockBaker.StakingBalance -= operation.BakerFee;

            sender.SmartRollupRefuteCount--;
            if (rollup != null) rollup.SmartRollupRefuteCount--;

            sender.Counter = operation.Counter - 1;
            (sender as User).Revealed = true;

            Cache.AppState.Get().SmartRollupRefuteOpsCount--;
            #endregion

            Db.SmartRollupRefuteOps.Remove(operation);
            Cache.AppState.ReleaseManagerCounter();
            Cache.AppState.ReleaseOperationId();
        }
    }
}
