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

        public GenesisHandler(TezosNode node, TzktContext db, CacheService cache, ILogger<GenesisHandler> logger) : base(node, db, cache, logger)
        {
            Serializer = new Serializer();
            Validator = new Validator(this);
        }

        public override async Task<List<ICommit>> GetCommits(IBlock block)
        {
            var rawBlock = block as RawBlock;

            var commits = new List<ICommit>();
            commits.Add(await ProtoCommit.Create(this, commits, rawBlock));
            commits.Add(await BlockCommit.Create(this, commits, rawBlock));
            commits.Add(await StateCommit.Create(this, commits, rawBlock));

            return commits;
        }

        public override async Task<List<ICommit>> GetReverts()
        {
            var commits = new List<ICommit>();
            commits.Add(await BlockCommit.Create(this, commits));
            commits.Add(await ProtoCommit.Create(this, commits));
            commits.Add(await StateCommit.Create(this, commits));

            return commits;
        }
    }
}
