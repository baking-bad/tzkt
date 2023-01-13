using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Netezos.Contracts;
using Netezos.Encoding;
using Newtonsoft.Json.Linq;
using Npgsql;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto15
{
    partial class ProtoActivator : Proto14.ProtoActivator
    {
        public ProtoActivator(ProtocolHandler proto) : base(proto) { }

        protected override void SetParameters(Protocol protocol, JToken parameters)
        {
            base.SetParameters(protocol, parameters);
            protocol.TokensPerRoll = parameters["minimal_stake"]?.Value<long>() ?? 6_000_000L;
        }

        protected override async Task MigrateContext(AppState state)
        {
            var prevProto = await Cache.Protocols.GetAsync(state.Protocol);
            var nextProto = await Cache.Protocols.GetAsync(state.NextProtocol);

            await AddInvoice(state, "tz1MidLyXXvKWMmbRvKKeusDtP95NDJ5gAUx", 10_000_000_000L);
            await AddInvoice(state, "tz1X81bCXPtMiHu1d4UZF4GPhMPkvkp56ssb", 15_000_000_000L);
            await MigrateCurrentRights(state, prevProto, nextProto);
            await MigrateFutureRights(state, nextProto);
            await PatchContracts(state);

            if (state.ChainId == "NetXnHfVqm9iesp") // ghostnet: amend broken voting period
            {
                await RestartVotingPeriod(state, nextProto);
            }
        }

        protected override Task RevertContext(AppState state)
        {
            throw new NotImplementedException("Reverting Lima migration block is not implemented, because likely won't be needed");
        }

        async Task RestartVotingPeriod(AppState state, Protocol nextProto)
        {
            var currentPeriod = await Cache.Periods.GetAsync(58);
            Db.TryAttach(currentPeriod);

            #region update current period
            currentPeriod.LastLevel = state.Level;
            currentPeriod.Status = currentPeriod.ProposalsCount == 0
                ? PeriodStatus.NoProposals
                : PeriodStatus.NoQuorum;
            #endregion

            #region update proposals status
            if (currentPeriod.ProposalsCount > 0)
            {
                var proposals = await Db.Proposals
                    .Where(x => x.Status == ProposalStatus.Active)
                    .ToListAsync();

                var pendings = Db.ChangeTracker.Entries()
                    .Where(x => x.Entity is Proposal p && p.Status == ProposalStatus.Active)
                    .Select(x => x.Entity as Proposal)
                    .ToList();

                foreach (var pending in pendings)
                    if (!proposals.Any(x => x.Id == pending.Id))
                        proposals.Add(pending);

                foreach (var proposal in proposals)
                    proposal.Status = ProposalStatus.Skipped;
            }
            #endregion

            #region update snapshots
            await Db.Database.ExecuteSqlRawAsync("""
                DELETE FROM "VotingSnapshots"
                WHERE "Period" > 58;
                """);

            Db.VotingSnapshots.AddRange((await Db.VotingSnapshots
                .AsNoTracking()
                .Where(x => x.Period == 58)
                .ToListAsync())
                .Select(x => new VotingSnapshot
                {
                    BakerId = x.BakerId,
                    Level = x.Level,
                    Period = x.Period + 1,
                    Status = VoterStatus.None,
                    VotingPower = x.VotingPower
                }));
            #endregion

            #region add next period
            await Db.Database.ExecuteSqlRawAsync("""
                DELETE FROM "VotingPeriods"
                WHERE "Index" > 58;
                """);

            Db.VotingPeriods.Add(new VotingPeriod
            {
                Index = currentPeriod.Index + 1,
                Epoch = currentPeriod.Epoch + 1,
                FirstLevel = currentPeriod.LastLevel + 1,
                LastLevel = currentPeriod.LastLevel + nextProto.BlocksPerVoting,
                Kind = PeriodKind.Proposal,
                Status = PeriodStatus.Active,
                TotalBakers = currentPeriod.TotalBakers,
                TotalVotingPower = currentPeriod.TotalVotingPower,
                UpvotesQuorum = nextProto.ProposalQuorum,
                ProposalsCount = 0,
                TopUpvotes = 0,
                TopVotingPower = 0,
                SingleWinner = false,
            });
            #endregion

            state.VotingPeriod = currentPeriod.Index + 1;
            state.VotingEpoch = currentPeriod.Epoch + 1;
            Cache.Periods.Reset();
        }

        async Task AddInvoice(AppState state, string address, long amount)
        {
            var block = await Cache.Blocks.CurrentAsync();
            var account = await Cache.Accounts.GetAsync(address);

            Db.TryAttach(account);
            account.FirstLevel = Math.Min(account.FirstLevel, state.Level);
            account.LastLevel = state.Level;
            account.Balance += amount;
            account.MigrationsCount++;

            block.Operations |= Operations.Migrations;
            Db.MigrationOps.Add(new MigrationOperation
            {
                Id = Cache.AppState.NextOperationId(),
                Block = block,
                Level = block.Level,
                Timestamp = block.Timestamp,
                Account = account,
                Kind = MigrationKind.ProposalInvoice,
                BalanceChange = amount
            });

            state.MigrationOpsCount++;

            var stats = await Cache.Statistics.GetAsync(state.Level);
            stats.TotalCreated += amount;
        }

        async Task MigrateCurrentRights(AppState state, Protocol prevProto, Protocol nextProto)
        {
            var cycle = await Db.Cycles.AsNoTracking().FirstAsync(x => x.Index == state.Cycle);
            if (state.Level == cycle.LastLevel) return;

            var bakerCycles = await Cache.BakerCycles.GetAsync(state.Cycle);

            #region revert current rights
            var rights = await Db.BakingRights
                .AsNoTracking()
                .Where(x => x.Level > state.Level && x.Cycle == state.Cycle)
                .ToListAsync();

            foreach (var br in rights.Where(x => x.Type == BakingRightType.Baking && x.Round == 0))
            {
                var bakerCycle = bakerCycles[br.BakerId];
                Db.TryAttach(bakerCycle);

                bakerCycle.FutureBlocks--;
                bakerCycle.FutureBlockRewards -= prevProto.MaxBakingReward;
            }

            foreach (var er in rights.Where(x => x.Type == BakingRightType.Endorsing))
            {
                var bakerCycle = bakerCycles[er.BakerId];
                Db.TryAttach(bakerCycle);

                bakerCycle.FutureEndorsements -= (int)er.Slots;
            }

            await Db.Database.ExecuteSqlRawAsync($@"DELETE FROM ""BakingRights"" WHERE ""Level"" > {state.Level} AND ""Cycle"" = {state.Cycle}");
            #endregion

            #region apply new rights
            var sampler = GetOldSampler(bakerCycles.Values
                .Where(x => x.ActiveStake > 0)
                .Select(x => (x.BakerId, x.ActiveStake))
                .ToList());

            #region temporary diagnostics
            await sampler.Validate(Proto, state.Level, cycle.Index);
            #endregion

            var brs = new List<RightsGenerator.BR>();
            var ers = new List<RightsGenerator.ER>();
            for (int level = state.Level + 1; level <= cycle.LastLevel; level++)
            {
                foreach (var br in RightsGenerator.GetBakingRights(sampler, cycle, level))
                {
                    brs.Add(br);
                    if (br.Round == 0)
                    {
                        var bakerCycle = bakerCycles[br.Baker];
                        Db.TryAttach(bakerCycle);
                        bakerCycle.FutureBlocks++;
                        bakerCycle.FutureBlockRewards += nextProto.MaxBakingReward;
                    }
                }
                foreach (var er in RightsGenerator.GetEndorsingRights(sampler, nextProto, cycle, level - 1))
                {
                    ers.Add(er);
                    var bakerCycle = bakerCycles[er.Baker];
                    Db.TryAttach(bakerCycle);
                    bakerCycle.FutureEndorsements += er.Slots;
                }
            }

            var conn = Db.Database.GetDbConnection() as NpgsqlConnection;
            using var writer = conn.BeginBinaryImport(@"
                COPY ""BakingRights"" (""Cycle"", ""Level"", ""BakerId"", ""Type"", ""Status"", ""Round"", ""Slots"")
                FROM STDIN (FORMAT BINARY)");

            foreach (var er in ers)
            {
                writer.StartRow();
                writer.Write(cycle.Index, NpgsqlTypes.NpgsqlDbType.Integer);
                writer.Write(er.Level + 1, NpgsqlTypes.NpgsqlDbType.Integer);
                writer.Write(er.Baker, NpgsqlTypes.NpgsqlDbType.Integer);
                writer.Write((byte)BakingRightType.Endorsing, NpgsqlTypes.NpgsqlDbType.Smallint);
                writer.Write((byte)BakingRightStatus.Future, NpgsqlTypes.NpgsqlDbType.Smallint);
                writer.WriteNull();
                writer.Write(er.Slots, NpgsqlTypes.NpgsqlDbType.Integer);
            }

            foreach (var br in brs)
            {
                writer.StartRow();
                writer.Write(cycle.Index, NpgsqlTypes.NpgsqlDbType.Integer);
                writer.Write(br.Level, NpgsqlTypes.NpgsqlDbType.Integer);
                writer.Write(br.Baker, NpgsqlTypes.NpgsqlDbType.Integer);
                writer.Write((byte)BakingRightType.Baking, NpgsqlTypes.NpgsqlDbType.Smallint);
                writer.Write((byte)BakingRightStatus.Future, NpgsqlTypes.NpgsqlDbType.Smallint);
                writer.Write(br.Round, NpgsqlTypes.NpgsqlDbType.Integer);
                writer.WriteNull();
            }

            writer.Complete();
            #endregion
        }

        async Task MigrateFutureRights(AppState state, Protocol nextProto)
        {
            await Db.Database.ExecuteSqlRawAsync($@"DELETE FROM ""BakingRights"" WHERE ""Cycle"" > {state.Cycle}");

            var cycles = await Db.Cycles
                .AsNoTracking()
                .Where(x => x.Index >= state.Cycle)
                .OrderBy(x => x.Index)
                .ToListAsync();

            var conn = Db.Database.GetDbConnection() as NpgsqlConnection;
            IEnumerable<RightsGenerator.ER> shifted = Enumerable.Empty<RightsGenerator.ER>();

            foreach (var cycle in cycles)
            {
                var bakerCycles = await Cache.BakerCycles.GetAsync(cycle.Index);
                var sampler = GetOldSampler(bakerCycles.Values
                    .Where(x => x.ActiveStake > 0)
                    .Select(x => (x.BakerId, x.ActiveStake))
                    .ToList());

                #region temporary diagnostics
                await sampler.Validate(Proto, state.Level, cycle.Index);
                #endregion

                if (cycle.Index == state.Cycle)
                {
                    shifted = RightsGenerator.GetEndorsingRights(sampler, nextProto, cycle, cycle.LastLevel);

                    #region save shifted
                    using (var writer = conn.BeginBinaryImport(@"
                        COPY ""BakingRights"" (""Cycle"", ""Level"", ""BakerId"", ""Type"", ""Status"", ""Round"", ""Slots"")
                        FROM STDIN (FORMAT BINARY)"))
                    {
                        foreach (var er in shifted)
                        {
                            writer.StartRow();
                            writer.Write(cycle.Index + 1, NpgsqlTypes.NpgsqlDbType.Integer);
                            writer.Write(er.Level + 1, NpgsqlTypes.NpgsqlDbType.Integer);
                            writer.Write(er.Baker, NpgsqlTypes.NpgsqlDbType.Integer);
                            writer.Write((byte)BakingRightType.Endorsing, NpgsqlTypes.NpgsqlDbType.Smallint);
                            writer.Write((byte)BakingRightStatus.Future, NpgsqlTypes.NpgsqlDbType.Smallint);
                            writer.WriteNull();
                            writer.Write(er.Slots, NpgsqlTypes.NpgsqlDbType.Integer);
                        }

                        writer.Complete();
                    }
                    #endregion
                }
                else
                {
                    GC.Collect();
                    var brs = await RightsGenerator.GetBakingRightsAsync(sampler, nextProto, cycle);
                    var ers = await RightsGenerator.GetEndorsingRightsAsync(sampler, nextProto, cycle);

                    #region save rights
                    using (var writer = conn.BeginBinaryImport(@"
                    COPY ""BakingRights"" (""Cycle"", ""Level"", ""BakerId"", ""Type"", ""Status"", ""Round"", ""Slots"")
                    FROM STDIN (FORMAT BINARY)"))
                    {
                        foreach (var er in ers)
                        {
                            writer.StartRow();
                            writer.Write(nextProto.GetCycle(er.Level + 1), NpgsqlTypes.NpgsqlDbType.Integer);
                            writer.Write(er.Level + 1, NpgsqlTypes.NpgsqlDbType.Integer);
                            writer.Write(er.Baker, NpgsqlTypes.NpgsqlDbType.Integer);
                            writer.Write((byte)BakingRightType.Endorsing, NpgsqlTypes.NpgsqlDbType.Smallint);
                            writer.Write((byte)BakingRightStatus.Future, NpgsqlTypes.NpgsqlDbType.Smallint);
                            writer.WriteNull();
                            writer.Write(er.Slots, NpgsqlTypes.NpgsqlDbType.Integer);
                        }

                        foreach (var br in brs)
                        {
                            writer.StartRow();
                            writer.Write(cycle.Index, NpgsqlTypes.NpgsqlDbType.Integer);
                            writer.Write(br.Level, NpgsqlTypes.NpgsqlDbType.Integer);
                            writer.Write(br.Baker, NpgsqlTypes.NpgsqlDbType.Integer);
                            writer.Write((byte)BakingRightType.Baking, NpgsqlTypes.NpgsqlDbType.Smallint);
                            writer.Write((byte)BakingRightStatus.Future, NpgsqlTypes.NpgsqlDbType.Smallint);
                            writer.Write(br.Round, NpgsqlTypes.NpgsqlDbType.Integer);
                            writer.WriteNull();
                        }

                        writer.Complete();
                    }
                    #endregion

                    #region reset baker cycles
                    foreach (var bakerCycle in bakerCycles.Values)
                    {
                        bakerCycle.FutureBlocks = 0;
                        bakerCycle.FutureBlockRewards = 0;
                        bakerCycle.FutureEndorsements = 0;
                    }

                    foreach (var br in brs.Where(x => x.Round == 0))
                    {
                        if (!bakerCycles.TryGetValue(br.Baker, out var bakerCycle))
                            throw new Exception("Nonexistent baker cycle");

                        bakerCycle.FutureBlocks++;
                        bakerCycle.FutureBlockRewards += nextProto.MaxBakingReward;
                    }

                    foreach (var er in shifted)
                    {
                        if (bakerCycles.TryGetValue(er.Baker, out var bakerCycle))
                        {
                            bakerCycle.FutureEndorsements += er.Slots;
                        }
                    }

                    foreach (var er in ers.TakeWhile(x => x.Level < cycle.LastLevel))
                    {
                        if (!bakerCycles.TryGetValue(er.Baker, out var bakerCycle))
                            throw new Exception("Nonexistent baker cycle");

                        bakerCycle.FutureEndorsements += er.Slots;
                    }
                    #endregion
                    
                    shifted = ers.Where(x => x.Level == cycle.LastLevel).ToList();
                }
            }
        }

        async Task PatchContracts(AppState state)
        {
            var block = await Cache.Blocks.CurrentAsync();
            var patched = File.ReadAllLines("./Protocols/Handlers/Proto15/Activation/patched.contracts");
            foreach (var address in patched)
            {
                if (await Cache.Accounts.GetAsync(address) is Contract contract)
                {
                    Db.TryAttach(contract);

                    var oldScript = await Db.Scripts.FirstAsync(x => x.ContractId == contract.Id && x.Current);
                    var oldStorage = await Cache.Storages.GetAsync(contract);

                    var rawContract = await Proto.Rpc.GetContractAsync(state.Level, contract.Address);

                    var code = Micheline.FromJson(rawContract.Required("script").Required("code")) as MichelineArray;
                    var micheParameter = code.First(x => x is MichelinePrim p && p.Prim == PrimType.parameter).ToBytes();
                    var micheStorage = code.First(x => x is MichelinePrim p && p.Prim == PrimType.storage).ToBytes();
                    var micheCode = code.First(x => x is MichelinePrim p && p.Prim == PrimType.code).ToBytes();
                    var micheViews = code.Where(x => x is MichelinePrim p && p.Prim == PrimType.view);

                    var newSchema = new ContractScript(code);
                    var newStorageValue = Micheline.FromJson(rawContract.Required("script").Required("storage"));
                    var newRawStorageValue = newSchema.OptimizeStorage(newStorageValue, false).ToBytes();

                    if (oldScript.ParameterSchema.IsEqual(micheParameter) &&
                        oldScript.StorageSchema.IsEqual(micheStorage) &&
                        oldScript.CodeSchema.IsEqual(micheCode) &&
                        oldStorage.RawValue.IsEqual(newRawStorageValue))
                        continue;

                    Db.TryAttach(oldScript);
                    oldScript.Current = false;

                    Db.TryAttach(oldStorage);
                    oldStorage.Current = false;

                    var migration = new MigrationOperation
                    {
                        Id = Cache.AppState.NextOperationId(),
                        Block = block,
                        Level = block.Level,
                        Timestamp = block.Timestamp,
                        Account = contract,
                        Kind = MigrationKind.CodeChange
                    };
                    var newScript = new Script
                    {
                        Id = Cache.AppState.NextScriptId(),
                        Level = migration.Level,
                        ContractId = contract.Id,
                        MigrationId = migration.Id,
                        ParameterSchema = micheParameter,
                        StorageSchema = micheStorage,
                        CodeSchema = micheCode,
                        Views = micheViews.Any()
                            ? micheViews.Select(x => x.ToBytes()).ToArray()
                            : null,
                        Current = true
                    };
                    var newStorage = new Storage
                    {
                        Id = Cache.AppState.NextStorageId(),
                        Level = migration.Level,
                        ContractId = contract.Id,
                        MigrationId = migration.Id,
                        RawValue = newRawStorageValue,
                        JsonValue = newScript.Schema.HumanizeStorage(newStorageValue),
                        Current = true
                    };

                    var viewsBytes = newScript.Views?
                        .OrderBy(x => x, new BytesComparer())
                        .SelectMany(x => x)
                        .ToArray()
                        ?? Array.Empty<byte>();
                    var typeSchema = newScript.ParameterSchema.Concat(newScript.StorageSchema).Concat(viewsBytes);
                    var fullSchema = typeSchema.Concat(newScript.CodeSchema);
                    contract.TypeHash = newScript.TypeHash = Script.GetHash(typeSchema);
                    contract.CodeHash = newScript.CodeHash = Script.GetHash(fullSchema);

                    migration.Script = newScript;
                    migration.Storage = newStorage;

                    contract.MigrationsCount++;
                    state.MigrationOpsCount++;

                    Db.MigrationOps.Add(migration);

                    Db.Scripts.Add(newScript);
                    Cache.Schemas.Add(contract, newScript.Schema);

                    Db.Storages.Add(newStorage);
                    Cache.Storages.Add(contract, newStorage);
                }
            }
        }

        Sampler GetOldSampler(IEnumerable<(int id, long stake)> selection)
        {
            var sorted = selection
                .OrderByDescending(x => x.stake)
                .ThenByDescending(x =>
                {
                    var baker = Cache.Accounts.GetDelegate(x.id);
                    return new byte[] { (byte)baker.PublicKey[0] }.Concat(Base58.Parse(baker.Address));
                }, new BytesComparer());

            return new Sampler(sorted.Select(x => x.id).ToArray(), sorted.Select(x => x.stake).ToArray());
        }
    }
}
