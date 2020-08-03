using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Dapper;

using Tzkt.Api.Models;
using Tzkt.Api.Services.Cache;
using Tzkt.Api.Services.Metadata;

namespace Tzkt.Api.Repositories
{
    public class OperationRepository : DbConnection
    {
        readonly AccountsCache Accounts;
        readonly ProposalMetadataService Proposals;
        readonly QuotesCache Quotes;

        public OperationRepository(AccountsCache accounts, ProposalMetadataService proposals, QuotesCache quotes, IConfiguration config) : base(config)
        {
            Accounts = accounts;
            Proposals = proposals;
            Quotes = quotes;
        }

        #region operations
        public async Task<IEnumerable<Operation>> Get(string hash, Symbols quotes)
        {
            #region test manager operations
            var delegations = GetDelegations(hash, quotes);
            var originations = GetOriginations(hash, quotes);
            var transactions = GetTransactions(hash, quotes);
            var reveals = GetReveals(hash, quotes);

            await Task.WhenAll(delegations, originations, transactions, reveals);

            var managerOps = ((IEnumerable<Operation>)delegations.Result)
                .Concat(originations.Result)
                .Concat(transactions.Result)
                .Concat(reveals.Result);

            if (managerOps.Any())
                return managerOps.OrderBy(x => x.Id);
            #endregion

            #region less likely
            var activations = GetActivations(hash, quotes);
            var proposals = GetProposals(hash, quotes);
            var ballots = GetBallots(hash, quotes);

            await Task.WhenAll(activations, proposals, ballots);

            if (activations.Result.Any())
                return activations.Result;

            if (proposals.Result.Any())
                return proposals.Result;

            if (ballots.Result.Any())
                return ballots.Result;
            #endregion

            #region very unlikely
            var endorsements = GetEndorsements(hash, quotes);
            var dobleBaking = GetDoubleBakings(hash, quotes);
            var doubleEndorsing = GetDoubleEndorsings(hash, quotes);
            var nonceRevelation = GetNonceRevelations(hash, quotes);

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

        public async Task<IEnumerable<Operation>> Get(string hash, int counter, Symbols quotes)
        {
            var delegations = GetDelegations(hash, counter, quotes);
            var originations = GetOriginations(hash, counter, quotes);
            var transactions = GetTransactions(hash, counter, quotes);
            var reveals = GetReveals(hash, counter, quotes);

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

        public async Task<IEnumerable<Operation>> Get(string hash, int counter, int nonce, Symbols quotes)
        {
            var delegations = GetDelegations(hash, counter, nonce, quotes);
            var originations = GetOriginations(hash, counter, nonce, quotes);
            var transactions = GetTransactions(hash, counter, nonce, quotes);

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

        public async Task<IEnumerable<EndorsementOperation>> GetEndorsements(string hash, Symbols quotes)
        {
            var sql = @"
                SELECT      o.""Id"", o.""Level"", o.""Timestamp"", o.""DelegateId"", o.""Slots"", o.""Reward"", b.""Hash""
                FROM        ""EndorsementOps"" as o
                INNER JOIN  ""Blocks"" as b 
                        ON  b.""Level"" = o.""Level""
                WHERE       o.""OpHash"" = @hash::character(51)
                LIMIT       1";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { hash });

            return rows.Select(row => new EndorsementOperation
            {
                Id = row.Id,
                Level = row.Level,
                Block = row.Hash,
                Timestamp = row.Timestamp,
                Hash = hash,
                Delegate = Accounts.GetAlias(row.DelegateId),
                Slots = row.Slots,
                Rewards = row.Reward,
                Quote = Quotes.Get(quotes, row.Level)
            });
        }

        public async Task<IEnumerable<EndorsementOperation>> GetEndorsements(Block block, Symbols quotes)
        {
            var sql = @"
                SELECT      ""Id"", ""Timestamp"", ""OpHash"", ""DelegateId"", ""Slots"", ""Reward""
                FROM        ""EndorsementOps""
                WHERE       ""Level"" = @level
                ORDER BY    ""Id""";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { level = block.Level });

            return rows.Select(row => new EndorsementOperation
            {
                Id = row.Id,
                Level = block.Level,
                Block = block.Hash,
                Timestamp = row.Timestamp,
                Hash = row.OpHash,
                Delegate = Accounts.GetAlias(row.DelegateId),
                Slots = row.Slots,
                Rewards = row.Reward,
                Quote = Quotes.Get(quotes, block.Level)
            });
        }

        public async Task<IEnumerable<EndorsementOperation>> GetEndorsements(
            Int32Parameter level,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            Symbols quotes)
        {
            var sql = new SqlBuilder(@"SELECT o.*, b.""Hash"" FROM ""EndorsementOps"" AS o INNER JOIN ""Blocks"" as b ON b.""Level"" = o.""Level""")
                .FilterA(@"o.""Level""", level)
                .Take(sort, offset, limit, x => x == "level" ? ("Id", "Level") : ("Id", "Id"), "o");

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            return rows.Select(row => new EndorsementOperation
            {
                Id = row.Id,
                Level = row.Level,
                Block = row.Hash,
                Timestamp = row.Timestamp,
                Hash = row.OpHash,
                Delegate = Accounts.GetAlias(row.DelegateId),
                Slots = row.Slots,
                Rewards = row.Reward,
                Quote = Quotes.Get(quotes, row.Level)
            });
        }

        public async Task<object[][]> GetEndorsements(
            Int32Parameter level,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            string[] fields,
            Symbols quotes)
        {
            var columns = new HashSet<string>(fields.Length);
            var joins = new HashSet<string>(1);

            foreach (var field in fields)
            {
                switch (field)
                {
                    case "id": columns.Add(@"o.""Id"""); break;
                    case "level": columns.Add(@"o.""Level"""); break;
                    case "timestamp": columns.Add(@"o.""Timestamp"""); break;
                    case "hash": columns.Add(@"o.""OpHash"""); break;
                    case "delegate": columns.Add(@"o.""DelegateId"""); break;
                    case "slots": columns.Add(@"o.""Slots"""); break;
                    case "rewards": columns.Add(@"o.""Reward"""); break;
                    case "block":
                        columns.Add(@"b.""Hash""");
                        joins.Add(@"INNER JOIN ""Blocks"" as b ON b.""Level"" = o.""Level""");
                        break;
                    case "quote": columns.Add(@"o.""Level"""); break;
                }
            }

            if (columns.Count == 0)
                return Array.Empty<object[]>();

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""EndorsementOps"" as o {string.Join(' ', joins)}")
                .FilterA(@"o.""Level""", level)
                .Take(sort, offset, limit, x => x == "level" ? ("Id", "Level") : ("Id", "Id"), "o");

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
                    case "block":
                        foreach (var row in rows)
                            result[j++][i] = row.Hash;
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
                    case "quote":
                        foreach (var row in rows)
                            result[j++][i] = Quotes.Get(quotes, row.Level);
                        break;
                }
            }

            return result;
        }

        public async Task<object[]> GetEndorsements(
            Int32Parameter level,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            string field,
            Symbols quotes)
        {
            var columns = new HashSet<string>(1);
            var joins = new HashSet<string>(1);

            switch (field)
            {
                case "id": columns.Add(@"o.""Id"""); break;
                case "level": columns.Add(@"o.""Level"""); break;
                case "timestamp": columns.Add(@"o.""Timestamp"""); break;
                case "hash": columns.Add(@"o.""OpHash"""); break;
                case "delegate": columns.Add(@"o.""DelegateId"""); break;
                case "slots": columns.Add(@"o.""Slots"""); break;
                case "rewards": columns.Add(@"o.""Reward"""); break;
                case "block":
                    columns.Add(@"b.""Hash""");
                    joins.Add(@"INNER JOIN ""Blocks"" as b ON b.""Level"" = o.""Level""");
                    break;
                case "quote": columns.Add(@"o.""Level"""); break;
            }

            if (columns.Count == 0)
                return Array.Empty<object>();

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""EndorsementOps"" as o {string.Join(' ', joins)}")
                .FilterA(@"o.""Level""", level)
                .Take(sort, offset, limit, x => x == "level" ? ("Id", "Level") : ("Id", "Id"), "o");

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
                case "block":
                    foreach (var row in rows)
                        result[j++] = row.Hash;
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
                case "quote":
                    foreach (var row in rows)
                        result[j++] = Quotes.Get(quotes, row.Level);
                    break;
            }

            return result;
        }

        public async Task<IEnumerable<EndorsementOperation>> GetEndorsements(RawAccount account, SortMode sort, int offset, OffsetMode offsetMode, int limit, Symbols quotes)
        {
            var sql = $@"
                SELECT      o.""Id"", o.""Level"", o.""Timestamp"", o.""OpHash"", o.""Slots"", o.""Reward"", b.""Hash""
                FROM        ""EndorsementOps"" as o
                INNER JOIN  ""Blocks"" as b 
                        ON  b.""Level"" = o.""Level""
                WHERE       o.""DelegateId"" = @accountId
                {Pagination("o", sort, offset, offsetMode, limit)}";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { accountId = account.Id });
            
            return rows.Select(row => new EndorsementOperation
            {
                Id = row.Id,
                Level = row.Level,
                Block = row.Hash,
                Timestamp = row.Timestamp,
                Hash = row.OpHash,
                Delegate = Accounts.GetAlias(account.Id),
                Slots = row.Slots,
                Rewards = row.Reward,
                Quote = Quotes.Get(quotes, row.Level)
            });
        }

        public async Task<IEnumerable<EndorsementOperation>> GetEndorsements(RawAccount account, DateTime from, DateTime to, SortMode sort, int offset, OffsetMode offsetMode, int limit, Symbols quotes)
        {
            var sql = $@"
                SELECT      o.""Id"", o.""Level"", o.""Timestamp"", o.""OpHash"", o.""Slots"", o.""Reward"", b.""Hash""
                FROM        ""EndorsementOps"" as o
                INNER JOIN  ""Blocks"" as b 
                        ON  b.""Level"" = o.""Level""
                WHERE       o.""DelegateId"" = @accountId
                AND         o.""Timestamp"" >= @from
                AND         o.""Timestamp"" < @to
                {Pagination("o", sort, offset, offsetMode, limit)}";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { accountId = account.Id, from, to });

            return rows.Select(row => new EndorsementOperation
            {
                Id = row.Id,
                Level = row.Level,
                Block = row.Hash,
                Timestamp = row.Timestamp,
                Hash = row.OpHash,
                Delegate = Accounts.GetAlias(account.Id),
                Slots = row.Slots,
                Rewards = row.Reward,
                Quote = Quotes.Get(quotes, row.Level)
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

        public async Task<IEnumerable<BallotOperation>> GetBallots(string hash, Symbols quotes)
        {
            var sql = @"
                SELECT      o.""Id"", o.""Level"", o.""Timestamp"", o.""SenderId"", o.""Rolls"", o.""Vote"", b.""Hash"", proposal.""Hash"" as proposal,
                            period.""Code"", period.""Kind"", period.""StartLevel"", period.""EndLevel""
                FROM        ""BallotOps"" as o
                INNER JOIN  ""Blocks"" as b 
                        ON  b.""Level"" = o.""Level""
                INNER JOIN  ""Proposals"" as proposal
                        ON  proposal.""Id"" = o.""ProposalId""
                INNER JOIN  ""VotingPeriods"" as period
                        ON  period.""Id"" = o.""PeriodId""
                WHERE       o.""OpHash"" = @hash::character(51)
                LIMIT       1";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { hash });

            return rows.Select(row => new BallotOperation
            {
                Id = row.Id,
                Level = row.Level,
                Block = row.Hash,
                Timestamp = row.Timestamp,
                Hash = hash,
                Period = new PeriodInfo
                {
                    Id = row.Code,
                    Kind = PeriodToString(row.Kind),
                    StartLevel = row.StartLevel,
                    EndLevel = row.EndLevel
                },
                Proposal = new ProposalAlias
                {
                    Hash = row.proposal,
                    Alias = Proposals[row.proposal]?.Alias
                },
                Delegate = Accounts.GetAlias(row.SenderId),
                Rolls = row.Rolls,
                Vote = VoteToString(row.Vote),
                Quote = Quotes.Get(quotes, row.Level)
            });
        }

        public async Task<IEnumerable<BallotOperation>> GetBallots(Block block, Symbols quotes)
        {
            var sql = @"
                SELECT      o.""Id"", o.""Timestamp"", o.""OpHash"", o.""SenderId"", o.""Rolls"", o.""Vote"", proposal.""Hash"" as proposal,
                            period.""Code"", period.""Kind"", period.""StartLevel"", period.""EndLevel""
                FROM        ""BallotOps"" as o
                INNER JOIN  ""Proposals"" as proposal
                        ON  proposal.""Id"" = o.""ProposalId""
                INNER JOIN  ""VotingPeriods"" as period
                        ON  period.""Id"" = o.""PeriodId""
                WHERE       o.""Level"" = @level
                ORDER BY    o.""Id""";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { level = block.Level });

            return rows.Select(row => new BallotOperation
            {
                Id = row.Id,
                Level = block.Level,
                Block = block.Hash,
                Timestamp = row.Timestamp,
                Hash = row.OpHash,
                Period = new PeriodInfo
                {
                    Id = row.Code,
                    Kind = PeriodToString(row.Kind),
                    StartLevel = row.StartLevel,
                    EndLevel = row.EndLevel
                },
                Proposal = new ProposalAlias
                {
                    Hash = row.proposal,
                    Alias = Proposals[row.proposal]?.Alias
                },
                Delegate = Accounts.GetAlias(row.SenderId),
                Rolls = row.Rolls,
                Vote = VoteToString(row.Vote),
                Quote = Quotes.Get(quotes, block.Level)
            });
        }

        public async Task<IEnumerable<BallotOperation>> GetBallots(
            Int32Parameter level,
            Int32Parameter period,
            ProtocolParameter proposal,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            Symbols quotes)
        {
            var sql = new SqlBuilder(@"
                SELECT      o.""Id"", o.""Level"", o.""Timestamp"", o.""OpHash"", o.""SenderId"", o.""Rolls"", o.""Vote"", b.""Hash"", proposal.""Hash"" as proposal,
                            period.""Code"", period.""Kind"", period.""StartLevel"", period.""EndLevel""
                FROM        ""BallotOps"" as o
                INNER JOIN  ""Blocks"" as b ON b.""Level"" = o.""Level""
                INNER JOIN  ""Proposals"" as proposal ON proposal.""Id"" = o.""ProposalId""
                INNER JOIN  ""VotingPeriods"" as period ON period.""Id"" = o.""PeriodId""
                ")
                .FilterA(@"o.""Level""", level)
                .FilterA(@"period.""Code""", period)
                .FilterA(@"proposal.""Hash""", proposal)
                .Take(sort, offset, limit, x => x == "level" ? ("Id", "Level") : ("Id", "Id"), "o");

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            return rows.Select(row => new BallotOperation
            {
                Id = row.Id,
                Level = row.Level,
                Block = row.Hash,
                Timestamp = row.Timestamp,
                Hash = row.OpHash,
                Period = new PeriodInfo
                {
                    Id = row.Code,
                    Kind = PeriodToString(row.Kind),
                    StartLevel = row.StartLevel,
                    EndLevel = row.EndLevel
                },
                Proposal = new ProposalAlias
                {
                    Hash = row.proposal,
                    Alias = Proposals[row.proposal]?.Alias
                },
                Delegate = Accounts.GetAlias(row.SenderId),
                Rolls = row.Rolls,
                Vote = VoteToString(row.Vote),
                Quote = Quotes.Get(quotes, row.Level)
            });
        }

        public async Task<object[][]> GetBallots(
            Int32Parameter level,
            Int32Parameter period,
            ProtocolParameter proposal,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            string[] fields,
            Symbols quotes)
        {
            var columns = new HashSet<string>(fields.Length + 3);
            var joins = new HashSet<string>(3);

            foreach (var field in fields)
            {
                switch (field)
                {
                    case "id": columns.Add(@"o.""Id"""); break;
                    case "level": columns.Add(@"o.""Level"""); break;
                    case "timestamp": columns.Add(@"o.""Timestamp"""); break;
                    case "hash": columns.Add(@"o.""OpHash"""); break;
                    case "delegate": columns.Add(@"o.""SenderId"""); break;
                    case "rolls": columns.Add(@"o.""Rolls"""); break;
                    case "vote": columns.Add(@"o.""Vote"""); break;
                    case "proposal":
                        columns.Add(@"proposal.""Hash"" as proposal");
                        joins.Add(@"INNER JOIN ""Proposals"" as proposal ON proposal.""Id"" = o.""ProposalId""");
                        break;
                    case "period": 
                        columns.Add(@"period.""Code""");
                        columns.Add(@"period.""Kind""");
                        columns.Add(@"period.""StartLevel""");
                        columns.Add(@"period.""EndLevel""");
                        joins.Add(@"INNER JOIN ""VotingPeriods"" as period ON period.""Id"" = o.""PeriodId""");
                        break;
                    case "block":
                        columns.Add(@"b.""Hash""");
                        joins.Add(@"INNER JOIN ""Blocks"" as b ON b.""Level"" = o.""Level""");
                        break;
                    case "quote": columns.Add(@"o.""Level"""); break;
                }
            }

            if (period != null)
                joins.Add(@"INNER JOIN ""VotingPeriods"" as period ON period.""Id"" = o.""PeriodId""");

            if (proposal != null)
                joins.Add(@"INNER JOIN ""Proposals"" as proposal ON proposal.""Id"" = o.""ProposalId""");

            if (columns.Count == 0)
                return Array.Empty<object[]>();

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""BallotOps"" as o {string.Join(' ', joins)}")
                .FilterA(@"o.""Level""", level)
                .FilterA(@"period.""Code""", period)
                .FilterA(@"proposal.""Hash""", proposal)
                .Take(sort, offset, limit, x => x == "level" ? ("Id", "Level") : ("Id", "Id"), "o");

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
                    case "block":
                        foreach (var row in rows)
                            result[j++][i] = row.Hash;
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
                            result[j++][i] = new ProposalAlias
                            {
                                Hash = row.proposal,
                                Alias = Proposals[row.proposal]?.Alias
                            };
                        break;
                    case "delegate":
                        foreach (var row in rows)
                            result[j++][i] = await Accounts.GetAliasAsync(row.SenderId);
                        break;
                    case "rolls":
                        foreach (var row in rows)
                            result[j++][i] = row.Rolls;
                        break;
                    case "vote":
                        foreach (var row in rows)
                            result[j++][i] = VoteToString(row.Vote);
                        break;
                    case "quote":
                        foreach (var row in rows)
                            result[j++][i] = Quotes.Get(quotes, row.Level);
                        break;
                }
            }

            return result;
        }

