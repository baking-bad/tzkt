using System.Numerics;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto18
{
    class DelegationsCommit(ProtocolHandler protocol) : Proto14.DelegationsCommit(protocol)
    {
        protected override async Task Unstake(DelegationOperation operation, List<JsonElement> balanceUpdates)
        {
            if (balanceUpdates.Count == 0)
                return;

            var updates = await ParseStakingUpdates(operation, balanceUpdates);

            await new StakingUpdateCommit(Proto).Apply(updates);

            operation.StakingUpdatesCount = updates.Count;
        }

        protected override async Task RevertUnstake(DelegationOperation operation)
        {
            if (operation.StakingUpdatesCount == null)
                return;

            var updates = await Db.StakingUpdates
                .Where(x => x.DelegationOpId == operation.Id)
                .OrderByDescending(x => x.Id)
                .ToListAsync();

            await new StakingUpdateCommit(Proto).Revert(updates);
            
            operation.StakingUpdatesCount = null;
        }

        async Task<List<StakingUpdate>> ParseStakingUpdates(DelegationOperation operation, List<JsonElement> balanceUpdates)
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

                if (kind == "freezer" && category == "deposits")
                {
                    if (nextKind == "freezer" && nextCategory == "unstaked_deposits")
                    {
                        #region unstake
                        var baker = nextUpdate.Required("staker").RequiredString("delegate");
                        var staker = nextUpdate.Required("staker").RequiredString("contract");
                        var change = nextUpdate.RequiredInt64("change");
                        var cycle = nextUpdate.RequiredInt32("cycle");

                        if (baker != (update.Required("staker").RequiredString("delegate")) ||
                            staker != (update.Required("staker").RequiredString("contract")) ||
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
                            Level = operation.Level,
                            Cycle = cycle,
                            BakerId = Cache.Accounts.GetExistingDelegate(baker).Id,
                            StakerId = (await Cache.Accounts.GetExistingAsync(staker)).Id,
                            Type = StakingUpdateType.Unstake,
                            Amount = change,
                            Pseudotokens = pseudotokens,
                            DelegationOpId = operation.Id,
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
                            Level = operation.Level,
                            Cycle = cycle,
                            BakerId = Cache.Accounts.GetExistingDelegate(baker).Id,
                            StakerId = (await Cache.Accounts.GetExistingAsync(staker)).Id,
                            Type = StakingUpdateType.Finalize,
                            Amount = change,
                            Pseudotokens = null,
                            DelegationOpId = operation.Id,
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
    }
}
