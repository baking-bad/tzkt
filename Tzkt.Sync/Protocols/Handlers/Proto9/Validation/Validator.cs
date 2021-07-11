using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto9
{
    class Validator : Proto8.Validator
    {
        public Validator(ProtocolHandler protocol) : base(protocol) { }

        protected override async Task ValidateBlockMetadata(JsonElement metadata)
        {
            Baker = metadata.RequiredString("baker");

            if (!Cache.Accounts.DelegateExists(Baker))
                throw new ValidationException($"non-existent block baker");

            await ValidateBlockVoting(metadata);

            foreach (var baker in metadata.RequiredArray("deactivated").EnumerateArray())
                if (!Cache.Accounts.DelegateExists(baker.GetString()))
                    throw new ValidationException($"non-existent deactivated baker {baker}");

            var balanceUpdates = ParseBalanceUpdates(metadata.RequiredArray("balance_updates").EnumerateArray().Where(x => x.RequiredString("origin")[0] == 'b'));
            var rewardUpdates = Cycle < Protocol.NoRewardCycles || Block.RequiredArray("operations", 4)[0].Count() == 0 ? 2 : 3;

            ValidateBlockRewards(balanceUpdates.Take(rewardUpdates));
            ValidateCycleRewards(balanceUpdates.Skip(rewardUpdates));
        }

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
                            default:
                                throw new ValidationException("invalid operation content kind");
                        }
                    }
                }
        }

        protected override void ValidateEndorsement(JsonElement content)
        {
            var endorsement = content.Required("endorsement").Required("operations");

            if (endorsement.RequiredString("kind") != "endorsement")
                throw new ValidationException("invalid endorsement kind");

            if (endorsement.RequiredInt32("level") != Cache.AppState.GetLevel())
                throw new ValidationException("invalid endorsed block level");

            ValidateEndorsementMetadata(content.Required("metadata"));
        }

        protected override PeriodKind ParsePeriodKind(string kind) => kind switch
        {
            "proposal" => PeriodKind.Proposal,
            "exploration" => PeriodKind.Exploration,
            "cooldown" => PeriodKind.Testing,
            "promotion" => PeriodKind.Promotion,
            "adoption" => PeriodKind.Adoption,
            _ => throw new ValidationException("invalid voting period kind")
        };
    }
}
