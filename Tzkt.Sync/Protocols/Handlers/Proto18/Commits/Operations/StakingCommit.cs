using System.Numerics;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models;
using Tzkt.Data.Models.Base;

namespace Tzkt.Sync.Protocols.Proto18
{
    class StakingCommit(ProtocolHandler protocol) : ProtocolCommit(protocol)
    {
        public static readonly HashSet<string> Entrypoints = ["stake", "unstake", "finalize_unstake"];

        public async Task Apply(Block block, JsonElement op, JsonElement content)
        {
            #region init
            var sender = (await Cache.Accounts.GetExistingAsync(content.RequiredString("source")) as User)!;
            var senderDelegate = sender as Data.Models.Delegate ?? Cache.Accounts.GetDelegate(sender.DelegateId);

            var result = content.Required("metadata").Required("operation_result");
            var operation = new StakingOperation
            {
                Id = Cache.AppState.NextOperationId(),
                OpHash = op.RequiredString("hash"),
                Level = block.Level,
                Timestamp = block.Timestamp,
                BakerFee = content.RequiredInt64("fee"),
                Counter = content.RequiredInt32("counter"),
                GasLimit = content.RequiredInt32("gas_limit"),
                StorageLimit = content.RequiredInt32("storage_limit"),
                SenderId = sender.Id,
                Action = content.Required("parameters").RequiredString("entrypoint") switch
                {
                    "stake" => StakingAction.Stake,
                    "unstake" => StakingAction.Unstake,
                    "finalize_unstake" => StakingAction.Finalize,
                    _ => throw new NotImplementedException()
                },
                RequestedAmount = content.RequiredInt64("amount"),
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
                AllocationFee = null,
                StorageFee = null,
                StorageUsed = 0
            };
            #endregion

            #region apply operation
            Db.TryAttach(sender);
            sender.Balance -= operation.BakerFee;
            sender.Counter = operation.Counter;
            sender.StakingOpsCount++;

            if (senderDelegate != null)
            {
                Db.TryAttach(senderDelegate);
                senderDelegate.StakingBalance -= operation.BakerFee;
                if (senderDelegate != sender)
                {
                    senderDelegate.DelegatedBalance -= operation.BakerFee;
                    senderDelegate.StakingOpsCount++;
                }
            }

            Context.Proposer.Balance += operation.BakerFee;
            Context.Proposer.StakingBalance += operation.BakerFee;

            block.Operations |= Operations.Staking;
            block.Fees += operation.BakerFee;

            Cache.AppState.Get().StakingOpsCount++;
            #endregion

            #region apply result
            if (operation.Status == OperationStatus.Applied)
            {
                if (result.TryGetProperty("balance_updates", out var balanceUpdates))
                {
                    var updates = await ParseStakingUpdates(block, operation, [.. balanceUpdates.EnumerateArray()]);
                    await new StakingUpdateCommit(Proto).Apply(updates);

                    operation.Amount = operation.Action switch
                    {
                        StakingAction.Stake => operation.Amount = updates
                            .Where(x => x.Type == StakingUpdateType.Stake || x.Type == StakingUpdateType.Restake)
                            .Sum(x => x.Amount),
                        StakingAction.Unstake => operation.Amount = updates
                            .Where(x => x.Type == StakingUpdateType.Unstake)
                            .Sum(x => x.Amount),
                        StakingAction.Finalize => operation.Amount = updates
                            .Where(x => x.Type == StakingUpdateType.Finalize)
                            .Sum(x => x.Amount),
                        _ => throw new NotImplementedException()
                    };
                    operation.BakerId = operation.Action == StakingAction.Finalize
                        ? updates.FirstOrDefault(x => x.Type == StakingUpdateType.Finalize)?.BakerId ?? sender.UnstakedBakerId ?? senderDelegate?.Id
                        : updates.FirstOrDefault(x => x.Type != StakingUpdateType.Finalize)?.BakerId ?? senderDelegate?.Id;
                    operation.StakingUpdatesCount = updates.Count;
                }
                else
                {
                    operation.Amount = 0;
                    operation.BakerId = operation.Action == StakingAction.Finalize
                        ? sender.UnstakedBakerId ?? senderDelegate?.Id
                        : senderDelegate?.Id;
                    operation.StakingUpdatesCount = 0;
                }
            }
            #endregion

            Proto.Manager.Set(sender);
            Db.StakingOps.Add(operation);
            Context.StakingOps.Add(operation);
        }

