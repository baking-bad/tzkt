using System.Text.Json;

namespace Tzkt.Sync.Protocols.Proto23
{
    class PreattestationAggregateCommit(ProtocolHandler protocol) : ProtocolCommit(protocol)
    {
        public IEnumerable<(string, string, int)> ExtractPreattestations(JsonElement op, JsonElement content)
        {
            var res = new List<(string, string, int)>();

            var opHash = op.RequiredString("hash");
            foreach (var c in content.Required("metadata").RequiredArray("committee").EnumerateArray())
            {
                var baker = Cache.Accounts.GetExistingDelegate(c.RequiredString("delegate"));
                var slots = c.RequiredInt32("consensus_power");
                res.Add((opHash, baker.Address, slots));
            }

            return res;
        }
    }
}
