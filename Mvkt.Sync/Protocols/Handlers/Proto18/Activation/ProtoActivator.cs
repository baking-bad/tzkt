using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Netmavryk.Encoding;
using Newtonsoft.Json.Linq;
using Mvkt.Data.Models;

namespace Mvkt.Sync.Protocols.Proto18
{
    partial class ProtoActivator : Proto17.ProtoActivator
    {
        public ProtoActivator(ProtocolHandler proto) : base(proto) { }

        protected override async Task<List<Account>> BootstrapAccounts(Protocol protocol, JToken parameters)
        {
            var accounts = await base.BootstrapAccounts(protocol, parameters);

            var bakers = accounts
                .Where(x => x is Data.Models.Delegate d && d.StakingBalance >= protocol.MinimalStake)
                .Select(x => x as Data.Models.Delegate);

            Cache.Statistics.Current.TotalFrozen = 0;
            foreach (var baker in bakers)
            {
                baker.StakedBalance = baker.StakingBalance / (protocol.MaxDelegatedOverFrozenRatio + 1);
                baker.TotalStakedBalance = baker.StakedBalance;
                Cache.Statistics.Current.TotalFrozen += baker.StakedBalance;
            }

            return accounts;
        }

        public override List<Cycle> BootstrapCycles(Protocol protocol, List<Account> accounts, JToken parameters)
        {
            var cycles = base.BootstrapCycles(protocol, accounts, parameters);

            var bakers = accounts
                .Where(x => x is Data.Models.Delegate d && d.StakingBalance >= protocol.MinimalStake)
                .Select(x => x as Data.Models.Delegate);

            var issuances = Proto.Rpc.GetExpectedIssuance(1).Result;

            foreach (var cycle in cycles)
            {
                cycle.TotalBakingPower = bakers.Sum(x => Math.Min(x.StakingBalance, x.TotalStakedBalance * (protocol.MaxDelegatedOverFrozenRatio + 1)));
                cycle.TotalBakers = bakers.Count();

                var issuance = issuances.EnumerateArray().First(x => x.RequiredInt32("cycle") == cycle.Index);

                cycle.BlockReward = issuance.RequiredInt64("baking_reward_fixed_portion");
                cycle.BlockBonusPerSlot = issuance.RequiredInt64("baking_reward_bonus_per_slot");
                cycle.MaxBlockReward = cycle.BlockReward + cycle.BlockBonusPerSlot * (protocol.EndorsersPerBlock - protocol.ConsensusThreshold);
                cycle.EndorsementRewardPerSlot = issuance.RequiredInt64("attesting_reward_per_slot");
                cycle.NonceRevelationReward = issuance.RequiredInt64("seed_nonce_revelation_tip");
                cycle.VdfRevelationReward = issuance.RequiredInt64("vdf_revelation_tip");
                cycle.LBSubsidy = issuance.RequiredInt64("liquidity_baking_subsidy");
            }

            return cycles;
        }

