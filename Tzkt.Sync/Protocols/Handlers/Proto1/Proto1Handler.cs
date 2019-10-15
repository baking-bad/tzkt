using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

using Tzkt.Data;
using Tzkt.Sync.Services;
using Tzkt.Sync.Protocols.Proto1;

namespace Tzkt.Sync.Protocols
{
    class Proto1Handler : ProtocolHandler
    {
        public override string Protocol => "Proto 1";
        public override ISerializer Serializer { get; }
        public override IValidator Validator { get; }

        public Proto1Handler(TezosNode node, TzktContext db, CacheService cache, DiagnosticService diagnostics, ILogger<Proto1Handler> logger)
            : base(node, db, cache, diagnostics, logger)
        {
            Serializer = new Serializer();
            Validator = new Validator(this);
        }

        public override Task<List<IPreprocessor>> GetPreprocessors(IBlock block)
        {
            return Task.FromResult(new List<IPreprocessor>
            { 
                new CounterPreprocessor(this, block as RawBlock)
            });
        }

        public override async Task<List<ICommit>> GetCommits(IBlock block)
        {
            var rawBlock = block as RawBlock;
            var commits = new List<ICommit>();

            commits.Add(await ProtoCommit.Create(this, commits, rawBlock));
            commits.Add(await BlockCommit.Create(this, commits, rawBlock));
            commits.Add(await FreezerCommit.Create(this, commits, rawBlock));

            commits.Add(await ActivationsCommit.Create(this, commits, rawBlock));
            commits.Add(await RevealsCommit.Create(this, commits, rawBlock));
            commits.Add(await DelegationsCommit.Create(this, commits, rawBlock));
            commits.Add(await OriginationsCommit.Create(this, commits, rawBlock));
            commits.Add(await TransactionsCommit.Create(this, commits, rawBlock));

            commits.Add(await EndorsementsCommit.Create(this, commits, rawBlock));
            commits.Add(await NonceRevelationsCommit.Create(this, commits, rawBlock));

            commits.Add(await StateCommit.Create(this, commits, rawBlock));

            return commits;
        }

        public override async Task<List<ICommit>> GetReverts()
        {
            var commits = new List<ICommit>();

            commits.Add(await NonceRevelationsCommit.Create(this, commits));
            commits.Add(await EndorsementsCommit.Create(this, commits));

            commits.Add(await TransactionsCommit.Create(this, commits));
            commits.Add(await OriginationsCommit.Create(this, commits));
            commits.Add(await DelegationsCommit.Create(this, commits));
            commits.Add(await RevealsCommit.Create(this, commits));
            commits.Add(await ActivationsCommit.Create(this, commits));

            commits.Add(await FreezerCommit.Create(this, commits));
            commits.Add(await BlockCommit.Create(this, commits));
            commits.Add(await ProtoCommit.Create(this, commits));

            commits.Add(await StateCommit.Create(this, commits));

            return commits;
        }
    }
}
