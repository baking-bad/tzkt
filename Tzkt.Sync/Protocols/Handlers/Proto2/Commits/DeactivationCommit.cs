using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto2
{
    class DeactivationCommit : ProtocolCommit
    {
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
        }

        public async Task Init(Block block)
        {
            if (block.Events.HasFlag(BlockEvents.Deactivations))
            {
                DeactivationLevel = block.Level;
                Delegates = await Db.Delegates
                    .Include(x => x.DelegatedAccounts)
                    .Where(x => x.DeactivationLevel == block.Level)
                    .ToListAsync();
            }
        }

        public override async Task Apply()
        {
            if (Delegates == null) return;

            foreach (var delegat in Delegates)
            {
                Cache.AddAccount(delegat);
                Db.TryAttach(delegat);

                delegat.DeactivationBlock = await Cache.GetBlockAsync(DeactivationLevel);
                delegat.DeactivationLevel = DeactivationLevel;
                delegat.Staked = false;

                foreach (var delegator in delegat.DelegatedAccounts)
                {
                    Cache.AddAccount(delegator);
                    Db.TryAttach(delegator);

                    delegator.Staked = false;
                }

                Db.DelegateChanges.Add(new DelegateChange
                {
                    Delegate = delegat,
                    Level = DeactivationLevel,
                    Type = DelegateChangeType.Deactivated
                });
            }
        }

        public override async Task Revert()
        {
            if (Delegates == null) return;

            foreach (var delegat in Delegates)
            {
                Cache.AddAccount(delegat);
                Db.TryAttach(delegat);

                delegat.DeactivationBlock = null;
                delegat.DeactivationLevel = null;
                delegat.Staked = true;

                foreach (var delegator in delegat.DelegatedAccounts)
                {
                    Cache.AddAccount(delegator);
                    Db.TryAttach(delegator);

                    delegator.Staked = true;
                }

                Db.DelegateChanges.RemoveRange(
                    await Db.DelegateChanges.Where(x =>
                        x.Level == DeactivationLevel &&
                        x.Type == DelegateChangeType.Deactivated)
                        .ToListAsync());
            }
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
