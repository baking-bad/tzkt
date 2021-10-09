using System.Text.Json;
using System.Threading.Tasks;

namespace Tzkt.Sync.Protocols.Proto11
{
    class Validator : Proto10.Validator
    {
        public Validator(ProtocolHandler protocol) : base(protocol) { }

        protected override async Task ValidateOperations(JsonElement operations)
        {
            foreach (var group in operations.EnumerateArray())
                foreach (var op in group.RequiredArray().EnumerateArray())
                {
                    if (op.RequiredString("hash").Length == 0)
                        throw new ValidationException("invalid operation hash");

                    foreach (var content in op.RequiredArray("contents").EnumerateArray())
                    {
                        switch (content.RequiredString("kind"))
                        {
                            case "endorsement_with_slot": ValidateEndorsement(content); break;
                            case "ballot": await ValidateBallot(content); break;
                            case "proposals": ValidateProposal(content); break;
                            case "activate_account": await ValidateActivation(content); break;
                            case "double_baking_evidence": ValidateDoubleBaking(content); break;
                            case "double_endorsement_evidence": ValidateDoubleEndorsing(content); break;
                            case "seed_nonce_revelation": await ValidateSeedNonceRevelation(content); break;
                            case "delegation": await ValidateDelegation(content); break;
                            case "origination": await ValidateOrigination(content); break;
                            case "transaction": await ValidateTransaction(content); break;
                            case "reveal": await ValidateReveal(content); break;
                            case "register_global_constant": await ValidateRegisterConstant(content); break;
                            default:
                                throw new ValidationException("invalid operation content kind");
                        }
                    }
                }
        }

        protected virtual async Task ValidateRegisterConstant(JsonElement content)
        {
            var source = content.RequiredString("source");

            if (!await Cache.Accounts.ExistsAsync(source))
                throw new ValidationException("unknown source account");

            ValidateFeeBalanceUpdates(
                ParseBalanceUpdates(content.Required("metadata").RequiredArray("balance_updates").EnumerateArray()),
                source,
                content.RequiredInt64("fee"));
        }
    }
}
