using System.Text.Json;

namespace Tzkt.Sync.Protocols.Proto10
{
    class BlockCommit : Proto9.BlockCommit
    {
        public BlockCommit(ProtocolHandler protocol) : base(protocol) { }

        protected override bool? GetLBToggleVote(JsonElement block)
            => !block.Required("header").RequiredBool("liquidity_baking_escape_vote");

        protected override int GetLBToggleEma(JsonElement block)
            => block.Required("metadata").RequiredInt32("liquidity_baking_escape_ema") * 1000;
    }
}
