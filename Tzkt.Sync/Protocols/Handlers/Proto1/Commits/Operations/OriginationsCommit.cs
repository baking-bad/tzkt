using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto1
{
    class OriginationsCommit : ProtocolCommit
    {
        public List<OriginationOperation> Originations { get; protected set; }

        public OriginationsCommit(ProtocolHandler protocol, List<ICommit> commits) : base(protocol, commits) { }

        public override Task Init(IBlock block)
        {
            var rawBlock = block as RawBlock;

            Originations = new List<OriginationOperation>();
            return Task.CompletedTask;
        }

        public override Task Apply()
        {
            foreach (var origination in Originations)
            {
                throw new NotImplementedException();
            }

            return Task.CompletedTask;
        }

        public override Task Revert()
        {
            foreach (var origination in Originations)
            {
                throw new NotImplementedException();
            }

            return Task.CompletedTask;
        }

        #region static
        public static async Task<OriginationsCommit> Create(ProtocolHandler protocol, List<ICommit> commits, RawBlock rawBlock)
        {
            var commit = new OriginationsCommit(protocol, commits);
            await commit.Init(rawBlock);
            return commit;
        }

        public static Task<OriginationsCommit> Create(ProtocolHandler protocol, List<ICommit> commits, List<OriginationOperation> originations)
        {
            var commit = new OriginationsCommit(protocol, commits) { Originations = originations };
            return Task.FromResult(commit);
        }
        #endregion
    }
}
