using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;

using Tzkt.Data;
using Tzkt.Data.Models;
using Tzkt.Sync.Services;

namespace Tzkt.Sync.Protocols.Proto1
{
    class DeactivationCommit : ProtocolCommit
    {
        public List<Data.Models.Delegate> Delegates { get; protected set; }

        public DeactivationCommit(ProtocolHandler protocol, List<ICommit> commits) : base(protocol, commits) { }

        public override async Task Init(IBlock block)
        {
            var rawBlock = block as RawBlock;

            Delegates = new List<Data.Models.Delegate>();
            foreach (var baker in rawBlock.Metadata.Deactivated)
                Delegates.Add((Data.Models.Delegate)await Accounts.GetAccountAsync(baker));
        }

        public override Task Apply()
        {
            foreach (var baker in Delegates)
            {
                throw new NotImplementedException();
            }

            return Task.CompletedTask;
        }

        public override Task Revert()
        {
            foreach (var baker in Delegates)
            {
                throw new NotImplementedException();
            }

            return Task.CompletedTask;
        }

        #region static
        public static async Task<DeactivationCommit> Create(ProtocolHandler protocol, List<ICommit> commits, RawBlock rawBlock)
        {
            var commit = new DeactivationCommit(protocol, commits);
            await commit.Init(rawBlock);
            return commit;
        }

        public static Task<DeactivationCommit> Create(ProtocolHandler protocol, List<ICommit> commits, List<Data.Models.Delegate> delegates)
        {
            var commit = new DeactivationCommit(protocol, commits) { Delegates = delegates };
            return Task.FromResult(commit);
        }
        #endregion
    }
}
