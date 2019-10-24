using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Initiator
{
    class ProtoCommit : ProtocolCommit
    {
        public Protocol Protocol { get; private set; }

        ProtoCommit(ProtocolHandler protocol) : base(protocol) { }

        public async Task Init(RawBlock rawBlock)
        {
            Protocol = await Cache.GetProtocolAsync(rawBlock.Metadata.Protocol);
        }

        public async Task Init(Block block)
        {
            Protocol = await Cache.GetProtocolAsync(block.ProtoCode);
        }

        public override Task Apply()
        {
            Protocol.Weight++;
            return Task.CompletedTask;
        }

        public override Task Revert()
        {
            Db.Protocols.Remove(Protocol);
            Cache.RemoveProtocol(Protocol);
            return Task.CompletedTask;
        }

        #region static
        public static async Task<ProtoCommit> Apply(ProtocolHandler proto, RawBlock rawBlock)
        {
            var commit = new ProtoCommit(proto);
            await commit.Init(rawBlock);
            await commit.Apply();

            return commit;
        }

        public static async Task<ProtoCommit> Revert(ProtocolHandler proto, Block block)
        {
            var commit = new ProtoCommit(proto);
            await commit.Init(block);
            await commit.Revert();

            return commit;
        }
        #endregion
    }
}
