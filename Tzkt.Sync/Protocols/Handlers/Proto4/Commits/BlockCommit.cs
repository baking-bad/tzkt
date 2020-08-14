using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models;
using Tzkt.Sync.Utils;

namespace Tzkt.Sync.Protocols.Proto4
{
    class BlockCommit : ProtocolCommit
    {
        public Block Block { get; private set; }

        BlockCommit(ProtocolHandler protocol) : base(protocol) { }

        public async Task Init(RawBlock rawBlock)
        {
            var protocol = await Cache.Protocols.GetAsync(rawBlock.Protocol);
            var votingPeriod = await Cache.Periods.CurrentAsync();
            var events = BlockEvents.None;

            if (rawBlock.Level % protocol.BlocksPerCycle == 1)
                events |= BlockEvents.CycleBegin;
            else if (rawBlock.Level % protocol.BlocksPerCycle == 0)
                events |= BlockEvents.CycleEnd;

            if (protocol.FirstLevel == rawBlock.Level)
                events |= BlockEvents.ProtocolBegin;
            else if (rawBlock.Metadata.Protocol != rawBlock.Metadata.NextProtocol)
                events |= BlockEvents.ProtocolEnd;

            if (rawBlock.Level == votingPeriod.EndLevel)
                events |= BlockEvents.VotingPeriodEnd;
            else if (rawBlock.Level > votingPeriod.EndLevel)
                events |= BlockEvents.VotingPeriodBegin;

            if (rawBlock.Metadata.Deactivated.Count > 0)
                events |= BlockEvents.Deactivations;

            if (rawBlock.Level % protocol.BlocksPerSnapshot == 0)
                events |= BlockEvents.Snapshot;

            Block = new Block
            {
                Id = Cache.AppState.NextOperationId(),
                Hash = rawBlock.Hash,
                Level = rawBlock.Level,
                Protocol = protocol,
                Timestamp = rawBlock.Header.Timestamp,
                Priority = rawBlock.Header.Priority,
                Baker = Cache.Accounts.GetDelegate(rawBlock.Metadata.Baker),
                Events = events,
                Reward = protocol.BlockReward0,
                Version = BitConverter.ToInt32(Hex.Parse(rawBlock.Header.PowNonce.Substring(0, 8)))
            };
        }

        public async Task Init(Block block)
        {
            Block = block;
            Block.Protocol ??= await Cache.Protocols.GetAsync(block.ProtoCode);
            Block.Baker ??= Cache.Accounts.GetDelegate(block.BakerId);
        }

        public override async Task Apply()
        {
            #region entities
            var block = Block;
            var proto = Block.Protocol;
            var baker = Block.Baker;

            Db.TryAttach(proto);
            Db.TryAttach(baker);
            #endregion
            
            baker.Balance += Block.Reward;
            baker.FrozenRewards += Block.Reward;
            baker.FrozenDeposits += Block.Protocol.BlockDeposit;
            baker.BlocksCount++;
            baker.Version = Block.Version;

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

        public override async Task Revert()
        {
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

        async Task UpdateDelegate(Data.Models.Delegate delegat, bool staked)
        {
            delegat.Staked = staked;

            foreach (var delegator in await Db.Accounts.Where(x => x.DelegateId == delegat.Id).ToListAsync())
            {
                Cache.Accounts.Add(delegator);
                Db.TryAttach(delegator);

                delegator.Staked = staked;
            }
        }

        #region static
        public static async Task<BlockCommit> Apply(ProtocolHandler proto, RawBlock rawBlock)
        {
            var commit = new BlockCommit(proto);
            await commit.Init(rawBlock);
            await commit.Apply();

            return commit;
        }

        public static async Task<BlockCommit> Revert(ProtocolHandler proto, Block block)
        {
            var commit = new BlockCommit(proto);
            await commit.Init(block);
            await commit.Revert();

            return commit;
        }
        #endregion
    }
}
