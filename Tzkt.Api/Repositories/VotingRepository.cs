using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Dapper;

using Tzkt.Api.Models;
using Tzkt.Api.Services.Metadata;
using Tzkt.Api.Services.Cache;

namespace Tzkt.Api.Repositories
{
    public class VotingRepository : DbConnection
    {
        readonly AccountsCache Accounts;
        readonly ProposalMetadataService ProposalMetadata;

        public VotingRepository(AccountsCache accounts, ProposalMetadataService proposalMetadata, IConfiguration config) : base(config)
        {
            Accounts = accounts;
            ProposalMetadata = proposalMetadata;
        }

        #region proposals
        public async Task<int> GetProposalsCount()
        {
            var sql = @"
                SELECT   COUNT(*)
                FROM     ""Proposals""";

            using var db = GetConnection();
            return await db.QueryFirstAsync<int>(sql);
        }

        public async Task<Proposal> GetProposal(string hash)
        {
            var sql = @"
                SELECT      p.""InitiatorId"", p.""Upvotes"", p.""ExplorationPeriodId"", p.""PromotionPeriodId"", v.""Code""
                FROM        ""Proposals"" as p
                INNER JOIN  ""VotingPeriods"" as v
                        ON  v.""Id"" = p.""ProposalPeriodId""
                WHERE       p.""Hash"" = @hash::character(51)
                LIMIT       1";

            using var db = GetConnection();
            var row = await db.QueryFirstOrDefaultAsync(sql, new { hash });
            if (row == null) return null;

            return new Proposal
            {
                Hash = hash,
                Initiator = Accounts.GetAlias(row.InitiatorId),
                Period = row.Code,
                Upvotes = row.Upvotes,
                Status = row.ExplorationPeriodId == null ? "skipped" : row.PromotionPeriodId == null ? "rejected" : "accepted",
                Metadata = ProposalMetadata[hash]
            };
        }

        public async Task<IEnumerable<Proposal>> GetProposals(SortParameter sort, OffsetParameter offset, int limit)
        {
            var sql = new SqlBuilder(@"
                SELECT      p.""Hash"", p.""InitiatorId"", p.""Upvotes"", p.""ExplorationPeriodId"", p.""PromotionPeriodId"", v.""Code""
                FROM        ""Proposals"" as p
                INNER JOIN  ""VotingPeriods"" as v ON v.""Id"" = p.""ProposalPeriodId""
                ")
                .Take(sort, offset, limit, x => ("Id", "Id"), "p");

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            return rows.Select(row => new Proposal
            {
                Hash = row.Hash,
                Initiator = Accounts.GetAlias(row.InitiatorId),
                Period = row.Code,
                Upvotes = row.Upvotes,
                Status = row.ExplorationPeriodId == null ? "skipped" : row.PromotionPeriodId == null ? "rejected" : "accepted",
                Metadata = ProposalMetadata[row.Hash]
            });
        }

        public async Task<object[][]> GetProposals(SortParameter sort, OffsetParameter offset, int limit, string[] fields)
        {
            var columns = new HashSet<string>(fields.Length + 1);
            var joins = new HashSet<string>(1);

            foreach (var field in fields)
            {
                switch (field)
                {
                    case "hash": columns.Add(@"p.""Hash"""); break;
                    case "initiator": columns.Add(@"p.""InitiatorId"""); break;
                    case "upvotes": columns.Add(@"p.""Upvotes"""); break;
                    case "metadata": columns.Add(@"p.""Hash"""); break;
                    case "status": 
                        columns.Add(@"p.""ExplorationPeriodId""");
                        columns.Add(@"p.""PromotionPeriodId""");
                        break;
                    case "period":
                        columns.Add(@"v.""Code""");
                        joins.Add(@"INNER JOIN ""VotingPeriods"" as v ON v.""Id"" = p.""ProposalPeriodId""");
                        break;
                }
            }

            if (columns.Count == 0)
                return Array.Empty<object[]>();

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""Proposals"" as p {string.Join(' ', joins)}")
                .Take(sort, offset, limit, x => ("Id", "Id"), "p");

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            var result = new object[rows.Count()][];
            for (int i = 0; i < result.Length; i++)
                result[i] = new object[fields.Length];

            for (int i = 0, j = 0; i < fields.Length; j = 0, i++)
            {
                switch (fields[i])
                {
                    case "hash":
                        foreach (var row in rows)
                            result[j++][i] = row.Hash;
                        break;
                    case "initiator":
                        foreach (var row in rows)
                            result[j++][i] = await Accounts.GetAliasAsync(row.InitiatorId);
                        break;
                    case "upvotes":
                        foreach (var row in rows)
                            result[j++][i] = row.Upvotes;
                        break;
                    case "metadata":
                        foreach (var row in rows)
                            result[j++][i] = ProposalMetadata[row.Hash];
                        break;
                    case "status":
                        foreach (var row in rows)
                            result[j++][i] = row.ExplorationPeriodId == null ? "skipped" : row.PromotionPeriodId == null ? "rejected" : "accepted";
                        break;
                    case "period":
                        foreach (var row in rows)
                            result[j++][i] = row.Code;
                        break;
                }
            }

            return result;
        }

        public async Task<object[]> GetProposals(SortParameter sort, OffsetParameter offset, int limit, string field)
        {
            var columns = new HashSet<string>(2);
            var joins = new HashSet<string>(1);

            switch (field)
            {
                case "hash": columns.Add(@"p.""Hash"""); break;
                case "initiator": columns.Add(@"p.""InitiatorId"""); break;
                case "upvotes": columns.Add(@"p.""Upvotes"""); break;
                case "metadata": columns.Add(@"p.""Hash"""); break;
                case "status":
                    columns.Add(@"p.""ExplorationPeriodId""");
                    columns.Add(@"p.""PromotionPeriodId""");
                    break;
                case "period":
                    columns.Add(@"v.""Code""");
                    joins.Add(@"INNER JOIN ""VotingPeriods"" as v ON v.""Id"" = p.""ProposalPeriodId""");
                    break;
            }

            if (columns.Count == 0)
                return Array.Empty<object>();

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""Proposals"" as p {string.Join(' ', joins)}")
                .Take(sort, offset, limit, x => ("Id", "Id"), "p");

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            //TODO: optimize memory allocation
            var result = new object[rows.Count()];
            var j = 0;

            switch (field)
            {
                case "hash":
                    foreach (var row in rows)
                        result[j++] = row.Hash;
                    break;
                case "initiator":
                    foreach (var row in rows)
                        result[j++] = await Accounts.GetAliasAsync(row.InitiatorId);
                    break;
                case "upvotes":
                    foreach (var row in rows)
                        result[j++] = row.Upvotes;
                    break;
                case "metadata":
                    foreach (var row in rows)
                        result[j++] = ProposalMetadata[row.Hash];
                    break;
                case "status":
                    foreach (var row in rows)
                        result[j++] = row.ExplorationPeriodId == null ? "skipped" : row.PromotionPeriodId == null ? "rejected" : "accepted";
                    break;
                case "period":
                    foreach (var row in rows)
                        result[j++] = row.Code;
                    break;
            }

            return result;
        }
        #endregion

        #region periods
        public async Task<VotingPeriod> GetPeriod(int index)
        {
            var sql = $@"
                SELECT  ""Code"", ""Kind"", ""StartLevel"", ""EndLevel""
                FROM    ""VotingPeriods""
                WHERE   ""Code"" = {index}
                LIMIT   1";

            using var db = GetConnection();
            var row = await db.QueryFirstOrDefaultAsync(sql);
            if (row == null) return null;

            return new VotingPeriod
            {
                Index = row.Code,
                Kind = KindToString(row.Kind),
                FirstLevel = row.StartLevel,
                LastLevel = row.EndLevel
            };
        }

        public async Task<IEnumerable<VotingPeriod>> GetPeriods(SortParameter sort, OffsetParameter offset, int limit)
        {
            var sql = new SqlBuilder(@"SELECT ""Code"", ""Kind"", ""StartLevel"", ""EndLevel"" FROM ""VotingPeriods""")
                .Take(sort, offset, limit, x => ("Id", "Id"));

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            return rows.Select(row => new VotingPeriod
            {
                Index = row.Code,
                Kind = KindToString(row.Kind),
                FirstLevel = row.StartLevel,
                LastLevel = row.EndLevel
            });
        }

        public async Task<object[][]> GetPeriods(SortParameter sort, OffsetParameter offset, int limit, string[] fields)
        {
            var columns = new HashSet<string>(fields.Length);

            foreach (var field in fields)
            {
                switch (field)
                {
                    case "index": columns.Add(@"""Code"""); break;
                    case "kind": columns.Add(@"""Kind"""); break;
                    case "firstLevel": columns.Add(@"""StartLevel"""); break;
                    case "lastLevel": columns.Add(@"""EndLevel"""); break;
                }
            }

            if (columns.Count == 0)
                return Array.Empty<object[]>();

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""VotingPeriods""")
                .Take(sort, offset, limit, x => ("Id", "Id"));

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            var result = new object[rows.Count()][];
            for (int i = 0; i < result.Length; i++)
                result[i] = new object[fields.Length];

            for (int i = 0, j = 0; i < fields.Length; j = 0, i++)
            {
                switch (fields[i])
                {
                    case "index":
                        foreach (var row in rows)
                            result[j++][i] = row.Code;
                        break;
                    case "kind":
                        foreach (var row in rows)
                            result[j++][i] = KindToString(row.Kind);
                        break;
                    case "firstLevel":
                        foreach (var row in rows)
                            result[j++][i] = row.StartLevel;
                        break;
                    case "lastLevel":
                        foreach (var row in rows)
                            result[j++][i] = row.EndLevel;
                        break;
                }
            }

            return result;
        }

        public async Task<object[]> GetPeriods(SortParameter sort, OffsetParameter offset, int limit, string field)
        {
            var columns = new HashSet<string>(1);

            switch (field)
            {
                case "index": columns.Add(@"""Code"""); break;
                case "kind": columns.Add(@"""Kind"""); break;
                case "firstLevel": columns.Add(@"""StartLevel"""); break;
                case "lastLevel": columns.Add(@"""EndLevel"""); break;
            }

            if (columns.Count == 0)
                return Array.Empty<object>();

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""VotingPeriods""")
                .Take(sort, offset, limit, x => ("Id", "Id"));

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            //TODO: optimize memory allocation
            var result = new object[rows.Count()];
            var j = 0;

            switch (field)
            {
                case "index":
                    foreach (var row in rows)
                        result[j++] = row.Code;
                    break;
                case "kind":
                    foreach (var row in rows)
                        result[j++] = KindToString(row.Kind);
                    break;
                case "firstLevel":
                    foreach (var row in rows)
                        result[j++] = row.StartLevel;
                    break;
                case "lastLevel":
                    foreach (var row in rows)
                        result[j++] = row.EndLevel;
                    break;
            }

            return result;
        }
        #endregion

        string KindToString(int kind)
        {
            return kind switch
            {
                0 => "proposal",
                1 => "exploration",
                2 => "testing",
                3 => "promotion",
                _ => "unknown"
            };
        }
    }
}
