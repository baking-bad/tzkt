using System.Collections.Generic;
using System.Threading.Tasks;
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
        public override ISerializer Serializer { get; }
        public override IValidator Validator { get; }

        public GenesisHandler(TzktContext db, CacheService cache, ILogger<GenesisHandler> logger) : base(db, cache, logger)
        {
            Serializer = new Serializer();
            Validator = new Validator(this);
        }

        public override async Task<List<ICommit>> GetCommits(IBlock block)
        {
            var rawBlock = block as RawBlock;

            var commits = new List<ICommit>();
            commits.Add(await BlockCommit.Create(this, commits, rawBlock));
            commits.Add(await StateCommit.Create(this, commits, rawBlock));

            return commits;
        }

        public override async Task<List<ICommit>> GetCommits(Block block)
        {
            var commits = new List<ICommit>();
            commits.Add(await BlockCommit.Create(this, commits, block));
            commits.Add(await StateCommit.Create(this, commits));

            return commits;
        }
    }
}
