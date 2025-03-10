using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto18
{
    class AutostakingCommit : ProtocolCommit
    {
        public AutostakingCommit(ProtocolHandler protocol) : base(protocol) { }

        public virtual async Task Apply(Block block, JsonElement rawBlock)
        {
            if (!block.Events.HasFlag(BlockEvents.CycleEnd))
                return;

            var balanceUpdates = rawBlock
                .Required("metadata")
                .RequiredArray("balance_updates")
                .EnumerateArray()
                .Where(x => x.RequiredString("origin") == "block")
                .ToList();

            var updates = ParseStakingUpdates(block, balanceUpdates);

            if (updates.Count > 0)
            {
                Db.TryAttach(block);
                block.Operations |= Operations.Autostaking;

                var state = Cache.AppState.Get();
                Db.TryAttach(state);

                foreach (var group in updates.GroupBy(x => x.BakerId))
                {
                    var staked = group.Where(x => x.Type == StakingUpdateType.Stake || x.Type == StakingUpdateType.Restake).Sum(x => x.Amount);
                    var unstaked = group.Where(x => x.Type == StakingUpdateType.Unstake).Sum(x => x.Amount);
                    var finalized = group.Where(x => x.Type == StakingUpdateType.Finalize).Sum(x => x.Amount);

                    var operation = new AutostakingOperation
                    {
                        Id = Cache.AppState.NextOperationId(),
                        Level = group.First().Level,
                        Action = staked != 0 ? StakingAction.Stake : unstaked != 0 ? StakingAction.Unstake : StakingAction.Finalize,
                        Amount = staked != 0 ? staked : unstaked != 0 ? unstaked : finalized,
                        BakerId = group.Key,
                        StakingUpdatesCount = group.Count()
                    };

                    foreach (var update in group)
                        update.AutostakingOpId = operation.Id;

                    var baker = Cache.Accounts.GetDelegate(group.Key);
                    Db.TryAttach(baker);
                    baker.AutostakingOpsCount++;
                    baker.LastLevel = block.Level;

                    state.AutostakingOpsCount++;

                    Db.AutostakingOps.Add(operation);
                    Context.AutostakingOps.Add(operation);
                }

                await new StakingUpdateCommit(Proto).Apply(updates);
            }
        }

        public virtual async Task Revert(Block block)
        {
            if (!block.Operations.HasFlag(Operations.Autostaking))
                return;

            var state = Cache.AppState.Get();
            Db.TryAttach(state);

            foreach (var op in await Db.AutostakingOps.Where(x => x.Level == block.Level).ToListAsync())
            {
                var baker = Cache.Accounts.GetDelegate(op.BakerId);
                Db.TryAttach(baker);
                baker.AutostakingOpsCount--;

                state.AutostakingOpsCount--;

                Db.AutostakingOps.Remove(op);
                Cache.AppState.ReleaseOperationId();
            }

            var updates = await Db.StakingUpdates
                .Where(x => x.Level == block.Level && x.AutostakingOpId != null)
                .OrderByDescending(x => x.Id)
                .ToListAsync();

            await new StakingUpdateCommit(Proto).Revert(updates);
        }

        protected virtual List<StakingUpdate> ParseStakingUpdates(Block block, List<JsonElement> balanceUpdates)
        {
            var res = new List<StakingUpdate>();

            if (balanceUpdates.Count % 2 != 0)
                throw new Exception("Unexpected autostaking balance updates behavior");

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
                        throw new Exception("Unexpected autostaking balance updates behavior");

                    #region stake
                    var baker = GetFreezerBaker(nextUpdate);
                    var change = nextUpdate.RequiredInt64("change");

                    if (baker != update.RequiredString("contract") ||
                        change != -update.RequiredInt64("change"))
                        throw new Exception("Unexpected autostaking balance updates behavior");

                    res.Add(new StakingUpdate
                    {
                        Id = ++Cache.AppState.Get().StakingUpdatesCount,
                        Level = block.Level,
                        Cycle = block.Cycle,
                        BakerId = Cache.Accounts.GetDelegate(baker).Id,
                        StakerId = Cache.Accounts.GetDelegate(baker).Id,
                        Type = StakingUpdateType.Stake,
                        Amount = change
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

                        if (baker != staker || 
                            baker != GetFreezerBaker(update) ||
                            change != -update.RequiredInt64("change"))
                            throw new Exception("Unexpected autostaking balance updates behavior");

                        res.Add(new StakingUpdate
                        {
                            Id = ++Cache.AppState.Get().StakingUpdatesCount,
                            Level = block.Level,
                            Cycle = cycle,
                            BakerId = Cache.Accounts.GetDelegate(baker).Id,
                            StakerId = Cache.Accounts.GetDelegate(staker).Id,
                            Type = StakingUpdateType.Unstake,
                            Amount = change
                        });
                        #endregion
                    }
                    else
                    {
                        throw new Exception("Unexpected autostaking balance updates behavior");
                    }
                }
                else if (kind == "freezer" && category == "unstaked_deposits")
                {
                    if (nextKind == "contract")
                    {
                        var baker = update.Required("staker").RequiredString("delegate");
                        var staker = update.Required("staker").RequiredString("contract");
                        var change = nextUpdate.RequiredInt64("change");
                        var cycle = update.RequiredInt32("cycle");

                        #region finalize
                        if (baker != staker ||
                            baker != nextUpdate.RequiredString("contract") ||
                            change != -update.RequiredInt64("change"))
                            throw new Exception("Unexpected autostaking balance updates behavior");

                        res.Add(new StakingUpdate
                        {
                            Id = ++Cache.AppState.Get().StakingUpdatesCount,
                            Level = block.Level,
                            Cycle = cycle,
                            BakerId = Cache.Accounts.GetDelegate(baker).Id,
                            StakerId = Cache.Accounts.GetDelegate(staker).Id,
                            Type = StakingUpdateType.Finalize,
                            Amount = change
                        });
                        #endregion
                    }
                    else if (nextKind == "freezer" && nextCategory == "deposits")
                    {
                        var baker = update.Required("staker").RequiredString("delegate");
                        var staker = update.Required("staker").RequiredString("contract");
                        var change = nextUpdate.RequiredInt64("change");
                        var cycle = update.RequiredInt32("cycle");

                        #region restake
                        if (baker != staker || 
                            baker != GetFreezerBaker(nextUpdate) ||
                            change != -update.RequiredInt64("change"))
                            throw new Exception("Unexpected autostaking balance updates behavior");

                        res.Add(new StakingUpdate
                        {
                            Id = ++Cache.AppState.Get().StakingUpdatesCount,
                            Level = block.Level,
                            Cycle = cycle,
                            BakerId = Cache.Accounts.GetDelegate(baker).Id,
                            StakerId = Cache.Accounts.GetDelegate(staker).Id,
                            Type = StakingUpdateType.Restake,
                            Amount = change
                        });
                        #endregion
                    }
                    else
                    {
                        throw new Exception("Unexpected autostaking balance updates behavior");
                    }
                }
                else if (kind != "accumulator" && kind != "minted")
                {
                    throw new Exception("Unexpected autostaking balance updates behavior");
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