        public override void BootstrapBakerCycles(
            Protocol protocol,
            List<Account> accounts,
            List<Cycle> cycles,
            List<IEnumerable<RightsGenerator.BR>> bakingRights,
            List<IEnumerable<RightsGenerator.ER>> endorsingRights)
        {
            var bakers = accounts
                .Where(x => x.Type == AccountType.Delegate)
                .Select(x => x as Data.Models.Delegate);

            foreach (var cycle in cycles)
            {
                var bakerCycles = bakers.ToDictionary(x => x.Id, x =>
                {
                    var bakerCycle = new BakerCycle
                    {
                        Cycle = cycle.Index,
                        BakerId = x.Id,
                        OwnDelegatedBalance = x.Balance - x.StakedBalance,
                        ExternalDelegatedBalance = x.DelegatedBalance,
                        DelegatorsCount = x.DelegatorsCount,
                        OwnStakedBalance = x.StakedBalance,
                        ExternalStakedBalance = x.ExternalStakedBalance,
                        StakersCount = x.StakersCount,
                        BakingPower = 0,
                        TotalBakingPower = cycle.TotalBakingPower
                    };
                    if (x.StakingBalance >= protocol.MinimalStake)
                    {
                        var bakingPower = Math.Min(x.StakingBalance, x.TotalStakedBalance * (protocol.MaxDelegatedOverFrozenRatio + 1));
                        var expectedEndorsements = (int)(new BigInteger(protocol.BlocksPerCycle) * protocol.EndorsersPerBlock * bakingPower / cycle.TotalBakingPower);
                        bakerCycle.BakingPower = bakingPower;
                        bakerCycle.ExpectedBlocks = protocol.BlocksPerCycle * bakingPower / cycle.TotalBakingPower;
                        bakerCycle.ExpectedEndorsements = expectedEndorsements;
                        bakerCycle.FutureEndorsementRewards = expectedEndorsements * cycle.EndorsementRewardPerSlot;
                    }
                    return bakerCycle;
                });

                #region future baking rights
                foreach (var br in bakingRights[cycle.Index].SkipWhile(x => x.Level == 1).Where(x => x.Round == 0))
                {
                    if (!bakerCycles.TryGetValue(br.Baker, out var bakerCycle))
                        throw new Exception("Unknown baking right recipient");

                    bakerCycle.FutureBlocks++;
                    bakerCycle.FutureBlockRewards += cycle.MaxBlockReward;
                }
                #endregion

                #region future endorsing rights
                foreach (var er in endorsingRights[cycle.Index].TakeWhile(x => x.Level < cycle.LastLevel))
                {
                    if (!bakerCycles.TryGetValue(er.Baker, out var bakerCycle))
                        throw new Exception("Unknown endorsing right recipient");

                    bakerCycle.FutureEndorsements += er.Slots;
                }
                #endregion

                #region shifted future endirsing rights
                if (cycle.Index > 0)
                {
                    foreach (var er in endorsingRights[cycle.Index - 1].Reverse().TakeWhile(x => x.Level == cycle.FirstLevel - 1))
                    {
                        if (!bakerCycles.TryGetValue(er.Baker, out var bakerCycle))
                            throw new Exception("Unknown endorsing right recipient");

                        bakerCycle.FutureEndorsements += er.Slots;
                    }
                }
                #endregion

                Db.BakerCycles.AddRange(bakerCycles.Values);
            }
        }

        protected override void SetParameters(Protocol protocol, JToken parameters)
        {
            base.SetParameters(protocol, parameters);
            protocol.MinimalFrozenStake = parameters["minimal_frozen_stake"]?.Value<long>() ?? 600_000_000;
            protocol.MaxDelegatedOverFrozenRatio = parameters["limit_of_delegation_over_baking"]?.Value<int>() ?? 9;
            protocol.MaxExternalOverOwnStakeRatio = parameters["global_limit_of_staking_over_baking"]?.Value<int>() ?? 5;
            protocol.StakePowerMultiplier = parameters["edge_of_staking_over_delegation"]?.Value<int>() ?? 2;
            
            protocol.BaseIssuedPerMinute = parameters["issuance_weights"]?["base_total_issued_per_minute"]?.Value<long>() ?? 85_007_812;
            protocol.BlockRewardWeight = parameters["issuance_weights"]?["baking_reward_fixed_portion_weight"]?.Value<int>() ?? 5120;
            protocol.BlockBonusWeight = parameters["issuance_weights"]?["baking_reward_bonus_weight"]?.Value<int>() ?? 5120;
            protocol.EndorsingRewardWeight = parameters["issuance_weights"]?["attesting_reward_weight"]?.Value<int>() ?? 10240;
            protocol.NonceRevelationRewardWeight = parameters["issuance_weights"]?["seed_nonce_revelation_tip_weight"]?.Value<int>() ?? 1;
            protocol.VdfRevelationRewardWeight = parameters["issuance_weights"]?["vdf_revelation_tip_weight"]?.Value<int>() ?? 1;
            protocol.LBSubsidyWeight = parameters["issuance_weights"]?["liquidity_baking_subsidy_weight"]?.Value<int>() ?? 1280;

            protocol.DoubleBakingSlashedPercentage = parameters["percentage_of_frozen_deposits_slashed_per_double_baking"]?.Value<int>() ?? 5;
            protocol.DoubleEndorsingSlashedPercentage = parameters["percentage_of_frozen_deposits_slashed_per_double_attestation"]?.Value<int>() ?? 50;

            protocol.BlockDeposit = 0;
            protocol.BlockReward0 = 0;
            protocol.BlockReward1 = 0;
            protocol.MaxBakingReward = 0;
            protocol.EndorsementDeposit = 0;
            protocol.EndorsementReward0 = 0;
            protocol.EndorsementReward1 = 0;
            protocol.MaxEndorsingReward = 0;
        }

