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

        public async Task Init(RawBlock rawBlock)
        {
            var protocol = await Cache.GetProtocolAsync(rawBlock.Protocol);
            if (rawBlock.Level % protocol.BlocksPerCycle != 0 || rawBlock.Metadata.Deactivated.Count == 0)
                return;

            DeactivationLevel = rawBlock.Level;
            Delegates = await Db.Delegates
                .Include(x => x.DelegatedAccounts)
                .Where(x => rawBlock.Metadata.Deactivated.Contains(x.Address) && x.Staked)
                .ToListAsync();
        }

        public async Task Init(Block block)
        {
            var protocol = await Cache.GetProtocolAsync(block.ProtoCode);
            if (block.Level % protocol.BlocksPerCycle != 0)
                return;

            DeactivationLevel = block.Level;
            Delegates = await Db.Delegates
                .Include(x => x.DelegatedAccounts)
                .Where(x => x.DeactivationLevel == block.Level)
                .ToListAsync();
        }

        public override async Task Apply()
        {
            if (Delegates != null)
            {
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
                }
            }
        }

        public override Task Revert()
        {
            if (Delegates != null)
            {
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
                }
            }

            return Task.CompletedTask;
        }

        #region static
        public static async Task<DeactivationCommit> Apply(ProtocolHandler proto, RawBlock rawBlock)
        {
            var commit = new DeactivationCommit(proto);
            await commit.Init(rawBlock);
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
