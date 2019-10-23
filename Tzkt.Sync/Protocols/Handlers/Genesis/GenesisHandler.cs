using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

using Tzkt.Data;
using Tzkt.Sync.Services;
using Tzkt.Sync.Protocols.Genesis;

namespace Tzkt.Sync.Protocols
{
    class GenesisHandler : ProtocolHandler
    {
        public override string Protocol => "Genesis";
        public override ISerializer Serializer { get; }
        public override IValidator Validator { get; }

        public GenesisHandler(TezosNode node, TzktContext db, CacheService cache, DiagnosticService diagnostics, ILogger<GenesisHandler> logger)
            : base(node, db, cache, diagnostics, logger)
        {
            Serializer = new Serializer();
            Validator = new Validator(this);
        }

        public override async Task Commit(IBlock block)
        {
            var rawBlock = block as RawBlock;

            await ProtoCommit.Apply(this, rawBlock);
            var blockCommit = await BlockCommit.Apply(this, rawBlock);

            await StateCommit.Apply(this, blockCommit.Block, rawBlock);
        }

        public override async Task Revert()
        {
            var currBlock = await Cache.GetCurrentBlockAsync();

            await BlockCommit.Revert(this, currBlock);
            await ProtoCommit.Revert(this, currBlock);

            await StateCommit.Revert(this, currBlock);
        }
    }
}
