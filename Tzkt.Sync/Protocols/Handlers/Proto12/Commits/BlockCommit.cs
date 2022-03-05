using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto12
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
            var baker = Cache.Accounts.GetDelegate(metadata.RequiredString("baker"));
            var proposer = Cache.Accounts.GetDelegate(metadata.RequiredString("proposer"));
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

            var balanceUpdates = metadata.RequiredArray("balance_updates").EnumerateArray();
            var rewardUpdate = balanceUpdates.FirstOrDefault(x => x.RequiredString("kind") == "minted" && x.RequiredString("category") == "baking rewards");
            var bonusUpdate = balanceUpdates.FirstOrDefault(x => x.RequiredString("kind") == "minted" && x.RequiredString("category") == "baking bonuses");

            Block = new Block
            {
                Id = Cache.AppState.NextOperationId(),
                Hash = rawBlock.RequiredString("hash"), 
                Cycle = protocol.GetCycle(level),
                Level = level,
                ProtoCode = protocol.Code,
                Protocol = protocol,
                Timestamp = header.RequiredDateTime("timestamp"),
                Priority = header.RequiredInt32("payload_round"),
                Baker = baker,
                BakerId = baker.Id,
                ProposerId = proposer.Id,
                Events = events,
                Reward = rewardUpdate.ValueKind == JsonValueKind.Undefined ? 0 : -rewardUpdate.RequiredInt64("change"),
                Bonus = bonusUpdate.ValueKind == JsonValueKind.Undefined ? 0 : -bonusUpdate.RequiredInt64("change"),
                LBEscapeVote = header.RequiredBool("liquidity_baking_escape_vote"),
                LBEscapeEma = metadata.RequiredInt32("liquidity_baking_escape_ema")
            };

            Db.TryAttach(baker);
            baker.Balance += Block.Reward;
            baker.StakingBalance += Block.Reward;
            baker.BlocksCount++;

            #region set baker active
            var newDeactivationLevel = baker.Staked ? GracePeriod.Reset(Block) : GracePeriod.Init(Block);
            if (baker.DeactivationLevel < newDeactivationLevel)
            {
                if (baker.DeactivationLevel <= Block.Level)
                    await UpdateDelegate(baker, true);

                Block.ResetBakerDeactivation = baker.DeactivationLevel;
                baker.DeactivationLevel = newDeactivationLevel;
            }
            #endregion

            Db.TryAttach(proposer);
            proposer.Balance += Block.Bonus;
            proposer.StakingBalance += Block.Bonus;
            if (proposer.Id != baker.Id)
            {
                proposer.BlocksCount++;

                #region set proposer active
                newDeactivationLevel = proposer.Staked ? GracePeriod.Reset(Block) : GracePeriod.Init(Block);
                if (proposer.DeactivationLevel < newDeactivationLevel)
                {
                    if (proposer.DeactivationLevel <= Block.Level)
                        await UpdateDelegate(proposer, true);

                    Block.ResetProposerDeactivation = proposer.DeactivationLevel;
                    proposer.DeactivationLevel = newDeactivationLevel;
                }
                #endregion
            }

            if (Block.Events.HasFlag(BlockEvents.ProtocolEnd))
            {
                Db.TryAttach(protocol);
                protocol.LastLevel = Block.Level;
            }

            Db.Blocks.Add(Block);
            Cache.Blocks.Add(Block);
        }

        public virtual async Task Revert(Block block)
        {
            Block = block;
            Block.Protocol ??= await Cache.Protocols.GetAsync(block.ProtoCode);
            Block.Baker ??= Cache.Accounts.GetDelegate(block.BakerId);
            
            var baker = Block.Baker;
            Db.TryAttach(baker);
            baker.Balance -= Block.Reward;
            baker.StakingBalance -= Block.Reward;
            baker.BlocksCount--;

            #region reset baker activity
            if (Block.ResetBakerDeactivation != null)
            {
                if (Block.ResetBakerDeactivation <= Block.Level)
                    await UpdateDelegate(baker, false);

                baker.DeactivationLevel = (int)Block.ResetBakerDeactivation;
            }
            #endregion

            var proposer = Cache.Accounts.GetDelegate(block.ProposerId);
            Db.TryAttach(proposer);
            proposer.Balance -= Block.Bonus;
            proposer.StakingBalance -= Block.Bonus;
            if (proposer.Id != baker.Id)
            {
                proposer.BlocksCount--;

                #region reset proposer activity
                if (Block.ResetProposerDeactivation != null)
                {
                    if (Block.ResetProposerDeactivation <= Block.Level)
                        await UpdateDelegate(proposer, false);

                    proposer.DeactivationLevel = (int)Block.ResetProposerDeactivation;
                }
                #endregion
            }

            Db.Blocks.Remove(Block);
        }
    }
}
