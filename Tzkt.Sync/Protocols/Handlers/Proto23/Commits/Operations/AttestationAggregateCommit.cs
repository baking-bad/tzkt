using System.Text.Json;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto23
{
    class AttestationAggregateCommit(ProtocolHandler protocol) : ProtocolCommit(protocol)
    {
        public async Task<IEnumerable<(string, string, int)>> ExtractAttestations(JsonElement op, JsonElement content)
        {
            var res = new List<(string, string, int)>();

            var opHash = op.RequiredString("hash");
            var totalSlots = 0;
            var slots = (await Cache.BakingRights.GetAsync(Context.Block.Level))
                .Where(x => x.Type == BakingRightType.Attestation)
                .ToDictionary(x => x.BakerId, x => x.Slots!.Value);

            foreach (var c in content.Required("metadata").RequiredArray("committee").EnumerateArray())
            {
                var baker = Cache.Accounts.GetExistingDelegate(c.RequiredString("delegate"));
                var bakerSlots = slots[baker.Id];
                res.Add((opHash, baker.Address, bakerSlots));
                totalSlots += bakerSlots;
            }

            if (totalSlots != content.Required("metadata").RequiredInt32("consensus_power"))
                throw new Exception("Wrong attestations_aggregate slots number");

            return res;
        }
    }
}
