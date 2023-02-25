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
            var refutation = content.Required("refutation");
            var result = content.Required("metadata").Required("operation_result");
            var move = refutation.RequiredString("refutation_kind") switch
            {
                "start" => RefutationMove.Start,
                "move" when refutation.Required("step").ValueKind == JsonValueKind.Array => RefutationMove.Dissection,
                "move" when refutation.Required("step").ValueKind == JsonValueKind.Object => RefutationMove.Proof,
                _ => throw new NotImplementedException("Unknown refutation kind")
            };

            var sender = await Cache.Accounts.GetAsync(content.RequiredString("source"));
            sender.Delegate ??= Cache.Accounts.GetDelegate(sender.DelegateId);
            var opponent = await Cache.Accounts.GetAsync(content.RequiredString("opponent"));
            var rollup = await Cache.Accounts.GetSmartRollupOrDefaultAsync(content.RequiredString("rollup"));
            var game = move != RefutationMove.Start 
                ? await Cache.RefutationGames.GetOrDefaultAsync(rollup?.Id, sender.Id, opponent?.Id)
                : null;

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
                Move = move,
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
            Db.TryAttach(opponent);
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
                if (operation.Move == RefutationMove.Start)
                {
                    var initiatorCommitmentHash = refutation.RequiredString("player_commitment_hash");
                    var opponentCommitmentHash = refutation.RequiredString("opponent_commitment_hash");

                    var initiatorCommitment = await Cache.SmartRollupCommitments.GetAsync(initiatorCommitmentHash, rollup.Id);
                    var opponentCommitment = await Cache.SmartRollupCommitments.GetAsync(opponentCommitmentHash, rollup.Id);

                    game = new RefutationGame
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

                    sender.RefutationGamesCount++;
                    sender.ActiveRefutationGamesCount++;

                    opponent.RefutationGamesCount++;
                    opponent.ActiveRefutationGamesCount++;

                    rollup.RefutationGamesCount++;
                    rollup.ActiveRefutationGamesCount++;

                    operation.GameId = game.Id;
                }
                else
                {
                    game.LastMoveId = operation.Id;

                    if (operation.GameStatus != RefutationGameStatus.Ongoing)
                    {
                        var initiator = await Cache.Accounts.GetAsync(game.InitiatorId);
                        var initiatorBaker = Cache.Accounts.GetDelegate(initiator.DelegateId) ?? (initiator as Data.Models.Delegate);

                        opponent = await Cache.Accounts.GetAsync(game.OpponentId);
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

            #region revert result
            if (operation.Status == OperationStatus.Applied)
            {
                var game = await Cache.RefutationGames.GetAsync((int)operation.GameId);
                var opponent = await Cache.Accounts.GetAsync(game.OpponentId);
                
                Db.TryAttach(game);
                Db.TryAttach(opponent);

                if (operation.Move == RefutationMove.Start)
                {
                    Cache.AppState.ReleaseRefutationGameId();
                    Cache.RefutationGames.Remove(game);
                    Db.RefutationGames.Remove(game);

                    sender.RefutationGamesCount--;
                    sender.ActiveRefutationGamesCount--;
                   
                    opponent.RefutationGamesCount--;
                    opponent.ActiveRefutationGamesCount--;
                    
                    rollup.RefutationGamesCount--;
                    rollup.ActiveRefutationGamesCount--;
                }
                else
                {
                    var prevMove = await Db.SmartRollupRefuteOps
                        .AsNoTracking()
                        .Where(x => x.Id < operation.Id && x.Status == OperationStatus.Applied)
                        .OrderByDescending(x => x.Id)
                        .FirstAsync();

                    game.LastMoveId = prevMove.Id;

                    if (operation.GameStatus != RefutationGameStatus.Ongoing)
                    {
                        var initiator = await Cache.Accounts.GetAsync(game.InitiatorId);
                        var initiatorBaker = Cache.Accounts.GetDelegate(initiator.DelegateId) ?? (initiator as Data.Models.Delegate);

                        //var opponent = await Cache.Accounts.GetAsync(game.OpponentId);
                        var opponentBaker = Cache.Accounts.GetDelegate(opponent.DelegateId) ?? (opponent as Data.Models.Delegate);

                        Db.TryAttach(initiator);
                        Db.TryAttach(initiatorBaker);

                        Db.TryAttach(opponent);
                        Db.TryAttach(opponentBaker);

                        var initiatorChange = game.InitiatorReward ?? -game.InitiatorLoss ?? 0;
                        var opponentChange = game.OpponentReward ?? -game.OpponentLoss ?? 0;

                        if (initiatorChange > 0)
                        {
                            game.InitiatorReward = null;
                        }
                        else
                        {
                            game.InitiatorLoss = null;
                            initiator.SmartRollupBonds -= initiatorChange;
                            rollup.SmartRollupBonds -= initiatorChange;
                        }

                        if (opponentChange > 0)
                        {
                            game.OpponentReward = null;
                        }
                        else
                        {
                            game.OpponentLoss = null;
                            opponent.SmartRollupBonds -= opponentChange;
                            rollup.SmartRollupBonds -= opponentChange;
                        }

                        initiator.Balance -= initiatorChange;
                        if (initiatorBaker != null)
                        {
                            initiatorBaker.StakingBalance -= initiatorChange;
                            if (initiatorBaker.Id != initiator.Id)
                                initiatorBaker.DelegatedBalance -= initiatorChange;
                        }

                        opponent.Balance -= opponentChange;
                        if (opponentBaker != null)
                        {
                            opponentBaker.StakingBalance -= opponentChange;
                            if (opponentBaker.Id != opponent.Id)
                                opponentBaker.DelegatedBalance -= opponentChange;
                        }

                        initiator.ActiveRefutationGamesCount++;
                        opponent.ActiveRefutationGamesCount++;
                        rollup.ActiveRefutationGamesCount++;
                    }
                }
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

            // game.LastLevel is not reverted

            Cache.AppState.Get().SmartRollupRefuteOpsCount--;
            #endregion

            Db.SmartRollupRefuteOps.Remove(operation);
            Cache.AppState.ReleaseManagerCounter();
            Cache.AppState.ReleaseOperationId();
        }
    }
}
