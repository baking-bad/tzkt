using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;

using Tezzycat.Data;
using Tezzycat.Models;

namespace Tezzycat.Services.Protocols
{
    public class GenesisHandler : IProtocolHandler
    {
        public string Kind => "Genesis";

        public async Task<AppState> ApplyBlock(AppDbContext db, JObject block)
        {
            var state = await db.AppState.FirstOrDefaultAsync()
                ?? throw new Exception("AppState is missed");

            var level = block["header"]["level"].Int32();
            if (state.Level != level - 1)
                throw new Exception($"Invalid blocks order: {state.Level} -> {level}");

            var protoHash = block["protocol"].String();
            var proto = await db.Protocols.FirstOrDefaultAsync(x => x.Hash == protoHash)
                ?? new Protocol { Hash = protoHash };
            
            var b = new Block
            {
                Hash = block["hash"].String(),
                Level = level,
                Priority = 0,
                Protocol = proto,
                Timestamp = block["header"]["timestamp"].DateTime(),
            };
            db.Blocks.Add(b);

            proto.Blocks++;
            state.Level = b.Level;
            state.Timestamp = b.Timestamp;
            state.Protocol = proto.Hash;
            state.Hash = b.Hash;

            await db.SaveChangesAsync();
            return state;
        }

        public async Task<AppState> RevertLastBlock(AppDbContext db)
        {
            var state = await db.AppState.FirstOrDefaultAsync()
                ?? throw new Exception("AppState is missed");

            var block = await db.Blocks
                .Include(x => x.Protocol)
                .FirstOrDefaultAsync(x => x.Level == state.Level)
                ?? throw new Exception($"Failed to revert: block {state.Level} is not found");

            db.Blocks.Remove(block);

            block.Protocol.Blocks--;
            if (block.Protocol.Blocks == 0)
                db.Protocols.Remove(block.Protocol);

            var prevBlock = await db.Blocks
                .Include(x => x.Protocol)
                .FirstOrDefaultAsync(x => x.Level == state.Level - 1);

            state.Level = prevBlock?.Level ?? -1;
            state.Timestamp = prevBlock?.Timestamp ?? DateTime.MinValue;
            state.Protocol = prevBlock?.Protocol.Hash ?? "";
            state.Hash = prevBlock?.Hash ?? "";

            await db.SaveChangesAsync();
            return state;
        }
    }
}