        protected override void UpgradeParameters(Protocol protocol, Protocol prev)
        {
            protocol.MinimalFrozenStake = 600_000_000;
            protocol.MaxDelegatedOverFrozenRatio = 9;
            protocol.MaxExternalOverOwnStakeRatio = 5;
            protocol.StakePowerMultiplier = 2;

            protocol.BaseIssuedPerMinute = 85_007_812;
            protocol.BlockRewardWeight = 5120;
            protocol.BlockBonusWeight = 5120;
            protocol.EndorsingRewardWeight = 10240;
            protocol.NonceRevelationRewardWeight = 1;
            protocol.VdfRevelationRewardWeight = 1;
            protocol.LBSubsidyWeight = 1280;

            protocol.DoubleBakingSlashedPercentage = 5;
            protocol.DoubleEndorsingSlashedPercentage = 50;

            protocol.BlockDeposit = 0;
            protocol.BlockReward0 = 0;
            protocol.BlockReward1 = 0;
            protocol.MaxBakingReward = 0;
            protocol.EndorsementDeposit = 0;
            protocol.EndorsementReward0 = 0;
            protocol.EndorsementReward1 = 0;
            protocol.MaxEndorsingReward = 0;
        }

        protected override async Task MigrateContext(AppState state)
        {
            await RemoveDeadRefutationGames(state);
            await RemoveBigmapKeys(state);
            await MigrateBakers(state);
        }

        protected async Task RemoveDeadRefutationGames(AppState state)
        {
            var activeGames = await Db.RefutationGames
                .AsNoTracking()
                .Where(x =>
                    x.InitiatorReward == null &&
                    x.InitiatorLoss == null &&
                    x.OpponentReward == null &&
                    x.OpponentLoss == null)
                .ToListAsync();

            foreach (var game in activeGames)
            {
                var initiatorBond = await Db.SmartRollupPublishOps
                    .AsNoTracking()
                    .Where(x =>
                        x.SmartRollupId == game.SmartRollupId &&
                        x.BondStatus == SmartRollupBondStatus.Active &&
                        x.SenderId == game.InitiatorId)
                    .FirstOrDefaultAsync();

                if (initiatorBond != null)
                    continue;

                var opponentBond = await Db.SmartRollupPublishOps
                    .AsNoTracking()
                    .Where(x =>
                        x.SmartRollupId == game.SmartRollupId &&
                        x.BondStatus == SmartRollupBondStatus.Active &&
                        x.SenderId == game.OpponentId)
                    .FirstOrDefaultAsync();

                if (opponentBond != null)
                    continue;

                Db.TryAttach(game);
                game.LastLevel = state.Level;
                game.InitiatorReward = 0;
                game.InitiatorLoss = 0;
                game.OpponentReward = 0;
                game.OpponentLoss = 0;

                var initiator = await Cache.Accounts.GetAsync(game.InitiatorId);
                Db.TryAttach(initiator);
                initiator.ActiveRefutationGamesCount--;

                var opponent = await Cache.Accounts.GetAsync(game.OpponentId);
                Db.TryAttach(opponent);
                opponent.ActiveRefutationGamesCount--;

                var rollup = await Cache.Accounts.GetAsync(game.SmartRollupId);
                Db.TryAttach(rollup);
                rollup.ActiveRefutationGamesCount--;
            }
        }

