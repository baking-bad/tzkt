using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json.Linq;

using Tezzycat.Data;
using Tezzycat.Data.Models;

namespace Tezzycat.Sync.Services.Protocols
{
    public class GenesisHandler : IProtocolHandler
    {
        private readonly SyncContext Db;
        private readonly IMemoryCache Cache;

        public GenesisHandler(SyncContext db, IMemoryCache cache)
        {
            Db = db;
            Cache = cache;
        }

        #region IProtocolHandler
        public virtual string Kind => "Genesis";

        public virtual async Task<AppState> ApplyBlock(JObject json)
        {
            var block = await ParseBlock(json);

            await PreApply(block);
            await Apply(block);
            await PostApply(block);

            await Db.SaveChangesAsync();
            return await GetAppStateAsync();
        }

        public virtual async Task<AppState> RevertLastBlock()
        {
            var block = await GetLastBlock();

            await PreRevert(block);
            await Revert(block);
            await PostRevert(block);

            await Db.SaveChangesAsync();
            return await GetAppStateAsync();
        }
        #endregion

        #region cached data
        protected virtual async Task<AppState> GetAppStateAsync()
        {
            if (!Cache.TryGetValue<AppState>(nameof(AppState), out var state))
            {
                state = await Db.AppState.FirstOrDefaultAsync()
                    ?? throw new Exception("Failed to get app state");
                Cache.Set(nameof(AppState), state);
            }
            return state;
        }

        protected virtual async Task<Protocol> GetProtocolAsync(string hash)
        {
            if (!Cache.TryGetValue<Protocol>(hash, out var protocol))
            {
                protocol = await Db.Protocols.FirstOrDefaultAsync(x => x.Hash == hash)
                    ?? new Protocol { Hash = hash };
                Cache.Set(hash, protocol);
            }
            return protocol;
        }

        protected virtual async Task UpdateAppState(Block block)
        {
            var state = await GetAppStateAsync();

            state.Level = block?.Level ?? -1;
            state.Timestamp = block?.Timestamp ?? DateTime.MinValue;
            state.Protocol = block?.Protocol.Hash ?? "";
            state.Hash = block?.Hash ?? "";

            Db.Update(state);
        }

        protected virtual Task IncrementProtocol(Protocol protocol)
        {
            protocol.Blocks++;

            if (Db.Entry(protocol).State != EntityState.Added)
                Db.Update(protocol);

            return Task.CompletedTask;
        }

        protected virtual Task DecrementProtocol(Protocol protocol)
        {
            if (--protocol.Blocks == 0)
            {
                Db.Protocols.Remove(protocol);
                Cache.Remove(protocol.Hash);
            }
            else
            {
                Db.Update(protocol);
            }
            return Task.CompletedTask;
        }
        #endregion

        #region blocks
        protected virtual async Task<Block> ParseBlock(JObject block)
        {
            return new Block
            {
                Hash = block["hash"].String(),
                Level = block["header"]["level"].Int32(),
                Protocol = await GetProtocolAsync(block["protocol"].String()),
                Timestamp = block["header"]["timestamp"].DateTime(),
            };
        }

        protected virtual async Task<Block> GetLastBlock()
        {
            var state = await GetAppStateAsync();

            return await Db.Blocks
                .Include(x => x.Protocol)
                .FirstOrDefaultAsync(x => x.Level == state.Level)
                ?? throw new Exception("There are no blocks");
        }

        protected virtual async Task<Block> GetSecondLastBlock()
        {
            var state = await GetAppStateAsync();

            return await Db.Blocks
                .Include(x => x.Protocol)
                .FirstOrDefaultAsync(x => x.Level == state.Level - 1);
        }
        #endregion

        #region applying
        protected virtual Task PreApply(Block block)
        {
            if (block.Protocol.Blocks > 0)
                throw new Exception("Genesis block already exists");
            return Task.CompletedTask;
        }

        protected virtual Task Apply(Block block)
        {
            Db.Blocks.Add(block);
            return Task.CompletedTask;
        }

        protected virtual async Task PostApply(Block block)
        {
            await IncrementProtocol(block.Protocol);
            await UpdateAppState(block);
        }
        #endregion

        #region reverting
        protected virtual Task PreRevert(Block block)
        {
            return Task.CompletedTask;
        }

        protected virtual Task Revert(Block block)
        {
            Db.Blocks.Remove(block);
            return Task.CompletedTask;
        }

        protected virtual async Task PostRevert(Block block)
        {
            await DecrementProtocol(block.Protocol);
            var lastBlock = await GetSecondLastBlock();
            await UpdateAppState(lastBlock);
        }
        #endregion
    }
}
