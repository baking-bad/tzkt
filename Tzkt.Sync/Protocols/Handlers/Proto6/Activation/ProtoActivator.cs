using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto6
{
    class ProtoActivator : Proto5.ProtoActivator
    {
        public ProtoActivator(ProtocolHandler proto) : base(proto) { }

        protected override void SetParameters(Protocol protocol, JToken parameters)
        {
            var br = parameters["baking_reward_per_endorsement"] as JArray;
            var er = parameters["endorsement_reward"] as JArray;

            protocol.RampUpCycles = parameters["security_deposit_ramp_up_cycles"]?.Value<int>() ?? 0;
            protocol.NoRewardCycles = parameters["no_reward_cycles"]?.Value<int>() ?? 0;
            protocol.BlockDeposit = parameters["block_security_deposit"]?.Value<long>() ?? 512_000_000;
            protocol.BlockReward0 = br == null ? 1_250_000 : br.Count > 0 ? br[0].Value<long>() : 0;
            protocol.BlockReward1 = br == null ? 187_500 : br.Count > 1 ? br[1].Value<long>() : protocol.BlockReward0;
            protocol.BlocksPerCommitment = parameters["blocks_per_commitment"]?.Value<int>() ?? 32;
            protocol.BlocksPerCycle = parameters["blocks_per_cycle"]?.Value<int>() ?? 4096;
            protocol.BlocksPerSnapshot = parameters["blocks_per_roll_snapshot"]?.Value<int>() ?? 256;
            protocol.BlocksPerVoting = parameters["blocks_per_voting_period"]?.Value<int>() ?? 32_768;
            protocol.ByteCost = parameters["cost_per_byte"]?.Value<int>() ?? 1000;
            protocol.EndorsementDeposit = parameters["endorsement_security_deposit"]?.Value<long>() ?? 64_000_000;
            protocol.EndorsementReward0 = er == null ? 1_250_000 : er.Count > 0 ? er[0].Value<long>() : 0;
            protocol.EndorsementReward1 = er == null ? 833_333 : er.Count > 1 ? er[1].Value<long>() : protocol.EndorsementReward0;
            protocol.EndorsersPerBlock = parameters["endorsers_per_block"]?.Value<int>() ?? 32;
            protocol.HardBlockGasLimit = parameters["hard_gas_limit_per_block"]?.Value<int>() ?? 10_400_000;
            protocol.HardOperationGasLimit = parameters["hard_gas_limit_per_operation"]?.Value<int>() ?? 1_040_000;
            protocol.HardOperationStorageLimit = parameters["hard_storage_limit_per_operation"]?.Value<int>() ?? 60_000;
            protocol.OriginationSize = parameters["origination_size"]?.Value<int>() ?? 257;
            protocol.PreservedCycles = parameters["preserved_cycles"]?.Value<int>() ?? 5;
            protocol.RevelationReward = parameters["seed_nonce_revelation_tip"]?.Value<long>() ?? 125_000;
            protocol.TimeBetweenBlocks = parameters["time_between_blocks"]?[0].Value<int>() ?? 60;
            protocol.TokensPerRoll = parameters["tokens_per_roll"]?.Value<long>() ?? 8_000_000_000;
            protocol.BallotQuorumMin = parameters["quorum_min"]?.Value<int>() ?? 2000;
            protocol.BallotQuorumMax = parameters["quorum_max"]?.Value<int>() ?? 7000;
            protocol.ProposalQuorum = parameters["min_proposal_quorum"]?.Value<int>() ?? 500;
        }

        protected override void UpgradeParameters(Protocol protocol, Protocol prev)
        {
            protocol.BlockReward0 = 1_250_000;
            protocol.BlockReward1 = 187_500;
            protocol.EndorsementReward0 = 1_250_000;
            protocol.EndorsementReward1 = 833_333;
            protocol.HardBlockGasLimit = 10_400_000;
            protocol.HardOperationGasLimit = 1_040_000;
        }

        protected override long GetFutureBlockReward(Protocol protocol, int cycle)
            => cycle < protocol.NoRewardCycles ? 0 : (protocol.BlockReward0 * protocol.EndorsersPerBlock);

        protected override long GetFutureEndorsementReward(Protocol protocol, int cycle, int slots)
            => cycle < protocol.NoRewardCycles ? 0 : (slots * protocol.EndorsementReward0);

        // migrate baker cycles

        protected override async Task MigrateContext(AppState state)
        {
            var block = await Cache.Blocks.CurrentAsync();

            await Db.Database.ExecuteSqlRawAsync($@"
                UPDATE  ""BakerCycles""
                SET     ""FutureBlockRewards"" = ""FutureBlocks"" * 40000000 :: bigint,
                        ""FutureEndorsementRewards"" = ""FutureEndorsements"" * 1250000 :: bigint
                WHERE ""Cycle"" > {block.Cycle};");
        }

        protected override async Task RevertContext(AppState state)
        {
            var block = await Cache.Blocks.CurrentAsync();

            await Db.Database.ExecuteSqlRawAsync($@"
                UPDATE  ""BakerCycles""
                SET     ""FutureBlockRewards"" = ""FutureBlocks"" * 16000000 :: bigint,
                        ""FutureEndorsementRewards"" = ""FutureEndorsements"" * 2000000 :: bigint
                WHERE ""Cycle"" > {block.Cycle};");
        }
    }
}
