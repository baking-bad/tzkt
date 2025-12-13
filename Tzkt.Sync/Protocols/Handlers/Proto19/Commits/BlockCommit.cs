using System.Text.Json;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto19
{
    class BlockCommit(ProtocolHandler protocol) : Proto18.BlockCommit(protocol)
    {
        public override async Task Apply(JsonElement rawBlock)
        {
            await base.Apply(rawBlock);

            var state = Cache.AppState.Get();
            if (state.AiActivationLevel is null)
            {
                if (rawBlock.Required("metadata").OptionalInt32("adaptive_issuance_activation_cycle") is int aiCycle && aiCycle == state.Cycle)
                {
                    state.AiActivationLevel = Block.Level;
                    UpdateBakersPower();
                }
            }
        }

        public override void Revert(Block block)
        {
            var state = Cache.AppState.Get();
            if (state.AiActivationLevel == block.Level)
            {
                state.AiActivationLevel = null;
                RevertBakersPower();
            }

            base.Revert(block);
        }

        protected override (long, long, long, long, long, long, long, long) ParseRewards(Data.Models.Delegate proposer, Data.Models.Delegate producer, List<JsonElement> balanceUpdates)
        {
            var rewardDelegated = 0L;
            var rewardStakedOwn = 0L;
            var rewardStakedEdge = 0L;
            var rewardStakedShared = 0L;
            var bonusDelegated = 0L;
            var bonusStakedOwn = 0L;
            var bonusStakedEdge = 0L;
            var bonusStakedShared = 0L;

            for (int i = 0; i < balanceUpdates.Count; i++)
            {
                var update = balanceUpdates[i];
                if (update.RequiredString("kind") == "minted" && update.RequiredString("category") == "baking rewards")
                {
                    if (i == balanceUpdates.Count - 1)
                        throw new Exception("Unexpected baking rewards balance updates behavior");

                    var change = -update.RequiredInt64("change");

                    var nextUpdate = balanceUpdates[i + 1];
                    if (nextUpdate.RequiredInt64("change") != change)
                        throw new Exception("Unexpected baking rewards balance updates behavior");

                    if (nextUpdate.RequiredString("kind") == "freezer" && nextUpdate.RequiredString("category") == "deposits")
                    {
                        var staker = nextUpdate.Required("staker");
                        if (staker.TryGetProperty("baker_own_stake", out var p) && p.GetString() == proposer.Address)
                        {
                            rewardStakedOwn += change;
                        }
                        else if (staker.TryGetProperty("baker_edge", out p) && p.GetString() == proposer.Address)
                        {
                            rewardStakedEdge += change;
                        }
                        else if (staker.TryGetProperty("delegate", out p) && p.GetString() == proposer.Address)
                        {
                            rewardStakedShared += change;
                        }
                        else
                        {
                            throw new Exception("Unexpected baking rewards balance updates behavior");
                        }
                    }
                    else if (nextUpdate.RequiredString("kind") == "contract" && nextUpdate.RequiredString("contract") == proposer.Address)
                    {
                        rewardDelegated += change;
                    }
                    else
                    {
                        throw new Exception("Unexpected baking rewards balance updates behavior");
                    }
                }
                else if (update.RequiredString("kind") == "minted" && update.RequiredString("category") == "baking bonuses")
                {
                    if (i == balanceUpdates.Count - 1)
                        throw new Exception("Unexpected baking bonuses balance updates behavior");

                    var change = -update.RequiredInt64("change");

                    var nextUpdate = balanceUpdates[i + 1];
                    if (nextUpdate.RequiredInt64("change") != change)
                        throw new Exception("Unexpected baking bonuses balance updates behavior");

                    if (nextUpdate.RequiredString("kind") == "freezer" && nextUpdate.RequiredString("category") == "deposits")
                    {
                        var staker = nextUpdate.Required("staker");
                        if (staker.TryGetProperty("baker_own_stake", out var p) && p.GetString() == producer.Address)
                        {
                            bonusStakedOwn += change;
                        }
                        else if (staker.TryGetProperty("baker_edge", out p) && p.GetString() == producer.Address)
                        {
                            bonusStakedEdge += change;
                        }
                        else if (staker.TryGetProperty("delegate", out p) && p.GetString() == producer.Address)
                        {
                            bonusStakedShared += change;
                        }
                        else
                        {
                            throw new Exception("Unexpected baking bonuses balance updates behavior");
                        }
                    }
                    else if (nextUpdate.RequiredString("kind") == "contract" && nextUpdate.RequiredString("contract") == producer.Address)
                    {
                        bonusDelegated += change;
                    }
                    else
                    {
                        throw new Exception("Unexpected baking bonuses balance updates behavior");
                    }
                }
            }

            return (rewardDelegated, rewardStakedOwn, rewardStakedEdge, rewardStakedShared, bonusDelegated, bonusStakedOwn, bonusStakedEdge, bonusStakedShared);
        }
    }
}
