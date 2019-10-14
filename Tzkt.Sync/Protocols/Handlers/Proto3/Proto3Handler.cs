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

        public override Task<List<IPreprocessor>> GetPreprocessors(IBlock block)
        {
            return Task.FromResult(new List<IPreprocessor>(0));
        }

        public override Task<List<ICommit>> GetReverts()
        {
            throw new NotImplementedException();
        }

        public override Task<List<ICommit>> GetCommits(IBlock block)
        {
            throw new NotImplementedException();
        }
    }
}
