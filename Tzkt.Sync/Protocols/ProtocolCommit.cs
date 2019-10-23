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
        protected readonly TzktContext Db;
        protected readonly CacheService Cache;
        protected readonly ProtocolHandler Proto;
        protected readonly ILogger Logger;

        public ProtocolCommit(ProtocolHandler protocol)
        {
            Proto = protocol;
            Db = protocol.Db;
            Cache = protocol.Cache;
            Logger = protocol.Logger;
        }

        public abstract Task Apply();

        public abstract Task Revert();
    }
}