        public async Task Revert(Block block, StakingOperation operation)
        {
            var sender = (await Cache.Accounts.GetAsync(operation.SenderId) as User)!;
            var senderDelegate = sender as Data.Models.Delegate ?? Cache.Accounts.GetDelegate(sender.DelegateId);

            Db.TryAttach(sender);
            Db.TryAttach(senderDelegate);

            #region revert result
            if (operation.Status == OperationStatus.Applied)
            {
                if (operation.StakingUpdatesCount != null)
                {
                    var updates = await Db.StakingUpdates
                        .Where(x => x.StakingOpId == operation.Id)
                        .OrderByDescending(x => x.Id)
                        .ToListAsync();
                    await new StakingUpdateCommit(Proto).Revert(updates);
                }
            }
            #endregion

            #region revert operation
            sender.Balance += operation.BakerFee;
            sender.Counter = operation.Counter - 1;
            sender.StakingOpsCount--;

            if (senderDelegate != null)
            {
                senderDelegate.StakingBalance += operation.BakerFee;
                if (senderDelegate != sender)
                {
                    senderDelegate.DelegatedBalance += operation.BakerFee;
                    senderDelegate.StakingOpsCount--;
                }
            }

            Context.Proposer.Balance -= operation.BakerFee;
            Context.Proposer.StakingBalance -= operation.BakerFee;

            Cache.AppState.Get().StakingOpsCount--;
            #endregion

            Db.StakingOps.Remove(operation);
            Cache.AppState.ReleaseManagerCounter();
            Cache.AppState.ReleaseOperationId();
        }

