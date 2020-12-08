using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto6
{
    class ProtoActivator : Proto4.ProtoActivator
    {
        public ProtoActivator(ProtocolHandler proto) : base(proto) { }

        protected override void SetParameters(Protocol protocol, JToken parameters)
        {
            protocol.BlockDeposit = parameters["block_security_deposit"]?.Value<long>() ?? 512_000_000;
            protocol.BlockReward0 = parameters["baking_reward_per_endorsement"]?[0].Value<long>() ?? 1_250_000;
            protocol.BlockReward1 = parameters["baking_reward_per_endorsement"]?[1].Value<long>() ?? 187_500;
            protocol.BlocksPerCommitment = parameters["blocks_per_commitment"]?.Value<int>() ?? 32;
            protocol.BlocksPerCycle = parameters["blocks_per_cycle"]?.Value<int>() ?? 4096;
            protocol.BlocksPerSnapshot = parameters["blocks_per_roll_snapshot"]?.Value<int>() ?? 256;
            protocol.BlocksPerVoting = parameters["blocks_per_voting_period"]?.Value<int>() ?? 32_768;
            protocol.ByteCost = parameters["cost_per_byte"]?.Value<int>() ?? 1000;
            protocol.EndorsementDeposit = parameters["endorsement_security_deposit"]?.Value<long>() ?? 64_000_000;
            protocol.EndorsementReward0 = parameters["endorsement_reward"]?[0].Value<long>() ?? 1_250_000;
            protocol.EndorsementReward1 = parameters["endorsement_reward"]?[1].Value<long>() ?? 833_333;
            protocol.EndorsersPerBlock = parameters["endorsers_per_block"]?.Value<int>() ?? 32;
            protocol.HardBlockGasLimit = parameters["hard_gas_limit_per_block"]?.Value<int>() ?? 10_400_000;
            protocol.HardOperationGasLimit = parameters["hard_gas_limit_per_operation"]?.Value<int>() ?? 1_040_000;
            protocol.HardOperationStorageLimit = parameters["hard_storage_limit_per_operation"]?.Value<int>() ?? 60_000;
            protocol.OriginationSize = parameters["origination_size"]?.Value<int>() ?? 257;
            protocol.PreservedCycles = parameters["preserved_cycles"]?.Value<int>() ?? 5;
            protocol.RevelationReward = parameters["seed_nonce_revelation_tip"]?.Value<long>() ?? 125_000;
            protocol.TimeBetweenBlocks = parameters["time_between_blocks"]?[0].Value<int>() ?? 60;
            protocol.TokensPerRoll = parameters["tokens_per_roll"]?.Value<long>() ?? 8_000_000_000;
        }

        protected override void UpgradeParameters(Protocol protocol, Protocol prev)
        {
            base.UpgradeParameters(protocol, prev);
            protocol.BlockReward0 = 1_250_000;
            protocol.BlockReward1 = 187_500;
            protocol.EndorsementReward0 = 1_250_000;
            protocol.EndorsementReward1 = 833_333;
            protocol.HardBlockGasLimit = 10_400_000;
            protocol.HardOperationGasLimit = 1_040_000;
        }

        // migrate baker cycles

        protected override async Task MigrateContext(AppState state)
        {
            var block = await Cache.Blocks.CurrentAsync();
            var protocol = await Cache.Protocols.GetAsync(block.ProtoCode);
            var cycle = (block.Level - 1) / protocol.BlocksPerCycle;

            await Db.Database.ExecuteSqlRawAsync($@"
                UPDATE  ""BakerCycles""
                SET     ""FutureBlockRewards"" = ""FutureBlocks"" * 40000000 :: bigint,
                        ""FutureEndorsementRewards"" = ""FutureEndorsements"" * 1250000 :: bigint
                WHERE ""Cycle"" > {cycle};");
        }

        protected override async Task RevertContext(AppState state)
        {
            var block = await Cache.Blocks.CurrentAsync();
            var protocol = await Cache.Protocols.GetAsync(block.ProtoCode);
            var cycle = (block.Level - 1) / protocol.BlocksPerCycle;

            await Db.Database.ExecuteSqlRawAsync($@"
                UPDATE  ""BakerCycles""
                SET     ""FutureBlockRewards"" = ""FutureBlocks"" * 16000000 :: bigint,
                        ""FutureEndorsementRewards"" = ""FutureEndorsements"" * 2000000 :: bigint
                WHERE ""Cycle"" > {cycle};");
        }
    }
}
