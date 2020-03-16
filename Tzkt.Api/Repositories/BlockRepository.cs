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

        public BlockRepository(AccountsCache accounts, OperationRepository operations, IConfiguration config) : base(config)
        {
            Accounts = accounts;
            Operations = operations;
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

        public async Task<IEnumerable<Block>> Get(int limit = 100, int offset = 0)
        {

            var sql = @"
                SELECT  ""Level"", ""Hash"", ""Timestamp"", ""ProtoCode"", ""Priority"", ""Validations"", ""Operations"", ""Reward"", ""Fees"", ""BakerId"", ""RevelationId""
                FROM    ""Blocks""
                ORDER BY ""Level""
                OFFSET   @offset
                LIMIT    @limit";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { limit, offset });

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

        public async Task<IEnumerable<int>> GetEventLevels(Data.Models.BlockEvents @event, int offset = 0, int limit = 100)
        {
            var sql = $@"
                SELECT  ""Level""
                FROM    ""Blocks""
                WHERE   ""Events"" & {(int)@event} > 0
                ORDER BY ""Id""
                OFFSET   @offset
                LIMIT    @limit";

            using var db = GetConnection();
            return await db.QueryAsync<int>(sql, new { limit, offset });
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
                ? Operations.GetEndorsements(block.Level)
                : Task.FromResult(Enumerable.Empty<EndorsementOperation>());

            var proposals = operations.HasFlag(Data.Models.Operations.Proposals)
                ? Operations.GetProposals(block.Level)
                : Task.FromResult(Enumerable.Empty<ProposalOperation>());

            var ballots = operations.HasFlag(Data.Models.Operations.Ballots)
                ? Operations.GetBallots(block.Level)
                : Task.FromResult(Enumerable.Empty<BallotOperation>());

            var activations = operations.HasFlag(Data.Models.Operations.Activations)
                ? Operations.GetActivations(block.Level)
                : Task.FromResult(Enumerable.Empty<ActivationOperation>());

            var doubleBaking = operations.HasFlag(Data.Models.Operations.DoubleBakings)
                ? Operations.GetDoubleBakings(block.Level)
                : Task.FromResult(Enumerable.Empty<DoubleBakingOperation>());

            var doubleEndorsing = operations.HasFlag(Data.Models.Operations.DoubleEndorsings)
                ? Operations.GetDoubleEndorsings(block.Level)
                : Task.FromResult(Enumerable.Empty<DoubleEndorsingOperation>());

            var nonceRevelations = operations.HasFlag(Data.Models.Operations.Revelations)
                ? Operations.GetNonceRevelations(block.Level)
                : Task.FromResult(Enumerable.Empty<NonceRevelationOperation>());

            var delegations = operations.HasFlag(Data.Models.Operations.Delegations)
                ? Operations.GetDelegations(block.Level)
                : Task.FromResult(Enumerable.Empty<DelegationOperation>());

            var originations = operations.HasFlag(Data.Models.Operations.Originations)
                ? Operations.GetOriginations(block.Level)
                : Task.FromResult(Enumerable.Empty<OriginationOperation>());

            var transactions = operations.HasFlag(Data.Models.Operations.Transactions)
                ? Operations.GetTransactions(block.Level)
                : Task.FromResult(Enumerable.Empty<TransactionOperation>());

            var reveals = operations.HasFlag(Data.Models.Operations.Reveals)
                ? Operations.GetReveals(block.Level)
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
