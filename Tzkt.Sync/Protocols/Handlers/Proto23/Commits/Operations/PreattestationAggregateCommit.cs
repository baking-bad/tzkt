using System.Text.Json;

namespace Tzkt.Sync.Protocols.Proto23
{
    class PreattestationAggregateCommit(ProtocolHandler protocol) : ProtocolCommit(protocol)
    {
        public IEnumerable<(string, string, long)> ExtractPreattestations(JsonElement op, JsonElement content)
        {
            var res = new List<(string, string, long)>();

            var opHash = op.RequiredString("hash");
            foreach (var c in content.Required("metadata").RequiredArray("committee").EnumerateArray())
            {
                var baker = Cache.Accounts.GetExistingDelegate(c.RequiredString("delegate"));
                var power = GetPower(c);
                res.Add((opHash, baker.Address, power));
            }

            return res;
        }

        protected virtual long GetPower(JsonElement c)
        {
            return c.RequiredInt64("consensus_power");
        }
    }
}
