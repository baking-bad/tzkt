using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

using Tzkt.Data;
using Tzkt.Data.Models;
using Tzkt.Sync.Services;

namespace Tzkt.Sync.Protocols
{
    public class GenesisHandler : IProtocolHandler
    {
        public virtual string Protocol => "Genesis";

        protected readonly TzktContext Db;
        protected readonly ProtocolsCache ProtoCache;
        protected readonly StateCache StateCache;

        public GenesisHandler(TzktContext db, ProtocolsCache protoCache, StateCache stateCache)
        {
            Db = db;
            ProtoCache = protoCache;
            StateCache = stateCache;
        }

        public virtual async Task<AppState> ApplyBlock(JObject json)
        {
            var block = await ParseBlock(json);

            if (block.Level != 0)
                throw new Exception("Genesis block must be at level 0");

            if (block.Protocol.Weight > 0)
                throw new Exception("Genesis block already exists");

            Db.Blocks.Add(block);
            ProtoCache.ProtocolUp(block.Protocol);
            await StateCache.SetAppStateAsync(block, json["metadata"]["next_protocol"].String());

            await Db.SaveChangesAsync();
            return await StateCache.GetAppStateAsync();
        }

        public virtual async Task<AppState> RevertLastBlock()
        {
            var currentBlock = await StateCache.GetCurrentBlock();

            if (currentBlock == null)
                throw new Exception("Nothing to revert");

            if (currentBlock.Level != 0)
                throw new Exception("Genesis block must be at level 0");

            Db.Blocks.Remove(currentBlock);
            ProtoCache.ProtocolDown(currentBlock.Protocol);
            await StateCache.ReduceAppStateAsync();

            await Db.SaveChangesAsync();
            return await StateCache.GetAppStateAsync();
        }

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
        #endregion
    }
}
