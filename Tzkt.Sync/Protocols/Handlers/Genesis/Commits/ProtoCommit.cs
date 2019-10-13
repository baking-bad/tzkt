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

        public ProtoCommit(ProtocolHandler protocol, List<ICommit> commits) : base(protocol, commits) { }

        public override async Task Init()
        {
            var state = await Cache.GetAppStateAsync();

            Protocol = await Cache.GetProtocolAsync(state.Protocol);
            NextProtocol = await Cache.GetProtocolAsync(state.NextProtocol);
        }

        public override async Task Init(IBlock block)
        {
            var rawBlock = block as RawBlock;

            Protocol = await Cache.GetProtocolAsync(rawBlock.Metadata.Protocol);
            NextProtocol = await Cache.GetProtocolAsync(rawBlock.Metadata.NextProtocol);
            NextProtocol.Code++;
        }

        public override Task Apply()
        {
            if (Protocol == null)
                throw new Exception("Commit is not initialized");

            Db.Protocols.Add(Protocol);
            Db.Protocols.Add(NextProtocol);
            Protocol.Weight++;

            return Task.CompletedTask;
        }

        public override Task Revert()
        {
            if (Protocol == null)
                throw new Exception("Commit is not initialized");

            Db.Protocols.Remove(Protocol);
            Db.Protocols.Remove(NextProtocol);

            Cache.RemoveProtocol(Protocol);
            Cache.RemoveProtocol(NextProtocol);

            return Task.CompletedTask;
        }

        #region static
        public static async Task<ProtoCommit> Create(ProtocolHandler protocol, List<ICommit> commits, RawBlock rawBlock)
        {
            var commit = new ProtoCommit(protocol, commits);
            await commit.Init(rawBlock);
            return commit;
        }

        public static async Task<ProtoCommit> Create(ProtocolHandler protocol, List<ICommit> commits)
        {
            var commit = new ProtoCommit(protocol, commits);
            await commit.Init();
            return commit;
        }
        #endregion
    }
}
