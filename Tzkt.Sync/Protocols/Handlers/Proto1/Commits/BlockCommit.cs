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
            var votingPeriod = await Cache.Periods.CurrentAsync();
            var events = BlockEvents.None;

            var metadata = rawBlock.Required("metadata");
            var reward = metadata
                    .RequiredArray("balance_updates")
                    .EnumerateArray()
                    .Take(3)
                    .FirstOrDefault(x => x.RequiredString("kind")[0] == 'f' && x.RequiredString("category")[0] == 'r');

            if (level % protocol.BlocksPerCycle == 1)
                events |= BlockEvents.CycleBegin;
            else if (level % protocol.BlocksPerCycle == 0)
                events |= BlockEvents.CycleEnd;

            if (protocol.FirstLevel == level)
                events |= BlockEvents.ProtocolBegin;
            else if (metadata.RequiredString("protocol") != metadata.RequiredString("next_protocol"))
                events |= BlockEvents.ProtocolEnd;

            if (level == votingPeriod.EndLevel)
                events |= BlockEvents.VotingPeriodEnd;
            else if (level > votingPeriod.EndLevel)
                events |= BlockEvents.VotingPeriodBegin;

            if (metadata.RequiredArray("deactivated").Count() > 0)
                events |= BlockEvents.Deactivations;

            if (level % protocol.BlocksPerSnapshot == 0)
                events |= BlockEvents.Snapshot;

            Block = new Block
            {
                Id = Cache.AppState.NextOperationId(),
                Hash = rawBlock.RequiredString("hash"),
                Level = level,
                Protocol = protocol,
                Timestamp = rawBlock.Required("header").RequiredDateTime("timestamp"),
                Priority = rawBlock.Required("header").RequiredInt32("priority"),
                Baker = Cache.Accounts.GetDelegate(rawBlock.Required("metadata").RequiredString("baker")),
                Events = events,
                Reward = reward.ValueKind != JsonValueKind.Undefined ? reward.RequiredInt64("change") : 0
            };

            #region entities
            var proto = Block.Protocol;
            var baker = Block.Baker;

            Db.TryAttach(proto);
            Db.TryAttach(baker);
            #endregion

            baker.Balance += Block.Reward;
            baker.FrozenRewards += Block.Reward;
            baker.FrozenDeposits += Block.Protocol.BlockDeposit;
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
            baker.FrozenDeposits -= Block.Protocol.BlockDeposit;
            baker.BlocksCount--;

            if (Block.Events.HasFlag(BlockEvents.ProtocolBegin))
            {
                Db.Protocols.Remove(proto);
                Cache.Protocols.Remove(proto);
            }
            else if (Block.Events.HasFlag(BlockEvents.ProtocolEnd))
            {
                proto.LastLevel = -1;
            }

            if (Block.ResetDeactivation != null)
            {
                if (Block.ResetDeactivation <= Block.Level)
                    await UpdateDelegate(baker, false);

                baker.DeactivationLevel = (int)Block.ResetDeactivation;
            }

            Db.Blocks.Remove(Block);
        }

        public override Task Apply() => Task.CompletedTask;
        public override Task Revert() => Task.CompletedTask;
    }
}
