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
        readonly StateCache State;
        readonly TimeCache Time;
        readonly AccountsCache Accounts;
        readonly ProposalMetadataService ProposalMetadata;

        public VotingRepository(StateCache state, TimeCache time, AccountsCache accounts, ProposalMetadataService proposalMetadata, IConfiguration config) : base(config)
        {
            State = state;
            Time = time;
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
                SELECT      *
                FROM        ""Proposals""
                WHERE       ""Hash"" = @hash::character(51)
                LIMIT       1";

            using var db = GetConnection();
            var row = await db.QueryFirstOrDefaultAsync(sql, new { hash });
            if (row == null) return null;

            return new Proposal
            {
                Hash = hash,
                Initiator = Accounts.GetAlias(row.InitiatorId),
                FirstPeriod = row.FirstPeriod,
                LastPeriod = row.LastPeriod,
                Epoch = row.Epoch,
                Upvotes = row.Upvotes,
                Rolls = row.Rolls,
                Status = ProposalStatusToString(row.Status),
                Metadata = ProposalMetadata[hash]
            };
        }

        public async Task<IEnumerable<Proposal>> GetProposals(
            Int32Parameter epoch,
            SortParameter sort,
            OffsetParameter offset,
            int limit)
        {
            var sql = new SqlBuilder(@"SELECT * FROM ""Proposals""")
                .Filter("Epoch", epoch)
                .Take(sort, offset, limit, x => x switch
                {
                    "upvotes" => ("Upvotes", "Upvotes"),
                    "rolls" => ("Rolls", "Rolls"),
                    _ => ("Id", "Id")
                });

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            return rows.Select(row => new Proposal
            {
                Hash = row.Hash,
                Initiator = Accounts.GetAlias(row.InitiatorId),
                FirstPeriod = row.FirstPeriod,
                LastPeriod = row.LastPeriod,
                Epoch = row.Epoch,
                Upvotes = row.Upvotes,
                Rolls = row.Rolls,
                Status = ProposalStatusToString(row.Status),
                Metadata = ProposalMetadata[row.Hash]
            });
        }

        public async Task<object[][]> GetProposals(
            Int32Parameter epoch,
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
                    case "hash": columns.Add(@"""Hash"""); break;
                    case "initiator": columns.Add(@"""InitiatorId"""); break;
                    case "firstPeriod": columns.Add(@"""FirstPeriod"""); break;
                    case "lastPeriod": columns.Add(@"""LastPeriod"""); break;
                    case "epoch": columns.Add(@"""Epoch"""); break;
                    case "upvotes": columns.Add(@"""Upvotes"""); break;
                    case "rolls": columns.Add(@"""Rolls"""); break;
                    case "status": columns.Add(@"""Status"""); break;
                    case "metadata": columns.Add(@"""Hash"""); break;
                }
            }

            if (columns.Count == 0)
                return Array.Empty<object[]>();

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""Proposals""")
                .Filter("Epoch", epoch)
                .Take(sort, offset, limit, x => x switch
                {
                    "upvotes" => ("Upvotes", "Upvotes"),
                    "rolls" => ("Rolls", "Rolls"),
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
                    case "hash":
                        foreach (var row in rows)
                            result[j++][i] = row.Hash;
                        break;
                    case "initiator":
                        foreach (var row in rows)
                            result[j++][i] = await Accounts.GetAliasAsync(row.InitiatorId);
                        break;
                    case "firstPeriod":
                        foreach (var row in rows)
                            result[j++][i] = row.FirstPeriod;
                        break;
                    case "lastPeriod":
                        foreach (var row in rows)
                            result[j++][i] = row.LastPeriod;
                        break;
                    case "epoch":
                        foreach (var row in rows)
                            result[j++][i] = row.Epoch;
                        break;
                    case "upvotes":
                        foreach (var row in rows)
                            result[j++][i] = row.Upvotes;
                        break;
                    case "rolls":
                        foreach (var row in rows)
                            result[j++][i] = row.Rolls;
                        break;
                    case "status":
                        foreach (var row in rows)
                            result[j++][i] = ProposalStatusToString(row.Status);
                        break;
                    case "metadata":
                        foreach (var row in rows)
                            result[j++][i] = ProposalMetadata[row.Hash];
                        break;
                }
            }

            return result;
        }

        public async Task<object[]> GetProposals(
            Int32Parameter epoch,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            string field)
        {
            var columns = new HashSet<string>(1);

            switch (field)
            {
                case "hash": columns.Add(@"""Hash"""); break;
                case "initiator": columns.Add(@"""InitiatorId"""); break;
                case "firstPeriod": columns.Add(@"""FirstPeriod"""); break;
                case "lastPeriod": columns.Add(@"""LastPeriod"""); break;
                case "epoch": columns.Add(@"""Epoch"""); break;
                case "upvotes": columns.Add(@"""Upvotes"""); break;
                case "rolls": columns.Add(@"""Rolls"""); break;
                case "status": columns.Add(@"""Status"""); break;
                case "metadata": columns.Add(@"""Hash"""); break;
            }

            if (columns.Count == 0)
                return Array.Empty<object>();

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""Proposals""")
                .Filter("Epoch", epoch)
                .Take(sort, offset, limit, x => x switch
                {
                    "upvotes" => ("Upvotes", "Upvotes"),
                    "rolls" => ("Rolls", "Rolls"),
                    _ => ("Id", "Id")
                });

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
                case "firstPeriod":
                    foreach (var row in rows)
                        result[j++] = row.FirstPeriod;
                    break;
                case "lastPeriod":
                    foreach (var row in rows)
                        result[j++] = row.LastPeriod;
                    break;
                case "epoch":
                    foreach (var row in rows)
                        result[j++] = row.Epoch;
                    break;
                case "upvotes":
                    foreach (var row in rows)
                        result[j++] = row.Upvotes;
                    break;
                case "rolls":
                    foreach (var row in rows)
                        result[j++] = row.Rolls;
                    break;
                case "status":
                    foreach (var row in rows)
                        result[j++] = ProposalStatusToString(row.Status);
                    break;
                case "metadata":
                    foreach (var row in rows)
                        result[j++] = ProposalMetadata[row.Hash];
                    break;
            }

            return result;
        }
        #endregion

        #region periods
        public async Task<VotingPeriod> GetPeriod(int index)
        {
            var sql = $@"
                SELECT  *
                FROM    ""VotingPeriods""
                WHERE   ""Index"" = {index}
                LIMIT   1";

            using var db = GetConnection();
            var row = await db.QueryFirstOrDefaultAsync(sql);
            if (row == null) return null;

            return new VotingPeriod
            {
                Index = row.Index,
                Epoch = row.Epoch,
                FirstLevel = row.FirstLevel,
                StartTime = Time[row.FirstLevel],
                LastLevel = row.LastLevel,
                EndTime = Time[row.LastLevel],
                Kind = KindToString(row.Kind),
                Status = PeriodStatusToString(row.Status),
                TotalBakers = row.TotalBakers,
                TotalRolls = row.TotalRolls,
                UpvotesQuorum = row.UpvotesQuorum == null ? null : row.UpvotesQuorum / 100.0,
                ProposalsCount = row.ProposalsCount,
                TopUpvotes = row.TopUpvotes,
                TopRolls = row.TopRolls,
                BallotsQuorum = row.BallotsQuorum == null ? null : row.BallotsQuorum / 100.0,
                Supermajority = row.Supermajority == null ? null : row.Supermajority / 100.0,
                YayBallots = row.YayBallots,
                YayRolls = row.YayRolls,
                NayBallots = row.NayBallots,
                NayRolls = row.NayRolls,
                PassBallots = row.PassBallots,
                PassRolls = row.PassRolls
            };
        }

        public async Task<IEnumerable<VotingPeriod>> GetPeriods(SortParameter sort, OffsetParameter offset, int limit)
        {
            var sql = new SqlBuilder(@"SELECT * FROM ""VotingPeriods""")
                .Take(sort, offset, limit, x => ("Id", "Id"));

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            return rows.Select(row => new VotingPeriod
            {
                Index = row.Index,
                Epoch = row.Epoch,
                FirstLevel = row.FirstLevel,
                StartTime = Time[row.FirstLevel],
                LastLevel = row.LastLevel,
                EndTime = Time[row.LastLevel],
                Kind = KindToString(row.Kind),
                Status = PeriodStatusToString(row.Status),
                TotalBakers = row.TotalBakers,
                TotalRolls = row.TotalRolls,
                UpvotesQuorum = row.UpvotesQuorum == null ? null : row.UpvotesQuorum / 100.0,
                ProposalsCount = row.ProposalsCount,
                TopUpvotes = row.TopUpvotes,
                TopRolls = row.TopRolls,
                BallotsQuorum = row.BallotsQuorum == null ? null : row.BallotsQuorum / 100.0,
                Supermajority = row.Supermajority == null ? null : row.Supermajority / 100.0,
                YayBallots = row.YayBallots,
                YayRolls = row.YayRolls,
                NayBallots = row.NayBallots,
                NayRolls = row.NayRolls,
                PassBallots = row.PassBallots,
                PassRolls = row.PassRolls
            });
        }

        public async Task<object[][]> GetPeriods(SortParameter sort, OffsetParameter offset, int limit, string[] fields)
        {
            var columns = new HashSet<string>(fields.Length);

            foreach (var field in fields)
            {
                switch (field)
                {
                    case "index": columns.Add(@"""Index"""); break;
                    case "epoch": columns.Add(@"""Epoch"""); break;
                    case "firstLevel": columns.Add(@"""FirstLevel"""); break;
                    case "startTime": columns.Add(@"""FirstLevel"""); break;
                    case "lastLevel": columns.Add(@"""LastLevel"""); break;
                    case "endTime": columns.Add(@"""LastLevel"""); break;
                    case "kind": columns.Add(@"""Kind"""); break;
                    case "status": columns.Add(@"""Status"""); break;
                    case "totalBakers": columns.Add(@"""TotalBakers"""); break;
                    case "totalRolls": columns.Add(@"""TotalRolls"""); break;
                    case "upvotesQuorum": columns.Add(@"""UpvotesQuorum"""); break;
                    case "proposalsCount": columns.Add(@"""ProposalsCount"""); break;
                    case "topUpvotes": columns.Add(@"""TopUpvotes"""); break;
                    case "topRolls": columns.Add(@"""TopRolls"""); break;
                    case "ballotsQuorum": columns.Add(@"""BallotsQuorum"""); break;
                    case "supermajority": columns.Add(@"""Supermajority"""); break;
                    case "yayBallots": columns.Add(@"""YayBallots"""); break;
                    case "yayRolls": columns.Add(@"""YayRolls"""); break;
                    case "nayBallots": columns.Add(@"""NayBallots"""); break;
                    case "nayRolls": columns.Add(@"""NayRolls"""); break;
                    case "passBallots": columns.Add(@"""PassBallots"""); break;
                    case "passRolls": columns.Add(@"""PassRolls"""); break;
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
                            result[j++][i] = row.Index;
                        break;
                    case "epoch":
                        foreach (var row in rows)
                            result[j++][i] = row.Epoch;
                        break;
                    case "firstLevel":
                        foreach (var row in rows)
                            result[j++][i] = row.FirstLevel;
                        break;
                    case "startTime":
                        foreach (var row in rows)
                            result[j++][i] = Time[row.FirstLevel];
                        break;
                    case "lastLevel":
                        foreach (var row in rows)
                            result[j++][i] = row.LastLevel;
                        break;
                    case "endTime":
                        foreach (var row in rows)
                            result[j++][i] = Time[row.LastLevel];
                        break;
                    case "kind":
                        foreach (var row in rows)
                            result[j++][i] = KindToString(row.Kind);
                        break;
                    case "status":
                        foreach (var row in rows)
                            result[j++][i] = PeriodStatusToString(row.Status);
                        break;
                    case "totalBakers":
                        foreach (var row in rows)
                            result[j++][i] = row.TotalBakers;
                        break;
                    case "totalRolls":
                        foreach (var row in rows)
                            result[j++][i] = row.TotalRolls;
                        break;
                    case "upvotesQuorum":
                        foreach (var row in rows)
                            result[j++][i] = row.UpvotesQuorum;
                        break;
                    case "proposalsCount":
                        foreach (var row in rows)
                            result[j++][i] = row.ProposalsCount;
                        break;
                    case "topUpvotes":
                        foreach (var row in rows)
                            result[j++][i] = row.TopUpvotes;
                        break;
                    case "topRolls":
                        foreach (var row in rows)
                            result[j++][i] = row.TopRolls;
                        break;
                    case "ballotsQuorum":
                        foreach (var row in rows)
                            result[j++][i] = row.BallotsQuorum;
                        break;
                    case "supermajority":
                        foreach (var row in rows)
                            result[j++][i] = row.Supermajority;
                        break;
                    case "yayBallots":
                        foreach (var row in rows)
                            result[j++][i] = row.YayBallots;
                        break;
                    case "yayRolls":
                        foreach (var row in rows)
                            result[j++][i] = row.YayRolls;
                        break;
                    case "nayBallots":
                        foreach (var row in rows)
                            result[j++][i] = row.NayBallots;
                        break;
                    case "nayRolls":
                        foreach (var row in rows)
                            result[j++][i] = row.NayRolls;
                        break;
                    case "passBallots":
                        foreach (var row in rows)
                            result[j++][i] = row.PassBallots;
                        break;
                    case "passRolls":
                        foreach (var row in rows)
                            result[j++][i] = row.PassRolls;
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
                case "index": columns.Add(@"""Index"""); break;
                case "epoch": columns.Add(@"""Epoch"""); break;
                case "firstLevel": columns.Add(@"""FirstLevel"""); break;
                case "startTime": columns.Add(@"""FirstLevel"""); break;
                case "lastLevel": columns.Add(@"""LastLevel"""); break;
                case "endTime": columns.Add(@"""LastLevel"""); break;
                case "kind": columns.Add(@"""Kind"""); break;
                case "status": columns.Add(@"""Status"""); break;
                case "totalBakers": columns.Add(@"""TotalBakers"""); break;
                case "totalRolls": columns.Add(@"""TotalRolls"""); break;
                case "upvotesQuorum": columns.Add(@"""UpvotesQuorum"""); break;
                case "proposalsCount": columns.Add(@"""ProposalsCount"""); break;
                case "topUpvotes": columns.Add(@"""TopUpvotes"""); break;
                case "topRolls": columns.Add(@"""TopRolls"""); break;
                case "ballotsQuorum": columns.Add(@"""BallotsQuorum"""); break;
                case "supermajority": columns.Add(@"""Supermajority"""); break;
                case "yayBallots": columns.Add(@"""YayBallots"""); break;
                case "yayRolls": columns.Add(@"""YayRolls"""); break;
                case "nayBallots": columns.Add(@"""NayBallots"""); break;
                case "nayRolls": columns.Add(@"""NayRolls"""); break;
                case "passBallots": columns.Add(@"""PassBallots"""); break;
                case "passRolls": columns.Add(@"""PassRolls"""); break;
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
                        result[j++] = row.Index;
                    break;
                case "epoch":
                    foreach (var row in rows)
                        result[j++] = row.Epoch;
                    break;
                case "firstLevel":
                    foreach (var row in rows)
                        result[j++] = row.FirstLevel;
                    break;
                case "startTime":
                    foreach (var row in rows)
                        result[j++] = Time[row.FirstLevel];
                    break;
                case "lastLevel":
                    foreach (var row in rows)
                        result[j++] = row.LastLevel;
                    break;
                case "endTime":
                    foreach (var row in rows)
                        result[j++] = Time[row.LastLevel];
                    break;
                case "kind":
                    foreach (var row in rows)
                        result[j++] = KindToString(row.Kind);
                    break;
                case "status":
                    foreach (var row in rows)
                        result[j++] = PeriodStatusToString(row.Status);
                    break;
                case "totalBakers":
                    foreach (var row in rows)
                        result[j++] = row.TotalBakers;
                    break;
                case "totalRolls":
                    foreach (var row in rows)
                        result[j++] = row.TotalRolls;
                    break;
                case "upvotesQuorum":
                    foreach (var row in rows)
                        result[j++] = row.UpvotesQuorum;
                    break;
                case "proposalsCount":
                    foreach (var row in rows)
                        result[j++] = row.ProposalsCount;
                    break;
                case "topUpvotes":
                    foreach (var row in rows)
                        result[j++] = row.TopUpvotes;
                    break;
                case "topRolls":
                    foreach (var row in rows)
                        result[j++] = row.TopRolls;
                    break;
                case "ballotsQuorum":
                    foreach (var row in rows)
                        result[j++] = row.BallotsQuorum;
                    break;
                case "supermajority":
                    foreach (var row in rows)
                        result[j++] = row.Supermajority;
                    break;
                case "yayBallots":
                    foreach (var row in rows)
                        result[j++] = row.YayBallots;
                    break;
                case "yayRolls":
                    foreach (var row in rows)
                        result[j++] = row.YayRolls;
                    break;
                case "nayBallots":
                    foreach (var row in rows)
                        result[j++] = row.NayBallots;
                    break;
                case "nayRolls":
                    foreach (var row in rows)
                        result[j++] = row.NayRolls;
                    break;
                case "passBallots":
                    foreach (var row in rows)
                        result[j++] = row.PassBallots;
                    break;
                case "passRolls":
                    foreach (var row in rows)
                        result[j++] = row.PassRolls;
                    break;
            }

            return result;
        }

        public async Task<VoterSnapshot> GetVoter(int period, string address)
        {
            var rawAccount = await Accounts.GetAsync(address);
            if (rawAccount is not RawDelegate baker) return null;

            var sql = $@"
                SELECT  *
                FROM    ""VotingSnapshots""
                WHERE   ""Period"" = {period}
                AND     ""BakerId"" = {baker.Id}
                LIMIT   1";

            using var db = GetConnection();
            var row = await db.QueryFirstOrDefaultAsync(sql);
            if (row == null) return null;

            return new VoterSnapshot
            {
                Delegate = Accounts.GetAlias(row.BakerId),
                Rolls = row.Rolls,
                Status = VoterStatusToString(row.Status)
            };
        }

        public async Task<IEnumerable<VoterSnapshot>> GetVoters(
            int period,
            VoterStatusParameter status,
            SortParameter sort,
            OffsetParameter offset,
            int limit)
        {
            var sql = new SqlBuilder($@"SELECT * FROM ""VotingSnapshots""")
                .Filter("Period", period)
                .Filter("Status", status)
                .Take(sort, offset, limit, x => x == "rolls" ? ("Rolls", "Rolls") : ("Id", "Id"));

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            return rows.Select(row => new VoterSnapshot
            {
                Delegate = Accounts.GetAlias(row.BakerId),
                Rolls = row.Rolls,
                Status = VoterStatusToString(row.Status)
            });
        }
        #endregion

        #region epochs
        public async Task<VotingEpoch> GetEpoch(int index)
        {
            var sql = $@"
                SELECT  *
                FROM     ""VotingPeriods""
                WHERE    ""Epoch"" = {index}
                ORDER BY ""Index""";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql);
            if (!rows.Any()) return null;

            return new VotingEpoch
            {
                Index = rows.First().Epoch,
                FirstLevel = rows.First().FirstLevel,
                StartTime = Time[rows.First().FirstLevel],
                LastLevel = rows.Last().LastLevel,
                EndTime = Time[rows.Last().LastLevel],
                Status = GetEpochStatus(rows),
                Periods = rows.Select(row => new VotingPeriod
                {
                    Index = row.Index,
                    Epoch = row.Epoch,
                    FirstLevel = row.FirstLevel,
                    StartTime = Time[row.FirstLevel],
                    LastLevel = row.LastLevel,
                    EndTime = Time[row.LastLevel],
                    Kind = KindToString(row.Kind),
                    Status = PeriodStatusToString(row.Status),
                    TotalBakers = row.TotalBakers,
                    TotalRolls = row.TotalRolls,
                    UpvotesQuorum = row.UpvotesQuorum == null ? null : row.UpvotesQuorum / 100.0,
                    ProposalsCount = row.ProposalsCount,
                    TopUpvotes = row.TopUpvotes,
                    TopRolls = row.TopRolls,
                    BallotsQuorum = row.BallotsQuorum == null ? null : row.BallotsQuorum / 100.0,
                    Supermajority = row.Supermajority == null ? null : row.Supermajority / 100.0,
                    YayBallots = row.YayBallots,
                    YayRolls = row.YayRolls,
                    NayBallots = row.NayBallots,
                    NayRolls = row.NayRolls,
                    PassBallots = row.PassBallots,
                    PassRolls = row.PassRolls
                })
            };
        }

        public async Task<IEnumerable<VotingEpoch>> GetEpochs(SortParameter sort, OffsetParameter offset, int limit)
        {
            var sql = new SqlBuilder(@"SELECT * FROM ""VotingPeriods""")
                .Take(sort, offset, limit * 5, x => ("Id", "Id"));

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.Query, sql.Params);
            if (!rows.Any()) return Enumerable.Empty<VotingEpoch>();

            return rows
                .GroupBy(x => x.Epoch)
                .Select(group =>
                {
                    var periods = group.OrderBy(x => x.Index);
                    return new VotingEpoch
                    {
                        Index = group.Key,
                        FirstLevel = periods.First().FirstLevel,
                        StartTime = Time[periods.First().FirstLevel],
                        LastLevel = periods.Last().LastLevel,
                        EndTime = Time[periods.Last().LastLevel],
                        Status = GetEpochStatus(periods),
                        Periods = periods.Select(row => new VotingPeriod
                        {
                            Index = row.Index,
                            Epoch = row.Epoch,
                            FirstLevel = row.FirstLevel,
                            StartTime = Time[row.FirstLevel],
                            LastLevel = row.LastLevel,
                            EndTime = Time[row.LastLevel],
                            Kind = KindToString(row.Kind),
                            Status = PeriodStatusToString(row.Status),
                            TotalBakers = row.TotalBakers,
                            TotalRolls = row.TotalRolls,
                            UpvotesQuorum = row.UpvotesQuorum == null ? null : row.UpvotesQuorum / 100.0,
                            ProposalsCount = row.ProposalsCount,
                            TopUpvotes = row.TopUpvotes,
                            TopRolls = row.TopRolls,
                            BallotsQuorum = row.BallotsQuorum == null ? null : row.BallotsQuorum / 100.0,
                            Supermajority = row.Supermajority == null ? null : row.Supermajority / 100.0,
                            YayBallots = row.YayBallots,
                            YayRolls = row.YayRolls,
                            NayBallots = row.NayBallots,
                            NayRolls = row.NayRolls,
                            PassBallots = row.PassBallots,
                            PassRolls = row.PassRolls
                        })
                    };
                });
        }

        public async Task<VotingEpoch> GetLatestVoting()
        {
            var sql = $@"
                SELECT  period.*
                FROM    ""VotingPeriods"" as p
                INNER JOIN ""VotingPeriods"" as period on period.""Epoch"" = p.""Epoch""
                WHERE   p.""ProposalsCount"" IS NOT NULL
                AND     p.""ProposalsCount"" > 0
                ORDER BY p.""Index"" DESC
                LIMIT 5";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql);
            if (!rows.Any()) return null;

            var epoch = rows.First().Epoch;
            rows = rows.Where(x => x.Epoch == epoch).OrderBy(x => x.Index);

            return new VotingEpoch
            {
                Index = rows.First().Epoch,
                FirstLevel = rows.First().FirstLevel,
                StartTime = Time[rows.First().FirstLevel],
                LastLevel = rows.Last().LastLevel,
                EndTime = Time[rows.Last().LastLevel],
                Status = GetEpochStatus(rows),
                Periods = rows.Select(row => new VotingPeriod
                {
                    Index = row.Index,
                    Epoch = row.Epoch,
                    FirstLevel = row.FirstLevel,
                    StartTime = Time[row.FirstLevel],
                    LastLevel = row.LastLevel,
                    EndTime = Time[row.LastLevel],
                    Kind = KindToString(row.Kind),
                    Status = PeriodStatusToString(row.Status),
                    TotalBakers = row.TotalBakers,
                    TotalRolls = row.TotalRolls,
                    UpvotesQuorum = row.UpvotesQuorum == null ? null : row.UpvotesQuorum / 100.0,
                    ProposalsCount = row.ProposalsCount,
                    TopUpvotes = row.TopUpvotes,
                    TopRolls = row.TopRolls,
                    BallotsQuorum = row.BallotsQuorum == null ? null : row.BallotsQuorum / 100.0,
                    Supermajority = row.Supermajority == null ? null : row.Supermajority / 100.0,
                    YayBallots = row.YayBallots,
                    YayRolls = row.YayRolls,
                    NayBallots = row.NayBallots,
                    NayRolls = row.NayRolls,
                    PassBallots = row.PassBallots,
                    PassRolls = row.PassRolls
                })
            };
        }

        string GetEpochStatus(IEnumerable<dynamic> periods)
        {
            if (periods.First().Status == (int)Data.Models.PeriodStatus.NoProposals)
                return "no_proposals";

            if (periods.Last().Status == (int)Data.Models.PeriodStatus.Active)
                return "voting";

            if (periods.Last().Status == (int)Data.Models.PeriodStatus.Success &&
                periods.Last().Epoch != State.Current.VotingEpoch)
                return "completed";

            return "failed";
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
                4 => "adoption",
                _ => "unknown"
            };
        }

        string ProposalStatusToString(int status)
        {
            return status switch
            {
                0 => "active",
                1 => "accepted",
                2 => "rejected",
                3 => "skipped",
                _ => "unknown"
            };
        }

        string PeriodStatusToString(int status)
        {
            return status switch
            {
                0 => "active",
                1 => "no_proposals",
                2 => "no_quorum",
                3 => "no_supermajority",
                4 => "success",
                _ => "unknown"
            };
        }

        string VoterStatusToString(int status)
        {
            return status switch
            {
                0 => VoterStatuses.None,
                1 => VoterStatuses.Upvoted,
                2 => VoterStatuses.VotedYay,
                3 => VoterStatuses.VotedNay,
                4 => VoterStatuses.VotedPass,
                _ => "unknown"
            };
        }
    }
}
