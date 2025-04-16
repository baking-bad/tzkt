using System.Text.Json;

namespace Mvkt.Sync.Protocols.Proto20
{
    class BlockCommit : Proto19.BlockCommit
    {
        public BlockCommit(ProtocolHandler protocol) : base(protocol) { }

        public async Task ApplyRewards(JsonElement rawBlock)
        {
            var proposer = Cache.Accounts.GetDelegate(Block.ProposerId);
            var producer = Cache.Accounts.GetDelegate(Block.ProducerId);
            var burnAddress = await Cache.Accounts.GetAsync(BurnAddress.Address);
            var buffer = await Cache.Accounts.GetAsync(Proto10.ProtoActivator.BufferContract);
            var protocolTreasury = await Cache.Accounts.GetAsync(Proto20.ProtoActivator.ProtocolTreasuryContract);

            var balanceUpdates = rawBlock
                .Required("metadata")
                .RequiredArray("balance_updates")
                .EnumerateArray()
                .Where(x => x.RequiredString("origin") == "block")
                .ToList();

            var (
                rewardDelegated,
                rewardStakedOwn,
                rewardStakedEdge,
                rewardStakedShared,
                bonusDelegated,
                bonusStakedOwn,
                bonusStakedEdge,
                bonusStakedShared,
                feeProtocolTreasury,
                feeBurnAddress
            ) = ParseRewards(proposer, producer, balanceUpdates);

            Block.RewardDelegated = rewardDelegated;
            Block.RewardStakedOwn = rewardStakedOwn;
            Block.RewardStakedEdge = rewardStakedEdge;
            Block.RewardStakedShared = rewardStakedShared;
            Block.BonusDelegated = bonusDelegated;
            Block.BonusStakedOwn = bonusStakedOwn;
            Block.BonusStakedEdge = bonusStakedEdge;
            Block.BonusStakedShared = bonusStakedShared;

            Db.TryAttach(protocolTreasury);
            protocolTreasury.Balance += feeProtocolTreasury;

            Db.TryAttach(burnAddress);
            burnAddress.Balance += feeBurnAddress;

            Db.TryAttach(proposer);
            proposer.Balance += Block.RewardDelegated + Block.RewardStakedOwn + Block.RewardStakedEdge;
            proposer.StakingBalance += Block.RewardDelegated + Block.RewardStakedOwn + Block.RewardStakedEdge + Block.RewardStakedShared;
            proposer.OwnStakedBalance += Block.RewardStakedOwn + Block.RewardStakedEdge;
            proposer.ExternalStakedBalance += Block.RewardStakedShared;
            proposer.BlocksCount++;

            #region set baker active
            var newDeactivationLevel = proposer.Staked ? GracePeriod.Reset(Block) : GracePeriod.Init(Block);
            if (proposer.DeactivationLevel < newDeactivationLevel)
            {
                if (proposer.DeactivationLevel <= Block.Level)
                    await UpdateDelegate(proposer, true);

                Block.ResetBakerDeactivation = proposer.DeactivationLevel;
                proposer.DeactivationLevel = newDeactivationLevel;
            }
            #endregion

            Db.TryAttach(producer);
            producer.Balance += Block.BonusDelegated + Block.BonusStakedOwn + Block.BonusStakedEdge;
            producer.StakingBalance += Block.BonusDelegated + Block.BonusStakedOwn + Block.BonusStakedEdge + Block.BonusStakedShared;
            producer.OwnStakedBalance += Block.BonusStakedOwn + Block.BonusStakedEdge;
            producer.ExternalStakedBalance += Block.BonusStakedShared;
            if (producer != proposer)
            {
                producer.BlocksCount++;

                #region set proposer active
                newDeactivationLevel = producer.Staked ? GracePeriod.Reset(Block) : GracePeriod.Init(Block);
                if (producer.DeactivationLevel < newDeactivationLevel)
                {
                    if (producer.DeactivationLevel <= Block.Level)
                        await UpdateDelegate(producer, true);

                    Block.ResetProposerDeactivation = producer.DeactivationLevel;
                    producer.DeactivationLevel = newDeactivationLevel;
                }
                #endregion
            }

            Cache.Statistics.Current.TotalCreated +=
                Block.RewardDelegated + Block.RewardStakedOwn + Block.RewardStakedEdge + Block.RewardStakedShared +
                Block.BonusDelegated + Block.BonusStakedOwn + Block.BonusStakedEdge + Block.BonusStakedShared;

            Cache.Statistics.Current.TotalFrozen +=
                Block.RewardStakedOwn + Block.RewardStakedEdge + Block.RewardStakedShared +
                Block.BonusStakedOwn + Block.BonusStakedEdge + Block.BonusStakedShared;
        }

        
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
                        (nextUpdate.RequiredString("contract") == Proto20.ProtoActivator.ProtocolTreasuryContract || nextUpdate.RequiredString("contract") == Proto10.ProtoActivator.BufferContract) &&
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
