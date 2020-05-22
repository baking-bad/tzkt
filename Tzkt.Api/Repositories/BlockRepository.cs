using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Dapper;

using Tzkt.Api.Models;
using Tzkt.Api.Services.Cache;

namespace Tzkt.Api.Repositories
{
    public class BlockRepository : DbConnection
    {
        readonly AccountsCache Accounts;
        readonly OperationRepository Operations;
        readonly StateCache State;

        public BlockRepository(AccountsCache accounts, OperationRepository operations, StateCache state, IConfiguration config) : base(config)
        {
            Accounts = accounts;
            Operations = operations;
            State = state;
        }

        public Task<int> GetCount()
        {
            return Task.FromResult(State.GetState().Level + 1);
        }

        public async Task<Block> Get(int level, bool operations = false)
        {
            var sql = @"
                SELECT  ""Hash"", ""Timestamp"", ""ProtoCode"", ""Priority"", ""Validations"", ""Operations"", ""Reward"", ""Fees"", ""BakerId"", ""RevelationId""
                FROM    ""Blocks""
                WHERE   ""Level"" = @level
                LIMIT   1";

            using var db = GetConnection();
            var row = await db.QueryFirstOrDefaultAsync(sql, new { level });
            if (row == null) return null;

            var block = new Block
            {
                Level = level,
                Hash = row.Hash,
                Timestamp = row.Timestamp,
                Proto = row.ProtoCode,
                Priority = row.Priority,
                Validations = row.Validations,
                Reward = row.Reward,
                Fees = row.Fees,
                NonceRevealed = row.RevelationId != null,
                Baker = row.BakerId != null ? await Accounts.GetAliasAsync(row.BakerId) : null
            };

            if (operations)
                await LoadOperations(block, (Data.Models.Operations)row.Operations);

            return block;
        }

        public async Task<Block> Get(string hash, bool operations = false)
        {
            var sql = @"
                SELECT  ""Level"", ""Timestamp"", ""ProtoCode"", ""Priority"", ""Validations"", ""Operations"", ""Reward"", ""Fees"", ""BakerId"", ""RevelationId""
                FROM    ""Blocks""
                WHERE   ""Hash"" = @hash::character(51)
                LIMIT   1";

            using var db = GetConnection();
            var row = await db.QueryFirstOrDefaultAsync(sql, new { hash });
            if (row == null) return null;

            var block = new Block
            {
                Level = row.Level,
                Hash = hash,
                Timestamp = row.Timestamp,
                Proto = row.ProtoCode,
                Priority = row.Priority,
                Validations = row.Validations,
                Reward = row.Reward,
                Fees = row.Fees,
                NonceRevealed = row.RevelationId != null,
                Baker = row.BakerId != null ? await Accounts.GetAliasAsync(row.BakerId) : null
            };

            if (operations)
                await LoadOperations(block, (Data.Models.Operations)row.Operations);

            return block;
        }

        public async Task<IEnumerable<Block>> Get(SortParameter sort, OffsetParameter offset, int limit)
        {
            var sql = new SqlBuilder(@"SELECT ""Level"", ""Hash"", ""Timestamp"", ""ProtoCode"", ""Priority"", ""Validations"", ""Operations"", ""Reward"", ""Fees"", ""BakerId"", ""RevelationId"" FROM ""Blocks""")
                .Take(sort, offset, limit, x => x switch
                {
                    "level" => "Id",
                    "timestamp" => "Id",
                    "priority" => "Priority",
                    "validations" => "Validations",
                    "reward" => "Reward",
                    "fees" => "Fees",
                    _ => "Id",
                });

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            return rows.Select(row => new Block
            {
                Level = row.Level,
                Hash = row.Hash,
                Timestamp = row.Timestamp,
                Proto = row.ProtoCode,
                Priority = row.Priority,
                Validations = row.Validations,
                Reward = row.Reward,
                Fees = row.Fees,
                NonceRevealed = row.RevelationId != null,
                Baker = row.BakerId != null ? Accounts.GetAlias(row.BakerId) : null
            });
        }

