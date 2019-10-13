using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Tzkt.Data;
using Tzkt.Data.Models;
using Tzkt.Sync.Services;

namespace Tzkt.Sync.Protocols.Proto1
{
    class FreezerCommit : ProtocolCommit
    {
        public List<IBalanceUpdate> BalanceUpdates { get; protected set; }

        public FreezerCommit(ProtocolHandler protocol, List<ICommit> commits) : base(protocol, commits) { }

        public override Task Init()
        {
            BalanceUpdates = new List<IBalanceUpdate>();
            return Task.CompletedTask;
        }

        public override Task Init(IBlock block)
        {
            var rawBlock = block as RawBlock;

            BalanceUpdates = new List<IBalanceUpdate>();
            BalanceUpdates.AddRange(rawBlock.Metadata.BalanceUpdates.Skip(2));
            return Task.CompletedTask;
        }

        public override Task Apply()
        {
            foreach (var update in BalanceUpdates)
            {
                throw new NotImplementedException();
            }

            return Task.CompletedTask;
        }

        public override Task Revert()
        {
            foreach (var update in BalanceUpdates)
            {
                throw new NotImplementedException();
            }

            return Task.CompletedTask;
        }

        #region static
        public static async Task<FreezerCommit> Create(ProtocolHandler protocol, List<ICommit> commits, RawBlock rawBlock)
        {
            var commit = new FreezerCommit(protocol, commits);
            await commit.Init(rawBlock);
            return commit;
        }

        public static async Task<FreezerCommit> Create(ProtocolHandler protocol, List<ICommit> commits)
        {
            var commit = new FreezerCommit(protocol, commits);
            await commit.Init();
            return commit;
        }
        #endregion
    }
}
