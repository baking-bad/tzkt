using System.Text.Json;
using Netezos.Encoding;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto24
{
    class BlockCommit : Proto23.BlockCommit
    {
        public BlockCommit(ProtocolHandler protocol) : base(protocol) { }

        protected override bool? GetAiToggle(JsonElement header)
        {
            return null;
        }

        protected override int GetAiToggleEma(JsonElement metadata)
        {
            return 0;
        }
    }
}