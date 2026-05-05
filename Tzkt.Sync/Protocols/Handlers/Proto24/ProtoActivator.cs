using Microsoft.EntityFrameworkCore;
using Netezos.Contracts;
using Netezos.Encoding;
using System.Text.Json;
using Tzkt.Data;
using Tzkt.Data.Models;
using Tzkt.Sync.Services;

namespace Tzkt.Sync.Protocols.Proto24
{
    public class ProtoActivator(ProtocolHandler proto) : IActivator
    {
        protected readonly ProtocolHandler Proto = proto;
        protected readonly IRpc Rpc = proto.Rpc;
        protected readonly TzktContext Db = proto.Db;
        protected readonly CacheService Cache = proto.Cache;
        protected readonly BlockContext Context = proto.Context;
        protected readonly ILogger Logger = proto.Logger;

        public async Task ActivateContext(AppState state, JsonElement rawBlock)
        {
            var header = rawBlock.Required("header");
            var level = header.RequiredInt32("level");
            var timestamp = header.RequiredDateTime("timestamp");
            var protocolHash = rawBlock.RequiredString("protocol");

            #region protocol
            var constants = await Rpc.GetConstantsAsync(level);
            var protocol = new Protocol
            {
                Id = 0,
                Code = protocolHash == "PrihK96nBAFSxVL1GLJTVhu9YnzkMFiBeuJRPA8NwuZVZCE1L6i" ? -1 : 1,
                Hash = protocolHash,
                Version = Proto.VersionNumber,
                FirstLevel = level,
                LastLevel = -1,
                FirstCycle = 0,
                FirstCycleLevel = level,

                RampUpCycles = constants.OptionalInt32("security_deposit_ramp_up_cycles") ?? 0,
                NoRewardCycles = constants.OptionalInt32("no_reward_cycles") ?? 0,
                ByteCost = constants.OptionalInt32("cost_per_byte") ?? 250,
                HardOperationGasLimit = constants.OptionalInt32("hard_gas_limit_per_operation") ?? 1_040_000,
                HardOperationStorageLimit = constants.OptionalInt32("hard_storage_limit_per_operation") ?? 60_000,
                OriginationSize = constants.OptionalInt32("origination_size") ?? 257,
                BallotQuorumMin = constants.OptionalInt32("quorum_min") ?? 2000,
                BallotQuorumMax = constants.OptionalInt32("quorum_max") ?? 7000,
                ProposalQuorum = constants.OptionalInt32("min_proposal_quorum") ?? 500,

                BlocksPerCycle = constants.OptionalInt32("blocks_per_cycle") ?? 8192,
                BlocksPerCommitment = constants.OptionalInt32("blocks_per_commitment") ?? 64,

                HardBlockGasLimit = constants.OptionalInt32("hard_gas_limit_per_block") ?? 5_200_000,
                TimeBetweenBlocks = constants.OptionalInt32("minimal_block_delay") ?? 30,

                AttestersPerBlock = constants.OptionalInt32("consensus_committee_size") ?? 7000,

                MinParticipationNumerator = constants.Optional("minimal_participation_ratio")?.OptionalInt32("numerator") ?? 2,
                MinParticipationDenominator = constants.Optional("minimal_participation_ratio")?.OptionalInt32("denominator") ?? 3,

                LBToggleThreshold = constants.OptionalInt32("liquidity_baking_toggle_ema_threshold") ?? 1_000_000_000,
                BlocksPerVoting = (constants.OptionalInt32("cycles_per_voting_period") ?? 5) * (constants.OptionalInt32("blocks_per_cycle") ?? 8192),
                Dictator = constants.OptionalString("testnet_dictator"),
                MinimalStake = constants.OptionalInt64("minimal_stake") ?? 6_000_000L,
                SmartRollupOriginationSize = constants.OptionalInt32("smart_rollup_origination_size") ?? 6_314,
                SmartRollupStakeAmount = constants.OptionalInt64("smart_rollup_stake_amount") ?? 10_000_000_000L,
                SmartRollupChallengeWindow = constants.OptionalInt32("smart_rollup_challenge_window_in_blocks") ?? 80_640,
                SmartRollupCommitmentPeriod = constants.OptionalInt32("smart_rollup_commitment_period_in_blocks") ?? 60,
                SmartRollupTimeoutPeriod = constants.OptionalInt32("smart_rollup_timeout_period_in_blocks") ?? 40_320,

                MinimalFrozenStake = constants.OptionalInt64("minimal_frozen_stake") ?? 600_000_000,
                MaxDelegatedOverFrozenRatio = constants.OptionalInt32("limit_of_delegation_over_baking") ?? 9,
                MaxExternalOverOwnStakeRatio = constants.OptionalInt32("global_limit_of_staking_over_baking") ?? 5,
                StakePowerMultiplier = constants.OptionalInt32("edge_of_staking_over_delegation") ?? 2,

                BlockDeposit = 0,
                BlockReward0 = 0,
                BlockReward1 = 0,
                MaxBakingReward = 0,
                AttestationDeposit = 0,
                AttestationReward0 = 0,
                AttestationReward1 = 0,
                MaxAttestationReward = 0,

                ConsensusRightsDelay = constants.OptionalInt32("consensus_rights_delay") ?? 2,
                DelegateParametersActivationDelay = constants.OptionalInt32("delegate_parameters_activation_delay") ?? 5,
                DoubleBakingSlashedPercentage = constants.OptionalInt32("percentage_of_frozen_deposits_slashed_per_double_baking") ?? 500,
                DoubleConsensusSlashedPercentage = constants.OptionalInt32("percentage_of_frozen_deposits_slashed_per_double_attestation") ?? 5000,
                NumberOfShards = constants.Optional("dal_parametric")?.OptionalInt32("number_of_shards") ?? 512,
                BlocksPerSnapshot = constants.OptionalInt32("blocks_per_cycle") ?? 8192,

                ConsensusThreshold = constants.OptionalInt32("consensus_threshold_size") ?? 4667,
                DenunciationPeriod = constants.OptionalInt32("denunciation_period") ?? 1,
                SlashingDelay = constants.OptionalInt32("slashing_delay") ?? 1,
                ToleratedInactivityPeriod = constants.OptionalInt32("tolerated_inactivity_period") ?? 2,
            };

            state.ProtocolsCount++;

            Db.Protocols.Add(protocol);
            Cache.Protocols.Add(protocol);
            Context.Protocol = protocol;
            #endregion

            #region null-address
            var nullAddress = new User
            {
                Id = Cache.AppState.NextAccountId(),
                Address = NullAddress.Address,
                Type = AccountType.User,
                FirstLevel = level,
                LastLevel = level,
                Index = 0
            };

            if (nullAddress.Id != NullAddress.Id)
                throw new Exception("Failed to allocate null-address");

            Cache.Accounts.Add(nullAddress);
            Db.Accounts.Add(nullAddress);
            #endregion

            #region accounts
            var stats = new Statistics
            {
                Id = 0,
                Level = level - 1
            };
            Cache.Statistics.SetCurrent(stats);

            var accounts = new List<Account>();
            var addresses = await Rpc.GetContractsAsync(level);
            foreach (var address in addresses.EnumerateArray().Select(x => x.RequiredString()))
            {
                if (address.StartsWith("KT1"))
                    throw new NotImplementedException("Smart contracts bootstrap is not implemented");

                var rawContract = await Rpc.GetContractAsync(level, address);
                var rawKey = await Rpc.GetContractManagerKeyAsync(level, address);

                #region account
                var user = new User
                {
                    Id = Cache.AppState.NextAccountId(),
                    Address = address,
                    FirstLevel = level,
                    LastLevel = level,
                    Type = AccountType.User,
                    Balance = rawContract.RequiredInt64("balance"),
                    Counter = rawContract.RequiredInt32("counter"),
                    PublicKey = rawKey.OptionalString(),
                    Revealed = rawKey.OptionalString() != null
                };

                Cache.Accounts.Add(user);
                Db.Accounts.Add(user);
                #endregion

                #region migration
                var migration = new MigrationOperation
                {
                    Id = Cache.AppState.NextOperationId(),
                    Level = level,
                    Timestamp = timestamp,
                    AccountId = user.Id,
                    Kind = MigrationKind.Bootstrap,
                    BalanceChange = user.Balance,
                };

                user.MigrationsCount++;
                state.MigrationOpsCount++;
                stats.TotalBootstrapped += migration.BalanceChange;

                Db.MigrationOps.Add(migration);
                Context.MigrationOps.Add(migration);
                #endregion
            }
            #endregion

            #region precompiles
            foreach (var address in Proto.Config.Precompiles ?? [])
            {
                var rawContract = await Proto.Rpc.GetContractAsync(level, address);

                var balance = rawContract.RequiredInt64("balance");
                var rawScript = rawContract.Required("script");
                var codeStr = rawScript.Required("code").GetRawText();
                var storageStr = rawScript.Required("storage").GetRawText();

                #region contract
                var creator = nullAddress;
                var contract = new Contract
                {
                    Id = Cache.AppState.NextAccountId(),
                    Address = address,
                    FirstLevel = level,
                    LastLevel = level,
                    Type = AccountType.Contract,
                    Kind = ContractKind.SmartContract,
                    Balance = balance,
                    CreatorId = creator.Id,
                };

                creator.ContractsCount++;

                Cache.Accounts.Add(contract);
                Db.Accounts.Add(contract);
                #endregion

                #region script
                var code = (Micheline.FromJson(codeStr) as MichelineArray)!;
                var micheParameter = code.First(x => x is MichelinePrim p && p.Prim == PrimType.parameter);
                var micheStorage = code.First(x => x is MichelinePrim p && p.Prim == PrimType.storage);
                var micheCode = code.First(x => x is MichelinePrim p && p.Prim == PrimType.code);
                var micheViews = code.Where(x => x is MichelinePrim p && p.Prim == PrimType.view);
                var script = new Script
                {
                    Id = Cache.AppState.NextScriptId(),
                    Level = level,
                    ContractId = contract.Id,
                    ParameterSchema = micheParameter.ToBytes(),
                    StorageSchema = micheStorage.ToBytes(),
                    CodeSchema = micheCode.ToBytes(),
                    Views = micheViews.Any()
                        ? [.. micheViews.Select(x => x.ToBytes())]
                        : null,
                    Current = true
                };

                var viewsBytes = script.Views?
                    .OrderBy(x => x, new BytesComparer())
                    .SelectMany(x => x)
                    .ToArray()
                    ?? [];
                var typeSchema = script.ParameterSchema.Concat(script.StorageSchema).Concat(viewsBytes);
                var fullSchema = typeSchema.Concat(script.CodeSchema);
                contract.TypeHash = script.TypeHash = Script.GetHash(typeSchema);
                contract.CodeHash = script.CodeHash = Script.GetHash(fullSchema);

                if (script.Schema.IsFA1())
                {
                    if (script.Schema.IsFA12())
                        contract.Tags |= ContractTags.FA12;

                    contract.Tags |= ContractTags.FA1;
                    contract.Kind = ContractKind.Asset;
                }
                if (script.Schema.IsFA2())
                {
                    contract.Tags |= ContractTags.FA2;
                    contract.Kind = ContractKind.Asset;
                }

                Db.Scripts.Add(script);
                Cache.Schemas.Add(contract, script.Schema);
                #endregion

                #region storage
                var storageValue = Micheline.FromJson(storageStr)!;
                var storage = new Storage
                {
                    Id = Cache.AppState.NextStorageId(),
                    Level = level,
                    ContractId = contract.Id,
                    RawValue = script.Schema.OptimizeStorage(storageValue, false).ToBytes(),
                    JsonValue = script.Schema.HumanizeStorage(storageValue),
                    Current = true
                };

                Db.Storages.Add(storage);
                Cache.Storages.Add(contract, storage);
                #endregion

                #region migration
                var migration = new MigrationOperation
                {
                    Id = Cache.AppState.NextOperationId(),
                    Level = level,
                    Timestamp = timestamp,
                    AccountId = contract.Id,
                    Kind = MigrationKind.Origination,
                    BalanceChange = contract.Balance,
                    ScriptId = script.Id,
                    StorageId = storage.Id
                };

                script.MigrationId = migration.Id;
                storage.MigrationId = migration.Id;

                contract.MigrationsCount++;
                
                state.MigrationOpsCount++;
                stats.TotalBootstrapped += migration.BalanceChange;

                Db.MigrationOps.Add(migration);
                Context.MigrationOps.Add(migration);
                #endregion
            }
            #endregion
        }

        public async Task DeactivateContext(AppState state)
        {
            await Db.Database.ExecuteSqlRawAsync("""
                DELETE FROM "Protocols";
                DELETE FROM "Accounts";
                DELETE FROM "MigrationOps";
                DELETE FROM "Scripts";
                DELETE FROM "Storages";
                """);

            await Cache.Protocols.ResetAsync();
            await Cache.Accounts.ResetAsync();
            Cache.Schemas.Reset();
            Cache.Storages.Reset();

            Cache.AppState.ReleaseOperationId(state.MigrationOpsCount);
            state.ProtocolsCount = 0;
            state.AccountCounter = 0;
            state.MigrationOpsCount = 0;
            state.ScriptCounter = 0;
            state.StorageCounter = 0;
        }
    }
}