        public async Task<object[]> GetBallots(
            Int32Parameter level,
            Int32Parameter period,
            ProtocolParameter proposal,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            string field,
            Symbols quotes)
        {
            var columns = new HashSet<string>(4);
            var joins = new HashSet<string>(3);

            switch (field)
            {
                case "id": columns.Add(@"o.""Id"""); break;
                case "level": columns.Add(@"o.""Level"""); break;
                case "timestamp": columns.Add(@"o.""Timestamp"""); break;
                case "hash": columns.Add(@"o.""OpHash"""); break;
                case "delegate": columns.Add(@"o.""SenderId"""); break;
                case "rolls": columns.Add(@"o.""Rolls"""); break;
                case "vote": columns.Add(@"o.""Vote"""); break;
                case "proposal":
                    columns.Add(@"proposal.""Hash"" as proposal");
                    joins.Add(@"INNER JOIN ""Proposals"" as proposal ON proposal.""Id"" = o.""ProposalId""");
                    break;
                case "period":
                    columns.Add(@"period.""Code""");
                    columns.Add(@"period.""Kind""");
                    columns.Add(@"period.""StartLevel""");
                    columns.Add(@"period.""EndLevel""");
                    joins.Add(@"INNER JOIN ""VotingPeriods"" as period ON period.""Id"" = o.""PeriodId""");
                    break;
                case "block":
                    columns.Add(@"b.""Hash""");
                    joins.Add(@"INNER JOIN ""Blocks"" as b ON b.""Level"" = o.""Level""");
                    break;
                case "quote": columns.Add(@"o.""Level"""); break;
            }

            if (period != null)
                joins.Add(@"INNER JOIN ""VotingPeriods"" as period ON period.""Id"" = o.""PeriodId""");

            if (proposal != null)
                joins.Add(@"INNER JOIN ""Proposals"" as proposal ON proposal.""Id"" = o.""ProposalId""");

            if (columns.Count == 0)
                return Array.Empty<object>();

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""BallotOps"" as o {string.Join(' ', joins)}")
                .FilterA(@"o.""Level""", level)
                .FilterA(@"period.""Code""", period)
                .FilterA(@"proposal.""Hash""", proposal)
                .Take(sort, offset, limit, x => x == "level" ? ("Id", "Level") : ("Id", "Id"), "o");

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
                case "block":
                    foreach (var row in rows)
                        result[j++] = row.Hash;
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
                        result[j++] = new ProposalAlias
                        {
                            Hash = row.proposal,
                            Alias = Proposals[row.proposal]?.Alias
                        };
                    break;
                case "delegate":
                    foreach (var row in rows)
                        result[j++] = await Accounts.GetAliasAsync(row.SenderId);
                    break;
                case "rolls":
                    foreach (var row in rows)
                        result[j++] = row.Rolls;
                    break;
                case "vote":
                    foreach (var row in rows)
                        result[j++] = VoteToString(row.Vote);
                    break;
                case "quote":
                    foreach (var row in rows)
                        result[j++] = Quotes.Get(quotes, row.Level);
                    break;
            }

