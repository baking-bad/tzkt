using System.Text.Json;

namespace Tzkt.Sync.Protocols.Proto13
{
    class BlockCommit : Proto12.BlockCommit
    {
        public BlockCommit(ProtocolHandler protocol) : base(protocol) { }

        protected override bool? GetLBToggleVote(JsonElement block)
        {
            var vote = block.Required("header").RequiredString("liquidity_baking_toggle_vote");
            return vote == "on" ? true : vote == "off" ? false : null;
        }

        protected override int GetLBToggleEma(JsonElement block)
            => block.Required("metadata").RequiredInt32("liquidity_baking_toggle_ema");
    }
}
