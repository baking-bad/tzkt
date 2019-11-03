using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto3
{
    class DeactivationCommit : ProtocolCommit
    {
        public Block Block { get; private set; }
        public List<Data.Models.Delegate> Delegates { get; private set; }
        public int DeactivationLevel { get; private set; }

        DeactivationCommit(ProtocolHandler protocol) : base(protocol) { }

        public async Task Init(Block block, RawBlock rawBlock)
        {
            if (block.Events.HasFlag(BlockEvents.Deactivations))
            {
                DeactivationLevel = rawBlock.Level;
                Delegates = await Db.Delegates
                    .Include(x => x.DelegatedAccounts)
                    .Where(x => x.Staked && rawBlock.Metadata.Deactivated.Contains(x.Address))
                    .ToListAsync();
            }
            else if (block.Events.HasFlag(BlockEvents.CycleBegin))
            {
                DeactivationLevel = rawBlock.Level;
                Delegates = await Db.Delegates
                    .Include(x => x.DelegatedAccounts)
                    .Where(x => x.Staked && x.DeactivationLevel == rawBlock.Level)
                    .ToListAsync();
            }
        }

        public async Task Init(Block block)
        {
            if (block.Events.HasFlag(BlockEvents.Deactivations) || block.Events.HasFlag(BlockEvents.CycleBegin))
            {
                Block = block;
                DeactivationLevel = block.Level;
                Delegates = await Db.Delegates
                    .Include(x => x.DelegatedAccounts)
                    .Where(x => x.DeactivationLevel == block.Level)
                    .ToListAsync();
            }
        }

        public override Task Apply()
        {
            if (Delegates == null) return Task.CompletedTask;

            foreach (var delegat in Delegates)
            {
                Cache.AddAccount(delegat);
                Db.TryAttach(delegat);

                delegat.DeactivationLevel = DeactivationLevel;
                delegat.Staked = false;

                foreach (var delegator in delegat.DelegatedAccounts)
                {
                    Cache.AddAccount(delegator);
                    Db.TryAttach(delegator);

                    delegator.Staked = false;
                }
            }

            return Task.CompletedTask;
        }

        public override Task Revert()
        {
            if (Delegates == null) return Task.CompletedTask;

            foreach (var delegat in Delegates)
            {
                Cache.AddAccount(delegat);
                Db.TryAttach(delegat);

                delegat.DeactivationLevel = Block.Events.HasFlag(BlockEvents.CycleEnd)
                    ? DeactivationLevel + 1
                    : DeactivationLevel;

                delegat.Staked = true;

                foreach (var delegator in delegat.DelegatedAccounts)
                {
                    Cache.AddAccount(delegator);
                    Db.TryAttach(delegator);

                    delegator.Staked = true;
                }
            }

            return Task.CompletedTask;
        }

        #region static
        public static async Task<DeactivationCommit> Apply(ProtocolHandler proto, Block block, RawBlock rawBlock)
        {
            var commit = new DeactivationCommit(proto);
            await commit.Init(block, rawBlock);
            await commit.Apply();

            return commit;
        }

        public static async Task<DeactivationCommit> Revert(ProtocolHandler proto, Block block)
        {
            var commit = new DeactivationCommit(proto);
            await commit.Init(block);
            await commit.Revert();

            return commit;
        }
        #endregion
    }
}