        async Task RemoveBigmapKeys(AppState state)
        {
            var keyHashes = new HashSet<string>
            {
                "exprtXBtxJxCDEDETueKAFLL7r7vZtNEo1MHajpHba1djtGKqJzWd3",
                "exprtbuRhaGDS942BgZ1qFdD7HAKeBjPEqzRxgLQyWQ6HWxcaiLC2c",
                "exprtePxSLgrhJmTPZEePyFBmESLhaBUN1WodvLYy9xYhEYE6dKPLe",
                "exprtx9GaYz5Fy5ytiuYgSfJqeYqkxGgobust8U6dpCLaeZUMiitmg",
                "expru28t4XoyB61WuRQnExk3Kq8ssGv1ejgdo9XHxpTXoQjXTGw1Dg",
                "expru2fZALknjB4vJjmQBPkrs3dJZ5ytuzfmE9A7ScUk5opJiZQyiJ",
                "expru2riAFKURjHJ1vNpvsZGGw6z4wtTvbstXVuwQPj1MaTqKPeQ6z",
                "expruHoZDr8ioVhaAs495crYTprAYyC87CruEJ6HaS7diYV6qLARqQ",
                "expruMie2gfy5smMd81NtcvvWm4jD7ThUebw9hpF3N3apKVtxkVG9M",
                "expruc3QW7cdxrGurDJQa6k9QqMZjGkRDJahy2XNtBt9WQzC1yavJK",
                "exprud86wYL7inFCVHkF1Jcz8uMXVY7dnbzxVupyyknZjtDVmwoQTJ",
                "exprufYzeBTGn9733Ga8xEEmU4SsrSyDrzEip8V8hTBAG253T5zZQx",
                "exprum9tuHNvisMa3c372AFmCa27rmkbCGrhzMSprrxgJjzXhrKAag",
                "expruokt7oQ6dDHRvL4sURKUzfwJirR8FPHvpXwjgUD4KHhPWhDGbv",
                "expruom5ds2hVgjdTB877Fx3ZuWT5WUnw1H6kUZavVHcJFbCkcgo3x",
                "exprv2DPd1pV3GVSN2CgW7PPrAQUTuZAdeJphwToQrTNrxiJcWzvtX",
                "exprv65Czv5TnKyEWgBHjDztkCkc1FAVEPxZ3V3ocgvGjfXwjPLo8M",
                "exprv6S2KAvqAC18jDLYjaj1w9oc4ESdDGJkUZ63EpkqSTAz88cSYB",
                "exprvNg3VDBnhtTHvc75krAEYzz6vUMr3iU5jtLdxs83FbgTbZ9nFT",
                "exprvS7wNDHYKYZ19nj3ZUo7AAVMCDpTK3NNERFhqe5SJGCBL4pwFA"
            };

            var valueType = new MichelinePrim
            {
                Prim = PrimType.pair,
                Args = new(2)
                {
                    new MichelinePrim
                    {
                        Prim = PrimType.timestamp
                    },
                    new MichelinePrim
                    {
                        Prim = PrimType.ticket,
                        Args = new(1)
                        {
                            new MichelinePrim
                            {
                                Prim = PrimType.@string,
                                Annots = new(1)
                                {
                                    new FieldAnnotation("data")
                                }
                            }
                        }
                    }
                }
            };

            var bigmap = await Db.BigMaps
                .FirstOrDefaultAsync(x => x.Ptr == 5696);

            if (bigmap?.ValueType.IsEqual(valueType.ToBytes()) != true)
                return;

            var block = await Cache.Blocks.CurrentAsync();
            
            var contract = await Cache.Accounts.GetAsync(bigmap.ContractId);
            Db.TryAttach(contract);

            var keys = await Db.BigMapKeys
                .Where(x => x.BigMapPtr == bigmap.Ptr && x.Active && keyHashes.Contains(x.KeyHash))
                .ToListAsync();

            foreach (var key in keys)
            {
                var value = Micheline.FromBytes(key.RawValue);
                if (((((value as MichelinePrim).Args[1] as MichelinePrim).Args[1] as MichelinePrim).Args[1] as MichelineInt).Value == BigInteger.Zero)
                {
                    var migration = new MigrationOperation
                    {
                        Id = Cache.AppState.NextOperationId(),
                        Block = block,
                        Account = contract,
                        AccountId = contract.Id,
                        BigMapUpdates = 1,
                        Kind = MigrationKind.RemoveBigMapKey,
                        Level = block.Level,
                        Timestamp = block.Timestamp
                    };
                    Db.MigrationOps.Add(migration);

                    block.Operations |= Operations.Migrations;
                    contract.MigrationsCount++;
                    state.MigrationOpsCount++;

                    var update = new BigMapUpdate
                    {
                        Id = Cache.AppState.NextBigMapUpdateId(),
                        Action = BigMapAction.RemoveKey,
                        BigMapKeyId = key.Id,
                        BigMapPtr = bigmap.Ptr,
                        JsonValue = key.JsonValue,
                        Level = block.Level,
                        MigrationId = migration.Id,
                        RawValue = key.RawValue
                    };
                    Db.BigMapUpdates.Add(update);

                    key.Active = false;
                    key.LastLevel = block.Level;
                    key.Updates++;

                    bigmap.ActiveKeys--;
                    bigmap.LastLevel = block.Level;
                    bigmap.Updates++;
                }
            }
        }

        async Task MigrateBakers(AppState state)
        {
            var stakes = (await Proto.Node.GetAsync($"chains/main/blocks/{state.Level}/context/raw/json/staking_balance/current?depth=1"))
                .EnumerateArray()
                .ToDictionary(
                    x => x.EnumerateArray().First().RequiredString(),
                    x => x.EnumerateArray().Last().RequiredInt64("own_frozen"));

            foreach (var baker in Cache.Accounts.GetDelegates())
            {
                Db.TryAttach(baker);
                baker.StakedBalance = stakes.GetValueOrDefault(baker.Address);
                baker.TotalStakedBalance = stakes.GetValueOrDefault(baker.Address);
            }
        }
    }
}
