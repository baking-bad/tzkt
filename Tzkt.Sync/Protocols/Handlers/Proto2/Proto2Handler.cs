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
    class Proto2Handler : ProtocolHandler
    {
        public override string Protocol => throw new NotImplementedException();
        public override ISerializer Serializer => throw new NotImplementedException();
        public override IValidator Validator => throw new NotImplementedException();

        public Proto2Handler(TezosNode node, TzktContext db, CacheService cache, DiagnosticService diagnostics, ILogger<Proto2Handler> logger)
            : base(node, db, cache, diagnostics, logger)
        {

        }

        public override Task Revert()
        {
            throw new NotImplementedException();
        }

        public override Task Commit(IBlock block)
        {
            throw new NotImplementedException();
        }
    }
}
