using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

using Tzkt.Data;
using Tzkt.Sync.Services;

namespace Tzkt.Sync.Protocols
{
    public abstract class ProtocolCommit : ICommit
    {
        protected readonly List<ICommit> Commits;

        protected readonly ProtocolHandler Proto;
        protected readonly TzktContext Db;
        protected readonly AccountManager Accounts;
        protected readonly ProtocolManager Protocols;
        protected readonly StateManager State;
        protected readonly ILogger Logger;

        public ProtocolCommit(ProtocolHandler protocol, List<ICommit> commits)
        {
            Proto = protocol;
            Db = protocol.Db;
            Accounts = protocol.Accounts;
            Protocols = protocol.Protocols;
            State = protocol.State;
            Logger = protocol.Logger;

            Commits = commits;
        }
        protected T FindCommit<T>() where T : ProtocolCommit
            => Commits.FirstOrDefault(x => x is T) as T;

        public abstract Task Init(IBlock block);

        public abstract Task Apply();

        public abstract Task Revert();
    }
}
