using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Genesis
{
    class BlockCommit : ProtocolCommit
    {
        public Block Block { get; private set; }

        BlockCommit(ProtocolHandler protocol) : base(protocol) { }

        public async Task Init(JsonElement block)
        {
            Block = new Block
            {
                Id = Cache.AppState.NextOperationId(),
                Hash = block.RequiredString("hash"),
                Level = block.Required("header").RequiredInt32("level"),
                Protocol = await Cache.Protocols.GetAsync(block.RequiredString("protocol")),
                Timestamp = block.Required("header").RequiredDateTime("timestamp"),
                Events = BlockEvents.ProtocolBegin | BlockEvents.ProtocolEnd
            };
        }

        public async Task Init(Block block)
        {
            Block = block;
            Block.Protocol ??= await Cache.Protocols.GetAsync(block.ProtoCode);
        }

        public override Task Apply()
        {
            Db.TryAttach(Block.Protocol);

            Db.Blocks.Add(Block);
            Cache.Blocks.Add(Block);

            return Task.CompletedTask;
        }

        public override Task Revert()
        {
            Db.Protocols.Remove(Block.Protocol);
            Cache.Protocols.Remove(Block.Protocol);

            Db.Blocks.Remove(Block);
            return Task.CompletedTask;
        }

        #region static
        public static async Task<BlockCommit> Apply(ProtocolHandler proto, JsonElement block)
        {
            var commit = new BlockCommit(proto);
            await commit.Init(block);
            await commit.Apply();

            return commit;
        }

        public static async Task<BlockCommit> Revert(ProtocolHandler proto, Block block)
        {
            var commit = new BlockCommit(proto);
            await commit.Init(block);
            await commit.Revert();

            return commit;
        }
        #endregion
    }
}
