using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

using Tzkt.Data;
using Tzkt.Data.Models;
using Tzkt.Sync.Protocols;
using Tzkt.Sync.Services;

namespace Tzkt.Sync
{
    public abstract class ProtocolHandler
    {
        public abstract string Protocol { get; }
        public abstract ISerializer Serializer { get; }
        public abstract IValidator Validator { get; }

        public readonly TezosNode Node;
        public readonly TzktContext Db;
        public readonly CacheService Cache;
        public readonly AccountManager Accounts;
        public readonly ProtocolManager Protocols;
        public readonly StateManager State;
        public readonly ILogger Logger;

        public ProtocolHandler(TezosNode node, TzktContext db, CacheService cache, ILogger logger)
        {
            Node = node;
            Db = db;
            Cache = cache;
            Accounts = cache.Accounts;
            Protocols = cache.Protocols;
            State = cache.State;
            Logger = logger;
        }

        public virtual async Task<AppState> ApplyBlock(Stream stream)
        {
            Logger.LogDebug("Deserializing block...");
            var rawBlock = await Serializer.DeserializeBlock(stream);

            Logger.LogDebug("Validating block...");
            rawBlock = await Validator.ValidateBlock(rawBlock);

            Logger.LogDebug("Init commits...");
            var commits = await GetCommits(rawBlock);

            Logger.LogDebug("Applying commits...");
            foreach (var commit in commits)
                await commit.Apply();

            Logger.LogDebug("Saving...");
            await Db.SaveChangesAsync();

            return await State.GetAppStateAsync();
        }
        
        public virtual async Task<AppState> RevertLastBlock()
        {
            Logger.LogDebug("Loading last block...");
            var block = await State.GetCurrentBlock();

            Logger.LogDebug("Init commits...");
            var commits = await GetCommits(block);

            Logger.LogDebug("Applying revert commits...");
            foreach (var commit in commits)
                await commit.Revert();

            Logger.LogDebug("Saving...");
            await Db.SaveChangesAsync();

            return await State.GetAppStateAsync();
        }

        public abstract Task<List<ICommit>> GetCommits(Block block);

        public abstract Task<List<ICommit>> GetCommits(IBlock block);
    }
}
