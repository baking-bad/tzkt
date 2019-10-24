using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

using Tzkt.Data;
using Tzkt.Data.Models;
using Tzkt.Sync.Services;

namespace Tzkt.Sync.Protocols
{
    class Proto3Handler : ProtocolHandler
    {
        public override string Protocol => throw new NotImplementedException();
        public override ISerializer Serializer => throw new NotImplementedException();
        public override IValidator Validator => throw new NotImplementedException();

        public Proto3Handler(TezosNode node, TzktContext db, CacheService cache, DiagnosticService diagnostics, ILogger<Proto3Handler> logger)
            : base(node, db, cache, diagnostics, logger)
        {

        }

        public override Task InitProtocol(IBlock block)
        {
            throw new NotImplementedException();
        }

        public override Task InitProtocol()
        {
            throw new NotImplementedException();
        }

        public override Task Commit(IBlock block)
        {
            throw new NotImplementedException();
        }

        public override Task Revert()
        {
            throw new NotImplementedException();
        }
    }
}
