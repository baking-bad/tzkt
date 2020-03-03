using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Dapper;

using Tzkt.Api.Models;
using Tzkt.Api.Services.Cache;

namespace Tzkt.Api.Repositories
{
    public class OperationRepository : DbConnection
    {
        readonly AccountsCache Accounts;

        public OperationRepository(AccountsCache accounts, IConfiguration config) : base(config)
        {
            Accounts = accounts;
        }

        #region operations
        public async Task<IEnumerable<Operation>> Get(string hash)
        {
            #region test manager operations
            var delegations = GetDelegations(hash);
            var originations = GetOriginations(hash);
            var transactions = GetTransactions(hash);
            var reveals = GetReveals(hash);

            await Task.WhenAll(delegations, originations, transactions, reveals);

            var managerOps = ((IEnumerable<Operation>)delegations.Result)
                .Concat(originations.Result)
                .Concat(transactions.Result)
                .Concat(reveals.Result);

            if (managerOps.Any())
                return managerOps.OrderBy(x => x.Id);
            #endregion

            #region less likely
            var activations = GetActivations(hash);
            var proposals = GetProposals(hash);
            var ballots = GetBallots(hash);

            await Task.WhenAll(activations, proposals, ballots);

            if (activations.Result.Any())
                return activations.Result;

            if (proposals.Result.Any())
                return proposals.Result;

            if (ballots.Result.Any())
                return ballots.Result;
            #endregion

            #region very unlikely
            var endorsements = GetEndorsements(hash);
            var dobleBaking = GetDoubleBakings(hash);
            var doubleEndorsing = GetDoubleEndorsings(hash);
            var nonceRevelation = GetNonceRevelations(hash);

            await Task.WhenAll(endorsements, dobleBaking, doubleEndorsing, nonceRevelation);

            if (endorsements.Result.Any())
                return endorsements.Result;

            if (dobleBaking.Result.Any())
                return dobleBaking.Result;

            if (doubleEndorsing.Result.Any())
                return doubleEndorsing.Result;

            if (nonceRevelation.Result.Any())
                return nonceRevelation.Result;
            #endregion

            return new List<Operation>(0);
        }

        public async Task<IEnumerable<Operation>> Get(string hash, int counter)
        {
            var delegations = GetDelegations(hash, counter);
            var originations = GetOriginations(hash, counter);
            var transactions = GetTransactions(hash, counter);
            var reveals = GetReveals(hash, counter);

            await Task.WhenAll(delegations, originations, transactions, reveals);

            if (reveals.Result.Any())
                return reveals.Result;

            var managerOps = ((IEnumerable<Operation>)delegations.Result)
                .Concat(originations.Result)
                .Concat(transactions.Result);

            if (managerOps.Any())
                return managerOps.OrderBy(x => x.Id);

            return new List<Operation>(0);
        }

        public async Task<IEnumerable<Operation>> Get(string hash, int counter, int nonce)
        {
            var delegations = GetDelegations(hash, counter, nonce);
            var originations = GetOriginations(hash, counter, nonce);
            var transactions = GetTransactions(hash, counter, nonce);

            await Task.WhenAll(delegations, originations, transactions);

            if (delegations.Result.Any())
                return delegations.Result;

            if (originations.Result.Any())
                return originations.Result;

            if (transactions.Result.Any())
                return transactions.Result;

            return new List<Operation>(0);
        }
        #endregion

        #region endorsements
        public async Task<int> GetEndorsementsCount()
        {
            var sql = @"
                SELECT   COUNT(*)
                FROM     ""EndorsementOps""";

            using var db = GetConnection();
            return await db.QueryFirstAsync<int>(sql);
        }

        public async Task<IEnumerable<EndorsementOperation>> GetEndorsements(string hash)
        {
            var sql = @"
                SELECT   ""Id"", ""Level"", ""Timestamp"", ""DelegateId"", ""Slots"", ""Reward""
                FROM     ""EndorsementOps""
                WHERE    ""OpHash"" = @hash::character(51)
                LIMIT    1";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { hash });

            return rows.Select(row => new EndorsementOperation
            {
                Id = row.Id,
                Level = row.Level,
                Timestamp = row.Timestamp,
                Hash = hash,
                Delegate = Accounts.GetAlias(row.DelegateId),
                Slots = row.Slots,
                Rewards = row.Reward
            });
        }

        public async Task<IEnumerable<EndorsementOperation>> GetEndorsements(int level)
        {
            var sql = @"
                SELECT   ""Id"", ""Timestamp"", ""OpHash"", ""DelegateId"", ""Slots"", ""Reward""
                FROM     ""EndorsementOps""
                WHERE    ""Level"" = @level
                ORDER BY ""Id""";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { level });

            return rows.Select(row => new EndorsementOperation
            {
                Id = row.Id,
                Level = level,
                Timestamp = row.Timestamp,
                Hash = row.OpHash,
                Delegate = Accounts.GetAlias(row.DelegateId),
                Slots = row.Slots,
                Rewards = row.Reward
            });
        }

        public async Task<IEnumerable<EndorsementOperation>> GetEndorsements(int limit = 100, int offset = 0)
        {
            var sql = @"
                SELECT   ""Id"", ""Level"", ""Timestamp"", ""OpHash"", ""DelegateId"", ""Slots"", ""Reward""
                FROM     ""EndorsementOps""
                ORDER BY ""Id""
                OFFSET   @offset
                LIMIT    @limit";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { limit, offset });

            return rows.Select(row => new EndorsementOperation
            {
                Id = row.Id,
                Level = row.Level,
                Timestamp = row.Timestamp,
                Hash = row.OpHash,
                Delegate = Accounts.GetAlias(row.DelegateId),
                Slots = row.Slots,
                Rewards = row.Reward
            });
        }

        public async Task<IEnumerable<EndorsementOperation>> GetEndorsements(RawAccount account, SortMode sort, int offset, OffsetMode offsetMode, int limit)
        {
            var sql = $@"
                SELECT   ""Id"", ""Level"", ""Timestamp"", ""OpHash"", ""Slots"", ""Reward""
                FROM     ""EndorsementOps""
                WHERE    ""DelegateId"" = @accountId
                {Pagination(sort, offset, offsetMode, limit)}";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { accountId = account.Id });

            return rows.Select(row => new EndorsementOperation
            {
                Id = row.Id,
                Level = row.Level,
                Timestamp = row.Timestamp,
                Hash = row.OpHash,
                Delegate = Accounts.GetAlias(account.Id),
                Slots = row.Slots,
                Rewards = row.Reward
            });
        }

        public async Task<IEnumerable<EndorsementOperation>> GetEndorsements(RawAccount account, DateTime from, DateTime to, SortMode sort, int offset, OffsetMode offsetMode, int limit)
        {
            var sql = $@"
                SELECT   ""Id"", ""Level"", ""Timestamp"", ""OpHash"", ""Slots"", ""Reward""
                FROM     ""EndorsementOps""
                WHERE    ""DelegateId"" = @accountId
                AND      ""Timestamp"" >= @from
                AND      ""Timestamp"" < @to
                {Pagination(sort, offset, offsetMode, limit)}";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { accountId = account.Id, from, to });

            return rows.Select(row => new EndorsementOperation
            {
                Id = row.Id,
                Level = row.Level,
                Timestamp = row.Timestamp,
                Hash = row.OpHash,
                Delegate = Accounts.GetAlias(account.Id),
                Slots = row.Slots,
                Rewards = row.Reward
            });
        }
        #endregion

        #region ballots
        public async Task<int> GetBallotsCount()
        {
            var sql = @"
                SELECT   COUNT(*)
                FROM     ""BallotOps""";

            using var db = GetConnection();
            return await db.QueryFirstAsync<int>(sql);
        }

        public async Task<IEnumerable<BallotOperation>> GetBallots(string hash)
        {
            var sql = @"
                SELECT    op.""Id"", op.""Level"", op.""Timestamp"", op.""SenderId"", op.""Vote"", proposal.""Hash"",
                          period.""Code"", period.""Kind"", period.""StartLevel"", period.""EndLevel""
                FROM      ""BallotOps"" as op
                LEFT JOIN ""Proposals"" as proposal ON proposal.""Id"" = op.""ProposalId""
                LEFT JOIN ""VotingPeriods"" as period ON period.""Id"" = op.""PeriodId""
                WHERE     op.""OpHash"" = @hash::character(51)
                LIMIT     1";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { hash });

            return rows.Select(row => new BallotOperation
            {
                Id = row.Id,
                Level = row.Level,
                Timestamp = row.Timestamp,
                Hash = hash,
                Period = new PeriodInfo
                {
                    Id = row.Code,
                    Kind = PeriodToString(row.Kind),
                    StartLevel = row.StartLevel,
                    EndLevel = row.EndLevel
                },
                Proposal = row.Hash,
                Delegate = Accounts.GetAlias(row.SenderId),
                Vote = VoteToString(row.Vote)
            });
        }

        public async Task<IEnumerable<BallotOperation>> GetBallots(int level)
        {
            var sql = @"
                SELECT    op.""Id"", op.""Timestamp"", op.""OpHash"", op.""SenderId"", op.""Vote"", proposal.""Hash"",
                          period.""Code"", period.""Kind"", period.""StartLevel"", period.""EndLevel""
                FROM      ""BallotOps"" as op
                LEFT JOIN ""Proposals"" as proposal ON proposal.""Id"" = op.""ProposalId""
                LEFT JOIN ""VotingPeriods"" as period ON period.""Id"" = op.""PeriodId""
                WHERE     op.""Level"" = @level
                ORDER BY  ""Id""";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { level });

