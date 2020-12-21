using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto3
{
    class Validator : Proto1.Validator
    {
        public Validator(ProtocolHandler protocol) : base(protocol) { }

        // new period kind enum & wtf
        protected override async Task ValidateBlockVoting(JsonElement metadata)
        {
            var periodIndex = metadata.Required("level").RequiredInt32("voting_period");

            if (Cache.AppState.Get().VotingPeriod != periodIndex)
                throw new ValidationException("invalid voting period index");

            var period = await Cache.Periods.GetAsync(periodIndex);

            var kind = metadata.RequiredString("voting_period_kind") switch
            {
                "proposal" => PeriodKind.Proposal,
                "testing_vote" => PeriodKind.Exploration,
                "testing" => PeriodKind.Testing,
                "promotion_vote" => PeriodKind.Promotion,
                _ => throw new ValidationException("invalid voting period kind")
            };

            // WTF: [level:360448] - Exploration period started before the proposals period ended.
            if (Level < period.LastLevel)
            {
                if (period.Kind != kind)
                    throw new ValidationException("unexpected voting period");
            }
            else
            {
                if (kind != PeriodKind.Proposal && (int)kind != (int)period.Kind + 1)
                    throw new ValidationException("inconsistent voting period");
            }
        }

        // fixed non-existent delegate & separate allocation fee
        protected override async Task ValidateOrigination(JsonElement content)
        {
            var source = content.RequiredString("source");
            var delegat = content.OptionalString("delegate");
            var metadata = content.Required("metadata");
            var result = metadata.Required("operation_result");

            if (!await Cache.Accounts.ExistsAsync(source))
                throw new ValidationException("unknown source account");

            if (result.RequiredString("status") == "applied" && delegat != null)
                if (!Cache.Accounts.DelegateExists(delegat))
                    throw new ValidationException("unknown delegate account");

            ValidateFeeBalanceUpdates(
                ParseBalanceUpdates(metadata.RequiredArray("balance_updates")),
                source,
                content.RequiredInt64("fee"));

            if (result.TryGetProperty("balance_updates", out var resultUpdates))
                ValidateTransferBalanceUpdates(
                    ParseBalanceUpdates(resultUpdates),
                    source,
                    result.RequiredArray("originated_contracts", 1)[0].RequiredString(),
                    content.RequiredInt64("balance"),
                    (result.OptionalInt32("paid_storage_size_diff") ?? 0) * Protocol.ByteCost,
                    Protocol.OriginationSize * Protocol.ByteCost);
        }

        // fixed non-existent delegate & separate allocation fee
        protected override void ValidateInternalOrigination(JsonElement content, string initiator)
        {
            var delegat = content.OptionalString("delegate");
            var result = content.Required("result");

            if (result.RequiredString("status") == "applied" && delegat != null)
                if (!Cache.Accounts.DelegateExists(delegat))
                    throw new ValidationException("unknown delegate account");

            if (result.TryGetProperty("balance_updates", out var resultUpdates))
                ValidateTransferBalanceUpdates(
                    ParseBalanceUpdates(resultUpdates.RequiredArray()),
                    content.RequiredString("source"),
                    result.RequiredArray("originated_contracts", 1)[0].RequiredString(),
                    content.RequiredInt64("balance"),
                    (result.OptionalInt32("paid_storage_size_diff") ?? 0) * Protocol.ByteCost,
                    Protocol.OriginationSize * Protocol.ByteCost,
                    initiator);
        }
    }
}
