using System.Text.Json;
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
                ProtoCode = protocol.Code,
                Timestamp = rawBlock.Required("header").RequiredDateTime("timestamp"),
                PayloadRound = round,
                BlockRound = round,
                ProposerId = baker.Id,
                ProducerId = baker.Id,
                Events = events,
                RewardDelegated = reward,
                Deposit = deposit,
                LBToggle = GetLBToggleVote(rawBlock),
                LBToggleEma = GetLBToggleEma(rawBlock)
            };

            #region entities
            Db.TryAttach(protocol);
            Db.TryAttach(baker);
            #endregion

            baker.Balance += Block.RewardDelegated;
            baker.BlocksCount++;

            var newDeactivationLevel = baker.Staked ? GracePeriod.Reset(Block.Level, protocol) : GracePeriod.Init(Block.Level, protocol);
            if (baker.DeactivationLevel < newDeactivationLevel)
            {
                if (baker.DeactivationLevel <= Block.Level)
                    await UpdateDelegate(baker, true);

                Block.ResetBakerDeactivation = baker.DeactivationLevel;
                baker.DeactivationLevel = newDeactivationLevel;
            }

            if (Block.Events.HasFlag(BlockEvents.ProtocolEnd))
                protocol.LastLevel = Block.Level;

            Cache.AppState.Get().BlocksCount++;
            Cache.Statistics.Current.TotalCreated += Block.RewardDelegated;
            Cache.Statistics.Current.TotalFrozen += Block.RewardDelegated + Block.Deposit + Block.Fees;

            Db.Blocks.Add(Block);
            Cache.Blocks.Add(Block);

            Context.Block = Block;
            Context.Proposer = baker;
            Context.Protocol = protocol;
        }

        public virtual async Task Revert(Block block)
        {
            Block = block;

            #region entities
            var baker = Cache.Accounts.GetDelegate(block.ProposerId);
            Db.TryAttach(baker);
            #endregion

            baker.Balance -= Block.RewardDelegated;
            baker.BlocksCount--;

            if (Block.ResetBakerDeactivation != null)
            {
                if (Block.ResetBakerDeactivation <= Block.Level)
                    await UpdateDelegate(baker, false);

                baker.DeactivationLevel = (int)Block.ResetBakerDeactivation;
            }

            Cache.AppState.Get().BlocksCount--;

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