            return rows.Select(row => new BallotOperation
            {
                Id = row.Id,
                Level = level,
                Timestamp = row.Timestamp,
                Hash = row.OpHash,
                Period = new PeriodInfo
                {
                    Id = row.Code,
                    Kind = PeriodToString(row.Kind),
                    StartLevel = row.StartLevel,
                    EndLevel = row.EndLevel
                },
                Proposal = row.Hash,
                Delegate = Accounts.GetAlias(row.SenderId),
                Vote = VoteToString(row.Vote)
            });
        }

        public async Task<IEnumerable<BallotOperation>> GetBallots(int limit = 100, int offset = 0)
        {
            var sql = @"
                SELECT    op.""Id"", op.""Level"", op.""Timestamp"", op.""OpHash"", op.""SenderId"", op.""Vote"", proposal.""Hash"",
                          period.""Code"", period.""Kind"", period.""StartLevel"", period.""EndLevel""
                FROM      ""BallotOps"" as op
                LEFT JOIN ""Proposals"" as proposal ON proposal.""Id"" = op.""ProposalId""
                LEFT JOIN ""VotingPeriods"" as period ON period.""Id"" = op.""PeriodId""
                ORDER BY  op.""Id""
                OFFSET    @offset
                LIMIT     @limit";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { limit, offset });

            return rows.Select(row => new BallotOperation
            {
                Id = row.Id,
                Level = row.Level,
                Timestamp = row.Timestamp,
                Hash = row.OpHash,
                Period = new PeriodInfo
                {
                    Id = row.Code,
                    Kind = PeriodToString(row.Kind),
                    StartLevel = row.StartLevel,
                    EndLevel = row.EndLevel
                },
                Proposal = row.Hash,
                Delegate = Accounts.GetAlias(row.SenderId),
                Vote = VoteToString(row.Vote)
            });
        }

        public async Task<IEnumerable<BallotOperation>> GetBallots(RawAccount account, SortMode sort, int offset, OffsetMode offsetMode, int limit)
        {
            var sql = $@"
                SELECT    op.""Id"", op.""Level"", op.""Timestamp"", op.""OpHash"", op.""Vote"", proposal.""Hash"",
                          period.""Code"", period.""Kind"", period.""StartLevel"", period.""EndLevel""
                FROM      ""BallotOps"" as op
                LEFT JOIN ""Proposals"" as proposal ON proposal.""Id"" = op.""ProposalId""
                LEFT JOIN ""VotingPeriods"" as period ON period.""Id"" = op.""PeriodId""
                WHERE     op.""SenderId"" = @accountId
                {Pagination("op", sort, offset, offsetMode, limit)}";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { accountId = account.Id });

            return rows.Select(row => new BallotOperation
            {
                Id = row.Id,
                Level = row.Level,
                Timestamp = row.Timestamp,
                Hash = row.OpHash,
                Period = new PeriodInfo
                {
                    Id = row.Code,
                    Kind = PeriodToString(row.Kind),
                    StartLevel = row.StartLevel,
                    EndLevel = row.EndLevel
                },
                Proposal = row.Hash,
                Delegate = Accounts.GetAlias(account.Id),
                Vote = VoteToString(row.Vote)
            });
        }

        public async Task<IEnumerable<BallotOperation>> GetBallots(RawAccount account, DateTime from, DateTime to, SortMode sort, int offset, OffsetMode offsetMode, int limit)
        {
            var sql = $@"
                SELECT    op.""Id"", op.""Level"", op.""Timestamp"", op.""OpHash"", op.""Vote"", proposal.""Hash"",
                          period.""Code"", period.""Kind"", period.""StartLevel"", period.""EndLevel""
                FROM      ""BallotOps"" as op
                LEFT JOIN ""Proposals"" as proposal ON proposal.""Id"" = op.""ProposalId""
                LEFT JOIN ""VotingPeriods"" as period ON period.""Id"" = op.""PeriodId""
                WHERE     op.""SenderId"" = @accountId
                AND       op.""Timestamp"" >= @from
                AND       op.""Timestamp"" < @to
                {Pagination("op", sort, offset, offsetMode, limit)}";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { accountId = account.Id, from, to });

            return rows.Select(row => new BallotOperation
            {
                Id = row.Id,
                Level = row.Level,
                Timestamp = row.Timestamp,
                Hash = row.OpHash,
                Period = new PeriodInfo
                {
                    Id = row.Code,
                    Kind = PeriodToString(row.Kind),
                    StartLevel = row.StartLevel,
                    EndLevel = row.EndLevel
                },
                Proposal = row.Hash,
                Delegate = Accounts.GetAlias(account.Id),
                Vote = VoteToString(row.Vote)
            });
        }
        #endregion

        #region proposals
        public async Task<int> GetProposalsCount()
        {
            var sql = @"
                SELECT   COUNT(*)
                FROM     ""ProposalOps""";

            using var db = GetConnection();
            return await db.QueryFirstAsync<int>(sql);
        }

        public async Task<IEnumerable<ProposalOperation>> GetProposals(string hash)
        {
            var sql = @"
                SELECT    op.""Id"", op.""Level"", op.""Timestamp"", op.""SenderId"", proposal.""Hash"",
                          period.""Code"", period.""Kind"", period.""StartLevel"", period.""EndLevel""
                FROM      ""ProposalOps"" as op
                LEFT JOIN ""Proposals"" as proposal ON proposal.""Id"" = op.""ProposalId""
                LEFT JOIN ""VotingPeriods"" as period ON period.""Id"" = op.""PeriodId""
                WHERE     op.""OpHash"" = @hash::character(51)
                ORDER BY  op.""Id""";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { hash });

            return rows.Select(row => new ProposalOperation
            {
                Id = row.Id,
                Level = row.Level,
                Timestamp = row.Timestamp,
                Hash = hash,
                Period = new PeriodInfo
                {
                    Id = row.Code,
                    Kind = PeriodToString(row.Kind),
                    StartLevel = row.StartLevel,
                    EndLevel = row.EndLevel
                },
                Proposal = row.Hash,
                Delegate = Accounts.GetAlias(row.SenderId)
            });
        }

        public async Task<IEnumerable<ProposalOperation>> GetProposals(int level)
        {
            var sql = @"
                SELECT    op.""Id"", op.""Timestamp"", op.""OpHash"", op.""SenderId"", proposal.""Hash"",
                          period.""Code"", period.""Kind"", period.""StartLevel"", period.""EndLevel""
                FROM      ""ProposalOps"" as op
                LEFT JOIN ""Proposals"" as proposal ON proposal.""Id"" = op.""ProposalId""
                LEFT JOIN ""VotingPeriods"" as period ON period.""Id"" = op.""PeriodId""
                WHERE     op.""Level"" = @level
                ORDER BY  op.""Id""";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { level });

            return rows.Select(row => new ProposalOperation
            {
                Id = row.Id,
                Level = level,
                Timestamp = row.Timestamp,
                Hash = row.OpHash,
                Period = new PeriodInfo
                {
                    Id = row.Code,
                    Kind = PeriodToString(row.Kind),
                    StartLevel = row.StartLevel,
                    EndLevel = row.EndLevel
                },
                Proposal = row.Hash,
                Delegate = Accounts.GetAlias(row.SenderId)
            });
        }

        public async Task<IEnumerable<ProposalOperation>> GetProposals(int limit = 100, int offset = 0)
        {
            var sql = @"
                SELECT    op.""Id"", op.""Level"", op.""Timestamp"", op.""OpHash"", op.""SenderId"", proposal.""Hash"",
                          period.""Code"", period.""Kind"", period.""StartLevel"", period.""EndLevel""
                FROM      ""ProposalOps"" as op
                LEFT JOIN ""Proposals"" as proposal ON proposal.""Id"" = op.""ProposalId""
                LEFT JOIN ""VotingPeriods"" as period ON period.""Id"" = op.""PeriodId""
                ORDER BY  op.""Id""
                OFFSET    @offset
                LIMIT     @limit";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { limit, offset });

            return rows.Select(row => new ProposalOperation
            {
                Id = row.Id,
                Level = row.Level,
                Timestamp = row.Timestamp,
                Hash = row.OpHash,
                Period = new PeriodInfo
                {
                    Id = row.Code,
                    Kind = PeriodToString(row.Kind),
                    StartLevel = row.StartLevel,
                    EndLevel = row.EndLevel
                },
                Proposal = row.Hash,
                Delegate = Accounts.GetAlias(row.SenderId)
            });
        }

        public async Task<IEnumerable<ProposalOperation>> GetProposals(RawAccount account, SortMode sort, int offset, OffsetMode offsetMode, int limit)
        {
            var sql = $@"
                SELECT    op.""Id"", op.""Level"", op.""Timestamp"", op.""OpHash"", proposal.""Hash"",
                          period.""Code"", period.""Kind"", period.""StartLevel"", period.""EndLevel""
                FROM      ""ProposalOps"" as op
                LEFT JOIN ""Proposals"" as proposal ON proposal.""Id"" = op.""ProposalId""
                LEFT JOIN ""VotingPeriods"" as period ON period.""Id"" = op.""PeriodId""
                WHERE     op.""SenderId"" = @accountId
                {Pagination("op", sort, offset, offsetMode, limit)}";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { accountId = account.Id });

            return rows.Select(row => new ProposalOperation
            {
                Id = row.Id,
                Level = row.Level,
                Timestamp = row.Timestamp,
                Hash = row.OpHash,
                Period = new PeriodInfo
                {
                    Id = row.Code,
                    Kind = PeriodToString(row.Kind),
                    StartLevel = row.StartLevel,
                    EndLevel = row.EndLevel
                },
                Proposal = row.Hash,
                Delegate = Accounts.GetAlias(account.Id)
            });
        }

        public async Task<IEnumerable<ProposalOperation>> GetProposals(RawAccount account, DateTime from, DateTime to, SortMode sort, int offset, OffsetMode offsetMode, int limit)
        {
            var sql = $@"
                SELECT    op.""Id"", op.""Level"", op.""Timestamp"", op.""OpHash"", proposal.""Hash"",
                          period.""Code"", period.""Kind"", period.""StartLevel"", period.""EndLevel""
                FROM      ""ProposalOps"" as op
                LEFT JOIN ""Proposals"" as proposal ON proposal.""Id"" = op.""ProposalId""
                LEFT JOIN ""VotingPeriods"" as period ON period.""Id"" = op.""PeriodId""
                WHERE     op.""SenderId"" = @accountId
                AND       op.""Timestamp"" >= @from
                AND       op.""Timestamp"" < @to
                {Pagination("op", sort, offset, offsetMode, limit)}";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { accountId = account.Id, from, to });

            return rows.Select(row => new ProposalOperation
            {
                Id = row.Id,
                Level = row.Level,
                Timestamp = row.Timestamp,
                Hash = row.OpHash,
                Period = new PeriodInfo
                {
                    Id = row.Code,
                    Kind = PeriodToString(row.Kind),
                    StartLevel = row.StartLevel,
                    EndLevel = row.EndLevel
                },
                Proposal = row.Hash,
                Delegate = Accounts.GetAlias(account.Id)
            });
        }
        #endregion

        #region activations
        public async Task<int> GetActivationsCount()
        {
            var sql = @"
                SELECT   COUNT(*)
                FROM     ""ActivationOps""";

            using var db = GetConnection();
            return await db.QueryFirstAsync<int>(sql);
        }

        public async Task<IEnumerable<ActivationOperation>> GetActivations(string hash)
        {
            var sql = @"
                SELECT    ""Id"", ""Level"", ""Timestamp"", ""AccountId"", ""Balance""
                FROM      ""ActivationOps""
                WHERE     ""OpHash"" = @hash::character(51)
                LIMIT     1";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { hash });

            return rows.Select(row => new ActivationOperation
            {
                Id = row.Id,
                Level = row.Level,
                Timestamp = row.Timestamp,
                Hash = hash,
                Account = Accounts.GetAlias(row.AccountId),
                Balance = row.Balance
            });
        }

        public async Task<IEnumerable<ActivationOperation>> GetActivations(int level)
        {
            var sql = @"
                SELECT    ""Id"", ""Timestamp"", ""OpHash"", ""AccountId"", ""Balance""
                FROM      ""ActivationOps""
                WHERE     ""Level"" = @level
                ORDER BY  ""Id""";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { level });

            return rows.Select(row => new ActivationOperation
            {
                Id = row.Id,
                Level = level,
                Timestamp = row.Timestamp,
                Hash = row.OpHash,
                Account = Accounts.GetAlias(row.AccountId),
                Balance = row.Balance
            });
        }

        public async Task<IEnumerable<ActivationOperation>> GetActivations(int limit = 100, int offset = 0)
        {
            var sql = @"
                SELECT    ""Id"", ""Level"", ""Timestamp"", ""OpHash"", ""AccountId"", ""Balance""
                FROM      ""ActivationOps""
                ORDER BY  ""Id""
                OFFSET    @offset
                LIMIT     @limit";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { limit, offset });

            return rows.Select(row => new ActivationOperation
            {
                Id = row.Id,
                Level = row.Level,
                Timestamp = row.Timestamp,
                Hash = row.OpHash,
                Account = Accounts.GetAlias(row.AccountId),
                Balance = row.Balance
            });
        }

        public async Task<IEnumerable<ActivationOperation>> GetActivations(RawAccount account, SortMode sort, int offset, OffsetMode offsetMode, int limit)
        {
            var sql = $@"
                SELECT    ""Id"", ""Level"", ""Timestamp"", ""OpHash"", ""Balance""
                FROM      ""ActivationOps""
                WHERE     ""AccountId"" = @accountId
                {Pagination(sort, offset, offsetMode, limit)}";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { accountId = account.Id });

            return rows.Select(row => new ActivationOperation
            {
                Id = row.Id,
                Level = row.Level,
                Timestamp = row.Timestamp,
                Hash = row.OpHash,
                Account = Accounts.GetAlias(account.Id),
                Balance = row.Balance
            });
        }

        public async Task<IEnumerable<ActivationOperation>> GetActivations(RawAccount account, DateTime from, DateTime to, SortMode sort, int offset, OffsetMode offsetMode, int limit)
        {
            var sql = $@"
                SELECT    ""Id"", ""Level"", ""Timestamp"", ""OpHash"", ""Balance""
                FROM      ""ActivationOps""
                WHERE     ""AccountId"" = @accountId
                AND       ""Timestamp"" >= @from
                AND       ""Timestamp"" < @to
                {Pagination(sort, offset, offsetMode, limit)}";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { accountId = account.Id, from, to });

            return rows.Select(row => new ActivationOperation
            {
                Id = row.Id,
                Level = row.Level,
                Timestamp = row.Timestamp,
                Hash = row.OpHash,
                Account = Accounts.GetAlias(account.Id),
                Balance = row.Balance
            });
        }
        #endregion

        #region double baking
        public async Task<int> GetDoubleBakingsCount()
        {
            var sql = @"
                SELECT   COUNT(*)
                FROM     ""DoubleBakingOps""";

            using var db = GetConnection();
            return await db.QueryFirstAsync<int>(sql);
        }

        public async Task<IEnumerable<DoubleBakingOperation>> GetDoubleBakings(string hash)
        {
            var sql = @"
                SELECT    ""Id"", ""Level"", ""Timestamp"", ""AccusedLevel"", ""AccuserId"", ""AccuserReward"",
                          ""OffenderId"", ""OffenderLostDeposit"", ""OffenderLostReward"", ""OffenderLostFee""
                FROM      ""DoubleBakingOps""
                WHERE     ""OpHash"" = @hash::character(51)
                LIMIT     1";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { hash });

            return rows.Select(row => new DoubleBakingOperation
            {
                Id = row.Id,
                Level = row.Level,
                Timestamp = row.Timestamp,
                Hash = hash,
                AccusedLevel = row.AccusedLevel,
                Accuser = Accounts.GetAlias(row.AccuserId),
                AccuserRewards = row.AccuserReward,
                Offender = Accounts.GetAlias(row.OffenderId),
                OffenderLostDeposits = row.OffenderLostDeposit,
                OffenderLostRewards = row.OffenderLostReward,
                OffenderLostFees = row.OffenderLostFee
            });
        }

        public async Task<IEnumerable<DoubleBakingOperation>> GetDoubleBakings(int level)
        {
            var sql = @"
                SELECT    ""Id"", ""Timestamp"", ""OpHash"", ""AccusedLevel"", ""AccuserId"", ""AccuserReward"",
                          ""OffenderId"", ""OffenderLostDeposit"", ""OffenderLostReward"", ""OffenderLostFee""
                FROM      ""DoubleBakingOps""
                WHERE     ""Level"" = @level
                ORDER BY  ""Id""";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { level });

            return rows.Select(row => new DoubleBakingOperation
            {
                Id = row.Id,
                Level = level,
                Timestamp = row.Timestamp,
                Hash = row.OpHash,
                AccusedLevel = row.AccusedLevel,
                Accuser = Accounts.GetAlias(row.AccuserId),
                AccuserRewards = row.AccuserReward,
                Offender = Accounts.GetAlias(row.OffenderId),
                OffenderLostDeposits = row.OffenderLostDeposit,
                OffenderLostRewards = row.OffenderLostReward,
                OffenderLostFees = row.OffenderLostFee
            });
        }

        public async Task<IEnumerable<DoubleBakingOperation>> GetDoubleBakings(int limit = 100, int offset = 0)
        {
            var sql = @"
                SELECT    ""Id"", ""Level"", ""Timestamp"", ""OpHash"", ""AccusedLevel"", ""AccuserId"", ""AccuserReward"",
                          ""OffenderId"", ""OffenderLostDeposit"", ""OffenderLostReward"", ""OffenderLostFee""
                FROM      ""DoubleBakingOps""
                ORDER BY  ""Id""
                OFFSET    @offset
                LIMIT     @limit";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { limit, offset });

            return rows.Select(row => new DoubleBakingOperation
            {
                Id = row.Id,
                Level = row.Level,
                Timestamp = row.Timestamp,
                Hash = row.OpHash,
                AccusedLevel = row.AccusedLevel,
                Accuser = Accounts.GetAlias(row.AccuserId),
                AccuserRewards = row.AccuserReward,
                Offender = Accounts.GetAlias(row.OffenderId),
                OffenderLostDeposits = row.OffenderLostDeposit,
                OffenderLostRewards = row.OffenderLostReward,
                OffenderLostFees = row.OffenderLostFee
            });
        }

        public async Task<IEnumerable<DoubleBakingOperation>> GetDoubleBakings(RawAccount account, SortMode sort, int offset, OffsetMode offsetMode, int limit)
        {
            var sql = $@"
                SELECT    ""Id"", ""Level"", ""Timestamp"", ""OpHash"", ""AccusedLevel"", ""AccuserId"", ""AccuserReward"",
                          ""OffenderId"", ""OffenderLostDeposit"", ""OffenderLostReward"", ""OffenderLostFee""
                FROM      ""DoubleBakingOps""
                WHERE     (""AccuserId"" = @accountId
                OR        ""OffenderId"" = @accountId)
                {Pagination(sort, offset, offsetMode, limit)}";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { accountId = account.Id });

            return rows.Select(row => new DoubleBakingOperation
            {
                Id = row.Id,
                Level = row.Level,
                Timestamp = row.Timestamp,
                Hash = row.OpHash,
                AccusedLevel = row.AccusedLevel,
                Accuser = Accounts.GetAlias(row.AccuserId),
                AccuserRewards = row.AccuserReward,
                Offender = Accounts.GetAlias(row.OffenderId),
                OffenderLostDeposits = row.OffenderLostDeposit,
                OffenderLostRewards = row.OffenderLostReward,
                OffenderLostFees = row.OffenderLostFee
            });
        }

        public async Task<IEnumerable<DoubleBakingOperation>> GetDoubleBakings(RawAccount account, DateTime from, DateTime to, SortMode sort, int offset, OffsetMode offsetMode, int limit)
        {
            var sql = $@"
                SELECT    ""Id"", ""Level"", ""Timestamp"", ""OpHash"", ""AccusedLevel"", ""AccuserId"", ""AccuserReward"",
                          ""OffenderId"", ""OffenderLostDeposit"", ""OffenderLostReward"", ""OffenderLostFee""
                FROM      ""DoubleBakingOps""
                WHERE     (""AccuserId"" = @accountId
                OR        ""OffenderId"" = @accountId)
                AND       ""Timestamp"" >= @from
                AND       ""Timestamp"" < @to
                {Pagination(sort, offset, offsetMode, limit)}";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { accountId = account.Id, from, to });

            return rows.Select(row => new DoubleBakingOperation
            {
                Id = row.Id,
                Level = row.Level,
                Timestamp = row.Timestamp,
                Hash = row.OpHash,
                AccusedLevel = row.AccusedLevel,
                Accuser = Accounts.GetAlias(row.AccuserId),
                AccuserRewards = row.AccuserReward,
                Offender = Accounts.GetAlias(row.OffenderId),
                OffenderLostDeposits = row.OffenderLostDeposit,
                OffenderLostRewards = row.OffenderLostReward,
                OffenderLostFees = row.OffenderLostFee
            });
        }
        #endregion

        #region double endorsing
        public async Task<int> GetDoubleEndorsingsCount()
        {
            var sql = @"
                SELECT   COUNT(*)
                FROM     ""DoubleEndorsingOps""";

            using var db = GetConnection();
            return await db.QueryFirstAsync<int>(sql);
        }

        public async Task<IEnumerable<DoubleEndorsingOperation>> GetDoubleEndorsings(string hash)
        {
            var sql = @"
                SELECT    ""Id"", ""Level"", ""Timestamp"", ""AccusedLevel"", ""AccuserId"", ""AccuserReward"",
                          ""OffenderId"", ""OffenderLostDeposit"", ""OffenderLostReward"", ""OffenderLostFee""
                FROM      ""DoubleEndorsingOps""
                WHERE     ""OpHash"" = @hash::character(51)
                LIMIT     1";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { hash });

            return rows.Select(row => new DoubleEndorsingOperation
            {
                Id = row.Id,
                Level = row.Level,
                Timestamp = row.Timestamp,
                Hash = hash,
                AccusedLevel = row.AccusedLevel,
                Accuser = Accounts.GetAlias(row.AccuserId),
                AccuserRewards = row.AccuserReward,
                Offender = Accounts.GetAlias(row.OffenderId),
                OffenderLostDeposits = row.OffenderLostDeposit,
                OffenderLostRewards = row.OffenderLostReward,
                OffenderLostFees = row.OffenderLostFee
            });
        }

        public async Task<IEnumerable<DoubleEndorsingOperation>> GetDoubleEndorsings(int level)
        {
            var sql = @"
                SELECT    ""Id"", ""Timestamp"", ""OpHash"", ""AccusedLevel"", ""AccuserId"", ""AccuserReward"",
                          ""OffenderId"", ""OffenderLostDeposit"", ""OffenderLostReward"", ""OffenderLostFee""
                FROM      ""DoubleEndorsingOps""
                WHERE     ""Level"" = @level
                ORDER BY  ""Id""";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { level });

            return rows.Select(row => new DoubleEndorsingOperation
            {
                Id = row.Id,
                Level = level,
                Timestamp = row.Timestamp,
                Hash = row.OpHash,
                AccusedLevel = row.AccusedLevel,
                Accuser = Accounts.GetAlias(row.AccuserId),
                AccuserRewards = row.AccuserReward,
                Offender = Accounts.GetAlias(row.OffenderId),
                OffenderLostDeposits = row.OffenderLostDeposit,
                OffenderLostRewards = row.OffenderLostReward,
                OffenderLostFees = row.OffenderLostFee
            });
        }

        public async Task<IEnumerable<DoubleEndorsingOperation>> GetDoubleEndorsings(int limit = 100, int offset = 0)
        {
            var sql = @"
                SELECT    ""Id"", ""Level"", ""Timestamp"", ""OpHash"", ""AccusedLevel"", ""AccuserId"", ""AccuserReward"",
                          ""OffenderId"", ""OffenderLostDeposit"", ""OffenderLostReward"", ""OffenderLostFee""
                FROM      ""DoubleEndorsingOps""
                ORDER BY  ""Id""
                OFFSET    @offset
                LIMIT     @limit";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { limit, offset });

            return rows.Select(row => new DoubleEndorsingOperation
            {
                Id = row.Id,
                Level = row.Level,
                Timestamp = row.Timestamp,
                Hash = row.OpHash,
                AccusedLevel = row.AccusedLevel,
                Accuser = Accounts.GetAlias(row.AccuserId),
                AccuserRewards = row.AccuserReward,
                Offender = Accounts.GetAlias(row.OffenderId),
                OffenderLostDeposits = row.OffenderLostDeposit,
                OffenderLostRewards = row.OffenderLostReward,
                OffenderLostFees = row.OffenderLostFee
            });
        }

        public async Task<IEnumerable<DoubleEndorsingOperation>> GetDoubleEndorsings(RawAccount account, SortMode sort, int offset, OffsetMode offsetMode, int limit)
        {
            var sql = $@"
                SELECT    ""Id"", ""Level"", ""Timestamp"", ""OpHash"", ""AccusedLevel"", ""AccuserId"", ""AccuserReward"",
                          ""OffenderId"", ""OffenderLostDeposit"", ""OffenderLostReward"", ""OffenderLostFee""
                FROM      ""DoubleEndorsingOps""
                WHERE     (""AccuserId"" = @accountId
                OR        ""OffenderId"" = @accountId)
                {Pagination(sort, offset, offsetMode, limit)}";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { accountId = account.Id });

            return rows.Select(row => new DoubleEndorsingOperation
            {
                Id = row.Id,
                Level = row.Level,
                Timestamp = row.Timestamp,
                Hash = row.OpHash,
                AccusedLevel = row.AccusedLevel,
                Accuser = Accounts.GetAlias(row.AccuserId),
                AccuserRewards = row.AccuserReward,
                Offender = Accounts.GetAlias(row.OffenderId),
                OffenderLostDeposits = row.OffenderLostDeposit,
                OffenderLostRewards = row.OffenderLostReward,
                OffenderLostFees = row.OffenderLostFee
            });
        }

        public async Task<IEnumerable<DoubleEndorsingOperation>> GetDoubleEndorsings(RawAccount account, DateTime from, DateTime to, SortMode sort, int offset, OffsetMode offsetMode, int limit)
        {
            var sql = $@"
                SELECT    ""Id"", ""Level"", ""Timestamp"", ""OpHash"", ""AccusedLevel"", ""AccuserId"", ""AccuserReward"",
                          ""OffenderId"", ""OffenderLostDeposit"", ""OffenderLostReward"", ""OffenderLostFee""
                FROM      ""DoubleEndorsingOps""
                WHERE     (""AccuserId"" = @accountId
                OR        ""OffenderId"" = @accountId)
                AND       ""Timestamp"" >= @from
                AND       ""Timestamp"" < @to
                {Pagination(sort, offset, offsetMode, limit)}";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { accountId = account.Id, from, to });

            return rows.Select(row => new DoubleEndorsingOperation
            {
                Id = row.Id,
                Level = row.Level,
                Timestamp = row.Timestamp,
                Hash = row.OpHash,
                AccusedLevel = row.AccusedLevel,
                Accuser = Accounts.GetAlias(row.AccuserId),
                AccuserRewards = row.AccuserReward,
                Offender = Accounts.GetAlias(row.OffenderId),
                OffenderLostDeposits = row.OffenderLostDeposit,
                OffenderLostRewards = row.OffenderLostReward,
                OffenderLostFees = row.OffenderLostFee
            });
        }
        #endregion

        #region nonce revelations
        public async Task<int> GetNonceRevelationsCount()
        {
            var sql = @"
                SELECT   COUNT(*)
                FROM     ""NonceRevelationOps""";

            using var db = GetConnection();
            return await db.QueryFirstAsync<int>(sql);
        }

        public async Task<IEnumerable<NonceRevelationOperation>> GetNonceRevelations(string hash)
        {
            var sql = @"
                SELECT    ""Id"", ""Level"", ""Timestamp"", ""BakerId"", ""SenderId"", ""RevealedLevel""
                FROM      ""NonceRevelationOps""
                WHERE     ""OpHash"" = @hash::character(51)
                LIMIT     1";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { hash });

            return rows.Select(row => new NonceRevelationOperation
            {
                Id = row.Id,
                Level = row.Level,
                Timestamp = row.Timestamp,
                Hash = hash,
                Baker = Accounts.GetAlias(row.BakerId),
                Sender = Accounts.GetAlias(row.SenderId),
                RevealedLevel = row.RevealedLevel
            });
        }

        public async Task<IEnumerable<NonceRevelationOperation>> GetNonceRevelations(int level)
        {
            var sql = @"
                SELECT    ""Id"", ""Timestamp"", ""OpHash"", ""BakerId"", ""SenderId"", ""RevealedLevel""
                FROM      ""NonceRevelationOps""
                WHERE     ""Level"" = @level
                ORDER BY  ""Id""";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { level });

            return rows.Select(row => new NonceRevelationOperation
            {
                Id = row.Id,
                Level = level,
                Timestamp = row.Timestamp,
                Hash = row.OpHash,
                Baker = Accounts.GetAlias(row.BakerId),
                Sender = Accounts.GetAlias(row.SenderId),
                RevealedLevel = row.RevealedLevel
            });
        }

        public async Task<IEnumerable<NonceRevelationOperation>> GetNonceRevelations(int limit = 100, int offset = 0)
        {
            var sql = @"
                SELECT    ""Id"", ""Level"", ""Timestamp"", ""OpHash"", ""BakerId"", ""SenderId"", ""RevealedLevel""
                FROM      ""NonceRevelationOps""
                ORDER BY  ""Id""
                OFFSET    @offset
                LIMIT     @limit";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { limit, offset });

            return rows.Select(row => new NonceRevelationOperation
            {
                Id = row.Id,
                Level = row.Level,
                Timestamp = row.Timestamp,
                Hash = row.OpHash,
                Baker = Accounts.GetAlias(row.BakerId),
                Sender = Accounts.GetAlias(row.SenderId),
                RevealedLevel = row.RevealedLevel
            });
        }

        public async Task<IEnumerable<NonceRevelationOperation>> GetNonceRevelations(RawAccount account, SortMode sort, int offset, OffsetMode offsetMode, int limit)
        {
            var sql = $@"
                SELECT    ""Id"", ""Level"", ""Timestamp"", ""OpHash"", ""BakerId"", ""SenderId"", ""RevealedLevel""
                FROM      ""NonceRevelationOps""
                WHERE     (""BakerId"" = @accountId
                OR        ""SenderId"" = @accountId)
                {Pagination(sort, offset, offsetMode, limit)}";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { accountId = account.Id });

            return rows.Select(row => new NonceRevelationOperation
            {
                Id = row.Id,
                Level = row.Level,
                Timestamp = row.Timestamp,
                Hash = row.OpHash,
                Baker = Accounts.GetAlias(row.BakerId),
                Sender = Accounts.GetAlias(row.SenderId),
                RevealedLevel = row.RevealedLevel
            });
        }

        public async Task<IEnumerable<NonceRevelationOperation>> GetNonceRevelations(RawAccount account, DateTime from, DateTime to, SortMode sort, int offset, OffsetMode offsetMode, int limit)
        {
            var sql = $@"
                SELECT    ""Id"", ""Level"", ""Timestamp"", ""OpHash"", ""BakerId"", ""SenderId"", ""RevealedLevel""
                FROM      ""NonceRevelationOps""
                WHERE     (""BakerId"" = @accountId
                OR        ""SenderId"" = @accountId)
                AND       ""Timestamp"" >= @from
                AND       ""Timestamp"" < @to
                {Pagination(sort, offset, offsetMode, limit)}";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { accountId = account.Id, from, to });

            return rows.Select(row => new NonceRevelationOperation
            {
                Id = row.Id,
                Level = row.Level,
                Timestamp = row.Timestamp,
                Hash = row.OpHash,
                Baker = Accounts.GetAlias(row.BakerId),
                Sender = Accounts.GetAlias(row.SenderId),
                RevealedLevel = row.RevealedLevel
            });
        }
        #endregion

        #region delegations
        public async Task<int> GetDelegationsCount()
        {
            var sql = @"
                SELECT   COUNT(*)
                FROM     ""DelegationOps""";

            using var db = GetConnection();
            return await db.QueryFirstAsync<int>(sql);
        }

        public async Task<IEnumerable<DelegationOperation>> GetDelegations(string hash)
        {
            var sql = @"
                SELECT    ""Id"", ""Level"", ""Timestamp"", ""SenderId"", ""InitiatorId"", ""Counter"", ""BakerFee"",
                          ""GasLimit"", ""GasUsed"", ""Status"", ""Nonce"", ""PrevDelegateId"", ""DelegateId"", ""Errors""
                FROM      ""DelegationOps""
                WHERE     ""OpHash"" = @hash::character(51)
                ORDER BY  ""Id""";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { hash });

            return rows.Select(row => new DelegationOperation
            {
                Id = row.Id,
                Level = row.Level,
                Timestamp = row.Timestamp,
                Hash = hash,
                Initiator = row.InitiatorId != null ? Accounts.GetAlias(row.InitiatorId) : null,
                Sender = Accounts.GetAlias(row.SenderId),
                Counter = row.Counter,
                Nonce = row.Nonce,
                GasLimit = row.GasLimit,
                GasUsed = row.GasUsed,
                BakerFee = row.BakerFee,
                PrevDelegate = row.PrevDelegateId != null ? Accounts.GetAlias(row.PrevDelegateId) : null,
                NewDelegate = row.DelegateId != null ? Accounts.GetAlias(row.DelegateId) : null,
                Status = StatusToString(row.Status),
                Errors = row.Errors != null ? OperationErrorSerializer.Deserialize(row.Errors) : null
            });
        }

        public async Task<IEnumerable<DelegationOperation>> GetDelegations(string hash, int counter)
        {
            var sql = @"
                SELECT    ""Id"", ""Level"", ""Timestamp"", ""SenderId"", ""InitiatorId"", ""BakerFee"",
                          ""GasLimit"", ""GasUsed"", ""Status"", ""Nonce"", ""PrevDelegateId"", ""DelegateId"", ""Errors""
                FROM      ""DelegationOps""
                WHERE     ""OpHash"" = @hash::character(51) AND ""Counter"" = @counter
                ORDER BY  ""Id""";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { hash, counter });

            return rows.Select(row => new DelegationOperation
            {
                Id = row.Id,
                Level = row.Level,
                Timestamp = row.Timestamp,
                Hash = hash,
                Initiator = row.InitiatorId != null ? Accounts.GetAlias(row.InitiatorId) : null,
                Sender = Accounts.GetAlias(row.SenderId),
                Counter = counter,
                Nonce = row.Nonce,
                GasLimit = row.GasLimit,
                GasUsed = row.GasUsed,
                BakerFee = row.BakerFee,
                PrevDelegate = row.PrevDelegateId != null ? Accounts.GetAlias(row.PrevDelegateId) : null,
                NewDelegate = row.DelegateId != null ? Accounts.GetAlias(row.DelegateId) : null,
                Status = StatusToString(row.Status),
                Errors = row.Errors != null ? OperationErrorSerializer.Deserialize(row.Errors) : null
            });
        }

        public async Task<IEnumerable<DelegationOperation>> GetDelegations(string hash, int counter, int nonce)
        {
            var sql = @"
                SELECT    ""Id"", ""Level"", ""Timestamp"", ""SenderId"", ""InitiatorId"", ""BakerFee"",
                          ""GasLimit"", ""GasUsed"", ""Status"", ""PrevDelegateId"", ""DelegateId"", ""Errors""
                FROM      ""DelegationOps""
                WHERE     ""OpHash"" = @hash::character(51) AND ""Counter"" = @counter AND ""Nonce"" = @nonce
                LIMIT     1";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { hash, counter, nonce });

            return rows.Select(row => new DelegationOperation
            {
                Id = row.Id,
                Level = row.Level,
                Timestamp = row.Timestamp,
                Hash = hash,
                Initiator = row.InitiatorId != null ? Accounts.GetAlias(row.InitiatorId) : null,
                Sender = Accounts.GetAlias(row.SenderId),
                Counter = counter,
                Nonce = nonce,
                GasLimit = row.GasLimit,
                GasUsed = row.GasUsed,
                BakerFee = row.BakerFee,
                PrevDelegate = row.PrevDelegateId != null ? Accounts.GetAlias(row.PrevDelegateId) : null,
                NewDelegate = row.DelegateId != null ? Accounts.GetAlias(row.DelegateId) : null,
                Status = StatusToString(row.Status),
                Errors = row.Errors != null ? OperationErrorSerializer.Deserialize(row.Errors) : null
            });
        }

        public async Task<IEnumerable<DelegationOperation>> GetDelegations(int level)
        {
            var sql = @"
                SELECT    ""Id"", ""Timestamp"", ""OpHash"", ""SenderId"", ""InitiatorId"", ""Counter"", ""BakerFee"",
                          ""GasLimit"", ""GasUsed"", ""Status"", ""Nonce"", ""PrevDelegateId"", ""DelegateId"", ""Errors""
                FROM      ""DelegationOps""
                WHERE     ""Level"" = @level
                ORDER BY  ""Id""";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { level });

            return rows.Select(row => new DelegationOperation
            {
                Id = row.Id,
                Level = level,
                Timestamp = row.Timestamp,
                Hash = row.OpHash,
                Initiator = row.InitiatorId != null ? Accounts.GetAlias(row.InitiatorId) : null,
                Sender = Accounts.GetAlias(row.SenderId),
                Counter = row.Counter,
                Nonce = row.Nonce,
                GasLimit = row.GasLimit,
                GasUsed = row.GasUsed,
                BakerFee = row.BakerFee,
                PrevDelegate = row.PrevDelegateId != null ? Accounts.GetAlias(row.PrevDelegateId) : null,
                NewDelegate = row.DelegateId != null ? Accounts.GetAlias(row.DelegateId) : null,
                Status = StatusToString(row.Status),
                Errors = row.Errors != null ? OperationErrorSerializer.Deserialize(row.Errors) : null
            });
        }

        public async Task<IEnumerable<DelegationOperation>> GetDelegations(int limit = 100, int offset = 0)
        {
            var sql = @"
                SELECT    ""Id"", ""Level"", ""Timestamp"", ""OpHash"", ""SenderId"", ""InitiatorId"", ""Counter"", ""BakerFee"",
                          ""GasLimit"", ""GasUsed"", ""Status"", ""Nonce"", ""PrevDelegateId"", ""DelegateId"", ""Errors""
                FROM      ""DelegationOps""
                ORDER BY  ""Id""
                OFFSET    @offset
                LIMIT     @limit";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { limit, offset });

            return rows.Select(row => new DelegationOperation
            {
                Id = row.Id,
                Level = row.Level,
                Timestamp = row.Timestamp,
                Hash = row.OpHash,
                Initiator = row.InitiatorId != null ? Accounts.GetAlias(row.InitiatorId) : null,
                Sender = Accounts.GetAlias(row.SenderId),
                Counter = row.Counter,
                Nonce = row.Nonce,
                GasLimit = row.GasLimit,
                GasUsed = row.GasUsed,
                BakerFee = row.BakerFee,
                PrevDelegate = row.PrevDelegateId != null ? Accounts.GetAlias(row.PrevDelegateId) : null,
                NewDelegate = row.DelegateId != null ? Accounts.GetAlias(row.DelegateId) : null,
                Status = StatusToString(row.Status),
                Errors = row.Errors != null ? OperationErrorSerializer.Deserialize(row.Errors) : null
            });
        }

        public async Task<IEnumerable<DelegationOperation>> GetDelegations(RawAccount account, SortMode sort, int offset, OffsetMode offsetMode, int limit)
        {
            var sql = $@"
                SELECT    ""Id"", ""Level"", ""Timestamp"", ""OpHash"", ""SenderId"", ""InitiatorId"", ""Counter"", ""BakerFee"",
                          ""GasLimit"", ""GasUsed"", ""Status"", ""Nonce"", ""PrevDelegateId"", ""DelegateId"", ""Errors""
                FROM      ""DelegationOps""
                WHERE     (""SenderId"" = @accountId
                OR        ""PrevDelegateId"" = @accountId
                OR        ""DelegateId"" = @accountId
                OR        ""InitiatorId"" = @accountId)
                {Pagination(sort, offset, offsetMode, limit)}";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { accountId = account.Id });

            return rows.Select(row => new DelegationOperation
            {
                Id = row.Id,
                Level = row.Level,
                Timestamp = row.Timestamp,
                Hash = row.OpHash,
                Initiator = row.InitiatorId != null ? Accounts.GetAlias(row.InitiatorId) : null,
                Sender = Accounts.GetAlias(row.SenderId),
                Counter = row.Counter,
                Nonce = row.Nonce,
                GasLimit = row.GasLimit,
                GasUsed = row.GasUsed,
                BakerFee = row.BakerFee,
                PrevDelegate = row.PrevDelegateId != null ? Accounts.GetAlias(row.PrevDelegateId) : null,
                NewDelegate = row.DelegateId != null ? Accounts.GetAlias(row.DelegateId) : null,
                Status = StatusToString(row.Status),
                Errors = row.Errors != null ? OperationErrorSerializer.Deserialize(row.Errors) : null
            });
        }

        public async Task<IEnumerable<DelegationOperation>> GetDelegations(RawAccount account, DateTime from, DateTime to, SortMode sort, int offset, OffsetMode offsetMode, int limit)
        {
            var sql = $@"
                SELECT    ""Id"", ""Level"", ""Timestamp"", ""OpHash"", ""SenderId"", ""InitiatorId"", ""Counter"", ""BakerFee"",
                          ""GasLimit"", ""GasUsed"", ""Status"", ""Nonce"", ""PrevDelegateId"", ""DelegateId"", ""Errors""
                FROM      ""DelegationOps""
                WHERE     (""SenderId"" = @accountId
                OR        ""PrevDelegateId"" = @accountId
                OR        ""DelegateId"" = @accountId
                OR        ""InitiatorId"" = @accountId)
                AND       ""Timestamp"" >= @from
                AND       ""Timestamp"" < @to
                {Pagination(sort, offset, offsetMode, limit)}";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { accountId = account.Id, from, to });

            return rows.Select(row => new DelegationOperation
            {
                Id = row.Id,
                Level = row.Level,
                Timestamp = row.Timestamp,
                Hash = row.OpHash,
                Initiator = row.InitiatorId != null ? Accounts.GetAlias(row.InitiatorId) : null,
                Sender = Accounts.GetAlias(row.SenderId),
                Counter = row.Counter,
                Nonce = row.Nonce,
                GasLimit = row.GasLimit,
                GasUsed = row.GasUsed,
                BakerFee = row.BakerFee,
                PrevDelegate = row.PrevDelegateId != null ? Accounts.GetAlias(row.PrevDelegateId) : null,
                NewDelegate = row.DelegateId != null ? Accounts.GetAlias(row.DelegateId) : null,
                Status = StatusToString(row.Status),
                Errors = row.Errors != null ? OperationErrorSerializer.Deserialize(row.Errors) : null
            });
        }
        #endregion

        #region originations
        public async Task<int> GetOriginationsCount()
        {
            var sql = @"
                SELECT   COUNT(*)
                FROM     ""OriginationOps""";

            using var db = GetConnection();
            return await db.QueryFirstAsync<int>(sql);
        }

        public async Task<IEnumerable<OriginationOperation>> GetOriginations(string hash)
        {
            var sql = @"
                SELECT    ""Id"", ""Level"", ""Timestamp"", ""SenderId"", ""InitiatorId"", ""Counter"", ""BakerFee"", ""StorageFee"", ""AllocationFee"", 
                          ""GasLimit"", ""GasUsed"", ""StorageLimit"", ""StorageUsed"", ""Status"", ""Nonce"", ""ContractId"", ""DelegateId"", ""Balance"", ""ManagerId"", ""Errors""
                FROM      ""OriginationOps""
                WHERE     ""OpHash"" = @hash::character(51)
                ORDER BY  ""Id""";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { hash });

            return rows.Select(row =>
            {
                var contract = row.ContractId == null ? null
                    : (RawContract)Accounts.Get((int)row.ContractId);

                var contractMetadata = contract == null ? null
                    : Accounts.GetMetadata(contract.Id);

                return new OriginationOperation
                {
                    Id = row.Id,
                    Level = row.Level,
                    Timestamp = row.Timestamp,
                    Hash = hash,
                    Initiator = row.InitiatorId != null ? Accounts.GetAlias(row.InitiatorId) : null,
                    Sender = Accounts.GetAlias(row.SenderId),
                    Counter = row.Counter,
                    Nonce = row.Nonce,
                    GasLimit = row.GasLimit,
                    GasUsed = row.GasUsed,
                    StorageLimit = row.StorageLimit,
                    StorageUsed = row.StorageUsed,
                    BakerFee = row.BakerFee,
                    StorageFee = row.StorageFee ?? 0,
                    AllocationFee = row.AllocationFee ?? 0,
                    ContractDelegate = row.DelegateId != null ? Accounts.GetAlias(row.DelegateId) : null,
                    ContractBalance = row.Balance,
                    Status = StatusToString(row.Status),
                    OriginatedContract = contract == null ? null :
                        new OriginatedContract
                        {
                            Alias = contractMetadata?.Alias,
                            Address = contract.Address,
                            Kind = contract.KindString
                        },
                    ContractManager = row.ManagerId != null ? Accounts.GetAlias(row.ManagerId) : null,
                    Errors = row.Errors != null ? OperationErrorSerializer.Deserialize(row.Errors) : null
                };
            });
        }

        public async Task<IEnumerable<OriginationOperation>> GetOriginations(string hash, int counter)
        {
            var sql = @"
                SELECT    ""Id"", ""Level"", ""Timestamp"", ""SenderId"", ""InitiatorId"", ""BakerFee"", ""StorageFee"", ""AllocationFee"", 
                          ""GasLimit"", ""GasUsed"", ""StorageLimit"", ""StorageUsed"", ""Status"", ""Nonce"", ""ContractId"", ""DelegateId"", ""Balance"", ""ManagerId"", ""Errors""
                FROM      ""OriginationOps""
                WHERE     ""OpHash"" = @hash::character(51) AND ""Counter"" = @counter
                ORDER BY  ""Id""";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { hash, counter });

            return rows.Select(row =>
            {
                var contract = row.ContractId == null ? null
                    : (RawContract)Accounts.Get((int)row.ContractId);

                var contractMetadata = contract == null ? null
                    : Accounts.GetMetadata(contract.Id);

                return new OriginationOperation
                {
                    Id = row.Id,
                    Level = row.Level,
                    Timestamp = row.Timestamp,
                    Hash = hash,
                    Initiator = row.InitiatorId != null ? Accounts.GetAlias(row.InitiatorId) : null,
                    Sender = Accounts.GetAlias(row.SenderId),
                    Counter = counter,
                    Nonce = row.Nonce,
                    GasLimit = row.GasLimit,
                    GasUsed = row.GasUsed,
                    StorageLimit = row.StorageLimit,
                    StorageUsed = row.StorageUsed,
                    BakerFee = row.BakerFee,
                    StorageFee = row.StorageFee ?? 0,
                    AllocationFee = row.AllocationFee ?? 0,
                    ContractDelegate = row.DelegateId != null ? Accounts.GetAlias(row.DelegateId) : null,
                    ContractBalance = row.Balance,
                    Status = StatusToString(row.Status),
                    OriginatedContract = contract == null ? null :
                        new OriginatedContract
                        {
                            Alias = contractMetadata?.Alias,
                            Address = contract.Address,
                            Kind = contract.KindString
                        },
                    ContractManager = row.ManagerId != null ? Accounts.GetAlias(row.ManagerId) : null,
                    Errors = row.Errors != null ? OperationErrorSerializer.Deserialize(row.Errors) : null
                };
            });
        }

        public async Task<IEnumerable<OriginationOperation>> GetOriginations(string hash, int counter, int nonce)
        {
            var sql = @"
                SELECT    ""Id"", ""Level"", ""Timestamp"", ""SenderId"", ""InitiatorId"", ""BakerFee"", ""StorageFee"", ""AllocationFee"", 
                          ""GasLimit"", ""GasUsed"", ""StorageLimit"", ""StorageUsed"", ""Status"", ""ContractId"", ""DelegateId"", ""Balance"", ""ManagerId"", ""Errors""
                FROM      ""OriginationOps""
                WHERE     ""OpHash"" = @hash::character(51) AND ""Counter"" = @counter AND ""Nonce"" = @nonce
                LIMIT     1";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { hash, counter, nonce });

            return rows.Select(row =>
            {
                var contract = row.ContractId == null ? null
                    : (RawContract)Accounts.Get((int)row.ContractId);

                var contractMetadata = contract == null ? null
                    : Accounts.GetMetadata(contract.Id);

                return new OriginationOperation
                {
                    Id = row.Id,
                    Level = row.Level,
                    Timestamp = row.Timestamp,
                    Hash = hash,
                    Initiator = row.InitiatorId != null ? Accounts.GetAlias(row.InitiatorId) : null,
                    Sender = Accounts.GetAlias(row.SenderId),
                    Counter = counter,
                    Nonce = nonce,
                    GasLimit = row.GasLimit,
                    GasUsed = row.GasUsed,
                    StorageLimit = row.StorageLimit,
                    StorageUsed = row.StorageUsed,
                    BakerFee = row.BakerFee,
                    StorageFee = row.StorageFee ?? 0,
                    AllocationFee = row.AllocationFee ?? 0,
                    ContractDelegate = row.DelegateId != null ? Accounts.GetAlias(row.DelegateId) : null,
                    ContractBalance = row.Balance,
                    Status = StatusToString(row.Status),
                    OriginatedContract = contract == null ? null :
                        new OriginatedContract
                        {
                            Alias = contractMetadata?.Alias,
                            Address = contract.Address,
                            Kind = contract.KindString
                        },
                    ContractManager = row.ManagerId != null ? Accounts.GetAlias(row.ManagerId) : null,
                    Errors = row.Errors != null ? OperationErrorSerializer.Deserialize(row.Errors) : null
                };
            });
        }

        public async Task<IEnumerable<OriginationOperation>> GetOriginations(int level)
        {
            var sql = @"
                SELECT    ""Id"", ""Timestamp"", ""OpHash"", ""SenderId"", ""InitiatorId"", ""Counter"", ""BakerFee"", ""StorageFee"", ""AllocationFee"", 
                          ""GasLimit"", ""GasUsed"", ""StorageLimit"", ""StorageUsed"", ""Status"", ""Nonce"", ""ContractId"", ""DelegateId"", ""Balance"", ""ManagerId"", ""Errors""
                FROM      ""OriginationOps""
                WHERE     ""Level"" = @level
                ORDER BY  ""Id""";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { level });

            return rows.Select(row =>
            {
                var contract = row.ContractId == null ? null
                    : (RawContract)Accounts.Get((int)row.ContractId);

                var contractMetadata = contract == null ? null
                    : Accounts.GetMetadata(contract.Id);

                return new OriginationOperation
                {
                    Id = row.Id,
                    Level = level,
                    Timestamp = row.Timestamp,
                    Hash = row.OpHash,
                    Initiator = row.InitiatorId != null ? Accounts.GetAlias(row.InitiatorId) : null,
                    Sender = Accounts.GetAlias(row.SenderId),
                    Counter = row.Counter,
                    Nonce = row.Nonce,
                    GasLimit = row.GasLimit,
                    GasUsed = row.GasUsed,
                    StorageLimit = row.StorageLimit,
                    StorageUsed = row.StorageUsed,
                    BakerFee = row.BakerFee,
                    StorageFee = row.StorageFee ?? 0,
                    AllocationFee = row.AllocationFee ?? 0,
                    ContractDelegate = row.DelegateId != null ? Accounts.GetAlias(row.DelegateId) : null,
                    ContractBalance = row.Balance,
                    Status = StatusToString(row.Status),
                    OriginatedContract = contract == null ? null :
                        new OriginatedContract
                        {
                            Alias = contractMetadata?.Alias,
                            Address = contract.Address,
                            Kind = contract.KindString
                        },
                    ContractManager = row.ManagerId != null ? Accounts.GetAlias(row.ManagerId) : null,
                    Errors = row.Errors != null ? OperationErrorSerializer.Deserialize(row.Errors) : null
                };
            });
        }

        public async Task<IEnumerable<OriginationOperation>> GetOriginations(int limit = 100, int offset = 0)
        {
            var sql = @"
                SELECT    ""Id"", ""Level"", ""Timestamp"", ""OpHash"", ""SenderId"", ""InitiatorId"", ""Counter"", ""BakerFee"", ""StorageFee"", ""AllocationFee"", 
                          ""GasLimit"", ""GasUsed"", ""StorageLimit"", ""StorageUsed"", ""Status"", ""Nonce"", ""ContractId"", ""DelegateId"", ""Balance"", ""ManagerId"", ""Errors""
                FROM      ""OriginationOps""
                ORDER BY  ""Id""
                OFFSET    @offset
                LIMIT     @limit";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { limit, offset });

            return rows.Select(row =>
            {
                var contract = row.ContractId == null ? null
                    : (RawContract)Accounts.Get((int)row.ContractId);

                var contractMetadata = contract == null ? null
                    : Accounts.GetMetadata(contract.Id);

                return new OriginationOperation
                {
                    Id = row.Id,
                    Level = row.Level,
                    Timestamp = row.Timestamp,
                    Hash = row.OpHash,
                    Initiator = row.InitiatorId != null ? Accounts.GetAlias(row.InitiatorId) : null,
                    Sender = Accounts.GetAlias(row.SenderId),
                    Counter = row.Counter,
                    Nonce = row.Nonce,
                    GasLimit = row.GasLimit,
                    GasUsed = row.GasUsed,
                    StorageLimit = row.StorageLimit,
                    StorageUsed = row.StorageUsed,
                    BakerFee = row.BakerFee,
                    StorageFee = row.StorageFee ?? 0,
                    AllocationFee = row.AllocationFee ?? 0,
                    ContractDelegate = row.DelegateId != null ? Accounts.GetAlias(row.DelegateId) : null,
                    ContractBalance = row.Balance,
                    Status = StatusToString(row.Status),
                    OriginatedContract = contract == null ? null :
                        new OriginatedContract
                        {
                            Alias = contractMetadata?.Alias,
                            Address = contract.Address,
                            Kind = contract.KindString
                        },
                    ContractManager = row.ManagerId != null ? Accounts.GetAlias(row.ManagerId) : null,
                    Errors = row.Errors != null ? OperationErrorSerializer.Deserialize(row.Errors) : null
                };
            });
        }

        public async Task<IEnumerable<OriginationOperation>> GetOriginations(RawAccount account, SortMode sort, int offset, OffsetMode offsetMode, int limit)
        {
            var sql = $@"
                SELECT    ""Id"", ""Level"", ""Timestamp"", ""OpHash"", ""SenderId"", ""InitiatorId"", ""Counter"", ""BakerFee"", ""StorageFee"", ""AllocationFee"", 
                          ""GasLimit"", ""GasUsed"", ""StorageLimit"", ""StorageUsed"", ""Status"", ""Nonce"", ""ContractId"", ""DelegateId"", ""Balance"", ""ManagerId"", ""Errors""
                FROM      ""OriginationOps""
                WHERE     (""SenderId"" = @accountId
                OR        ""ManagerId"" = @accountId
                OR        ""DelegateId"" = @accountId
                OR        ""ContractId"" = @accountId
                OR        ""InitiatorId"" = @accountId)
                {Pagination(sort, offset, offsetMode, limit)}";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { accountId = account.Id });

            return rows.Select(row =>
            {
                var contract = row.ContractId == null ? null
                    : (RawContract)Accounts.Get((int)row.ContractId);

                var contractMetadata = contract == null ? null
                    : Accounts.GetMetadata(contract.Id);

                return new OriginationOperation
                {
                    Id = row.Id,
                    Level = row.Level,
                    Timestamp = row.Timestamp,
                    Hash = row.OpHash,
                    Initiator = row.InitiatorId != null ? Accounts.GetAlias(row.InitiatorId) : null,
                    Sender = Accounts.GetAlias(row.SenderId),
                    Counter = row.Counter,
                    Nonce = row.Nonce,
                    GasLimit = row.GasLimit,
                    GasUsed = row.GasUsed,
                    StorageLimit = row.StorageLimit,
                    StorageUsed = row.StorageUsed,
                    BakerFee = row.BakerFee,
                    StorageFee = row.StorageFee ?? 0,
                    AllocationFee = row.AllocationFee ?? 0,
                    ContractDelegate = row.DelegateId != null ? Accounts.GetAlias(row.DelegateId) : null,
                    ContractBalance = row.Balance,
                    Status = StatusToString(row.Status),
                    OriginatedContract = contract == null ? null :
                        new OriginatedContract
                        {
                            Alias = contractMetadata?.Alias,
                            Address = contract.Address,
                            Kind = contract.KindString
                        },
                    ContractManager = row.ManagerId != null ? Accounts.GetAlias(row.ManagerId) : null,
                    Errors = row.Errors != null ? OperationErrorSerializer.Deserialize(row.Errors) : null
                };
            });
        }

        public async Task<IEnumerable<OriginationOperation>> GetOriginations(RawAccount account, DateTime from, DateTime to, SortMode sort, int offset, OffsetMode offsetMode, int limit)
        {
            var sql = $@"
                SELECT    ""Id"", ""Level"", ""Timestamp"", ""OpHash"", ""SenderId"", ""InitiatorId"", ""Counter"", ""BakerFee"", ""StorageFee"", ""AllocationFee"", 
                          ""GasLimit"", ""GasUsed"", ""StorageLimit"", ""StorageUsed"", ""Status"", ""Nonce"", ""ContractId"", ""DelegateId"", ""Balance"", ""ManagerId"", ""Errors""
                FROM      ""OriginationOps""
                WHERE     (""SenderId"" = @accountId
                OR        ""ManagerId"" = @accountId
                OR        ""DelegateId"" = @accountId
                OR        ""ContractId"" = @accountId
                OR        ""InitiatorId"" = @accountId)
                AND       ""Timestamp"" >= @from
                AND       ""Timestamp"" < @to
                {Pagination(sort, offset, offsetMode, limit)}";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { accountId = account.Id, from, to });

            return rows.Select(row =>
            {
                var contract = row.ContractId == null ? null
                    : (RawContract)Accounts.Get((int)row.ContractId);

                var contractMetadata = contract == null ? null
                    : Accounts.GetMetadata(contract.Id);

                return new OriginationOperation
                {
                    Id = row.Id,
                    Level = row.Level,
                    Timestamp = row.Timestamp,
                    Hash = row.OpHash,
                    Initiator = row.InitiatorId != null ? Accounts.GetAlias(row.InitiatorId) : null,
                    Sender = Accounts.GetAlias(row.SenderId),
                    Counter = row.Counter,
                    Nonce = row.Nonce,
                    GasLimit = row.GasLimit,
                    GasUsed = row.GasUsed,
                    StorageLimit = row.StorageLimit,
                    StorageUsed = row.StorageUsed,
                    BakerFee = row.BakerFee,
                    StorageFee = row.StorageFee ?? 0,
                    AllocationFee = row.AllocationFee ?? 0,
                    ContractDelegate = row.DelegateId != null ? Accounts.GetAlias(row.DelegateId) : null,
                    ContractBalance = row.Balance,
                    Status = StatusToString(row.Status),
                    OriginatedContract = contract == null ? null :
                        new OriginatedContract
                        {
                            Alias = contractMetadata?.Alias,
                            Address = contract.Address,
                            Kind = contract.KindString
                        },
                    ContractManager = row.ManagerId != null ? Accounts.GetAlias(row.ManagerId) : null,
                    Errors = row.Errors != null ? OperationErrorSerializer.Deserialize(row.Errors) : null
                };
            });
        }
        #endregion

        #region transactions
        public async Task<int> GetTransactionsCount()
        {
            var sql = @"
                SELECT   COUNT(*)
                FROM     ""TransactionOps""";

            using var db = GetConnection();
            return await db.QueryFirstAsync<int>(sql);
        }

        public async Task<IEnumerable<TransactionOperation>> GetTransactions(string hash)
        {
            var sql = @"
                SELECT    ""Id"", ""Level"", ""Timestamp"", ""SenderId"", ""InitiatorId"", ""Counter"", ""BakerFee"", ""StorageFee"", ""AllocationFee"", ""Parameters"",
                          ""GasLimit"", ""GasUsed"", ""StorageLimit"", ""StorageUsed"", ""Status"", ""Nonce"", ""TargetId"", ""Amount"", ""InternalOperations"", ""Errors""
                FROM      ""TransactionOps""
                WHERE     ""OpHash"" = @hash::character(51)
                ORDER BY  ""Id""";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { hash });

            return rows.Select(row => new TransactionOperation
            {
                Id = row.Id,
                Level = row.Level,
                Timestamp = row.Timestamp,
                Hash = hash,
                Initiator = row.InitiatorId != null ? Accounts.GetAlias(row.InitiatorId) : null,
                Sender = Accounts.GetAlias(row.SenderId),
                Counter = row.Counter,
                Nonce = row.Nonce,
                GasLimit = row.GasLimit,
                GasUsed = row.GasUsed,
                StorageLimit = row.StorageLimit,
                StorageUsed = row.StorageUsed,
                BakerFee = row.BakerFee,
                StorageFee = row.StorageFee ?? 0,
                AllocationFee = row.AllocationFee ?? 0,
                Target = row.TargetId != null ? Accounts.GetAlias(row.TargetId) : null,
                Amount = row.Amount,
                Parameters = row.Parameters,
                Status = StatusToString(row.Status),
                Errors = row.Errors != null ? OperationErrorSerializer.Deserialize(row.Errors) : null,
                HasInternals = row.InternalOperations > 0
            });
        }

        public async Task<IEnumerable<TransactionOperation>> GetTransactions(string hash, int counter)
        {
            var sql = @"
                SELECT    ""Id"", ""Level"", ""Timestamp"", ""SenderId"", ""InitiatorId"", ""BakerFee"", ""StorageFee"", ""AllocationFee"", ""Parameters"",
                          ""GasLimit"", ""GasUsed"", ""StorageLimit"", ""StorageUsed"", ""Status"", ""Nonce"", ""TargetId"", ""Amount"", ""InternalOperations"", ""Errors""
                FROM      ""TransactionOps""
                WHERE     ""OpHash"" = @hash::character(51) AND ""Counter"" = @counter
                ORDER BY  ""Id""";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { hash, counter });

            return rows.Select(row => new TransactionOperation
            {
                Id = row.Id,
                Level = row.Level,
                Timestamp = row.Timestamp,
                Hash = hash,
                Initiator = row.InitiatorId != null ? Accounts.GetAlias(row.InitiatorId) : null,
                Sender = Accounts.GetAlias(row.SenderId),
                Counter = counter,
                Nonce = row.Nonce,
                GasLimit = row.GasLimit,
                GasUsed = row.GasUsed,
                StorageLimit = row.StorageLimit,
                StorageUsed = row.StorageUsed,
                BakerFee = row.BakerFee,
                StorageFee = row.StorageFee ?? 0,
                AllocationFee = row.AllocationFee ?? 0,
                Target = row.TargetId != null ? Accounts.GetAlias(row.TargetId) : null,
                Amount = row.Amount,
                Parameters = row.Parameters,
                Status = StatusToString(row.Status),
                Errors = row.Errors != null ? OperationErrorSerializer.Deserialize(row.Errors) : null,
                HasInternals = row.InternalOperations > 0
            });
        }

        public async Task<IEnumerable<TransactionOperation>> GetTransactions(string hash, int counter, int nonce)
        {
            var sql = @"
                SELECT    ""Id"", ""Level"", ""Timestamp"", ""SenderId"", ""InitiatorId"", ""BakerFee"", ""StorageFee"", ""AllocationFee"", ""Parameters"",
                          ""GasLimit"", ""GasUsed"", ""StorageLimit"", ""StorageUsed"", ""Status"", ""TargetId"", ""Amount"", ""InternalOperations"", ""Errors""
                FROM      ""TransactionOps""
                WHERE     ""OpHash"" = @hash::character(51) AND ""Counter"" = @counter AND ""Nonce"" = @nonce
                LIMIT     1";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { hash, counter, nonce });

            return rows.Select(row => new TransactionOperation
            {
                Id = row.Id,
                Level = row.Level,
                Timestamp = row.Timestamp,
                Hash = hash,
                Initiator = row.InitiatorId != null ? Accounts.GetAlias(row.InitiatorId) : null,
                Sender = Accounts.GetAlias(row.SenderId),
                Counter = counter,
                Nonce = nonce,
                GasLimit = row.GasLimit,
                GasUsed = row.GasUsed,
                StorageLimit = row.StorageLimit,
                StorageUsed = row.StorageUsed,
                BakerFee = row.BakerFee,
                StorageFee = row.StorageFee ?? 0,
                AllocationFee = row.AllocationFee ?? 0,
                Target = row.TargetId != null ? Accounts.GetAlias(row.TargetId) : null,
                Amount = row.Amount,
                Parameters = row.Parameters,
                Status = StatusToString(row.Status),
                Errors = row.Errors != null ? OperationErrorSerializer.Deserialize(row.Errors) : null,
                HasInternals = row.InternalOperations > 0
            });
        }

        public async Task<IEnumerable<TransactionOperation>> GetTransactions(int level)
        {
            var sql = @"
                SELECT    ""Id"", ""Timestamp"", ""OpHash"", ""SenderId"", ""InitiatorId"", ""Counter"", ""BakerFee"", ""StorageFee"", ""AllocationFee"", ""Parameters"",
                          ""GasLimit"", ""GasUsed"", ""StorageLimit"", ""StorageUsed"", ""Status"", ""Nonce"", ""TargetId"", ""Amount"", ""InternalOperations"", ""Errors""
                FROM      ""TransactionOps""
                WHERE     ""Level"" = @level
                ORDER BY  ""Id""";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { level });

            return rows.Select(row => new TransactionOperation
            {
                Id = row.Id,
                Level = level,
                Timestamp = row.Timestamp,
                Hash = row.OpHash,
                Initiator = row.InitiatorId != null ? Accounts.GetAlias(row.InitiatorId) : null,
                Sender = Accounts.GetAlias(row.SenderId),
                Counter = row.Counter,
                Nonce = row.Nonce,
                GasLimit = row.GasLimit,
                GasUsed = row.GasUsed,
                StorageLimit = row.StorageLimit,
                StorageUsed = row.StorageUsed,
                BakerFee = row.BakerFee,
                StorageFee = row.StorageFee ?? 0,
                AllocationFee = row.AllocationFee ?? 0,
                Target = row.TargetId != null ? Accounts.GetAlias(row.TargetId) : null,
                Amount = row.Amount,
                Parameters = row.Parameters,
                Status = StatusToString(row.Status),
                Errors = row.Errors != null ? OperationErrorSerializer.Deserialize(row.Errors) : null,
                HasInternals = row.InternalOperations > 0
            });
        }

        public async Task<IEnumerable<TransactionOperation>> GetTransactions(
            AccountParameter initiator,
            AccountParameter sender,
            AccountParameter target,
            StringParameter parameters,
            SortParameter sort,
            int limit, int offset)
        {
            var sqlParams = new DynamicParameters();
            var sqlFilters = "";

            #region initiator
            if (initiator != null)
            {
                switch(initiator.Mode)
                {
                    case QueryMode.Exact:
                        sqlFilters += $@"AND ""InitiatorId"" = {initiator.Value} ";
                        break;
                    case QueryMode.Any:
                        sqlFilters += @"AND ""InitiatorId"" = ANY (@initiatorIds) ";
                        sqlParams.Add("initiatorIds", initiator.Values);
                        break;
                    case QueryMode.Null:
                        sqlFilters += @"AND ""InitiatorId"" IS NULL ";
                        break;
                    case QueryMode.Column:
                        sqlFilters += @"AND ""InitiatorId"" = ""TargetId"" ";
                        break;
                    default:
                        throw new Exception("Unsupported account filter mode");
                }
            }
            #endregion

            #region sender
            if (sender != null)
            {
                switch (sender.Mode)
                {
                    case QueryMode.Exact:
                        sqlFilters += $@"AND ""SenderId"" = {sender.Value} ";
                        break;
                    case QueryMode.Any:
                        sqlFilters += @"AND ""SenderId"" = ANY (@senderIds) ";
                        sqlParams.Add("senderIds", sender.Values);
                        break;
                    case QueryMode.Null:
                        sqlFilters += @"AND ""SenderId"" IS NULL ";
                        break;
                    case QueryMode.Column:
                        sqlFilters += @"AND ""SenderId"" = ""TargetId"" ";
                        break;
                    default:
                        throw new Exception("Unsupported account filter mode");
                }
            }
            #endregion

            #region target
            if (target != null)
            {
                switch (target.Mode)
                {
                    case QueryMode.Exact:
                        sqlFilters += $@"AND ""TargetId"" = {target.Value} ";
                        break;
                    case QueryMode.Any:
                        sqlFilters += @"AND ""TargetId"" = ANY (@targetIds) ";
                        sqlParams.Add("targetIds", target.Values);
                        break;
                    case QueryMode.Null:
                        sqlFilters += @"AND ""TargetId"" IS NULL ";
                        break;
                    case QueryMode.Column:
                        sqlFilters += target.Column == "sender"
                            ? @"AND ""TargetId"" = ""SenderId"" "
                            : @"AND ""TargetId"" = ""InitiatorId"" ";
                        break;
                    default:
                        throw new Exception("Unsupported account filter mode");
                }
            }
            #endregion

            #region parameters
            if (parameters != null)
            {
                switch (parameters.Mode)
                {
                    case QueryMode.Exact:
                        sqlFilters += $@"AND ""Parameters"" = @parameters ";
                        sqlParams.Add("parameters", parameters.Value);
                        break;
                    case QueryMode.Like:
                        sqlFilters += @"AND ""Parameters"" LIKE @parameters ";
                        sqlParams.Add("parameters", parameters.Value);
                        break;
                    case QueryMode.Null:
                        sqlFilters += @"AND ""Parameters"" IS NULL ";
                        break;
                    default:
                        throw new Exception("Unsupported account filter mode");
                }
            }
            #endregion

            if (sqlFilters.Length > 0)
                sqlFilters = "WHERE" + sqlFilters[3..];

            #region sort
            var sqlSort = @"""Id""";
            if (sort != null)
            {
                sqlSort = sort.Value switch
                {
                    "id" => sort.Desc ? @"""Id"" DESC" : @"""Id""",
                    "level" => sort.Desc ? @"""Id"" DESC" : @"""Id""",
                    "timestamp" => sort.Desc ? @"""Id"" DESC" : @"""Id""",
                    "amount" => sort.Desc ? @"""Amount"" DESC" : @"""Amount""",
                    _ => throw new Exception("Unsupported sorting column"),
                };
            }
            #endregion

            var sql = $@"
                SELECT    ""Id"", ""Level"", ""Timestamp"", ""OpHash"", ""SenderId"", ""InitiatorId"", ""Counter"", ""BakerFee"", ""StorageFee"", ""AllocationFee"", ""Parameters"",
                          ""GasLimit"", ""GasUsed"", ""StorageLimit"", ""StorageUsed"", ""Status"", ""Nonce"", ""TargetId"", ""Amount"", ""InternalOperations"", ""Errors""
                FROM      ""TransactionOps""
                {sqlFilters}
                ORDER BY  {sqlSort}
                OFFSET    @offset
                LIMIT     @limit";

            sqlParams.Add("offset", offset);
            sqlParams.Add("limit", limit);

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, sqlParams);

            return rows.Select(row => new TransactionOperation
            {
                Id = row.Id,
                Level = row.Level,
                Timestamp = row.Timestamp,
                Hash = row.OpHash,
                Initiator = row.InitiatorId != null ? Accounts.GetAlias(row.InitiatorId) : null,
                Sender = Accounts.GetAlias(row.SenderId),
                Counter = row.Counter,
                Nonce = row.Nonce,
                GasLimit = row.GasLimit,
                GasUsed = row.GasUsed,
                StorageLimit = row.StorageLimit,
                StorageUsed = row.StorageUsed,
                BakerFee = row.BakerFee,
                StorageFee = row.StorageFee ?? 0,
                AllocationFee = row.AllocationFee ?? 0,
                Target = row.TargetId != null ? Accounts.GetAlias(row.TargetId) : null,
                Amount = row.Amount,
                Parameters = row.Parameters,
                Status = StatusToString(row.Status),
                Errors = row.Errors != null ? OperationErrorSerializer.Deserialize(row.Errors) : null,
                HasInternals = row.InternalOperations > 0
            });
        }

        public async Task<IEnumerable<TransactionOperation>> GetTransactions(RawAccount account, SortMode sort, int offset, OffsetMode offsetMode, int limit)
        {
            var sql = $@"
                SELECT    ""Id"", ""Level"", ""Timestamp"", ""OpHash"", ""SenderId"", ""InitiatorId"", ""Counter"", ""BakerFee"", ""StorageFee"", ""AllocationFee"", ""Parameters"",
                          ""GasLimit"", ""GasUsed"", ""StorageLimit"", ""StorageUsed"", ""Status"", ""Nonce"", ""TargetId"", ""Amount"", ""InternalOperations"", ""Errors""
                FROM      ""TransactionOps""
                WHERE     (""SenderId"" = @accountId
                OR        ""TargetId"" = @accountId
                OR        ""InitiatorId"" = @accountId)
                {Pagination(sort, offset, offsetMode, limit)}";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { accountId = account.Id });

            return rows.Select(row => new TransactionOperation
            {
                Id = row.Id,
                Level = row.Level,
                Timestamp = row.Timestamp,
                Hash = row.OpHash,
                Initiator = row.InitiatorId != null ? Accounts.GetAlias(row.InitiatorId) : null,
                Sender = Accounts.GetAlias(row.SenderId),
                Counter = row.Counter,
                Nonce = row.Nonce,
                GasLimit = row.GasLimit,
                GasUsed = row.GasUsed,
                StorageLimit = row.StorageLimit,
                StorageUsed = row.StorageUsed,
                BakerFee = row.BakerFee,
                StorageFee = row.StorageFee ?? 0,
                AllocationFee = row.AllocationFee ?? 0,
                Target = row.TargetId != null ? Accounts.GetAlias(row.TargetId) : null,
                Amount = row.Amount,
                Parameters = row.Parameters,
                Status = StatusToString(row.Status),
                Errors = row.Errors != null ? OperationErrorSerializer.Deserialize(row.Errors) : null,
                HasInternals = row.InternalOperations > 0
            });
        }

        public async Task<IEnumerable<TransactionOperation>> GetTransactions(RawAccount account, DateTime from, DateTime to, SortMode sort, int offset, OffsetMode offsetMode, int limit)
        {
            var sql = $@"
                SELECT    ""Id"", ""Level"", ""Timestamp"", ""OpHash"", ""SenderId"", ""InitiatorId"", ""Counter"", ""BakerFee"", ""StorageFee"", ""AllocationFee"", ""Parameters"",
                          ""GasLimit"", ""GasUsed"", ""StorageLimit"", ""StorageUsed"", ""Status"", ""Nonce"", ""TargetId"", ""Amount"", ""InternalOperations"", ""Errors""
                FROM      ""TransactionOps""
                WHERE     (""SenderId"" = @accountId
                OR        ""TargetId"" = @accountId
                OR        ""InitiatorId"" = @accountId)
                AND       ""Timestamp"" >= @from
                AND       ""Timestamp"" < @to
                {Pagination(sort, offset, offsetMode, limit)}";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { accountId = account.Id, from, to });

            return rows.Select(row => new TransactionOperation
            {
                Id = row.Id,
                Level = row.Level,
                Timestamp = row.Timestamp,
                Hash = row.OpHash,
                Initiator = row.InitiatorId != null ? Accounts.GetAlias(row.InitiatorId) : null,
                Sender = Accounts.GetAlias(row.SenderId),
                Counter = row.Counter,
                Nonce = row.Nonce,
                GasLimit = row.GasLimit,
                GasUsed = row.GasUsed,
                StorageLimit = row.StorageLimit,
                StorageUsed = row.StorageUsed,
                BakerFee = row.BakerFee,
                StorageFee = row.StorageFee ?? 0,
                AllocationFee = row.AllocationFee ?? 0,
                Target = row.TargetId != null ? Accounts.GetAlias(row.TargetId) : null,
                Amount = row.Amount,
                Parameters = row.Parameters,
                Status = StatusToString(row.Status),
                Errors = row.Errors != null ? OperationErrorSerializer.Deserialize(row.Errors) : null,
                HasInternals = row.InternalOperations > 0
            });
        }
        #endregion

        #region reveals
        public async Task<int> GetRevealsCount()
        {
            var sql = @"
                SELECT   COUNT(*)
                FROM     ""RevealOps""";

            using var db = GetConnection();
            return await db.QueryFirstAsync<int>(sql);
        }

        public async Task<IEnumerable<RevealOperation>> GetReveals(string hash)
        {
            var sql = @"
                SELECT    ""Id"", ""Level"", ""Timestamp"", ""SenderId"", ""Counter"", ""BakerFee"", ""GasLimit"", ""GasUsed"", ""Status"", ""Errors""
                FROM      ""RevealOps""
                WHERE     ""OpHash"" = @hash::character(51)
                ORDER BY  ""Id""";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { hash });

            return rows.Select(row => new RevealOperation
            {
                Id = row.Id,
                Level = row.Level,
                Timestamp = row.Timestamp,
                Hash = hash,
                Sender = Accounts.GetAlias(row.SenderId),
                Counter = row.Counter,
                GasLimit = row.GasLimit,
                GasUsed = row.GasUsed,
                BakerFee = row.BakerFee,
                Status = StatusToString(row.Status),
                Errors = row.Errors != null ? OperationErrorSerializer.Deserialize(row.Errors) : null
            });
        }

        public async Task<IEnumerable<RevealOperation>> GetReveals(string hash, int counter)
        {
            var sql = @"
                SELECT  ""Id"", ""Level"", ""Timestamp"", ""SenderId"", ""BakerFee"", ""GasLimit"", ""GasUsed"", ""Status"", ""Errors""
                FROM    ""RevealOps""
                WHERE   ""OpHash"" = @hash::character(51) AND ""Counter"" = @counter
                LIMIT   1";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { hash, counter });

            return rows.Select(row => new RevealOperation
            {
                Id = row.Id,
                Level = row.Level,
                Timestamp = row.Timestamp,
                Hash = hash,
                Sender = Accounts.GetAlias(row.SenderId),
                Counter = counter,
                GasLimit = row.GasLimit,
                GasUsed = row.GasUsed,
                BakerFee = row.BakerFee,
                Status = StatusToString(row.Status),
                Errors = row.Errors != null ? OperationErrorSerializer.Deserialize(row.Errors) : null
            });
        }

        public async Task<IEnumerable<RevealOperation>> GetReveals(int level)
        {
            var sql = @"
                SELECT    ""Id"", ""Timestamp"", ""OpHash"", ""SenderId"", ""Counter"", ""BakerFee"", ""GasLimit"", ""GasUsed"", ""Status"", ""Errors""
                FROM      ""RevealOps""
                WHERE     ""Level"" = @level
                ORDER BY  ""Id""";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { level });

            return rows.Select(row => new RevealOperation
            {
                Id = row.Id,
                Level = level,
                Timestamp = row.Timestamp,
                Hash = row.OpHash,
                Sender = Accounts.GetAlias(row.SenderId),
                Counter = row.Counter,
                GasLimit = row.GasLimit,
                GasUsed = row.GasUsed,
                BakerFee = row.BakerFee,
                Status = StatusToString(row.Status),
                Errors = row.Errors != null ? OperationErrorSerializer.Deserialize(row.Errors) : null
            });
        }

        public async Task<IEnumerable<RevealOperation>> GetReveals(int limit = 100, int offset = 0)
        {
            var sql = @"
                SELECT    ""Id"", ""Level"", ""Timestamp"", ""OpHash"", ""SenderId"", ""Counter"", ""BakerFee"", ""GasLimit"", ""GasUsed"", ""Status"", ""Errors""
                FROM      ""RevealOps""
                ORDER BY  ""Id""
                OFFSET    @offset
                LIMIT     @limit";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { limit, offset });

            return rows.Select(row => new RevealOperation
            {
                Id = row.Id,
                Level = row.Level,
                Timestamp = row.Timestamp,
                Hash = row.OpHash,
                Sender = Accounts.GetAlias(row.SenderId),
                Counter = row.Counter,
                GasLimit = row.GasLimit,
                GasUsed = row.GasUsed,
                BakerFee = row.BakerFee,
                Status = StatusToString(row.Status),
                Errors = row.Errors != null ? OperationErrorSerializer.Deserialize(row.Errors) : null
            });
        }

        public async Task<IEnumerable<RevealOperation>> GetReveals(RawAccount account, SortMode sort, int offset, OffsetMode offsetMode, int limit)
        {
            var sql = $@"
                SELECT    ""Id"", ""Level"", ""Timestamp"", ""OpHash"", ""SenderId"", ""Counter"", ""BakerFee"", ""GasLimit"", ""GasUsed"", ""Status"", ""Errors""
                FROM      ""RevealOps""
                WHERE     ""SenderId"" = @accountId
                {Pagination(sort, offset, offsetMode, limit)}";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { accountId = account.Id });

            return rows.Select(row => new RevealOperation
            {
                Id = row.Id,
                Level = row.Level,
                Timestamp = row.Timestamp,
                Hash = row.OpHash,
                Sender = Accounts.GetAlias(row.SenderId),
                Counter = row.Counter,
                GasLimit = row.GasLimit,
                GasUsed = row.GasUsed,
                BakerFee = row.BakerFee,
                Status = StatusToString(row.Status),
                Errors = row.Errors != null ? OperationErrorSerializer.Deserialize(row.Errors) : null
            });
        }

        public async Task<IEnumerable<RevealOperation>> GetReveals(RawAccount account, DateTime from, DateTime to, SortMode sort, int offset, OffsetMode offsetMode, int limit)
        {
            var sql = $@"
                SELECT    ""Id"", ""Level"", ""Timestamp"", ""OpHash"", ""SenderId"", ""Counter"", ""BakerFee"", ""GasLimit"", ""GasUsed"", ""Status"", ""Errors""
                FROM      ""RevealOps""
                WHERE     ""SenderId"" = @accountId
                AND       ""Timestamp"" >= @from
                AND       ""Timestamp"" < @to
                {Pagination(sort, offset, offsetMode, limit)}";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { accountId = account.Id, from, to });

            return rows.Select(row => new RevealOperation
            {
                Id = row.Id,
                Level = row.Level,
                Timestamp = row.Timestamp,
                Hash = row.OpHash,
                Sender = Accounts.GetAlias(row.SenderId),
                Counter = row.Counter,
                GasLimit = row.GasLimit,
                GasUsed = row.GasUsed,
                BakerFee = row.BakerFee,
                Status = StatusToString(row.Status),
                Errors = row.Errors != null ? OperationErrorSerializer.Deserialize(row.Errors) : null
            });
        }
        #endregion

        #region migrations
        public async Task<int> GetMigrationsCount()
        {
            var sql = @"
                SELECT   COUNT(*)
                FROM     ""MigrationOps""";

            using var db = GetConnection();
            return await db.QueryFirstAsync<int>(sql);
        }

        public async Task<IEnumerable<MigrationOperation>> GetMigrations(int limit = 100, int offset = 0)
        {
            var sql = @"
                SELECT    ""Id"", ""Level"", ""Timestamp"", ""AccountId"", ""Kind"", ""BalanceChange""
                FROM      ""MigrationOps""
                ORDER BY  ""Id""
                OFFSET    @offset
                LIMIT     @limit";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { limit, offset });

            return rows.Select(row => new MigrationOperation
            {
                Id = row.Id,
                Level = row.Level,
                Timestamp = row.Timestamp,
                Account = Accounts.GetAlias(row.AccountId),
                Kind = MigrationKindToString(row.Kind),
                BalanceChange = row.BalanceChange
            });
        }

        public async Task<IEnumerable<MigrationOperation>> GetMigrations(RawAccount account, SortMode sort, int offset, OffsetMode offsetMode, int limit)
        {
            var sql = $@"
                SELECT    ""Id"", ""Level"", ""Timestamp"", ""Kind"", ""BalanceChange""
                FROM      ""MigrationOps""
                WHERE     ""AccountId"" = @accountId
                {Pagination(sort, offset, offsetMode, limit)}";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { accountId = account.Id });

            return rows.Select(row => new MigrationOperation
            {
                Id = row.Id,
                Level = row.Level,
                Timestamp = row.Timestamp,
                Account = Accounts.GetAlias(account.Id),
                Kind = MigrationKindToString(row.Kind),
                BalanceChange = row.BalanceChange
            });
        }

        public async Task<IEnumerable<MigrationOperation>> GetMigrations(RawAccount account, DateTime from, DateTime to, SortMode sort, int offset, OffsetMode offsetMode, int limit)
        {
            var sql = $@"
                SELECT    ""Id"", ""Level"", ""Timestamp"", ""Kind"", ""BalanceChange""
                FROM      ""MigrationOps""
                WHERE     ""AccountId"" = @accountId
                AND       ""Timestamp"" >= @from
                AND       ""Timestamp"" < @to
                {Pagination(sort, offset, offsetMode, limit)}";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { accountId = account.Id, from, to });

            return rows.Select(row => new MigrationOperation
            {
                Id = row.Id,
                Level = row.Level,
                Timestamp = row.Timestamp,
                Account = Accounts.GetAlias(account.Id),
                Kind = MigrationKindToString(row.Kind),
                BalanceChange = row.BalanceChange
            });
        }
        #endregion

        #region revelation penalty
        public async Task<int> GetRevelationPenaltiesCount()
        {
            var sql = @"
                SELECT   COUNT(*)
                FROM     ""RevelationPenaltyOps""";

            using var db = GetConnection();
            return await db.QueryFirstAsync<int>(sql);
        }

        public async Task<IEnumerable<RevelationPenaltyOperation>> GetRevelationPenalties(int limit = 100, int offset = 0)
        {
            var sql = @"
                SELECT    *
                FROM      ""RevelationPenaltyOps""
                ORDER BY  ""Id""
                OFFSET    @offset
                LIMIT     @limit";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { limit, offset });

            return rows.Select(row => new RevelationPenaltyOperation
            {
                Id = row.Id,
                Level = row.Level,
                Timestamp = row.Timestamp,
                Baker = Accounts.GetAlias(row.BakerId),
                MissedLevel = row.MissedLevel,
                LostReward = row.LostReward,
                LostFees = row.LostFees
            });
        }

        public async Task<IEnumerable<RevelationPenaltyOperation>> GetRevelationPenalties(RawAccount account, SortMode sort, int offset, OffsetMode offsetMode, int limit)
        {
            var sql = $@"
                SELECT    *
                FROM      ""RevelationPenaltyOps""
                WHERE     ""BakerId"" = @accountId
                {Pagination(sort, offset, offsetMode, limit)}";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { accountId = account.Id });

            return rows.Select(row => new RevelationPenaltyOperation
            {
                Id = row.Id,
                Level = row.Level,
                Timestamp = row.Timestamp,
                Baker = Accounts.GetAlias(row.BakerId),
                MissedLevel = row.MissedLevel,
                LostReward = row.LostReward,
                LostFees = row.LostFees
            });
        }

        public async Task<IEnumerable<RevelationPenaltyOperation>> GetRevelationPenalties(RawAccount account, DateTime from, DateTime to, SortMode sort, int offset, OffsetMode offsetMode, int limit)
        {
            var sql = $@"
                SELECT    *
                FROM      ""RevelationPenaltyOps""
                WHERE     ""BakerId"" = @accountId
                AND       ""Timestamp"" >= @from
                AND       ""Timestamp"" < @to
                {Pagination(sort, offset, offsetMode, limit)}";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { accountId = account.Id, from, to });

            return rows.Select(row => new RevelationPenaltyOperation
            {
                Id = row.Id,
                Level = row.Level,
                Timestamp = row.Timestamp,
                Baker = Accounts.GetAlias(row.BakerId),
                MissedLevel = row.MissedLevel,
                LostReward = row.LostReward,
                LostFees = row.LostFees
            });
        }
        #endregion

        #region baking
        public async Task<int> GetBakingsCount()
        {
            var sql = @"
                SELECT   (""Level"" - 1)
                FROM     ""AppState""";

            using var db = GetConnection();
            return await db.QueryFirstAsync<int>(sql);
        }

        public async Task<IEnumerable<BakingOperation>> GetBakings(int limit = 100, int offset = 0)
        {
            var sql = @"
                SELECT    ""Id"", ""Level"", ""Timestamp"", ""BakerId"", ""Hash"", ""Priority"", ""Reward"", ""Fees""
                FROM      ""Blocks""
                WHERE     ""BakerId"" IS NOT NULL
                ORDER BY  ""Id""
                OFFSET    @offset
                LIMIT     @limit";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { limit, offset });

            return rows.Select(row => new BakingOperation
            {
                Id = row.Id,
                Level = row.Level,
                Timestamp = row.Timestamp,
                Baker = Accounts.GetAlias(row.BakerId),
                Block = row.Hash,
                Priority = row.Priority,
                Reward = row.Reward,
                Fees = row.Fees
            });
        }

        public async Task<IEnumerable<BakingOperation>> GetBakings(RawAccount account, SortMode sort, int offset, OffsetMode offsetMode, int limit)
        {
            var sql = $@"
                SELECT    ""Id"", ""Level"", ""Timestamp"", ""BakerId"", ""Hash"", ""Priority"", ""Reward"", ""Fees""
                FROM      ""Blocks""
                WHERE     ""BakerId"" = @accountId
                {Pagination(sort, offset, offsetMode, limit)}";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { accountId = account.Id });

            return rows.Select(row => new BakingOperation
            {
                Id = row.Id,
                Level = row.Level,
                Timestamp = row.Timestamp,
                Baker = Accounts.GetAlias(row.BakerId),
                Block = row.Hash,
                Priority = row.Priority,
                Reward = row.Reward,
                Fees = row.Fees
            });
        }

        public async Task<IEnumerable<BakingOperation>> GetBakings(RawAccount account, DateTime from, DateTime to, SortMode sort, int offset, OffsetMode offsetMode, int limit)
        {
            var sql = $@"
                SELECT    ""Id"", ""Level"", ""Timestamp"", ""BakerId"", ""Hash"", ""Priority"", ""Reward"", ""Fees""
                FROM      ""Blocks""
                WHERE     ""BakerId"" = @accountId
                AND       ""Timestamp"" >= @from
                AND       ""Timestamp"" < @to
                {Pagination(sort, offset, offsetMode, limit)}";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { accountId = account.Id, from, to });

            return rows.Select(row => new BakingOperation
            {
                Id = row.Id,
                Level = row.Level,
                Timestamp = row.Timestamp,
                Baker = Accounts.GetAlias(row.BakerId),
                Block = row.Hash,
                Priority = row.Priority,
                Reward = row.Reward,
                Fees = row.Fees
            });
        }
        #endregion

        public string Pagination(SortMode sort, int offset, OffsetMode offsetMode, int limit)
        {
            var sortMode = sort == SortMode.Ascending ? "" : "DESC";

            if (offset == 0)
            {
                return $@"
                    ORDER BY ""Id"" {sortMode}
                    LIMIT    {limit}";
            }

            if (offsetMode == OffsetMode.Id)
            {
                return sort == SortMode.Ascending
                    ? $@"
                        AND      ""Id"" > {offset}
                        ORDER BY ""Id"" {sortMode}
                        LIMIT    {limit}"
                    : $@"
                        AND      ""Id"" < {offset}
                        ORDER BY ""Id"" {sortMode}
                        LIMIT    {limit}";
            }

            var offsetValue = offsetMode == OffsetMode.Page ? limit * offset : offset;

            return $@"
                ORDER BY ""Id"" {sortMode}
                OFFSET   {offsetValue}
                LIMIT    {limit}";
        }

        public string Pagination(string alias, SortMode sort, int offset, OffsetMode offsetMode, int limit)
        {
            var sortMode = sort == SortMode.Ascending ? "" : "DESC";

            if (offset == 0)
            {
                return $@"
                    ORDER BY {alias}.""Id"" {sortMode} NULLS LAST
                    LIMIT    {limit}";
            }

            if (offsetMode == OffsetMode.Id)
            {
                return sort == SortMode.Ascending
                    ? $@"
                        AND      {alias}.""Id"" > {offset}
                        ORDER BY {alias}.""Id"" {sortMode} NULLS LAST
                        LIMIT    {limit}"
                    : $@"
                        AND      {alias}.""Id"" < {offset}
                        ORDER BY {alias}.""Id"" {sortMode} NULLS LAST
                        LIMIT    {limit}";
            }

            var offsetValue = offsetMode == OffsetMode.Page ? limit * offset : offset;

            return $@"
                ORDER BY {alias}.""Id"" {sortMode} NULLS LAST
                OFFSET   {offsetValue}
                LIMIT    {limit}";
        }

        string MigrationKindToString(int kind) => kind switch
        {
            0 => "bootstrap",
            1 => "activate_delegate",
            2 => "airdrop",
            3 => "proposal_invoice",
            _ => "unknown"
        };

        string PeriodToString(int period) => period switch
        {
            0 => "proposal",
            1 => "exploration",
            2 => "testing",
            3 => "promotion",
            _ => "unknown"
        };

        string VoteToString(int vote) => vote switch
        {
            0 => "yay",
            1 => "nay",
            2 => "pass",
            _ => "unknown"
        };

        string StatusToString(int status) => status switch
        {
            1 => "applied",
            2 => "backtracked",
            3 => "skipped",
            4 => "failed",
            _ => "unknown"
        };
    }
}
