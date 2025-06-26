using System.Text.Json;

namespace Tzkt.Sync.Protocols.Proto23
{
    class AttestationAggregateCommit(ProtocolHandler protocol) : ProtocolCommit(protocol)
    {
        public async Task<IEnumerable<(string, string, int)>> ExtractAttestations(JsonElement op, JsonElement content)
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
