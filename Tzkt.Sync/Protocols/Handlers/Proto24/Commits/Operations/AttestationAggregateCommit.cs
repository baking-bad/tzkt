using System.Text.Json;

namespace Tzkt.Sync.Protocols.Proto24
{
    class AttestationAggregateCommit(ProtocolHandler protocol) : ProtocolCommit(protocol)
    {
        public IEnumerable<(string, string, int)> ExtractAttestations(JsonElement op, JsonElement content)
        {
            var res = new List<(string, string, int)>();

            var opHash = op.RequiredString("hash");
            foreach (var c in content.Required("metadata").RequiredArray("committee").EnumerateArray())
            {
                var baker = Cache.Accounts.GetExistingDelegate(c.RequiredString("delegate"));
                var consensus = c.Required("consensus_power");
                var slots = consensus.RequiredInt32("slots");
                res.Add((opHash, baker.Address, slots));
            }

            return res;
        }
    }
}