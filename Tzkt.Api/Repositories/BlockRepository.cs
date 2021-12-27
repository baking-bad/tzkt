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
        readonly QuotesCache Quotes;
        readonly StateCache State;
        readonly SoftwareCache Software;

        public BlockRepository(AccountsCache accounts, OperationRepository operations, QuotesCache quotes, StateCache state, SoftwareCache software, IConfiguration config) : base(config)
        {
            Accounts = accounts;
            Operations = operations;
            Quotes = quotes;
            State = state;
            Software = software;
        }

        public Task<int> GetCount()
        {
            return Task.FromResult(State.Current.Level + 1);
        }

        public async Task<Block> Get(int level, bool operations, MichelineFormat format, Symbols quote)
        {
            var sql = @"
                SELECT  *
                FROM    ""Blocks""
                WHERE   ""Level"" = @level
                LIMIT   1";

            using var db = GetConnection();
            var row = await db.QueryFirstOrDefaultAsync(sql, new { level });
            if (row == null) return null;

            var block = new Block
            {
                Cycle = row.Cycle,
                Level = level,
                Hash = row.Hash,
                Timestamp = row.Timestamp,
                Proto = row.ProtoCode,
                Priority = row.Priority,
                Validations = row.Validations,
                Deposit = row.Deposit,
                Reward = row.Reward,
                Fees = row.Fees,
                NonceRevealed = row.RevelationId != null,
                Baker = row.BakerId != null ? await Accounts.GetAliasAsync(row.BakerId) : null,
                Software = row.SoftwareId != null ? Software[row.SoftwareId] : null,
                LBEscapeVote = row.LBEscapeVote,
                LBEscapeEma = row.LBEscapeEma,
                Quote = Quotes.Get(quote, level)
            };

            if (operations)
                await LoadOperations(block, (Data.Models.Operations)row.Operations, format, quote);

            return block;
        }

        public async Task<Block> Get(string hash, bool operations, MichelineFormat format, Symbols quote)
        {
            var sql = @"
                SELECT  *
                FROM    ""Blocks""
                WHERE   ""Hash"" = @hash::character(51)
                LIMIT   1";

            using var db = GetConnection();
            var row = await db.QueryFirstOrDefaultAsync(sql, new { hash });
            if (row == null) return null;

            var block = new Block
            {
                Cycle = row.Cycle,
                Level = row.Level,
                Hash = hash,
                Timestamp = row.Timestamp,
                Proto = row.ProtoCode,
                Priority = row.Priority,
                Validations = row.Validations,
                Deposit = row.Deposit,
                Reward = row.Reward,
                Fees = row.Fees,
                NonceRevealed = row.RevelationId != null,
                Baker = row.BakerId != null ? await Accounts.GetAliasAsync(row.BakerId) : null,
                Software = row.SoftwareId != null ? Software[row.SoftwareId] : null,
                LBEscapeVote = row.LBEscapeVote,
                LBEscapeEma = row.LBEscapeEma,
                Quote = Quotes.Get(quote, row.Level)
            };

            if (operations)
                await LoadOperations(block, (Data.Models.Operations)row.Operations, format, quote);

            return block;
        }

        public async Task<IEnumerable<Block>> Get(
            AccountParameter baker,
            Int32Parameter level,
            DateTimeParameter timestamp,
            Int32Parameter priority,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            Symbols quote)
        {
            var sql = new SqlBuilder(@"SELECT * FROM ""Blocks""")
                .Filter("BakerId", baker)
                .Filter("Level", level)
                .Filter("Timestamp", timestamp)
                .Filter("Priority", priority)
                .Take(sort, offset, limit, x => x switch
                {
                    "level" => ("Id", "Level"),
                    "priority" => ("Priority", "Priority"),
                    "validations" => ("Validations", "Validations"),
                    "reward" => ("Reward", "Reward"),
                    "fees" => ("Fees", "Fees"),
                    _ => ("Id", "Id")
                });

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            return rows.Select(row => new Block
            {
                Cycle = row.Cycle,
                Level = row.Level,
                Hash = row.Hash,
                Timestamp = row.Timestamp,
                Proto = row.ProtoCode,
                Priority = row.Priority,
                Validations = row.Validations,
                Deposit = row.Deposit,
                Reward = row.Reward,
                Fees = row.Fees,
                NonceRevealed = row.RevelationId != null,
                Baker = row.BakerId != null ? Accounts.GetAlias(row.BakerId) : null,
                Software = row.SoftwareId != null ? Software[row.SoftwareId] : null,
                LBEscapeVote = row.LBEscapeVote,
                LBEscapeEma = row.LBEscapeEma,
                Quote = Quotes.Get(quote, row.Level)
            });
        }

        public async Task<object[][]> Get(
            AccountParameter baker,
            Int32Parameter level,
            DateTimeParameter timestamp,
            Int32Parameter priority,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            string[] fields,
            Symbols quote)
        {
            var columns = new HashSet<string>(fields.Length);
            foreach (var field in fields)
            {
                switch (field)
                {
                    case "cycle": columns.Add(@"""Cycle"""); break;
                    case "level": columns.Add(@"""Level"""); break;
                    case "hash": columns.Add(@"""Hash"""); break;
                    case "timestamp": columns.Add(@"""Timestamp"""); break;
                    case "proto": columns.Add(@"""ProtoCode"""); break;
                    case "priority": columns.Add(@"""Priority"""); break;
                    case "validations": columns.Add(@"""Validations"""); break;
                    case "deposit": columns.Add(@"""Deposit"""); break;
                    case "reward": columns.Add(@"""Reward"""); break;
                    case "fees": columns.Add(@"""Fees"""); break;
                    case "nonceRevealed": columns.Add(@"""RevelationId"""); break;
                    case "baker": columns.Add(@"""BakerId"""); break;
                    case "software": columns.Add(@"""SoftwareId"""); break;
                    case "quote": columns.Add(@"""Level"""); break;
                    case "lbEscapeVote": columns.Add(@"""LBEscapeVote"""); break; 
                    case "lbEscapeEma": columns.Add(@"""LBEscapeEma"""); break; 
                }
            }

            if (columns.Count == 0)
                return Array.Empty<object[]>();

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""Blocks""")
                .Filter("BakerId", baker)
                .Filter("Level", level)
                .Filter("Timestamp", timestamp)
                .Filter("Priority", priority)
                .Take(sort, offset, limit, x => x switch
                {
                    "level" => ("Id", "Level"),
                    "priority" => ("Priority", "Priority"),
                    "validations" => ("Validations", "Validations"),
                    "reward" => ("Reward", "Reward"),
                    "fees" => ("Fees", "Fees"),
                    _ => ("Id", "Id")
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
                    case "cycle":
                        foreach (var row in rows)
                            result[j++][i] = row.Cycle;
                        break;
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
                    case "deposit":
                        foreach (var row in rows)
                            result[j++][i] = row.Deposit;
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
                    case "software":
                        foreach (var row in rows)
                            result[j++][i] = row.SoftwareId != null ? Software[row.SoftwareId] : null;
                        break;
                    case "lbEscapeVote":
                        foreach (var row in rows)
                            result[j++][i] = row.LBEscapeVote;
                        break;
                    case "lbEscapeEma":
                        foreach (var row in rows)
                            result[j++][i] = row.LBEscapeEma;
                        break;
                    case "quote":
                        foreach (var row in rows)
                            result[j++][i] = Quotes.Get(quote, row.Level);
                        break;
                }
            }

            return result;
        }

        public async Task<object[]> Get(
            AccountParameter baker,
            Int32Parameter level,
            DateTimeParameter timestamp,
            Int32Parameter priority,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            string field,
            Symbols quote)
        {
            var columns = new HashSet<string>(1);
            switch (field)
            {
                case "cycle": columns.Add(@"""Cycle"""); break;
                case "level": columns.Add(@"""Level"""); break;
                case "hash": columns.Add(@"""Hash"""); break;
                case "timestamp": columns.Add(@"""Timestamp"""); break;
                case "proto": columns.Add(@"""ProtoCode"""); break;
                case "priority": columns.Add(@"""Priority"""); break;
                case "validations": columns.Add(@"""Validations"""); break;
                case "deposit": columns.Add(@"""Deposit"""); break;
                case "reward": columns.Add(@"""Reward"""); break;
                case "fees": columns.Add(@"""Fees"""); break;
                case "nonceRevealed": columns.Add(@"""RevelationId"""); break;
                case "baker": columns.Add(@"""BakerId"""); break;
                case "software": columns.Add(@"""SoftwareId"""); break;
                case "quote": columns.Add(@"""Level"""); break;
                case "lbEscapeVote": columns.Add(@"""LBEscapeVote"""); break;
                case "lbEscapeEma": columns.Add(@"""LBEscapeEma"""); break;
            }

            if (columns.Count == 0)
                return Array.Empty<object>();

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""Blocks""")
                .Filter("BakerId", baker)
                .Filter("Level", level)
                .Filter("Timestamp", timestamp)
                .Filter("Priority", priority)
                .Take(sort, offset, limit, x => x switch
                {
                    "level" => ("Id", "Level"),
                    "priority" => ("Priority", "Priority"),
                    "validations" => ("Validations", "Validations"),
                    "reward" => ("Reward", "Reward"),
                    "fees" => ("Fees", "Fees"),
                    _ => ("Id", "Id")
                });

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            //TODO: optimize memory allocation
            var result = new object[rows.Count()];
            var j = 0;

            switch (field)
            {
                case "cycle":
                    foreach (var row in rows)
                        result[j++] = row.Cycle;
                    break;
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
                case "deposit":
                    foreach (var row in rows)
                        result[j++] = row.Deposit;
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
                case "software":
                    foreach (var row in rows)
                        result[j++] = row.SoftwareId != null ? Software[row.SoftwareId] : null;
                    break;
                case "lbEscapeVote":
                    foreach (var row in rows)
                        result[j++] = row.LBEscapeVote;
                    break;
                case "lbEscapeEma":
                    foreach (var row in rows)
                        result[j++] = row.LBEscapeEma;
                    break;
                case "quote":
                    foreach (var row in rows)
                        result[j++] = Quotes.Get(quote, row.Level);
                    break;
            }

            return result;
        }

        public async Task<IEnumerable<int>> GetEventLevels(Data.Models.BlockEvents @event, OffsetParameter offset, int limit = 100)
        {
            var sql = new SqlBuilder(@"SELECT ""Level"" FROM ""Blocks""")
                .Filter($@"""Events"" & {(int)@event} > 0")
                .Take(new SortParameter { Asc = "level" }, offset, limit, x => ("Level", "Level"));

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

        async Task LoadOperations(Block block, Data.Models.Operations operations, MichelineFormat format, Symbols quote)
        {
            var endorsements = operations.HasFlag(Data.Models.Operations.Endorsements)
                ? Operations.GetEndorsements(block, quote)
                : Task.FromResult(Enumerable.Empty<EndorsementOperation>());

            var proposals = operations.HasFlag(Data.Models.Operations.Proposals)
                ? Operations.GetProposals(block, quote)
                : Task.FromResult(Enumerable.Empty<ProposalOperation>());

            var ballots = operations.HasFlag(Data.Models.Operations.Ballots)
                ? Operations.GetBallots(block, quote)
                : Task.FromResult(Enumerable.Empty<BallotOperation>());

            var activations = operations.HasFlag(Data.Models.Operations.Activations)
                ? Operations.GetActivations(block, quote)
                : Task.FromResult(Enumerable.Empty<ActivationOperation>());

            var doubleBaking = operations.HasFlag(Data.Models.Operations.DoubleBakings)
                ? Operations.GetDoubleBakings(block, quote)
                : Task.FromResult(Enumerable.Empty<DoubleBakingOperation>());

            var doubleEndorsing = operations.HasFlag(Data.Models.Operations.DoubleEndorsings)
                ? Operations.GetDoubleEndorsings(block, quote)
                : Task.FromResult(Enumerable.Empty<DoubleEndorsingOperation>());

            var nonceRevelations = operations.HasFlag(Data.Models.Operations.Revelations)
                ? Operations.GetNonceRevelations(block, quote)
                : Task.FromResult(Enumerable.Empty<NonceRevelationOperation>());

            var delegations = operations.HasFlag(Data.Models.Operations.Delegations)
                ? Operations.GetDelegations(block, quote)
                : Task.FromResult(Enumerable.Empty<DelegationOperation>());

            var originations = operations.HasFlag(Data.Models.Operations.Originations)
                ? Operations.GetOriginations(block, quote)
                : Task.FromResult(Enumerable.Empty<OriginationOperation>());

            var transactions = operations.HasFlag(Data.Models.Operations.Transactions)
                ? Operations.GetTransactions(block, format, quote)
                : Task.FromResult(Enumerable.Empty<TransactionOperation>());

            var reveals = operations.HasFlag(Data.Models.Operations.Reveals)
                ? Operations.GetReveals(block, quote)
                : Task.FromResult(Enumerable.Empty<RevealOperation>());

            var registerConstants = operations.HasFlag(Data.Models.Operations.RegisterConstant)
                ? Operations.GetRegisterConstants(block, format, quote)
                : Task.FromResult(Enumerable.Empty<RegisterConstantOperation>());

            var migrations = operations.HasFlag(Data.Models.Operations.Migrations)
                ? Operations.GetMigrations(null, null, null, null, new Int32Parameter { Eq = block.Level }, null, null, null, 10_000, format, quote)
                : Task.FromResult(Enumerable.Empty<MigrationOperation>());

            var penalties = operations.HasFlag(Data.Models.Operations.RevelationPenalty)
                ? Operations.GetRevelationPenalties(null, new Int32Parameter { Eq = block.Level }, null, null, null, 10_000, quote)
                : Task.FromResult(Enumerable.Empty<RevelationPenaltyOperation>());

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
                reveals,
                registerConstants,
                migrations,
                penalties);

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
            block.RegisterConstants = registerConstants.Result;
            block.Migrations = migrations.Result;
            block.RevelationPenalties = penalties.Result;
        }
    }
}