            return result;
        }

        public async Task<IEnumerable<BallotOperation>> GetBallots(RawAccount account, SortMode sort, int offset, OffsetMode offsetMode, int limit, Symbols quotes)
        {
            var sql = $@"
                SELECT      o.""Id"", o.""Level"", o.""Timestamp"", o.""OpHash"", o.""Rolls"", o.""Vote"", b.""Hash"", proposal.""Hash"" as proposal,
                            period.""Code"", period.""Kind"", period.""StartLevel"", period.""EndLevel""
                FROM        ""BallotOps"" as o
                INNER JOIN  ""Blocks"" as b 
                        ON  b.""Level"" = o.""Level""
                INNER JOIN  ""Proposals"" as proposal
                        ON  proposal.""Id"" = o.""ProposalId""
                INNER JOIN  ""VotingPeriods"" as period
                        ON  period.""Id"" = o.""PeriodId""
                WHERE       o.""SenderId"" = @accountId
                {Pagination("o", sort, offset, offsetMode, limit)}";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { accountId = account.Id });

            return rows.Select(row => new BallotOperation
            {
                Id = row.Id,
                Level = row.Level,
                Block = row.Hash,
                Timestamp = row.Timestamp,
                Hash = row.OpHash,
                Period = new PeriodInfo
                {
                    Id = row.Code,
                    Kind = PeriodToString(row.Kind),
                    StartLevel = row.StartLevel,
                    EndLevel = row.EndLevel
                },
                Proposal = new ProposalAlias
                {
                    Hash = row.proposal,
                    Alias = Proposals[row.proposal]?.Alias
                },
                Delegate = Accounts.GetAlias(account.Id),
                Rolls = row.Rolls,
                Vote = VoteToString(row.Vote),
                Quote = Quotes.Get(quotes, row.Level)
            });
        }

        public async Task<IEnumerable<BallotOperation>> GetBallots(RawAccount account, DateTime from, DateTime to, SortMode sort, int offset, OffsetMode offsetMode, int limit, Symbols quotes)
        {
            var sql = $@"
                SELECT      o.""Id"", o.""Level"", o.""Timestamp"", o.""OpHash"", o.""Rolls"", o.""Vote"", b.""Hash"", proposal.""Hash"" as proposal,
                            period.""Code"", period.""Kind"", period.""StartLevel"", period.""EndLevel""
                FROM        ""BallotOps"" as o
                INNER JOIN  ""Blocks"" as b 
                        ON  b.""Level"" = o.""Level""
                INNER JOIN  ""Proposals"" as proposal
                        ON  proposal.""Id"" = o.""ProposalId""
                INNER JOIN  ""VotingPeriods"" as period
                        ON  period.""Id"" = o.""PeriodId""
                WHERE       o.""SenderId"" = @accountId
                AND         o.""Timestamp"" >= @from
                AND         o.""Timestamp"" < @to
                {Pagination("o", sort, offset, offsetMode, limit)}";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { accountId = account.Id, from, to });

            return rows.Select(row => new BallotOperation
            {
                Id = row.Id,
                Level = row.Level,
                Block = row.Hash,
                Timestamp = row.Timestamp,
                Hash = row.OpHash,
                Period = new PeriodInfo
                {
                    Id = row.Code,
                    Kind = PeriodToString(row.Kind),
                    StartLevel = row.StartLevel,
                    EndLevel = row.EndLevel
                },
                Proposal = new ProposalAlias
                {
                    Hash = row.proposal,
                    Alias = Proposals[row.proposal]?.Alias
                },
                Delegate = Accounts.GetAlias(account.Id),
                Rolls = row.Rolls,
                Vote = VoteToString(row.Vote),
                Quote = Quotes.Get(quotes, row.Level)
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

        public async Task<IEnumerable<ProposalOperation>> GetProposals(string hash, Symbols quotes)
        {
            var sql = @"
                SELECT      o.""Id"", o.""Level"", o.""Timestamp"", o.""SenderId"", o.""Rolls"", o.""Duplicated"", b.""Hash"", proposal.""Hash"" as proposal,
                            period.""Code"", period.""Kind"", period.""StartLevel"", period.""EndLevel""
                FROM        ""ProposalOps"" as o
                INNER JOIN  ""Blocks"" as b 
                        ON  b.""Level"" = o.""Level""
                INNER JOIN  ""Proposals"" as proposal
                        ON  proposal.""Id"" = o.""ProposalId""
                INNER JOIN  ""VotingPeriods"" as period
                        ON  period.""Id"" = o.""PeriodId""
                WHERE       o.""OpHash"" = @hash::character(51)
                ORDER BY    o.""Id""";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { hash });

            return rows.Select(row => new ProposalOperation
            {
                Id = row.Id,
                Level = row.Level,
                Block = row.Hash,
                Timestamp = row.Timestamp,
                Hash = hash,
                Rolls = row.Rolls,
                Duplicated = row.Duplicated,
                Period = new PeriodInfo
                {
                    Id = row.Code,
                    Kind = PeriodToString(row.Kind),
                    StartLevel = row.StartLevel,
                    EndLevel = row.EndLevel
                },
                Proposal = new ProposalAlias
                {
                    Hash = row.proposal,
                    Alias = Proposals[row.proposal]?.Alias
                },
                Delegate = Accounts.GetAlias(row.SenderId),
                Quote = Quotes.Get(quotes, row.Level)
            });
        }

        public async Task<IEnumerable<ProposalOperation>> GetProposals(Block block, Symbols quotes)
        {
            var sql = @"
                SELECT      o.""Id"", o.""Timestamp"", o.""OpHash"", o.""SenderId"", o.""Rolls"", o.""Duplicated"", proposal.""Hash"" as proposal,
                            period.""Code"", period.""Kind"", period.""StartLevel"", period.""EndLevel""
                FROM        ""ProposalOps"" as o
                INNER JOIN  ""Proposals"" as proposal
                        ON  proposal.""Id"" = o.""ProposalId""
                INNER JOIN  ""VotingPeriods"" as period
                        ON  period.""Id"" = o.""PeriodId""
                WHERE       o.""Level"" = @level
                ORDER BY    o.""Id""";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { level = block.Level });

            return rows.Select(row => new ProposalOperation
            {
                Id = row.Id,
                Level = block.Level,
                Block = block.Hash,
                Timestamp = row.Timestamp,
                Hash = row.OpHash,
                Rolls = row.Rolls,
                Duplicated = row.Duplicated,
                Period = new PeriodInfo
                {
                    Id = row.Code,
                    Kind = PeriodToString(row.Kind),
                    StartLevel = row.StartLevel,
                    EndLevel = row.EndLevel
                },
                Proposal = new ProposalAlias
                {
                    Hash = row.proposal,
                    Alias = Proposals[row.proposal]?.Alias
                },
                Delegate = Accounts.GetAlias(row.SenderId),
                Quote = Quotes.Get(quotes, block.Level)
            });
        }

        public async Task<IEnumerable<ProposalOperation>> GetProposals(
            Int32Parameter level,
            Int32Parameter period,
            ProtocolParameter proposal,
            BoolParameter duplicated,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            Symbols quotes)
        {
            var sql = new SqlBuilder(@"
                SELECT      o.""Id"", o.""Level"", o.""Timestamp"", o.""OpHash"", o.""SenderId"", o.""Rolls"", o.""Duplicated"", b.""Hash"", proposal.""Hash"" as proposal,
                            period.""Code"", period.""Kind"", period.""StartLevel"", period.""EndLevel""
                FROM        ""ProposalOps"" as o
                INNER JOIN  ""Blocks"" as b ON b.""Level"" = o.""Level""
                INNER JOIN  ""Proposals"" as proposal ON proposal.""Id"" = o.""ProposalId""
                INNER JOIN  ""VotingPeriods"" as period ON period.""Id"" = o.""PeriodId""
                ")
                .FilterA(@"o.""Level""", level)
                .FilterA(@"o.""Duplicated""", duplicated)
                .FilterA(@"period.""Code""", period)
                .FilterA(@"proposal.""Hash""", proposal)
                .Take(sort, offset, limit, x => x == "level" ? ("Id", "Level") : ("Id", "Id"), "o");

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            return rows.Select(row => new ProposalOperation
            {
                Id = row.Id,
                Level = row.Level,
                Block = row.Hash,
                Timestamp = row.Timestamp,
                Hash = row.OpHash,
                Rolls = row.Rolls,
                Duplicated = row.Duplicated,
                Period = new PeriodInfo
                {
                    Id = row.Code,
                    Kind = PeriodToString(row.Kind),
                    StartLevel = row.StartLevel,
                    EndLevel = row.EndLevel
                },
                Proposal = new ProposalAlias
                {
                    Hash = row.proposal,
                    Alias = Proposals[row.proposal]?.Alias
                },
                Delegate = Accounts.GetAlias(row.SenderId),
                Quote = Quotes.Get(quotes, row.Level)
            });
        }

        public async Task<object[][]> GetProposals(
            Int32Parameter level,
            Int32Parameter period,
            ProtocolParameter proposal,
            BoolParameter duplicated,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            string[] fields,
            Symbols quotes)
        {
            var columns = new HashSet<string>(fields.Length + 3);
            var joins = new HashSet<string>(3);

            foreach (var field in fields)
            {
                switch (field)
                {
                    case "id": columns.Add(@"o.""Id"""); break;
                    case "level": columns.Add(@"o.""Level"""); break;
                    case "timestamp": columns.Add(@"o.""Timestamp"""); break;
                    case "hash": columns.Add(@"o.""OpHash"""); break;
                    case "delegate": columns.Add(@"o.""SenderId"""); break;
                    case "rolls": columns.Add(@"o.""Rolls"""); break;
                    case "duplicated": columns.Add(@"o.""Duplicated"""); break;
                    case "proposal":
                        columns.Add(@"proposal.""Hash"" as proposal");
                        joins.Add(@"INNER JOIN ""Proposals"" as proposal ON proposal.""Id"" = o.""ProposalId""");
                        break;
                    case "period":
                        columns.Add(@"period.""Code""");
                        columns.Add(@"period.""Kind""");
                        columns.Add(@"period.""StartLevel""");
                        columns.Add(@"period.""EndLevel""");
                        joins.Add(@"INNER JOIN ""VotingPeriods"" as period ON period.""Id"" = o.""PeriodId""");
                        break;
                    case "block":
                        columns.Add(@"b.""Hash""");
                        joins.Add(@"INNER JOIN ""Blocks"" as b ON b.""Level"" = o.""Level""");
                        break;
                    case "quote": columns.Add(@"o.""Level"""); break;
                }
            }

            if (period != null)
                joins.Add(@"INNER JOIN ""VotingPeriods"" as period ON period.""Id"" = o.""PeriodId""");

            if (proposal != null)
                joins.Add(@"INNER JOIN ""Proposals"" as proposal ON proposal.""Id"" = o.""ProposalId""");

            if (columns.Count == 0)
                return Array.Empty<object[]>();

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""ProposalOps"" as o {string.Join(' ', joins)}")
                .FilterA(@"o.""Level""", level)
                .FilterA(@"o.""Duplicated""", duplicated)
                .FilterA(@"period.""Code""", period)
                .FilterA(@"proposal.""Hash""", proposal)
                .Take(sort, offset, limit, x => x == "level" ? ("Id", "Level") : ("Id", "Id"), "o");

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
                    case "block":
                        foreach (var row in rows)
                            result[j++][i] = row.Hash;
                        break;
                    case "timestamp":
                        foreach (var row in rows)
                            result[j++][i] = row.Timestamp;
                        break;
                    case "hash":
                        foreach (var row in rows)
                            result[j++][i] = row.OpHash;
                        break;
                    case "rolls":
                        foreach (var row in rows)
                            result[j++][i] = row.Rolls;
                        break;
                    case "duplicated":
                        foreach (var row in rows)
                            result[j++][i] = row.Duplicated;
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
                            result[j++][i] = new ProposalAlias
                            {
                                Hash = row.proposal,
                                Alias = Proposals[row.proposal]?.Alias
                            };
                        break;
                    case "delegate":
                        foreach (var row in rows)
                            result[j++][i] = await Accounts.GetAliasAsync(row.SenderId);
                        break;
                    case "quote":
                        foreach (var row in rows)
                            result[j++][i] = Quotes.Get(quotes, row.Level);
                        break;
                }
            }

            return result;
        }

        public async Task<object[]> GetProposals(
            Int32Parameter level,
            Int32Parameter period,
            ProtocolParameter proposal,
            BoolParameter duplicated,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            string field,
            Symbols quotes)
        {
            var columns = new HashSet<string>(4);
            var joins = new HashSet<string>(3);

            switch (field)
            {
                case "id": columns.Add(@"o.""Id"""); break;
                case "level": columns.Add(@"o.""Level"""); break;
                case "timestamp": columns.Add(@"o.""Timestamp"""); break;
                case "hash": columns.Add(@"o.""OpHash"""); break;
                case "delegate": columns.Add(@"o.""SenderId"""); break;
                case "rolls": columns.Add(@"o.""Rolls"""); break;
                case "duplicated": columns.Add(@"o.""Duplicated"""); break;
                case "proposal":
                    columns.Add(@"proposal.""Hash"" as proposal");
                    joins.Add(@"INNER JOIN ""Proposals"" as proposal ON proposal.""Id"" = o.""ProposalId""");
                    break;
                case "period":
                    columns.Add(@"period.""Code""");
                    columns.Add(@"period.""Kind""");
                    columns.Add(@"period.""StartLevel""");
                    columns.Add(@"period.""EndLevel""");
                    joins.Add(@"INNER JOIN ""VotingPeriods"" as period ON period.""Id"" = o.""PeriodId""");
                    break;
                case "block":
                    columns.Add(@"b.""Hash""");
                    joins.Add(@"INNER JOIN ""Blocks"" as b ON b.""Level"" = o.""Level""");
                    break;
                case "quote": columns.Add(@"o.""Level"""); break;
            }

            if (period != null)
                joins.Add(@"INNER JOIN ""VotingPeriods"" as period ON period.""Id"" = o.""PeriodId""");

            if (proposal != null)
                joins.Add(@"INNER JOIN ""Proposals"" as proposal ON proposal.""Id"" = o.""ProposalId""");

            if (columns.Count == 0)
                return Array.Empty<object>();

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""ProposalOps"" as o {string.Join(' ', joins)}")
                .FilterA(@"o.""Level""", level)
                .FilterA(@"o.""Duplicated""", duplicated)
                .FilterA(@"period.""Code""", period)
                .FilterA(@"proposal.""Hash""", proposal)
                .Take(sort, offset, limit, x => x == "level" ? ("Id", "Level") : ("Id", "Id"), "o");

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
                case "block":
                    foreach (var row in rows)
                        result[j++] = row.Hash;
                    break;
                case "timestamp":
                    foreach (var row in rows)
                        result[j++] = row.Timestamp;
                    break;
                case "hash":
                    foreach (var row in rows)
                        result[j++] = row.OpHash;
                    break;
                case "rolls":
                    foreach (var row in rows)
                        result[j++] = row.Rolls;
                    break;
                case "duplicated":
                    foreach (var row in rows)
                        result[j++] = row.Duplicated;
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
                        result[j++] = new ProposalAlias
                        {
                            Hash = row.proposal,
                            Alias = Proposals[row.proposal]?.Alias
                        };
                    break;
                case "delegate":
                    foreach (var row in rows)
                        result[j++] = await Accounts.GetAliasAsync(row.SenderId);
                    break;
                case "quote":
                    foreach (var row in rows)
                        result[j++] = Quotes.Get(quotes, row.Level);
                    break;
            }

            return result;
        }

        public async Task<IEnumerable<ProposalOperation>> GetProposals(RawAccount account, SortMode sort, int offset, OffsetMode offsetMode, int limit, Symbols quotes)
        {
            var sql = $@"
                SELECT      o.""Id"", o.""Level"", o.""Timestamp"", o.""OpHash"", o.""Rolls"", o.""Duplicated"", b.""Hash"", proposal.""Hash"" as proposal,
                            period.""Code"", period.""Kind"", period.""StartLevel"", period.""EndLevel""
                FROM        ""ProposalOps"" as o
                INNER JOIN  ""Blocks"" as b 
                        ON  b.""Level"" = o.""Level""
                INNER JOIN  ""Proposals"" as proposal
                        ON  proposal.""Id"" = o.""ProposalId""
                INNER JOIN  ""VotingPeriods"" as period
                        ON  period.""Id"" = o.""PeriodId""
                WHERE       o.""SenderId"" = @accountId
                {Pagination("o", sort, offset, offsetMode, limit)}";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { accountId = account.Id });

            return rows.Select(row => new ProposalOperation
            {
                Id = row.Id,
                Level = row.Level,
                Block = row.Hash,
                Timestamp = row.Timestamp,
                Hash = row.OpHash,
                Rolls = row.Rolls,
                Duplicated = row.Duplicated,
                Period = new PeriodInfo
                {
                    Id = row.Code,
                    Kind = PeriodToString(row.Kind),
                    StartLevel = row.StartLevel,
                    EndLevel = row.EndLevel
                },
                Proposal = new ProposalAlias
                {
                    Hash = row.proposal,
                    Alias = Proposals[row.proposal]?.Alias
                },
                Delegate = Accounts.GetAlias(account.Id),
                Quote = Quotes.Get(quotes, row.Level)
            });
        }

        public async Task<IEnumerable<ProposalOperation>> GetProposals(RawAccount account, DateTime from, DateTime to, SortMode sort, int offset, OffsetMode offsetMode, int limit, Symbols quotes)
        {
            var sql = $@"
                SELECT      o.""Id"", o.""Level"", o.""Timestamp"", o.""OpHash"", b.""Hash"", o.""Rolls"", o.""Duplicated"", proposal.""Hash"" as proposal,
                            period.""Code"", period.""Kind"", period.""StartLevel"", period.""EndLevel""
                FROM        ""ProposalOps"" as o
                INNER JOIN  ""Blocks"" as b 
                        ON  b.""Level"" = o.""Level""
                INNER JOIN  ""Proposals"" as proposal
                        ON  proposal.""Id"" = o.""ProposalId""
                INNER JOIN  ""VotingPeriods"" as period
                        ON  period.""Id"" = o.""PeriodId""
                WHERE       o.""SenderId"" = @accountId
                AND         o.""Timestamp"" >= @from
                AND         o.""Timestamp"" < @to
                {Pagination("o", sort, offset, offsetMode, limit)}";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { accountId = account.Id, from, to });

            return rows.Select(row => new ProposalOperation
            {
                Id = row.Id,
                Level = row.Level,
                Block = row.Hash,
                Timestamp = row.Timestamp,
                Hash = row.OpHash,
                Rolls = row.Rolls,
                Duplicated = row.Duplicated,
                Period = new PeriodInfo
                {
                    Id = row.Code,
                    Kind = PeriodToString(row.Kind),
                    StartLevel = row.StartLevel,
                    EndLevel = row.EndLevel
                },
                Proposal = new ProposalAlias
                {
                    Hash = row.proposal,
                    Alias = Proposals[row.proposal]?.Alias
                },
                Delegate = Accounts.GetAlias(account.Id),
                Quote = Quotes.Get(quotes, row.Level)
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

        public async Task<IEnumerable<ActivationOperation>> GetActivations(string hash, Symbols quotes)
        {
            var sql = @"
                SELECT      o.""Id"", o.""Level"", o.""Timestamp"", o.""AccountId"", o.""Balance"", b.""Hash""
                FROM        ""ActivationOps"" as o
                INNER JOIN  ""Blocks"" as b 
                        ON  b.""Level"" = o.""Level""
                WHERE       o.""OpHash"" = @hash::character(51)
                LIMIT       1";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { hash });

            return rows.Select(row => new ActivationOperation
            {
                Id = row.Id,
                Level = row.Level,
                Block = row.Hash,
                Timestamp = row.Timestamp,
                Hash = hash,
                Account = Accounts.GetAlias(row.AccountId),
                Balance = row.Balance,
                Quote = Quotes.Get(quotes, row.Level)
            });
        }

        public async Task<IEnumerable<ActivationOperation>> GetActivations(Block block, Symbols quotes)
        {
            var sql = @"
                SELECT      ""Id"", ""Timestamp"", ""OpHash"", ""AccountId"", ""Balance""
                FROM        ""ActivationOps""
                WHERE       ""Level"" = @level
                ORDER BY    ""Id""";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { level = block.Level });

            return rows.Select(row => new ActivationOperation
            {
                Id = row.Id,
                Level = block.Level,
                Block = block.Hash,
                Timestamp = row.Timestamp,
                Hash = row.OpHash,
                Account = Accounts.GetAlias(row.AccountId),
                Balance = row.Balance,
                Quote = Quotes.Get(quotes, block.Level)
            });
        }

        public async Task<IEnumerable<ActivationOperation>> GetActivations(
            Int32Parameter level,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            Symbols quotes)
        {
            var sql = new SqlBuilder(@"SELECT o.*, b.""Hash"" FROM ""ActivationOps"" AS o INNER JOIN ""Blocks"" as b ON b.""Level"" = o.""Level""")
                .FilterA(@"o.""Level""", level)
                .Take(sort, offset, limit, x => x switch
                {
                    "level" => ("Id", "Level"),
                    "balance" => ("Balance", "Balance"),
                    _ => ("Id", "Id")
                }, "o");

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            return rows.Select(row => new ActivationOperation
            {
                Id = row.Id,
                Level = row.Level,
                Block = row.Hash,
                Timestamp = row.Timestamp,
                Hash = row.OpHash,
                Account = Accounts.GetAlias(row.AccountId),
                Balance = row.Balance,
                Quote = Quotes.Get(quotes, row.Level)
            });
        }

        public async Task<object[][]> GetActivations(
            Int32Parameter level,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            string[] fields,
            Symbols quotes)
        {
            var columns = new HashSet<string>(fields.Length);
            var joins = new HashSet<string>(1);

            foreach (var field in fields)
            {
                switch (field)
                {
                    case "id": columns.Add(@"o.""Id"""); break;
                    case "level": columns.Add(@"o.""Level"""); break;
                    case "timestamp": columns.Add(@"o.""Timestamp"""); break;
                    case "hash": columns.Add(@"o.""OpHash"""); break;
                    case "account": columns.Add(@"o.""AccountId"""); break;
                    case "balance": columns.Add(@"o.""Balance"""); break;
                    case "block":
                        columns.Add(@"b.""Hash""");
                        joins.Add(@"INNER JOIN ""Blocks"" as b ON b.""Level"" = o.""Level""");
                        break;
                    case "quote": columns.Add(@"o.""Level"""); break;
                }
            }

            if (columns.Count == 0)
                return Array.Empty<object[]>();

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""ActivationOps"" as o {string.Join(' ', joins)}")
                .FilterA(@"o.""Level""", level)
                .Take(sort, offset, limit, x => x switch
                {
                    "level" => ("Id", "Level"),
                    "balance" => ("Balance", "Balance"),
                    _ => ("Id", "Id")
                }, "o");

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
                    case "block":
                        foreach (var row in rows)
                            result[j++][i] = row.Hash;
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
                    case "quote":
                        foreach (var row in rows)
                            result[j++][i] = Quotes.Get(quotes, row.Level);
                        break;
                }
            }

            return result;
        }

        public async Task<object[]> GetActivations(
            Int32Parameter level,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            string field,
            Symbols quotes)
        {
            var columns = new HashSet<string>(1);
            var joins = new HashSet<string>(1);

            switch (field)
            {
                case "id": columns.Add(@"o.""Id"""); break;
                case "level": columns.Add(@"o.""Level"""); break;
                case "timestamp": columns.Add(@"o.""Timestamp"""); break;
                case "hash": columns.Add(@"o.""OpHash"""); break;
                case "account": columns.Add(@"o.""AccountId"""); break;
                case "balance": columns.Add(@"o.""Balance"""); break;
                case "block":
                    columns.Add(@"b.""Hash""");
                    joins.Add(@"INNER JOIN ""Blocks"" as b ON b.""Level"" = o.""Level""");
                    break;
                case "quote": columns.Add(@"o.""Level"""); break;
            }

            if (columns.Count == 0)
                return Array.Empty<object>();

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""ActivationOps"" as o {string.Join(' ', joins)}")
                .FilterA(@"o.""Level""", level)
                .Take(sort, offset, limit, x => x switch
                {
                    "level" => ("Id", "Level"),
                    "balance" => ("Balance", "Balance"),
                    _ => ("Id", "Id")
                }, "o");

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
                case "block":
                    foreach (var row in rows)
                        result[j++] = row.Hash;
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
                case "quote":
                    foreach (var row in rows)
                        result[j++] = Quotes.Get(quotes, row.Level);
                    break;
            }

            return result;
        }

        public async Task<IEnumerable<ActivationOperation>> GetActivations(RawAccount account, SortMode sort, int offset, OffsetMode offsetMode, int limit, Symbols quotes)
        {
            var sql = $@"
                SELECT      o.""Id"", o.""Level"", o.""Timestamp"", o.""OpHash"", o.""Balance"", b.""Hash""
                FROM        ""ActivationOps"" as o
                INNER JOIN  ""Blocks"" as b 
                        ON  b.""Level"" = o.""Level""
                WHERE       o.""AccountId"" = @accountId
                {Pagination("o", sort, offset, offsetMode, limit)}";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { accountId = account.Id });

            return rows.Select(row => new ActivationOperation
            {
                Id = row.Id,
                Level = row.Level,
                Block = row.Hash,
                Timestamp = row.Timestamp,
                Hash = row.OpHash,
                Account = Accounts.GetAlias(account.Id),
                Balance = row.Balance,
                Quote = Quotes.Get(quotes, row.Level)
            });
        }

        public async Task<IEnumerable<ActivationOperation>> GetActivations(RawAccount account, DateTime from, DateTime to, SortMode sort, int offset, OffsetMode offsetMode, int limit, Symbols quotes)
        {
            var sql = $@"
                SELECT      o.""Id"", o.""Level"", o.""Timestamp"", o.""OpHash"", o.""Balance"", b.""Hash""
                FROM        ""ActivationOps"" as o
                INNER JOIN  ""Blocks"" as b 
                        ON  b.""Level"" = o.""Level""
                WHERE       o.""AccountId"" = @accountId
                AND         o.""Timestamp"" >= @from
                AND         o.""Timestamp"" < @to
                {Pagination("o", sort, offset, offsetMode, limit)}";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { accountId = account.Id, from, to });

            return rows.Select(row => new ActivationOperation
            {
                Id = row.Id,
                Level = row.Level,
                Block = row.Hash,
                Timestamp = row.Timestamp,
                Hash = row.OpHash,
                Account = Accounts.GetAlias(account.Id),
                Balance = row.Balance,
                Quote = Quotes.Get(quotes, row.Level)
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

        public async Task<IEnumerable<DoubleBakingOperation>> GetDoubleBakings(string hash, Symbols quotes)
        {
            var sql = @"
                SELECT      o.""Id"", o.""Level"", o.""Timestamp"", o.""AccusedLevel"", o.""AccuserId"", o.""AccuserReward"",
                            o.""OffenderId"", o.""OffenderLostDeposit"", o.""OffenderLostReward"", o.""OffenderLostFee"", b.""Hash""
                FROM        ""DoubleBakingOps"" as o
                INNER JOIN  ""Blocks"" as b 
                        ON  b.""Level"" = o.""Level""
                WHERE       o.""OpHash"" = @hash::character(51)
                LIMIT       1";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { hash });

            return rows.Select(row => new DoubleBakingOperation
            {
                Id = row.Id,
                Level = row.Level,
                Block = row.Hash,
                Timestamp = row.Timestamp,
                Hash = hash,
                AccusedLevel = row.AccusedLevel,
                Accuser = Accounts.GetAlias(row.AccuserId),
                AccuserRewards = row.AccuserReward,
                Offender = Accounts.GetAlias(row.OffenderId),
                OffenderLostDeposits = row.OffenderLostDeposit,
                OffenderLostRewards = row.OffenderLostReward,
                OffenderLostFees = row.OffenderLostFee,
                Quote = Quotes.Get(quotes, row.Level)
            });
        }

        public async Task<IEnumerable<DoubleBakingOperation>> GetDoubleBakings(Block block, Symbols quotes)
        {
            var sql = @"
                SELECT      ""Id"", ""Timestamp"", ""OpHash"", ""AccusedLevel"", ""AccuserId"", ""AccuserReward"",
                            ""OffenderId"", ""OffenderLostDeposit"", ""OffenderLostReward"", ""OffenderLostFee""
                FROM        ""DoubleBakingOps""
                WHERE       ""Level"" = @level
                ORDER BY    ""Id""";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { level = block.Level });

            return rows.Select(row => new DoubleBakingOperation
            {
                Id = row.Id,
                Level = block.Level,
                Block = block.Hash,
                Timestamp = row.Timestamp,
                Hash = row.OpHash,
                AccusedLevel = row.AccusedLevel,
                Accuser = Accounts.GetAlias(row.AccuserId),
                AccuserRewards = row.AccuserReward,
                Offender = Accounts.GetAlias(row.OffenderId),
                OffenderLostDeposits = row.OffenderLostDeposit,
                OffenderLostRewards = row.OffenderLostReward,
                OffenderLostFees = row.OffenderLostFee,
                Quote = Quotes.Get(quotes, block.Level)
            });
        }

        public async Task<IEnumerable<DoubleBakingOperation>> GetDoubleBakings(
            Int32Parameter level,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            Symbols quotes)
        {
            var sql = new SqlBuilder(@"SELECT o.*, b.""Hash"" FROM ""DoubleBakingOps"" AS o INNER JOIN ""Blocks"" as b ON b.""Level"" = o.""Level""")
                .FilterA(@"o.""Level""", level)
                .Take(sort, offset, limit, x => x switch
                {
                    "level" => ("Id", "Level"),
                    "accusedLevel" => ("AccusedLevel", "AccusedLevel"),
                    "accuserRewards" => ("AccuserReward", "AccuserReward"),
                    "offenderLostDeposits" => ("OffenderLostDeposit", "OffenderLostDeposit"),
                    "offenderLostRewards" => ("OffenderLostReward", "OffenderLostReward"),
                    "offenderLostFees" => ("OffenderLostFee", "OffenderLostFee"),
                    _ => ("Id", "Id")
                }, "o");

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            return rows.Select(row => new DoubleBakingOperation
            {
                Id = row.Id,
                Level = row.Level,
                Block = row.Hash,
                Timestamp = row.Timestamp,
                Hash = row.OpHash,
                AccusedLevel = row.AccusedLevel,
                Accuser = Accounts.GetAlias(row.AccuserId),
                AccuserRewards = row.AccuserReward,
                Offender = Accounts.GetAlias(row.OffenderId),
                OffenderLostDeposits = row.OffenderLostDeposit,
                OffenderLostRewards = row.OffenderLostReward,
                OffenderLostFees = row.OffenderLostFee,
                Quote = Quotes.Get(quotes, row.Level)
            });
        }

        public async Task<object[][]> GetDoubleBakings(
            Int32Parameter level,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            string[] fields,
            Symbols quotes)
        {
            var columns = new HashSet<string>(fields.Length);
            var joins = new HashSet<string>(1);

            foreach (var field in fields)
            {
                switch (field)
                {
                    case "id": columns.Add(@"o.""Id"""); break;
                    case "level": columns.Add(@"o.""Level"""); break;
                    case "timestamp": columns.Add(@"o.""Timestamp"""); break;
                    case "hash": columns.Add(@"o.""OpHash"""); break;
                    case "accusedLevel": columns.Add(@"o.""AccusedLevel"""); break;
                    case "accuser": columns.Add(@"o.""AccuserId"""); break;
                    case "accuserRewards": columns.Add(@"o.""AccuserReward"""); break;
                    case "offender": columns.Add(@"o.""OffenderId"""); break;
                    case "offenderLostDeposits": columns.Add(@"o.""OffenderLostDeposit"""); break;
                    case "offenderLostRewards": columns.Add(@"o.""OffenderLostReward"""); break;
                    case "offenderLostFees": columns.Add(@"o.""OffenderLostFee"""); break;
                    case "block":
                        columns.Add(@"b.""Hash""");
                        joins.Add(@"INNER JOIN ""Blocks"" as b ON b.""Level"" = o.""Level""");
                        break;
                    case "quote": columns.Add(@"o.""Level"""); break;
                }
            }

            if (columns.Count == 0)
                return Array.Empty<object[]>();

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""DoubleBakingOps"" as o {string.Join(' ', joins)}")
                .FilterA(@"o.""Level""", level)
                .Take(sort, offset, limit, x => x switch
                {
                    "level" => ("Id", "Level"),
                    "accusedLevel" => ("AccusedLevel", "AccusedLevel"),
                    "accuserRewards" => ("AccuserReward", "AccuserReward"),
                    "offenderLostDeposits" => ("OffenderLostDeposit", "OffenderLostDeposit"),
                    "offenderLostRewards" => ("OffenderLostReward", "OffenderLostReward"),
                    "offenderLostFees" => ("OffenderLostFee", "OffenderLostFee"),
                    _ => ("Id", "Id")
                }, "o");

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
                    case "block":
                        foreach (var row in rows)
                            result[j++][i] = row.Hash;
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
                    case "quote":
                        foreach (var row in rows)
                            result[j++][i] = Quotes.Get(quotes, row.Level);
                        break;
                }
            }

            return result;
        }

        public async Task<object[]> GetDoubleBakings(
            Int32Parameter level,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            string field,
            Symbols quotes)
        {
            var columns = new HashSet<string>(1);
            var joins = new HashSet<string>(1);

            switch (field)
            {
                case "id": columns.Add(@"o.""Id"""); break;
                case "level": columns.Add(@"o.""Level"""); break;
                case "timestamp": columns.Add(@"o.""Timestamp"""); break;
                case "hash": columns.Add(@"o.""OpHash"""); break;
                case "accusedLevel": columns.Add(@"o.""AccusedLevel"""); break;
                case "accuser": columns.Add(@"o.""AccuserId"""); break;
                case "accuserRewards": columns.Add(@"o.""AccuserReward"""); break;
                case "offender": columns.Add(@"o.""OffenderId"""); break;
                case "offenderLostDeposits": columns.Add(@"o.""OffenderLostDeposit"""); break;
                case "offenderLostRewards": columns.Add(@"o.""OffenderLostReward"""); break;
                case "offenderLostFees": columns.Add(@"o.""OffenderLostFee"""); break;
                case "block":
                    columns.Add(@"b.""Hash""");
                    joins.Add(@"INNER JOIN ""Blocks"" as b ON b.""Level"" = o.""Level""");
                    break;
                case "quote": columns.Add(@"o.""Level"""); break;
            }

            if (columns.Count == 0)
                return Array.Empty<object>();

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""DoubleBakingOps"" as o {string.Join(' ', joins)}")
                .FilterA(@"o.""Level""", level)
                .Take(sort, offset, limit, x => x switch
                {
                    "level" => ("Id", "Level"),
                    "accusedLevel" => ("AccusedLevel", "AccusedLevel"),
                    "accuserRewards" => ("AccuserReward", "AccuserReward"),
                    "offenderLostDeposits" => ("OffenderLostDeposit", "OffenderLostDeposit"),
                    "offenderLostRewards" => ("OffenderLostReward", "OffenderLostReward"),
                    "offenderLostFees" => ("OffenderLostFee", "OffenderLostFee"),
                    _ => ("Id", "Id")
                }, "o");

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
                case "block":
                    foreach (var row in rows)
                        result[j++] = row.Hash;
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
                case "quote":
                    foreach (var row in rows)
                        result[j++] = Quotes.Get(quotes, row.Level);
                    break;
            }

            return result;
        }

        public async Task<IEnumerable<DoubleBakingOperation>> GetDoubleBakings(RawAccount account, SortMode sort, int offset, OffsetMode offsetMode, int limit, Symbols quotes)
        {
            var sql = $@"
                SELECT      o.""Id"", o.""Level"", o.""Timestamp"", o.""OpHash"", o.""AccusedLevel"", o.""AccuserId"", o.""AccuserReward"",
                            o.""OffenderId"", o.""OffenderLostDeposit"", o.""OffenderLostReward"", o.""OffenderLostFee"", b.""Hash""
                FROM        ""DoubleBakingOps"" as o
                INNER JOIN  ""Blocks"" as b 
                        ON  b.""Level"" = o.""Level""
                WHERE      (o.""AccuserId"" = @accountId
                OR          o.""OffenderId"" = @accountId)
                {Pagination("o", sort, offset, offsetMode, limit)}";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { accountId = account.Id });

            return rows.Select(row => new DoubleBakingOperation
            {
                Id = row.Id,
                Level = row.Level,
                Block = row.Hash,
                Timestamp = row.Timestamp,
                Hash = row.OpHash,
                AccusedLevel = row.AccusedLevel,
                Accuser = Accounts.GetAlias(row.AccuserId),
                AccuserRewards = row.AccuserReward,
                Offender = Accounts.GetAlias(row.OffenderId),
                OffenderLostDeposits = row.OffenderLostDeposit,
                OffenderLostRewards = row.OffenderLostReward,
                OffenderLostFees = row.OffenderLostFee,
                Quote = Quotes.Get(quotes, row.Level)
            });
        }

        public async Task<IEnumerable<DoubleBakingOperation>> GetDoubleBakings(RawAccount account, DateTime from, DateTime to, SortMode sort, int offset, OffsetMode offsetMode, int limit, Symbols quotes)
        {
            var sql = $@"
                SELECT      o.""Id"", o.""Level"", o.""Timestamp"", o.""OpHash"", o.""AccusedLevel"", o.""AccuserId"", o.""AccuserReward"",
                            o.""OffenderId"", o.""OffenderLostDeposit"", o.""OffenderLostReward"", o.""OffenderLostFee"", b.""Hash""
                FROM        ""DoubleBakingOps"" as o
                INNER JOIN  ""Blocks"" as b 
                        ON  b.""Level"" = o.""Level""
                WHERE      (o.""AccuserId"" = @accountId
                OR          o.""OffenderId"" = @accountId)
                AND         o.""Timestamp"" >= @from
                AND         o.""Timestamp"" < @to
                {Pagination("o", sort, offset, offsetMode, limit)}";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { accountId = account.Id, from, to });

            return rows.Select(row => new DoubleBakingOperation
            {
                Id = row.Id,
                Level = row.Level,
                Block = row.Hash,
                Timestamp = row.Timestamp,
                Hash = row.OpHash,
                AccusedLevel = row.AccusedLevel,
                Accuser = Accounts.GetAlias(row.AccuserId),
                AccuserRewards = row.AccuserReward,
                Offender = Accounts.GetAlias(row.OffenderId),
                OffenderLostDeposits = row.OffenderLostDeposit,
                OffenderLostRewards = row.OffenderLostReward,
                OffenderLostFees = row.OffenderLostFee,
                Quote = Quotes.Get(quotes, row.Level)
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

        public async Task<IEnumerable<DoubleEndorsingOperation>> GetDoubleEndorsings(string hash, Symbols quotes)
        {
            var sql = @"
                SELECT      o.""Id"", o.""Level"", o.""Timestamp"", o.""AccusedLevel"", o.""AccuserId"", o.""AccuserReward"",
                            o.""OffenderId"", o.""OffenderLostDeposit"", o.""OffenderLostReward"", o.""OffenderLostFee"", b.""Hash""
                FROM        ""DoubleEndorsingOps"" as o
                INNER JOIN  ""Blocks"" as b 
                        ON  b.""Level"" = o.""Level""
                WHERE       o.""OpHash"" = @hash::character(51)
                LIMIT       1";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { hash });

            return rows.Select(row => new DoubleEndorsingOperation
            {
                Id = row.Id,
                Level = row.Level,
                Block = row.Hash,
                Timestamp = row.Timestamp,
                Hash = hash,
                AccusedLevel = row.AccusedLevel,
                Accuser = Accounts.GetAlias(row.AccuserId),
                AccuserRewards = row.AccuserReward,
                Offender = Accounts.GetAlias(row.OffenderId),
                OffenderLostDeposits = row.OffenderLostDeposit,
                OffenderLostRewards = row.OffenderLostReward,
                OffenderLostFees = row.OffenderLostFee,
                Quote = Quotes.Get(quotes, row.Level)
            });
        }

        public async Task<IEnumerable<DoubleEndorsingOperation>> GetDoubleEndorsings(Block block, Symbols quotes)
        {
            var sql = @"
                SELECT      ""Id"", ""Timestamp"", ""OpHash"", ""AccusedLevel"", ""AccuserId"", ""AccuserReward"",
                            ""OffenderId"", ""OffenderLostDeposit"", ""OffenderLostReward"", ""OffenderLostFee""
                FROM        ""DoubleEndorsingOps""
                WHERE       ""Level"" = @level
                ORDER BY    ""Id""";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { level = block.Level });

            return rows.Select(row => new DoubleEndorsingOperation
            {
                Id = row.Id,
                Level = block.Level,
                Block = block.Hash,
                Timestamp = row.Timestamp,
                Hash = row.OpHash,
                AccusedLevel = row.AccusedLevel,
                Accuser = Accounts.GetAlias(row.AccuserId),
                AccuserRewards = row.AccuserReward,
                Offender = Accounts.GetAlias(row.OffenderId),
                OffenderLostDeposits = row.OffenderLostDeposit,
                OffenderLostRewards = row.OffenderLostReward,
                OffenderLostFees = row.OffenderLostFee,
                Quote = Quotes.Get(quotes, block.Level)
            });
        }

        public async Task<IEnumerable<DoubleEndorsingOperation>> GetDoubleEndorsings(
            Int32Parameter level,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            Symbols quotes)
        {
            var sql = new SqlBuilder(@"SELECT o.*, b.""Hash"" FROM ""DoubleEndorsingOps"" AS o INNER JOIN ""Blocks"" as b ON b.""Level"" = o.""Level""")
                .FilterA(@"o.""Level""", level)
                .Take(sort, offset, limit, x => x switch
                {
                    "level" => ("Id", "Level"),
                    "accusedLevel" => ("AccusedLevel", "AccusedLevel"),
                    "accuserRewards" => ("AccuserReward", "AccuserReward"),
                    "offenderLostDeposits" => ("OffenderLostDeposit", "OffenderLostDeposit"),
                    "offenderLostRewards" => ("OffenderLostReward", "OffenderLostReward"),
                    "offenderLostFees" => ("OffenderLostFee", "OffenderLostFee"),
                    _ => ("Id", "Id")
                }, "o");

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            return rows.Select(row => new DoubleEndorsingOperation
            {
                Id = row.Id,
                Level = row.Level,
                Block = row.Hash,
                Timestamp = row.Timestamp,
                Hash = row.OpHash,
                AccusedLevel = row.AccusedLevel,
                Accuser = Accounts.GetAlias(row.AccuserId),
                AccuserRewards = row.AccuserReward,
                Offender = Accounts.GetAlias(row.OffenderId),
                OffenderLostDeposits = row.OffenderLostDeposit,
                OffenderLostRewards = row.OffenderLostReward,
                OffenderLostFees = row.OffenderLostFee,
                Quote = Quotes.Get(quotes, row.Level)
            });
        }

        public async Task<object[][]> GetDoubleEndorsings(
            Int32Parameter level,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            string[] fields,
            Symbols quotes)
        {
            var columns = new HashSet<string>(fields.Length);
            var joins = new HashSet<string>(1);

            foreach (var field in fields)
            {
                switch (field)
                {
                    case "id": columns.Add(@"o.""Id"""); break;
                    case "level": columns.Add(@"o.""Level"""); break;
                    case "timestamp": columns.Add(@"o.""Timestamp"""); break;
                    case "hash": columns.Add(@"o.""OpHash"""); break;
                    case "accusedLevel": columns.Add(@"o.""AccusedLevel"""); break;
                    case "accuser": columns.Add(@"o.""AccuserId"""); break;
                    case "accuserRewards": columns.Add(@"o.""AccuserReward"""); break;
                    case "offender": columns.Add(@"o.""OffenderId"""); break;
                    case "offenderLostDeposits": columns.Add(@"o.""OffenderLostDeposit"""); break;
                    case "offenderLostRewards": columns.Add(@"o.""OffenderLostReward"""); break;
                    case "offenderLostFees": columns.Add(@"o.""OffenderLostFee"""); break;
                    case "block":
                        columns.Add(@"b.""Hash""");
                        joins.Add(@"INNER JOIN ""Blocks"" as b ON b.""Level"" = o.""Level""");
                        break;
                    case "quote": columns.Add(@"o.""Level"""); break;
                }
            }

            if (columns.Count == 0)
                return Array.Empty<object[]>();

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""DoubleEndorsingOps"" as o {string.Join(' ', joins)}")
                .FilterA(@"o.""Level""", level)
                .Take(sort, offset, limit, x => x switch
                {
                    "level" => ("Id", "Level"),
                    "accusedLevel" => ("AccusedLevel", "AccusedLevel"),
                    "accuserRewards" => ("AccuserReward", "AccuserReward"),
                    "offenderLostDeposits" => ("OffenderLostDeposit", "OffenderLostDeposit"),
                    "offenderLostRewards" => ("OffenderLostReward", "OffenderLostReward"),
                    "offenderLostFees" => ("OffenderLostFee", "OffenderLostFee"),
                    _ => ("Id", "Id")
                }, "o");

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
                    case "block":
                        foreach (var row in rows)
                            result[j++][i] = row.Hash;
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
                    case "quote":
                        foreach (var row in rows)
                            result[j++][i] = Quotes.Get(quotes, row.Level);
                        break;
                }
            }

            return result;
        }

        public async Task<object[]> GetDoubleEndorsings(
            Int32Parameter level,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            string field,
            Symbols quotes)
        {
            var columns = new HashSet<string>(1);
            var joins = new HashSet<string>(1);

            switch (field)
            {
                case "id": columns.Add(@"o.""Id"""); break;
                case "level": columns.Add(@"o.""Level"""); break;
                case "timestamp": columns.Add(@"o.""Timestamp"""); break;
                case "hash": columns.Add(@"o.""OpHash"""); break;
                case "accusedLevel": columns.Add(@"o.""AccusedLevel"""); break;
                case "accuser": columns.Add(@"o.""AccuserId"""); break;
                case "accuserRewards": columns.Add(@"o.""AccuserReward"""); break;
                case "offender": columns.Add(@"o.""OffenderId"""); break;
                case "offenderLostDeposits": columns.Add(@"o.""OffenderLostDeposit"""); break;
                case "offenderLostRewards": columns.Add(@"o.""OffenderLostReward"""); break;
                case "offenderLostFees": columns.Add(@"o.""OffenderLostFee"""); break;
                case "block":
                    columns.Add(@"b.""Hash""");
                    joins.Add(@"INNER JOIN ""Blocks"" as b ON b.""Level"" = o.""Level""");
                    break;
                case "quote": columns.Add(@"o.""Level"""); break;
            }

            if (columns.Count == 0)
                return Array.Empty<object>();

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""DoubleEndorsingOps"" as o {string.Join(' ', joins)}")
                .FilterA(@"o.""Level""", level)
                .Take(sort, offset, limit, x => x switch
                {
                    "level" => ("Id", "Level"),
                    "accusedLevel" => ("AccusedLevel", "AccusedLevel"),
                    "accuserRewards" => ("AccuserReward", "AccuserReward"),
                    "offenderLostDeposits" => ("OffenderLostDeposit", "OffenderLostDeposit"),
                    "offenderLostRewards" => ("OffenderLostReward", "OffenderLostReward"),
                    "offenderLostFees" => ("OffenderLostFee", "OffenderLostFee"),
                    _ => ("Id", "Id")
                }, "o");

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
                case "block":
                    foreach (var row in rows)
                        result[j++] = row.Hash;
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
                case "quote":
                    foreach (var row in rows)
                        result[j++] = Quotes.Get(quotes, row.Level);
                    break;
            }

            return result;
        }

        public async Task<IEnumerable<DoubleEndorsingOperation>> GetDoubleEndorsings(RawAccount account, SortMode sort, int offset, OffsetMode offsetMode, int limit, Symbols quotes)
        {
            var sql = $@"
                SELECT      o.""Id"", o.""Level"", o.""Timestamp"", o.""OpHash"", o.""AccusedLevel"", o.""AccuserId"", o.""AccuserReward"",
                            o.""OffenderId"", o.""OffenderLostDeposit"", o.""OffenderLostReward"", o.""OffenderLostFee"", b.""Hash""
                FROM        ""DoubleEndorsingOps"" as o
                INNER JOIN  ""Blocks"" as b 
                        ON  b.""Level"" = o.""Level""
                WHERE      (o.""AccuserId"" = @accountId
                OR          o.""OffenderId"" = @accountId)
                {Pagination("o", sort, offset, offsetMode, limit)}";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { accountId = account.Id });

            return rows.Select(row => new DoubleEndorsingOperation
            {
                Id = row.Id,
                Level = row.Level,
                Block = row.Hash,
                Timestamp = row.Timestamp,
                Hash = row.OpHash,
                AccusedLevel = row.AccusedLevel,
                Accuser = Accounts.GetAlias(row.AccuserId),
                AccuserRewards = row.AccuserReward,
                Offender = Accounts.GetAlias(row.OffenderId),
                OffenderLostDeposits = row.OffenderLostDeposit,
                OffenderLostRewards = row.OffenderLostReward,
                OffenderLostFees = row.OffenderLostFee,
                Quote = Quotes.Get(quotes, row.Level)
            });
        }

        public async Task<IEnumerable<DoubleEndorsingOperation>> GetDoubleEndorsings(RawAccount account, DateTime from, DateTime to, SortMode sort, int offset, OffsetMode offsetMode, int limit, Symbols quotes)
        {
            var sql = $@"
                SELECT      o.""Id"", o.""Level"", o.""Timestamp"", o.""OpHash"", o.""AccusedLevel"", o.""AccuserId"", o.""AccuserReward"",
                            o.""OffenderId"", o.""OffenderLostDeposit"", o.""OffenderLostReward"", o.""OffenderLostFee"", b.""Hash""
                FROM        ""DoubleEndorsingOps"" as o
                INNER JOIN  ""Blocks"" as b 
                        ON  b.""Level"" = o.""Level""
                WHERE      (o.""AccuserId"" = @accountId
                OR          o.""OffenderId"" = @accountId)
                AND         o.""Timestamp"" >= @from
                AND         o.""Timestamp"" < @to
                {Pagination("o", sort, offset, offsetMode, limit)}";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { accountId = account.Id, from, to });

            return rows.Select(row => new DoubleEndorsingOperation
            {
                Id = row.Id,
                Level = row.Level,
                Block = row.Hash,
                Timestamp = row.Timestamp,
                Hash = row.OpHash,
                AccusedLevel = row.AccusedLevel,
                Accuser = Accounts.GetAlias(row.AccuserId),
                AccuserRewards = row.AccuserReward,
                Offender = Accounts.GetAlias(row.OffenderId),
                OffenderLostDeposits = row.OffenderLostDeposit,
                OffenderLostRewards = row.OffenderLostReward,
                OffenderLostFees = row.OffenderLostFee,
                Quote = Quotes.Get(quotes, row.Level)
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

        public async Task<IEnumerable<NonceRevelationOperation>> GetNonceRevelations(string hash, Symbols quotes)
        {
            var sql = @"
                SELECT      o.""Id"", o.""Level"", o.""Timestamp"", o.""BakerId"", o.""SenderId"", o.""RevealedLevel"", b.""Hash""
                FROM        ""NonceRevelationOps"" as o
                INNER JOIN  ""Blocks"" as b 
                        ON  b.""Level"" = o.""Level""
                WHERE       o.""OpHash"" = @hash::character(51)
                LIMIT       1";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { hash });

            return rows.Select(row => new NonceRevelationOperation
            {
                Id = row.Id,
                Level = row.Level,
                Block = row.Hash,
                Timestamp = row.Timestamp,
                Hash = hash,
                Baker = Accounts.GetAlias(row.BakerId),
                BakerRewards = 125_000,
                Sender = Accounts.GetAlias(row.SenderId),
                RevealedLevel = row.RevealedLevel,
                Quote = Quotes.Get(quotes, row.Level)
            });
        }

        public async Task<IEnumerable<NonceRevelationOperation>> GetNonceRevelations(Block block, Symbols quotes)
        {
            var sql = @"
                SELECT    ""Id"", ""Timestamp"", ""OpHash"", ""BakerId"", ""SenderId"", ""RevealedLevel""
                FROM      ""NonceRevelationOps""
                WHERE     ""Level"" = @level
                ORDER BY  ""Id""";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { level = block.Level });

            return rows.Select(row => new NonceRevelationOperation
            {
                Id = row.Id,
                Level = block.Level,
                Block = block.Hash,
                Timestamp = row.Timestamp,
                Hash = row.OpHash,
                Baker = Accounts.GetAlias(row.BakerId),
                BakerRewards = 125_000,
                Sender = Accounts.GetAlias(row.SenderId),
                RevealedLevel = row.RevealedLevel,
                Quote = Quotes.Get(quotes, block.Level)
            });
        }

        public async Task<IEnumerable<NonceRevelationOperation>> GetNonceRevelations(
            Int32Parameter level,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            Symbols quotes)
        {
            var sql = new SqlBuilder(@"SELECT o.*, b.""Hash"" FROM ""NonceRevelationOps"" AS o INNER JOIN ""Blocks"" as b ON b.""Level"" = o.""Level""")
                .FilterA(@"o.""Level""", level)
                .Take(sort, offset, limit, x => x switch
                {
                    "level" => ("Id", "Level"),
                    "revealedLevel" => ("RevealedLevel", "RevealedLevel"),
                    _ => ("Id", "Id")
                }, "o");

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            return rows.Select(row => new NonceRevelationOperation
            {
                Id = row.Id,
                Level = row.Level,
                Block = row.Hash,
                Timestamp = row.Timestamp,
                Hash = row.OpHash,
                Baker = Accounts.GetAlias(row.BakerId),
                BakerRewards = 125_000,
                Sender = Accounts.GetAlias(row.SenderId),
                RevealedLevel = row.RevealedLevel,
                Quote = Quotes.Get(quotes, row.Level)
            });
        }

        public async Task<object[][]> GetNonceRevelations(
            Int32Parameter level,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            string[] fields,
            Symbols quotes)
        {
            var columns = new HashSet<string>(fields.Length);
            var joins = new HashSet<string>(1);

            foreach (var field in fields)
            {
                switch (field)
                {
                    case "id": columns.Add(@"o.""Id"""); break;
                    case "level": columns.Add(@"o.""Level"""); break;
                    case "timestamp": columns.Add(@"o.""Timestamp"""); break;
                    case "hash": columns.Add(@"o.""OpHash"""); break;
                    case "baker": columns.Add(@"o.""BakerId"""); break;
                    case "sender": columns.Add(@"o.""SenderId"""); break;
                    case "revealedLevel": columns.Add(@"o.""RevealedLevel"""); break;
                    case "block":
                        columns.Add(@"b.""Hash""");
                        joins.Add(@"INNER JOIN ""Blocks"" as b ON b.""Level"" = o.""Level""");
                        break;
                    case "quote": columns.Add(@"o.""Level"""); break;
                }
            }

            if (columns.Count == 0)
                return Array.Empty<object[]>();

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""NonceRevelationOps"" as o {string.Join(' ', joins)}")
                .FilterA(@"o.""Level""", level)
                .Take(sort, offset, limit, x => x switch
                {
                    "level" => ("Id", "Level"),
                    "revealedLevel" => ("RevealedLevel", "RevealedLevel"),
                    _ => ("Id", "Id")
                }, "o");

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
                    case "block":
                        foreach (var row in rows)
                            result[j++][i] = row.Hash;
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
                    case "quote":
                        foreach (var row in rows)
                            result[j++][i] = Quotes.Get(quotes, row.Level);
                        break;
                }
            }

            return result;
        }

        public async Task<object[]> GetNonceRevelations(
            Int32Parameter level,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            string field,
            Symbols quotes)
        {
            var columns = new HashSet<string>(1);
            var joins = new HashSet<string>(1);

            switch (field)
            {
                case "id": columns.Add(@"o.""Id"""); break;
                case "level": columns.Add(@"o.""Level"""); break;
                case "timestamp": columns.Add(@"o.""Timestamp"""); break;
                case "hash": columns.Add(@"o.""OpHash"""); break;
                case "baker": columns.Add(@"o.""BakerId"""); break;
                case "sender": columns.Add(@"o.""SenderId"""); break;
                case "revealedLevel": columns.Add(@"o.""RevealedLevel"""); break;
                case "block":
                    columns.Add(@"b.""Hash""");
                    joins.Add(@"INNER JOIN ""Blocks"" as b ON b.""Level"" = o.""Level""");
                    break;
                case "quote": columns.Add(@"o.""Level"""); break;
            }

            if (columns.Count == 0)
                return Array.Empty<object>();

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""NonceRevelationOps"" as o {string.Join(' ', joins)}")
                .FilterA(@"o.""Level""", level)
                .Take(sort, offset, limit, x => x switch
                {
                    "level" => ("Id", "Level"),
                    "revealedLevel" => ("RevealedLevel", "RevealedLevel"),
                    _ => ("Id", "Id")
                }, "o");

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
                case "block":
                    foreach (var row in rows)
                        result[j++] = row.Hash;
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
                case "quote":
                    foreach (var row in rows)
                        result[j++] = Quotes.Get(quotes, row.Level);
                    break;
            }

            return result;
        }

        public async Task<IEnumerable<NonceRevelationOperation>> GetNonceRevelations(RawAccount account, SortMode sort, int offset, OffsetMode offsetMode, int limit, Symbols quotes)
        {
            var sql = $@"
                SELECT      o.""Id"", o.""Level"", o.""Timestamp"", o.""OpHash"", o.""BakerId"", o.""SenderId"", o.""RevealedLevel"", b.""Hash""
                FROM        ""NonceRevelationOps"" as o
                INNER JOIN  ""Blocks"" as b 
                        ON  b.""Level"" = o.""Level""
                WHERE      (o.""BakerId"" = @accountId
                OR          o.""SenderId"" = @accountId)
                {Pagination("o", sort, offset, offsetMode, limit)}";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { accountId = account.Id });

            return rows.Select(row => new NonceRevelationOperation
            {
                Id = row.Id,
                Level = row.Level,
                Block = row.Hash,
                Timestamp = row.Timestamp,
                Hash = row.OpHash,
                Baker = Accounts.GetAlias(row.BakerId),
                BakerRewards = 125_000,
                Sender = Accounts.GetAlias(row.SenderId),
                RevealedLevel = row.RevealedLevel,
                Quote = Quotes.Get(quotes, row.Level)
            });
        }

        public async Task<IEnumerable<NonceRevelationOperation>> GetNonceRevelations(RawAccount account, DateTime from, DateTime to, SortMode sort, int offset, OffsetMode offsetMode, int limit, Symbols quotes)
        {
            var sql = $@"
                SELECT      o.""Id"", o.""Level"", o.""Timestamp"", o.""OpHash"", o.""BakerId"", o.""SenderId"", o.""RevealedLevel"", b.""Hash""
                FROM        ""NonceRevelationOps"" as o
                INNER JOIN  ""Blocks"" as b 
                        ON  b.""Level"" = o.""Level""
                WHERE      (o.""BakerId"" = @accountId
                OR          o.""SenderId"" = @accountId)
                AND         o.""Timestamp"" >= @from
                AND         o.""Timestamp"" < @to
                {Pagination("o", sort, offset, offsetMode, limit)}";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { accountId = account.Id, from, to });

            return rows.Select(row => new NonceRevelationOperation
            {
                Id = row.Id,
                Level = row.Level,
                Block = row.Hash,
                Timestamp = row.Timestamp,
                Hash = row.OpHash,
                Baker = Accounts.GetAlias(row.BakerId),
                BakerRewards = 125_000,
                Sender = Accounts.GetAlias(row.SenderId),
                RevealedLevel = row.RevealedLevel,
                Quote = Quotes.Get(quotes, row.Level)
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

        public async Task<IEnumerable<DelegationOperation>> GetDelegations(string hash, Symbols quotes)
        {
            var sql = @"
                SELECT      o.""Id"", o.""Level"", o.""Timestamp"", o.""SenderId"", o.""InitiatorId"", o.""Counter"", o.""BakerFee"",
                            o.""GasLimit"", o.""GasUsed"", o.""Status"", o.""Nonce"", o.""PrevDelegateId"", o.""DelegateId"", o.""Errors"", b.""Hash""
                FROM        ""DelegationOps"" as o
                INNER JOIN  ""Blocks"" as b 
                        ON  b.""Level"" = o.""Level""
                WHERE       o.""OpHash"" = @hash::character(51)
                ORDER BY    o.""Id""";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { hash });

            return rows.Select(row => new DelegationOperation
            {
                Id = row.Id,
                Level = row.Level,
                Block = row.Hash,
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
                Errors = row.Errors != null ? OperationErrorSerializer.Deserialize(row.Errors) : null,
                Quote = Quotes.Get(quotes, row.Level)
            });
        }

        public async Task<IEnumerable<DelegationOperation>> GetDelegations(string hash, int counter, Symbols quotes)
        {
            var sql = @"
                SELECT      o.""Id"", o.""Level"", o.""Timestamp"", o.""SenderId"", o.""InitiatorId"", o.""BakerFee"",
                            o.""GasLimit"", o.""GasUsed"", o.""Status"", o.""Nonce"", o.""PrevDelegateId"", o.""DelegateId"", o.""Errors"", b.""Hash""
                FROM        ""DelegationOps"" as o
                INNER JOIN  ""Blocks"" as b 
                        ON  b.""Level"" = o.""Level""
                WHERE       o.""OpHash"" = @hash::character(51) AND o.""Counter"" = @counter
                ORDER BY    o.""Id""";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { hash, counter });

            return rows.Select(row => new DelegationOperation
            {
                Id = row.Id,
                Level = row.Level,
                Block = row.Hash,
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
                Errors = row.Errors != null ? OperationErrorSerializer.Deserialize(row.Errors) : null,
                Quote = Quotes.Get(quotes, row.Level)
            });
        }

        public async Task<IEnumerable<DelegationOperation>> GetDelegations(string hash, int counter, int nonce, Symbols quotes)
        {
            var sql = @"
                SELECT      o.""Id"", o.""Level"", o.""Timestamp"", o.""SenderId"", o.""InitiatorId"", o.""BakerFee"",
                            o.""GasLimit"", o.""GasUsed"", o.""Status"", o.""PrevDelegateId"", o.""DelegateId"", o.""Errors"", b.""Hash""
                FROM        ""DelegationOps"" as o
                INNER JOIN  ""Blocks"" as b 
                        ON  b.""Level"" = o.""Level""
                WHERE       o.""OpHash"" = @hash::character(51) AND o.""Counter"" = @counter AND o.""Nonce"" = @nonce
                LIMIT       1";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { hash, counter, nonce });

            return rows.Select(row => new DelegationOperation
            {
                Id = row.Id,
                Level = row.Level,
                Block = row.Hash,
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
                Errors = row.Errors != null ? OperationErrorSerializer.Deserialize(row.Errors) : null,
                Quote = Quotes.Get(quotes, row.Level)
            });
        }

        public async Task<IEnumerable<DelegationOperation>> GetDelegations(Block block, Symbols quotes)
        {
            var sql = @"
                SELECT    ""Id"", ""Timestamp"", ""OpHash"", ""SenderId"", ""InitiatorId"", ""Counter"", ""BakerFee"",
                          ""GasLimit"", ""GasUsed"", ""Status"", ""Nonce"", ""PrevDelegateId"", ""DelegateId"", ""Errors""
                FROM      ""DelegationOps""
                WHERE     ""Level"" = @level
                ORDER BY  ""Id""";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { level = block.Level });

            return rows.Select(row => new DelegationOperation
            {
                Id = row.Id,
                Level = block.Level,
                Block = block.Hash,
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
                Errors = row.Errors != null ? OperationErrorSerializer.Deserialize(row.Errors) : null,
                Quote = Quotes.Get(quotes, block.Level)
            });
        }

        public async Task<IEnumerable<DelegationOperation>> GetDelegations(
            AccountParameter initiator,
            AccountParameter sender,
            AccountParameter prevDelegate,
            AccountParameter newDelegate,
            Int32Parameter level,
            OperationStatusParameter status,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            Symbols quotes)
        {
            var sql = new SqlBuilder(@"SELECT o.*, b.""Hash"" FROM ""DelegationOps"" AS o INNER JOIN ""Blocks"" as b ON b.""Level"" = o.""Level""")
                .Filter("InitiatorId", initiator, x => x == "prevDelegate" ? "PrevDelegateId" : "DelegateId")
                .Filter("SenderId", sender, x => x == "prevDelegate" ? "PrevDelegateId" : "DelegateId")
                .Filter("PrevDelegateId", prevDelegate, x => x == "initiator" ? "InitiatorId" : x == "sender" ? "SenderId" : "DelegateId")
                .Filter("DelegateId", newDelegate, x => x == "initiator" ? "InitiatorId" : x == "sender" ? "SenderId" : "PrevDelegateId")
                .FilterA(@"o.""Level""", level)
                .Filter("Status", status)
                .Take(sort, offset, limit, x => x switch
                {
                    "level" => ("Id", "Level"),
                    "gasUsed" => ("GasUsed", "GasUsed"),
                    "bakerFee" => ("BakerFee", "BakerFee"),
                    _ => ("Id", "Id")
                }, "o");

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            return rows.Select(row => new DelegationOperation
            {
                Id = row.Id,
                Level = row.Level,
                Block = row.Hash,
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
                Errors = row.Errors != null ? OperationErrorSerializer.Deserialize(row.Errors) : null,
                Quote = Quotes.Get(quotes, row.Level)
            });
        }

        public async Task<object[][]> GetDelegations(
            AccountParameter initiator,
            AccountParameter sender,
            AccountParameter prevDelegate,
            AccountParameter newDelegate,
            Int32Parameter level,
            OperationStatusParameter status,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            string[] fields,
            Symbols quotes)
        {
            var columns = new HashSet<string>(fields.Length);
            var joins = new HashSet<string>(1);

            foreach (var field in fields)
            {
                switch (field)
                {
                    case "id": columns.Add(@"o.""Id"""); break;
                    case "level": columns.Add(@"o.""Level"""); break;
                    case "timestamp": columns.Add(@"o.""Timestamp"""); break;
                    case "hash": columns.Add(@"o.""OpHash"""); break;
                    case "initiator": columns.Add(@"o.""InitiatorId"""); break;
                    case "sender": columns.Add(@"o.""SenderId"""); break;
                    case "counter": columns.Add(@"o.""Counter"""); break;
                    case "nonce": columns.Add(@"o.""Nonce"""); break;
                    case "gasLimit": columns.Add(@"o.""GasLimit"""); break;
                    case "gasUsed": columns.Add(@"o.""GasUsed"""); break;
                    case "bakerFee": columns.Add(@"o.""BakerFee"""); break;
                    case "prevDelegate": columns.Add(@"o.""PrevDelegateId"""); break;
                    case "newDelegate": columns.Add(@"o.""DelegateId"""); break;
                    case "status": columns.Add(@"o.""Status"""); break;
                    case "errors": columns.Add(@"o.""Errors"""); break;
                    case "block":
                        columns.Add(@"b.""Hash""");
                        joins.Add(@"INNER JOIN ""Blocks"" as b ON b.""Level"" = o.""Level""");
                        break;
                    case "quote": columns.Add(@"o.""Level"""); break;
                }
            }

            if (columns.Count == 0)
                return Array.Empty<object[]>();

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""DelegationOps"" as o {string.Join(' ', joins)}")
                .Filter("InitiatorId", initiator, x => x == "prevDelegate" ? "PrevDelegateId" : "DelegateId")
                .Filter("SenderId", sender, x => x == "prevDelegate" ? "PrevDelegateId" : "DelegateId")
                .Filter("PrevDelegateId", prevDelegate, x => x == "initiator" ? "InitiatorId" : x == "sender" ? "SenderId" : "DelegateId")
                .Filter("DelegateId", newDelegate, x => x == "initiator" ? "InitiatorId" : x == "sender" ? "SenderId" : "PrevDelegateId")
                .FilterA(@"o.""Level""", level)
                .Filter("Status", status)
                .Take(sort, offset, limit, x => x switch
                {
                    "level" => ("Id", "Level"),
                    "gasUsed" => ("GasUsed", "GasUsed"),
                    "bakerFee" => ("BakerFee", "BakerFee"),
                    _ => ("Id", "Id")
                }, "o");

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
                    case "block":
                        foreach (var row in rows)
                            result[j++][i] = row.Hash;
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
                    case "quote":
                        foreach (var row in rows)
                            result[j++][i] = Quotes.Get(quotes, row.Level);
                        break;
                }
            }

            return result;
        }

        public async Task<object[]> GetDelegations(
            AccountParameter initiator,
            AccountParameter sender,
            AccountParameter prevDelegate,
            AccountParameter newDelegate,
            Int32Parameter level,
            OperationStatusParameter status,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            string field,
            Symbols quotes)
        {
            var columns = new HashSet<string>(1);
            var joins = new HashSet<string>(1);

            switch (field)
            {
                case "id": columns.Add(@"o.""Id"""); break;
                case "level": columns.Add(@"o.""Level"""); break;
                case "timestamp": columns.Add(@"o.""Timestamp"""); break;
                case "hash": columns.Add(@"o.""OpHash"""); break;
                case "initiator": columns.Add(@"o.""InitiatorId"""); break;
                case "sender": columns.Add(@"o.""SenderId"""); break;
                case "counter": columns.Add(@"o.""Counter"""); break;
                case "nonce": columns.Add(@"o.""Nonce"""); break;
                case "gasLimit": columns.Add(@"o.""GasLimit"""); break;
                case "gasUsed": columns.Add(@"o.""GasUsed"""); break;
                case "bakerFee": columns.Add(@"o.""BakerFee"""); break;
                case "prevDelegate": columns.Add(@"o.""PrevDelegateId"""); break;
                case "newDelegate": columns.Add(@"o.""DelegateId"""); break;
                case "status": columns.Add(@"o.""Status"""); break;
                case "errors": columns.Add(@"o.""Errors"""); break;
                case "block":
                    columns.Add(@"b.""Hash""");
                    joins.Add(@"INNER JOIN ""Blocks"" as b ON b.""Level"" = o.""Level""");
                    break;
                case "quote": columns.Add(@"o.""Level"""); break;
            }

            if (columns.Count == 0)
                return Array.Empty<object>();

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""DelegationOps"" as o {string.Join(' ', joins)}")
                .Filter("InitiatorId", initiator, x => x == "prevDelegate" ? "PrevDelegateId" : "DelegateId")
                .Filter("SenderId", sender, x => x == "prevDelegate" ? "PrevDelegateId" : "DelegateId")
                .Filter("PrevDelegateId", prevDelegate, x => x == "initiator" ? "InitiatorId" : x == "sender" ? "SenderId" : "DelegateId")
                .Filter("DelegateId", newDelegate, x => x == "initiator" ? "InitiatorId" : x == "sender" ? "SenderId" : "PrevDelegateId")
                .FilterA(@"o.""Level""", level)
                .Filter("Status", status)
                .Take(sort, offset, limit, x => x switch
                {
                    "level" => ("Id", "Level"),
                    "gasUsed" => ("GasUsed", "GasUsed"),
                    "bakerFee" => ("BakerFee", "BakerFee"),
                    _ => ("Id", "Id")
                }, "o");

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
                case "block":
                    foreach (var row in rows)
                        result[j++] = row.Hash;
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
                case "quote":
                    foreach (var row in rows)
                        result[j++] = Quotes.Get(quotes, row.Level);
                    break;
            }

            return result;
        }

        public async Task<IEnumerable<DelegationOperation>> GetDelegations(RawAccount account, SortMode sort, int offset, OffsetMode offsetMode, int limit, Symbols quotes)
        {
            var sql = $@"
                SELECT      o.""Id"", o.""Level"", o.""Timestamp"", o.""OpHash"", o.""SenderId"", o.""InitiatorId"", o.""Counter"", o.""BakerFee"",
                            o.""GasLimit"", o.""GasUsed"", o.""Status"", o.""Nonce"", o.""PrevDelegateId"", o.""DelegateId"", o.""Errors"", b.""Hash""
                FROM        ""DelegationOps"" as o
                INNER JOIN  ""Blocks"" as b 
                        ON  b.""Level"" = o.""Level""
                WHERE      (o.""SenderId"" = @accountId
                OR          o.""PrevDelegateId"" = @accountId
                OR          o.""DelegateId"" = @accountId
                OR          o.""InitiatorId"" = @accountId)
                {Pagination("o", sort, offset, offsetMode, limit)}";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { accountId = account.Id });

            return rows.Select(row => new DelegationOperation
            {
                Id = row.Id,
                Level = row.Level,
                Block = row.Hash,
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
                Errors = row.Errors != null ? OperationErrorSerializer.Deserialize(row.Errors) : null,
                Quote = Quotes.Get(quotes, row.Level)
            });
        }

        public async Task<IEnumerable<DelegationOperation>> GetDelegations(RawAccount account, DateTime from, DateTime to, SortMode sort, int offset, OffsetMode offsetMode, int limit, Symbols quotes)
        {
            var sql = $@"
                SELECT      o.""Id"", o.""Level"", o.""Timestamp"", o.""OpHash"", o.""SenderId"", o.""InitiatorId"", o.""Counter"", o.""BakerFee"",
                            o.""GasLimit"", o.""GasUsed"", o.""Status"", o.""Nonce"", o.""PrevDelegateId"", o.""DelegateId"", o.""Errors"", b.""Hash""
                FROM        ""DelegationOps"" as o
                INNER JOIN  ""Blocks"" as b 
                        ON  b.""Level"" = o.""Level""
                WHERE      (o.""SenderId"" = @accountId
                OR          o.""PrevDelegateId"" = @accountId
                OR          o.""DelegateId"" = @accountId
                OR          o.""InitiatorId"" = @accountId)
                AND         o.""Timestamp"" >= @from
                AND         o.""Timestamp"" < @to
                {Pagination("o", sort, offset, offsetMode, limit)}";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { accountId = account.Id, from, to });

            return rows.Select(row => new DelegationOperation
            {
                Id = row.Id,
                Level = row.Level,
                Block = row.Hash,
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
                Errors = row.Errors != null ? OperationErrorSerializer.Deserialize(row.Errors) : null,
                Quote = Quotes.Get(quotes, row.Level)
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

        public async Task<IEnumerable<OriginationOperation>> GetOriginations(string hash, Symbols quotes)
        {
            var sql = @"
                SELECT      o.""Id"", o.""Level"", o.""Timestamp"", o.""SenderId"", o.""InitiatorId"", o.""Counter"",
                            o.""BakerFee"", o.""StorageFee"", o.""AllocationFee"", o.""GasLimit"", o.""GasUsed"", o.""StorageLimit"", o.""StorageUsed"",
                            o.""Status"", o.""Nonce"", o.""ContractId"", o.""DelegateId"", o.""Balance"", o.""ManagerId"", o.""Errors"", b.""Hash""
                FROM        ""OriginationOps"" as o
                INNER JOIN  ""Blocks"" as b 
                        ON  b.""Level"" = o.""Level""
                WHERE       o.""OpHash"" = @hash::character(51)
                ORDER BY    o.""Id""";

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
                    Block = row.Hash,
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
                    Errors = row.Errors != null ? OperationErrorSerializer.Deserialize(row.Errors) : null,
                    Quote = Quotes.Get(quotes, row.Level)
                };
            });
        }

        public async Task<IEnumerable<OriginationOperation>> GetOriginations(string hash, int counter, Symbols quotes)
        {
            var sql = @"
                SELECT      o.""Id"", o.""Level"", o.""Timestamp"", o.""SenderId"", o.""InitiatorId"",
                            o.""BakerFee"", o.""StorageFee"", o.""AllocationFee"", o.""GasLimit"", o.""GasUsed"", o.""StorageLimit"", o.""StorageUsed"",
                            o.""Status"", o.""Nonce"", o.""ContractId"", o.""DelegateId"", o.""Balance"", o.""ManagerId"", o.""Errors"", b.""Hash""
                FROM        ""OriginationOps"" as o
                INNER JOIN  ""Blocks"" as b 
                        ON  b.""Level"" = o.""Level""
                WHERE       o.""OpHash"" = @hash::character(51) AND o.""Counter"" = @counter
                ORDER BY    o.""Id""";

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
                    Block = row.Hash,
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
                    Errors = row.Errors != null ? OperationErrorSerializer.Deserialize(row.Errors) : null,
                    Quote = Quotes.Get(quotes, row.Level)
                };
            });
        }

        public async Task<IEnumerable<OriginationOperation>> GetOriginations(string hash, int counter, int nonce, Symbols quotes)
        {
            var sql = @"
                SELECT      o.""Id"", o.""Level"", o.""Timestamp"", o.""SenderId"", o.""InitiatorId"",
                            o.""BakerFee"", o.""StorageFee"", o.""AllocationFee"", o.""GasLimit"", o.""GasUsed"", o.""StorageLimit"", o.""StorageUsed"",
                            o.""Status"", o.""ContractId"", o.""DelegateId"", o.""Balance"", o.""ManagerId"", o.""Errors"", b.""Hash""
                FROM        ""OriginationOps"" as o
                INNER JOIN  ""Blocks"" as b 
                        ON  b.""Level"" = o.""Level""
                WHERE       o.""OpHash"" = @hash::character(51) AND o.""Counter"" = @counter AND o.""Nonce"" = @nonce
                LIMIT       1";

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
                    Block = row.Hash,
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
                    Errors = row.Errors != null ? OperationErrorSerializer.Deserialize(row.Errors) : null,
                    Quote = Quotes.Get(quotes, row.Level)
                };
            });
        }

        public async Task<IEnumerable<OriginationOperation>> GetOriginations(Block block, Symbols quotes)
        {
            var sql = @"
                SELECT    ""Id"", ""Timestamp"", ""OpHash"", ""SenderId"", ""InitiatorId"", ""Counter"", ""BakerFee"", ""StorageFee"", ""AllocationFee"", 
                          ""GasLimit"", ""GasUsed"", ""StorageLimit"", ""StorageUsed"", ""Status"", ""Nonce"", ""ContractId"", ""DelegateId"", ""Balance"", ""ManagerId"", ""Errors""
                FROM      ""OriginationOps""
                WHERE     ""Level"" = @level
                ORDER BY  ""Id""";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { level = block.Level });

            return rows.Select(row =>
            {
                var contract = row.ContractId == null ? null
                    : (RawContract)Accounts.Get((int)row.ContractId);

                var contractMetadata = contract == null ? null
                    : Accounts.GetMetadata(contract.Id);

                return new OriginationOperation
                {
                    Id = row.Id,
                    Level = block.Level,
                    Block = block.Hash,
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
                    Errors = row.Errors != null ? OperationErrorSerializer.Deserialize(row.Errors) : null,
                    Quote = Quotes.Get(quotes, block.Level)
                };
            });
        }

        public async Task<IEnumerable<OriginationOperation>> GetOriginations(
            AccountParameter initiator,
            AccountParameter sender,
            AccountParameter contractManager,
            AccountParameter contractDelegate,
            AccountParameter originatedContract,
            Int32Parameter level,
            OperationStatusParameter status,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            Symbols quotes)
        {
            var sql = new SqlBuilder(@"SELECT o.*, b.""Hash"" FROM ""OriginationOps"" AS o INNER JOIN ""Blocks"" as b ON b.""Level"" = o.""Level""")
                .Filter("InitiatorId", initiator, x => x == "contractManager" ? "ManagerId" : "DelegateId")
                .Filter("SenderId", sender, x => x == "contractManager" ? "ManagerId" : "DelegateId")
                .Filter("ManagerId", contractManager, x => x == "initiator" ? "InitiatorId" : x == "sender" ? "SenderId" : "DelegateId")
                .Filter("DelegateId", contractDelegate, x => x == "initiator" ? "InitiatorId" : x == "sender" ? "SenderId" : "ManagerId")
                .Filter("ContractId", originatedContract)
                .FilterA(@"o.""Level""", level)
                .Filter("Status", status)
                .Take(sort, offset, limit, x => x switch
                {
                    "level" => ("Id", "Level"),
                    "gasUsed" => ("GasUsed", "GasUsed"),
                    "storageUsed" => ("StorageUsed", "StorageUsed"),
                    "bakerFee" => ("BakerFee", "BakerFee"),
                    "storageFee" => ("StorageFee", "StorageFee"),
                    "allocationFee" => ("AllocationFee", "AllocationFee"),
                    "contractBalance" => ("Balance", "Balance"),
                    _ => ("Id", "Id")
                }, "o");

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
                    Block = row.Hash,
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
                    Errors = row.Errors != null ? OperationErrorSerializer.Deserialize(row.Errors) : null,
                    Quote = Quotes.Get(quotes, row.Level)
                };
            });
        }

        public async Task<object[][]> GetOriginations(
            AccountParameter initiator,
            AccountParameter sender,
            AccountParameter contractManager,
            AccountParameter contractDelegate,
            AccountParameter originatedContract,
            Int32Parameter level,
            OperationStatusParameter status,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            string[] fields,
            Symbols quotes)
        {
            var columns = new HashSet<string>(fields.Length);
            var joins = new HashSet<string>(1);

            foreach (var field in fields)
            {
                switch (field)
                {
                    case "id": columns.Add(@"o.""Id"""); break;
                    case "level": columns.Add(@"o.""Level"""); break;
                    case "timestamp": columns.Add(@"o.""Timestamp"""); break;
                    case "hash": columns.Add(@"o.""OpHash"""); break;
                    case "initiator": columns.Add(@"o.""InitiatorId"""); break;
                    case "sender": columns.Add(@"o.""SenderId"""); break;
                    case "counter": columns.Add(@"o.""Counter"""); break;
                    case "nonce": columns.Add(@"o.""Nonce"""); break;
                    case "gasLimit": columns.Add(@"o.""GasLimit"""); break;
                    case "gasUsed": columns.Add(@"o.""GasUsed"""); break;
                    case "storageLimit": columns.Add(@"o.""StorageLimit"""); break;
                    case "storageUsed": columns.Add(@"o.""StorageUsed"""); break;
                    case "bakerFee": columns.Add(@"o.""BakerFee"""); break;
                    case "storageFee": columns.Add(@"o.""StorageFee"""); break;
                    case "allocationFee": columns.Add(@"o.""AllocationFee"""); break;
                    case "contractDelegate": columns.Add(@"o.""DelegateId"""); break;
                    case "contractBalance": columns.Add(@"o.""Balance"""); break;
                    case "status": columns.Add(@"o.""Status"""); break;
                    case "originatedContract": columns.Add(@"o.""ContractId"""); break;
                    case "contractManager": columns.Add(@"o.""ManagerId"""); break;
                    case "errors": columns.Add(@"o.""Errors"""); break;
                    case "block":
                        columns.Add(@"b.""Hash""");
                        joins.Add(@"INNER JOIN ""Blocks"" as b ON b.""Level"" = o.""Level""");
                        break;
                    case "quote": columns.Add(@"o.""Level"""); break;
                }
            }

            if (columns.Count == 0)
                return Array.Empty<object[]>();

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""OriginationOps"" as o {string.Join(' ', joins)}")
                .Filter("InitiatorId", initiator, x => x == "contractManager" ? "ManagerId" : "DelegateId")
                .Filter("SenderId", sender, x => x == "contractManager" ? "ManagerId" : "DelegateId")
                .Filter("ManagerId", contractManager, x => x == "initiator" ? "InitiatorId" : x == "sender" ? "SenderId" : "DelegateId")
                .Filter("DelegateId", contractDelegate, x => x == "initiator" ? "InitiatorId" : x == "sender" ? "SenderId" : "ManagerId")
                .Filter("ContractId", originatedContract)
                .FilterA(@"o.""Level""", level)
                .Filter("Status", status)
                .Take(sort, offset, limit, x => x switch
                {
                    "level" => ("Id", "Level"),
                    "gasUsed" => ("GasUsed", "GasUsed"),
                    "storageUsed" => ("StorageUsed", "StorageUsed"),
                    "bakerFee" => ("BakerFee", "BakerFee"),
                    "storageFee" => ("StorageFee", "StorageFee"),
                    "allocationFee" => ("AllocationFee", "AllocationFee"),
                    "contractBalance" => ("Balance", "Balance"),
                    _ => ("Id", "Id")
                }, "o");

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
                    case "block":
                        foreach (var row in rows)
                            result[j++][i] = row.Hash;
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
                    case "quote":
                        foreach (var row in rows)
                            result[j++][i] = Quotes.Get(quotes, row.Level);
                        break;
                }
            }

            return result;
        }

        public async Task<object[]> GetOriginations(
            AccountParameter initiator,
            AccountParameter sender,
            AccountParameter contractManager,
            AccountParameter contractDelegate,
            AccountParameter originatedContract,
            Int32Parameter level,
            OperationStatusParameter status,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            string field,
            Symbols quotes)
        {
            var columns = new HashSet<string>(1);
            var joins = new HashSet<string>(1);

            switch (field)
            {
                case "id": columns.Add(@"o.""Id"""); break;
                case "level": columns.Add(@"o.""Level"""); break;
                case "timestamp": columns.Add(@"o.""Timestamp"""); break;
                case "hash": columns.Add(@"o.""OpHash"""); break;
                case "initiator": columns.Add(@"o.""InitiatorId"""); break;
                case "sender": columns.Add(@"o.""SenderId"""); break;
                case "counter": columns.Add(@"o.""Counter"""); break;
                case "nonce": columns.Add(@"o.""Nonce"""); break;
                case "gasLimit": columns.Add(@"o.""GasLimit"""); break;
                case "gasUsed": columns.Add(@"o.""GasUsed"""); break;
                case "storageLimit": columns.Add(@"o.""StorageLimit"""); break;
                case "storageUsed": columns.Add(@"o.""StorageUsed"""); break;
                case "bakerFee": columns.Add(@"o.""BakerFee"""); break;
                case "storageFee": columns.Add(@"o.""StorageFee"""); break;
                case "allocationFee": columns.Add(@"o.""AllocationFee"""); break;
                case "contractDelegate": columns.Add(@"o.""DelegateId"""); break;
                case "contractBalance": columns.Add(@"o.""Balance"""); break;
                case "status": columns.Add(@"o.""Status"""); break;
                case "originatedContract": columns.Add(@"o.""ContractId"""); break;
                case "contractManager": columns.Add(@"o.""ManagerId"""); break;
                case "errors": columns.Add(@"o.""Errors"""); break;
                case "block":
                    columns.Add(@"b.""Hash""");
                    joins.Add(@"INNER JOIN ""Blocks"" as b ON b.""Level"" = o.""Level""");
                    break;
                case "quote": columns.Add(@"o.""Level"""); break;
            }

            if (columns.Count == 0)
                return Array.Empty<object>();

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""OriginationOps"" as o {string.Join(' ', joins)}")
                .Filter("InitiatorId", initiator, x => x == "contractManager" ? "ManagerId" : "DelegateId")
                .Filter("SenderId", sender, x => x == "contractManager" ? "ManagerId" : "DelegateId")
                .Filter("ManagerId", contractManager, x => x == "initiator" ? "InitiatorId" : x == "sender" ? "SenderId" : "DelegateId")
                .Filter("DelegateId", contractDelegate, x => x == "initiator" ? "InitiatorId" : x == "sender" ? "SenderId" : "ManagerId")
                .Filter("ContractId", originatedContract)
                .FilterA(@"o.""Level""", level)
                .Filter("Status", status)
                .Take(sort, offset, limit, x => x switch
                {
                    "level" => ("Id", "Level"),
                    "gasUsed" => ("GasUsed", "GasUsed"),
                    "storageUsed" => ("StorageUsed", "StorageUsed"),
                    "bakerFee" => ("BakerFee", "BakerFee"),
                    "storageFee" => ("StorageFee", "StorageFee"),
                    "allocationFee" => ("AllocationFee", "AllocationFee"),
                    "contractBalance" => ("Balance", "Balance"),
                    _ => ("Id", "Id")
                }, "o");

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
                case "block":
                    foreach (var row in rows)
                        result[j++] = row.Hash;
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
                case "quote":
                    foreach (var row in rows)
                        result[j++] = Quotes.Get(quotes, row.Level);
                    break;
            }

            return result;
        }

        public async Task<IEnumerable<OriginationOperation>> GetOriginations(RawAccount account, SortMode sort, int offset, OffsetMode offsetMode, int limit, Symbols quotes)
        {
            var sql = $@"
                SELECT      o.""Id"", o.""Level"", o.""Timestamp"", o.""OpHash"", o.""SenderId"", o.""InitiatorId"", o.""Counter"",
                            o.""BakerFee"", o.""StorageFee"", o.""AllocationFee"", o.""GasLimit"", o.""GasUsed"", o.""StorageLimit"", o.""StorageUsed"",
                            o.""Status"", o.""Nonce"", o.""ContractId"", o.""DelegateId"", o.""Balance"", o.""ManagerId"", o.""Errors"", b.""Hash""
                FROM        ""OriginationOps"" as o
                INNER JOIN  ""Blocks"" as b 
                        ON  b.""Level"" = o.""Level""
                WHERE      (o.""SenderId"" = @accountId
                OR          o.""ManagerId"" = @accountId
                OR          o.""DelegateId"" = @accountId
                OR          o.""ContractId"" = @accountId
                OR          o.""InitiatorId"" = @accountId)
                {Pagination("o", sort, offset, offsetMode, limit)}";

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
                    Block = row.Hash,
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
                    Errors = row.Errors != null ? OperationErrorSerializer.Deserialize(row.Errors) : null,
                    Quote = Quotes.Get(quotes, row.Level)
                };
            });
        }

        public async Task<IEnumerable<OriginationOperation>> GetOriginations(RawAccount account, DateTime from, DateTime to, SortMode sort, int offset, OffsetMode offsetMode, int limit, Symbols quotes)
        {
            var sql = $@"
                SELECT      o.""Id"", o.""Level"", o.""Timestamp"", o.""OpHash"", o.""SenderId"", o.""InitiatorId"", o.""Counter"",
                            o.""BakerFee"", o.""StorageFee"", o.""AllocationFee"", o.""GasLimit"", o.""GasUsed"", o.""StorageLimit"", o.""StorageUsed"",
                            o.""Status"", o.""Nonce"", o.""ContractId"", o.""DelegateId"", o.""Balance"", o.""ManagerId"", o.""Errors"", b.""Hash""
                FROM        ""OriginationOps"" as o
                INNER JOIN  ""Blocks"" as b 
                        ON  b.""Level"" = o.""Level""
                WHERE      (o.""SenderId"" = @accountId
                OR          o.""ManagerId"" = @accountId
                OR          o.""DelegateId"" = @accountId
                OR          o.""ContractId"" = @accountId
                OR          o.""InitiatorId"" = @accountId)
                AND         o.""Timestamp"" >= @from
                AND         o.""Timestamp"" < @to
                {Pagination("o", sort, offset, offsetMode, limit)}";

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
                    Block = row.Hash,
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
                    Errors = row.Errors != null ? OperationErrorSerializer.Deserialize(row.Errors) : null,
                    Quote = Quotes.Get(quotes, row.Level)
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

        public async Task<IEnumerable<TransactionOperation>> GetTransactions(string hash, Symbols quotes)
        {
            var sql = @"
                SELECT      o.""Id"", o.""Level"", o.""Timestamp"", o.""SenderId"", o.""InitiatorId"", o.""Counter"", o.""BakerFee"", o.""StorageFee"", o.""AllocationFee"", o.""Parameters"",
                            o.""GasLimit"", o.""GasUsed"", o.""StorageLimit"", o.""StorageUsed"", o.""Status"", o.""Nonce"", o.""TargetId"", o.""Amount"", o.""InternalOperations"", o.""Errors"", b.""Hash""
                FROM        ""TransactionOps"" as o
                INNER JOIN  ""Blocks"" as b 
                        ON  b.""Level"" = o.""Level""
                WHERE       o.""OpHash"" = @hash::character(51)
                ORDER BY    o.""Id""";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { hash });

            return rows.Select(row => new TransactionOperation
            {
                Id = row.Id,
                Level = row.Level,
                Block = row.Hash,
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
                HasInternals = row.InternalOperations > 0,
                Quote = Quotes.Get(quotes, row.Level)
            });
        }

        public async Task<IEnumerable<TransactionOperation>> GetTransactions(string hash, int counter, Symbols quotes)
        {
            var sql = @"
                SELECT      o.""Id"", o.""Level"", o.""Timestamp"", o.""SenderId"", o.""InitiatorId"", o.""BakerFee"", o.""StorageFee"", o.""AllocationFee"", o.""Parameters"",
                            o.""GasLimit"", o.""GasUsed"", o.""StorageLimit"", o.""StorageUsed"", o.""Status"", o.""Nonce"", o.""TargetId"", o.""Amount"", o.""InternalOperations"", o.""Errors"", b.""Hash""
                FROM        ""TransactionOps"" as o
                INNER JOIN  ""Blocks"" as b 
                        ON  b.""Level"" = o.""Level""
                WHERE       o.""OpHash"" = @hash::character(51) AND o.""Counter"" = @counter
                ORDER BY    o.""Id""";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { hash, counter });

            return rows.Select(row => new TransactionOperation
            {
                Id = row.Id,
                Level = row.Level,
                Block = row.Hash,
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
                HasInternals = row.InternalOperations > 0,
                Quote = Quotes.Get(quotes, row.Level)
            });
        }

        public async Task<IEnumerable<TransactionOperation>> GetTransactions(string hash, int counter, int nonce, Symbols quotes)
        {
            var sql = @"
                SELECT      o.""Id"", o.""Level"", o.""Timestamp"", o.""SenderId"", o.""InitiatorId"", o.""BakerFee"", o.""StorageFee"", o.""AllocationFee"", o.""Parameters"",
                            o.""GasLimit"", o.""GasUsed"", o.""StorageLimit"", o.""StorageUsed"", o.""Status"", o.""TargetId"", o.""Amount"", o.""InternalOperations"", o.""Errors"", b.""Hash""
                FROM        ""TransactionOps"" as o
                INNER JOIN  ""Blocks"" as b 
                        ON  b.""Level"" = o.""Level""
                WHERE       o.""OpHash"" = @hash::character(51) AND o.""Counter"" = @counter AND o.""Nonce"" = @nonce
                LIMIT       1";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { hash, counter, nonce });

            return rows.Select(row => new TransactionOperation
            {
                Id = row.Id,
                Level = row.Level,
                Block = row.Hash,
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
                HasInternals = row.InternalOperations > 0,
                Quote = Quotes.Get(quotes, row.Level)
            });
        }

        public async Task<IEnumerable<TransactionOperation>> GetTransactions(Block block, Symbols quotes)
        {
            var sql = @"
                SELECT    ""Id"", ""Timestamp"", ""OpHash"", ""SenderId"", ""InitiatorId"", ""Counter"", ""BakerFee"", ""StorageFee"", ""AllocationFee"", ""Parameters"",
                          ""GasLimit"", ""GasUsed"", ""StorageLimit"", ""StorageUsed"", ""Status"", ""Nonce"", ""TargetId"", ""Amount"", ""InternalOperations"", ""Errors""
                FROM      ""TransactionOps""
                WHERE     ""Level"" = @level
                ORDER BY  ""Id""";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { level = block.Level });

            return rows.Select(row => new TransactionOperation
            {
                Id = row.Id,
                Level = block.Level,
                Block = block.Hash,
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
                HasInternals = row.InternalOperations > 0,
                Quote = Quotes.Get(quotes, block.Level)
            });
        }

        public async Task<IEnumerable<TransactionOperation>> GetTransactions(
            AccountParameter initiator,
            AccountParameter sender,
            AccountParameter target,
            Int64Parameter amount,
            Int32Parameter level,
            StringParameter parameters,
            OperationStatusParameter status,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            Symbols quotes)
        {
            var sql = new SqlBuilder(@"SELECT o.*, b.""Hash"" FROM ""TransactionOps"" AS o INNER JOIN ""Blocks"" as b ON b.""Level"" = o.""Level""")
                .Filter("InitiatorId", initiator, x => "TargetId")
                .Filter("SenderId", sender, x => "TargetId")
                .Filter("TargetId", target, x => x == "sender" ? "SenderId" : "InitiatorId")
                .Filter("Amount", amount)
                .Filter("Parameters", parameters)
                .Filter("Status", status)
                .FilterA(@"o.""Level""", level)
                .Take(sort, offset, limit, x => x switch
                {
                    "level" => ("Id", "Level"),
                    "gasUsed" => ("GasUsed", "GasUsed"),
                    "storageUsed" => ("StorageUsed", "StorageUsed"),
                    "bakerFee" => ("BakerFee", "BakerFee"),
                    "storageFee" => ("StorageFee", "StorageFee"),
                    "allocationFee" => ("AllocationFee", "AllocationFee"),
                    "amount" => ("Amount", "Amount"),
                    _ => ("Id", "Id")
                }, "o");

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            return rows.Select(row => new TransactionOperation
            {
                Id = row.Id,
                Level = row.Level,
                Block = row.Hash,
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
                HasInternals = row.InternalOperations > 0,
                Quote = Quotes.Get(quotes, row.Level)
            });
        }

        public async Task<object[][]> GetTransactions(
            AccountParameter initiator,
            AccountParameter sender,
            AccountParameter target,
            Int64Parameter amount,
            Int32Parameter level,
            StringParameter parameters,
            OperationStatusParameter status,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            string[] fields,
            Symbols quotes)
        {
            var columns = new HashSet<string>(fields.Length);
            var joins = new HashSet<string>(1);

            foreach (var field in fields)
            {
                switch (field)
                {
                    case "id": columns.Add(@"o.""Id"""); break;
                    case "level": columns.Add(@"o.""Level"""); break;
                    case "timestamp": columns.Add(@"o.""Timestamp"""); break;
                    case "hash": columns.Add(@"o.""OpHash"""); break;
                    case "initiator": columns.Add(@"o.""InitiatorId"""); break;
                    case "sender": columns.Add(@"o.""SenderId"""); break;
                    case "counter": columns.Add(@"o.""Counter"""); break;
                    case "nonce": columns.Add(@"o.""Nonce"""); break;
                    case "gasLimit": columns.Add(@"o.""GasLimit"""); break;
                    case "gasUsed": columns.Add(@"o.""GasUsed"""); break;
                    case "storageLimit": columns.Add(@"o.""StorageLimit"""); break;
                    case "storageUsed": columns.Add(@"o.""StorageUsed"""); break;
                    case "bakerFee": columns.Add(@"o.""BakerFee"""); break;
                    case "storageFee": columns.Add(@"o.""StorageFee"""); break;
                    case "allocationFee": columns.Add(@"o.""AllocationFee"""); break;
                    case "target": columns.Add(@"o.""TargetId"""); break;
                    case "amount": columns.Add(@"o.""Amount"""); break;
                    case "parameters": columns.Add(@"o.""Parameters"""); break;
                    case "status": columns.Add(@"o.""Status"""); break;
                    case "errors": columns.Add(@"o.""Errors"""); break;
                    case "hasInternals": columns.Add(@"o.""InternalOperations"""); break;
                    case "block":
                        columns.Add(@"b.""Hash""");
                        joins.Add(@"INNER JOIN ""Blocks"" as b ON b.""Level"" = o.""Level""");
                        break;
                    case "quote": columns.Add(@"o.""Level"""); break;
                }
            }

            if (columns.Count == 0)
                return Array.Empty<object[]>();

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""TransactionOps"" as o {string.Join(' ', joins)}")
                .Filter("InitiatorId", initiator, x => "TargetId")
                .Filter("SenderId", sender, x => "TargetId")
                .Filter("TargetId", target, x => x == "sender" ? "SenderId" : "InitiatorId")
                .Filter("Amount", amount)
                .Filter("Parameters", parameters)
                .Filter("Status", status)
                .FilterA(@"o.""Level""", level)
                .Take(sort, offset, limit, x => x switch
                {
                    "level" => ("Id", "Level"),
                    "gasUsed" => ("GasUsed", "GasUsed"),
                    "storageUsed" => ("StorageUsed", "StorageUsed"),
                    "bakerFee" => ("BakerFee", "BakerFee"),
                    "storageFee" => ("StorageFee", "StorageFee"),
                    "allocationFee" => ("AllocationFee", "AllocationFee"),
                    "amount" => ("Amount", "Amount"),
                    _ => ("Id", "Id")
                }, "o");

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
                    case "block":
                        foreach (var row in rows)
                            result[j++][i] = row.Hash;
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
                    case "quote":
                        foreach (var row in rows)
                            result[j++][i] = Quotes.Get(quotes, row.Level);
                        break;
                }
            }

            return result;
        }

        public async Task<object[]> GetTransactions(
            AccountParameter initiator,
            AccountParameter sender,
            AccountParameter target,
            Int64Parameter amount,
            Int32Parameter level,
            StringParameter parameters,
            OperationStatusParameter status,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            string field,
            Symbols quotes)
        {
            var columns = new HashSet<string>(1);
            var joins = new HashSet<string>(1);

            switch (field)
            {
                case "id": columns.Add(@"o.""Id"""); break;
                case "level": columns.Add(@"o.""Level"""); break;
                case "timestamp": columns.Add(@"o.""Timestamp"""); break;
                case "hash": columns.Add(@"o.""OpHash"""); break;
                case "initiator": columns.Add(@"o.""InitiatorId"""); break;
                case "sender": columns.Add(@"o.""SenderId"""); break;
                case "counter": columns.Add(@"o.""Counter"""); break;
                case "nonce": columns.Add(@"o.""Nonce"""); break;
                case "gasLimit": columns.Add(@"o.""GasLimit"""); break;
                case "gasUsed": columns.Add(@"o.""GasUsed"""); break;
                case "storageLimit": columns.Add(@"o.""StorageLimit"""); break;
                case "storageUsed": columns.Add(@"o.""StorageUsed"""); break;
                case "bakerFee": columns.Add(@"o.""BakerFee"""); break;
                case "storageFee": columns.Add(@"o.""StorageFee"""); break;
                case "allocationFee": columns.Add(@"o.""AllocationFee"""); break;
                case "target": columns.Add(@"o.""TargetId"""); break;
                case "amount": columns.Add(@"o.""Amount"""); break;
                case "parameters": columns.Add(@"o.""Parameters"""); break;
                case "status": columns.Add(@"o.""Status"""); break;
                case "errors": columns.Add(@"o.""Errors"""); break;
                case "hasInternals": columns.Add(@"o.""InternalOperations"""); break;
                case "block":
                    columns.Add(@"b.""Hash""");
                    joins.Add(@"INNER JOIN ""Blocks"" as b ON b.""Level"" = o.""Level""");
                    break;
                case "quote": columns.Add(@"o.""Level"""); break;
            }

            if (columns.Count == 0)
                return Array.Empty<object>();

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""TransactionOps"" as o {string.Join(' ', joins)}")
                .Filter("InitiatorId", initiator, x => "TargetId")
                .Filter("SenderId", sender, x => "TargetId")
                .Filter("TargetId", target, x => x == "sender" ? "SenderId" : "InitiatorId")
                .Filter("Amount", amount)
                .Filter("Parameters", parameters)
                .Filter("Status", status)
                .FilterA(@"o.""Level""", level)
                .Take(sort, offset, limit, x => x switch
                {
                    "level" => ("Id", "Level"),
                    "gasUsed" => ("GasUsed", "GasUsed"),
                    "storageUsed" => ("StorageUsed", "StorageUsed"),
                    "bakerFee" => ("BakerFee", "BakerFee"),
                    "storageFee" => ("StorageFee", "StorageFee"),
                    "allocationFee" => ("AllocationFee", "AllocationFee"),
                    "amount" => ("Amount", "Amount"),
                    _ => ("Id", "Id")
                }, "o");

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
                case "block":
                    foreach (var row in rows)
                        result[j++] = row.Hash;
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
                case "quote":
                    foreach (var row in rows)
                        result[j++] = Quotes.Get(quotes, row.Level);
                    break;
            }

            return result;
        }

        public async Task<IEnumerable<TransactionOperation>> GetTransactions(RawAccount account, SortMode sort, int offset, OffsetMode offsetMode, int limit, Symbols quotes)
        {
            var sql = $@"
                SELECT      o.""Id"", o.""Level"", o.""Timestamp"", o.""OpHash"", o.""SenderId"", o.""InitiatorId"", o.""Counter"", o.""BakerFee"", o.""StorageFee"", o.""AllocationFee"", o.""Parameters"",
                            o.""GasLimit"", o.""GasUsed"", o.""StorageLimit"", o.""StorageUsed"", o.""Status"", o.""Nonce"", o.""TargetId"", o.""Amount"", o.""InternalOperations"", o.""Errors"", b.""Hash""
                FROM        ""TransactionOps"" as o
                INNER JOIN  ""Blocks"" as b 
                        ON  b.""Level"" = o.""Level""
                WHERE      (o.""SenderId"" = @accountId
                OR          o.""TargetId"" = @accountId
                OR          o.""InitiatorId"" = @accountId)
                {Pagination("o", sort, offset, offsetMode, limit)}";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { accountId = account.Id });

            return rows.Select(row => new TransactionOperation
            {
                Id = row.Id,
                Level = row.Level,
                Block = row.Hash,
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
                HasInternals = row.InternalOperations > 0,
                Quote = Quotes.Get(quotes, row.Level)
            });
        }

        public async Task<IEnumerable<TransactionOperation>> GetTransactions(RawAccount account, DateTime from, DateTime to, SortMode sort, int offset, OffsetMode offsetMode, int limit, Symbols quotes)
        {
            var sql = $@"
                SELECT      o.""Id"", o.""Level"", o.""Timestamp"", o.""OpHash"", o.""SenderId"", o.""InitiatorId"", o.""Counter"", o.""BakerFee"", o.""StorageFee"", o.""AllocationFee"", o.""Parameters"",
                            o.""GasLimit"", o.""GasUsed"", o.""StorageLimit"", o.""StorageUsed"", o.""Status"", o.""Nonce"", o.""TargetId"", o.""Amount"", o.""InternalOperations"", o.""Errors"", b.""Hash""
                FROM        ""TransactionOps"" as o
                INNER JOIN  ""Blocks"" as b 
                        ON  b.""Level"" = o.""Level""
                WHERE      (o.""SenderId"" = @accountId
                OR          o.""TargetId"" = @accountId
                OR          o.""InitiatorId"" = @accountId)
                AND         o.""Timestamp"" >= @from
                AND         o.""Timestamp"" < @to
                {Pagination("o", sort, offset, offsetMode, limit)}";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { accountId = account.Id, from, to });

            return rows.Select(row => new TransactionOperation
            {
                Id = row.Id,
                Level = row.Level,
                Block = row.Hash,
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
                HasInternals = row.InternalOperations > 0,
                Quote = Quotes.Get(quotes, row.Level)
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

        public async Task<IEnumerable<RevealOperation>> GetReveals(string hash, Symbols quotes)
        {
            var sql = @"
                SELECT      o.""Id"", o.""Level"", o.""Timestamp"", o.""SenderId"", o.""Counter"", o.""BakerFee"", o.""GasLimit"", o.""GasUsed"", o.""Status"", o.""Errors"", b.""Hash""
                FROM        ""RevealOps"" as o
                INNER JOIN  ""Blocks"" as b 
                        ON  b.""Level"" = o.""Level""
                WHERE       o.""OpHash"" = @hash::character(51)
                ORDER BY    o.""Id""";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { hash });

            return rows.Select(row => new RevealOperation
            {
                Id = row.Id,
                Level = row.Level,
                Block = row.Hash,
                Timestamp = row.Timestamp,
                Hash = hash,
                Sender = Accounts.GetAlias(row.SenderId),
                Counter = row.Counter,
                GasLimit = row.GasLimit,
                GasUsed = row.GasUsed,
                BakerFee = row.BakerFee,
                Status = StatusToString(row.Status),
                Errors = row.Errors != null ? OperationErrorSerializer.Deserialize(row.Errors) : null,
                Quote = Quotes.Get(quotes, row.Level)
            });
        }

        public async Task<IEnumerable<RevealOperation>> GetReveals(string hash, int counter, Symbols quotes)
        {
            var sql = @"
                SELECT      o.""Id"", o.""Level"", o.""Timestamp"", o.""SenderId"", o.""BakerFee"", o.""GasLimit"", o.""GasUsed"", o.""Status"", o.""Errors"", b.""Hash""
                FROM        ""RevealOps"" as o
                INNER JOIN  ""Blocks"" as b 
                        ON  b.""Level"" = o.""Level""
                WHERE       o.""OpHash"" = @hash::character(51) AND o.""Counter"" = @counter
                LIMIT       1";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { hash, counter });

            return rows.Select(row => new RevealOperation
            {
                Id = row.Id,
                Level = row.Level,
                Block = row.Hash,
                Timestamp = row.Timestamp,
                Hash = hash,
                Sender = Accounts.GetAlias(row.SenderId),
                Counter = counter,
                GasLimit = row.GasLimit,
                GasUsed = row.GasUsed,
                BakerFee = row.BakerFee,
                Status = StatusToString(row.Status),
                Errors = row.Errors != null ? OperationErrorSerializer.Deserialize(row.Errors) : null,
                Quote = Quotes.Get(quotes, row.Level)
            });
        }

        public async Task<IEnumerable<RevealOperation>> GetReveals(Block block, Symbols quotes)
        {
            var sql = @"
                SELECT    ""Id"", ""Timestamp"", ""OpHash"", ""SenderId"", ""Counter"", ""BakerFee"", ""GasLimit"", ""GasUsed"", ""Status"", ""Errors""
                FROM      ""RevealOps""
                WHERE     ""Level"" = @level
                ORDER BY  ""Id""";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { level = block.Level });

            return rows.Select(row => new RevealOperation
            {
                Id = row.Id,
                Level = block.Level,
                Block = block.Hash,
                Timestamp = row.Timestamp,
                Hash = row.OpHash,
                Sender = Accounts.GetAlias(row.SenderId),
                Counter = row.Counter,
                GasLimit = row.GasLimit,
                GasUsed = row.GasUsed,
                BakerFee = row.BakerFee,
                Status = StatusToString(row.Status),
                Errors = row.Errors != null ? OperationErrorSerializer.Deserialize(row.Errors) : null,
                Quote = Quotes.Get(quotes, block.Level)
            });
        }

        public async Task<IEnumerable<RevealOperation>> GetReveals(
            AccountParameter sender,
            Int32Parameter level,
            OperationStatusParameter status,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            Symbols quotes)
        {
            var sql = new SqlBuilder(@"SELECT o.*, b.""Hash"" FROM ""RevealOps"" AS o INNER JOIN ""Blocks"" as b ON b.""Level"" = o.""Level""")
                .Filter("SenderId", sender)
                .FilterA(@"o.""Level""", level)
                .Filter("Status", status)
                .Take(sort, offset, limit, x => x switch
                {
                    "level" => ("Id", "Level"),
                    "gasUsed" => ("GasUsed", "GasUsed"),
                    "bakerFee" => ("BakerFee", "BakerFee"),
                    _ => ("Id", "Id")
                }, "o");

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            return rows.Select(row => new RevealOperation
            {
                Id = row.Id,
                Level = row.Level,
                Block = row.Hash,
                Timestamp = row.Timestamp,
                Hash = row.OpHash,
                Sender = Accounts.GetAlias(row.SenderId),
                Counter = row.Counter,
                GasLimit = row.GasLimit,
                GasUsed = row.GasUsed,
                BakerFee = row.BakerFee,
                Status = StatusToString(row.Status),
                Errors = row.Errors != null ? OperationErrorSerializer.Deserialize(row.Errors) : null,
                Quote = Quotes.Get(quotes, row.Level)
            });
        }

        public async Task<object[][]> GetReveals(
            AccountParameter sender,
            Int32Parameter level,
            OperationStatusParameter status,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            string[] fields,
            Symbols quotes)
        {
            var columns = new HashSet<string>(fields.Length);
            var joins = new HashSet<string>(1);

            foreach (var field in fields)
            {
                switch (field)
                {
                    case "id": columns.Add(@"o.""Id"""); break;
                    case "level": columns.Add(@"o.""Level"""); break;
                    case "timestamp": columns.Add(@"o.""Timestamp"""); break;
                    case "hash": columns.Add(@"o.""OpHash"""); break;
                    case "sender": columns.Add(@"o.""SenderId"""); break;
                    case "counter": columns.Add(@"o.""Counter"""); break;
                    case "gasLimit": columns.Add(@"o.""GasLimit"""); break;
                    case "gasUsed": columns.Add(@"o.""GasUsed"""); break;
                    case "bakerFee": columns.Add(@"o.""BakerFee"""); break;
                    case "status": columns.Add(@"o.""Status"""); break;
                    case "errors": columns.Add(@"o.""Errors"""); break;
                    case "block":
                        columns.Add(@"b.""Hash""");
                        joins.Add(@"INNER JOIN ""Blocks"" as b ON b.""Level"" = o.""Level""");
                        break;
                    case "quote": columns.Add(@"o.""Level"""); break;
                }
            }

            if (columns.Count == 0)
                return Array.Empty<object[]>();

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""RevealOps"" as o {string.Join(' ', joins)}")
                .Filter("SenderId", sender)
                .FilterA(@"o.""Level""", level)
                .Filter("Status", status)
                .Take(sort, offset, limit, x => x switch
                {
                    "level" => ("Id", "Level"),
                    "gasUsed" => ("GasUsed", "GasUsed"),
                    "bakerFee" => ("BakerFee", "BakerFee"),
                    _ => ("Id", "Id")
                }, "o");

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
                    case "block":
                        foreach (var row in rows)
                            result[j++][i] = row.Hash;
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
                    case "quote":
                        foreach (var row in rows)
                            result[j++][i] = Quotes.Get(quotes, row.Level);
                        break;
                }
            }

            return result;
        }

        public async Task<object[]> GetReveals(
            AccountParameter sender,
            Int32Parameter level,
            OperationStatusParameter status,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            string field,
            Symbols quotes)
        {
            var columns = new HashSet<string>(1);
            var joins = new HashSet<string>(1);

            switch (field)
            {
                case "id": columns.Add(@"o.""Id"""); break;
                case "level": columns.Add(@"o.""Level"""); break;
                case "timestamp": columns.Add(@"o.""Timestamp"""); break;
                case "hash": columns.Add(@"o.""OpHash"""); break;
                case "sender": columns.Add(@"o.""SenderId"""); break;
                case "counter": columns.Add(@"o.""Counter"""); break;
                case "gasLimit": columns.Add(@"o.""GasLimit"""); break;
                case "gasUsed": columns.Add(@"o.""GasUsed"""); break;
                case "bakerFee": columns.Add(@"o.""BakerFee"""); break;
                case "status": columns.Add(@"o.""Status"""); break;
                case "errors": columns.Add(@"o.""Errors"""); break;
                case "block":
                    columns.Add(@"b.""Hash""");
                    joins.Add(@"INNER JOIN ""Blocks"" as b ON b.""Level"" = o.""Level""");
                    break;
                case "quote": columns.Add(@"o.""Level"""); break;
            }

            if (columns.Count == 0)
                return Array.Empty<object>();

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""RevealOps"" as o {string.Join(' ', joins)}")
                .Filter("SenderId", sender)
                .FilterA(@"o.""Level""", level)
                .Filter("Status", status)
                .Take(sort, offset, limit, x => x switch
                {
                    "level" => ("Id", "Level"),
                    "gasUsed" => ("GasUsed", "GasUsed"),
                    "bakerFee" => ("BakerFee", "BakerFee"),
                    _ => ("Id", "Id")
                }, "o");

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
                case "block":
                    foreach (var row in rows)
                        result[j++] = row.Hash;
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
                case "quote":
                    foreach (var row in rows)
                        result[j++] = Quotes.Get(quotes, row.Level);
                    break;
            }

            return result;
        }

        public async Task<IEnumerable<RevealOperation>> GetReveals(RawAccount account, SortMode sort, int offset, OffsetMode offsetMode, int limit, Symbols quotes)
        {
            var sql = $@"
                SELECT      o.""Id"", o.""Level"", o.""Timestamp"", o.""OpHash"", o.""SenderId"", o.""Counter"", o.""BakerFee"", o.""GasLimit"", o.""GasUsed"", o.""Status"", o.""Errors"", b.""Hash""
                FROM        ""RevealOps"" as o
                INNER JOIN  ""Blocks"" as b 
                        ON  b.""Level"" = o.""Level""
                WHERE       o.""SenderId"" = @accountId
                {Pagination("o", sort, offset, offsetMode, limit)}";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { accountId = account.Id });

            return rows.Select(row => new RevealOperation
            {
                Id = row.Id,
                Level = row.Level,
                Block = row.Hash,
                Timestamp = row.Timestamp,
                Hash = row.OpHash,
                Sender = Accounts.GetAlias(row.SenderId),
                Counter = row.Counter,
                GasLimit = row.GasLimit,
                GasUsed = row.GasUsed,
                BakerFee = row.BakerFee,
                Status = StatusToString(row.Status),
                Errors = row.Errors != null ? OperationErrorSerializer.Deserialize(row.Errors) : null,
                Quote = Quotes.Get(quotes, row.Level)
            });
        }

        public async Task<IEnumerable<RevealOperation>> GetReveals(RawAccount account, DateTime from, DateTime to, SortMode sort, int offset, OffsetMode offsetMode, int limit, Symbols quotes)
        {
            var sql = $@"
                SELECT      o.""Id"", o.""Level"", o.""Timestamp"", o.""OpHash"", o.""SenderId"", o.""Counter"", o.""BakerFee"", o.""GasLimit"", o.""GasUsed"", o.""Status"", o.""Errors"", b.""Hash""
                FROM        ""RevealOps"" as o
                INNER JOIN  ""Blocks"" as b 
                        ON  b.""Level"" = o.""Level""
                WHERE       o.""SenderId"" = @accountId
                AND         o.""Timestamp"" >= @from
                AND         o.""Timestamp"" < @to
                {Pagination("o", sort, offset, offsetMode, limit)}";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { accountId = account.Id, from, to });

            return rows.Select(row => new RevealOperation
            {
                Id = row.Id,
                Level = row.Level,
                Block = row.Hash,
                Timestamp = row.Timestamp,
                Hash = row.OpHash,
                Sender = Accounts.GetAlias(row.SenderId),
                Counter = row.Counter,
                GasLimit = row.GasLimit,
                GasUsed = row.GasUsed,
                BakerFee = row.BakerFee,
                Status = StatusToString(row.Status),
                Errors = row.Errors != null ? OperationErrorSerializer.Deserialize(row.Errors) : null,
                Quote = Quotes.Get(quotes, row.Level)
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

        public async Task<IEnumerable<MigrationOperation>> GetMigrations(
            Int32Parameter level,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            Symbols quotes)
        {
            var sql = new SqlBuilder(@"SELECT o.*, b.""Hash"" FROM ""MigrationOps"" AS o INNER JOIN ""Blocks"" as b ON b.""Level"" = o.""Level""")
                .FilterA(@"o.""Level""", level)
                .Take(sort, offset, limit, x => x == "level" ? ("Id", "Level") : ("Id", "Id"), "o");

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            return rows.Select(row => new MigrationOperation
            {
                Id = row.Id,
                Level = row.Level,
                Block = row.Hash,
                Timestamp = row.Timestamp,
                Account = Accounts.GetAlias(row.AccountId),
                Kind = MigrationKindToString(row.Kind),
                BalanceChange = row.BalanceChange,
                Quote = Quotes.Get(quotes, row.Level)
            });
        }

        public async Task<object[][]> GetMigrations(
            Int32Parameter level,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            string[] fields,
            Symbols quotes)
        {
            var columns = new HashSet<string>(fields.Length);
            var joins = new HashSet<string>(1);

            foreach (var field in fields)
            {
                switch (field)
                {
                    case "id": columns.Add(@"o.""Id"""); break;
                    case "level": columns.Add(@"o.""Level"""); break;
                    case "timestamp": columns.Add(@"o.""Timestamp"""); break;
                    case "account": columns.Add(@"o.""AccountId"""); break;
                    case "kind": columns.Add(@"o.""Kind"""); break;
                    case "balanceChange": columns.Add(@"o.""BalanceChange"""); break;
                    case "block":
                        columns.Add(@"b.""Hash""");
                        joins.Add(@"INNER JOIN ""Blocks"" as b ON b.""Level"" = o.""Level""");
                        break;
                    case "quote": columns.Add(@"o.""Level"""); break;
                }
            }

            if (columns.Count == 0)
                return Array.Empty<object[]>();

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""MigrationOps"" as o {string.Join(' ', joins)}")
                .FilterA(@"o.""Level""", level)
                .Take(sort, offset, limit, x => x == "level" ? ("Id", "Level") : ("Id", "Id"), "o");

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
                    case "block":
                        foreach (var row in rows)
                            result[j++][i] = row.Hash;
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
                    case "quote":
                        foreach (var row in rows)
                            result[j++][i] = Quotes.Get(quotes, row.Level);
                        break;
                }
            }

            return result;
        }

        public async Task<object[]> GetMigrations(
            Int32Parameter level,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            string field,
            Symbols quotes)
        {
            var columns = new HashSet<string>(1);
            var joins = new HashSet<string>(1);

            switch (field)
            {
                case "id": columns.Add(@"o.""Id"""); break;
                case "level": columns.Add(@"o.""Level"""); break;
                case "timestamp": columns.Add(@"o.""Timestamp"""); break;
                case "account": columns.Add(@"o.""AccountId"""); break;
                case "kind": columns.Add(@"o.""Kind"""); break;
                case "balanceChange": columns.Add(@"o.""BalanceChange"""); break;
                case "block":
                    columns.Add(@"b.""Hash""");
                    joins.Add(@"INNER JOIN ""Blocks"" as b ON b.""Level"" = o.""Level""");
                    break;
                case "quote": columns.Add(@"o.""Level"""); break;
            }

            if (columns.Count == 0)
                return Array.Empty<object>();

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""MigrationOps"" as o {string.Join(' ', joins)}")
                .FilterA(@"o.""Level""", level)
                .Take(sort, offset, limit, x => x == "level" ? ("Id", "Level") : ("Id", "Id"), "o");

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
                case "block":
                    foreach (var row in rows)
                        result[j++] = row.Hash;
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
                case "quote":
                    foreach (var row in rows)
                        result[j++] = Quotes.Get(quotes, row.Level);
                    break;
            }

            return result;
        }

        public async Task<IEnumerable<MigrationOperation>> GetMigrations(RawAccount account, SortMode sort, int offset, OffsetMode offsetMode, int limit, Symbols quotes)
        {
            var sql = $@"
                SELECT      o.""Id"", o.""Level"", o.""Timestamp"", o.""Kind"", o.""BalanceChange"", b.""Hash""
                FROM        ""MigrationOps"" as o
                INNER JOIN  ""Blocks"" as b 
                        ON  b.""Level"" = o.""Level""
                WHERE       o.""AccountId"" = @accountId
                {Pagination("o", sort, offset, offsetMode, limit)}";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { accountId = account.Id });

            return rows.Select(row => new MigrationOperation
            {
                Id = row.Id,
                Level = row.Level,
                Block = row.Hash,
                Timestamp = row.Timestamp,
                Account = Accounts.GetAlias(account.Id),
                Kind = MigrationKindToString(row.Kind),
                BalanceChange = row.BalanceChange,
                Quote = Quotes.Get(quotes, row.Level)
            });
        }

        public async Task<IEnumerable<MigrationOperation>> GetMigrations(RawAccount account, DateTime from, DateTime to, SortMode sort, int offset, OffsetMode offsetMode, int limit, Symbols quotes)
        {
            var sql = $@"
                SELECT      o.""Id"", o.""Level"", o.""Timestamp"", o.""Kind"", o.""BalanceChange"", b.""Hash""
                FROM        ""MigrationOps"" as o
                INNER JOIN  ""Blocks"" as b 
                        ON  b.""Level"" = o.""Level""
                WHERE       o.""AccountId"" = @accountId
                AND         o.""Timestamp"" >= @from
                AND         o.""Timestamp"" < @to
                {Pagination("o", sort, offset, offsetMode, limit)}";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { accountId = account.Id, from, to });

            return rows.Select(row => new MigrationOperation
            {
                Id = row.Id,
                Level = row.Level,
                Block = row.Hash,
                Timestamp = row.Timestamp,
                Account = Accounts.GetAlias(account.Id),
                Kind = MigrationKindToString(row.Kind),
                BalanceChange = row.BalanceChange,
                Quote = Quotes.Get(quotes, row.Level)
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

        public async Task<IEnumerable<RevelationPenaltyOperation>> GetRevelationPenalties(
            Int32Parameter level,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            Symbols quotes)
        {
            var sql = new SqlBuilder(@"SELECT o.*, b.""Hash"" FROM ""RevelationPenaltyOps"" AS o INNER JOIN ""Blocks"" as b ON b.""Level"" = o.""Level""")
                .FilterA(@"o.""Level""", level)
                .Take(sort, offset, limit, x => x == "level" ? ("Id", "Level") : ("Id", "Id"), "o");

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            return rows.Select(row => new RevelationPenaltyOperation
            {
                Id = row.Id,
                Level = row.Level,
                Block = row.Hash,
                Timestamp = row.Timestamp,
                Baker = Accounts.GetAlias(row.BakerId),
                MissedLevel = row.MissedLevel,
                LostReward = row.LostReward,
                LostFees = row.LostFees,
                Quote = Quotes.Get(quotes, row.Level)
            });
        }

        public async Task<object[][]> GetRevelationPenalties(
            Int32Parameter level,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            string[] fields,
            Symbols quotes)
        {
            var columns = new HashSet<string>(fields.Length);
            var joins = new HashSet<string>(1);

            foreach (var field in fields)
            {
                switch (field)
                {
                    case "id": columns.Add(@"o.""Id"""); break;
                    case "level": columns.Add(@"o.""Level"""); break;
                    case "timestamp": columns.Add(@"o.""Timestamp"""); break;
                    case "baker": columns.Add(@"o.""BakerId"""); break;
                    case "missedLevel": columns.Add(@"o.""MissedLevel"""); break;
                    case "lostReward": columns.Add(@"o.""LostReward"""); break;
                    case "lostFees": columns.Add(@"o.""LostFees"""); break;
                    case "block":
                        columns.Add(@"b.""Hash""");
                        joins.Add(@"INNER JOIN ""Blocks"" as b ON b.""Level"" = o.""Level""");
                        break;
                    case "quote": columns.Add(@"o.""Level"""); break;
                }
            }

            if (columns.Count == 0)
                return Array.Empty<object[]>();

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""RevelationPenaltyOps"" as o {string.Join(' ', joins)}")
                .FilterA(@"o.""Level""", level)
                .Take(sort, offset, limit, x => x == "level" ? ("Id", "Level") : ("Id", "Id"), "o");

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
                    case "block":
                        foreach (var row in rows)
                            result[j++][i] = row.Hash;
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
                    case "quote":
                        foreach (var row in rows)
                            result[j++][i] = Quotes.Get(quotes, row.Level);
                        break;
                }
            }

            return result;
        }

        public async Task<object[]> GetRevelationPenalties(
            Int32Parameter level,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            string field,
            Symbols quotes)
        {
            var columns = new HashSet<string>(1);
            var joins = new HashSet<string>(1);

            switch (field)
            {
                case "id": columns.Add(@"o.""Id"""); break;
                case "level": columns.Add(@"o.""Level"""); break;
                case "timestamp": columns.Add(@"o.""Timestamp"""); break;
                case "baker": columns.Add(@"o.""BakerId"""); break;
                case "missedLevel": columns.Add(@"o.""MissedLevel"""); break;
                case "lostReward": columns.Add(@"o.""LostReward"""); break;
                case "lostFees": columns.Add(@"o.""LostFees"""); break;
                case "block":
                    columns.Add(@"b.""Hash""");
                    joins.Add(@"INNER JOIN ""Blocks"" as b ON b.""Level"" = o.""Level""");
                    break;
                case "quote": columns.Add(@"o.""Level"""); break;
            }

            if (columns.Count == 0)
                return Array.Empty<object>();

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""RevelationPenaltyOps"" as o {string.Join(' ', joins)}")
                .FilterA(@"o.""Level""", level)
                .Take(sort, offset, limit, x => x == "level" ? ("Id", "Level") : ("Id", "Id"), "o");

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
                case "block":
                    foreach (var row in rows)
                        result[j++] = row.Hash;
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
                case "quote":
                    foreach (var row in rows)
                        result[j++] = Quotes.Get(quotes, row.Level);
                    break;
            }

            return result;
        }

        public async Task<IEnumerable<RevelationPenaltyOperation>> GetRevelationPenalties(RawAccount account, SortMode sort, int offset, OffsetMode offsetMode, int limit, Symbols quotes)
        {
            var sql = $@"
                SELECT      o.*, b.""Hash""
                FROM        ""RevelationPenaltyOps"" as o
                INNER JOIN  ""Blocks"" as b 
                        ON  b.""Level"" = o.""Level""
                WHERE       o.""BakerId"" = @accountId
                {Pagination("o", sort, offset, offsetMode, limit)}";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { accountId = account.Id });

            return rows.Select(row => new RevelationPenaltyOperation
            {
                Id = row.Id,
                Level = row.Level,
                Block = row.Hash,
                Timestamp = row.Timestamp,
                Baker = Accounts.GetAlias(row.BakerId),
                MissedLevel = row.MissedLevel,
                LostReward = row.LostReward,
                LostFees = row.LostFees,
                Quote = Quotes.Get(quotes, row.Level)
            });
        }

        public async Task<IEnumerable<RevelationPenaltyOperation>> GetRevelationPenalties(RawAccount account, DateTime from, DateTime to, SortMode sort, int offset, OffsetMode offsetMode, int limit, Symbols quotes)
        {
            var sql = $@"
                SELECT      o.*, b.""Hash""
                FROM        ""RevelationPenaltyOps"" as o
                INNER JOIN  ""Blocks"" as b 
                        ON  b.""Level"" = o.""Level""
                WHERE       o.""BakerId"" = @accountId
                AND         o.""Timestamp"" >= @from
                AND         o.""Timestamp"" < @to
                {Pagination("o", sort, offset, offsetMode, limit)}";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { accountId = account.Id, from, to });

            return rows.Select(row => new RevelationPenaltyOperation
            {
                Id = row.Id,
                Level = row.Level,
                Block = row.Hash,
                Timestamp = row.Timestamp,
                Baker = Accounts.GetAlias(row.BakerId),
                MissedLevel = row.MissedLevel,
                LostReward = row.LostReward,
                LostFees = row.LostFees,
                Quote = Quotes.Get(quotes, row.Level)
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

        public async Task<IEnumerable<BakingOperation>> GetBakings(
            Int32Parameter level,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            Symbols quotes)
        {
            var sql = new SqlBuilder(@"SELECT ""Id"", ""Level"", ""Timestamp"", ""BakerId"", ""Hash"", ""Priority"", ""Reward"", ""Fees"" FROM ""Blocks""")
                .Filter(@"""BakerId"" IS NOT NULL")
                .Filter("Level", level)
                .Take(sort, offset, limit, x => x == "level" ? ("Id", "Level") : ("Id", "Id"));

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
                Fees = row.Fees,
                Quote = Quotes.Get(quotes, row.Level)
            });
        }

        public async Task<object[][]> GetBakings(
            Int32Parameter level,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            string[] fields,
            Symbols quotes)
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
                    case "quote": columns.Add(@"""Level"""); break;
                }
            }

            if (columns.Count == 0)
                return Array.Empty<object[]>();

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""Blocks""")
                .Filter(@"""BakerId"" IS NOT NULL")
                .Filter("Level", level)
                .Take(sort, offset, limit, x => x == "level" ? ("Id", "Level") : ("Id", "Id"));

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
                    case "quote":
                        foreach (var row in rows)
                            result[j++][i] = Quotes.Get(quotes, row.Level);
                        break;
                }
            }

            return result;
        }

        public async Task<object[]> GetBakings(
            Int32Parameter level,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            string field,
            Symbols quotes)
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
                case "quote": columns.Add(@"""Level"""); break;
            }

            if (columns.Count == 0)
                return Array.Empty<object>();

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""Blocks""")
                .Filter(@"""BakerId"" IS NOT NULL")
                .Filter("Level", level)
                .Take(sort, offset, limit, x => x == "level" ? ("Id", "Level") : ("Id", "Id"));

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
                case "quote":
                    foreach (var row in rows)
                        result[j++] = Quotes.Get(quotes, row.Level);
                    break;
            }

            return result;
        }

        public async Task<IEnumerable<BakingOperation>> GetBakings(RawAccount account, SortMode sort, int offset, OffsetMode offsetMode, int limit, Symbols quotes)
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
                Fees = row.Fees,
                Quote = Quotes.Get(quotes, row.Level)
            });
        }

        public async Task<IEnumerable<BakingOperation>> GetBakings(RawAccount account, DateTime from, DateTime to, SortMode sort, int offset, OffsetMode offsetMode, int limit, Symbols quotes)
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
                Fees = row.Fees,
                Quote = Quotes.Get(quotes, row.Level)
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
                    ORDER BY {alias}.""Id"" {sortMode}
                    LIMIT    {limit}";
            }

            if (offsetMode == OffsetMode.Id)
            {
                return sort == SortMode.Ascending
                    ? $@"
                        AND      {alias}.""Id"" > {offset}
                        ORDER BY {alias}.""Id"" {sortMode}
                        LIMIT    {limit}"
                    : $@"
                        AND      {alias}.""Id"" < {offset}
                        ORDER BY {alias}.""Id"" {sortMode}
                        LIMIT    {limit}";
            }

            var offsetValue = offsetMode == OffsetMode.Page ? limit * offset : offset;

            return $@"
                ORDER BY {alias}.""Id"" {sortMode}
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
