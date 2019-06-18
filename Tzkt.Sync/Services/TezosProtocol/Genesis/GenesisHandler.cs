using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json.Linq;

using Tzkt.Data;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Services.Protocols
{
    public class GenesisHandler : IProtocolHandler
    {
        protected readonly TzktContext Db;
        protected readonly IMemoryCache Cache;

        public GenesisHandler(TzktContext db, IMemoryCache cache)
        {
            Db = db;
            Cache = cache;
        }

        #region IProtocolHandler
        public virtual string Kind => "Genesis";

        public virtual async Task<AppState> ApplyBlock(JObject json)
        {
            var block = await ParseBlock(json);

            if (block.Level != 0)
                throw new Exception("Genesis block must be at level 0");

            if (block.Protocol.Blocks > 0)
                throw new Exception("Genesis block already exists");

            Db.Blocks.Add(block);
            ProtocolUp(block.Protocol);
            await SetAppState(block);

            await Db.SaveChangesAsync();
            return await GetAppStateAsync();
        }

        public virtual async Task<AppState> RevertLastBlock()
        {
            var lastBlock = await GetLastBlock();

            if (lastBlock == null)
                throw new Exception("Nothing to revert");

            if (lastBlock.Level != 0)
                throw new Exception("Genesis block must be at level 0");

            Db.Blocks.Remove(lastBlock);
            ProtocolDown(lastBlock.Protocol);
            await SetAppState(null);

            await Db.SaveChangesAsync();
            return await GetAppStateAsync();
        }
        #endregion

        #region cached state
        protected async Task<AppState> GetAppStateAsync()
        {
            if (!Cache.TryGetValue<AppState>(nameof(AppState), out var state))
            {
                state = await Db.AppState.FirstOrDefaultAsync()
                    ?? throw new Exception("Failed to get app state");
                Cache.Set(nameof(AppState), state);
            }
            return state;
        }

        protected async Task SetAppState(Block block)
        {
            var state = await GetAppStateAsync();

            state.Level = block?.Level ?? -1;
            state.Timestamp = block?.Timestamp ?? DateTime.MinValue;
            state.Protocol = block?.Protocol.Hash ?? "";
            state.Hash = block?.Hash ?? "";

            Db.Update(state);
        }
        #endregion

        #region cached protocols
        protected async Task<Protocol> GetProtocolAsync(string hash)
        {
            if (!Cache.TryGetValue<Protocol>(hash, out var protocol))
            {
                protocol = await Db.Protocols.FirstOrDefaultAsync(x => x.Hash == hash)
                    ?? new Protocol { Hash = hash };
                Cache.Set(hash, protocol);
            }
            return protocol;
        }

        protected void ProtocolUp(Protocol protocol)
        {
            protocol.Blocks++;

            if (Db.Entry(protocol).State != EntityState.Added)
                Db.Update(protocol);
        }

        protected void ProtocolDown(Protocol protocol)
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
        }
        #endregion

        #region virtual
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
                .FirstOrDefaultAsync(x => x.Level == state.Level);
        }
        #endregion
    }
}
