using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto1
{
    class BlockCommit : ProtocolCommit
    {
        public Block Block { get; private set; }

        public BlockCommit(ProtocolHandler protocol) : base(protocol) { }

        public virtual async Task Apply(JsonElement rawBlock)
        {
            var level = rawBlock.Required("header").RequiredInt32("level");
            var protocol = await Cache.Protocols.GetAsync(rawBlock.RequiredString("protocol"));
            var events = BlockEvents.None;

            var metadata = rawBlock.Required("metadata");
            var reward = GetBlockReward(metadata);
            var deposit = GetBlockDeposit(metadata);

            if (protocol.IsCycleStart(level))
                events |= BlockEvents.CycleBegin;
            else if (protocol.IsCycleEnd(level))
                events |= BlockEvents.CycleEnd;

            if (protocol.FirstLevel == level)
                events |= BlockEvents.ProtocolBegin;
            else if (metadata.RequiredString("protocol") != metadata.RequiredString("next_protocol"))
                events |= BlockEvents.ProtocolEnd;

            if (metadata.RequiredArray("deactivated").Count() > 0)
                events |= BlockEvents.Deactivations;

            if (level % protocol.BlocksPerSnapshot == 0)
                events |= BlockEvents.BalanceSnapshot;

            Block = new Block
            {
                Id = Cache.AppState.NextOperationId(),
                Hash = rawBlock.RequiredString("hash"),
                Cycle = protocol.GetCycle(level),
                Level = level,
                Protocol = protocol,
                Timestamp = rawBlock.Required("header").RequiredDateTime("timestamp"),
                Priority = rawBlock.Required("header").RequiredInt32("priority"),
                Baker = Cache.Accounts.GetDelegate(rawBlock.Required("metadata").RequiredString("baker")),
                Events = events,
                Reward = reward.ValueKind != JsonValueKind.Undefined ? reward.RequiredInt64("change") : 0,
                Deposit = deposit.ValueKind != JsonValueKind.Undefined ? deposit.RequiredInt64("change") : 0
            };

            #region entities
            var proto = Block.Protocol;
            var baker = Block.Baker;

            Db.TryAttach(proto);
            Db.TryAttach(baker);
            #endregion

            baker.Balance += Block.Reward;
            baker.FrozenRewards += Block.Reward;
            baker.FrozenDeposits += Block.Deposit;
            baker.BlocksCount++;

            var newDeactivationLevel = baker.Staked ? GracePeriod.Reset(Block) : GracePeriod.Init(Block);
            if (baker.DeactivationLevel < newDeactivationLevel)
            {
                if (baker.DeactivationLevel <= Block.Level)
                    await UpdateDelegate(baker, true);

                Block.ResetDeactivation = baker.DeactivationLevel;
                baker.DeactivationLevel = newDeactivationLevel;
            }

            if (Block.Events.HasFlag(BlockEvents.ProtocolEnd))
                proto.LastLevel = Block.Level;

            Db.Blocks.Add(Block);
            Cache.Blocks.Add(Block);
        }

        public virtual async Task Revert(Block block)
        {
            Block = block;
            Block.Protocol ??= await Cache.Protocols.GetAsync(block.ProtoCode);
            Block.Baker ??= Cache.Accounts.GetDelegate(block.BakerId);

            #region entities
            var proto = Block.Protocol;
            var baker = Block.Baker;

            Db.TryAttach(proto);
            Db.TryAttach(baker);
            #endregion

            baker.Balance -= Block.Reward;
            baker.FrozenRewards -= Block.Reward;
            baker.FrozenDeposits -= Block.Deposit;
            baker.BlocksCount--;

            if (Block.ResetDeactivation != null)
            {
                if (Block.ResetDeactivation <= Block.Level)
                    await UpdateDelegate(baker, false);

                baker.DeactivationLevel = (int)Block.ResetDeactivation;
            }

            Db.Blocks.Remove(Block);
        }

        protected virtual JsonElement GetBlockReward(JsonElement metadata)
        {
            return metadata
                .RequiredArray("balance_updates")
                .EnumerateArray()
                .Take(3)
                .FirstOrDefault(x => x.RequiredString("kind")[0] == 'f' && x.RequiredString("category")[0] == 'r');
        }

        protected virtual JsonElement GetBlockDeposit(JsonElement metadata)
        {
            return metadata
                .RequiredArray("balance_updates")
                .EnumerateArray()
                .Take(3)
                .FirstOrDefault(x => x.RequiredString("kind")[0] == 'f' && x.RequiredString("category")[0] == 'd');
        }
    }
}