        public async Task<object[][]> Get(SortParameter sort, OffsetParameter offset, int limit, string[] fields)
        {
            var columns = new HashSet<string>(fields.Length);
            foreach (var field in fields)
            {
                switch (field)
                {
                    case "level": columns.Add(@"""Level"""); break;
                    case "hash": columns.Add(@"""Hash"""); break;
                    case "timestamp": columns.Add(@"""Timestamp"""); break;
                    case "proto": columns.Add(@"""ProtoCode"""); break;
                    case "priority": columns.Add(@"""Priority"""); break;
                    case "validations": columns.Add(@"""Validations"""); break;
                    case "reward": columns.Add(@"""Reward"""); break;
                    case "fees": columns.Add(@"""Fees"""); break;
                    case "nonceRevealed": columns.Add(@"""RevelationId"""); break;
                    case "baker": columns.Add(@"""BakerId"""); break;
                }
            }

            if (columns.Count == 0)
                return Array.Empty<object[]>();

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""Blocks""")
                .Take(sort, offset, limit, x => x switch
                {
                    "level" => "Id",
                    "timestamp" => "Id",
                    "priority" => "Priority",
                    "validations" => "Validations",
                    "reward" => "Reward",
                    "fees" => "Fees",
                    _ => "Id",
                });

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            var result = new object[rows.Count()][];
            for (int i = 0; i < result.Length; i++)
                result[i] = new object[fields.Length];

            for (int i = 0, j = 0; i < fields.Length; j = 0, i++)
            {
                switch (fields[i])
                {
                    case "level":
                        foreach (var row in rows)
                            result[j++][i] = row.Level;
                        break;
                    case "hash":
                        foreach (var row in rows)
                            result[j++][i] = row.Hash;
                        break;
                    case "timestamp":
                        foreach (var row in rows)
                            result[j++][i] = row.Timestamp;
                        break;
                    case "proto":
                        foreach (var row in rows)
                            result[j++][i] = row.ProtoCode;
                        break;
                    case "priority":
                        foreach (var row in rows)
                            result[j++][i] = row.Priority;
                        break;
                    case "validations":
                        foreach (var row in rows)
                            result[j++][i] = row.Validations;
                        break;
                    case "reward":
                        foreach (var row in rows)
                            result[j++][i] = row.Reward;
                        break;
                    case "fees":
                        foreach (var row in rows)
                            result[j++][i] = row.Fees;
                        break;
                    case "nonceRevealed":
                        foreach (var row in rows)
                            result[j++][i] = row.RevelationId != null;
                        break;
                    case "baker":
                        foreach (var row in rows)
                            result[j++][i] = row.BakerId != null ? await Accounts.GetAliasAsync(row.BakerId) : null;
                        break;
                }
            }

            return result;
        }

        public async Task<object[]> Get(SortParameter sort, OffsetParameter offset, int limit, string field)
        {
            var columns = new HashSet<string>(1);
            switch (field)
            {
                case "level": columns.Add(@"""Level"""); break;
                case "hash": columns.Add(@"""Hash"""); break;
                case "timestamp": columns.Add(@"""Timestamp"""); break;
                case "proto": columns.Add(@"""ProtoCode"""); break;
                case "priority": columns.Add(@"""Priority"""); break;
                case "validations": columns.Add(@"""Validations"""); break;
                case "reward": columns.Add(@"""Reward"""); break;
                case "fees": columns.Add(@"""Fees"""); break;
                case "nonceRevealed": columns.Add(@"""RevelationId"""); break;
                case "baker": columns.Add(@"""BakerId"""); break;
            }

            if (columns.Count == 0)
                return Array.Empty<object>();

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""Blocks""")
                .Take(sort, offset, limit, x => x switch
                {
                    "level" => "Id",
                    "timestamp" => "Id",
                    "priority" => "Priority",
                    "validations" => "Validations",
                    "reward" => "Reward",
                    "fees" => "Fees",
                    _ => "Id",
                });

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            //TODO: optimize memory allocation
            var result = new object[rows.Count()];
            var j = 0;

            switch (field)
            {
                case "level":
                    foreach (var row in rows)
                        result[j++] = row.Level;
                    break;
                case "hash":
                    foreach (var row in rows)
                        result[j++] = row.Hash;
                    break;
                case "timestamp":
                    foreach (var row in rows)
                        result[j++] = row.Timestamp;
                    break;
                case "proto":
                    foreach (var row in rows)
                        result[j++] = row.ProtoCode;
                    break;
                case "priority":
                    foreach (var row in rows)
                        result[j++] = row.Priority;
                    break;
                case "validations":
                    foreach (var row in rows)
                        result[j++] = row.Validations;
                    break;
                case "reward":
                    foreach (var row in rows)
                        result[j++] = row.Reward;
                    break;
                case "fees":
                    foreach (var row in rows)
                        result[j++] = row.Fees;
                    break;
                case "nonceRevealed":
                    foreach (var row in rows)
                        result[j++] = row.RevelationId != null;
                    break;
                case "baker":
                    foreach (var row in rows)
                        result[j++] = row.BakerId != null ? await Accounts.GetAliasAsync(row.BakerId) : null;
                    break;
            }

