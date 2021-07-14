using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Netezos.Encoding;
using Dapper;

using Tzkt.Api.Models;
using Tzkt.Api.Services.Cache;

namespace Tzkt.Api.Repositories
{
    public class OperationRepository : DbConnection
    {
        readonly AccountsCache Accounts;
        readonly QuotesCache Quotes;

        public OperationRepository(AccountsCache accounts, QuotesCache quotes, IConfiguration config) : base(config)
        {
            Accounts = accounts;
            Quotes = quotes;
        }

        #region operations
        public async Task<IEnumerable<Operation>> Get(string hash, MichelineFormat format, Symbols quote)
        {
            #region test manager operations
            var delegations = GetDelegations(hash, quote);
            var originations = GetOriginations(hash, format, quote);
            var transactions = GetTransactions(hash, format, quote);
            var reveals = GetReveals(hash, quote);

            await Task.WhenAll(delegations, originations, transactions, reveals);

            var managerOps = ((IEnumerable<Operation>)delegations.Result)
                .Concat(originations.Result)
                .Concat(transactions.Result)
                .Concat(reveals.Result);

            if (managerOps.Any())
                return managerOps.OrderBy(x => x.Id);
            #endregion

            #region less likely
            var activations = GetActivations(hash, quote);
            var proposals = GetProposals(hash, quote);
            var ballots = GetBallots(hash, quote);

            await Task.WhenAll(activations, proposals, ballots);

            if (activations.Result.Any())
                return activations.Result;

            if (proposals.Result.Any())
                return proposals.Result;

            if (ballots.Result.Any())
                return ballots.Result;
            #endregion

            #region very unlikely
            var endorsements = GetEndorsements(hash, quote);
            var dobleBaking = GetDoubleBakings(hash, quote);
            var doubleEndorsing = GetDoubleEndorsings(hash, quote);
            var nonceRevelation = GetNonceRevelations(hash, quote);

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

        public async Task<IEnumerable<Operation>> Get(string hash, int counter, MichelineFormat format, Symbols quote)
        {
            var delegations = GetDelegations(hash, counter, quote);
            var originations = GetOriginations(hash, counter, format, quote);
            var transactions = GetTransactions(hash, counter, format, quote);
            var reveals = GetReveals(hash, counter, quote);

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

        public async Task<IEnumerable<Operation>> Get(string hash, int counter, int nonce, MichelineFormat format, Symbols quote)
        {
            var delegations = GetDelegations(hash, counter, nonce, quote);
            var originations = GetOriginations(hash, counter, nonce, format, quote);
            var transactions = GetTransactions(hash, counter, nonce, format, quote);

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
        public async Task<int> GetEndorsementsCount(
            Int32Parameter level,
            DateTimeParameter timestamp)
        {
            var sql = new SqlBuilder(@"SELECT COUNT(*) FROM ""EndorsementOps""")
                .Filter("Level", level)
                .Filter("Timestamp", timestamp);

            using var db = GetConnection();
            return await db.QueryFirstAsync<int>(sql.Query, sql.Params);
        }

        public async Task<IEnumerable<EndorsementOperation>> GetEndorsements(string hash, Symbols quote)
        {
            var sql = @"
                SELECT      o.""Id"", o.""Level"", o.""Timestamp"", o.""DelegateId"", o.""Slots"", o.""Reward"", o.""Deposit"", b.""Hash""
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
                Deposit = row.Deposit,
                Quote = Quotes.Get(quote, row.Level)
            });
        }

        public async Task<IEnumerable<EndorsementOperation>> GetEndorsements(Block block, Symbols quote)
        {
            var sql = @"
                SELECT      ""Id"", ""Timestamp"", ""OpHash"", ""DelegateId"", ""Slots"", ""Reward"", ""Deposit""
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
                Deposit = row.Deposit,
                Quote = Quotes.Get(quote, block.Level)
            });
        }

