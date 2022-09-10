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
            var (deposit, reward) = ParseBalanceUpdates(metadata.RequiredArray("balance_updates"));

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

            var round = rawBlock.Required("header").RequiredInt32("priority");
            var baker = Cache.Accounts.GetDelegate(rawBlock.Required("metadata").RequiredString("baker"));
            Block = new Block
            {
                Id = Cache.AppState.NextOperationId(),
                Hash = rawBlock.RequiredString("hash"),
                Cycle = protocol.GetCycle(level),
                Level = level,
                Protocol = protocol,
                Timestamp = rawBlock.Required("header").RequiredDateTime("timestamp"),
                PayloadRound = round,
                BlockRound = round,
                Proposer = baker,
                ProposerId = baker.Id,
                ProducerId = baker.Id,
                Events = events,
                Reward = reward,
                Deposit = deposit,
                LBToggle = GetLBToggleVote(rawBlock),
                LBToggleEma = GetLBToggleEma(rawBlock)
            };

            #region entities
            var proto = Block.Protocol;
            Db.TryAttach(proto);
            Db.TryAttach(baker);
            #endregion

            baker.Balance += Block.Reward;
            baker.BlocksCount++;

            var newDeactivationLevel = baker.Staked ? GracePeriod.Reset(Block) : GracePeriod.Init(Block);
            if (baker.DeactivationLevel < newDeactivationLevel)
            {
                if (baker.DeactivationLevel <= Block.Level)
                    await UpdateDelegate(baker, true);

                Block.ResetBakerDeactivation = baker.DeactivationLevel;
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
            Block.Proposer ??= Cache.Accounts.GetDelegate(block.ProposerId);

            #region entities
            var proto = Block.Protocol;
            var baker = Block.Proposer;

            Db.TryAttach(proto);
            Db.TryAttach(baker);
            #endregion

            baker.Balance -= Block.Reward;
            baker.BlocksCount--;

            if (Block.ResetBakerDeactivation != null)
            {
                if (Block.ResetBakerDeactivation <= Block.Level)
                    await UpdateDelegate(baker, false);

                baker.DeactivationLevel = (int)Block.ResetBakerDeactivation;
            }

            Db.Blocks.Remove(Block);
            Cache.AppState.ReleaseOperationId();
        }

        protected virtual (long, long) ParseBalanceUpdates(JsonElement balanceUpdates)
        {
            var deposit = 0L;
            var reward = 0L;
            foreach (var bu in balanceUpdates.EnumerateArray().Take(3))
            {
                if (bu.RequiredString("kind")[0] == 'f')
                {
                    var change = bu.RequiredInt64("change");
                    if (change > 0)
                    {
                        if (bu.RequiredString("category")[0] == 'd')
                            deposit = change;
                        else
                            reward = change;
                    }

                }
            }
            return (deposit, reward);
        }

        protected virtual bool? GetLBToggleVote(JsonElement block) => null;

        protected virtual int GetLBToggleEma(JsonElement block) => 0;
    }
}
