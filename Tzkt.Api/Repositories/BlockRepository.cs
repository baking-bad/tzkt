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
                PayloadRound = row.PayloadRound,
                BlockRound = row.BlockRound,
                Validations = row.Validations,
                Deposit = row.Deposit,
                Reward = row.Reward,
                Bonus = row.Bonus,
                Fees = row.Fees,
                NonceRevealed = row.RevelationId != null,
                Proposer = row.ProposerId != null ? await Accounts.GetAliasAsync(row.ProposerId) : null,
                Producer = row.ProducerId != null ? await Accounts.GetAliasAsync(row.ProducerId) : null,
                Software = row.SoftwareId != null ? Software[row.SoftwareId] : null,
                LBToggle = row.LBToggle,
                LBToggleEma = row.LBToggleEma,
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
                PayloadRound = row.PayloadRound,
                BlockRound = row.BlockRound,
                Validations = row.Validations,
                Deposit = row.Deposit,
                Reward = row.Reward,
                Bonus = row.Bonus,
                Fees = row.Fees,
                NonceRevealed = row.RevelationId != null,
                Proposer = row.ProposerId != null ? await Accounts.GetAliasAsync(row.ProposerId) : null,
                Producer = row.ProducerId != null ? await Accounts.GetAliasAsync(row.ProducerId) : null,
                Software = row.SoftwareId != null ? Software[row.SoftwareId] : null,
                LBToggle = row.LBToggle,
                LBToggleEma = row.LBToggleEma,
                Quote = Quotes.Get(quote, row.Level)
            };

            if (operations)
                await LoadOperations(block, (Data.Models.Operations)row.Operations, format, quote);

            return block;
        }

        public async Task<IEnumerable<Block>> Get(
            AnyOfParameter anyof,
            AccountParameter proposer,
            AccountParameter producer,
            Int32Parameter level,
            DateTimeParameter timestamp,
            Int32Parameter blockRound,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            Symbols quote)
        {
            var sql = new SqlBuilder(@"SELECT * FROM ""Blocks""")
                .Filter(anyof, x => x == "proposer" ? "ProposerId" : "ProducerId")
                .Filter("ProposerId", proposer)
                .Filter("ProducerId", producer)
                .Filter("Level", level)
                .Filter("Timestamp", timestamp)
                .Filter("BlockRound", blockRound)
                .Take(sort, offset, limit, x => x switch
                {
                    "level" => ("Id", "Level"),
                    "payloadRound" => ("PayloadRound", "PayloadRound"),
                    "blockRound" => ("BlockRound", "BlockRound"),
                    "validations" => ("Validations", "Validations"),
                    "reward" => ("Reward", "Reward"),
                    "bonus" => ("Bonus", "Bonus"),
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
                PayloadRound = row.PayloadRound,
                BlockRound = row.BlockRound,
                Validations = row.Validations,
                Deposit = row.Deposit,
                Reward = row.Reward,
                Bonus = row.Bonus,
                Fees = row.Fees,
                NonceRevealed = row.RevelationId != null,
                Proposer = row.ProposerId != null ? Accounts.GetAlias(row.ProposerId) : null,
                Producer = row.ProducerId != null ? Accounts.GetAlias(row.ProducerId) : null,
                Software = row.SoftwareId != null ? Software[row.SoftwareId] : null,
                LBToggle = row.LBToggle,
                LBToggleEma = row.LBToggleEma,
                Quote = Quotes.Get(quote, row.Level)
            });
        }

        public async Task<object[][]> Get(
            AnyOfParameter anyof,
            AccountParameter proposer,
            AccountParameter producer,
            Int32Parameter level,
            DateTimeParameter timestamp,
            Int32Parameter blockRound,
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
                    case "payloadRound": columns.Add(@"""PayloadRound"""); break;
                    case "blockRound": columns.Add(@"""BlockRound"""); break;
                    case "validations": columns.Add(@"""Validations"""); break;
                    case "deposit": columns.Add(@"""Deposit"""); break;
                    case "reward": columns.Add(@"""Reward"""); break;
                    case "bonus": columns.Add(@"""Bonus"""); break;
                    case "fees": columns.Add(@"""Fees"""); break;
                    case "nonceRevealed": columns.Add(@"""RevelationId"""); break;
                    case "proposer": columns.Add(@"""ProposerId"""); break;
                    case "producer": columns.Add(@"""ProducerId"""); break;
                    case "software": columns.Add(@"""SoftwareId"""); break;
                    case "quote": columns.Add(@"""Level"""); break;
                    case "lbToggle": columns.Add(@"""LBToggle"""); break; 
                    case "lbToggleEma": columns.Add(@"""LBToggleEma"""); break; 
                }
            }

            if (columns.Count == 0)
                return Array.Empty<object[]>();

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""Blocks""")
                .Filter(anyof, x => x == "proposer" ? "ProposerId" : "ProducerId")
                .Filter("ProposerId", proposer)
                .Filter("ProducerId", producer)
                .Filter("Level", level)
                .Filter("Timestamp", timestamp)
                .Filter("BlockRound", blockRound)
                .Take(sort, offset, limit, x => x switch
                {
                    "level" => ("Id", "Level"),
                    "payloadRound" => ("PayloadRound", "PayloadRound"),
                    "blockRound" => ("BlockRound", "BlockRound"),
                    "validations" => ("Validations", "Validations"),
                    "reward" => ("Reward", "Reward"),
                    "bonus" => ("Bonus", "Bonus"),
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
                    case "payloadRound":
                        foreach (var row in rows)
                            result[j++][i] = row.PayloadRound;
                        break;
                    case "blockRound":
                        foreach (var row in rows)
                            result[j++][i] = row.BlockRound;
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
                    case "bonus":
                        foreach (var row in rows)
                            result[j++][i] = row.Bonus;
                        break;
                    case "fees":
                        foreach (var row in rows)
                            result[j++][i] = row.Fees;
                        break;
                    case "nonceRevealed":
                        foreach (var row in rows)
                            result[j++][i] = row.RevelationId != null;
                        break;
                    case "proposer":
                        foreach (var row in rows)
                            result[j++][i] = row.ProposerId != null ? await Accounts.GetAliasAsync(row.ProposerId) : null;
                        break;
                    case "producer":
                        foreach (var row in rows)
                            result[j++][i] = row.ProducerId != null ? await Accounts.GetAliasAsync(row.ProducerId) : null;
                        break;
                    case "software":
                        foreach (var row in rows)
                            result[j++][i] = row.SoftwareId != null ? Software[row.SoftwareId] : null;
                        break;
                    case "lbToggle":
                        foreach (var row in rows)
                            result[j++][i] = row.LBToggle;
                        break;
                    case "lbToggleEma":
                        foreach (var row in rows)
                            result[j++][i] = row.LBToggleEma;
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
            AnyOfParameter anyof,
            AccountParameter proposer,
            AccountParameter producer,
            Int32Parameter level,
            DateTimeParameter timestamp,
            Int32Parameter blockRound,
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
                case "payloadRound": columns.Add(@"""PayloadRound"""); break;
                case "blockRound": columns.Add(@"""BlockRound"""); break;
                case "validations": columns.Add(@"""Validations"""); break;
                case "deposit": columns.Add(@"""Deposit"""); break;
                case "reward": columns.Add(@"""Reward"""); break;
                case "bonus": columns.Add(@"""Bonus"""); break;
                case "fees": columns.Add(@"""Fees"""); break;
                case "nonceRevealed": columns.Add(@"""RevelationId"""); break;
                case "proposer": columns.Add(@"""ProposerId"""); break;
                case "producer": columns.Add(@"""ProducerId"""); break;
                case "software": columns.Add(@"""SoftwareId"""); break;
                case "quote": columns.Add(@"""Level"""); break;
                case "lbToggle": columns.Add(@"""LBToggle"""); break;
                case "lbToggleEma": columns.Add(@"""LBToggleEma"""); break;
            }

            if (columns.Count == 0)
                return Array.Empty<object>();

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""Blocks""")
                .Filter(anyof, x => x == "proposer" ? "ProposerId" : "ProducerId")
                .Filter("ProposerId", proposer)
                .Filter("ProducerId", producer)
                .Filter("Level", level)
                .Filter("Timestamp", timestamp)
                .Filter("BlockRound", blockRound)
                .Take(sort, offset, limit, x => x switch
                {
                    "level" => ("Id", "Level"),
                    "payloadRound" => ("PayloadRound", "PayloadRound"),
                    "blockRound" => ("BlockRound", "BlockRound"),
                    "validations" => ("Validations", "Validations"),
                    "reward" => ("Reward", "Reward"),
                    "bonus" => ("Bonus", "Bonus"),
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
                case "payloadRound":
                    foreach (var row in rows)
                        result[j++] = row.PayloadRound;
                    break;
                case "blockRound":
                    foreach (var row in rows)
                        result[j++] = row.BlockRound;
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
                case "proposer":
                    foreach (var row in rows)
                        result[j++] = row.ProposerId != null ? await Accounts.GetAliasAsync(row.ProposerId) : null;
                    break;
                case "producer":
                    foreach (var row in rows)
                        result[j++] = row.ProducerId != null ? await Accounts.GetAliasAsync(row.ProducerId) : null;
                    break;
                case "software":
                    foreach (var row in rows)
                        result[j++] = row.SoftwareId != null ? Software[row.SoftwareId] : null;
                    break;
                case "lbToggle":
                    foreach (var row in rows)
                        result[j++] = row.LBToggle;
                    break;
                case "lbToggleEma":
                    foreach (var row in rows)
                        result[j++] = row.LBToggleEma;
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

            var preendorsements = operations.HasFlag(Data.Models.Operations.Preendorsements)
                ? Operations.GetPreendorsements(block, quote)
                : Task.FromResult(Enumerable.Empty<PreendorsementOperation>());

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

            var doublePreendorsing = operations.HasFlag(Data.Models.Operations.DoublePreendorsings)
                ? Operations.GetDoublePreendorsings(block, quote)
                : Task.FromResult(Enumerable.Empty<DoublePreendorsingOperation>());

            var nonceRevelations = operations.HasFlag(Data.Models.Operations.Revelations)
                ? Operations.GetNonceRevelations(block, quote)
                : Task.FromResult(Enumerable.Empty<NonceRevelationOperation>());

            var vdfRevelations = operations.HasFlag(Data.Models.Operations.VdfRevelation)
                ? Operations.GetVdfRevelations(block, quote)
                : Task.FromResult(Enumerable.Empty<VdfRevelationOperation>());

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

            var setDepositsLimits = operations.HasFlag(Data.Models.Operations.SetDepositsLimits)
                ? Operations.GetSetDepositsLimits(block, quote)
                : Task.FromResult(Enumerable.Empty<SetDepositsLimitOperation>());

            var transferTicketOps = operations.HasFlag(Data.Models.Operations.TransferTicket)
                ? Operations.GetTransferTicketOps(block, format, quote)
                : Task.FromResult(Enumerable.Empty<TransferTicketOperation>());

            var txRollupCommitOps = operations.HasFlag(Data.Models.Operations.TxRollupCommit)
                ? Operations.GetTxRollupCommitOps(block, quote)
                : Task.FromResult(Enumerable.Empty<TxRollupCommitOperation>());
            
            var txRollupDispatchTicketsOps = operations.HasFlag(Data.Models.Operations.TxRollupDispatchTickets)
                ? Operations.GetTxRollupDispatchTicketsOps(block, quote)
                : Task.FromResult(Enumerable.Empty<TxRollupDispatchTicketsOperation>());

            var txRollupFinalizeCommitmentOps = operations.HasFlag(Data.Models.Operations.TxRollupFinalizeCommitment)
                ? Operations.GetTxRollupFinalizeCommitmentOps(block, quote)
                : Task.FromResult(Enumerable.Empty<TxRollupFinalizeCommitmentOperation>());

            var txRollupOriginationOps = operations.HasFlag(Data.Models.Operations.TxRollupOrigination)
                ? Operations.GetTxRollupOriginationOps(block, quote)
                : Task.FromResult(Enumerable.Empty<TxRollupOriginationOperation>());

            var txRollupRejectionOps = operations.HasFlag(Data.Models.Operations.TxRollupRejection)
                ? Operations.GetTxRollupRejectionOps(block, quote)
                : Task.FromResult(Enumerable.Empty<TxRollupRejectionOperation>());

            var txRollupRemoveCommitmentOps = operations.HasFlag(Data.Models.Operations.TxRollupRemoveCommitment)
                ? Operations.GetTxRollupRemoveCommitmentOps(block, quote)
                : Task.FromResult(Enumerable.Empty<TxRollupRemoveCommitmentOperation>());

            var txRollupReturnBondOps = operations.HasFlag(Data.Models.Operations.TxRollupReturnBond)
                ? Operations.GetTxRollupReturnBondOps(block, quote)
                : Task.FromResult(Enumerable.Empty<TxRollupReturnBondOperation>());

            var txRollupSubmitBatchOps = operations.HasFlag(Data.Models.Operations.TxRollupSubmitBatch)
                ? Operations.GetTxRollupSubmitBatchOps(block, quote)
                : Task.FromResult(Enumerable.Empty<TxRollupSubmitBatchOperation>());

            var increasePaidStorageOps = operations.HasFlag(Data.Models.Operations.IncreasePaidStorage)
                ? Operations.GetIncreasePaidStorageOps(block, quote)
                : Task.FromResult(Enumerable.Empty<IncreasePaidStorageOperation>());

            var migrations = operations.HasFlag(Data.Models.Operations.Migrations)
                ? Operations.GetMigrations(null, null, null, null, new Int32Parameter { Eq = block.Level }, null, null, null, 10_000, format, quote)
                : Task.FromResult(Enumerable.Empty<MigrationOperation>());

            var penalties = operations.HasFlag(Data.Models.Operations.RevelationPenalty)
                ? Operations.GetRevelationPenalties(null, new Int32Parameter { Eq = block.Level }, null, null, null, 10_000, quote)
                : Task.FromResult(Enumerable.Empty<RevelationPenaltyOperation>());

            var endorsingRewards = operations.HasFlag(Data.Models.Operations.EndorsingRewards)
                ? Operations.GetEndorsingRewards(null, new Int32Parameter { Eq = block.Level }, null, null, null, 10_000, quote)
                : Task.FromResult(Enumerable.Empty<EndorsingRewardOperation>());

            await Task.WhenAll(
                endorsements,
                preendorsements,
                proposals,
                ballots,
                activations,
                doubleBaking,
                doubleEndorsing,
                doublePreendorsing,
                nonceRevelations,
                vdfRevelations,
                delegations,
                originations,
                transactions,
                reveals,
                registerConstants,
                setDepositsLimits,
                transferTicketOps,
                txRollupCommitOps,
                txRollupDispatchTicketsOps,
                txRollupFinalizeCommitmentOps,
                txRollupOriginationOps,
                txRollupRejectionOps,
                txRollupRemoveCommitmentOps,
                txRollupReturnBondOps,
                txRollupSubmitBatchOps,
                increasePaidStorageOps,
                migrations,
                penalties,
                endorsingRewards);

            block.Endorsements = endorsements.Result;
            block.Preendorsements = preendorsements.Result;
            block.Proposals = proposals.Result;
            block.Ballots = ballots.Result;
            block.Activations = activations.Result;
            block.DoubleBaking = doubleBaking.Result;
            block.DoubleEndorsing = doubleEndorsing.Result;
            block.DoublePreendorsing = doublePreendorsing.Result;
            block.NonceRevelations = nonceRevelations.Result;
            block.VdfRevelations = vdfRevelations.Result;
            block.Delegations = delegations.Result;
            block.Originations = originations.Result;
            block.Transactions = transactions.Result;
            block.Reveals = reveals.Result;
            block.RegisterConstants = registerConstants.Result;
            block.SetDepositsLimits = setDepositsLimits.Result;
            block.TransferTicketOps = transferTicketOps.Result;
            block.TxRollupCommitOps = txRollupCommitOps.Result;
            block.TxRollupDispatchTicketsOps = txRollupDispatchTicketsOps.Result;
            block.TxRollupFinalizeCommitmentOps = txRollupFinalizeCommitmentOps.Result;
            block.TxRollupOriginationOps = txRollupOriginationOps.Result;
            block.TxRollupRejectionOps = txRollupRejectionOps.Result;
            block.TxRollupRemoveCommitmentOps = txRollupRemoveCommitmentOps.Result;
            block.TxRollupReturnBondOps = txRollupReturnBondOps.Result;
            block.TxRollupSubmitBatchOps = txRollupSubmitBatchOps.Result;
            block.IncreasePaidStorageOps = increasePaidStorageOps.Result;
            block.Migrations = migrations.Result;
            block.RevelationPenalties = penalties.Result;
            block.EndorsingRewards = endorsingRewards.Result;
        }
    }
}