        public async Task<IEnumerable<EndorsementOperation>> GetEndorsements(
            AccountParameter delegat,
            Int32Parameter level,
            DateTimeParameter timestamp,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            Symbols quote)
        {
            var sql = new SqlBuilder(@"SELECT o.*, b.""Hash"" FROM ""EndorsementOps"" AS o INNER JOIN ""Blocks"" as b ON b.""Level"" = o.""Level""")
                .Filter("DelegateId", delegat)
                .FilterA(@"o.""Level""", level)
                .FilterA(@"o.""Timestamp""", timestamp)
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
                Deposit = row.Deposit,
                Quote = Quotes.Get(quote, row.Level)
            });
        }

        public async Task<object[][]> GetEndorsements(
            AccountParameter delegat,
            Int32Parameter level,
            DateTimeParameter timestamp,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            string[] fields,
            Symbols quote)
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
                    case "deposit": columns.Add(@"o.""Deposit"""); break;
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
                .Filter("DelegateId", delegat)
                .FilterA(@"o.""Level""", level)
                .FilterA(@"o.""Timestamp""", timestamp)
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
                    case "deposit":
                        foreach (var row in rows)
                            result[j++][i] = row.Deposit;
                        break;
                    case "quote":
                        foreach (var row in rows)
                            result[j++][i] = Quotes.Get(quote, row.Level);
                        break;
                }
            }

            return result;
        }

        public async Task<object[]> GetEndorsements(
            AccountParameter delegat,
            Int32Parameter level,
            DateTimeParameter timestamp,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            string field,
            Symbols quote)
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
                case "deposit": columns.Add(@"o.""Deposit"""); break;
                case "block":
                    columns.Add(@"b.""Hash""");
                    joins.Add(@"INNER JOIN ""Blocks"" as b ON b.""Level"" = o.""Level""");
                    break;
                case "quote": columns.Add(@"o.""Level"""); break;
            }

            if (columns.Count == 0)
                return Array.Empty<object>();

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""EndorsementOps"" as o {string.Join(' ', joins)}")
                .Filter("DelegateId", delegat)
                .FilterA(@"o.""Level""", level)
                .FilterA(@"o.""Timestamp""", timestamp)
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
                case "deposit":
                    foreach (var row in rows)
                        result[j++] = row.Deposit;
                    break;
                case "quote":
                    foreach (var row in rows)
                        result[j++] = Quotes.Get(quote, row.Level);
                    break;
            }

            return result;
        }
        #endregion

        #region ballots
        public async Task<int> GetBallotsCount(
            Int32Parameter level,
            DateTimeParameter timestamp)
        {
            var sql = new SqlBuilder(@"SELECT COUNT(*) FROM ""BallotOps""")
                .Filter("Level", level)
                .Filter("Timestamp", timestamp);

            using var db = GetConnection();
            return await db.QueryFirstAsync<int>(sql.Query, sql.Params);
        }

        public async Task<IEnumerable<BallotOperation>> GetBallots(string hash, Symbols quote)
        {
            var sql = @"
                SELECT      o.""Id"", o.""Level"", o.""Timestamp"", o.""SenderId"", o.""Rolls"", o.""Vote"", o.""Epoch"", o.""Period"",
                            b.""Hash"",
                            proposal.""Hash"" as ""ProposalHash"", proposal.""Metadata"" ->> 'alias' as ""ProposalAlias"",
                            period.""Kind"", period.""FirstLevel"", period.""LastLevel""
                FROM        ""BallotOps"" as o
                INNER JOIN  ""Blocks"" as b 
                        ON  b.""Level"" = o.""Level""
                INNER JOIN  ""Proposals"" as proposal
                        ON  proposal.""Id"" = o.""ProposalId""
                INNER JOIN  ""VotingPeriods"" as period
                        ON  period.""Index"" = o.""Period""
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
                    Index = row.Period,
                    Epoch = row.Epoch,
                    Kind = PeriodToString(row.Kind),
                    FirstLevel = row.FirstLevel,
                    LastLevel = row.LastLevel
                },
                Proposal = new ProposalAlias
                {
                    Hash = row.ProposalHash,
                    Alias = row.ProposalAlias
                },
                Delegate = Accounts.GetAlias(row.SenderId),
                Rolls = row.Rolls,
                Vote = VoteToString(row.Vote),
                Quote = Quotes.Get(quote, row.Level)
            });
        }

        public async Task<IEnumerable<BallotOperation>> GetBallots(Block block, Symbols quote)
        {
            var sql = @"
                SELECT      o.""Id"", o.""Timestamp"", o.""OpHash"", o.""SenderId"", o.""Rolls"", o.""Vote"", o.""Epoch"", o.""Period"",
                            proposal.""Hash"" as ""ProposalHash"", proposal.""Metadata"" ->> 'alias' as ""ProposalAlias"",
                            period.""Kind"", period.""FirstLevel"", period.""LastLevel""
                FROM        ""BallotOps"" as o
                INNER JOIN  ""Proposals"" as proposal
                        ON  proposal.""Id"" = o.""ProposalId""
                INNER JOIN  ""VotingPeriods"" as period
                        ON  period.""Index"" = o.""Period""
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
                    Index = row.Period,
                    Epoch = row.Epoch,
                    Kind = PeriodToString(row.Kind),
                    FirstLevel = row.FirstLevel,
                    LastLevel = row.LastLevel
                },
                Proposal = new ProposalAlias
                {
                    Hash = row.ProposalHash,
                    Alias = row.ProposalAlias
                },
                Delegate = Accounts.GetAlias(row.SenderId),
                Rolls = row.Rolls,
                Vote = VoteToString(row.Vote),
                Quote = Quotes.Get(quote, block.Level)
            });
        }

        public async Task<IEnumerable<BallotOperation>> GetBallots(
            AccountParameter sender,
            Int32Parameter level,
            DateTimeParameter timestamp,
            Int32Parameter epoch,
            Int32Parameter period,
            ProtocolParameter proposal,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            Symbols quote)
        {
            var sql = new SqlBuilder(@"
                SELECT      o.""Id"", o.""Level"", o.""Timestamp"", o.""OpHash"", o.""SenderId"", o.""Rolls"", o.""Vote"", o.""Epoch"", o.""Period"",
                            b.""Hash"",
                            proposal.""Hash"" as ""ProposalHash"", proposal.""Metadata"" ->> 'alias' as ""ProposalAlias"",
                            period.""Kind"", period.""FirstLevel"", period.""LastLevel""
                FROM        ""BallotOps"" as o
                INNER JOIN  ""Blocks"" as b ON b.""Level"" = o.""Level""
                INNER JOIN  ""Proposals"" as proposal ON proposal.""Id"" = o.""ProposalId""
                INNER JOIN  ""VotingPeriods"" as period ON period.""Index"" = o.""Period""
                ")
                .Filter("SenderId", sender)
                .FilterA(@"o.""Level""", level)
                .FilterA(@"o.""Timestamp""", timestamp)
                .FilterA(@"o.""Epoch""", epoch)
                .FilterA(@"o.""Period""", period)
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
                    Index = row.Period,
                    Epoch = row.Epoch,
                    Kind = PeriodToString(row.Kind),
                    FirstLevel = row.FirstLevel,
                    LastLevel = row.LastLevel
                },
                Proposal = new ProposalAlias
                {
                    Hash = row.ProposalHash,
                    Alias = row.ProposalAlias
                },
                Delegate = Accounts.GetAlias(row.SenderId),
                Rolls = row.Rolls,
                Vote = VoteToString(row.Vote),
                Quote = Quotes.Get(quote, row.Level)
            });
        }

        public async Task<object[][]> GetBallots(
            AccountParameter sender,
            Int32Parameter level,
            DateTimeParameter timestamp,
            Int32Parameter epoch,
            Int32Parameter period,
            ProtocolParameter proposal,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            string[] fields,
            Symbols quote)
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
                        columns.Add(@"proposal.""Hash"" as ""ProposalHash""");
                        columns.Add(@"proposal.""Metadata""->> 'alias' as ""ProposalAlias""");
                        joins.Add(@"INNER JOIN ""Proposals"" as proposal ON proposal.""Id"" = o.""ProposalId""");
                        break;
                    case "period": 
                        columns.Add(@"o.""Epoch""");
                        columns.Add(@"o.""Period""");
                        columns.Add(@"period.""Kind""");
                        columns.Add(@"period.""FirstLevel""");
                        columns.Add(@"period.""LastLevel""");
                        joins.Add(@"INNER JOIN ""VotingPeriods"" as period ON period.""Index"" = o.""Period""");
                        break;
                    case "block":
                        columns.Add(@"b.""Hash""");
                        joins.Add(@"INNER JOIN ""Blocks"" as b ON b.""Level"" = o.""Level""");
                        break;
                    case "quote": columns.Add(@"o.""Level"""); break;
                }
            }

            if (period != null)
                joins.Add(@"INNER JOIN ""VotingPeriods"" as period ON period.""Index"" = o.""Period""");

            if (proposal != null)
                joins.Add(@"INNER JOIN ""Proposals"" as proposal ON proposal.""Id"" = o.""ProposalId""");

            if (columns.Count == 0)
                return Array.Empty<object[]>();

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""BallotOps"" as o {string.Join(' ', joins)}")
                .Filter("SenderId", sender)
                .FilterA(@"o.""Level""", level)
                .FilterA(@"o.""Timestamp""", timestamp)
                .FilterA(@"o.""Epoch""", epoch)
                .FilterA(@"o.""Period""", period)
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
                                Index = row.Period,
                                Epoch = row.Epoch,
                                Kind = PeriodToString(row.Kind),
                                FirstLevel = row.FirstLevel,
                                LastLevel = row.LastLevel
                            };
                        break;
                    case "proposal":
                        foreach (var row in rows)
                            result[j++][i] = new ProposalAlias
                            {
                                Hash = row.ProposalHash,
                                Alias = row.ProposalAlias
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
                            result[j++][i] = Quotes.Get(quote, row.Level);
                        break;
                }
            }

            return result;
        }

        public async Task<object[]> GetBallots(
            AccountParameter sender,
            Int32Parameter level,
            DateTimeParameter timestamp,
            Int32Parameter epoch,
            Int32Parameter period,
            ProtocolParameter proposal,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            string field,
            Symbols quote)
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
                    columns.Add(@"proposal.""Hash"" as ""ProposalHash""");
                    columns.Add(@"proposal.""Metadata""->> 'alias' as ""ProposalAlias""");
                    joins.Add(@"INNER JOIN ""Proposals"" as proposal ON proposal.""Id"" = o.""ProposalId""");
                    break;
                case "period":
                    columns.Add(@"o.""Epoch""");
                    columns.Add(@"o.""Period""");
                    columns.Add(@"period.""Kind""");
                    columns.Add(@"period.""FirstLevel""");
                    columns.Add(@"period.""LastLevel""");
                    joins.Add(@"INNER JOIN ""VotingPeriods"" as period ON period.""Index"" = o.""Period""");
                    break;
                case "block":
                    columns.Add(@"b.""Hash""");
                    joins.Add(@"INNER JOIN ""Blocks"" as b ON b.""Level"" = o.""Level""");
                    break;
                case "quote": columns.Add(@"o.""Level"""); break;
            }

            if (period != null)
                joins.Add(@"INNER JOIN ""VotingPeriods"" as period ON period.""Index"" = o.""Period""");

            if (proposal != null)
                joins.Add(@"INNER JOIN ""Proposals"" as proposal ON proposal.""Id"" = o.""ProposalId""");

            if (columns.Count == 0)
                return Array.Empty<object>();

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""BallotOps"" as o {string.Join(' ', joins)}")
                .Filter("SenderId", sender)
                .FilterA(@"o.""Level""", level)
                .FilterA(@"o.""Timestamp""", timestamp)
                .FilterA(@"o.""Epoch""", epoch)
                .FilterA(@"o.""Period""", period)
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
                            Index = row.Period,
                            Epoch = row.Epoch,
                            Kind = PeriodToString(row.Kind),
                            FirstLevel = row.FirstLevel,
                            LastLevel = row.LastLevel
                        };
                    break;
                case "proposal":
                    foreach (var row in rows)
                        result[j++] = new ProposalAlias
                        {
                            Hash = row.ProposalHash,
                            Alias = row.ProposalAlias
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
                        result[j++] = Quotes.Get(quote, row.Level);
                    break;
            }

            return result;
        }
        #endregion

        #region proposals
        public async Task<int> GetProposalsCount(
            Int32Parameter level,
            DateTimeParameter timestamp)
        {
            var sql = new SqlBuilder(@"SELECT COUNT(*) FROM ""ProposalOps""")
                .Filter("Level", level)
                .Filter("Timestamp", timestamp);

            using var db = GetConnection();
            return await db.QueryFirstAsync<int>(sql.Query, sql.Params);
        }

        public async Task<IEnumerable<ProposalOperation>> GetProposals(string hash, Symbols quote)
        {
            var sql = @"
                SELECT      o.""Id"", o.""Level"", o.""Timestamp"", o.""SenderId"", o.""Rolls"", o.""Duplicated"", o.""Epoch"", o.""Period"",
                            b.""Hash"",
                            proposal.""Hash"" as ""ProposalHash"", proposal.""Metadata"" ->> 'alias' as ""ProposalAlias"",
                            period.""Kind"", period.""FirstLevel"", period.""LastLevel""
                FROM        ""ProposalOps"" as o
                INNER JOIN  ""Blocks"" as b 
                        ON  b.""Level"" = o.""Level""
                INNER JOIN  ""Proposals"" as proposal
                        ON  proposal.""Id"" = o.""ProposalId""
                INNER JOIN  ""VotingPeriods"" as period
                        ON  period.""Index"" = o.""Period""
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
                    Index = row.Period,
                    Epoch = row.Epoch,
                    Kind = PeriodToString(row.Kind),
                    FirstLevel = row.FirstLevel,
                    LastLevel = row.LastLevel
                },
                Proposal = new ProposalAlias
                {
                    Hash = row.ProposalHash,
                    Alias = row.ProposalAlias
                },
                Delegate = Accounts.GetAlias(row.SenderId),
                Quote = Quotes.Get(quote, row.Level)
            });
        }

        public async Task<IEnumerable<ProposalOperation>> GetProposals(Block block, Symbols quote)
        {
            var sql = @"
                SELECT      o.""Id"", o.""Timestamp"", o.""OpHash"", o.""SenderId"", o.""Rolls"", o.""Duplicated"", o.""Epoch"", o.""Period"",
                            proposal.""Hash"" as ""ProposalHash"", proposal.""Metadata"" ->> 'alias' as ""ProposalAlias"",
                            period.""Kind"", period.""FirstLevel"", period.""LastLevel""
                FROM        ""ProposalOps"" as o
                INNER JOIN  ""Proposals"" as proposal
                        ON  proposal.""Id"" = o.""ProposalId""
                INNER JOIN  ""VotingPeriods"" as period
                        ON  period.""Index"" = o.""Period""
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
                    Index = row.Period,
                    Epoch = row.Epoch,
                    Kind = PeriodToString(row.Kind),
                    FirstLevel = row.FirstLevel,
                    LastLevel = row.LastLevel
                },
                Proposal = new ProposalAlias
                {
                    Hash = row.ProposalHash,
                    Alias = row.ProposalAlias
                },
                Delegate = Accounts.GetAlias(row.SenderId),
                Quote = Quotes.Get(quote, block.Level)
            });
        }

        public async Task<IEnumerable<ProposalOperation>> GetProposals(
            AccountParameter sender,
            Int32Parameter level,
            DateTimeParameter timestamp,
            Int32Parameter epoch,
            Int32Parameter period,
            ProtocolParameter proposal,
            BoolParameter duplicated,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            Symbols quote)
        {
            var sql = new SqlBuilder(@"
                SELECT      o.""Id"", o.""Level"", o.""Timestamp"", o.""OpHash"", o.""SenderId"", o.""Rolls"", o.""Duplicated"", o.""Epoch"", o.""Period"",
                            b.""Hash"",
                            proposal.""Hash"" as ""ProposalHash"", proposal.""Metadata"" ->> 'alias' as ""ProposalAlias"",
                            period.""Kind"", period.""FirstLevel"", period.""LastLevel""
                FROM        ""ProposalOps"" as o
                INNER JOIN  ""Blocks"" as b ON b.""Level"" = o.""Level""
                INNER JOIN  ""Proposals"" as proposal ON proposal.""Id"" = o.""ProposalId""
                INNER JOIN  ""VotingPeriods"" as period ON period.""Index"" = o.""Period""
                ")
                .Filter("SenderId", sender)
                .FilterA(@"o.""Level""", level)
                .FilterA(@"o.""Timestamp""", timestamp)
                .FilterA(@"o.""Duplicated""", duplicated)
                .FilterA(@"o.""Epoch""", epoch)
                .FilterA(@"o.""Period""", period)
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
                    Index = row.Period,
                    Epoch = row.Epoch,
                    Kind = PeriodToString(row.Kind),
                    FirstLevel = row.FirstLevel,
                    LastLevel = row.LastLevel
                },
                Proposal = new ProposalAlias
                {
                    Hash = row.ProposalHash,
                    Alias = row.ProposalAlias
                },
                Delegate = Accounts.GetAlias(row.SenderId),
                Quote = Quotes.Get(quote, row.Level)
            });
        }

        public async Task<object[][]> GetProposals(
            AccountParameter sender,
            Int32Parameter level,
            DateTimeParameter timestamp,
            Int32Parameter epoch,
            Int32Parameter period,
            ProtocolParameter proposal,
            BoolParameter duplicated,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            string[] fields,
            Symbols quote)
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
                        columns.Add(@"proposal.""Hash"" as ""ProposalHash""");
                        columns.Add(@"proposal.""Metadata"" ->> 'alias' as ""ProposalAlias""");
                        joins.Add(@"INNER JOIN ""Proposals"" as proposal ON proposal.""Id"" = o.""ProposalId""");
                        break;
                    case "period":
                        columns.Add(@"o.""Epoch""");
                        columns.Add(@"o.""Period""");
                        columns.Add(@"period.""Kind""");
                        columns.Add(@"period.""FirstLevel""");
                        columns.Add(@"period.""LastLevel""");
                        joins.Add(@"INNER JOIN ""VotingPeriods"" as period ON period.""Index"" = o.""Period""");
                        break;
                    case "block":
                        columns.Add(@"b.""Hash""");
                        joins.Add(@"INNER JOIN ""Blocks"" as b ON b.""Level"" = o.""Level""");
                        break;
                    case "quote": columns.Add(@"o.""Level"""); break;
                }
            }

            if (period != null)
                joins.Add(@"INNER JOIN ""VotingPeriods"" as period ON period.""Index"" = o.""Period""");

            if (proposal != null)
                joins.Add(@"INNER JOIN ""Proposals"" as proposal ON proposal.""Id"" = o.""ProposalId""");

            if (columns.Count == 0)
                return Array.Empty<object[]>();

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""ProposalOps"" as o {string.Join(' ', joins)}")
                .Filter("SenderId", sender)
                .FilterA(@"o.""Level""", level)
                .FilterA(@"o.""Timestamp""", timestamp)
                .FilterA(@"o.""Duplicated""", duplicated)
                .FilterA(@"o.""Epoch""", epoch)
                .FilterA(@"o.""Period""", period)
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
                                Index = row.Period,
                                Epoch = row.Epoch,
                                Kind = PeriodToString(row.Kind),
                                FirstLevel = row.FirstLevel,
                                LastLevel = row.LastLevel
                            };
                        break;
                    case "proposal":
                        foreach (var row in rows)
                            result[j++][i] = new ProposalAlias
                            {
                                Hash = row.ProposalHash,
                                Alias = row.ProposalAlias
                            };
                        break;
                    case "delegate":
                        foreach (var row in rows)
                            result[j++][i] = await Accounts.GetAliasAsync(row.SenderId);
                        break;
                    case "quote":
                        foreach (var row in rows)
                            result[j++][i] = Quotes.Get(quote, row.Level);
                        break;
                }
            }

            return result;
        }

        public async Task<object[]> GetProposals(
            AccountParameter sender,
            Int32Parameter level,
            DateTimeParameter timestamp,
            Int32Parameter epoch,
            Int32Parameter period,
            ProtocolParameter proposal,
            BoolParameter duplicated,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            string field,
            Symbols quote)
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
                    columns.Add(@"proposal.""Hash"" as ""ProposalHash""");
                    columns.Add(@"proposal.""Metadata"" ->> 'alias' as ""ProposalAlias""");
                    joins.Add(@"INNER JOIN ""Proposals"" as proposal ON proposal.""Id"" = o.""ProposalId""");
                    break;
                case "period":
                    columns.Add(@"o.""Epoch""");
                    columns.Add(@"o.""Period""");
                    columns.Add(@"period.""Kind""");
                    columns.Add(@"period.""FirstLevel""");
                    columns.Add(@"period.""LastLevel""");
                    joins.Add(@"INNER JOIN ""VotingPeriods"" as period ON period.""Index"" = o.""Period""");
                    break;
                case "block":
                    columns.Add(@"b.""Hash""");
                    joins.Add(@"INNER JOIN ""Blocks"" as b ON b.""Level"" = o.""Level""");
                    break;
                case "quote": columns.Add(@"o.""Level"""); break;
            }

            if (period != null)
                joins.Add(@"INNER JOIN ""VotingPeriods"" as period ON period.""Index"" = o.""Period""");

            if (proposal != null)
                joins.Add(@"INNER JOIN ""Proposals"" as proposal ON proposal.""Id"" = o.""ProposalId""");

            if (columns.Count == 0)
                return Array.Empty<object>();

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""ProposalOps"" as o {string.Join(' ', joins)}")
                .Filter("SenderId", sender)
                .FilterA(@"o.""Level""", level)
                .FilterA(@"o.""Timestamp""", timestamp)
                .FilterA(@"o.""Duplicated""", duplicated)
                .FilterA(@"o.""Epoch""", epoch)
                .FilterA(@"o.""Period""", period)
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
                            Index = row.Period,
                            Epoch = row.Epoch,
                            Kind = PeriodToString(row.Kind),
                            FirstLevel = row.FirstLevel,
                            LastLevel = row.LastLevel
                        };
                    break;
                case "proposal":
                    foreach (var row in rows)
                        result[j++] = new ProposalAlias
                        {
                            Hash = row.ProposalHash,
                            Alias = row.ProposalAlias
                        };
                    break;
                case "delegate":
                    foreach (var row in rows)
                        result[j++] = await Accounts.GetAliasAsync(row.SenderId);
                    break;
                case "quote":
                    foreach (var row in rows)
                        result[j++] = Quotes.Get(quote, row.Level);
                    break;
            }

            return result;
        }
        #endregion

        #region activations
        public async Task<int> GetActivationsCount(
            Int32Parameter level,
            DateTimeParameter timestamp)
        {
            var sql = new SqlBuilder(@"SELECT COUNT(*) FROM ""ActivationOps""")
                .Filter("Level", level)
                .Filter("Timestamp", timestamp);

            using var db = GetConnection();
            return await db.QueryFirstAsync<int>(sql.Query, sql.Params);
        }

        public async Task<IEnumerable<ActivationOperation>> GetActivations(string hash, Symbols quote)
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
                Quote = Quotes.Get(quote, row.Level)
            });
        }

        public async Task<IEnumerable<ActivationOperation>> GetActivations(Block block, Symbols quote)
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
                Quote = Quotes.Get(quote, block.Level)
            });
        }

        public async Task<IEnumerable<ActivationOperation>> GetActivations(
            AccountParameter account,
            Int32Parameter level,
            DateTimeParameter timestamp,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            Symbols quote)
        {
            var sql = new SqlBuilder(@"SELECT o.*, b.""Hash"" FROM ""ActivationOps"" AS o INNER JOIN ""Blocks"" as b ON b.""Level"" = o.""Level""")
                .Filter("AccountId", account)
                .FilterA(@"o.""Level""", level)
                .FilterA(@"o.""Timestamp""", timestamp)
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
                Quote = Quotes.Get(quote, row.Level)
            });
        }

        public async Task<object[][]> GetActivations(
            AccountParameter account,
            Int32Parameter level,
            DateTimeParameter timestamp,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            string[] fields,
            Symbols quote)
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
                .Filter("AccountId", account)
                .FilterA(@"o.""Level""", level)
                .FilterA(@"o.""Timestamp""", timestamp)
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
                            result[j++][i] = Quotes.Get(quote, row.Level);
                        break;
                }
            }

            return result;
        }

        public async Task<object[]> GetActivations(
            AccountParameter account,
            Int32Parameter level,
            DateTimeParameter timestamp,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            string field,
            Symbols quote)
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
                .Filter("AccountId", account)
                .FilterA(@"o.""Level""", level)
                .FilterA(@"o.""Timestamp""", timestamp)
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
                        result[j++] = Quotes.Get(quote, row.Level);
                    break;
            }

            return result;
        }
        #endregion

        #region double baking
        public async Task<int> GetDoubleBakingsCount(
            Int32Parameter level,
            DateTimeParameter timestamp)
        {
            var sql = new SqlBuilder(@"SELECT COUNT(*) FROM ""DoubleBakingOps""")
                .Filter("Level", level)
                .Filter("Timestamp", timestamp);

            using var db = GetConnection();
            return await db.QueryFirstAsync<int>(sql.Query, sql.Params);
        }

        public async Task<IEnumerable<DoubleBakingOperation>> GetDoubleBakings(string hash, Symbols quote)
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
                Quote = Quotes.Get(quote, row.Level)
            });
        }

        public async Task<IEnumerable<DoubleBakingOperation>> GetDoubleBakings(Block block, Symbols quote)
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
                Quote = Quotes.Get(quote, block.Level)
            });
        }

        public async Task<IEnumerable<DoubleBakingOperation>> GetDoubleBakings(
            AnyOfParameter anyof,
            AccountParameter accuser,
            AccountParameter offender,
            Int32Parameter level,
            DateTimeParameter timestamp,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            Symbols quote)
        {
            var sql = new SqlBuilder(@"SELECT o.*, b.""Hash"" FROM ""DoubleBakingOps"" AS o INNER JOIN ""Blocks"" as b ON b.""Level"" = o.""Level""")
                .Filter(anyof, x => x == "accuser" ? "AccuserId" : "OffenderId")
                .Filter("AccuserId", accuser, x => "OffenderId")
                .Filter("OffenderId", offender, x => "AccuserId")
                .FilterA(@"o.""Level""", level)
                .FilterA(@"o.""Timestamp""", timestamp)
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
                Quote = Quotes.Get(quote, row.Level)
            });
        }

        public async Task<object[][]> GetDoubleBakings(
            AnyOfParameter anyof,
            AccountParameter accuser,
            AccountParameter offender,
            Int32Parameter level,
            DateTimeParameter timestamp,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            string[] fields,
            Symbols quote)
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
                .Filter(anyof, x => x == "accuser" ? "AccuserId" : "OffenderId")
                .Filter("AccuserId", accuser, x => "OffenderId")
                .Filter("OffenderId", offender, x => "AccuserId")
                .FilterA(@"o.""Level""", level)
                .FilterA(@"o.""Timestamp""", timestamp)
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
                            result[j++][i] = Quotes.Get(quote, row.Level);
                        break;
                }
            }

            return result;
        }

        public async Task<object[]> GetDoubleBakings(
            AnyOfParameter anyof,
            AccountParameter accuser,
            AccountParameter offender,
            Int32Parameter level,
            DateTimeParameter timestamp,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            string field,
            Symbols quote)
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
                .Filter(anyof, x => x == "accuser" ? "AccuserId" : "OffenderId")
                .Filter("AccuserId", accuser, x => "OffenderId")
                .Filter("OffenderId", offender, x => "AccuserId")
                .FilterA(@"o.""Level""", level)
                .FilterA(@"o.""Timestamp""", timestamp)
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
                        result[j++] = Quotes.Get(quote, row.Level);
                    break;
            }

            return result;
        }
        #endregion

        #region double endorsing
        public async Task<int> GetDoubleEndorsingsCount(
            Int32Parameter level,
            DateTimeParameter timestamp)
        {
            var sql = new SqlBuilder(@"SELECT COUNT(*) FROM ""DoubleEndorsingOps""")
                .Filter(@"Level", level)
                .Filter(@"Timestamp", timestamp);

            using var db = GetConnection();
            return await db.QueryFirstAsync<int>(sql.Query, sql.Params);
        }

        public async Task<IEnumerable<DoubleEndorsingOperation>> GetDoubleEndorsings(string hash, Symbols quote)
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
                Quote = Quotes.Get(quote, row.Level)
            });
        }

        public async Task<IEnumerable<DoubleEndorsingOperation>> GetDoubleEndorsings(Block block, Symbols quote)
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
                Quote = Quotes.Get(quote, block.Level)
            });
        }

        public async Task<IEnumerable<DoubleEndorsingOperation>> GetDoubleEndorsings(
            AnyOfParameter anyof,
            AccountParameter accuser,
            AccountParameter offender,
            Int32Parameter level,
            DateTimeParameter timestamp,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            Symbols quote)
        {
            var sql = new SqlBuilder(@"SELECT o.*, b.""Hash"" FROM ""DoubleEndorsingOps"" AS o INNER JOIN ""Blocks"" as b ON b.""Level"" = o.""Level""")
                .Filter(anyof, x => x == "accuser" ? "AccuserId" : "OffenderId")
                .Filter("AccuserId", accuser, x => "OffenderId")
                .Filter("OffenderId", offender, x => "AccuserId")
                .FilterA(@"o.""Level""", level)
                .FilterA(@"o.""Timestamp""", timestamp)
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
                Quote = Quotes.Get(quote, row.Level)
            });
        }

        public async Task<object[][]> GetDoubleEndorsings(
            AnyOfParameter anyof,
            AccountParameter accuser,
            AccountParameter offender,
            Int32Parameter level,
            DateTimeParameter timestamp,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            string[] fields,
            Symbols quote)
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
                .Filter(anyof, x => x == "accuser" ? "AccuserId" : "OffenderId")
                .Filter("AccuserId", accuser, x => "OffenderId")
                .Filter("OffenderId", offender, x => "AccuserId")
                .FilterA(@"o.""Level""", level)
                .FilterA(@"o.""Timestamp""", timestamp)
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
                            result[j++][i] = Quotes.Get(quote, row.Level);
                        break;
                }
            }

            return result;
        }

        public async Task<object[]> GetDoubleEndorsings(
            AnyOfParameter anyof,
            AccountParameter accuser,
            AccountParameter offender,
            Int32Parameter level,
            DateTimeParameter timestamp,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            string field,
            Symbols quote)
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
                .Filter(anyof, x => x == "accuser" ? "AccuserId" : "OffenderId")
                .Filter("AccuserId", accuser, x => "OffenderId")
                .Filter("OffenderId", offender, x => "AccuserId")
                .FilterA(@"o.""Level""", level)
                .FilterA(@"o.""Timestamp""", timestamp)
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
                        result[j++] = Quotes.Get(quote, row.Level);
                    break;
            }

            return result;
        }
        #endregion

        #region nonce revelations
        public async Task<int> GetNonceRevelationsCount(
            Int32Parameter level,
            DateTimeParameter timestamp)
        {
            var sql = new SqlBuilder(@"SELECT COUNT(*) FROM ""NonceRevelationOps""")
                .Filter("Level", level)
                .Filter("Timestamp", timestamp);

            using var db = GetConnection();
            return await db.QueryFirstAsync<int>(sql.Query, sql.Params);
        }

        public async Task<IEnumerable<NonceRevelationOperation>> GetNonceRevelations(string hash, Symbols quote)
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
                Quote = Quotes.Get(quote, row.Level)
            });
        }

        public async Task<IEnumerable<NonceRevelationOperation>> GetNonceRevelations(Block block, Symbols quote)
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
                Quote = Quotes.Get(quote, block.Level)
            });
        }

        public async Task<IEnumerable<NonceRevelationOperation>> GetNonceRevelations(
            AnyOfParameter anyof,
            AccountParameter baker,
            AccountParameter sender,
            Int32Parameter level,
            DateTimeParameter timestamp,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            Symbols quote)
        {
            var sql = new SqlBuilder(@"SELECT o.*, b.""Hash"" FROM ""NonceRevelationOps"" AS o INNER JOIN ""Blocks"" as b ON b.""Level"" = o.""Level""")
                .FilterA(anyof, x => x == "baker" ? @"o.""BakerId""" : @"o.""SenderId""")
                .FilterA(@"o.""BakerId""", baker, x => @"o.""SenderId""")
                .FilterA(@"o.""SenderId""", sender, x => @"o.""BakerId""")
                .FilterA(@"o.""Level""", level)
                .FilterA(@"o.""Timestamp""", timestamp)
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
                Quote = Quotes.Get(quote, row.Level)
            });
        }

        public async Task<object[][]> GetNonceRevelations(
            AnyOfParameter anyof,
            AccountParameter baker,
            AccountParameter sender,
            Int32Parameter level,
            DateTimeParameter timestamp,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            string[] fields,
            Symbols quote)
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
                .FilterA(anyof, x => x == "baker" ? @"o.""BakerId""" : @"o.""SenderId""")
                .FilterA(@"o.""BakerId""", baker, x => @"o.""SenderId""")
                .FilterA(@"o.""SenderId""", sender, x => @"o.""BakerId""")
                .FilterA(@"o.""Level""", level)
                .FilterA(@"o.""Timestamp""", timestamp)
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
                            result[j++][i] = Quotes.Get(quote, row.Level);
                        break;
                }
            }

            return result;
        }

        public async Task<object[]> GetNonceRevelations(
            AnyOfParameter anyof,
            AccountParameter baker,
            AccountParameter sender,
            Int32Parameter level,
            DateTimeParameter timestamp,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            string field,
            Symbols quote)
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
                .FilterA(anyof, x => x == "baker" ? @"o.""BakerId""" : @"o.""SenderId""")
                .FilterA(@"o.""BakerId""", baker, x => @"o.""SenderId""")
                .FilterA(@"o.""SenderId""", sender, x => @"o.""BakerId""")
                .FilterA(@"o.""Level""", level)
                .FilterA(@"o.""Timestamp""", timestamp)
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
                        result[j++] = Quotes.Get(quote, row.Level);
                    break;
            }

            return result;
        }
        #endregion

        #region delegations
        public async Task<int> GetDelegationsCount(
            Int32Parameter level,
            DateTimeParameter timestamp)
        {
            var sql = new SqlBuilder(@"SELECT COUNT(*) FROM ""DelegationOps""")
                .Filter("Level", level)
                .Filter("Timestamp", timestamp);

            using var db = GetConnection();
            return await db.QueryFirstAsync<int>(sql.Query, sql.Params);
        }

        public async Task<IEnumerable<DelegationOperation>> GetDelegations(string hash, Symbols quote)
        {
            var sql = @"
                SELECT      o.""Id"", o.""Level"", o.""Timestamp"", o.""SenderId"", o.""InitiatorId"", o.""Counter"", o.""BakerFee"",
                            o.""GasLimit"", o.""GasUsed"", o.""Status"", o.""Nonce"", o.""Amount"", o.""PrevDelegateId"", o.""DelegateId"", o.""Errors"", b.""Hash""
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
                Amount = row.Amount,
                PrevDelegate = row.PrevDelegateId != null ? Accounts.GetAlias(row.PrevDelegateId) : null,
                NewDelegate = row.DelegateId != null ? Accounts.GetAlias(row.DelegateId) : null,
                Status = StatusToString(row.Status),
                Errors = row.Errors != null ? OperationErrorSerializer.Deserialize(row.Errors) : null,
                Quote = Quotes.Get(quote, row.Level)
            });
        }

        public async Task<IEnumerable<DelegationOperation>> GetDelegations(string hash, int counter, Symbols quote)
        {
            var sql = @"
                SELECT      o.""Id"", o.""Level"", o.""Timestamp"", o.""SenderId"", o.""InitiatorId"", o.""BakerFee"",
                            o.""GasLimit"", o.""GasUsed"", o.""Status"", o.""Nonce"", o.""Amount"", o.""PrevDelegateId"", o.""DelegateId"", o.""Errors"", b.""Hash""
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
                Amount = row.Amount,
                PrevDelegate = row.PrevDelegateId != null ? Accounts.GetAlias(row.PrevDelegateId) : null,
                NewDelegate = row.DelegateId != null ? Accounts.GetAlias(row.DelegateId) : null,
                Status = StatusToString(row.Status),
                Errors = row.Errors != null ? OperationErrorSerializer.Deserialize(row.Errors) : null,
                Quote = Quotes.Get(quote, row.Level)
            });
        }

        public async Task<IEnumerable<DelegationOperation>> GetDelegations(string hash, int counter, int nonce, Symbols quote)
        {
            var sql = @"
                SELECT      o.""Id"", o.""Level"", o.""Timestamp"", o.""SenderId"", o.""InitiatorId"", o.""BakerFee"",
                            o.""GasLimit"", o.""GasUsed"", o.""Status"", o.""Amount"", o.""PrevDelegateId"", o.""DelegateId"", o.""Errors"", b.""Hash""
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
                Amount = row.Amount,
                PrevDelegate = row.PrevDelegateId != null ? Accounts.GetAlias(row.PrevDelegateId) : null,
                NewDelegate = row.DelegateId != null ? Accounts.GetAlias(row.DelegateId) : null,
                Status = StatusToString(row.Status),
                Errors = row.Errors != null ? OperationErrorSerializer.Deserialize(row.Errors) : null,
                Quote = Quotes.Get(quote, row.Level)
            });
        }

        public async Task<IEnumerable<DelegationOperation>> GetDelegations(Block block, Symbols quote)
        {
            var sql = @"
                SELECT    ""Id"", ""Timestamp"", ""OpHash"", ""SenderId"", ""InitiatorId"", ""Counter"", ""BakerFee"",
                          ""GasLimit"", ""GasUsed"", ""Status"", ""Nonce"", ""Amount"", ""PrevDelegateId"", ""DelegateId"", ""Errors""
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
                Amount = row.Amount,
                PrevDelegate = row.PrevDelegateId != null ? Accounts.GetAlias(row.PrevDelegateId) : null,
                NewDelegate = row.DelegateId != null ? Accounts.GetAlias(row.DelegateId) : null,
                Status = StatusToString(row.Status),
                Errors = row.Errors != null ? OperationErrorSerializer.Deserialize(row.Errors) : null,
                Quote = Quotes.Get(quote, block.Level)
            });
        }

        public async Task<IEnumerable<DelegationOperation>> GetDelegations(
            AnyOfParameter anyof,
            AccountParameter initiator,
            AccountParameter sender,
            AccountParameter prevDelegate,
            AccountParameter newDelegate,
            Int32Parameter level,
            DateTimeParameter timestamp,
            OperationStatusParameter status,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            Symbols quote)
        {
            var sql = new SqlBuilder(@"SELECT o.*, b.""Hash"" FROM ""DelegationOps"" AS o INNER JOIN ""Blocks"" as b ON b.""Level"" = o.""Level""")
                .Filter(anyof, x => x switch
                {
                    "initiator" => "InitiatorId",
                    "sender" => "SenderId",
                    "prevDelegate" => "PrevDelegateId",
                    _ => "DelegateId"
                })
                .Filter("InitiatorId", initiator, x => x == "prevDelegate" ? "PrevDelegateId" : "DelegateId")
                .Filter("SenderId", sender, x => x == "prevDelegate" ? "PrevDelegateId" : "DelegateId")
                .Filter("PrevDelegateId", prevDelegate, x => x == "initiator" ? "InitiatorId" : x == "sender" ? "SenderId" : "DelegateId")
                .Filter("DelegateId", newDelegate, x => x == "initiator" ? "InitiatorId" : x == "sender" ? "SenderId" : "PrevDelegateId")
                .FilterA(@"o.""Level""", level)
                .FilterA(@"o.""Timestamp""", timestamp)
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
                Amount = row.Amount,
                PrevDelegate = row.PrevDelegateId != null ? Accounts.GetAlias(row.PrevDelegateId) : null,
                NewDelegate = row.DelegateId != null ? Accounts.GetAlias(row.DelegateId) : null,
                Status = StatusToString(row.Status),
                Errors = row.Errors != null ? OperationErrorSerializer.Deserialize(row.Errors) : null,
                Quote = Quotes.Get(quote, row.Level)
            });
        }

        public async Task<object[][]> GetDelegations(
            AnyOfParameter anyof,
            AccountParameter initiator,
            AccountParameter sender,
            AccountParameter prevDelegate,
            AccountParameter newDelegate,
            Int32Parameter level,
            DateTimeParameter timestamp,
            OperationStatusParameter status,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            string[] fields,
            Symbols quote)
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
                    case "amount": columns.Add(@"o.""Amount"""); break;
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
                .Filter(anyof, x => x switch
                {
                    "initiator" => "InitiatorId",
                    "sender" => "SenderId",
                    "prevDelegate" => "PrevDelegateId",
                    _ => "DelegateId"
                })
                .Filter("InitiatorId", initiator, x => x == "prevDelegate" ? "PrevDelegateId" : "DelegateId")
                .Filter("SenderId", sender, x => x == "prevDelegate" ? "PrevDelegateId" : "DelegateId")
                .Filter("PrevDelegateId", prevDelegate, x => x == "initiator" ? "InitiatorId" : x == "sender" ? "SenderId" : "DelegateId")
                .Filter("DelegateId", newDelegate, x => x == "initiator" ? "InitiatorId" : x == "sender" ? "SenderId" : "PrevDelegateId")
                .FilterA(@"o.""Level""", level)
                .FilterA(@"o.""Timestamp""", timestamp)
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
                    case "amount":
                        foreach (var row in rows)
                            result[j++][i] = row.Amount;
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
                            result[j++][i] = Quotes.Get(quote, row.Level);
                        break;
                }
            }

            return result;
        }

        public async Task<object[]> GetDelegations(
            AnyOfParameter anyof,
            AccountParameter initiator,
            AccountParameter sender,
            AccountParameter prevDelegate,
            AccountParameter newDelegate,
            Int32Parameter level,
            DateTimeParameter timestamp,
            OperationStatusParameter status,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            string field,
            Symbols quote)
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
                case "amount": columns.Add(@"o.""Amount"""); break;
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
                .Filter(anyof, x => x switch
                {
                    "initiator" => "InitiatorId",
                    "sender" => "SenderId",
                    "prevDelegate" => "PrevDelegateId",
                    _ => "DelegateId"
                })
                .Filter("InitiatorId", initiator, x => x == "prevDelegate" ? "PrevDelegateId" : "DelegateId")
                .Filter("SenderId", sender, x => x == "prevDelegate" ? "PrevDelegateId" : "DelegateId")
                .Filter("PrevDelegateId", prevDelegate, x => x == "initiator" ? "InitiatorId" : x == "sender" ? "SenderId" : "DelegateId")
                .Filter("DelegateId", newDelegate, x => x == "initiator" ? "InitiatorId" : x == "sender" ? "SenderId" : "PrevDelegateId")
                .FilterA(@"o.""Level""", level)
                .FilterA(@"o.""Timestamp""", timestamp)
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
                case "amount":
                    foreach (var row in rows)
                        result[j++] = row.Amount;
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
                        result[j++] = Quotes.Get(quote, row.Level);
                    break;
            }

            return result;
        }
        #endregion

        #region originations
        public async Task<int> GetOriginationsCount(
            Int32Parameter level,
            DateTimeParameter timestamp)
        {
            var sql = new SqlBuilder(@"SELECT COUNT(*) FROM ""OriginationOps""")
                .Filter("Level", level)
                .Filter("Timestamp", timestamp);

            using var db = GetConnection();
            return await db.QueryFirstAsync<int>(sql.Query, sql.Params);
        }

        public async Task<IEnumerable<OriginationOperation>> GetOriginations(string hash, MichelineFormat format, Symbols quote)
        {
            var sql = $@"
                SELECT      o.*, b.""Hash"", sc.""ParameterSchema"", sc.""StorageSchema"", sc.""CodeSchema""
                FROM        ""OriginationOps"" as o
                INNER JOIN  ""Blocks"" as b 
                        ON  b.""Level"" = o.""Level""
                LEFT  JOIN  ""Scripts"" as sc
                        ON  sc.""Id"" = o.""ScriptId""
                WHERE       o.""OpHash"" = @hash::character(51)
                ORDER BY    o.""Id""";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { hash });

            #region include storage
            var storages = await AccountRepository.GetStorages(db,
                rows.Where(x => x.StorageId != null)
                    .Select(x => (int)x.StorageId)
                    .Distinct()
                    .ToList(),
                format);
            #endregion

            #region include diffs
            var diffs = await BigMapsRepository.GetOriginationDiffs(db,
                rows.Where(x => x.BigMapUpdates != null)
                    .Select(x => (int)x.Id)
                    .ToList(),
                format);
            #endregion

            return rows.Select(row =>
            {
                var contract = row.ContractId == null ? null
                    : (RawContract)Accounts.Get((int)row.ContractId);

                var code = row.ParameterSchema == null ? null : new MichelineArray
                {
                    Micheline.FromBytes(row.ParameterSchema),
                    Micheline.FromBytes(row.StorageSchema),
                    Micheline.FromBytes(row.CodeSchema)
                };

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
                    Code = (int)format % 2 == 0 ? code : code.ToJson(),
                    Storage = row.StorageId == null ? null : storages?[row.StorageId],
                    Diffs = diffs?.GetValueOrDefault((int)row.Id),
                    Status = StatusToString(row.Status),
                    OriginatedContract = contract == null ? null :
                        new OriginatedContract
                        {
                            Alias = contract.Alias,
                            Address = contract.Address,
                            Kind = contract.KindString,
                            TypeHash = contract.TypeHash,
                            CodeHash = contract.CodeHash
                        },
                    ContractManager = row.ManagerId != null ? Accounts.GetAlias(row.ManagerId) : null,
                    Errors = row.Errors != null ? OperationErrorSerializer.Deserialize(row.Errors) : null,
                    Quote = Quotes.Get(quote, row.Level)
                };
            });
        }

        public async Task<IEnumerable<OriginationOperation>> GetOriginations(string hash, int counter, MichelineFormat format, Symbols quote)
        {
            var sql = $@"
                SELECT      o.*, b.""Hash"", sc.""ParameterSchema"", sc.""StorageSchema"", sc.""CodeSchema""
                FROM        ""OriginationOps"" as o
                INNER JOIN  ""Blocks"" as b 
                        ON  b.""Level"" = o.""Level""
                LEFT  JOIN  ""Scripts"" as sc
                        ON  sc.""Id"" = o.""ScriptId""
                WHERE       o.""OpHash"" = @hash::character(51) AND o.""Counter"" = @counter
                ORDER BY    o.""Id""";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { hash, counter });

            #region include storage
            var storages = await AccountRepository.GetStorages(db,
                rows.Where(x => x.StorageId != null)
                    .Select(x => (int)x.StorageId)
                    .Distinct()
                    .ToList(),
                format);
            #endregion

            #region include diffs
            var diffs = await BigMapsRepository.GetOriginationDiffs(db,
                rows.Where(x => x.BigMapUpdates != null)
                    .Select(x => (int)x.Id)
                    .ToList(),
                format);
            #endregion

            return rows.Select(row =>
            {
                var contract = row.ContractId == null ? null
                    : (RawContract)Accounts.Get((int)row.ContractId);

                var code = row.ParameterSchema == null ? null : new MichelineArray
                {
                    Micheline.FromBytes(row.ParameterSchema),
                    Micheline.FromBytes(row.StorageSchema),
                    Micheline.FromBytes(row.CodeSchema)
                };

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
                    Code = (int)format % 2 == 0 ? code : code.ToJson(),
                    Storage = row.StorageId == null ? null : storages?[row.StorageId],
                    Diffs = diffs?.GetValueOrDefault((int)row.Id),
                    Status = StatusToString(row.Status),
                    OriginatedContract = contract == null ? null :
                        new OriginatedContract
                        {
                            Alias = contract.Alias,
                            Address = contract.Address,
                            Kind = contract.KindString,
                            TypeHash = contract.TypeHash,
                            CodeHash = contract.CodeHash
                        },
                    ContractManager = row.ManagerId != null ? Accounts.GetAlias(row.ManagerId) : null,
                    Errors = row.Errors != null ? OperationErrorSerializer.Deserialize(row.Errors) : null,
                    Quote = Quotes.Get(quote, row.Level)
                };
            });
        }

        public async Task<IEnumerable<OriginationOperation>> GetOriginations(string hash, int counter, int nonce, MichelineFormat format, Symbols quote)
        {
            var sql = $@"
                SELECT      o.*, b.""Hash"", sc.""ParameterSchema"", sc.""StorageSchema"", sc.""CodeSchema""
                FROM        ""OriginationOps"" as o
                INNER JOIN  ""Blocks"" as b 
                        ON  b.""Level"" = o.""Level""
                LEFT  JOIN  ""Scripts"" as sc
                        ON  sc.""Id"" = o.""ScriptId""
                WHERE       o.""OpHash"" = @hash::character(51) AND o.""Counter"" = @counter AND o.""Nonce"" = @nonce
                LIMIT       1";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { hash, counter, nonce });

            #region include storage
            var storages = await AccountRepository.GetStorages(db,
                rows.Where(x => x.StorageId != null)
                    .Select(x => (int)x.StorageId)
                    .Distinct()
                    .ToList(),
                format);
            #endregion

            #region include diffs
            var diffs = await BigMapsRepository.GetOriginationDiffs(db,
                rows.Where(x => x.BigMapUpdates != null)
                    .Select(x => (int)x.Id)
                    .ToList(),
                format);
            #endregion

            return rows.Select(row =>
            {
                var contract = row.ContractId == null ? null
                    : (RawContract)Accounts.Get((int)row.ContractId);

                var code = row.ParameterSchema == null ? null : new MichelineArray
                {
                    Micheline.FromBytes(row.ParameterSchema),
                    Micheline.FromBytes(row.StorageSchema),
                    Micheline.FromBytes(row.CodeSchema)
                };

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
                    Code = (int)format % 2 == 0 ? code : code.ToJson(),
                    Storage = row.StorageId == null ? null : storages?[row.StorageId],
                    Diffs = diffs?.GetValueOrDefault((int)row.Id),
                    Status = StatusToString(row.Status),
                    OriginatedContract = contract == null ? null :
                        new OriginatedContract
                        {
                            Alias = contract.Alias,
                            Address = contract.Address,
                            Kind = contract.KindString,
                            TypeHash = contract.TypeHash,
                            CodeHash = contract.CodeHash
                        },
                    ContractManager = row.ManagerId != null ? Accounts.GetAlias(row.ManagerId) : null,
                    Errors = row.Errors != null ? OperationErrorSerializer.Deserialize(row.Errors) : null,
                    Quote = Quotes.Get(quote, row.Level)
                };
            });
        }

        public async Task<IEnumerable<OriginationOperation>> GetOriginations(Block block, Symbols quote)
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
                            Alias = contract.Alias,
                            Address = contract.Address,
                            Kind = contract.KindString,
                            TypeHash = contract.TypeHash,
                            CodeHash = contract.CodeHash
                        },
                    ContractManager = row.ManagerId != null ? Accounts.GetAlias(row.ManagerId) : null,
                    Errors = row.Errors != null ? OperationErrorSerializer.Deserialize(row.Errors) : null,
                    Quote = Quotes.Get(quote, block.Level)
                };
            });
        }

        public async Task<IEnumerable<OriginationOperation>> GetOriginations(
            AnyOfParameter anyof,
            AccountParameter initiator,
            AccountParameter sender,
            AccountParameter contractManager,
            AccountParameter contractDelegate,
            AccountParameter originatedContract,
            Int32Parameter typeHash,
            Int32Parameter codeHash,
            Int32Parameter level,
            DateTimeParameter timestamp,
            OperationStatusParameter status,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            MichelineFormat format,
            Symbols quote,
            bool includeStorage = false,
            bool includeBigmaps = false)
        {
            var sql = new SqlBuilder($@"
                SELECT      o.*, b.""Hash""
                FROM        ""OriginationOps"" AS o
                INNER JOIN  ""Blocks"" as b
                        ON  b.""Level"" = o.""Level""
                {(typeHash != null || codeHash != null ? @"LEFT JOIN ""Accounts"" as c ON c.""Id"" = o.""ContractId""" : "")}")
                .Filter(anyof, x => x switch
                {
                    "initiator" => "InitiatorId",
                    "sender" => "SenderId",
                    "contractManager" => "ManagerId",
                    "contractDelegate" => "DelegateId",
                    _ => "ContractId"
                })
                .Filter("InitiatorId", initiator, x => x == "contractManager" ? "ManagerId" : "DelegateId")
                .Filter("SenderId", sender, x => x == "contractManager" ? "ManagerId" : "DelegateId")
                .Filter("ManagerId", contractManager, x => x == "initiator" ? "InitiatorId" : x == "sender" ? "SenderId" : "DelegateId")
                .Filter("DelegateId", contractDelegate, x => x == "initiator" ? "InitiatorId" : x == "sender" ? "SenderId" : "ManagerId")
                .Filter("ContractId", originatedContract)
                .FilterA(@"c.""TypeHash""", typeHash)
                .FilterA(@"c.""CodeHash""", codeHash)
                .FilterA(@"o.""Level""", level)
                .FilterA(@"o.""Timestamp""", timestamp)
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

            #region include storage
            var storages = includeStorage
                ? await AccountRepository.GetStorages(db,
                    rows.Where(x => x.StorageId != null)
                        .Select(x => (int)x.StorageId)
                        .Distinct()
                        .ToList(),
                    format)
                : null;
            #endregion

            #region include diffs
            var diffs = includeBigmaps
                ? await BigMapsRepository.GetOriginationDiffs(db,
                    rows.Where(x => x.BigMapUpdates != null)
                        .Select(x => (int)x.Id)
                        .ToList(),
                    format)
                : null;
            #endregion

            return rows.Select(row =>
            {
                var contract = row.ContractId == null ? null
                    : (RawContract)Accounts.Get((int)row.ContractId);

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
                        Alias = contract.Alias,
                        Address = contract.Address,
                        Kind = contract.KindString,
                        TypeHash = contract.TypeHash,
                        CodeHash = contract.CodeHash
                    },
                    Storage = row.StorageId == null ? null : storages?[row.StorageId],
                    Diffs = diffs?.GetValueOrDefault((int)row.Id),
                    ContractManager = row.ManagerId != null ? Accounts.GetAlias(row.ManagerId) : null,
                    Errors = row.Errors != null ? OperationErrorSerializer.Deserialize(row.Errors) : null,
                    Quote = Quotes.Get(quote, row.Level)
                };
            });
        }

        public async Task<object[][]> GetOriginations(
            AnyOfParameter anyof,
            AccountParameter initiator,
            AccountParameter sender,
            AccountParameter contractManager,
            AccountParameter contractDelegate,
            AccountParameter originatedContract,
            Int32Parameter typeHash,
            Int32Parameter codeHash,
            Int32Parameter level,
            DateTimeParameter timestamp,
            OperationStatusParameter status,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            string[] fields,
            MichelineFormat format,
            Symbols quote)
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
                    case "code":
                        columns.Add(@"sc.""ParameterSchema""");
                        columns.Add(@"sc.""StorageSchema""");
                        columns.Add(@"sc.""CodeSchema""");
                        joins.Add(@"LEFT JOIN ""Scripts"" as sc ON sc.""Id"" = o.""ScriptId""");
                        break;
                    case "storage": columns.Add(@"o.""StorageId"""); break;
                    case "diffs":
                        columns.Add(@"o.""Id""");
                        columns.Add(@"o.""BigMapUpdates""");
                        break;
                    case "quote": columns.Add(@"o.""Level"""); break;
                }
            }

            if (columns.Count == 0)
                return Array.Empty<object[]>();

            if (typeHash != null || codeHash != null)
                joins.Add(@"LEFT JOIN ""Accounts"" as c ON c.""Id"" = o.""ContractId""");

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""OriginationOps"" as o {string.Join(' ', joins)}")
                .Filter(anyof, x => x switch
                {
                    "initiator" => "InitiatorId",
                    "sender" => "SenderId",
                    "contractManager" => "ManagerId",
                    "contractDelegate" => "DelegateId",
                    _ => "ContractId"
                })
                .Filter("InitiatorId", initiator, x => x == "contractManager" ? "ManagerId" : "DelegateId")
                .Filter("SenderId", sender, x => x == "contractManager" ? "ManagerId" : "DelegateId")
                .Filter("ManagerId", contractManager, x => x == "initiator" ? "InitiatorId" : x == "sender" ? "SenderId" : "DelegateId")
                .Filter("DelegateId", contractDelegate, x => x == "initiator" ? "InitiatorId" : x == "sender" ? "SenderId" : "ManagerId")
                .Filter("ContractId", originatedContract)
                .FilterA(@"c.""TypeHash""", typeHash)
                .FilterA(@"c.""CodeHash""", codeHash)
                .FilterA(@"o.""Level""", level)
                .FilterA(@"o.""Timestamp""", timestamp)
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
                    case "code":
                        foreach (var row in rows)
                        {
                            var code = row.ParameterSchema == null ? null : new MichelineArray
                            {
                                Micheline.FromBytes(row.ParameterSchema),
                                Micheline.FromBytes(row.StorageSchema),
                                Micheline.FromBytes(row.CodeSchema)
                            };
                            result[j++][i] = (int)format % 2 == 0 ? code : code.ToJson();
                        }
                        break;
                    case "storage":
                        var storages = await AccountRepository.GetStorages(db,
                            rows.Where(x => x.StorageId != null)
                                .Select(x => (int)x.StorageId)
                                .Distinct()
                                .ToList(),
                            format);
                        if (storages != null)
                            foreach (var row in rows)
                                result[j++][i] = row.StorageId == null ? null : storages[row.StorageId];
                        break;
                    case "diffs":
                        var diffs = await BigMapsRepository.GetOriginationDiffs(db,
                            rows.Where(x => x.BigMapUpdates != null)
                                .Select(x => (int)x.Id)
                                .ToList(),
                            format);
                        if (diffs != null)
                            foreach (var row in rows)
                                result[j++][i] = diffs.GetValueOrDefault((int)row.Id);
                        break;
                    case "status":
                        foreach (var row in rows)
                            result[j++][i] = StatusToString(row.Status);
                        break;
                    case "originatedContract":
                        foreach (var row in rows)
                        {
                            var contract = row.ContractId == null ? null : (RawContract)Accounts.Get((int)row.ContractId);
                            result[j++][i] = contract == null ? null : new OriginatedContract
                            {
                                Alias = contract.Alias,
                                Address = contract.Address,
                                Kind = contract.KindString,
                                TypeHash = contract.TypeHash,
                                CodeHash = contract.CodeHash
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
                            result[j++][i] = Quotes.Get(quote, row.Level);
                        break;
                }
            }

            return result;
        }

        public async Task<object[]> GetOriginations(
            AnyOfParameter anyof,
            AccountParameter initiator,
            AccountParameter sender,
            AccountParameter contractManager,
            AccountParameter contractDelegate,
            AccountParameter originatedContract,
            Int32Parameter typeHash,
            Int32Parameter codeHash,
            Int32Parameter level,
            DateTimeParameter timestamp,
            OperationStatusParameter status,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            string field,
            MichelineFormat format,
            Symbols quote)
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
                case "code":
                    columns.Add(@"sc.""ParameterSchema""");
                    columns.Add(@"sc.""StorageSchema""");
                    columns.Add(@"sc.""CodeSchema""");
                    joins.Add(@"LEFT JOIN ""Scripts"" as sc ON sc.""Id"" = o.""ScriptId""");
                    break;
                case "storage": columns.Add(@"o.""StorageId"""); break;
                case "diffs":
                    columns.Add(@"o.""Id""");
                    columns.Add(@"o.""BigMapUpdates""");
                    break;
                case "quote": columns.Add(@"o.""Level"""); break;
            }

            if (columns.Count == 0)
                return Array.Empty<object>();

            if (typeHash != null || codeHash != null)
                joins.Add(@"LEFT JOIN ""Accounts"" as c ON c.""Id"" = o.""ContractId""");

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""OriginationOps"" as o {string.Join(' ', joins)}")
                .Filter(anyof, x => x switch
                {
                    "initiator" => "InitiatorId",
                    "sender" => "SenderId",
                    "contractManager" => "ManagerId",
                    "contractDelegate" => "DelegateId",
                    _ => "ContractId"
                })
                .Filter("InitiatorId", initiator, x => x == "contractManager" ? "ManagerId" : "DelegateId")
                .Filter("SenderId", sender, x => x == "contractManager" ? "ManagerId" : "DelegateId")
                .Filter("ManagerId", contractManager, x => x == "initiator" ? "InitiatorId" : x == "sender" ? "SenderId" : "DelegateId")
                .Filter("DelegateId", contractDelegate, x => x == "initiator" ? "InitiatorId" : x == "sender" ? "SenderId" : "ManagerId")
                .Filter("ContractId", originatedContract)
                .FilterA(@"c.""TypeHash""", typeHash)
                .FilterA(@"c.""CodeHash""", codeHash)
                .FilterA(@"o.""Level""", level)
                .FilterA(@"o.""Timestamp""", timestamp)
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
                case "code":
                    foreach (var row in rows)
                    {
                        var code = row.ParameterSchema == null ? null : new MichelineArray
                        {
                            Micheline.FromBytes(row.ParameterSchema),
                            Micheline.FromBytes(row.StorageSchema),
                            Micheline.FromBytes(row.CodeSchema)
                        };
                        result[j++] = (int)format % 2 == 0 ? code : code.ToJson();
                    }
                    break;
                case "storage":
                    var storages = await AccountRepository.GetStorages(db,
                        rows.Where(x => x.StorageId != null)
                            .Select(x => (int)x.StorageId)
                            .Distinct()
                            .ToList(),
                        format);
                    if (storages != null)
                        foreach (var row in rows)
                            result[j++] = row.StorageId == null ? null : storages[row.StorageId];
                    break;
                case "diffs":
                    var diffs = await BigMapsRepository.GetOriginationDiffs(db,
                        rows.Where(x => x.BigMapUpdates != null)
                            .Select(x => (int)x.Id)
                            .ToList(),
                        format);
                    if (diffs != null)
                        foreach (var row in rows)
                            result[j++] = diffs.GetValueOrDefault((int)row.Id);
                    break;
                case "status":
                    foreach (var row in rows)
                        result[j++] = StatusToString(row.Status);
                    break;
                case "originatedContract":
                    foreach (var row in rows)
                    {
                        var contract = row.ContractId == null ? null : (RawContract)Accounts.Get((int)row.ContractId);
                        result[j++] = contract == null ? null : new OriginatedContract
                        {
                            Alias = contract.Alias,
                            Address = contract.Address,
                            Kind = contract.KindString,
                            TypeHash = contract.TypeHash,
                            CodeHash = contract.CodeHash
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
                        result[j++] = Quotes.Get(quote, row.Level);
                    break;
            }

            return result;
        }
        #endregion

        #region transactions
        public async Task<int> GetTransactionsCount(
            Int32Parameter level,
            DateTimeParameter timestamp,
            OperationStatusParameter status)
        {
            var sql = new SqlBuilder(@"SELECT COUNT(*) FROM ""TransactionOps""")
                .Filter("Level", level)
                .Filter("Timestamp", timestamp)
                .Filter("Status", status);

            using var db = GetConnection();
            return await db.QueryFirstAsync<int>(sql.Query, sql.Params);
        }

        public async Task<IEnumerable<TransactionOperation>> GetTransactions(string hash, MichelineFormat format, Symbols quote)
        {
            var sql = $@"
                SELECT      o.*, b.""Hash""
                FROM        ""TransactionOps"" as o
                INNER JOIN  ""Blocks"" as b 
                        ON  b.""Level"" = o.""Level""
                WHERE       o.""OpHash"" = @hash::character(51)
                ORDER BY    o.""Id""";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { hash });

            #region include storage
            var storages = await AccountRepository.GetStorages(db,
                rows.Where(x => x.StorageId != null)
                    .Select(x => (int)x.StorageId)
                    .Distinct()
                    .ToList(),
                format);
            #endregion

            #region include diffs
            var diffs = await BigMapsRepository.GetTransactionDiffs(db,
                rows.Where(x => x.BigMapUpdates != null)
                    .Select(x => (int)x.Id)
                    .ToList(),
                format);
            #endregion

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
                Parameter = row.Entrypoint == null ? null : new TxParameter
                {
                    Entrypoint = row.Entrypoint,
                    Value = format switch
                    {
                        MichelineFormat.Json => (RawJson)row.JsonParameters,
                        MichelineFormat.JsonString => row.JsonParameters,
                        MichelineFormat.Raw => row.RawParameters == null ? null : (RawJson)Micheline.ToJson(row.RawParameters),
                        MichelineFormat.RawString => row.RawParameters == null ? null : Micheline.ToJson(row.RawParameters),
                        _ => throw new Exception("Invalid MichelineFormat value")
                    }
                },
                Storage = row.StorageId == null ? null : storages?[row.StorageId],
                Diffs = diffs?.GetValueOrDefault((int)row.Id),
                Status = StatusToString(row.Status),
                Errors = row.Errors != null ? OperationErrorSerializer.Deserialize(row.Errors) : null,
                HasInternals = row.InternalOperations > 0,
                Quote = Quotes.Get(quote, row.Level),
                Parameters = row.RawParameters == null ? null : $"{{\"entrypoint\":\"{row.Entrypoint}\",\"value\":{Micheline.ToJson(row.RawParameters)}}}"
            });
        }

        public async Task<IEnumerable<TransactionOperation>> GetTransactions(string hash, int counter, MichelineFormat format, Symbols quote)
        {
            var sql = $@"
                SELECT      o.*, b.""Hash""
                FROM        ""TransactionOps"" as o
                INNER JOIN  ""Blocks"" as b 
                        ON  b.""Level"" = o.""Level""
                WHERE       o.""OpHash"" = @hash::character(51) AND o.""Counter"" = @counter
                ORDER BY    o.""Id""";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { hash, counter });

            #region include storage
            var storages = await AccountRepository.GetStorages(db,
                rows.Where(x => x.StorageId != null)
                    .Select(x => (int)x.StorageId)
                    .Distinct()
                    .ToList(),
                format);
            #endregion

            #region include diffs
            var diffs = await BigMapsRepository.GetTransactionDiffs(db,
                rows.Where(x => x.BigMapUpdates != null)
                    .Select(x => (int)x.Id)
                    .ToList(),
                format);
            #endregion

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
                Parameter = row.Entrypoint == null ? null : new TxParameter
                {
                    Entrypoint = row.Entrypoint,
                    Value = format switch
                    {
                        MichelineFormat.Json => (RawJson)row.JsonParameters,
                        MichelineFormat.JsonString => row.JsonParameters,
                        MichelineFormat.Raw => row.RawParameters == null ? null : (RawJson)Micheline.ToJson(row.RawParameters),
                        MichelineFormat.RawString => row.RawParameters == null ? null : Micheline.ToJson(row.RawParameters),
                        _ => throw new Exception("Invalid MichelineFormat value")
                    }
                },
                Storage = row.StorageId == null ? null : storages?[row.StorageId],
                Diffs = diffs?.GetValueOrDefault((int)row.Id),
                Status = StatusToString(row.Status),
                Errors = row.Errors != null ? OperationErrorSerializer.Deserialize(row.Errors) : null,
                HasInternals = row.InternalOperations > 0,
                Quote = Quotes.Get(quote, row.Level),
                Parameters = row.RawParameters == null ? null : $"{{\"entrypoint\":\"{row.Entrypoint}\",\"value\":{Micheline.ToJson(row.RawParameters)}}}"
            });
        }

        public async Task<IEnumerable<TransactionOperation>> GetTransactions(string hash, int counter, int nonce, MichelineFormat format, Symbols quote)
        {
            var sql = $@"
                SELECT      o.*, b.""Hash""
                FROM        ""TransactionOps"" as o
                INNER JOIN  ""Blocks"" as b 
                        ON  b.""Level"" = o.""Level""
                WHERE       o.""OpHash"" = @hash::character(51) AND o.""Counter"" = @counter AND o.""Nonce"" = @nonce
                LIMIT       1";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { hash, counter, nonce });

            #region include storage
            var storages = await AccountRepository.GetStorages(db,
                rows.Where(x => x.StorageId != null)
                    .Select(x => (int)x.StorageId)
                    .Distinct()
                    .ToList(),
                format);
            #endregion

            #region include diffs
            var diffs = await BigMapsRepository.GetTransactionDiffs(db,
                rows.Where(x => x.BigMapUpdates != null)
                    .Select(x => (int)x.Id)
                    .ToList(),
                format);
            #endregion

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
                Parameter = row.Entrypoint == null ? null : new TxParameter
                {
                    Entrypoint = row.Entrypoint,
                    Value = format switch
                    {
                        MichelineFormat.Json => (RawJson)row.JsonParameters,
                        MichelineFormat.JsonString => row.JsonParameters,
                        MichelineFormat.Raw => row.RawParameters == null ? null : (RawJson)Micheline.ToJson(row.RawParameters),
                        MichelineFormat.RawString => row.RawParameters == null ? null : Micheline.ToJson(row.RawParameters),
                        _ => throw new Exception("Invalid MichelineFormat value")
                    }
                },
                Storage = row.StorageId == null ? null : storages?[row.StorageId],
                Diffs = diffs?.GetValueOrDefault((int)row.Id),
                Status = StatusToString(row.Status),
                Errors = row.Errors != null ? OperationErrorSerializer.Deserialize(row.Errors) : null,
                HasInternals = row.InternalOperations > 0,
                Quote = Quotes.Get(quote, row.Level),
                Parameters = row.RawParameters == null ? null : $"{{\"entrypoint\":\"{row.Entrypoint}\",\"value\":{Micheline.ToJson(row.RawParameters)}}}"
            });
        }

        public async Task<IEnumerable<TransactionOperation>> GetTransactions(Block block, MichelineFormat format, Symbols quote)
        {
            var sql = @"
                SELECT    *
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
                Parameter = row.Entrypoint == null ? null : new TxParameter
                {
                    Entrypoint = row.Entrypoint,
                    Value = format switch
                    {
                        MichelineFormat.Json => (RawJson)row.JsonParameters,
                        MichelineFormat.JsonString => row.JsonParameters,
                        MichelineFormat.Raw => row.RawParameters == null ? null : (RawJson)Micheline.ToJson(row.RawParameters),
                        MichelineFormat.RawString => row.RawParameters == null ? null : Micheline.ToJson(row.RawParameters),
                        _ => throw new Exception("Invalid MichelineFormat value")
                    }
                },
                Status = StatusToString(row.Status),
                Errors = row.Errors != null ? OperationErrorSerializer.Deserialize(row.Errors) : null,
                HasInternals = row.InternalOperations > 0,
                Quote = Quotes.Get(quote, block.Level),
                Parameters = row.RawParameters == null ? null : $"{{\"entrypoint\":\"{row.Entrypoint}\",\"value\":{Micheline.ToJson(row.RawParameters)}}}"
            });
        }

        public async Task<IEnumerable<TransactionOperation>> GetTransactions(
            AnyOfParameter anyof,
            AccountParameter initiator,
            AccountParameter sender,
            AccountParameter target,
            Int64Parameter amount,
            Int32Parameter level,
            DateTimeParameter timestamp,
            StringParameter entrypoint,
            JsonParameter parameter,
            StringParameter parameters,
            BoolParameter hasInternals,
            OperationStatusParameter status,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            MichelineFormat format,
            Symbols quote,
            bool includeStorage = false,
            bool includeBigmaps = false)
        {
            #region backward compatibility
            // TODO: remove it asap
            var realSort = sort;
            var realOffset = offset;
            var realLimit = limit;
            if (parameters != null)
            {
                realSort = null;
                realOffset = null;
                realLimit = 1_000_000;
            }
            #endregion

            var sql = new SqlBuilder(@"
                SELECT      o.*, b.""Hash""
                FROM        ""TransactionOps"" AS o
                INNER JOIN  ""Blocks"" as b
                        ON  b.""Level"" = o.""Level""")
                .Filter(anyof, x => x == "sender" ? "SenderId" : x == "target" ? "TargetId" : "InitiatorId")
                .Filter("InitiatorId", initiator, x => "TargetId")
                .Filter("SenderId", sender, x => "TargetId")
                .Filter("TargetId", target, x => x == "sender" ? "SenderId" : "InitiatorId")
                .Filter("Amount", amount)
                .Filter("Entrypoint", entrypoint)
                .Filter("JsonParameters", parameter)
                .Filter("InternalOperations", hasInternals?.Eq == true
                    ? new Int32NullParameter { Gt = 0 }
                    : hasInternals?.Eq == false
                        ? new Int32NullParameter { Null = true }
                        : null)
                .Filter("Status", status)
                .FilterA(@"o.""Level""", level)
                .FilterA(@"o.""Timestamp""", timestamp)
                .Take(realSort, realOffset, realLimit, x => x switch
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

            #region include storage
            var storages = includeStorage
                ? await AccountRepository.GetStorages(db,
                    rows.Where(x => x.StorageId != null)
                        .Select(x => (int)x.StorageId)
                        .Distinct()
                        .ToList(),
                    format)
                : null;
            #endregion

            #region include diffs
            var diffs = includeBigmaps
                ? await BigMapsRepository.GetTransactionDiffs(db,
                    rows.Where(x => x.BigMapUpdates != null)
                        .Select(x => (int)x.Id)
                        .ToList(),
                    format)
                : null;
            #endregion

            var res = rows.Select(row => new TransactionOperation
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
                Parameter = row.Entrypoint == null ? null : new TxParameter
                {
                    Entrypoint = row.Entrypoint,
                    Value = format switch
                    {
                        MichelineFormat.Json => (RawJson)row.JsonParameters,
                        MichelineFormat.JsonString => row.JsonParameters,
                        MichelineFormat.Raw => row.RawParameters == null ? null : (RawJson)Micheline.ToJson(row.RawParameters),
                        MichelineFormat.RawString => row.RawParameters == null ? null : Micheline.ToJson(row.RawParameters),
                        _ => throw new Exception("Invalid MichelineFormat value")
                    }
                },
                Storage = row.StorageId == null ? null : storages?[row.StorageId],
                Diffs = diffs?.GetValueOrDefault((int)row.Id),
                Status = StatusToString(row.Status),
                Errors = row.Errors != null ? OperationErrorSerializer.Deserialize(row.Errors) : null,
                HasInternals = row.InternalOperations > 0,
                Quote = Quotes.Get(quote, row.Level),
                Parameters = row.RawParameters == null ? null : $"{{\"entrypoint\":\"{row.Entrypoint}\",\"value\":{Micheline.ToJson(row.RawParameters)}}}"
            });

            #region backward compatibility
            // TODO: remove it asap
            if (parameters != null)
            {
                if (parameters.Eq != null)
                    res = res.Where(x => x.Parameters == parameters.Eq);
                if (parameters.Ne != null)
                    res = res.Where(x => x.Parameters != parameters.Ne);
                if (parameters.In != null)
                    res = res.Where(x => parameters.In.Contains(x.Parameters));
                if (parameters.Ni != null)
                    res = res.Where(x => !parameters.Ni.Contains(x.Parameters));
                if (parameters.Null == true)
                    res = res.Where(x => x.Parameters == null);
                if (parameters.Null == false)
                    res = res.Where(x => x.Parameters != null);
                if (parameters.As != null)
                {
                    var pattern = $"^{parameters.As.Replace("%", ".*").Replace("[", "\\[").Replace("]", "\\]").Replace("{", "\\{").Replace("}", "\\}")}$";
                    res = res.Where(x => x.Parameters != null && System.Text.RegularExpressions.Regex.IsMatch(x.Parameters, pattern));
                }
                if (parameters.Un != null)
                {
                    var pattern = $"^{parameters.Un.Replace("%", ".*").Replace("[", "\\[").Replace("]", "\\]").Replace("{", "\\{").Replace("}", "\\}")}$";
                    res = res.Where(x => x.Parameters != null && !System.Text.RegularExpressions.Regex.IsMatch(x.Parameters, pattern));
                }

                if (sort?.Asc != null)
                {
                    res = sort.Asc switch
                    {
                        "level" => res.OrderBy(x => x.Level),
                        "gasUsed" => res.OrderBy(x => x.GasUsed),
                        "storageUsed" => res.OrderBy(x => x.StorageUsed),
                        "bakerFee" => res.OrderBy(x => x.BakerFee),
                        "storageFee" => res.OrderBy(x => x.StorageFee),
                        "allocationFee" => res.OrderBy(x => x.AllocationFee),
                        "amount" => res.OrderBy(x => x.Amount),
                        _ => res.OrderBy(x => x.Id)
                    };
                }
                else if (sort?.Desc != null)
                {
                    res = sort.Desc switch
                    {
                        "level" => res.OrderByDescending(x => x.Level),
                        "gasUsed" => res.OrderByDescending(x => x.GasUsed),
                        "storageUsed" => res.OrderByDescending(x => x.StorageUsed),
                        "bakerFee" => res.OrderByDescending(x => x.BakerFee),
                        "storageFee" => res.OrderByDescending(x => x.StorageFee),
                        "allocationFee" => res.OrderByDescending(x => x.AllocationFee),
                        "amount" => res.OrderByDescending(x => x.Amount),
                        _ => res.OrderByDescending(x => x.Id)
                    };
                }

                if (offset?.El != null)
                    res = res.Skip((int)offset.El);
                else if (offset?.Pg != null)
                    res = res.Skip((int)offset.Pg * limit);

                return res.Take(limit);
            }
            #endregion

            return res;
        }

        public async Task<object[][]> GetTransactions(
            AnyOfParameter anyof,
            AccountParameter initiator,
            AccountParameter sender,
            AccountParameter target,
            Int64Parameter amount,
            Int32Parameter level,
            DateTimeParameter timestamp,
            StringParameter entrypoint,
            JsonParameter parameter,
            BoolParameter hasInternals,
            OperationStatusParameter status,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            string[] fields,
            MichelineFormat format,
            Symbols quote)
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
                    case "parameter":
                        columns.Add(@"o.""Entrypoint""");
                        columns.Add(format switch
                        {
                            MichelineFormat.Json => $@"o.""JsonParameters""",
                            MichelineFormat.JsonString => $@"o.""JsonParameters""",
                            MichelineFormat.Raw => $@"o.""RawParameters""",
                            MichelineFormat.RawString => $@"o.""RawParameters""",
                            _ => throw new Exception("Invalid MichelineFormat value")
                        });
                        break;
                    case "storage": columns.Add(@"o.""StorageId"""); break;
                    case "diffs":
                        columns.Add(@"o.""Id""");
                        columns.Add(@"o.""BigMapUpdates""");
                        break;
                    case "status": columns.Add(@"o.""Status"""); break;
                    case "errors": columns.Add(@"o.""Errors"""); break;
                    case "hasInternals": columns.Add(@"o.""InternalOperations"""); break;
                    case "block":
                        columns.Add(@"b.""Hash""");
                        joins.Add(@"INNER JOIN ""Blocks"" as b ON b.""Level"" = o.""Level""");
                        break;
                    case "quote": columns.Add(@"o.""Level"""); break;
                    case "parameters": // backward compatibility
                        columns.Add($@"o.""Entrypoint""");
                        columns.Add($@"o.""RawParameters""");
                        break;
                }
            }

            if (columns.Count == 0)
                return Array.Empty<object[]>();

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""TransactionOps"" as o {string.Join(' ', joins)}")
                .Filter(anyof, x => x == "sender" ? "SenderId" : x == "target" ? "TargetId" : "InitiatorId")
                .Filter("InitiatorId", initiator, x => "TargetId")
                .Filter("SenderId", sender, x => "TargetId")
                .Filter("TargetId", target, x => x == "sender" ? "SenderId" : "InitiatorId")
                .Filter("Amount", amount)
                .Filter("Entrypoint", entrypoint)
                .Filter("JsonParameters", parameter)
                .Filter("InternalOperations", hasInternals?.Eq == true
                    ? new Int32NullParameter { Gt = 0 }
                    : hasInternals?.Eq == false
                        ? new Int32NullParameter { Null = true }
                        : null)
                .Filter("Status", status)
                .FilterA(@"o.""Level""", level)
                .FilterA(@"o.""Timestamp""", timestamp)
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
                    case "parameter":
                        foreach (var row in rows)
                            result[j++][i] = row.Entrypoint == null ? null : new TxParameter
                            {
                                Entrypoint = row.Entrypoint,
                                Value = format switch
                                {
                                    MichelineFormat.Json => (RawJson)row.JsonParameters,
                                    MichelineFormat.JsonString => row.JsonParameters,
                                    MichelineFormat.Raw => row.RawParameters == null ? null : (RawJson)Micheline.ToJson(row.RawParameters),
                                    MichelineFormat.RawString => row.RawParameters == null ? null : Micheline.ToJson(row.RawParameters),
                                    _ => throw new Exception("Invalid MichelineFormat value")
                                }
                            };
                        break;
                    case "storage":
                        var storages = await AccountRepository.GetStorages(db,
                            rows.Where(x => x.StorageId != null)
                                .Select(x => (int)x.StorageId)
                                .Distinct()
                                .ToList(),
                            format);
                        if (storages != null)
                            foreach (var row in rows)
                                result[j++][i] = row.StorageId == null ? null : storages[row.StorageId];
                        break;
                    case "diffs":
                        var diffs = await BigMapsRepository.GetTransactionDiffs(db,
                            rows.Where(x => x.BigMapUpdates != null)
                                .Select(x => (int)x.Id)
                                .ToList(),
                            format);
                        if (diffs != null)
                            foreach (var row in rows)
                                result[j++][i] = diffs.GetValueOrDefault((int)row.Id);
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
                            result[j++][i] = Quotes.Get(quote, row.Level);
                        break;
                    case "parameters":
                        foreach (var row in rows)
                            result[j++][i] = row.RawParameters == null ? null
                                : $"{{\"entrypoint\":\"{row.Entrypoint}\",\"value\":{Micheline.ToJson(row.RawParameters)}}}";
                        break;
                }
            }

            return result;
        }

        public async Task<object[]> GetTransactions(
            AnyOfParameter anyof,
            AccountParameter initiator,
            AccountParameter sender,
            AccountParameter target,
            Int64Parameter amount,
            Int32Parameter level,
            DateTimeParameter timestamp,
            StringParameter entrypoint,
            JsonParameter parameter,
            BoolParameter hasInternals,
            OperationStatusParameter status,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            string field,
            MichelineFormat format,
            Symbols quote)
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
                case "parameter":
                    columns.Add(@"o.""Entrypoint""");
                    columns.Add(format switch
                    {
                        MichelineFormat.Json => $@"o.""JsonParameters""",
                        MichelineFormat.JsonString => $@"o.""JsonParameters""",
                        MichelineFormat.Raw => $@"o.""RawParameters""",
                        MichelineFormat.RawString => $@"o.""RawParameters""",
                        _ => throw new Exception("Invalid MichelineFormat value")
                    });
                    break;
                case "storage": columns.Add(@"o.""StorageId"""); break;
                case "diffs":
                    columns.Add(@"o.""Id""");
                    columns.Add(@"o.""BigMapUpdates""");
                    break;
                case "status": columns.Add(@"o.""Status"""); break;
                case "errors": columns.Add(@"o.""Errors"""); break;
                case "hasInternals": columns.Add(@"o.""InternalOperations"""); break;
                case "block":
                    columns.Add(@"b.""Hash""");
                    joins.Add(@"INNER JOIN ""Blocks"" as b ON b.""Level"" = o.""Level""");
                    break;
                case "quote": columns.Add(@"o.""Level"""); break;
                case "parameters": // backward compatibility
                    columns.Add($@"o.""Entrypoint""");
                    columns.Add($@"o.""RawParameters""");
                    break;
            }

            if (columns.Count == 0)
                return Array.Empty<object>();

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""TransactionOps"" as o {string.Join(' ', joins)}")
                .Filter(anyof, x => x == "sender" ? "SenderId" : x == "target" ? "TargetId" : "InitiatorId")
                .Filter("InitiatorId", initiator, x => "TargetId")
                .Filter("SenderId", sender, x => "TargetId")
                .Filter("TargetId", target, x => x == "sender" ? "SenderId" : "InitiatorId")
                .Filter("Amount", amount)
                .Filter("Entrypoint", entrypoint)
                .Filter("JsonParameters", parameter)
                .Filter("InternalOperations", hasInternals?.Eq == true
                    ? new Int32NullParameter { Gt = 0 }
                    : hasInternals?.Eq == false
                        ? new Int32NullParameter { Null = true }
                        : null)
                .Filter("Status", status)
                .FilterA(@"o.""Level""", level)
                .FilterA(@"o.""Timestamp""", timestamp)
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
                case "parameter":
                    foreach (var row in rows)
                        result[j++] = row.Entrypoint == null ? null : new TxParameter
                        {
                            Entrypoint = row.Entrypoint,
                            Value = format switch
                            {
                                MichelineFormat.Json => (RawJson)row.JsonParameters,
                                MichelineFormat.JsonString => row.JsonParameters,
                                MichelineFormat.Raw => row.RawParameters == null ? null : (RawJson)Micheline.ToJson(row.RawParameters),
                                MichelineFormat.RawString => row.RawParameters == null ? null : Micheline.ToJson(row.RawParameters),
                                _ => throw new Exception("Invalid MichelineFormat value")
                            }
                        };
                    break;
                case "storage":
                    var storages = await AccountRepository.GetStorages(db,
                        rows.Where(x => x.StorageId != null)
                            .Select(x => (int)x.StorageId)
                            .Distinct()
                            .ToList(),
                        format);
                    if (storages != null)
                        foreach (var row in rows)
                            result[j++] = row.StorageId == null ? null : storages[row.StorageId];
                    break;
                case "diffs":
                    var diffs = await BigMapsRepository.GetTransactionDiffs(db,
                        rows.Where(x => x.BigMapUpdates != null)
                            .Select(x => (int)x.Id)
                            .ToList(),
                        format);
                    if (diffs != null)
                        foreach (var row in rows)
                            result[j++] = diffs.GetValueOrDefault((int)row.Id);
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
                        result[j++] = Quotes.Get(quote, row.Level);
                    break;
                case "parameters":
                    foreach (var row in rows)
                        result[j++] = row.RawParameters == null ? null
                            : $"{{\"entrypoint\":\"{row.Entrypoint}\",\"value\":{Micheline.ToJson(row.RawParameters)}}}";
                    break;
            }

            return result;
        }
        #endregion

        #region reveals
        public async Task<int> GetRevealsCount(
            Int32Parameter level,
            DateTimeParameter timestamp)
        {
            var sql = new SqlBuilder(@"SELECT COUNT(*) FROM ""RevealOps""")
                .Filter("Level", level)
                .Filter("Timestamp", timestamp);

            using var db = GetConnection();
            return await db.QueryFirstAsync<int>(sql.Query, sql.Params);
        }

        public async Task<IEnumerable<RevealOperation>> GetReveals(string hash, Symbols quote)
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
                Quote = Quotes.Get(quote, row.Level)
            });
        }

        public async Task<IEnumerable<RevealOperation>> GetReveals(string hash, int counter, Symbols quote)
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
                Quote = Quotes.Get(quote, row.Level)
            });
        }

        public async Task<IEnumerable<RevealOperation>> GetReveals(Block block, Symbols quote)
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
                Quote = Quotes.Get(quote, block.Level)
            });
        }

        public async Task<IEnumerable<RevealOperation>> GetReveals(
            AccountParameter sender,
            Int32Parameter level,
            DateTimeParameter timestamp,
            OperationStatusParameter status,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            Symbols quote)
        {
            var sql = new SqlBuilder(@"SELECT o.*, b.""Hash"" FROM ""RevealOps"" AS o INNER JOIN ""Blocks"" as b ON b.""Level"" = o.""Level""")
                .Filter("SenderId", sender)
                .FilterA(@"o.""Level""", level)
                .FilterA(@"o.""Timestamp""", timestamp)
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
                Quote = Quotes.Get(quote, row.Level)
            });
        }

        public async Task<object[][]> GetReveals(
            AccountParameter sender,
            Int32Parameter level,
            DateTimeParameter timestamp,
            OperationStatusParameter status,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            string[] fields,
            Symbols quote)
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
                .FilterA(@"o.""Timestamp""", timestamp)
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
                            result[j++][i] = Quotes.Get(quote, row.Level);
                        break;
                }
            }

            return result;
        }

        public async Task<object[]> GetReveals(
            AccountParameter sender,
            Int32Parameter level,
            DateTimeParameter timestamp,
            OperationStatusParameter status,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            string field,
            Symbols quote)
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
                .FilterA(@"o.""Timestamp""", timestamp)
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
                        result[j++] = Quotes.Get(quote, row.Level);
                    break;
            }

            return result;
        }
        #endregion

        #region migrations
        public async Task<int> GetMigrationsCount(
            Int32Parameter level,
            DateTimeParameter timestamp)
        {
            var sql = new SqlBuilder(@"SELECT COUNT(*) FROM ""MigrationOps""")
                .Filter("Level", level)
                .Filter("Timestamp", timestamp);

            using var db = GetConnection();
            return await db.QueryFirstAsync<int>(sql.Query, sql.Params);
        }

        public async Task<IEnumerable<MigrationOperation>> GetMigrations(
            AccountParameter account,
            MigrationKindParameter kind,
            Int64Parameter balanceChange,
            Int32Parameter level,
            DateTimeParameter timestamp,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            MichelineFormat format,
            Symbols quote,
            bool includeStorage = false,
            bool includeDiffs = false)
        {
            var sql = new SqlBuilder(@"SELECT o.*, b.""Hash"" FROM ""MigrationOps"" AS o INNER JOIN ""Blocks"" as b ON b.""Level"" = o.""Level""")
                .Filter("AccountId", account)
                .Filter("Kind", kind)
                .Filter("BalanceChange", balanceChange)
                .FilterA(@"o.""Level""", level)
                .FilterA(@"o.""Timestamp""", timestamp)
                .Take(sort, offset, limit, x => x == "level" ? ("Id", "Level") : ("Id", "Id"), "o");

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            #region include storage
            var storages = includeStorage
                ? await AccountRepository.GetStorages(db,
                    rows.Where(x => x.StorageId != null)
                        .Select(x => (int)x.StorageId)
                        .Distinct()
                        .ToList(),
                    format)
                : null;
            #endregion

            #region include diffs
            var diffs = includeDiffs
                ? await BigMapsRepository.GetMigrationDiffs(db,
                    rows.Where(x => x.BigMapUpdates != null)
                        .Select(x => (int)x.Id)
                        .ToList(),
                    format)
                : null;
            #endregion

            return rows.Select(row => new MigrationOperation
            {
                Id = row.Id,
                Level = row.Level,
                Block = row.Hash,
                Timestamp = row.Timestamp,
                Account = Accounts.GetAlias(row.AccountId),
                Kind = MigrationKindToString(row.Kind),
                BalanceChange = row.BalanceChange,
                Storage = row.StorageId == null ? null : storages?[row.StorageId],
                Diffs = row.BigMapUpdates == null ? null : diffs?[row.Id],
                Quote = Quotes.Get(quote, row.Level)
            });
        }

        public async Task<object[][]> GetMigrations(
            AccountParameter account,
            MigrationKindParameter kind,
            Int64Parameter balanceChange,
            Int32Parameter level,
            DateTimeParameter timestamp,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            string[] fields,
            MichelineFormat format,
            Symbols quote)
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
                    case "storage": columns.Add(@"o.""StorageId"""); break;
                    case "diffs":
                        columns.Add(@"o.""Id""");
                        columns.Add(@"o.""BigMapUpdates""");
                        break;
                    case "quote": columns.Add(@"o.""Level"""); break;
                }
            }

            if (columns.Count == 0)
                return Array.Empty<object[]>();

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""MigrationOps"" as o {string.Join(' ', joins)}")
                .Filter("AccountId", account)
                .Filter("Kind", kind)
                .Filter("BalanceChange", balanceChange)
                .FilterA(@"o.""Level""", level)
                .FilterA(@"o.""Timestamp""", timestamp)
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
                    case "storage":
                        var storages = await AccountRepository.GetStorages(db,
                            rows.Where(x => x.StorageId != null)
                                .Select(x => (int)x.StorageId)
                                .Distinct()
                                .ToList(),
                            format);
                        if (storages != null)
                            foreach (var row in rows)
                                result[j++][i] = row.StorageId == null ? null : storages[row.StorageId];
                        break;
                    case "diffs":
                        var diffs = await BigMapsRepository.GetMigrationDiffs(db,
                            rows.Where(x => x.BigMapUpdates != null)
                                .Select(x => (int)x.Id)
                                .ToList(),
                            format);
                        if (diffs != null)
                            foreach (var row in rows)
                                result[j++][i] = row.BigMapUpdates == null ? null : diffs[row.Id];
                        break;
                    case "quote":
                        foreach (var row in rows)
                            result[j++][i] = Quotes.Get(quote, row.Level);
                        break;
                }
            }

            return result;
        }

        public async Task<object[]> GetMigrations(
            AccountParameter account,
            MigrationKindParameter kind,
            Int64Parameter balanceChange,
            Int32Parameter level,
            DateTimeParameter timestamp,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            string field,
            MichelineFormat format,
            Symbols quote)
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
                case "storage": columns.Add(@"o.""StorageId"""); break;
                case "diffs":
                    columns.Add(@"o.""Id""");
                    columns.Add(@"o.""BigMapUpdates""");
                    break;
                case "quote": columns.Add(@"o.""Level"""); break;
            }

            if (columns.Count == 0)
                return Array.Empty<object>();

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""MigrationOps"" as o {string.Join(' ', joins)}")
                .Filter("AccountId", account)
                .Filter("Kind", kind)
                .Filter("BalanceChange", balanceChange)
                .FilterA(@"o.""Level""", level)
                .FilterA(@"o.""Timestamp""", timestamp)
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
                case "storage":
                    var storages = await AccountRepository.GetStorages(db,
                        rows.Where(x => x.StorageId != null)
                            .Select(x => (int)x.StorageId)
                            .Distinct()
                            .ToList(),
                        format);
                    if (storages != null)
                        foreach (var row in rows)
                            result[j++] = row.StorageId == null ? null : storages[row.StorageId];
                    break;
                case "diffs":
                    var diffs = await BigMapsRepository.GetMigrationDiffs(db,
                        rows.Where(x => x.BigMapUpdates != null)
                            .Select(x => (int)x.Id)
                            .ToList(),
                        format);
                    if (diffs != null)
                        foreach (var row in rows)
                            result[j++] = row.BigMapUpdates == null ? null : diffs[row.Id];
                    break;
                case "quote":
                    foreach (var row in rows)
                        result[j++] = Quotes.Get(quote, row.Level);
                    break;
            }

            return result;
        }
        #endregion

        #region revelation penalty
        public async Task<int> GetRevelationPenaltiesCount(
            Int32Parameter level,
            DateTimeParameter timestamp)
        {
            var sql = new SqlBuilder(@"SELECT COUNT(*) FROM ""RevelationPenaltyOps""")
                .Filter("Level", level)
                .Filter("Timestamp", timestamp);

            using var db = GetConnection();
            return await db.QueryFirstAsync<int>(sql.Query, sql.Params);
        }

        public async Task<IEnumerable<RevelationPenaltyOperation>> GetRevelationPenalties(
            AccountParameter baker,
            Int32Parameter level,
            DateTimeParameter timestamp,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            Symbols quote)
        {
            var sql = new SqlBuilder(@"SELECT o.*, b.""Hash"" FROM ""RevelationPenaltyOps"" AS o INNER JOIN ""Blocks"" as b ON b.""Level"" = o.""Level""")
                .FilterA(@"o.""BakerId""", baker)
                .FilterA(@"o.""Level""", level)
                .FilterA(@"o.""Timestamp""", timestamp)
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
                Quote = Quotes.Get(quote, row.Level)
            });
        }

        public async Task<object[][]> GetRevelationPenalties(
            AccountParameter baker,
            Int32Parameter level,
            DateTimeParameter timestamp,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            string[] fields,
            Symbols quote)
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
                .FilterA(@"o.""BakerId""", baker)
                .FilterA(@"o.""Level""", level)
                .FilterA(@"o.""Timestamp""", timestamp)
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
                            result[j++][i] = Quotes.Get(quote, row.Level);
                        break;
                }
            }

            return result;
        }

        public async Task<object[]> GetRevelationPenalties(
            AccountParameter baker,
            Int32Parameter level,
            DateTimeParameter timestamp,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            string field,
            Symbols quote)
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
                .FilterA(@"o.""BakerId""", baker)
                .FilterA(@"o.""Level""", level)
                .FilterA(@"o.""Timestamp""", timestamp)
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
                        result[j++] = Quotes.Get(quote, row.Level);
                    break;
            }

            return result;
        }
        #endregion

        #region baking
        public async Task<int> GetBakingsCount(
            Int32Parameter level,
            DateTimeParameter timestamp)
        {
            var sql = new SqlBuilder(@"SELECT COUNT(*) FROM ""Blocks""")
                .Filter(@"""BakerId"" IS NOT NULL")
                .Filter("Level", level)
                .Filter("Timestamp", timestamp);

            using var db = GetConnection();
            return await db.QueryFirstAsync<int>(sql.Query, sql.Params);
        }

        public async Task<IEnumerable<BakingOperation>> GetBakings(
            AccountParameter baker,
            Int32Parameter level,
            DateTimeParameter timestamp,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            Symbols quote)
        {
            var sql = new SqlBuilder(@"SELECT ""Id"", ""Level"", ""Timestamp"", ""BakerId"", ""Hash"", ""Priority"", ""Deposit"", ""Reward"", ""Fees"" FROM ""Blocks""")
                .Filter("BakerId", baker)
                .Filter(@"""BakerId"" IS NOT NULL")
                .Filter("Level", level)
                .Filter("Timestamp", timestamp)
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
                Deposit = row.Deposit,
                Reward = row.Reward,
                Fees = row.Fees,
                Quote = Quotes.Get(quote, row.Level)
            });
        }

        public async Task<object[][]> GetBakings(
            AccountParameter baker,
            Int32Parameter level,
            DateTimeParameter timestamp,
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
                    case "id": columns.Add(@"""Id"""); break;
                    case "level": columns.Add(@"""Level"""); break;
                    case "timestamp": columns.Add(@"""Timestamp"""); break;
                    case "baker": columns.Add(@"""BakerId"""); break;
                    case "block": columns.Add(@"""Hash"""); break;
                    case "priority": columns.Add(@"""Priority"""); break;
                    case "deposit": columns.Add(@"""Deposit"""); break;
                    case "reward": columns.Add(@"""Reward"""); break;
                    case "fees": columns.Add(@"""Fees"""); break;
                    case "quote": columns.Add(@"""Level"""); break;
                }
            }

            if (columns.Count == 0)
                return Array.Empty<object[]>();

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""Blocks""")
                .Filter("BakerId", baker)
                .Filter(@"""BakerId"" IS NOT NULL")
                .Filter("Level", level)
                .Filter("Timestamp", timestamp)
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
                    case "quote":
                        foreach (var row in rows)
                            result[j++][i] = Quotes.Get(quote, row.Level);
                        break;
                }
            }

            return result;
        }

        public async Task<object[]> GetBakings(
            AccountParameter baker,
            Int32Parameter level,
            DateTimeParameter timestamp,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            string field,
            Symbols quote)
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
                case "deposit": columns.Add(@"""Deposit"""); break;
                case "reward": columns.Add(@"""Reward"""); break;
                case "fees": columns.Add(@"""Fees"""); break;
                case "quote": columns.Add(@"""Level"""); break;
            }

            if (columns.Count == 0)
                return Array.Empty<object>();

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""Blocks""")
                .Filter("BakerId", baker)
                .Filter(@"""BakerId"" IS NOT NULL")
                .Filter("Level", level)
                .Filter("Timestamp", timestamp)
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
                case "quote":
                    foreach (var row in rows)
                        result[j++] = Quotes.Get(quote, row.Level);
                    break;
            }

            return result;
        }
        #endregion

        string MigrationKindToString(int kind) => kind switch
        {
            0 => MigrationKinds.Bootstrap,
            1 => MigrationKinds.ActivateDelegate,
            2 => MigrationKinds.Airdrop,
            3 => MigrationKinds.ProposalInvoice,
            4 => MigrationKinds.CodeChange,
            5 => MigrationKinds.Origination,
            6 => MigrationKinds.Subsidy,
            _ => "unknown"
        };

        string PeriodToString(int period) => period switch
        {
            0 => "proposal",
            1 => "exploration",
            2 => "testing",
            3 => "promotion",
            4 => "adoption",
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
