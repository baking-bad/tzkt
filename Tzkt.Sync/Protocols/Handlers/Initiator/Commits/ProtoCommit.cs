using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Initiator
{
    class ProtoCommit : ProtocolCommit
    {
        public Protocol Protocol { get; private set; }

        public ProtoCommit(ProtocolHandler protocol, List<ICommit> commits) : base(protocol, commits) { }

        public override async Task Init()
        {
            Protocol = await Cache.GetCurrentProtocolAsync();
        }

        public override async Task Init(IBlock block)
        {
            Protocol = await Cache.GetProtocolAsync(block.Protocol);
        }

        public override Task Apply()
        {
            if (Protocol == null)
                throw new Exception("Commit is not initialized");

            Db.Attach(Protocol);
            Protocol.Weight++;

            return Task.CompletedTask;
        }

        public override Task Revert()
        {
            if (Protocol == null)
                throw new Exception("Commit is not initialized");

            Db.Attach(Protocol);

            if (--Protocol.Weight == 0)
            {
                Db.Protocols.Remove(Protocol);
                Cache.RemoveProtocol(Protocol);
            }
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
