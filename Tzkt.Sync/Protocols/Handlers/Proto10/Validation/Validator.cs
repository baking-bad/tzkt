using System.Text.Json;
using System.Threading.Tasks;

namespace Tzkt.Sync.Protocols.Proto10
{
    class Validator : Proto9.Validator
    {
        public Validator(ProtocolHandler protocol) : base(protocol) { }

        protected override async Task ValidateBlockMetadata(JsonElement metadata)
        {
            await base.ValidateBlockMetadata(metadata);
            await ValidateImplicitOperations(metadata.RequiredArray("implicit_operations_results"));
        }

        protected virtual async Task ValidateImplicitOperations(JsonElement ops)
        {
            foreach (var op in ops.EnumerateArray())
            {
                var kind = op.RequiredString("kind");
                if (kind == "transaction")
                {
                    var balanceUpdates = op.RequiredArray("balance_updates", 1);

                    var origin = balanceUpdates[0].RequiredString("origin");
                    if (origin != "subsidy")
                        throw new ValidationException($"Unexpected subsidy origin: {origin}");

                    var contract = balanceUpdates[0].RequiredString("contract");
                    if (contract != ProtoActivator.CpmmContract)
                        throw new ValidationException($"Unexpected subsidy recepient: {contract}");
                }
                else if (kind == "origination" && Level == Protocol.FirstLevel)
                {
                    var contract = op.RequiredArray("originated_contracts", 1)[0].RequiredString();
                    if (!await Cache.Accounts.ExistsAsync(contract, Data.Models.AccountType.Contract))
                        throw new ValidationException($"Unexpected contract origination: {contract}");

                }
                else
                {
                    throw new ValidationException($"Unexpected implicit operation kind: {kind}");
                }
            }
        }
    }
}
