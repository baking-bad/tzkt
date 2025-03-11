using System.Text.Json;
using Netezos.Encoding;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto18
{
    class BlockCommit(ProtocolHandler protocol) : ProtocolCommit(protocol)
    {
        public Block Block { get; private set; } = null!;

        public virtual async Task Apply(JsonElement rawBlock)
        {
            var header = rawBlock.Required("header");
            var metadata = rawBlock.Required("metadata");

            var level = header.RequiredInt32("level");
            var proposer = Cache.Accounts.GetExistingDelegate(metadata.RequiredString("proposer"));
            var producer = Cache.Accounts.GetExistingDelegate(metadata.RequiredString("baker"));
            var protocol = await Cache.Protocols.GetAsync(rawBlock.RequiredString("protocol"));
            var events = BlockEvents.None;

            if (protocol.IsCycleStart(level))
                events |= BlockEvents.CycleBegin;
            else if (protocol.IsCycleEnd(level))
                events |= BlockEvents.CycleEnd;

            if (protocol.FirstLevel == level)
                events |= BlockEvents.ProtocolBegin;
            else if (protocol.Hash != metadata.RequiredString("next_protocol"))
                events |= BlockEvents.ProtocolEnd;

            if (metadata.RequiredArray("deactivated").Count() > 0)
                events |= BlockEvents.Deactivations;

            if ((level - Cache.Protocols.GetCycleStart(protocol.GetCycle(level)) + 1) % protocol.BlocksPerSnapshot == 0)
                events |= BlockEvents.BalanceSnapshot;

            var payloadRound = header.RequiredInt32("payload_round");
            var blockRound = Hex.Parse(header.RequiredArray("fitness", 5)[4].RequiredString()).ToInt32();
            var lbVote = header.RequiredString("liquidity_baking_toggle_vote");
            var aiVote = header.RequiredString("adaptive_issuance_vote");

            Block = new Block
            {
                Id = Cache.AppState.NextOperationId(),
                Hash = rawBlock.RequiredString("hash"),
                Cycle = protocol.GetCycle(level),
                Level = level,
                ProtoCode = protocol.Code,
                Timestamp = header.RequiredDateTime("timestamp"),
                PayloadRound = payloadRound,
                BlockRound = blockRound,
                ProposerId = proposer.Id,
                ProducerId = producer.Id,
                Events = events,
                LBToggle = lbVote == "on" ? true : lbVote == "off" ? false : null,
                LBToggleEma = metadata.RequiredInt32("liquidity_baking_toggle_ema"),
                AIToggle = aiVote == "on" ? true : aiVote == "off" ? false : null,
                AIToggleEma = metadata.RequiredInt32("adaptive_issuance_vote_ema")
            };

            Db.TryAttach(protocol); // if we don't attach it, ef will recognize it as 'added'
            if (Block.Events.HasFlag(BlockEvents.ProtocolEnd))
                protocol.LastLevel = Block.Level;

            Db.TryAttach(proposer); // if we don't attach it, ef will recognize it as 'added'
            Db.TryAttach(producer); // if we don't attach it, ef will recognize it as 'added'

            Cache.AppState.Get().BlocksCount++;

            Db.Blocks.Add(Block);
            Cache.Blocks.Add(Block);

            Context.Block = Block;
            Context.Proposer = proposer;
            Context.Protocol = protocol;
        }
        
        public async Task ApplyRewards(JsonElement rawBlock)
        {
            var proposer = Cache.Accounts.GetDelegate(Block.ProposerId!.Value);
            var producer = Cache.Accounts.GetDelegate(Block.ProducerId!.Value);

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
                bonusStakedShared
            ) = ParseRewards(proposer, producer, balanceUpdates);

            Block.RewardDelegated = rewardDelegated;
            Block.RewardStakedOwn = rewardStakedOwn;
            Block.RewardStakedEdge = rewardStakedEdge;
            Block.RewardStakedShared = rewardStakedShared;
            Block.BonusDelegated = bonusDelegated;
            Block.BonusStakedOwn = bonusStakedOwn;
            Block.BonusStakedEdge = bonusStakedEdge;
            Block.BonusStakedShared = bonusStakedShared;

            Db.TryAttach(proposer);
            proposer.Balance += Block.RewardDelegated + Block.RewardStakedOwn + Block.RewardStakedEdge;
            proposer.StakingBalance += Block.RewardDelegated + Block.RewardStakedOwn + Block.RewardStakedEdge + Block.RewardStakedShared;
            proposer.OwnStakedBalance += Block.RewardStakedOwn + Block.RewardStakedEdge;
            proposer.ExternalStakedBalance += Block.RewardStakedShared;
            proposer.BlocksCount++;

            #region set baker active
            var newDeactivationLevel = proposer.Staked ? GracePeriod.Reset(Block.Level, Context.Protocol) : GracePeriod.Init(Block.Level, Context.Protocol);
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
                newDeactivationLevel = producer.Staked ? GracePeriod.Reset(Block.Level, Context.Protocol) : GracePeriod.Init(Block.Level, Context.Protocol);
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

        public virtual void Revert(Block block)
        {
            Cache.AppState.Get().BlocksCount--;

            Db.Blocks.Remove(block);
            Cache.AppState.ReleaseOperationId();
        }

        public async Task RevertRewards(Block block)
        {
            var proposer = Cache.Accounts.GetDelegate(block.ProposerId!.Value);
            Db.TryAttach(proposer);
            proposer.Balance -= block.RewardDelegated + block.RewardStakedOwn + block.RewardStakedEdge;
            proposer.StakingBalance -= block.RewardDelegated + block.RewardStakedOwn + block.RewardStakedEdge + block.RewardStakedShared;
            proposer.OwnStakedBalance -= block.RewardStakedOwn + block.RewardStakedEdge;
            proposer.ExternalStakedBalance -= block.RewardStakedShared;
            proposer.BlocksCount--;

            #region reset baker activity
            if (block.ResetBakerDeactivation != null)
            {
                if (block.ResetBakerDeactivation <= block.Level)
                    await UpdateDelegate(proposer, false);

                proposer.DeactivationLevel = (int)block.ResetBakerDeactivation;
            }
            #endregion

            var producer = Cache.Accounts.GetDelegate(block.ProducerId!.Value);
            Db.TryAttach(producer);
            producer.Balance -= block.BonusDelegated + block.BonusStakedOwn + block.BonusStakedEdge;
            producer.StakingBalance -= block.BonusDelegated + block.BonusStakedOwn + block.BonusStakedEdge + block.BonusStakedShared;
            producer.OwnStakedBalance -= block.BonusStakedOwn + block.BonusStakedEdge;
            producer.ExternalStakedBalance -= block.BonusStakedShared;
            if (producer != proposer)
            {
                producer.BlocksCount--;

                #region reset proposer activity
                if (block.ResetProposerDeactivation != null)
                {
                    if (block.ResetProposerDeactivation <= block.Level)
                        await UpdateDelegate(producer, false);

                    producer.DeactivationLevel = (int)block.ResetProposerDeactivation;
                }
                #endregion
            }
        }

        protected virtual (long, long, long, long, long, long, long, long) ParseRewards(Data.Models.Delegate proposer, Data.Models.Delegate producer, List<JsonElement> balanceUpdates)
        {
            var rewardDelegated = 0L;
            var rewardStakedOwn = 0L;
            var bonusDelegated = 0L;
            var bonusStakedOwn = 0L;

            for (int i = 0; i < balanceUpdates.Count; i++)
            {
                var update = balanceUpdates[i];
                if (update.RequiredString("kind") == "minted" && update.RequiredString("category") == "baking rewards")
                {
                    if (i == balanceUpdates.Count - 1)
                        throw new Exception("Unexpected baking rewards balance updates behavior");

                    var change = -update.RequiredInt64("change");

                    var nextUpdate = balanceUpdates[i + 1];
                    if (nextUpdate.RequiredString("kind") == "freezer" &&
                        nextUpdate.RequiredString("category") == "deposits" &&
                        nextUpdate.Required("staker").RequiredString("baker") == proposer.Address &&
                        nextUpdate.RequiredInt64("change") == change)
                    {
                        if (proposer.ExternalStakedBalance != 0)
                            throw new Exception("Manual staking should be disabled in Oxford");

                        rewardStakedOwn += change;
                    }
                    else if (nextUpdate.RequiredString("kind") == "contract" &&
                        nextUpdate.RequiredString("contract") == proposer.Address &&
                        nextUpdate.RequiredInt64("change") == change)
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
                    if (nextUpdate.RequiredString("kind") == "freezer" &&
                        nextUpdate.RequiredString("category") == "deposits" &&
                        nextUpdate.Required("staker").RequiredString("baker") == producer.Address &&
                        nextUpdate.RequiredInt64("change") == change)
                    {
                        if (producer.ExternalStakedBalance != 0)
                            throw new Exception("Manual staking should be disabled in Oxford");

                        bonusStakedOwn += change;
                    }
                    else if (nextUpdate.RequiredString("kind") == "contract" &&
                        nextUpdate.RequiredString("contract") == producer.Address &&
                        nextUpdate.RequiredInt64("change") == change)
                    {
                        bonusDelegated += change;
                    }
                    else
                    {
                        throw new Exception("Unexpected baking bonuses balance updates behavior");
                    }
                }
            }

            return (rewardDelegated, rewardStakedOwn, 0L, 0L, bonusDelegated, bonusStakedOwn, 0L, 0L);
        }
    }
}
