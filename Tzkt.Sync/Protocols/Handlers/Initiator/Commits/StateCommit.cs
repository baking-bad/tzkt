using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Initiator
{
    class StateCommit : ProtocolCommit
    {
        public Block Block { get; private set; }
        public string NextProtocol { get; private set; }

        public StateCommit(ProtocolHandler protocol, List<ICommit> commits) : base(protocol, commits) { }

        public override Task Init(IBlock block)
        {
            Block = FindCommit<BlockCommit>().Block;
            NextProtocol = (block as RawBlock).Metadata.NextProtocol;
            return Task.CompletedTask;
        }

        public override async Task Apply()
        {
            if (Block == null || string.IsNullOrEmpty(NextProtocol))
                throw new Exception("Commit is not initialized");

            await State.SetAppStateAsync(Block, NextProtocol);
        }

        public override async Task Revert()
        {
            await State.ReduceAppStateAsync();
        }

        #region static
        public static async Task<StateCommit> Create(ProtocolHandler protocol, List<ICommit> commits, RawBlock rawBlock)
        {
            var commit = new StateCommit(protocol, commits);
            await commit.Init(rawBlock);
            return commit;
        }

        public static Task<StateCommit> Create(ProtocolHandler protocol, List<ICommit> commits)
        {
            var commit = new StateCommit(protocol, commits);
            return Task.FromResult(commit);
        }
        #endregion
    }
}
