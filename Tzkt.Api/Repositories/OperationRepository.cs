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

        public async Task<IEnumerable<EndorsementOperation>> GetEndorsements(SortParameter sort, OffsetParameter offset, int limit)
        {
            var sql = new SqlBuilder(@"SELECT * FROM ""EndorsementOps""")
                .Take(sort, offset, limit, x => "Id");

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

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

        public async Task<IEnumerable<object>> GetEndorsements(SortParameter sort, OffsetParameter offset, int limit, string[] fields)
        {
            var columns = new HashSet<string>(fields.Length);
            foreach (var field in fields)
            {
                switch (field)
                {
                    case "id": columns.Add(@"""Id"""); break;
                    case "level": columns.Add(@"""Level"""); break;
                    case "timestamp": columns.Add(@"""Timestamp"""); break;
                    case "hash": columns.Add(@"""OpHash"""); break;
                    case "delegate": columns.Add(@"""DelegateId"""); break;
                    case "slots": columns.Add(@"""Slots"""); break;
                    case "rewards": columns.Add(@"""Reward"""); break;
                }
            }

            if (columns.Count == 0)
                return Enumerable.Empty<object>();

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""EndorsementOps""")
                .Take(sort, offset, limit, x => "Id");

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            var result = new object[rows.Count()][];
            for (int i = 0; i < result.Length; i++)
                result[i] = new object[fields.Length];

            for (int i = 0, j = 0; i < fields.Length; j = 0, i++)
            {
                switch (fields[i])
                {
                    case "id":
                        foreach (var row in rows)
                            result[j++][i] = row.Id;
                        break;
                    case "level":
                        foreach (var row in rows)
                            result[j++][i] = row.Level;
                        break;
                    case "timestamp":
                        foreach (var row in rows)
                            result[j++][i] = row.Timestamp;
                        break;
                    case "hash":
                        foreach (var row in rows)
                            result[j++][i] = row.OpHash;
                        break; 
                    case "delegate":
                        foreach (var row in rows)
                            result[j++][i] = await Accounts.GetAliasAsync(row.DelegateId);
                        break;
                    case "slots":
                        foreach (var row in rows)
                            result[j++][i] = row.Slots;
                        break;
                    case "rewards":
                        foreach (var row in rows)
                            result[j++][i] = row.Reward;
                        break;
                }
            }

            return result;
        }

        public async Task<IEnumerable<object>> GetEndorsements(SortParameter sort, OffsetParameter offset, int limit, string field)
        {
            var columns = new HashSet<string>(1);
            switch (field)
            {
                case "id": columns.Add(@"""Id"""); break;
                case "level": columns.Add(@"""Level"""); break;
                case "timestamp": columns.Add(@"""Timestamp"""); break;
                case "hash": columns.Add(@"""OpHash"""); break;
                case "delegate": columns.Add(@"""DelegateId"""); break;
                case "slots": columns.Add(@"""Slots"""); break;
                case "rewards": columns.Add(@"""Reward"""); break;
            }

            if (columns.Count == 0)
                return Enumerable.Empty<object>();

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""EndorsementOps""")
                .Take(sort, offset, limit, x => "Id");

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            //TODO: optimize memory allocation
            var result = new object[rows.Count()];
            var j = 0;

            switch (field)
            {
                case "id":
                    foreach (var row in rows)
                        result[j++] = row.Id;
                    break;
                case "level":
                    foreach (var row in rows)
                        result[j++] = row.Level;
                    break;
                case "timestamp":
                    foreach (var row in rows)
                        result[j++] = row.Timestamp;
                    break;
                case "hash":
                    foreach (var row in rows)
                        result[j++] = row.OpHash;
                    break;
                case "delegate":
                    foreach (var row in rows)
                        result[j++] = await Accounts.GetAliasAsync(row.DelegateId);
                    break;
                case "slots":
                    foreach (var row in rows)
                        result[j++] = row.Slots;
                    break;
                case "rewards":
                    foreach (var row in rows)
                        result[j++] = row.Reward;
                    break;
            }

            return result;
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

        public async Task<IEnumerable<BallotOperation>> GetBallots(SortParameter sort, OffsetParameter offset, int limit)
        {
            var sql = new SqlBuilder(@"
                SELECT    op.""Id"", op.""Level"", op.""Timestamp"", op.""OpHash"", op.""SenderId"", op.""Vote"",
                          proposal.""Hash"", period.""Code"", period.""Kind"", period.""StartLevel"", period.""EndLevel""
                FROM      ""BallotOps"" as op
                LEFT JOIN ""Proposals"" as proposal ON proposal.""Id"" = op.""ProposalId""
                LEFT JOIN ""VotingPeriods"" as period ON period.""Id"" = op.""PeriodId""
                ")
                .Take(sort, offset, limit, x => "Id", "op");

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

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

        public async Task<IEnumerable<object>> GetBallots(SortParameter sort, OffsetParameter offset, int limit, string[] fields)
        {
            var columns = new HashSet<string>(fields.Length + 3);
            foreach (var field in fields)
            {
                switch (field)
                {
                    case "id": columns.Add(@"op.""Id"""); break;
                    case "level": columns.Add(@"op.""Level"""); break;
                    case "timestamp": columns.Add(@"op.""Timestamp"""); break;
                    case "hash": columns.Add(@"op.""OpHash"""); break;
                    case "proposal": columns.Add(@"proposal.""Hash"""); break;
                    case "delegate": columns.Add(@"op.""SenderId"""); break;
                    case "vote": columns.Add(@"op.""Vote"""); break;
                    case "period": 
                        columns.Add(@"period.""Code""");
                        columns.Add(@"period.""Kind""");
                        columns.Add(@"period.""StartLevel""");
                        columns.Add(@"period.""EndLevel""");
                        break;
                }
            }

            if (columns.Count == 0)
                return Enumerable.Empty<object>();

            var sql = new SqlBuilder($@"
                SELECT {string.Join(',', columns)} FROM ""BallotOps"" as op
                LEFT JOIN ""Proposals"" as proposal ON proposal.""Id"" = op.""ProposalId""
                LEFT JOIN ""VotingPeriods"" as period ON period.""Id"" = op.""PeriodId""
                ")
                .Take(sort, offset, limit, x => "Id", "op");

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            var result = new object[rows.Count()][];
            for (int i = 0; i < result.Length; i++)
                result[i] = new object[fields.Length];

            for (int i = 0, j = 0; i < fields.Length; j = 0, i++)
            {
                switch (fields[i])
                {
                    case "id":
                        foreach (var row in rows)
                            result[j++][i] = row.Id;
                        break;
                    case "level":
                        foreach (var row in rows)
                            result[j++][i] = row.Level;
                        break;
                    case "timestamp":
                        foreach (var row in rows)
                            result[j++][i] = row.Timestamp;
                        break;
                    case "hash":
                        foreach (var row in rows)
                            result[j++][i] = row.OpHash;
                        break;
                    case "period":
                        foreach (var row in rows)
                            result[j++][i] = new PeriodInfo
                            {
                                Id = row.Code,
                                Kind = PeriodToString(row.Kind),
                                StartLevel = row.StartLevel,
                                EndLevel = row.EndLevel
                            };
                        break;
                    case "proposal":
                        foreach (var row in rows)
                            result[j++][i] = row.Hash;
                        break;
                    case "delegate":
                        foreach (var row in rows)
                            result[j++][i] = await Accounts.GetAliasAsync(row.SenderId);
                        break;
                    case "vote":
                        foreach (var row in rows)
                            result[j++][i] = VoteToString(row.Vote);
                        break;
                }
            }

            return result;
        }

        public async Task<IEnumerable<object>> GetBallots(SortParameter sort, OffsetParameter offset, int limit, string field)
        {
            var columns = new HashSet<string>(4);
            switch (field)
            {
                case "id": columns.Add(@"op.""Id"""); break;
                case "level": columns.Add(@"op.""Level"""); break;
                case "timestamp": columns.Add(@"op.""Timestamp"""); break;
                case "hash": columns.Add(@"op.""OpHash"""); break;
                case "proposal": columns.Add(@"proposal.""Hash"""); break;
                case "delegate": columns.Add(@"op.""SenderId"""); break;
                case "vote": columns.Add(@"op.""Vote"""); break;
                case "period":
                    columns.Add(@"period.""Code""");
                    columns.Add(@"period.""Kind""");
                    columns.Add(@"period.""StartLevel""");
                    columns.Add(@"period.""EndLevel""");
                    break;
            }

            if (columns.Count == 0)
                return Enumerable.Empty<object>();

            var sql = new SqlBuilder($@"
                SELECT {string.Join(',', columns)} FROM ""BallotOps"" as op
                LEFT JOIN ""Proposals"" as proposal ON proposal.""Id"" = op.""ProposalId""
                LEFT JOIN ""VotingPeriods"" as period ON period.""Id"" = op.""PeriodId""
                ")
                .Take(sort, offset, limit, x => "Id", "op");

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            //TODO: optimize memory allocation
            var result = new object[rows.Count()];
            var j = 0;

            switch (field)
            {
                case "id":
                    foreach (var row in rows)
                        result[j++] = row.Id;
                    break;
                case "level":
                    foreach (var row in rows)
                        result[j++] = row.Level;
                    break;
                case "timestamp":
                    foreach (var row in rows)
                        result[j++] = row.Timestamp;
                    break;
                case "hash":
                    foreach (var row in rows)
                        result[j++] = row.OpHash;
                    break;
                case "period":
                    foreach (var row in rows)
                        result[j++] = new PeriodInfo
                        {
                            Id = row.Code,
                            Kind = PeriodToString(row.Kind),
                            StartLevel = row.StartLevel,
                            EndLevel = row.EndLevel
                        };
                    break;
                case "proposal":
                    foreach (var row in rows)
                        result[j++] = row.Hash;
                    break;
                case "delegate":
                    foreach (var row in rows)
                        result[j++] = await Accounts.GetAliasAsync(row.SenderId);
                    break;
                case "vote":
                    foreach (var row in rows)
                        result[j++] = VoteToString(row.Vote);
                    break;
            }

            return result;
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

        public async Task<IEnumerable<ProposalOperation>> GetProposals(SortParameter sort, OffsetParameter offset, int limit)
        {
            var sql = new SqlBuilder(@"
                SELECT    op.""Id"", op.""Level"", op.""Timestamp"", op.""OpHash"", op.""SenderId"",
                          proposal.""Hash"", period.""Code"", period.""Kind"", period.""StartLevel"", period.""EndLevel""
                FROM      ""ProposalOps"" as op
                LEFT JOIN ""Proposals"" as proposal ON proposal.""Id"" = op.""ProposalId""
                LEFT JOIN ""VotingPeriods"" as period ON period.""Id"" = op.""PeriodId""
                ")
                .Take(sort, offset, limit, x => "Id", "op");

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

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

        public async Task<IEnumerable<object>> GetProposals(SortParameter sort, OffsetParameter offset, int limit, string[] fields)
        {
            var columns = new HashSet<string>(fields.Length + 3);
            foreach (var field in fields)
            {
                switch (field)
                {
                    case "id": columns.Add(@"op.""Id"""); break;
                    case "level": columns.Add(@"op.""Level"""); break;
                    case "timestamp": columns.Add(@"op.""Timestamp"""); break;
                    case "hash": columns.Add(@"op.""OpHash"""); break;
                    case "proposal": columns.Add(@"proposal.""Hash"""); break;
                    case "delegate": columns.Add(@"op.""SenderId"""); break;
                    case "period":
                        columns.Add(@"period.""Code""");
                        columns.Add(@"period.""Kind""");
                        columns.Add(@"period.""StartLevel""");
                        columns.Add(@"period.""EndLevel""");
                        break;
                }
            }

            if (columns.Count == 0)
                return Enumerable.Empty<object>();

            var sql = new SqlBuilder($@"
                SELECT {string.Join(',', columns)} FROM ""ProposalOps"" as op
                LEFT JOIN ""Proposals"" as proposal ON proposal.""Id"" = op.""ProposalId""
                LEFT JOIN ""VotingPeriods"" as period ON period.""Id"" = op.""PeriodId""
                ")
                .Take(sort, offset, limit, x => "Id", "op");

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            var result = new object[rows.Count()][];
            for (int i = 0; i < result.Length; i++)
                result[i] = new object[fields.Length];

            for (int i = 0, j = 0; i < fields.Length; j = 0, i++)
            {
                switch (fields[i])
                {
                    case "id":
                        foreach (var row in rows)
                            result[j++][i] = row.Id;
                        break;
                    case "level":
                        foreach (var row in rows)
                            result[j++][i] = row.Level;
                        break;
                    case "timestamp":
                        foreach (var row in rows)
                            result[j++][i] = row.Timestamp;
                        break;
                    case "hash":
                        foreach (var row in rows)
                            result[j++][i] = row.OpHash;
                        break;
                    case "period":
                        foreach (var row in rows)
                            result[j++][i] = new PeriodInfo
                            {
                                Id = row.Code,
                                Kind = PeriodToString(row.Kind),
                                StartLevel = row.StartLevel,
                                EndLevel = row.EndLevel
                            };
                        break;
                    case "proposal":
                        foreach (var row in rows)
                            result[j++][i] = row.Hash;
                        break;
                    case "delegate":
                        foreach (var row in rows)
                            result[j++][i] = await Accounts.GetAliasAsync(row.SenderId);
                        break;
                }
            }

            return result;
        }

        public async Task<IEnumerable<object>> GetProposals(SortParameter sort, OffsetParameter offset, int limit, string field)
        {
            var columns = new HashSet<string>(4);
            switch (field)
            {
                case "id": columns.Add(@"op.""Id"""); break;
                case "level": columns.Add(@"op.""Level"""); break;
                case "timestamp": columns.Add(@"op.""Timestamp"""); break;
                case "hash": columns.Add(@"op.""OpHash"""); break;
                case "proposal": columns.Add(@"proposal.""Hash"""); break;
                case "delegate": columns.Add(@"op.""SenderId"""); break;
                case "period":
                    columns.Add(@"period.""Code""");
                    columns.Add(@"period.""Kind""");
                    columns.Add(@"period.""StartLevel""");
                    columns.Add(@"period.""EndLevel""");
                    break;
            }

            if (columns.Count == 0)
                return Enumerable.Empty<object>();

            var sql = new SqlBuilder($@"
                SELECT {string.Join(',', columns)} FROM ""ProposalOps"" as op
                LEFT JOIN ""Proposals"" as proposal ON proposal.""Id"" = op.""ProposalId""
                LEFT JOIN ""VotingPeriods"" as period ON period.""Id"" = op.""PeriodId""
                ")
                .Take(sort, offset, limit, x => "Id", "op");

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            //TODO: optimize memory allocation
            var result = new object[rows.Count()];
            var j = 0;

            switch (field)
            {
                case "id":
                    foreach (var row in rows)
                        result[j++] = row.Id;
                    break;
                case "level":
                    foreach (var row in rows)
                        result[j++] = row.Level;
                    break;
                case "timestamp":
                    foreach (var row in rows)
                        result[j++] = row.Timestamp;
                    break;
                case "hash":
                    foreach (var row in rows)
                        result[j++] = row.OpHash;
                    break;
                case "period":
                    foreach (var row in rows)
                        result[j++] = new PeriodInfo
                        {
                            Id = row.Code,
                            Kind = PeriodToString(row.Kind),
                            StartLevel = row.StartLevel,
                            EndLevel = row.EndLevel
                        };
                    break;
                case "proposal":
                    foreach (var row in rows)
                        result[j++] = row.Hash;
                    break;
                case "delegate":
                    foreach (var row in rows)
                        result[j++] = await Accounts.GetAliasAsync(row.SenderId);
                    break;
            }

            return result;
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

        public async Task<IEnumerable<ActivationOperation>> GetActivations(SortParameter sort, OffsetParameter offset, int limit)
        {
            var sql = new SqlBuilder(@"SELECT * FROM ""ActivationOps""")
                .Take(sort, offset, limit, x => "Id");

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

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

        public async Task<IEnumerable<object>> GetActivations(SortParameter sort, OffsetParameter offset, int limit, string[] fields)
        {
            var columns = new HashSet<string>(fields.Length);
            foreach (var field in fields)
            {
                switch (field)
                {
                    case "id": columns.Add(@"""Id"""); break;
                    case "level": columns.Add(@"""Level"""); break;
                    case "timestamp": columns.Add(@"""Timestamp"""); break;
                    case "hash": columns.Add(@"""OpHash"""); break;
                    case "account": columns.Add(@"""AccountId"""); break;
                    case "balance": columns.Add(@"""Balance"""); break;
                }
            }

            if (columns.Count == 0)
                return Enumerable.Empty<object>();

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""ActivationOps""")
                .Take(sort, offset, limit, x => "Id");

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            var result = new object[rows.Count()][];
            for (int i = 0; i < result.Length; i++)
                result[i] = new object[fields.Length];

            for (int i = 0, j = 0; i < fields.Length; j = 0, i++)
            {
                switch (fields[i])
                {
                    case "id":
                        foreach (var row in rows)
                            result[j++][i] = row.Id;
                        break;
                    case "level":
                        foreach (var row in rows)
                            result[j++][i] = row.Level;
                        break;
                    case "timestamp":
                        foreach (var row in rows)
                            result[j++][i] = row.Timestamp;
                        break;
                    case "hash":
                        foreach (var row in rows)
                            result[j++][i] = row.OpHash;
                        break;
                    case "account":
                        foreach (var row in rows)
                            result[j++][i] = await Accounts.GetAliasAsync(row.AccountId);
                        break;
                    case "balance":
                        foreach (var row in rows)
                            result[j++][i] = row.Balance;
                        break;
                }
            }

            return result;
        }

        public async Task<IEnumerable<object>> GetActivations(SortParameter sort, OffsetParameter offset, int limit, string field)
        {
            var columns = new HashSet<string>(1);
            switch (field)
            {
                case "id": columns.Add(@"""Id"""); break;
                case "level": columns.Add(@"""Level"""); break;
                case "timestamp": columns.Add(@"""Timestamp"""); break;
                case "hash": columns.Add(@"""OpHash"""); break;
                case "account": columns.Add(@"""AccountId"""); break;
                case "balance": columns.Add(@"""Balance"""); break;
            }

            if (columns.Count == 0)
                return Enumerable.Empty<object>();

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""ActivationOps""")
                .Take(sort, offset, limit, x => "Id");

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            //TODO: optimize memory allocation
            var result = new object[rows.Count()];
            var j = 0;

            switch (field)
            {
                case "id":
                    foreach (var row in rows)
                        result[j++] = row.Id;
                    break;
                case "level":
                    foreach (var row in rows)
                        result[j++] = row.Level;
                    break;
                case "timestamp":
                    foreach (var row in rows)
                        result[j++] = row.Timestamp;
                    break;
                case "hash":
                    foreach (var row in rows)
                        result[j++] = row.OpHash;
                    break;
                case "account":
                    foreach (var row in rows)
                        result[j++] = await Accounts.GetAliasAsync(row.AccountId);
                    break;
                case "balance":
                    foreach (var row in rows)
                        result[j++] = row.Balance;
                    break;
            }

            return result;
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

        public async Task<IEnumerable<DoubleBakingOperation>> GetDoubleBakings(SortParameter sort, OffsetParameter offset, int limit)
        {
            var sql = new SqlBuilder(@"SELECT * FROM ""DoubleBakingOps""")
                .Take(sort, offset, limit, x => "Id");

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

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

        public async Task<IEnumerable<object>> GetDoubleBakings(SortParameter sort, OffsetParameter offset, int limit, string[] fields)
        {
            var columns = new HashSet<string>(fields.Length);
            foreach (var field in fields)
            {
                switch (field)
                {
                    case "id": columns.Add(@"""Id"""); break;
                    case "level": columns.Add(@"""Level"""); break;
                    case "timestamp": columns.Add(@"""Timestamp"""); break;
                    case "hash": columns.Add(@"""OpHash"""); break;
                    case "accusedLevel": columns.Add(@"""AccusedLevel"""); break;
                    case "accuser": columns.Add(@"""AccuserId"""); break;
                    case "accuserRewards": columns.Add(@"""AccuserReward"""); break;
                    case "offender": columns.Add(@"""OffenderId"""); break;
                    case "offenderLostDeposits": columns.Add(@"""OffenderLostDeposit"""); break;
                    case "offenderLostRewards": columns.Add(@"""OffenderLostReward"""); break;
                    case "offenderLostFees": columns.Add(@"""OffenderLostFee"""); break;
                }
            }

            if (columns.Count == 0)
                return Enumerable.Empty<object>();

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""DoubleBakingOps""")
                .Take(sort, offset, limit, x => "Id");

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            var result = new object[rows.Count()][];
            for (int i = 0; i < result.Length; i++)
                result[i] = new object[fields.Length];

            for (int i = 0, j = 0; i < fields.Length; j = 0, i++)
            {
                switch (fields[i])
                {
                    case "id":
                        foreach (var row in rows)
                            result[j++][i] = row.Id;
                        break;
                    case "level":
                        foreach (var row in rows)
                            result[j++][i] = row.Level;
                        break;
                    case "timestamp":
                        foreach (var row in rows)
                            result[j++][i] = row.Timestamp;
                        break;
                    case "hash":
                        foreach (var row in rows)
                            result[j++][i] = row.OpHash;
                        break;
                    case "accusedLevel":
                        foreach (var row in rows)
                            result[j++][i] = row.AccusedLevel;
                        break;
                    case "accuser":
                        foreach (var row in rows)
                            result[j++][i] = await Accounts.GetAliasAsync(row.AccuserId);
                        break;
                    case "accuserRewards":
                        foreach (var row in rows)
                            result[j++][i] = row.AccuserReward;
                        break;
                    case "offender":
                        foreach (var row in rows)
                            result[j++][i] = await Accounts.GetAliasAsync(row.OffenderId);
                        break;
                    case "offenderLostDeposits":
                        foreach (var row in rows)
                            result[j++][i] = row.OffenderLostDeposit;
                        break;
                    case "offenderLostRewards":
                        foreach (var row in rows)
                            result[j++][i] = row.OffenderLostReward;
                        break;
                    case "offenderLostFees":
                        foreach (var row in rows)
                            result[j++][i] = row.OffenderLostFee;
                        break;
                }
            }

            return result;
        }

        public async Task<IEnumerable<object>> GetDoubleBakings(SortParameter sort, OffsetParameter offset, int limit, string field)
        {
            var columns = new HashSet<string>(1);
            switch (field)
            {
                case "id": columns.Add(@"""Id"""); break;
                case "level": columns.Add(@"""Level"""); break;
                case "timestamp": columns.Add(@"""Timestamp"""); break;
                case "hash": columns.Add(@"""OpHash"""); break;
                case "accusedLevel": columns.Add(@"""AccusedLevel"""); break;
                case "accuser": columns.Add(@"""AccuserId"""); break;
                case "accuserRewards": columns.Add(@"""AccuserReward"""); break;
                case "offender": columns.Add(@"""OffenderId"""); break;
                case "offenderLostDeposits": columns.Add(@"""OffenderLostDeposit"""); break;
                case "offenderLostRewards": columns.Add(@"""OffenderLostReward"""); break;
                case "offenderLostFees": columns.Add(@"""OffenderLostFee"""); break;
            }

            if (columns.Count == 0)
                return Enumerable.Empty<object>();

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""DoubleBakingOps""")
                .Take(sort, offset, limit, x => "Id");

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            //TODO: optimize memory allocation
            var result = new object[rows.Count()];
            var j = 0;

            switch (field)
            {
                case "id":
                    foreach (var row in rows)
                        result[j++] = row.Id;
                    break;
                case "level":
                    foreach (var row in rows)
                        result[j++] = row.Level;
                    break;
                case "timestamp":
                    foreach (var row in rows)
                        result[j++] = row.Timestamp;
                    break;
                case "hash":
                    foreach (var row in rows)
                        result[j++] = row.OpHash;
                    break;
                case "accusedLevel":
                    foreach (var row in rows)
                        result[j++] = row.AccusedLevel;
                    break;
                case "accuser":
                    foreach (var row in rows)
                        result[j++] = await Accounts.GetAliasAsync(row.AccuserId);
                    break;
                case "accuserRewards":
                    foreach (var row in rows)
                        result[j++] = row.AccuserReward;
                    break;
                case "offender":
                    foreach (var row in rows)
                        result[j++] = await Accounts.GetAliasAsync(row.OffenderId);
                    break;
                case "offenderLostDeposits":
                    foreach (var row in rows)
                        result[j++] = row.OffenderLostDeposit;
                    break;
                case "offenderLostRewards":
                    foreach (var row in rows)
                        result[j++] = row.OffenderLostReward;
                    break;
                case "offenderLostFees":
                    foreach (var row in rows)
                        result[j++] = row.OffenderLostFee;
                    break;
            }

            return result;
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

        public async Task<IEnumerable<DoubleEndorsingOperation>> GetDoubleEndorsings(SortParameter sort, OffsetParameter offset, int limit)
        {
            var sql = new SqlBuilder(@"SELECT * FROM ""DoubleEndorsingOps""")
                .Take(sort, offset, limit, x => "Id");

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

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

        public async Task<IEnumerable<object>> GetDoubleEndorsings(SortParameter sort, OffsetParameter offset, int limit, string[] fields)
        {
            var columns = new HashSet<string>(fields.Length);
            foreach (var field in fields)
            {
                switch (field)
                {
                    case "id": columns.Add(@"""Id"""); break;
                    case "level": columns.Add(@"""Level"""); break;
                    case "timestamp": columns.Add(@"""Timestamp"""); break;
                    case "hash": columns.Add(@"""OpHash"""); break;
                    case "accusedLevel": columns.Add(@"""AccusedLevel"""); break;
                    case "accuser": columns.Add(@"""AccuserId"""); break;
                    case "accuserRewards": columns.Add(@"""AccuserReward"""); break;
                    case "offender": columns.Add(@"""OffenderId"""); break;
                    case "offenderLostDeposits": columns.Add(@"""OffenderLostDeposit"""); break;
                    case "offenderLostRewards": columns.Add(@"""OffenderLostReward"""); break;
                    case "offenderLostFees": columns.Add(@"""OffenderLostFee"""); break;
                }
            }

            if (columns.Count == 0)
                return Enumerable.Empty<object>();

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""DoubleEndorsingOps""")
                .Take(sort, offset, limit, x => "Id");

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            var result = new object[rows.Count()][];
            for (int i = 0; i < result.Length; i++)
                result[i] = new object[fields.Length];

            for (int i = 0, j = 0; i < fields.Length; j = 0, i++)
            {
                switch (fields[i])
                {
                    case "id":
                        foreach (var row in rows)
                            result[j++][i] = row.Id;
                        break;
                    case "level":
                        foreach (var row in rows)
                            result[j++][i] = row.Level;
                        break;
                    case "timestamp":
                        foreach (var row in rows)
                            result[j++][i] = row.Timestamp;
                        break;
                    case "hash":
                        foreach (var row in rows)
                            result[j++][i] = row.OpHash;
                        break;
                    case "accusedLevel":
                        foreach (var row in rows)
                            result[j++][i] = row.AccusedLevel;
                        break;
                    case "accuser":
                        foreach (var row in rows)
                            result[j++][i] = await Accounts.GetAliasAsync(row.AccuserId);
                        break;
                    case "accuserRewards":
                        foreach (var row in rows)
                            result[j++][i] = row.AccuserReward;
                        break;
                    case "offender":
                        foreach (var row in rows)
                            result[j++][i] = await Accounts.GetAliasAsync(row.OffenderId);
                        break;
                    case "offenderLostDeposits":
                        foreach (var row in rows)
                            result[j++][i] = row.OffenderLostDeposit;
                        break;
                    case "offenderLostRewards":
                        foreach (var row in rows)
                            result[j++][i] = row.OffenderLostReward;
                        break;
                    case "offenderLostFees":
                        foreach (var row in rows)
                            result[j++][i] = row.OffenderLostFee;
                        break;
                }
            }

            return result;
        }

        public async Task<IEnumerable<object>> GetDoubleEndorsings(SortParameter sort, OffsetParameter offset, int limit, string field)
        {
            var columns = new HashSet<string>(1);
            switch (field)
            {
                case "id": columns.Add(@"""Id"""); break;
                case "level": columns.Add(@"""Level"""); break;
                case "timestamp": columns.Add(@"""Timestamp"""); break;
                case "hash": columns.Add(@"""OpHash"""); break;
                case "accusedLevel": columns.Add(@"""AccusedLevel"""); break;
                case "accuser": columns.Add(@"""AccuserId"""); break;
                case "accuserRewards": columns.Add(@"""AccuserReward"""); break;
                case "offender": columns.Add(@"""OffenderId"""); break;
                case "offenderLostDeposits": columns.Add(@"""OffenderLostDeposit"""); break;
                case "offenderLostRewards": columns.Add(@"""OffenderLostReward"""); break;
                case "offenderLostFees": columns.Add(@"""OffenderLostFee"""); break;
            }

            if (columns.Count == 0)
                return Enumerable.Empty<object>();

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""DoubleEndorsingOps""")
                .Take(sort, offset, limit, x => "Id");

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            //TODO: optimize memory allocation
            var result = new object[rows.Count()];
            var j = 0;

            switch (field)
            {
                case "id":
                    foreach (var row in rows)
                        result[j++] = row.Id;
                    break;
                case "level":
                    foreach (var row in rows)
                        result[j++] = row.Level;
                    break;
                case "timestamp":
                    foreach (var row in rows)
                        result[j++] = row.Timestamp;
                    break;
                case "hash":
                    foreach (var row in rows)
                        result[j++] = row.OpHash;
                    break;
                case "accusedLevel":
                    foreach (var row in rows)
                        result[j++] = row.AccusedLevel;
                    break;
                case "accuser":
                    foreach (var row in rows)
                        result[j++] = await Accounts.GetAliasAsync(row.AccuserId);
                    break;
                case "accuserRewards":
                    foreach (var row in rows)
                        result[j++] = row.AccuserReward;
                    break;
                case "offender":
                    foreach (var row in rows)
                        result[j++] = await Accounts.GetAliasAsync(row.OffenderId);
                    break;
                case "offenderLostDeposits":
                    foreach (var row in rows)
                        result[j++] = row.OffenderLostDeposit;
                    break;
                case "offenderLostRewards":
                    foreach (var row in rows)
                        result[j++] = row.OffenderLostReward;
                    break;
                case "offenderLostFees":
                    foreach (var row in rows)
                        result[j++] = row.OffenderLostFee;
                    break;
            }

            return result;
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

        public async Task<IEnumerable<NonceRevelationOperation>> GetNonceRevelations(SortParameter sort, OffsetParameter offset, int limit)
        {
            var sql = new SqlBuilder(@"SELECT * FROM ""NonceRevelationOps""")
                .Take(sort, offset, limit, x => "Id");

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

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

        public async Task<IEnumerable<object>> GetNonceRevelations(SortParameter sort, OffsetParameter offset, int limit, string[] fields)
        {
            var columns = new HashSet<string>(fields.Length);
            foreach (var field in fields)
            {
                switch (field)
                {
                    case "id": columns.Add(@"""Id"""); break;
                    case "level": columns.Add(@"""Level"""); break;
                    case "timestamp": columns.Add(@"""Timestamp"""); break;
                    case "hash": columns.Add(@"""OpHash"""); break;
                    case "baker": columns.Add(@"""BakerId"""); break;
                    case "sender": columns.Add(@"""SenderId"""); break;
                    case "revealedLevel": columns.Add(@"""RevealedLevel"""); break;
                }
            }

            if (columns.Count == 0)
                return Enumerable.Empty<object>();

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""NonceRevelationOps""")
                .Take(sort, offset, limit, x => "Id");

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            var result = new object[rows.Count()][];
            for (int i = 0; i < result.Length; i++)
                result[i] = new object[fields.Length];

            for (int i = 0, j = 0; i < fields.Length; j = 0, i++)
            {
                switch (fields[i])
                {
                    case "id":
                        foreach (var row in rows)
                            result[j++][i] = row.Id;
                        break;
                    case "level":
                        foreach (var row in rows)
                            result[j++][i] = row.Level;
                        break;
                    case "timestamp":
                        foreach (var row in rows)
                            result[j++][i] = row.Timestamp;
                        break;
                    case "hash":
                        foreach (var row in rows)
                            result[j++][i] = row.OpHash;
                        break;
                    case "baker":
                        foreach (var row in rows)
                            result[j++][i] = await Accounts.GetAliasAsync(row.BakerId);
                        break;
                    case "sender":
                        foreach (var row in rows)
                            result[j++][i] = await Accounts.GetAliasAsync(row.SenderId);
                        break;
                    case "revealedLevel":
                        foreach (var row in rows)
                            result[j++][i] = row.RevealedLevel;
                        break;
                }
            }

            return result;
        }

        public async Task<IEnumerable<object>> GetNonceRevelations(SortParameter sort, OffsetParameter offset, int limit, string field)
        {
            var columns = new HashSet<string>(1);
            switch (field)
            {
                case "id": columns.Add(@"""Id"""); break;
                case "level": columns.Add(@"""Level"""); break;
                case "timestamp": columns.Add(@"""Timestamp"""); break;
                case "hash": columns.Add(@"""OpHash"""); break;
                case "baker": columns.Add(@"""BakerId"""); break;
                case "sender": columns.Add(@"""SenderId"""); break;
                case "revealedLevel": columns.Add(@"""RevealedLevel"""); break;
            }

            if (columns.Count == 0)
                return Enumerable.Empty<object>();

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""NonceRevelationOps""")
                .Take(sort, offset, limit, x => "Id");

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            //TODO: optimize memory allocation
            var result = new object[rows.Count()];
            var j = 0;

            switch (field)
            {
                case "id":
                    foreach (var row in rows)
                        result[j++] = row.Id;
                    break;
                case "level":
                    foreach (var row in rows)
                        result[j++] = row.Level;
                    break;
                case "timestamp":
                    foreach (var row in rows)
                        result[j++] = row.Timestamp;
                    break;
                case "hash":
                    foreach (var row in rows)
                        result[j++] = row.OpHash;
                    break;
                case "baker":
                    foreach (var row in rows)
                        result[j++] = await Accounts.GetAliasAsync(row.BakerId);
                    break;
                case "sender":
                    foreach (var row in rows)
                        result[j++] = await Accounts.GetAliasAsync(row.SenderId);
                    break;
                case "revealedLevel":
                    foreach (var row in rows)
                        result[j++] = row.RevealedLevel;
                    break;
            }

            return result;
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

        public async Task<IEnumerable<DelegationOperation>> GetDelegations(
            AccountParameter sender,
            AccountParameter prevDelegate,
            AccountParameter newDelegate,
            OperationStatusParameter status,
            SortParameter sort,
            OffsetParameter offset,
            int limit)
        {
            var sql = new SqlBuilder(@"SELECT * FROM ""DelegationOps""")
                .Filter("SenderId", sender, x => x == "prevDelegate" ? "PrevDelegateId" : "DelegateId")
                .Filter("PrevDelegateId", prevDelegate, x => x == "sender" ? "SenderId" : "DelegateId")
                .Filter("DelegateId", newDelegate, x => x == "sender" ? "SenderId" : "PrevDelegateId")
                .Filter("Status", status)
                .Take(sort, offset, limit, x => "Id");

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

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

        public async Task<IEnumerable<object>> GetDelegations(
            AccountParameter sender,
            AccountParameter prevDelegate,
            AccountParameter newDelegate,
            OperationStatusParameter status,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            string[] fields)
        {
            var columns = new HashSet<string>(fields.Length);
            foreach (var field in fields)
            {
                switch (field)
                {
                    case "id": columns.Add(@"""Id"""); break;
                    case "level": columns.Add(@"""Level"""); break;
                    case "timestamp": columns.Add(@"""Timestamp"""); break;
                    case "hash": columns.Add(@"""OpHash"""); break;
                    case "initiator": columns.Add(@"""InitiatorId"""); break;
                    case "sender": columns.Add(@"""SenderId"""); break;
                    case "counter": columns.Add(@"""Counter"""); break;
                    case "nonce": columns.Add(@"""Nonce"""); break;
                    case "gasLimit": columns.Add(@"""GasLimit"""); break;
                    case "gasUsed": columns.Add(@"""GasUsed"""); break;
                    case "bakerFee": columns.Add(@"""BakerFee"""); break;
                    case "prevDelegate": columns.Add(@"""PrevDelegateId"""); break;
                    case "newDelegate": columns.Add(@"""DelegateId"""); break;
                    case "status": columns.Add(@"""Status"""); break;
                    case "errors": columns.Add(@"""Errors"""); break;
                }
            }

            if (columns.Count == 0)
                return Enumerable.Empty<object>();

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""DelegationOps""")
                .Filter("SenderId", sender, x => x == "prevDelegate" ? "PrevDelegateId" : "DelegateId")
                .Filter("PrevDelegateId", prevDelegate, x => x == "sender" ? "SenderId" : "DelegateId")
                .Filter("DelegateId", newDelegate, x => x == "sender" ? "SenderId" : "PrevDelegateId")
                .Filter("Status", status)
                .Take(sort, offset, limit, x => "Id");

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            var result = new object[rows.Count()][];
            for (int i = 0; i < result.Length; i++)
                result[i] = new object[fields.Length];

            for (int i = 0, j = 0; i < fields.Length; j = 0, i++)
            {
                switch (fields[i])
                {
                    case "id":
                        foreach (var row in rows)
                            result[j++][i] = row.Id;
                        break;
                    case "level":
                        foreach (var row in rows)
                            result[j++][i] = row.Level;
                        break;
                    case "timestamp":
                        foreach (var row in rows)
                            result[j++][i] = row.Timestamp;
                        break;
                    case "hash":
                        foreach (var row in rows)
                            result[j++][i] = row.OpHash;
                        break;
                    case "initiator":
                        foreach (var row in rows)
                            result[j++][i] = row.InitiatorId != null ? await Accounts.GetAliasAsync(row.InitiatorId) : null;
                        break;
                    case "sender":
                        foreach (var row in rows)
                            result[j++][i] = await Accounts.GetAliasAsync(row.SenderId);
                        break;
                    case "counter":
                        foreach (var row in rows)
                            result[j++][i] = row.Counter;
                        break;
                    case "nonce":
                        foreach (var row in rows)
                            result[j++][i] = row.Nonce;
                        break;
                    case "gasLimit":
                        foreach (var row in rows)
                            result[j++][i] = row.GasLimit;
                        break;
                    case "gasUsed":
                        foreach (var row in rows)
                            result[j++][i] = row.GasUsed;
                        break;
                    case "bakerFee":
                        foreach (var row in rows)
                            result[j++][i] = row.BakerFee;
                        break;
                    case "prevDelegate":
                        foreach (var row in rows)
                            result[j++][i] = row.PrevDelegateId != null ? await Accounts.GetAliasAsync(row.PrevDelegateId) : null;
                        break;
                    case "newDelegate":
                        foreach (var row in rows)
                            result[j++][i] = row.DelegateId != null ? await Accounts.GetAliasAsync(row.DelegateId) : null;
                        break;
                    case "status":
                        foreach (var row in rows)
                            result[j++][i] = StatusToString(row.Status);
                        break;
                    case "errors":
                        foreach (var row in rows)
                            result[j++][i] = row.Errors != null ? OperationErrorSerializer.Deserialize(row.Errors) : null;
                        break;
                }
            }

            return result;
        }

        public async Task<IEnumerable<object>> GetDelegations(
            AccountParameter sender,
            AccountParameter prevDelegate,
            AccountParameter newDelegate,
            OperationStatusParameter status,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            string field)
        {
            var columns = new HashSet<string>(1);
            switch (field)
            {
                case "id": columns.Add(@"""Id"""); break;
                case "level": columns.Add(@"""Level"""); break;
                case "timestamp": columns.Add(@"""Timestamp"""); break;
                case "hash": columns.Add(@"""OpHash"""); break;
                case "initiator": columns.Add(@"""InitiatorId"""); break;
                case "sender": columns.Add(@"""SenderId"""); break;
                case "counter": columns.Add(@"""Counter"""); break;
                case "nonce": columns.Add(@"""Nonce"""); break;
                case "gasLimit": columns.Add(@"""GasLimit"""); break;
                case "gasUsed": columns.Add(@"""GasUsed"""); break;
                case "bakerFee": columns.Add(@"""BakerFee"""); break;
                case "prevDelegate": columns.Add(@"""PrevDelegateId"""); break;
                case "newDelegate": columns.Add(@"""DelegateId"""); break;
                case "status": columns.Add(@"""Status"""); break;
                case "errors": columns.Add(@"""Errors"""); break;
            }

            if (columns.Count == 0)
                return Enumerable.Empty<object>();

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""DelegationOps""")
                .Filter("SenderId", sender, x => x == "prevDelegate" ? "PrevDelegateId" : "DelegateId")
                .Filter("PrevDelegateId", prevDelegate, x => x == "sender" ? "SenderId" : "DelegateId")
                .Filter("DelegateId", newDelegate, x => x == "sender" ? "SenderId" : "PrevDelegateId")
                .Filter("Status", status)
                .Take(sort, offset, limit, x => "Id");

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            //TODO: optimize memory allocation
            var result = new object[rows.Count()];
            var j = 0;

            switch (field)
            {
                case "id":
                    foreach (var row in rows)
                        result[j++] = row.Id;
                    break;
                case "level":
                    foreach (var row in rows)
                        result[j++] = row.Level;
                    break;
                case "timestamp":
                    foreach (var row in rows)
                        result[j++] = row.Timestamp;
                    break;
                case "hash":
                    foreach (var row in rows)
                        result[j++] = row.OpHash;
                    break;
                case "initiator":
                    foreach (var row in rows)
                        result[j++] = row.InitiatorId != null ? await Accounts.GetAliasAsync(row.InitiatorId) : null;
                    break;
                case "sender":
                    foreach (var row in rows)
                        result[j++] = await Accounts.GetAliasAsync(row.SenderId);
                    break;
                case "counter":
                    foreach (var row in rows)
                        result[j++] = row.Counter;
                    break;
                case "nonce":
                    foreach (var row in rows)
                        result[j++] = row.Nonce;
                    break;
                case "gasLimit":
                    foreach (var row in rows)
                        result[j++] = row.GasLimit;
                    break;
                case "gasUsed":
                    foreach (var row in rows)
                        result[j++] = row.GasUsed;
                    break;
                case "bakerFee":
                    foreach (var row in rows)
                        result[j++] = row.BakerFee;
                    break;
                case "prevDelegate":
                    foreach (var row in rows)
                        result[j++] = row.PrevDelegateId != null ? await Accounts.GetAliasAsync(row.PrevDelegateId) : null;
                    break;
                case "newDelegate":
                    foreach (var row in rows)
                        result[j++] = row.DelegateId != null ? await Accounts.GetAliasAsync(row.DelegateId) : null;
                    break;
                case "status":
                    foreach (var row in rows)
                        result[j++] = StatusToString(row.Status);
                    break;
                case "errors":
                    foreach (var row in rows)
                        result[j++] = row.Errors != null ? OperationErrorSerializer.Deserialize(row.Errors) : null;
                    break;
            }

            return result;
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

        public async Task<IEnumerable<OriginationOperation>> GetOriginations(SortParameter sort, OffsetParameter offset, int limit)
        {
            var sql = new SqlBuilder(@"SELECT * FROM ""OriginationOps""")
                .Take(sort, offset, limit, x => "Id");

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

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
                    OriginatedContract = contract == null ? null : new OriginatedContract
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

        public async Task<IEnumerable<object>> GetOriginations(SortParameter sort, OffsetParameter offset, int limit, string[] fields)
        {
            var columns = new HashSet<string>(fields.Length);
            foreach (var field in fields)
            {
                switch (field)
                {
                    case "id": columns.Add(@"""Id"""); break;
                    case "level": columns.Add(@"""Level"""); break;
                    case "timestamp": columns.Add(@"""Timestamp"""); break;
                    case "hash": columns.Add(@"""OpHash"""); break;
                    case "initiator": columns.Add(@"""InitiatorId"""); break;
                    case "sender": columns.Add(@"""SenderId"""); break;
                    case "counter": columns.Add(@"""Counter"""); break;
                    case "nonce": columns.Add(@"""Nonce"""); break;
                    case "gasLimit": columns.Add(@"""GasLimit"""); break;
                    case "gasUsed": columns.Add(@"""GasUsed"""); break;
                    case "storageLimit": columns.Add(@"""StorageLimit"""); break;
                    case "storageUsed": columns.Add(@"""StorageUsed"""); break;
                    case "bakerFee": columns.Add(@"""BakerFee"""); break;
                    case "storageFee": columns.Add(@"""StorageFee"""); break;
                    case "allocationFee": columns.Add(@"""AllocationFee"""); break;
                    case "contractDelegate": columns.Add(@"""DelegateId"""); break;
                    case "contractBalance": columns.Add(@"""Balance"""); break;
                    case "status": columns.Add(@"""Status"""); break;
                    case "originatedContract": columns.Add(@"""ContractId"""); break;
                    case "contractManager": columns.Add(@"""ManagerId"""); break;
                    case "errors": columns.Add(@"""Errors"""); break;
                }
            }

            if (columns.Count == 0)
                return Enumerable.Empty<object>();

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""OriginationOps""")
                .Take(sort, offset, limit, x => "Id");

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            var result = new object[rows.Count()][];
            for (int i = 0; i < result.Length; i++)
                result[i] = new object[fields.Length];

            for (int i = 0, j = 0; i < fields.Length; j = 0, i++)
            {
                switch (fields[i])
                {
                    case "id":
                        foreach (var row in rows)
                            result[j++][i] = row.Id;
                        break;
                    case "level":
                        foreach (var row in rows)
                            result[j++][i] = row.Level;
                        break;
                    case "timestamp":
                        foreach (var row in rows)
                            result[j++][i] = row.Timestamp;
                        break;
                    case "hash":
                        foreach (var row in rows)
                            result[j++][i] = row.OpHash;
                        break;
                    case "initiator":
                        foreach (var row in rows)
                            result[j++][i] = row.InitiatorId != null ? await Accounts.GetAliasAsync(row.InitiatorId) : null;
                        break;
                    case "sender":
                        foreach (var row in rows)
                            result[j++][i] = await Accounts.GetAliasAsync(row.SenderId);
                        break;
                    case "counter":
                        foreach (var row in rows)
                            result[j++][i] = row.Counter;
                        break;
                    case "nonce":
                        foreach (var row in rows)
                            result[j++][i] = row.Nonce;
                        break;
                    case "gasLimit":
                        foreach (var row in rows)
                            result[j++][i] = row.GasLimit;
                        break;
                    case "gasUsed":
                        foreach (var row in rows)
                            result[j++][i] = row.GasUsed;
                        break;
                    case "storageLimit":
                        foreach (var row in rows)
                            result[j++][i] = row.StorageLimit;
                        break;
                    case "storageUsed":
                        foreach (var row in rows)
                            result[j++][i] = row.StorageUsed;
                        break;
                    case "bakerFee":
                        foreach (var row in rows)
                            result[j++][i] = row.BakerFee;
                        break;
                    case "storageFee":
                        foreach (var row in rows)
                            result[j++][i] = row.StorageFee ?? 0;
                        break;
                    case "allocationFee":
                        foreach (var row in rows)
                            result[j++][i] = row.AllocationFee ?? 0;
                        break;
                    case "contractDelegate":
                        foreach (var row in rows)
                            result[j++][i] = row.DelegateId != null ? await Accounts.GetAliasAsync(row.DelegateId) : null;
                        break;
                    case "contractBalance":
                        foreach (var row in rows)
                            result[j++][i] = row.Balance;
                        break;
                    case "status":
                        foreach (var row in rows)
                            result[j++][i] = StatusToString(row.Status);
                        break;
                    case "originatedContract":
                        foreach (var row in rows)
                        {
                            var contract = row.ContractId == null ? null : (RawContract)Accounts.Get((int)row.ContractId);
                            var contractMetadata = contract == null ? null : Accounts.GetMetadata(contract.Id);

                            result[j++][i] = contract == null ? null : new OriginatedContract
                            {
                                Alias = contractMetadata?.Alias,
                                Address = contract.Address,
                                Kind = contract.KindString
                            };
                        }
                        break;
                    case "contractManager":
                        foreach (var row in rows)
                            result[j++][i] = row.ManagerId != null ? await Accounts.GetAliasAsync(row.ManagerId) : null;
                        break;
                    case "errors":
                        foreach (var row in rows)
                            result[j++][i] = row.Errors != null ? OperationErrorSerializer.Deserialize(row.Errors) : null;
                        break;
                }
            }

            return result;
        }

        public async Task<IEnumerable<object>> GetOriginations(SortParameter sort, OffsetParameter offset, int limit, string field)
        {
            var columns = new HashSet<string>(1);
            switch (field)
            {
                case "id": columns.Add(@"""Id"""); break;
                case "level": columns.Add(@"""Level"""); break;
                case "timestamp": columns.Add(@"""Timestamp"""); break;
                case "hash": columns.Add(@"""OpHash"""); break;
                case "initiator": columns.Add(@"""InitiatorId"""); break;
                case "sender": columns.Add(@"""SenderId"""); break;
                case "counter": columns.Add(@"""Counter"""); break;
                case "nonce": columns.Add(@"""Nonce"""); break;
                case "gasLimit": columns.Add(@"""GasLimit"""); break;
                case "gasUsed": columns.Add(@"""GasUsed"""); break;
                case "storageLimit": columns.Add(@"""StorageLimit"""); break;
                case "storageUsed": columns.Add(@"""StorageUsed"""); break;
                case "bakerFee": columns.Add(@"""BakerFee"""); break;
                case "storageFee": columns.Add(@"""StorageFee"""); break;
                case "allocationFee": columns.Add(@"""AllocationFee"""); break;
                case "contractDelegate": columns.Add(@"""DelegateId"""); break;
                case "contractBalance": columns.Add(@"""Balance"""); break;
                case "status": columns.Add(@"""Status"""); break;
                case "originatedContract": columns.Add(@"""ContractId"""); break;
                case "contractManager": columns.Add(@"""ManagerId"""); break;
                case "errors": columns.Add(@"""Errors"""); break;
            }

            if (columns.Count == 0)
                return Enumerable.Empty<object>();

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""OriginationOps""")
                .Take(sort, offset, limit, x => "Id");

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            //TODO: optimize memory allocation
            var result = new object[rows.Count()];
            var j = 0;

            switch (field)
            {
                case "id":
                    foreach (var row in rows)
                        result[j++] = row.Id;
                    break;
                case "level":
                    foreach (var row in rows)
                        result[j++] = row.Level;
                    break;
                case "timestamp":
                    foreach (var row in rows)
                        result[j++] = row.Timestamp;
                    break;
                case "hash":
                    foreach (var row in rows)
                        result[j++] = row.OpHash;
                    break;
                case "initiator":
                    foreach (var row in rows)
                        result[j++] = row.InitiatorId != null ? await Accounts.GetAliasAsync(row.InitiatorId) : null;
                    break;
                case "sender":
                    foreach (var row in rows)
                        result[j++] = await Accounts.GetAliasAsync(row.SenderId);
                    break;
                case "counter":
                    foreach (var row in rows)
                        result[j++] = row.Counter;
                    break;
                case "nonce":
                    foreach (var row in rows)
                        result[j++] = row.Nonce;
                    break;
                case "gasLimit":
                    foreach (var row in rows)
                        result[j++] = row.GasLimit;
                    break;
                case "gasUsed":
                    foreach (var row in rows)
                        result[j++] = row.GasUsed;
                    break;
                case "storageLimit":
                    foreach (var row in rows)
                        result[j++] = row.StorageLimit;
                    break;
                case "storageUsed":
                    foreach (var row in rows)
                        result[j++] = row.StorageUsed;
                    break;
                case "bakerFee":
                    foreach (var row in rows)
                        result[j++] = row.BakerFee;
                    break;
                case "storageFee":
                    foreach (var row in rows)
                        result[j++] = row.StorageFee ?? 0;
                    break;
                case "allocationFee":
                    foreach (var row in rows)
                        result[j++] = row.AllocationFee ?? 0;
                    break;
                case "contractDelegate":
                    foreach (var row in rows)
                        result[j++] = row.DelegateId != null ? await Accounts.GetAliasAsync(row.DelegateId) : null;
                    break;
                case "contractBalance":
                    foreach (var row in rows)
                        result[j++] = row.Balance;
                    break;
                case "status":
                    foreach (var row in rows)
                        result[j++] = StatusToString(row.Status);
                    break;
                case "originatedContract":
                    foreach (var row in rows)
                    {
                        var contract = row.ContractId == null ? null : (RawContract)Accounts.Get((int)row.ContractId);
                        var contractMetadata = contract == null ? null : Accounts.GetMetadata(contract.Id);

                        result[j++] = contract == null ? null : new OriginatedContract
                        {
                            Alias = contractMetadata?.Alias,
                            Address = contract.Address,
                            Kind = contract.KindString
                        };
                    }
                    break;
                case "contractManager":
                    foreach (var row in rows)
                        result[j++] = row.ManagerId != null ? await Accounts.GetAliasAsync(row.ManagerId) : null;
                    break;
                case "errors":
                    foreach (var row in rows)
                        result[j++] = row.Errors != null ? OperationErrorSerializer.Deserialize(row.Errors) : null;
                    break;
            }

            return result;
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
            OffsetParameter offset,
            int limit)
        {
            var sql = new SqlBuilder(@"SELECT * FROM ""TransactionOps""")
                .Filter("InitiatorId", initiator, x => "TargetId")
                .Filter("SenderId", sender, x => "TargetId")
                .Filter("TargetId", target, x => x == "sender" ? "SenderId" : "InitiatorId")
                .Filter("Parameters", parameters)
                .Take(sort, offset, limit, x => x == "amount" ? "Amount" : "Id");

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

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

        public async Task<IEnumerable<object>> GetTransactions(
            AccountParameter initiator,
            AccountParameter sender,
            AccountParameter target,
            StringParameter parameters,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            string[] fields)
        {
            var columns = new HashSet<string>(fields.Length);
            foreach (var field in fields)
            {
                switch (field)
                {
                    case "id": columns.Add(@"""Id"""); break;
                    case "level": columns.Add(@"""Level"""); break;
                    case "timestamp": columns.Add(@"""Timestamp"""); break;
                    case "hash": columns.Add(@"""OpHash"""); break;
                    case "initiator": columns.Add(@"""InitiatorId"""); break;
                    case "sender": columns.Add(@"""SenderId"""); break;
                    case "counter": columns.Add(@"""Counter"""); break;
                    case "nonce": columns.Add(@"""Nonce"""); break;
                    case "gasLimit": columns.Add(@"""GasLimit"""); break;
                    case "gasUsed": columns.Add(@"""GasUsed"""); break;
                    case "storageLimit": columns.Add(@"""StorageLimit"""); break;
                    case "storageUsed": columns.Add(@"""StorageUsed"""); break;
                    case "bakerFee": columns.Add(@"""BakerFee"""); break;
                    case "storageFee": columns.Add(@"""StorageFee"""); break;
                    case "allocationFee": columns.Add(@"""AllocationFee"""); break;
                    case "target": columns.Add(@"""TargetId"""); break;
                    case "amount": columns.Add(@"""Amount"""); break;
                    case "parameters": columns.Add(@"""Parameters"""); break;
                    case "status": columns.Add(@"""Status"""); break;
                    case "errors": columns.Add(@"""Errors"""); break;
                    case "hasInternals": columns.Add(@"""InternalOperations"""); break;
                }
            }

            if (columns.Count == 0)
                return Enumerable.Empty<object>();

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""TransactionOps""")
                .Filter("InitiatorId", initiator, x => "TargetId")
                .Filter("SenderId", sender, x => "TargetId")
                .Filter("TargetId", target, x => x == "sender" ? "SenderId" : "InitiatorId")
                .Filter("Parameters", parameters)
                .Take(sort, offset, limit, x => x == "amount" ? "Amount" : "Id");

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            var result = new object[rows.Count()][];
            for (int i = 0; i < result.Length; i++)
                result[i] = new object[fields.Length];

            for (int i = 0, j = 0; i < fields.Length; j = 0, i++)
            {
                switch (fields[i])
                {
                    case "id":
                        foreach (var row in rows)
                            result[j++][i] = row.Id;
                        break;
                    case "level":
                        foreach (var row in rows)
                            result[j++][i] = row.Level;
                        break;
                    case "timestamp":
                        foreach (var row in rows)
                            result[j++][i] = row.Timestamp;
                        break;
                    case "hash":
                        foreach (var row in rows)
                            result[j++][i] = row.OpHash;
                        break;
                    case "initiator":
                        foreach (var row in rows)
                            result[j++][i] = row.InitiatorId != null ? await Accounts.GetAliasAsync(row.InitiatorId) : null;
                        break;
                    case "sender":
                        foreach (var row in rows)
                            result[j++][i] = await Accounts.GetAliasAsync(row.SenderId);
                        break;
                    case "counter":
                        foreach (var row in rows)
                            result[j++][i] = row.Counter;
                        break;
                    case "nonce":
                        foreach (var row in rows)
                            result[j++][i] = row.Nonce;
                        break;
                    case "gasLimit":
                        foreach (var row in rows)
                            result[j++][i] = row.GasLimit;
                        break;
                    case "gasUsed":
                        foreach (var row in rows)
                            result[j++][i] = row.GasUsed;
                        break;
                    case "storageLimit":
                        foreach (var row in rows)
                            result[j++][i] = row.StorageLimit;
                        break;
                    case "storageUsed":
                        foreach (var row in rows)
                            result[j++][i] = row.StorageUsed;
                        break;
                    case "bakerFee":
                        foreach (var row in rows)
                            result[j++][i] = row.BakerFee;
                        break;
                    case "storageFee":
                        foreach (var row in rows)
                            result[j++][i] = row.StorageFee ?? 0;
                        break;
                    case "allocationFee":
                        foreach (var row in rows)
                            result[j++][i] = row.AllocationFee ?? 0;
                        break;
                    case "target":
                        foreach (var row in rows)
                            result[j++][i] = row.TargetId != null ? await Accounts.GetAliasAsync(row.TargetId) : null;
                        break;
                    case "amount":
                        foreach (var row in rows)
                            result[j++][i] = row.Amount;
                        break;
                    case "parameters":
                        foreach (var row in rows)
                            result[j++][i] = row.Parameters;
                        break;
                    case "status":
                        foreach (var row in rows)
                            result[j++][i] = StatusToString(row.Status);
                        break;
                    case "errors":
                        foreach (var row in rows)
                            result[j++][i] = row.Errors != null ? OperationErrorSerializer.Deserialize(row.Errors) : null;
                        break;
                    case "hasInternals":
                        foreach (var row in rows)
                            result[j++][i] = row.InternalOperations > 0;
                        break;
                }
            }

            return result;
        }

        public async Task<IEnumerable<object>> GetTransactions(
            AccountParameter initiator,
            AccountParameter sender,
            AccountParameter target,
            StringParameter parameters,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            string field)
        {
            var columns = new HashSet<string>(1);
            switch (field)
            {
                case "id": columns.Add(@"""Id"""); break;
                case "level": columns.Add(@"""Level"""); break;
                case "timestamp": columns.Add(@"""Timestamp"""); break;
                case "hash": columns.Add(@"""OpHash"""); break;
                case "initiator": columns.Add(@"""InitiatorId"""); break;
                case "sender": columns.Add(@"""SenderId"""); break;
                case "counter": columns.Add(@"""Counter"""); break;
                case "nonce": columns.Add(@"""Nonce"""); break;
                case "gasLimit": columns.Add(@"""GasLimit"""); break;
                case "gasUsed": columns.Add(@"""GasUsed"""); break;
                case "storageLimit": columns.Add(@"""StorageLimit"""); break;
                case "storageUsed": columns.Add(@"""StorageUsed"""); break;
                case "bakerFee": columns.Add(@"""BakerFee"""); break;
                case "storageFee": columns.Add(@"""StorageFee"""); break;
                case "allocationFee": columns.Add(@"""AllocationFee"""); break;
                case "target": columns.Add(@"""TargetId"""); break;
                case "amount": columns.Add(@"""Amount"""); break;
                case "parameters": columns.Add(@"""Parameters"""); break;
                case "status": columns.Add(@"""Status"""); break;
                case "errors": columns.Add(@"""Errors"""); break;
                case "hasInternals": columns.Add(@"""InternalOperations"""); break;
            }

            if (columns.Count == 0)
                return Enumerable.Empty<object>();

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""TransactionOps""")
                .Filter("InitiatorId", initiator, x => "TargetId")
                .Filter("SenderId", sender, x => "TargetId")
                .Filter("TargetId", target, x => x == "sender" ? "SenderId" : "InitiatorId")
                .Filter("Parameters", parameters)
                .Take(sort, offset, limit, x => x == "amount" ? "Amount" : "Id");

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            //TODO: optimize memory allocation
            var result = new object[rows.Count()];
            var j = 0;

            switch (field)
            {
                case "id":
                    foreach (var row in rows)
                        result[j++] = row.Id;
                    break;
                case "level":
                    foreach (var row in rows)
                        result[j++] = row.Level;
                    break;
                case "timestamp":
                    foreach (var row in rows)
                        result[j++] = row.Timestamp;
                    break;
                case "hash":
                    foreach (var row in rows)
                        result[j++] = row.OpHash;
                    break;
                case "initiator":
                    foreach (var row in rows)
                        result[j++] = row.InitiatorId != null ? await Accounts.GetAliasAsync(row.InitiatorId) : null;
                    break;
                case "sender":
                    foreach (var row in rows)
                        result[j++] = await Accounts.GetAliasAsync(row.SenderId);
                    break;
                case "counter":
                    foreach (var row in rows)
                        result[j++] = row.Counter;
                    break;
                case "nonce":
                    foreach (var row in rows)
                        result[j++] = row.Nonce;
                    break;
                case "gasLimit":
                    foreach (var row in rows)
                        result[j++] = row.GasLimit;
                    break;
                case "gasUsed":
                    foreach (var row in rows)
                        result[j++] = row.GasUsed;
                    break;
                case "storageLimit":
                    foreach (var row in rows)
                        result[j++] = row.StorageLimit;
                    break;
                case "storageUsed":
                    foreach (var row in rows)
                        result[j++] = row.StorageUsed;
                    break;
                case "bakerFee":
                    foreach (var row in rows)
                        result[j++] = row.BakerFee;
                    break;
                case "storageFee":
                    foreach (var row in rows)
                        result[j++] = row.StorageFee ?? 0;
                    break;
                case "allocationFee":
                    foreach (var row in rows)
                        result[j++] = row.AllocationFee ?? 0;
                    break;
                case "target":
                    foreach (var row in rows)
                        result[j++] = row.TargetId != null ? await Accounts.GetAliasAsync(row.TargetId) : null;
                    break;
                case "amount":
                    foreach (var row in rows)
                        result[j++] = row.Amount;
                    break;
                case "parameters":
                    foreach (var row in rows)
                        result[j++] = row.Parameters;
                    break;
                case "status":
                    foreach (var row in rows)
                        result[j++] = StatusToString(row.Status);
                    break;
                case "errors":
                    foreach (var row in rows)
                        result[j++] = row.Errors != null ? OperationErrorSerializer.Deserialize(row.Errors) : null;
                    break;
                case "hasInternals":
                    foreach (var row in rows)
                        result[j++] = row.InternalOperations > 0;
                    break;
            }

            return result;
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

        public async Task<IEnumerable<RevealOperation>> GetReveals(SortParameter sort, OffsetParameter offset, int limit)
        {
            var sql = new SqlBuilder(@"SELECT * FROM ""RevealOps""")
                .Take(sort, offset, limit, x => "Id");

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

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

        public async Task<IEnumerable<object>> GetReveals(SortParameter sort, OffsetParameter offset, int limit, string[] fields)
        {
            var columns = new HashSet<string>(fields.Length);
            foreach (var field in fields)
            {
                switch (field)
                {
                    case "id": columns.Add(@"""Id"""); break;
                    case "level": columns.Add(@"""Level"""); break;
                    case "timestamp": columns.Add(@"""Timestamp"""); break;
                    case "hash": columns.Add(@"""OpHash"""); break;
                    case "sender": columns.Add(@"""SenderId"""); break;
                    case "counter": columns.Add(@"""Counter"""); break;
                    case "gasLimit": columns.Add(@"""GasLimit"""); break;
                    case "gasUsed": columns.Add(@"""GasUsed"""); break;
                    case "bakerFee": columns.Add(@"""BakerFee"""); break;
                    case "status": columns.Add(@"""Status"""); break;
                    case "errors": columns.Add(@"""Errors"""); break;
                }
            }

            if (columns.Count == 0)
                return Enumerable.Empty<object>();

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""RevealOps""")
                .Take(sort, offset, limit, x => "Id");

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            var result = new object[rows.Count()][];
            for (int i = 0; i < result.Length; i++)
                result[i] = new object[fields.Length];

            for (int i = 0, j = 0; i < fields.Length; j = 0, i++)
            {
                switch (fields[i])
                {
                    case "id":
                        foreach (var row in rows)
                            result[j++][i] = row.Id;
                        break;
                    case "level":
                        foreach (var row in rows)
                            result[j++][i] = row.Level;
                        break;
                    case "timestamp":
                        foreach (var row in rows)
                            result[j++][i] = row.Timestamp;
                        break;
                    case "hash":
                        foreach (var row in rows)
                            result[j++][i] = row.OpHash;
                        break;
                    case "sender":
                        foreach (var row in rows)
                            result[j++][i] = await Accounts.GetAliasAsync(row.SenderId);
                        break;
                    case "counter":
                        foreach (var row in rows)
                            result[j++][i] = row.Counter;
                        break;
                    case "gasLimit":
                        foreach (var row in rows)
                            result[j++][i] = row.GasLimit;
                        break;
                    case "gasUsed":
                        foreach (var row in rows)
                            result[j++][i] = row.GasUsed;
                        break;
                    case "bakerFee":
                        foreach (var row in rows)
                            result[j++][i] = row.BakerFee;
                        break;
                    case "status":
                        foreach (var row in rows)
                            result[j++][i] = StatusToString(row.Status);
                        break;
                    case "errors":
                        foreach (var row in rows)
                            result[j++][i] = row.Errors != null ? OperationErrorSerializer.Deserialize(row.Errors) : null;
                        break;
                }
            }

            return result;
        }

        public async Task<IEnumerable<object>> GetReveals(SortParameter sort, OffsetParameter offset, int limit, string field)
        {
            var columns = new HashSet<string>(1);
            switch (field)
            {
                case "id": columns.Add(@"""Id"""); break;
                case "level": columns.Add(@"""Level"""); break;
                case "timestamp": columns.Add(@"""Timestamp"""); break;
                case "hash": columns.Add(@"""OpHash"""); break;
                case "sender": columns.Add(@"""SenderId"""); break;
                case "counter": columns.Add(@"""Counter"""); break;
                case "gasLimit": columns.Add(@"""GasLimit"""); break;
                case "gasUsed": columns.Add(@"""GasUsed"""); break;
                case "bakerFee": columns.Add(@"""BakerFee"""); break;
                case "status": columns.Add(@"""Status"""); break;
                case "errors": columns.Add(@"""Errors"""); break;
            }

            if (columns.Count == 0)
                return Enumerable.Empty<object>();

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""RevealOps""")
                .Take(sort, offset, limit, x => "Id");

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            //TODO: optimize memory allocation
            var result = new object[rows.Count()];
            var j = 0;

            switch (field)
            {
                case "id":
                    foreach (var row in rows)
                        result[j++] = row.Id;
                    break;
                case "level":
                    foreach (var row in rows)
                        result[j++] = row.Level;
                    break;
                case "timestamp":
                    foreach (var row in rows)
                        result[j++] = row.Timestamp;
                    break;
                case "hash":
                    foreach (var row in rows)
                        result[j++] = row.OpHash;
                    break;
                case "sender":
                    foreach (var row in rows)
                        result[j++] = await Accounts.GetAliasAsync(row.SenderId);
                    break;
                case "counter":
                    foreach (var row in rows)
                        result[j++] = row.Counter;
                    break;
                case "gasLimit":
                    foreach (var row in rows)
                        result[j++] = row.GasLimit;
                    break;
                case "gasUsed":
                    foreach (var row in rows)
                        result[j++] = row.GasUsed;
                    break;
                case "bakerFee":
                    foreach (var row in rows)
                        result[j++] = row.BakerFee;
                    break;
                case "status":
                    foreach (var row in rows)
                        result[j++] = StatusToString(row.Status);
                    break;
                case "errors":
                    foreach (var row in rows)
                        result[j++] = row.Errors != null ? OperationErrorSerializer.Deserialize(row.Errors) : null;
                    break;
            }

            return result;
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

        public async Task<IEnumerable<MigrationOperation>> GetMigrations(SortParameter sort, OffsetParameter offset, int limit)
        {
            var sql = new SqlBuilder(@"SELECT * FROM ""MigrationOps""")
                .Take(sort, offset, limit, x => "Id");

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

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

        public async Task<IEnumerable<object>> GetMigrations(SortParameter sort, OffsetParameter offset, int limit, string[] fields)
        {
            var columns = new HashSet<string>(fields.Length);
            foreach (var field in fields)
            {
                switch (field)
                {
                    case "id": columns.Add(@"""Id"""); break;
                    case "level": columns.Add(@"""Level"""); break;
                    case "timestamp": columns.Add(@"""Timestamp"""); break;
                    case "account": columns.Add(@"""AccountId"""); break;
                    case "kind": columns.Add(@"""Kind"""); break;
                    case "balanceChange": columns.Add(@"""BalanceChange"""); break;
                }
            }

            if (columns.Count == 0)
                return Enumerable.Empty<object>();

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""MigrationOps""")
                .Take(sort, offset, limit, x => "Id");

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            var result = new object[rows.Count()][];
            for (int i = 0; i < result.Length; i++)
                result[i] = new object[fields.Length];

            for (int i = 0, j = 0; i < fields.Length; j = 0, i++)
            {
                switch (fields[i])
                {
                    case "id":
                        foreach (var row in rows)
                            result[j++][i] = row.Id;
                        break;
                    case "level":
                        foreach (var row in rows)
                            result[j++][i] = row.Level;
                        break;
                    case "timestamp":
                        foreach (var row in rows)
                            result[j++][i] = row.Timestamp;
                        break;
                    case "account":
                        foreach (var row in rows)
                            result[j++][i] = await Accounts.GetAliasAsync(row.AccountId);
                        break;
                    case "kind":
                        foreach (var row in rows)
                            result[j++][i] = MigrationKindToString(row.Kind);
                        break;
                    case "balanceChange":
                        foreach (var row in rows)
                            result[j++][i] = row.BalanceChange;
                        break;
                }
            }

            return result;
        }

        public async Task<IEnumerable<object>> GetMigrations(SortParameter sort, OffsetParameter offset, int limit, string field)
        {
            var columns = new HashSet<string>(1);
            switch (field)
            {
                case "id": columns.Add(@"""Id"""); break;
                case "level": columns.Add(@"""Level"""); break;
                case "timestamp": columns.Add(@"""Timestamp"""); break;
                case "account": columns.Add(@"""AccountId"""); break;
                case "kind": columns.Add(@"""Kind"""); break;
                case "balanceChange": columns.Add(@"""BalanceChange"""); break;
            }

            if (columns.Count == 0)
                return Enumerable.Empty<object>();

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""MigrationOps""")
                .Take(sort, offset, limit, x => "Id");

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            //TODO: optimize memory allocation
            var result = new object[rows.Count()];
            var j = 0;

            switch (field)
            {
                case "id":
                    foreach (var row in rows)
                        result[j++] = row.Id;
                    break;
                case "level":
                    foreach (var row in rows)
                        result[j++] = row.Level;
                    break;
                case "timestamp":
                    foreach (var row in rows)
                        result[j++] = row.Timestamp;
                    break;
                case "account":
                    foreach (var row in rows)
                        result[j++] = await Accounts.GetAliasAsync(row.AccountId);
                    break;
                case "kind":
                    foreach (var row in rows)
                        result[j++] = MigrationKindToString(row.Kind);
                    break;
                case "balanceChange":
                    foreach (var row in rows)
                        result[j++] = row.BalanceChange;
                    break;
            }

            return result;
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

        public async Task<IEnumerable<RevelationPenaltyOperation>> GetRevelationPenalties(SortParameter sort, OffsetParameter offset, int limit)
        {
            var sql = new SqlBuilder(@"SELECT * FROM ""RevelationPenaltyOps""")
                .Take(sort, offset, limit, x => "Id");

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

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

        public async Task<IEnumerable<object>> GetRevelationPenalties(SortParameter sort, OffsetParameter offset, int limit, string[] fields)
        {
            var columns = new HashSet<string>(fields.Length);
            foreach (var field in fields)
            {
                switch (field)
                {
                    case "id": columns.Add(@"""Id"""); break;
                    case "level": columns.Add(@"""Level"""); break;
                    case "timestamp": columns.Add(@"""Timestamp"""); break;
                    case "baker": columns.Add(@"""BakerId"""); break;
                    case "missedLevel": columns.Add(@"""MissedLevel"""); break;
                    case "lostReward": columns.Add(@"""LostReward"""); break;
                    case "lostFees": columns.Add(@"""LostFees"""); break;
                }
            }

            if (columns.Count == 0)
                return Enumerable.Empty<object>();

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""RevelationPenaltyOps""")
                .Take(sort, offset, limit, x => "Id");

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            var result = new object[rows.Count()][];
            for (int i = 0; i < result.Length; i++)
                result[i] = new object[fields.Length];

            for (int i = 0, j = 0; i < fields.Length; j = 0, i++)
            {
                switch (fields[i])
                {
                    case "id":
                        foreach (var row in rows)
                            result[j++][i] = row.Id;
                        break;
                    case "level":
                        foreach (var row in rows)
                            result[j++][i] = row.Level;
                        break;
                    case "timestamp":
                        foreach (var row in rows)
                            result[j++][i] = row.Timestamp;
                        break;
                    case "baker":
                        foreach (var row in rows)
                            result[j++][i] = await Accounts.GetAliasAsync(row.BakerId);
                        break;
                    case "missedLevel":
                        foreach (var row in rows)
                            result[j++][i] = row.MissedLevel;
                        break;
                    case "lostReward":
                        foreach (var row in rows)
                            result[j++][i] = row.LostReward;
                        break;
                    case "lostFees":
                        foreach (var row in rows)
                            result[j++][i] = row.LostFees;
                        break;
                }
            }

            return result;
        }

        public async Task<IEnumerable<object>> GetRevelationPenalties(SortParameter sort, OffsetParameter offset, int limit, string field)
        {
            var columns = new HashSet<string>(1);
            switch (field)
            {
                case "id": columns.Add(@"""Id"""); break;
                case "level": columns.Add(@"""Level"""); break;
                case "timestamp": columns.Add(@"""Timestamp"""); break;
                case "baker": columns.Add(@"""BakerId"""); break;
                case "missedLevel": columns.Add(@"""MissedLevel"""); break;
                case "lostReward": columns.Add(@"""LostReward"""); break;
                case "lostFees": columns.Add(@"""LostFees"""); break;
            }

            if (columns.Count == 0)
                return Enumerable.Empty<object>();

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""RevelationPenaltyOps""")
                .Take(sort, offset, limit, x => "Id");

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            //TODO: optimize memory allocation
            var result = new object[rows.Count()];
            var j = 0;

            switch (field)
            {
                case "id":
                    foreach (var row in rows)
                        result[j++] = row.Id;
                    break;
                case "level":
                    foreach (var row in rows)
                        result[j++] = row.Level;
                    break;
                case "timestamp":
                    foreach (var row in rows)
                        result[j++] = row.Timestamp;
                    break;
                case "baker":
                    foreach (var row in rows)
                        result[j++] = await Accounts.GetAliasAsync(row.BakerId);
                    break;
                case "missedLevel":
                    foreach (var row in rows)
                        result[j++] = row.MissedLevel;
                    break;
                case "lostReward":
                    foreach (var row in rows)
                        result[j++] = row.LostReward;
                    break;
                case "lostFees":
                    foreach (var row in rows)
                        result[j++] = row.LostFees;
                    break;
            }

            return result;
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

        public async Task<IEnumerable<BakingOperation>> GetBakings(SortParameter sort, OffsetParameter offset, int limit)
        {
            var sql = new SqlBuilder(@"SELECT ""Id"", ""Level"", ""Timestamp"", ""BakerId"", ""Hash"", ""Priority"", ""Reward"", ""Fees"" FROM ""Blocks""")
                .Filter(@"""BakerId"" IS NOT NULL")
                .Take(sort, offset, limit, x => "Id");

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

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

        public async Task<IEnumerable<object>> GetBakings(SortParameter sort, OffsetParameter offset, int limit, string[] fields)
        {
            var columns = new HashSet<string>(fields.Length);
            foreach (var field in fields)
            {
                switch (field)
                {
                    case "id": columns.Add(@"""Id"""); break;
                    case "level": columns.Add(@"""Level"""); break;
                    case "timestamp": columns.Add(@"""Timestamp"""); break;
                    case "baker": columns.Add(@"""BakerId"""); break;
                    case "block": columns.Add(@"""Hash"""); break;
                    case "priority": columns.Add(@"""Priority"""); break;
                    case "reward": columns.Add(@"""Reward"""); break;
                    case "fees": columns.Add(@"""Fees"""); break;
                }
            }

            if (columns.Count == 0)
                return Enumerable.Empty<object>();

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""Blocks""")
                .Filter(@"""BakerId"" IS NOT NULL")
                .Take(sort, offset, limit, x => "Id");

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            var result = new object[rows.Count()][];
            for (int i = 0; i < result.Length; i++)
                result[i] = new object[fields.Length];

            for (int i = 0, j = 0; i < fields.Length; j = 0, i++)
            {
                switch (fields[i])
                {
                    case "id":
                        foreach (var row in rows)
                            result[j++][i] = row.Id;
                        break;
                    case "level":
                        foreach (var row in rows)
                            result[j++][i] = row.Level;
                        break;
                    case "timestamp":
                        foreach (var row in rows)
                            result[j++][i] = row.Timestamp;
                        break;
                    case "baker":
                        foreach (var row in rows)
                            result[j++][i] = await Accounts.GetAliasAsync(row.BakerId);
                        break;
                    case "block":
                        foreach (var row in rows)
                            result[j++][i] = row.Hash;
                        break;
                    case "priority":
                        foreach (var row in rows)
                            result[j++][i] = row.Priority;
                        break;
                    case "reward":
                        foreach (var row in rows)
                            result[j++][i] = row.Reward;
                        break;
                    case "fees":
                        foreach (var row in rows)
                            result[j++][i] = row.Fees;
                        break;
                }
            }

            return result;
        }

        public async Task<IEnumerable<object>> GetBakings(SortParameter sort, OffsetParameter offset, int limit, string field)
        {
            var columns = new HashSet<string>(1);
            switch (field)
            {
                case "id": columns.Add(@"""Id"""); break;
                case "level": columns.Add(@"""Level"""); break;
                case "timestamp": columns.Add(@"""Timestamp"""); break;
                case "baker": columns.Add(@"""BakerId"""); break;
                case "block": columns.Add(@"""Hash"""); break;
                case "priority": columns.Add(@"""Priority"""); break;
                case "reward": columns.Add(@"""Reward"""); break;
                case "fees": columns.Add(@"""Fees"""); break;
            }

            if (columns.Count == 0)
                return Enumerable.Empty<object>();

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""Blocks""")
                .Filter(@"""BakerId"" IS NOT NULL")
                .Take(sort, offset, limit, x => "Id");

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            //TODO: optimize memory allocation
            var result = new object[rows.Count()];
            var j = 0;

            switch (field)
            {
                case "id":
                    foreach (var row in rows)
                        result[j++] = row.Id;
                    break;
                case "level":
                    foreach (var row in rows)
                        result[j++] = row.Level;
                    break;
                case "timestamp":
                    foreach (var row in rows)
                        result[j++] = row.Timestamp;
                    break;
                case "baker":
                    foreach (var row in rows)
                        result[j++] = await Accounts.GetAliasAsync(row.BakerId);
                    break;
                case "block":
                    foreach (var row in rows)
                        result[j++] = row.Hash;
                    break;
                case "priority":
                    foreach (var row in rows)
                        result[j++] = row.Priority;
                    break;
                case "reward":
                    foreach (var row in rows)
                        result[j++] = row.Reward;
                    break;
                case "fees":
                    foreach (var row in rows)
                        result[j++] = row.Fees;
                    break;
            }

            return result;
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
