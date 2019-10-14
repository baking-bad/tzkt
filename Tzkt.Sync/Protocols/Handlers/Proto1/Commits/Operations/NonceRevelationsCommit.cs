using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto1
{
    class NonceRevelationsCommit : ProtocolCommit
    {
        public List<NonceRevelationOperation> Revelations { get; private set; }

        public NonceRevelationsCommit(ProtocolHandler protocol, List<ICommit> commits) : base(protocol, commits) { }

        public override Task Init()
        {
            Revelations = new List<NonceRevelationOperation>();
            return Task.CompletedTask;
        }

        public override Task Init(IBlock block)
        {
            var rawBlock = block as RawBlock;

            Revelations = new List<NonceRevelationOperation>();
            return Task.CompletedTask;
        }

        public override Task Apply()
        {
            foreach (var revelation in Revelations)
            {
                throw new NotImplementedException();
            }

            return Task.CompletedTask;
        }

        public override Task Revert()
        {
            foreach (var revelation in Revelations)
            {
                throw new NotImplementedException();
            }

            return Task.CompletedTask;
        }

        #region static
        public static async Task<NonceRevelationsCommit> Create(ProtocolHandler protocol, List<ICommit> commits, RawBlock rawBlock)
        {
            var commit = new NonceRevelationsCommit(protocol, commits);
            await commit.Init(rawBlock);
            return commit;
        }

        public static async Task<NonceRevelationsCommit> Create(ProtocolHandler protocol, List<ICommit> commits)
        {
            var commit = new NonceRevelationsCommit(protocol, commits);
            await commit.Init();
            return commit;
        }
        #endregion
    }
}
