using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto5
{
    class SoftwareCommit : ProtocolCommit
    {
        public Block Block { get; private set; }
        public Software Software { get; private set; }

        SoftwareCommit(ProtocolHandler protocol) : base(protocol) { }

        public async Task Init(RawBlock rawBlock, Block block)
        {
            Block = block;

            var version = rawBlock.Header.PowNonce.Substring(0, 8);
            Software = await Cache.Software.GetOrCreateAsync(version, () => new Software
            {
                FirstLevel = block.Level,
                ShortHash = version
            });
        }

        public async Task Init(Block block)
        {
            Block = block;
            Block.Baker ??= Cache.Accounts.GetDelegate(block.BakerId);

            Software = await Cache.Software.GetAsync(block.SoftwareId);
        }

        public override Task Apply()
        {
            if (Software.BlocksCount == 0)
                Db.Software.Add(Software);
            else
                Db.TryAttach(Software);

            Software.BlocksCount++;
            Software.LastLevel = Block.Level;

            Block.Software = Software;
            Block.Baker.Software = Software;

            return Task.CompletedTask;
        }

        public override Task Revert()
        {
            Db.TryAttach(Software);
            Software.BlocksCount--;

            // don't revert Baker.SoftwareId and Software.LastLevel
            // don't remove emptied software for historical purposes

            return Task.CompletedTask;
        }

        #region static
        public static async Task<SoftwareCommit> Apply(ProtocolHandler proto, Block block, RawBlock rawBlock)
        {
            var commit = new SoftwareCommit(proto);
            await commit.Init(rawBlock, block);
            await commit.Apply();

            return commit;
        }

        public static async Task<SoftwareCommit> Revert(ProtocolHandler proto, Block block)
        {
            var commit = new SoftwareCommit(proto);
            await commit.Init(block);
            await commit.Revert();

            return commit;
        }
        #endregion
    }
}
