using System.Text.Json;

namespace Mvkt.Sync.Protocols.Proto19
{
    class NonceRevelationsCommit : Proto18.NonceRevelationsCommit
    {
        public NonceRevelationsCommit(ProtocolHandler protocol) : base(protocol) { }

        protected override (long, long, long, long) ParseRewards(Data.Models.Delegate proposer, List<JsonElement> balanceUpdates)
        {
            var rewardDelegated = 0L;
            var rewardStakedOwn = 0L;
            var rewardStakedEdge = 0L;
            var rewardStakedShared = 0L;

            for (int i = 0; i < balanceUpdates.Count; i++)
            {
                var update = balanceUpdates[i];
                if (update.RequiredString("kind") == "minted" && update.RequiredString("category") == "nonce revelation rewards")
                {
                    if (i == balanceUpdates.Count - 1)
                        throw new Exception("Unexpected nonce revelation rewards balance updates behavior");

                    var change = -update.RequiredInt64("change");

                    var nextUpdate = balanceUpdates[i + 1];
                    if (nextUpdate.RequiredInt64("change") != change)
                        throw new Exception("Unexpected nonce revelation rewards balance updates behavior");

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
                            throw new Exception("Unexpected nonce revelation rewards balance updates behavior");
                        }
                    }
                    else if (nextUpdate.RequiredString("kind") == "contract" && nextUpdate.RequiredString("contract") == proposer.Address)
                    {
                        rewardDelegated += change;
                    }
                    else
                    {
                        throw new Exception("Unexpected nonce revelation rewards balance updates behavior");
                    }
                }
            }

            return (rewardDelegated, rewardStakedOwn, rewardStakedEdge, rewardStakedShared);
        }
    }
}
