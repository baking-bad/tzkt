using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using Tzkt.Data;
using Tzkt.Data.Models;
using Tzkt.Sync.Services;
using Tzkt.Sync.Protocols.Genesis;

namespace Tzkt.Sync.Protocols
{
    class GenesisHandler : ProtocolHandler
    {
        public override string Protocol => "Genesis";
        public override IDiagnostics Diagnostics { get; }
        public override ISerializer Serializer { get; }
        public override IValidator Validator { get; }

        public GenesisHandler(TezosNode node, TzktContext db, CacheService cache, IConfiguration config, ILogger<GenesisHandler> logger)
            : base(node, db, cache, config, logger)
        {
            Diagnostics = new Diagnostics();
            Serializer = new Serializer();
            Validator = new Validator(this);
        }

        public override Task InitProtocol(IBlock block)
        {
            var protocol = new Protocol
            {
                Hash = block.Protocol,
                Code = -1,
                FirstLevel = block.Level,
                LastLevel = block.Level
            };

            Db.Protocols.Add(protocol);
            Cache.Protocols.Add(protocol);
            return Task.CompletedTask;
        }

        public override Task InitProtocol()
        {
            return Task.CompletedTask;
        }

        public override async Task Commit(IBlock block)
        {
            var rawBlock = block as RawBlock;

            var blockCommit = await BlockCommit.Apply(this, rawBlock);

            await StateCommit.Apply(this, blockCommit.Block, rawBlock);
        }

        public override async Task Revert()
        {
            var currBlock = await Cache.Blocks.CurrentAsync();

            await BlockCommit.Revert(this, currBlock);

            await StateCommit.Revert(this, currBlock);
        }
    }
}
