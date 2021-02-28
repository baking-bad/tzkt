using System.Text.Json;
using System.Threading.Tasks;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto8
{
    class Validator : Proto6.Validator
    {
        public Validator(ProtocolHandler protocol) : base(protocol) { }

        // new period & new block.metadata rpc format
        protected override async Task ValidateBlockVoting(JsonElement metadata)
        {
            var rawPeriod = metadata.Required("voting_period_info").Required("voting_period");
            var periodIndex = rawPeriod.RequiredInt32("index");
            var kind = rawPeriod.RequiredString("kind") switch
            {
                "proposal" => PeriodKind.Proposal,
                "testing_vote" => PeriodKind.Exploration,
                "testing" => PeriodKind.Testing,
                "promotion_vote" => PeriodKind.Promotion,
                "adoption" => PeriodKind.Adoption,
                _ => throw new ValidationException("invalid voting period kind")
            };

            var period = await Cache.Periods.GetAsync(Cache.AppState.Get().VotingPeriod);

            if (Level < period.LastLevel)
            {
                if (periodIndex != period.Index)
                    throw new ValidationException("invalid voting period index");

                if (kind != period.Kind)
                    throw new ValidationException("unexpected voting period");
            }
            else
            {
                // WTF: period start time on mainnet [level:1363968] differs from edo2net [level:45056]

                //if (periodIndex != period.Index + 1)
                //    throw new ValidationException("invalid voting period index");

                //if (kind != PeriodKind.Proposal && (int)kind != (int)period.Kind + 1)
                //    throw new ValidationException("inconsistent voting period");
            }
        }
    }
}
