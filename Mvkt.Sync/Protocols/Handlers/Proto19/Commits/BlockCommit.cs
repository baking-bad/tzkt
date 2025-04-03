using System.Text.Json;

namespace Mvkt.Sync.Protocols.Proto19
{
    class BlockCommit : Proto18.BlockCommit
    {
        public BlockCommit(ProtocolHandler protocol) : base(protocol) { }

        protected override (long, long, long, long, long, long, long, long, long, long) ParseRewards(Data.Models.Delegate proposer, Data.Models.Delegate producer, List<JsonElement> balanceUpdates)
        {
            var rewardDelegated = 0L;
            var rewardStakedOwn = 0L;
            var rewardStakedEdge = 0L;
            var rewardStakedShared = 0L;
            var bonusDelegated = 0L;
            var bonusStakedOwn = 0L;
            var bonusStakedEdge = 0L;
            var bonusStakedShared = 0L;
            var feeProtocolTreasury = 0L;
            var feeBurnAddress = 0L;

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
                else if (update.RequiredString("kind") == "accumulator" && update.RequiredString("category") == "block fees")
                {
                    if (i == balanceUpdates.Count - 1)
                        throw new Exception("Unexpected baking rewards balance updates behavior");

                    var change = -update.RequiredInt64("change");

                    var nextUpdate = balanceUpdates[i + 1];
                    if (nextUpdate.RequiredString("kind") == "contract" &&
                        (nextUpdate.RequiredString("contract") == Proto10.ProtoActivator.ProtocolTreasuryContract || nextUpdate.RequiredString("contract") == Proto10.ProtoActivator.BufferContract) &&
                        nextUpdate.RequiredInt64("change") == change)
                    {
                        feeProtocolTreasury += change;
                        rewardDelegated -= feeProtocolTreasury;
                    }
                    else if (nextUpdate.RequiredString("kind") == "contract" &&
                        nextUpdate.RequiredString("contract") == BurnAddress.Address &&
                        nextUpdate.RequiredInt64("change") == change)
                    {
                        feeBurnAddress += change;
                        rewardDelegated -= feeBurnAddress;
                    }
                }
            }

            return (rewardDelegated, rewardStakedOwn, rewardStakedEdge, rewardStakedShared, bonusDelegated, bonusStakedOwn, bonusStakedEdge, bonusStakedShared, feeProtocolTreasury, feeBurnAddress);
        }
    }
}