            return result;
        }

        public async Task<IEnumerable<int>> GetEventLevels(Data.Models.BlockEvents @event, OffsetParameter offset, int limit = 100)
        {
            var sql = new SqlBuilder(@"SELECT ""Level"" FROM ""Blocks""")
                .Filter($@"""Events"" & {(int)@event} > 0")
                .Take(new SortParameter { Asc = "level" }, offset, limit, x => "Level");

            using var db = GetConnection();
            return await db.QueryAsync<int>(sql.Query, sql.Params);
        }

        public async Task<IEnumerable<DateTime>> GetTimestamps(int offset = 0, int limit = 100)
        {
            var sql = $@"
                SELECT  ""Timestamp""
                FROM    ""Blocks""
                ORDER BY ""Id""
                OFFSET   @offset
                LIMIT    @limit";

            using var db = GetConnection();
            return await db.QueryAsync<DateTime>(sql, new { limit, offset });
        }

        async Task LoadOperations(Block block, Data.Models.Operations operations)
        {
            var endorsements = operations.HasFlag(Data.Models.Operations.Endorsements)
                ? Operations.GetEndorsements(block)
                : Task.FromResult(Enumerable.Empty<EndorsementOperation>());

            var proposals = operations.HasFlag(Data.Models.Operations.Proposals)
                ? Operations.GetProposals(block)
                : Task.FromResult(Enumerable.Empty<ProposalOperation>());

            var ballots = operations.HasFlag(Data.Models.Operations.Ballots)
                ? Operations.GetBallots(block)
                : Task.FromResult(Enumerable.Empty<BallotOperation>());

            var activations = operations.HasFlag(Data.Models.Operations.Activations)
                ? Operations.GetActivations(block)
                : Task.FromResult(Enumerable.Empty<ActivationOperation>());

            var doubleBaking = operations.HasFlag(Data.Models.Operations.DoubleBakings)
                ? Operations.GetDoubleBakings(block)
                : Task.FromResult(Enumerable.Empty<DoubleBakingOperation>());

            var doubleEndorsing = operations.HasFlag(Data.Models.Operations.DoubleEndorsings)
                ? Operations.GetDoubleEndorsings(block)
                : Task.FromResult(Enumerable.Empty<DoubleEndorsingOperation>());

            var nonceRevelations = operations.HasFlag(Data.Models.Operations.Revelations)
                ? Operations.GetNonceRevelations(block)
                : Task.FromResult(Enumerable.Empty<NonceRevelationOperation>());

            var delegations = operations.HasFlag(Data.Models.Operations.Delegations)
                ? Operations.GetDelegations(block)
                : Task.FromResult(Enumerable.Empty<DelegationOperation>());

            var originations = operations.HasFlag(Data.Models.Operations.Originations)
                ? Operations.GetOriginations(block)
                : Task.FromResult(Enumerable.Empty<OriginationOperation>());

            var transactions = operations.HasFlag(Data.Models.Operations.Transactions)
                ? Operations.GetTransactions(block)
                : Task.FromResult(Enumerable.Empty<TransactionOperation>());

            var reveals = operations.HasFlag(Data.Models.Operations.Reveals)
                ? Operations.GetReveals(block)
                : Task.FromResult(Enumerable.Empty<RevealOperation>());

            await Task.WhenAll(
                endorsements,
                proposals,
                ballots,
                activations,
                doubleBaking,
                doubleEndorsing,
                nonceRevelations,
                delegations,
                originations,
                transactions,
                reveals);

            block.Endorsements = endorsements.Result;
            block.Proposals = proposals.Result;
            block.Ballots = ballots.Result;
            block.Activations = activations.Result;
            block.DoubleBaking = doubleBaking.Result;
            block.DoubleEndorsing = doubleEndorsing.Result;
            block.NonceRevelations = nonceRevelations.Result;
            block.Delegations = delegations.Result;
            block.Originations = originations.Result;
            block.Transactions = transactions.Result;
            block.Reveals = reveals.Result;
        }
    }
}
