using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json.Linq;

using Tzkt.Data;
using Tzkt.Data.Models;
using Tzkt.Sync.Services;

namespace Tzkt.Sync.Protocols
{
    public class GenesisHandler : IProtocolHandler
    {
        protected readonly TzktContext Db;
        protected readonly ProtocolsCache ProtoCache;
        protected readonly StateCache StateCache;
        protected readonly IMemoryCache Cache;

        public GenesisHandler(TzktContext db, ProtocolsCache protoCache, StateCache stateCache)
        {
            Db = db;
            ProtoCache = protoCache;
            StateCache = stateCache;
        }

        #region IProtocolHandler
        public virtual string Protocol => "Genesis";

        public virtual async Task<AppState> ApplyBlock(JObject json)
        {
            var block = await ParseBlock(json);

            if (block.Level != 0)
                throw new Exception("Genesis block must be at level 0");

            if (block.Protocol.Weight > 0)
                throw new Exception("Genesis block already exists");

            Db.Blocks.Add(block);
            ProtoCache.ProtocolUp(block.Protocol);
            await StateCache.SetAppStateAsync(block);

            await Db.SaveChangesAsync();
            return await StateCache.GetAppStateAsync();
        }

        public virtual async Task<AppState> RevertLastBlock()
        {
            var lastBlock = await GetLastBlock();

            if (lastBlock == null)
                throw new Exception("Nothing to revert");

            if (lastBlock.Level != 0)
                throw new Exception("Genesis block must be at level 0");

            Db.Blocks.Remove(lastBlock);
            ProtoCache.ProtocolDown(lastBlock.Protocol);
            await StateCache.SetAppStateAsync(null);

            await Db.SaveChangesAsync();
            return await StateCache.GetAppStateAsync();
        }
        #endregion

        #region virtual
        protected virtual async Task<Block> ParseBlock(JObject block)
        {
            return new Block
            {
                Hash = block["hash"].String(),
                Level = block["header"]["level"].Int32(),
                Protocol = await ProtoCache.GetProtocolAsync(block["protocol"].String()),
                Timestamp = block["header"]["timestamp"].DateTime(),
            };
        }

        protected virtual async Task<Block> GetLastBlock()
        {
            var state = await StateCache.GetAppStateAsync();

            return await Db.Blocks
                .Include(x => x.Protocol)
                .FirstOrDefaultAsync(x => x.Level == state.Level);
        }
        #endregion
    }
}
