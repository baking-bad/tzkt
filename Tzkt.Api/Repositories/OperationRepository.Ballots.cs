using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Tzkt.Api.Models;

namespace Tzkt.Api.Repositories
{
    public partial class OperationRepository : DbConnection
    {
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
                SELECT      o.""Id"", o.""Level"", o.""Timestamp"", o.""SenderId"", o.""VotingPower"", o.""Vote"", o.""Epoch"", o.""Period"",
                            b.""Hash"",
                            proposal.""Hash"" as ""ProposalHash"", proposal.""Extras"" ->> 'alias' as ""ProposalAlias"",
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
                    Kind = PeriodKinds.ToString(row.Kind),
                    FirstLevel = row.FirstLevel,
                    LastLevel = row.LastLevel
                },
                Proposal = new ProposalAlias
                {
                    Hash = row.ProposalHash,
                    Alias = row.ProposalAlias
                },
                Delegate = Accounts.GetAlias(row.SenderId),
                VotingPower = row.VotingPower,
                Vote = Votes.ToString(row.Vote),
                Quote = Quotes.Get(quote, row.Level)
            });
        }

        public async Task<IEnumerable<BallotOperation>> GetBallots(Block block, Symbols quote)
        {
            var sql = @"
                SELECT      o.""Id"", o.""Timestamp"", o.""OpHash"", o.""SenderId"", o.""VotingPower"", o.""Vote"", o.""Epoch"", o.""Period"",
                            proposal.""Hash"" as ""ProposalHash"", proposal.""Extras"" ->> 'alias' as ""ProposalAlias"",
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
                    Kind = PeriodKinds.ToString(row.Kind),
                    FirstLevel = row.FirstLevel,
                    LastLevel = row.LastLevel
                },
                Proposal = new ProposalAlias
                {
                    Hash = row.ProposalHash,
                    Alias = row.ProposalAlias
                },
                Delegate = Accounts.GetAlias(row.SenderId),
                VotingPower = row.VotingPower,
                Vote = Votes.ToString(row.Vote),
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
            VoteParameter vote,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            Symbols quote)
        {
            var sql = new SqlBuilder(@"
                SELECT      o.""Id"", o.""Level"", o.""Timestamp"", o.""OpHash"", o.""SenderId"", o.""VotingPower"", o.""Vote"", o.""Epoch"", o.""Period"",
                            b.""Hash"",
                            proposal.""Hash"" as ""ProposalHash"", proposal.""Extras"" ->> 'alias' as ""ProposalAlias"",
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
                .FilterA(@"o.""Vote""", vote)
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
                    Kind = PeriodKinds.ToString(row.Kind),
                    FirstLevel = row.FirstLevel,
                    LastLevel = row.LastLevel
                },
                Proposal = new ProposalAlias
                {
                    Hash = row.ProposalHash,
                    Alias = row.ProposalAlias
                },
                Delegate = Accounts.GetAlias(row.SenderId),
                VotingPower = row.VotingPower,
                Vote = Votes.ToString(row.Vote),
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
            VoteParameter vote,
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
                    case "votingPower": columns.Add(@"o.""VotingPower"""); break;
                    case "vote": columns.Add(@"o.""Vote"""); break;
                    case "proposal":
                        columns.Add(@"proposal.""Hash"" as ""ProposalHash""");
                        columns.Add(@"proposal.""Extras""->> 'alias' as ""ProposalAlias""");
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
                .FilterA(@"o.""Vote""", vote)
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
                                Kind = PeriodKinds.ToString(row.Kind),
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
                    case "votingPower":
                        foreach (var row in rows)
                            result[j++][i] = row.VotingPower;
                        break;
                    case "vote":
                        foreach (var row in rows)
                            result[j++][i] = Votes.ToString(row.Vote);
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
            VoteParameter vote,
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
                case "votingPower": columns.Add(@"o.""VotingPower"""); break;
                case "vote": columns.Add(@"o.""Vote"""); break;
                case "proposal":
                    columns.Add(@"proposal.""Hash"" as ""ProposalHash""");
                    columns.Add(@"proposal.""Extras""->> 'alias' as ""ProposalAlias""");
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
                .FilterA(@"o.""Vote""", vote)
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
                            Kind = PeriodKinds.ToString(row.Kind),
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
                case "votingPower":
                    foreach (var row in rows)
                        result[j++] = row.VotingPower;
                    break;
                case "vote":
                    foreach (var row in rows)
                        result[j++] = Votes.ToString(row.Vote);
                    break;
                case "quote":
                    foreach (var row in rows)
                        result[j++] = Quotes.Get(quote, row.Level);
                    break;
            }

            return result;
        }
    }
}