        async Task<List<StakingUpdate>> ParseStakingUpdates(Block block, StakingOperation operation, List<JsonElement> balanceUpdates)
        {
            var res = new List<StakingUpdate>();

            if (balanceUpdates.Count % 2 != 0)
                throw new Exception("Unexpected staking balance updates behavior");

            for (int i = 0; i < balanceUpdates.Count; i += 2)
            {
                var update = balanceUpdates[i];
                var kind = update.RequiredString("kind");
                var category = update.OptionalString("category");

                var nextUpdate = balanceUpdates[i + 1];
                var nextKind = nextUpdate.RequiredString("kind");
                var nextCategory = nextUpdate.OptionalString("category");

                if (kind == "contract")
                {
                    if (nextKind != "freezer" || nextCategory != "deposits")
                        throw new Exception("Unexpected staking balance updates behavior");

                    #region stake
                    var baker = nextUpdate.Required("staker").OptionalString("delegate") ?? GetFreezerBaker(nextUpdate);
                    var staker = nextUpdate.Required("staker").OptionalString("contract") ?? GetFreezerBaker(nextUpdate);
                    var change = nextUpdate.RequiredInt64("change");

                    if (staker != update.RequiredString("contract") || change != -update.RequiredInt64("change"))
                        throw new Exception("Unexpected staking balance updates behavior");

                    var pseudotokens = (BigInteger?)null;
                    if (i >= 2 && balanceUpdates[i - 1].RequiredString("kind") == "staking")
                    {
                        if (balanceUpdates[i - 2].RequiredString("kind") != "staking" ||
                            balanceUpdates[i - 2].RequiredString("change") != balanceUpdates[i - 1].RequiredString("change"))
                            throw new Exception("Unexpected staking balance updates behavior");

                        pseudotokens = balanceUpdates[i - 1].RequiredBigInteger("change");
                    }

                    res.Add(new StakingUpdate
                    {
                        Id = ++Cache.AppState.Get().StakingUpdatesCount,
                        Level = block.Level,
                        Cycle = block.Cycle,
                        BakerId = Cache.Accounts.GetExistingDelegate(baker).Id,
                        StakerId = (await Cache.Accounts.GetExistingAsync(staker)).Id,
                        Type = StakingUpdateType.Stake,
                        Amount = change,
                        Pseudotokens = pseudotokens,
                        StakingOpId = operation.Id,
                    });
                    #endregion
                }
                else if (kind == "freezer" && category == "deposits")
                {
                    if (nextKind == "freezer" && nextCategory == "unstaked_deposits")
                    {
                        #region unstake
                        var baker = nextUpdate.Required("staker").RequiredString("delegate");
                        var staker = nextUpdate.Required("staker").RequiredString("contract");
                        var change = nextUpdate.RequiredInt64("change");
                        var cycle = nextUpdate.RequiredInt32("cycle");

                        if (baker != (update.Required("staker").OptionalString("delegate") ?? GetFreezerBaker(update)) ||
                            staker != (update.Required("staker").OptionalString("contract") ?? GetFreezerBaker(update)) ||
                            change != -update.RequiredInt64("change"))
                            throw new Exception("Unexpected staking balance updates behavior");

                        var pseudotokens = (BigInteger?)null;
                        if (i >= 2 && balanceUpdates[i - 1].RequiredString("kind") == "staking")
                        {
                            if (balanceUpdates[i - 2].RequiredString("kind") != "staking" ||
                                balanceUpdates[i - 2].RequiredString("change") != balanceUpdates[i - 1].RequiredString("change"))
                                throw new Exception("Unexpected staking balance updates behavior");

                            pseudotokens = -balanceUpdates[i - 1].RequiredBigInteger("change");
                        }

                        res.Add(new StakingUpdate
                        {
                            Id = ++Cache.AppState.Get().StakingUpdatesCount,
                            Level = block.Level,
                            Cycle = cycle,
                            BakerId = Cache.Accounts.GetExistingDelegate(baker).Id,
                            StakerId = (await Cache.Accounts.GetExistingAsync(staker)).Id,
                            Type = StakingUpdateType.Unstake,
                            Amount = change,
                            Pseudotokens = pseudotokens,
                            StakingOpId = operation.Id,
                        });
                        #endregion
                    }
                    else
                    {
                        throw new Exception("Unexpected staking balance updates behavior");
                    }
                }
                else if (kind == "freezer" && category == "unstaked_deposits")
                {
                    if (nextKind == "contract")
                    {
                        #region finalize
                        var baker = update.Required("staker").RequiredString("delegate");
                        var staker = update.Required("staker").RequiredString("contract");
                        var change = nextUpdate.RequiredInt64("change");
                        var cycle = update.RequiredInt32("cycle");

                        if (staker != nextUpdate.RequiredString("contract") || change != -update.RequiredInt64("change"))
                            throw new Exception("Unexpected staking balance updates behavior");

                        res.Add(new StakingUpdate
                        {
                            Id = ++Cache.AppState.Get().StakingUpdatesCount,
                            Level = block.Level,
                            Cycle = cycle,
                            BakerId = Cache.Accounts.GetExistingDelegate(baker).Id,
                            StakerId = (await Cache.Accounts.GetExistingAsync(staker)).Id,
                            Type = StakingUpdateType.Finalize,
                            Amount = change,
                            Pseudotokens = null,
                            StakingOpId = operation.Id,
                        });
                        #endregion
                    }
                    else if (nextKind == "freezer" && nextCategory == "deposits")
                    {
                        #region restake
                        var baker = nextUpdate.Required("staker").OptionalString("delegate") ?? GetFreezerBaker(nextUpdate);
                        var staker = nextUpdate.Required("staker").OptionalString("contract") ?? GetFreezerBaker(nextUpdate);
                        var change = nextUpdate.RequiredInt64("change");
                        var cycle = update.RequiredInt32("cycle");

                        if (baker != (update.Required("staker").OptionalString("delegate") ?? GetFreezerBaker(update)) ||
                            staker != (update.Required("staker").OptionalString("contract") ?? GetFreezerBaker(update)) ||
                            change != -update.RequiredInt64("change"))
                            throw new Exception("Unexpected staking balance updates behavior");

                        var pseudotokens = (BigInteger?)null;
                        if (i >= 2 && balanceUpdates[i - 1].RequiredString("kind") == "staking")
                        {
                            if (balanceUpdates[i - 2].RequiredString("kind") != "staking" ||
                                balanceUpdates[i - 2].RequiredString("change") != balanceUpdates[i - 1].RequiredString("change"))
                                throw new Exception("Unexpected staking balance updates behavior");

                            pseudotokens = balanceUpdates[i - 1].RequiredBigInteger("change");
                        }

                        res.Add(new StakingUpdate
                        {
                            Id = ++Cache.AppState.Get().StakingUpdatesCount,
                            Level = block.Level,
                            Cycle = cycle,
                            BakerId = Cache.Accounts.GetExistingDelegate(baker).Id,
                            StakerId = (await Cache.Accounts.GetExistingAsync(staker)).Id,
                            Type = StakingUpdateType.Restake,
                            Amount = change,
                            Pseudotokens = pseudotokens,
                            StakingOpId = operation.Id,
                        });
                        #endregion
                    }
                    else
                    {
                        throw new Exception("Unexpected staking balance updates behavior");
                    }
                }
                else if (kind != "staking")
                {
                    throw new Exception("Unexpected staking balance updates behavior");
                }
            }

            return res;
        }

        protected virtual string GetFreezerBaker(JsonElement update)
        {
            return update.Required("staker").RequiredString("baker");
        }
    }
}
