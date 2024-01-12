using System.Numerics;
using System.Text.Json;
using Netezos.Encoding;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto18
{
    class BlockCommit : ProtocolCommit
    {
        public Block Block { get; private set; }

        public BlockCommit(ProtocolHandler protocol) : base(protocol) { }

        public virtual async Task Apply(JsonElement rawBlock)
        {
            var header = rawBlock.Required("header");
            var metadata = rawBlock.Required("metadata");

            var level = header.RequiredInt32("level");
            var proposer = Cache.Accounts.GetDelegate(metadata.RequiredString("proposer"));
            var producer = Cache.Accounts.GetDelegate(metadata.RequiredString("baker"));
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

            if (level % protocol.BlocksPerSnapshot == 0)
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
                Protocol = protocol,
                Timestamp = header.RequiredDateTime("timestamp"),
                PayloadRound = payloadRound,
                BlockRound = blockRound,
                Proposer = proposer,
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

            var state = Cache.AppState.Get();
            if (!state.AIActivated && metadata.OptionalInt32("adaptive_issuance_activation_cycle") is int aiActivationCycle)
            {
                state.AIActivated = true;
                state.AIActivationCycle = aiActivationCycle;
                state.AIFinalUpvoteLevel = Block.Level;
            }

            Db.TryAttach(proposer); // if we don't attach it, ef will recognize it as 'added'
            Db.TryAttach(producer); // if we don't attach it, ef will recognize it as 'added'

            Db.Blocks.Add(Block);
            Cache.Blocks.Add(Block);
        }

        public async Task ApplyRewards(JsonElement rawBlock)
        {
            var proposer = Cache.Accounts.GetDelegate(Block.ProposerId);
            var producer = Cache.Accounts.GetDelegate(Block.ProducerId);

            var balanceUpdates = rawBlock
                .Required("metadata")
                .RequiredArray("balance_updates")
                .EnumerateArray()
                .Where(x => x.RequiredString("origin") == "block")
                .ToList();

            #region parse rewards
            var rewardLiquid = 0L;
            var rewardStakedOwn = 0L;
            var rewardStakedShared = 0L;
            var bonusLiquid = 0L;
            var bonusStakedOwn = 0L;
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
                    if (nextUpdate.RequiredString("kind") == "freezer" &&
                        nextUpdate.RequiredString("category") == "deposits" &&
                        nextUpdate.Required("staker").RequiredString("baker") == proposer.Address &&
                        nextUpdate.RequiredInt64("change") == change)
                    {
                        var changeOwn = (long)((BigInteger)change * proposer.StakedBalance / proposer.TotalStakedBalance);
                        var changeShared = change - changeOwn;
                        rewardStakedOwn += changeOwn;
                        rewardStakedShared += changeShared;
                    }
                    else if (nextUpdate.RequiredString("kind") == "contract" &&
                        nextUpdate.RequiredString("contract") == proposer.Address &&
                        nextUpdate.RequiredInt64("change") == change)
                    {
                        rewardLiquid += change;
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
                        var changeOwn = (long)((BigInteger)change * producer.StakedBalance / producer.TotalStakedBalance);
                        var changeShared = change - changeOwn;
                        bonusStakedOwn += changeOwn;
                        bonusStakedShared += changeShared;
                    }
                    else if (nextUpdate.RequiredString("kind") == "contract" &&
                        nextUpdate.RequiredString("contract") == producer.Address &&
                        nextUpdate.RequiredInt64("change") == change)
                    {
                        bonusLiquid += change;
                    }
                    else
                    {
                        throw new Exception("Unexpected baking bonuses balance updates behavior");
                    }
                }
            }
            #endregion

            Block.RewardLiquid = rewardLiquid;
            Block.RewardStakedOwn = rewardStakedOwn;
            Block.RewardStakedShared = rewardStakedShared;
            Block.BonusLiquid = bonusLiquid;
            Block.BonusStakedOwn = bonusStakedOwn;
            Block.BonusStakedShared = bonusStakedShared;

            Db.TryAttach(proposer);
            proposer.Balance += Block.RewardLiquid + Block.RewardStakedOwn;
            proposer.StakingBalance += Block.RewardLiquid + Block.RewardStakedOwn + Block.RewardStakedShared;
            proposer.StakedBalance += Block.RewardStakedOwn;
            proposer.ExternalStakedBalance += Block.RewardStakedShared;
            proposer.TotalStakedBalance += Block.RewardStakedOwn + Block.RewardStakedShared;
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
            producer.Balance += Block.BonusLiquid + Block.BonusStakedOwn;
            producer.StakingBalance += Block.BonusLiquid + Block.BonusStakedOwn + Block.BonusStakedShared;
            producer.StakedBalance += Block.BonusStakedOwn;
            producer.ExternalStakedBalance += Block.BonusStakedShared;
            producer.TotalStakedBalance += Block.BonusStakedOwn + Block.BonusStakedShared;
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
                Block.RewardLiquid + Block.RewardStakedOwn + Block.RewardStakedShared +
                Block.BonusLiquid + Block.BonusStakedOwn + Block.BonusStakedShared;

            Cache.Statistics.Current.TotalFrozen +=
                Block.RewardStakedOwn + Block.RewardStakedShared +
                Block.BonusStakedOwn + Block.BonusStakedShared;
        }

        public virtual void Revert(Block block)
        {
            var state = Cache.AppState.Get();
            if (state.AIFinalUpvoteLevel == block.Level)
            {
                state.AIActivated = false;
                state.AIActivationCycle = 0;
                state.AIFinalUpvoteLevel = 0;
            }

            Db.Blocks.Remove(block);
            Cache.AppState.ReleaseOperationId();
        }

        public async Task RevertRewards(Block block)
        {
            var proposer = Cache.Accounts.GetDelegate(block.ProposerId);
            Db.TryAttach(proposer);
            proposer.Balance -= block.RewardLiquid + block.RewardStakedOwn;
            proposer.StakingBalance -= block.RewardLiquid + block.RewardStakedOwn + block.RewardStakedShared;
            proposer.StakedBalance -= block.RewardStakedOwn;
            proposer.ExternalStakedBalance -= block.RewardStakedShared;
            proposer.TotalStakedBalance -= block.RewardStakedOwn + block.RewardStakedShared;
            proposer.BlocksCount--;

            #region reset baker activity
            if (block.ResetBakerDeactivation != null)
            {
                if (block.ResetBakerDeactivation <= block.Level)
                    await UpdateDelegate(proposer, false);

                proposer.DeactivationLevel = (int)block.ResetBakerDeactivation;
            }
            #endregion

            var producer = Cache.Accounts.GetDelegate(block.ProducerId);
            Db.TryAttach(producer);
            producer.Balance -= block.BonusLiquid + block.BonusStakedOwn;
            producer.StakingBalance -= block.BonusLiquid + block.BonusStakedOwn + block.BonusStakedShared;
            producer.StakedBalance -= block.BonusStakedOwn;
            producer.ExternalStakedBalance -= block.BonusStakedShared;
            producer.TotalStakedBalance -= block.BonusStakedOwn + block.BonusStakedShared;
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
    }
}
