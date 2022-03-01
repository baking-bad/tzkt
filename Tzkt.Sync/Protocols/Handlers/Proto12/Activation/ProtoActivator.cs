using Newtonsoft.Json.Linq;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto12
{
    class ProtoActivator : Proto11.ProtoActivator
    {
        public ProtoActivator(ProtocolHandler proto) : base(proto) { }

        protected override void SetParameters(Protocol protocol, JToken parameters)
        {
            base.SetParameters(protocol, parameters);
            protocol.BlocksPerSnapshot = parameters["blocks_per_stake_snapshot"]?.Value<int>() ?? 512;
            protocol.EndorsersPerBlock = parameters["consensus_committee_size"]?.Value<int>() ?? 7000;
            protocol.TokensPerRoll = parameters["tokens_per_roll"]?.Value<long>() ?? 6_000_000_000;
            protocol.BlockDeposit = 0;
            protocol.EndorsementDeposit = 0;

            var totalReward = 80_000_000 / (60 / protocol.TimeBetweenBlocks);
            protocol.BlockReward0 = parameters["baking_reward_fixed_portion"]?.Value<long>() ?? (totalReward / 4);
            protocol.BlockReward1 = parameters["baking_reward_bonus_per_slot"]?.Value<long>() ?? (totalReward / 4 / (protocol.EndorsersPerBlock / 3));
            protocol.EndorsementReward0 = parameters["endorsing_reward_per_slot"]?.Value<long>() ?? (totalReward / 2 / protocol.EndorsersPerBlock);
            protocol.EndorsementReward1 = 0;

            protocol.LBSunsetLevel = parameters["liquidity_baking_sunset_level"]?.Value<int>() ?? 3_063_809;

            protocol.ConsensusThreshold = parameters["consensus_threshold"]?.Value<int>() ?? 4667;
            protocol.MinParticipationNumerator = parameters["minimal_participation_ratio"]?["numerator"]?.Value<int>() ?? 2;
            protocol.MinParticipationDenominator = parameters["minimal_participation_ratio"]?["denominator"]?.Value<int>() ?? 3;
            protocol.MaxSlashingPeriod = parameters["max_slashing_period"]?.Value<int>() ?? 2;
            protocol.FrozenDepositsPercentage = parameters["frozen_deposits_percentage"]?.Value<int>() ?? 10;
            protocol.DoubleBakingPunishment = parameters["double_baking_punishment"]?.Value<long>() ?? 640_000_000;
            protocol.DoubleEndorsingPunishmentNumerator = parameters["ratio_of_frozen_deposits_slashed_per_double_endorsement"]?["numerator"]?.Value<int>() ?? 1;
            protocol.DoubleEndorsingPunishmentDenominator = parameters["ratio_of_frozen_deposits_slashed_per_double_endorsement"]?["denominator"]?.Value<int>() ?? 2;
        }

        protected override void UpgradeParameters(Protocol protocol, Protocol prev)
        {
            protocol.EndorsersPerBlock = 7000;
            protocol.TokensPerRoll = 6_000_000_000;
            protocol.BlockDeposit = 0;
            protocol.EndorsementDeposit = 0;

            var totalReward = 80_000_000 / (60 / protocol.TimeBetweenBlocks);
            protocol.BlockReward0 = totalReward / 4;
            protocol.BlockReward1 = totalReward / 4 / (protocol.EndorsersPerBlock / 3);
            protocol.EndorsementReward0 = totalReward / 2 / protocol.EndorsersPerBlock;
            protocol.EndorsementReward1 = 0;

            if (protocol.LBSunsetLevel == 2_244_609)
                protocol.LBSunsetLevel = 3_063_809;

            protocol.ConsensusThreshold = 4667;
            protocol.MinParticipationNumerator = 2;
            protocol.MinParticipationDenominator = 3;
            protocol.MaxSlashingPeriod = 2;
            protocol.FrozenDepositsPercentage = 10;
            protocol.DoubleBakingPunishment = 640_000_000;
            protocol.DoubleEndorsingPunishmentNumerator = 1;
            protocol.DoubleEndorsingPunishmentDenominator = 2;
        }
    }
}
