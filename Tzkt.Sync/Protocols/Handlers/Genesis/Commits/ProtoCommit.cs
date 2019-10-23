using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Genesis
{
    class ProtoCommit : ProtocolCommit
    {
        public Protocol Protocol { get; private set; }
        public Protocol NextProtocol { get; private set; }

        ProtoCommit(ProtocolHandler protocol) : base(protocol) { }

        public async Task Init(RawBlock rawBlock)
        {
            Protocol = await Cache.GetProtocolAsync(rawBlock.Metadata.Protocol);
            NextProtocol = await Cache.GetProtocolAsync(rawBlock.Metadata.NextProtocol);
            NextProtocol.Code++;
        }

        public async Task Init(Block block)
        {
            var state = await Cache.GetAppStateAsync();

            Protocol = await Cache.GetProtocolAsync(state.Protocol);
            NextProtocol = await Cache.GetProtocolAsync(state.NextProtocol);
        }

        public override Task Apply()
        {
            Db.Protocols.Add(Protocol);
            Db.Protocols.Add(NextProtocol);
            Protocol.Weight++;

            return Task.CompletedTask;
        }

        public override Task Revert()
        {
            Db.Protocols.Remove(Protocol);
            Cache.RemoveProtocol(Protocol);

            Db.Protocols.Remove(NextProtocol);
            Cache.RemoveProtocol(NextProtocol);

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
