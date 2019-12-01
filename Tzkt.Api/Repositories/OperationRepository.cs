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

        public async Task<IEnumerable<BallotOperation>> GetLastBallots(RawAccount account, SortMode sort, int offset, OffsetMode offsetMode, int limit)
        {
            var sql = $@"
                SELECT    op.""Id"", op.""Level"", op.""Timestamp"", op.""OpHash"", op.""Vote"", proposal.""Hash"",
                          period.""Code"", period.""Kind"", period.""StartLevel"", period.""EndLevel""
                FROM      ""BallotOps"" as op
                LEFT JOIN ""Proposals"" as proposal ON proposal.""Id"" = op.""ProposalId""
                LEFT JOIN ""VotingPeriods"" as period ON period.""Id"" = op.""PeriodId""
                WHERE     ""SenderId"" = @accountId
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

        public async Task<IEnumerable<ProposalOperation>> GetLastProposals(RawAccount account, SortMode sort, int offset, OffsetMode offsetMode, int limit)
        {
            var sql = $@"
                SELECT    op.""Id"", op.""Level"", op.""Timestamp"", op.""OpHash"", proposal.""Hash"",
                          period.""Code"", period.""Kind"", period.""StartLevel"", period.""EndLevel""
                FROM      ""ProposalOps"" as op
                LEFT JOIN ""Proposals"" as proposal ON proposal.""Id"" = op.""ProposalId""
                LEFT JOIN ""VotingPeriods"" as period ON period.""Id"" = op.""PeriodId""
                WHERE     ""SenderId"" = @accountId
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

        public async Task<IEnumerable<ActivationOperation>> GetLastActivations(RawAccount account, SortMode sort, int offset, OffsetMode offsetMode, int limit)
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

        public async Task<IEnumerable<DoubleBakingOperation>> GetLastDoubleBakings(RawAccount account, SortMode sort, int offset, OffsetMode offsetMode, int limit)
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

        public async Task<IEnumerable<DoubleEndorsingOperation>> GetLastDoubleEndorsings(RawAccount account, SortMode sort, int offset, OffsetMode offsetMode, int limit)
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

        public async Task<IEnumerable<NonceRevelationOperation>> GetLastNonceRevelations(RawAccount account, SortMode sort, int offset, OffsetMode offsetMode, int limit)
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
                SELECT    ""Id"", ""Level"", ""Timestamp"", ""SenderId"", ""Counter"", ""BakerFee"",
                          ""GasLimit"", ""GasUsed"", ""Status"", ""Nonce"", ""DelegateId"", ""Errors""
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
                Sender = Accounts.GetAlias(row.SenderId),
                Counter = row.Counter,
                Nonce = row.Nonce,
                GasLimit = row.GasLimit,
                GasUsed = row.GasUsed,
                BakerFee = row.BakerFee,
                Delegate = row.DelegateId != null ? Accounts.GetAlias(row.DelegateId) : null,
                Status = StatusToString(row.Status),
                Errors = row.Errors != null ? OperationErrorSerializer.Deserialize(row.Errors) : null
            });
        }

        public async Task<IEnumerable<DelegationOperation>> GetDelegations(string hash, int counter)
        {
            var sql = @"
                SELECT    ""Id"", ""Level"", ""Timestamp"", ""SenderId"", ""BakerFee"",
                          ""GasLimit"", ""GasUsed"", ""Status"", ""Nonce"", ""DelegateId"", ""Errors""
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
                Sender = Accounts.GetAlias(row.SenderId),
                Counter = counter,
                Nonce = row.Nonce,
                GasLimit = row.GasLimit,
                GasUsed = row.GasUsed,
                BakerFee = row.BakerFee,
                Delegate = row.DelegateId != null ? Accounts.GetAlias(row.DelegateId) : null,
                Status = StatusToString(row.Status),
                Errors = row.Errors != null ? OperationErrorSerializer.Deserialize(row.Errors) : null
            });
        }

        public async Task<IEnumerable<DelegationOperation>> GetDelegations(string hash, int counter, int nonce)
        {
            var sql = @"
                SELECT    ""Id"", ""Level"", ""Timestamp"", ""SenderId"", ""BakerFee"",
                          ""GasLimit"", ""GasUsed"", ""Status"", ""DelegateId"", ""Errors""
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
                Sender = Accounts.GetAlias(row.SenderId),
                Counter = counter,
                Nonce = nonce,
                GasLimit = row.GasLimit,
                GasUsed = row.GasUsed,
                BakerFee = row.BakerFee,
                Delegate = row.DelegateId != null ? Accounts.GetAlias(row.DelegateId) : null,
                Status = StatusToString(row.Status),
                Errors = row.Errors != null ? OperationErrorSerializer.Deserialize(row.Errors) : null
            });
        }

        public async Task<IEnumerable<DelegationOperation>> GetDelegations(int level)
        {
            var sql = @"
                SELECT    ""Id"", ""Timestamp"", ""OpHash"", ""SenderId"", ""Counter"", ""BakerFee"",
                          ""GasLimit"", ""GasUsed"", ""Status"", ""Nonce"", ""DelegateId"", ""Errors""
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
                Sender = Accounts.GetAlias(row.SenderId),
                Counter = row.Counter,
                Nonce = row.Nonce,
                GasLimit = row.GasLimit,
                GasUsed = row.GasUsed,
                BakerFee = row.BakerFee,
                Delegate = row.DelegateId != null ? Accounts.GetAlias(row.DelegateId) : null,
                Status = StatusToString(row.Status),
                Errors = row.Errors != null ? OperationErrorSerializer.Deserialize(row.Errors) : null
            });
        }

        public async Task<IEnumerable<DelegationOperation>> GetDelegations(int limit = 100, int offset = 0)
        {
            var sql = @"
                SELECT    ""Id"", ""Level"", ""Timestamp"", ""OpHash"", ""SenderId"", ""Counter"", ""BakerFee"",
                          ""GasLimit"", ""GasUsed"", ""Status"", ""Nonce"", ""DelegateId"", ""Errors""
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
                Sender = Accounts.GetAlias(row.SenderId),
                Counter = row.Counter,
                Nonce = row.Nonce,
                GasLimit = row.GasLimit,
                GasUsed = row.GasUsed,
                BakerFee = row.BakerFee,
                Delegate = row.DelegateId != null ? Accounts.GetAlias(row.DelegateId) : null,
                Status = StatusToString(row.Status),
                Errors = row.Errors != null ? OperationErrorSerializer.Deserialize(row.Errors) : null
            });
        }

        public async Task<IEnumerable<DelegationOperation>> GetLastDelegations(RawAccount account, SortMode sort, int offset, OffsetMode offsetMode, int limit)
        {
            var sql = $@"
                SELECT    ""Id"", ""Level"", ""Timestamp"", ""OpHash"", ""SenderId"", ""Counter"", ""BakerFee"",
                          ""GasLimit"", ""GasUsed"", ""Status"", ""Nonce"", ""DelegateId"", ""Errors""
                FROM      ""DelegationOps""
                WHERE     (""SenderId"" = @accountId
                OR        ""DelegateId"" = @accountId)
                {Pagination(sort, offset, offsetMode, limit)}";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { accountId = account.Id });

            return rows.Select(row => new DelegationOperation
            {
                Id = row.Id,
                Level = row.Level,
                Timestamp = row.Timestamp,
                Hash = row.OpHash,
                Sender = Accounts.GetAlias(row.SenderId),
                Counter = row.Counter,
                Nonce = row.Nonce,
                GasLimit = row.GasLimit,
                GasUsed = row.GasUsed,
                BakerFee = row.BakerFee,
                Delegate = row.DelegateId != null ? Accounts.GetAlias(row.DelegateId) : null,
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
                SELECT    ""Id"", ""Level"", ""Timestamp"", ""SenderId"", ""Counter"", ""BakerFee"", ""StorageFee"", ""AllocationFee"", 
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
                SELECT    ""Id"", ""Level"", ""Timestamp"", ""SenderId"", ""BakerFee"", ""StorageFee"", ""AllocationFee"", 
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
                SELECT    ""Id"", ""Level"", ""Timestamp"", ""SenderId"", ""BakerFee"", ""StorageFee"", ""AllocationFee"", 
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
                SELECT    ""Id"", ""Timestamp"", ""OpHash"", ""SenderId"", ""Counter"", ""BakerFee"", ""StorageFee"", ""AllocationFee"", 
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
                SELECT    ""Id"", ""Level"", ""Timestamp"", ""OpHash"", ""SenderId"", ""Counter"", ""BakerFee"", ""StorageFee"", ""AllocationFee"", 
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

        public async Task<IEnumerable<OriginationOperation>> GetLastOriginations(RawAccount account, SortMode sort, int offset, OffsetMode offsetMode, int limit)
        {
            var sql = $@"
                SELECT    ""Id"", ""Level"", ""Timestamp"", ""OpHash"", ""SenderId"", ""Counter"", ""BakerFee"", ""StorageFee"", ""AllocationFee"", 
                          ""GasLimit"", ""GasUsed"", ""StorageLimit"", ""StorageUsed"", ""Status"", ""Nonce"", ""ContractId"", ""DelegateId"", ""Balance"", ""ManagerId"", ""Errors""
                FROM      ""OriginationOps""
                WHERE     (""SenderId"" = @accountId
                OR        ""DelegateId"" = @accountId
                OR        ""ContractId"" = @accountId)
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
                SELECT    ""Id"", ""Level"", ""Timestamp"", ""SenderId"", ""Counter"", ""BakerFee"", ""StorageFee"", ""AllocationFee"",
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
                Status = StatusToString(row.Status),
                Errors = row.Errors != null ? OperationErrorSerializer.Deserialize(row.Errors) : null,
                HasInternals = row.InternalOperations > 0
            });
        }

        public async Task<IEnumerable<TransactionOperation>> GetTransactions(string hash, int counter)
        {
            var sql = @"
                SELECT    ""Id"", ""Level"", ""Timestamp"", ""SenderId"", ""BakerFee"", ""StorageFee"", ""AllocationFee"",
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
                Status = StatusToString(row.Status),
                Errors = row.Errors != null ? OperationErrorSerializer.Deserialize(row.Errors) : null,
                HasInternals = row.InternalOperations > 0
            });
        }

        public async Task<IEnumerable<TransactionOperation>> GetTransactions(string hash, int counter, int nonce)
        {
            var sql = @"
                SELECT    ""Id"", ""Level"", ""Timestamp"", ""SenderId"", ""BakerFee"", ""StorageFee"", ""AllocationFee"",
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
                Status = StatusToString(row.Status),
                Errors = row.Errors != null ? OperationErrorSerializer.Deserialize(row.Errors) : null,
                HasInternals = row.InternalOperations > 0
            });
        }

        public async Task<IEnumerable<TransactionOperation>> GetTransactions(int level)
        {
            var sql = @"
                SELECT    ""Id"", ""Timestamp"", ""OpHash"", ""SenderId"", ""Counter"", ""BakerFee"", ""StorageFee"", ""AllocationFee"",
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
                Status = StatusToString(row.Status),
                Errors = row.Errors != null ? OperationErrorSerializer.Deserialize(row.Errors) : null,
                HasInternals = row.InternalOperations > 0
            });
        }

        public async Task<IEnumerable<TransactionOperation>> GetTransactions(int limit = 100, int offset = 0)
        {
            var sql = @"
                SELECT    ""Id"", ""Level"", ""Timestamp"", ""OpHash"", ""SenderId"", ""Counter"", ""BakerFee"", ""StorageFee"", ""AllocationFee"",
                          ""GasLimit"", ""GasUsed"", ""StorageLimit"", ""StorageUsed"", ""Status"", ""Nonce"", ""TargetId"", ""Amount"", ""InternalOperations"", ""Errors""
                FROM      ""TransactionOps""
                ORDER BY  ""Id""
                OFFSET    @offset
                LIMIT     @limit";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { limit, offset });

            return rows.Select(row => new TransactionOperation
            {
                Id = row.Id,
                Level = row.Level,
                Timestamp = row.Timestamp,
                Hash = row.OpHash,
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
                Status = StatusToString(row.Status),
                Errors = row.Errors != null ? OperationErrorSerializer.Deserialize(row.Errors) : null,
                HasInternals = row.InternalOperations > 0
            });
        }

        public async Task<IEnumerable<TransactionOperation>> GetLastTransactions(RawAccount account, SortMode sort, int offset, OffsetMode offsetMode, int limit)
        {
            var sql = $@"
                SELECT    ""Id"", ""Level"", ""Timestamp"", ""OpHash"", ""SenderId"", ""Counter"", ""BakerFee"", ""StorageFee"", ""AllocationFee"",
                          ""GasLimit"", ""GasUsed"", ""StorageLimit"", ""StorageUsed"", ""Status"", ""Nonce"", ""TargetId"", ""Amount"", ""InternalOperations"", ""Errors""
                FROM      ""TransactionOps""
                WHERE     (""SenderId"" = @accountId
                OR        ""TargetId"" = @accountId)
                {Pagination(sort, offset, offsetMode, limit)}";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { accountId = account.Id });

            return rows.Select(row => new TransactionOperation
            {
                Id = row.Id,
                Level = row.Level,
                Timestamp = row.Timestamp,
                Hash = row.OpHash,
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

        public async Task<IEnumerable<RevealOperation>> GetLastReveals(RawAccount account, SortMode sort, int offset, OffsetMode offsetMode, int limit)
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
        #endregion

        #region system
        public async Task<int> GetSystemOpsCount()
        {
            var sql = @"
                SELECT   COUNT(*)
                FROM     ""SystemOps""";

            using var db = GetConnection();
            return await db.QueryFirstAsync<int>(sql);
        }

        public async Task<IEnumerable<SystemOperation>> GetSystemOps(int limit = 100, int offset = 0)
        {
            var sql = @"
                SELECT    ""Id"", ""Level"", ""Timestamp"", ""AccountId"", ""Event"", ""BalanceChange""
                FROM      ""SystemOps""
                ORDER BY  ""Id""
                OFFSET    @offset
                LIMIT     @limit";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { limit, offset });

            return rows.Select(row => new SystemOperation
            {
                Id = row.Id,
                Level = row.Level,
                Timestamp = row.Timestamp,
                Account = Accounts.GetAlias(row.AccountId),
                Kind = SystemEventToString(row.Event),
                BalanceChange = row.BalanceChange
            });
        }

        public async Task<IEnumerable<SystemOperation>> GetLastSystemOps(RawAccount account, SortMode sort, int offset, OffsetMode offsetMode, int limit)
        {
            var sql = $@"
                SELECT    ""Id"", ""Level"", ""Timestamp"", ""Event"", ""BalanceChange""
                FROM      ""SystemOps""
                WHERE     ""AccountId"" = @accountId
                {Pagination(sort, offset, offsetMode, limit)}";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { accountId = account.Id });

            return rows.Select(row => new SystemOperation
            {
                Id = row.Id,
                Level = row.Level,
                Timestamp = row.Timestamp,
                Account = Accounts.GetAlias(account.Id),
                Kind = SystemEventToString(row.Event),
                BalanceChange = row.BalanceChange
            });
        }
        #endregion

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

        public string Pagination(SortMode sort, int offset, OffsetMode offsetMode, int limit)
        {
            var sortMode = sort == SortMode.Ascending ? "" : "DESC";

            if (offset == 0)
            {
                return $@"
                    ORDER BY ""Id"" {sortMode} NULLS LAST
                    LIMIT    {limit}";
            }

            if (offsetMode == OffsetMode.Id)
            {
                return sort == SortMode.Ascending
                    ? $@"
                        AND      ""Id"" > {offset}
                        ORDER BY ""Id"" {sortMode} NULLS LAST
                        LIMIT    {limit}"
                    : $@"
                        AND      ""Id"" < {offset}
                        ORDER BY ""Id"" {sortMode} NULLS LAST
                        LIMIT    {limit}";
            }

            var offsetValue = offsetMode == OffsetMode.Page ? limit * offset : offset;

            return $@"
                ORDER BY ""Id"" {sortMode} NULLS LAST
                OFFSET   {offsetValue}
                LIMIT    {limit}";
        }

        string SystemEventToString(int sysEvent) => sysEvent switch
        {
            0 => "bootstrap",
            1 => "activate_delegate",
            2 => "airdrop",
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
