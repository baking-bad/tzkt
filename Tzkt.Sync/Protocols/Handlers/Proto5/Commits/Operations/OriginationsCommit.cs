using System.Text.Json;
using Netezos.Encoding;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto5
{
    class OriginationsCommit(ProtocolHandler protocol) : Proto1.OriginationsCommit(protocol)
    {
        protected override ContractKind GetContractKind(JsonElement content)
        {
            return ManagerTz.Test(content.Required("script").Required("code"), content.Required("script").Required("storage"))
                ? ContractKind.DelegatorContract
                : ContractKind.SmartContract;
        }

        protected override BlockEvents GetBlockEvents(Contract contract)
        {
            return contract.Kind == ContractKind.DelegatorContract
                ? BlockEvents.DelegatorContracts
                : BlockEvents.SmartContracts;
        }

        protected override IEnumerable<BigMapDiff>? ParseBigMapDiffs(OriginationOperation origination, JsonElement result, MichelineArray code, IMicheline storage)
        {
            return result.TryGetProperty("big_map_diff", out var diffs)
                ? diffs.RequiredArray().EnumerateArray().Select(BigMapDiff.Parse)
                : null;
        }
    }
}
