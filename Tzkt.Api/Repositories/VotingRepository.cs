﻿using Dapper;
using Npgsql;
using Tzkt.Api.Models;
using Tzkt.Api.Services.Cache;

namespace Tzkt.Api.Repositories
{
    public class VotingRepository
    {
        readonly NpgsqlDataSource DataSource;
        readonly StateCache State;
        readonly TimeCache Time;
        readonly AccountsCache Accounts;

        public VotingRepository(NpgsqlDataSource dataSource, StateCache state, TimeCache time, AccountsCache accounts)
        {
            DataSource = dataSource;
            State = state;
            Time = time;
            Accounts = accounts;
        }

        #region proposals
        public async Task<int> GetProposalsCount()
        {
            var sql = @"
                SELECT   COUNT(*)
                FROM     ""Proposals""";

            await using var db = await DataSource.OpenConnectionAsync();
            return await db.QueryFirstAsync<int>(sql);
        }

        public async Task<Proposal?> GetProposal(string hash)
        {
            var sql = @"
                SELECT      *
                FROM        ""Proposals""
                WHERE       ""Hash"" = @hash::character(51)
                ORDER BY    ""Epoch"" DESC
                LIMIT       1";

            await using var db = await DataSource.OpenConnectionAsync();
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
                VotingPower = row.VotingPower,
                Status = ProposalStatuses.ToString(row.Status),
                Extras = row.Extras
            };
        }

        public async Task<IEnumerable<Proposal>> GetProposals(
            ProtocolParameter? hash,
            Int32Parameter? epoch,
            SortParameter? sort,
            OffsetParameter? offset,
            int limit)
        {
            var sql = new SqlBuilder(@"SELECT * FROM ""Proposals""")
                .Filter("Hash", hash)
                .Filter("Epoch", epoch)
                .Take(sort, offset, limit, x => x switch
                {
                    "upvotes" => ("Upvotes", "Upvotes"),
                    "votingPower" => ("VotingPower", "VotingPower"),
                    _ => ("Id", "Id")
                });

            await using var db = await DataSource.OpenConnectionAsync();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            return rows.Select(row => new Proposal
            {
                Hash = row.Hash,
                Initiator = Accounts.GetAlias(row.InitiatorId),
                FirstPeriod = row.FirstPeriod,
                LastPeriod = row.LastPeriod,
                Epoch = row.Epoch,
                Upvotes = row.Upvotes,
                VotingPower = row.VotingPower,
                Status = ProposalStatuses.ToString(row.Status),
                Extras = row.Extras
            });
        }

        public async Task<object?[][]> GetProposals(
            ProtocolParameter? hash,
            Int32Parameter? epoch,
            SortParameter? sort,
            OffsetParameter? offset,
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
                    case "votingPower": columns.Add(@"""VotingPower"""); break;
                    case "status": columns.Add(@"""Status"""); break;
                    case "extras": columns.Add(@"""Extras"""); break;
                }
            }

            if (columns.Count == 0)
                return [];

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""Proposals""")
                .Filter("Hash", hash)
                .Filter("Epoch", epoch)
                .Take(sort, offset, limit, x => x switch
                {
                    "upvotes" => ("Upvotes", "Upvotes"),
                    "votingPower" => ("VotingPower", "VotingPower"),
                    _ => ("Id", "Id")
                });

            await using var db = await DataSource.OpenConnectionAsync();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            var result = new object?[rows.Count()][];
            for (int i = 0; i < result.Length; i++)
                result[i] = new object?[fields.Length];

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
                    case "votingPower":
                        foreach (var row in rows)
                            result[j++][i] = row.VotingPower;
                        break;
                    case "status":
                        foreach (var row in rows)
                            result[j++][i] = ProposalStatuses.ToString(row.Status);
                        break;
                    case "extras":
                        foreach (var row in rows)
                            result[j++][i] = (RawJson?)row.Extras;
                        break;
                }
            }

            return result;
        }

        public async Task<object?[]> GetProposals(
            ProtocolParameter? hash,
            Int32Parameter? epoch,
            SortParameter? sort,
            OffsetParameter? offset,
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
                case "votingPower": columns.Add(@"""VotingPower"""); break;
                case "status": columns.Add(@"""Status"""); break;
                case "extras": columns.Add(@"""Extras"""); break;
            }

            if (columns.Count == 0)
                return [];

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""Proposals""")
                .Filter("Hash", hash)
                .Filter("Epoch", epoch)
                .Take(sort, offset, limit, x => x switch
                {
                    "upvotes" => ("Upvotes", "Upvotes"),
                    "votingPower" => ("VotingPower", "VotingPower"),
                    _ => ("Id", "Id")
                });

            await using var db = await DataSource.OpenConnectionAsync();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            //TODO: optimize memory allocation
            var result = new object?[rows.Count()];
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
                case "votingPower":
                    foreach (var row in rows)
                        result[j++] = row.VotingPower;
                    break;
                case "status":
                    foreach (var row in rows)
                        result[j++] = ProposalStatuses.ToString(row.Status);
                    break;
                case "extras":
                    foreach (var row in rows)
                        result[j++] = (RawJson?)row.Extras;
                    break;
            }

            return result;
        }
        #endregion

        #region periods
        public async Task<VotingPeriod?> GetPeriod(int index)
        {
            var sql = $@"
                SELECT  *
                FROM    ""VotingPeriods""
                WHERE   ""Index"" = {index}
                LIMIT   1";

            await using var db = await DataSource.OpenConnectionAsync();
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
                Kind = PeriodKinds.ToString(row.Kind),
                Status = PeriodStatuses.ToString(row.Status),
                Dictator = PeriodDictatorStatuses.ToString(row.Dictator),
                TotalBakers = row.TotalBakers,
                TotalVotingPower = row.TotalVotingPower,
                UpvotesQuorum = row.UpvotesQuorum == null ? null : row.UpvotesQuorum / 100.0,
                ProposalsCount = row.ProposalsCount,
                TopUpvotes = row.TopUpvotes,
                TopVotingPower = row.TopVotingPower,
                BallotsQuorum = row.BallotsQuorum == null ? null : row.BallotsQuorum / 100.0,
                Supermajority = row.Supermajority == null ? null : row.Supermajority / 100.0,
                YayBallots = row.YayBallots,
                YayVotingPower = row.YayVotingPower,
                NayBallots = row.NayBallots,
                NayVotingPower = row.NayVotingPower,
                PassBallots = row.PassBallots,
                PassVotingPower = row.PassVotingPower
            };
        }

        public async Task<IEnumerable<VotingPeriod>> GetPeriods(
            Int32Parameter? firstLevel,
            Int32Parameter? lastLevel,
            Int32Parameter? epoch,
            SortParameter? sort,
            OffsetParameter? offset,
            int limit)
        {
            var sql = new SqlBuilder(@"SELECT * FROM ""VotingPeriods""")
                .Filter("FirstLevel", firstLevel)
                .Filter("LastLevel", lastLevel)
                .Filter("Epoch", epoch)
                .Take(sort, offset, limit, x => ("Id", "Id"));

            await using var db = await DataSource.OpenConnectionAsync();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            return rows.Select(row => new VotingPeriod
            {
                Index = row.Index,
                Epoch = row.Epoch,
                FirstLevel = row.FirstLevel,
                StartTime = Time[row.FirstLevel],
                LastLevel = row.LastLevel,
                EndTime = Time[row.LastLevel],
                Kind = PeriodKinds.ToString(row.Kind),
                Status = PeriodStatuses.ToString(row.Status),
                Dictator = PeriodDictatorStatuses.ToString(row.Dictator),
                TotalBakers = row.TotalBakers,
                TotalVotingPower = row.TotalVotingPower,
                UpvotesQuorum = row.UpvotesQuorum == null ? null : row.UpvotesQuorum / 100.0,
                ProposalsCount = row.ProposalsCount,
                TopUpvotes = row.TopUpvotes,
                TopVotingPower = row.TopVotingPower,
                BallotsQuorum = row.BallotsQuorum == null ? null : row.BallotsQuorum / 100.0,
                Supermajority = row.Supermajority == null ? null : row.Supermajority / 100.0,
                YayBallots = row.YayBallots,
                YayVotingPower = row.YayVotingPower,
                NayBallots = row.NayBallots,
                NayVotingPower = row.NayVotingPower,
                PassBallots = row.PassBallots,
                PassVotingPower = row.PassVotingPower
            });
        }

        public async Task<object?[][]> GetPeriods(
            Int32Parameter? firstLevel,
            Int32Parameter? lastLevel,
            Int32Parameter? epoch,
            SortParameter? sort,
            OffsetParameter? offset,
            int limit,
            string[] fields)
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
                    case "dictator": columns.Add(@"""Dictator"""); break;
                    case "totalBakers": columns.Add(@"""TotalBakers"""); break;
                    case "totalVotingPower": columns.Add(@"""TotalVotingPower"""); break;
                    case "upvotesQuorum": columns.Add(@"""UpvotesQuorum"""); break;
                    case "proposalsCount": columns.Add(@"""ProposalsCount"""); break;
                    case "topUpvotes": columns.Add(@"""TopUpvotes"""); break;
                    case "topVotingPower": columns.Add(@"""TopVotingPower"""); break;
                    case "ballotsQuorum": columns.Add(@"""BallotsQuorum"""); break;
                    case "supermajority": columns.Add(@"""Supermajority"""); break;
                    case "yayBallots": columns.Add(@"""YayBallots"""); break;
                    case "yayVotingPower": columns.Add(@"""YayVotingPower"""); break;
                    case "nayBallots": columns.Add(@"""NayBallots"""); break;
                    case "nayVotingPower": columns.Add(@"""NayVotingPower"""); break;
                    case "passBallots": columns.Add(@"""PassBallots"""); break;
                    case "passVotingPower": columns.Add(@"""PassVotingPower"""); break;
                }
            }

            if (columns.Count == 0)
                return [];

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""VotingPeriods""")
                .Filter("FirstLevel", firstLevel)
                .Filter("LastLevel", lastLevel)
                .Filter("Epoch", epoch)
                .Take(sort, offset, limit, x => ("Id", "Id"));

            await using var db = await DataSource.OpenConnectionAsync();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            var result = new object?[rows.Count()][];
            for (int i = 0; i < result.Length; i++)
                result[i] = new object?[fields.Length];

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
                            result[j++][i] = PeriodKinds.ToString(row.Kind);
                        break;
                    case "status":
                        foreach (var row in rows)
                            result[j++][i] = PeriodStatuses.ToString(row.Status);
                        break;
                    case "dictator":
                        foreach (var row in rows)
                            result[j++][i] = PeriodDictatorStatuses.ToString(row.Dictator);
                        break;
                    case "totalBakers":
                        foreach (var row in rows)
                            result[j++][i] = row.TotalBakers;
                        break;
                    case "totalVotingPower":
                        foreach (var row in rows)
                            result[j++][i] = row.TotalVotingPower;
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
                    case "topVotingPower":
                        foreach (var row in rows)
                            result[j++][i] = row.TopVotingPower;
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
                    case "yayVotingPower":
                        foreach (var row in rows)
                            result[j++][i] = row.YayVotingPower;
                        break;
                    case "nayBallots":
                        foreach (var row in rows)
                            result[j++][i] = row.NayBallots;
                        break;
                    case "nayVotingPower":
                        foreach (var row in rows)
                            result[j++][i] = row.NayVotingPower;
                        break;
                    case "passBallots":
                        foreach (var row in rows)
                            result[j++][i] = row.PassBallots;
                        break;
                    case "passVotingPower":
                        foreach (var row in rows)
                            result[j++][i] = row.PassVotingPower;
                        break;
                }
            }

            return result;
        }

        public async Task<object?[]> GetPeriods(
            Int32Parameter? firstLevel,
            Int32Parameter? lastLevel,
            Int32Parameter? epoch,
            SortParameter? sort,
            OffsetParameter? offset,
            int limit,
            string field)
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
                case "dictator": columns.Add(@"""Dictator"""); break;
                case "totalBakers": columns.Add(@"""TotalBakers"""); break;
                case "totalVotingPower": columns.Add(@"""TotalVotingPower"""); break;
                case "upvotesQuorum": columns.Add(@"""UpvotesQuorum"""); break;
                case "proposalsCount": columns.Add(@"""ProposalsCount"""); break;
                case "topUpvotes": columns.Add(@"""TopUpvotes"""); break;
                case "topVotingPower": columns.Add(@"""TopVotingPower"""); break;
                case "ballotsQuorum": columns.Add(@"""BallotsQuorum"""); break;
                case "supermajority": columns.Add(@"""Supermajority"""); break;
                case "yayBallots": columns.Add(@"""YayBallots"""); break;
                case "yayVotingPower": columns.Add(@"""YayVotingPower"""); break;
                case "nayBallots": columns.Add(@"""NayBallots"""); break;
                case "nayVotingPower": columns.Add(@"""NayVotingPower"""); break;
                case "passBallots": columns.Add(@"""PassBallots"""); break;
                case "passVotingPower": columns.Add(@"""PassVotingPower"""); break;
            }

            if (columns.Count == 0)
                return [];

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""VotingPeriods""")
                .Filter("FirstLevel", firstLevel)
                .Filter("LastLevel", lastLevel)
                .Filter("Epoch", epoch)
                .Take(sort, offset, limit, x => ("Id", "Id"));

            await using var db = await DataSource.OpenConnectionAsync();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            //TODO: optimize memory allocation
            var result = new object?[rows.Count()];
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
                        result[j++] = PeriodKinds.ToString(row.Kind);
                    break;
                case "status":
                    foreach (var row in rows)
                        result[j++] = PeriodStatuses.ToString(row.Status);
                    break;
                case "dictator":
                    foreach (var row in rows)
                        result[j++] = PeriodDictatorStatuses.ToString(row.Dictator);
                    break;
                case "totalBakers":
                    foreach (var row in rows)
                        result[j++] = row.TotalBakers;
                    break;
                case "totalVotingPower":
                    foreach (var row in rows)
                        result[j++] = row.TotalVotingPower;
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
                case "topVotingPower":
                    foreach (var row in rows)
                        result[j++] = row.TopVotingPower;
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
                case "yayVotingPower":
                    foreach (var row in rows)
                        result[j++] = row.YayVotingPower;
                    break;
                case "nayBallots":
                    foreach (var row in rows)
                        result[j++] = row.NayBallots;
                    break;
                case "nayVotingPower":
                    foreach (var row in rows)
                        result[j++] = row.NayVotingPower;
                    break;
                case "passBallots":
                    foreach (var row in rows)
                        result[j++] = row.PassBallots;
                    break;
                case "passVotingPower":
                    foreach (var row in rows)
                        result[j++] = row.PassVotingPower;
                    break;
            }

            return result;
        }

        public async Task<VoterSnapshot?> GetVoter(int period, string address)
        {
            var rawAccount = await Accounts.GetAsync(address);
            if (rawAccount is not RawDelegate baker) return null;

            var sql = $@"
                SELECT  *
                FROM    ""VotingSnapshots""
                WHERE   ""Period"" = {period}
                AND     ""BakerId"" = {baker.Id}
                LIMIT   1";

            await using var db = await DataSource.OpenConnectionAsync();
            var row = await db.QueryFirstOrDefaultAsync(sql);
            if (row == null) return null;

            return new VoterSnapshot
            {
                Delegate = Accounts.GetAlias(row.BakerId),
                VotingPower = row.VotingPower,
                Status = VoterStatuses.ToString(row.Status)
            };
        }

        public async Task<IEnumerable<VoterSnapshot>> GetVoters(
            int period,
            VoterStatusParameter? status,
            SortParameter? sort,
            OffsetParameter? offset,
            int limit)
        {
            var sql = new SqlBuilder($@"SELECT * FROM ""VotingSnapshots""")
                .Filter("Period", period)
                .Filter("Status", status)
                .Take(sort, offset, limit, x => x == "votingPower" ? ("VotingPower", "VotingPower") : ("Id", "Id"));

            await using var db = await DataSource.OpenConnectionAsync();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            return rows.Select(row => new VoterSnapshot
            {
                Delegate = Accounts.GetAlias(row.BakerId),
                VotingPower = row.VotingPower,
                Status = VoterStatuses.ToString(row.Status)
            });
        }
        #endregion

        #region epochs
        public async Task<VotingEpoch?> GetEpoch(int index)
        {
            var sql = $@"
                SELECT  *
                FROM     ""VotingPeriods""
                WHERE    ""Epoch"" = {index}
                ORDER BY ""Index""";

            await using var db = await DataSource.OpenConnectionAsync();
            var rows = await db.QueryAsync(sql);
            if (!rows.Any()) return null;

            var proposals = await GetProposals(
                hash: null,
                new Int32Parameter { Eq = index },
                new SortParameter { Desc = "votingPower" },
                null, 1000);

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
                    Kind = PeriodKinds.ToString(row.Kind),
                    Status = PeriodStatuses.ToString(row.Status),
                    Dictator = PeriodDictatorStatuses.ToString(row.Dictator),
                    TotalBakers = row.TotalBakers,
                    TotalVotingPower = row.TotalVotingPower,
                    UpvotesQuorum = row.UpvotesQuorum == null ? null : row.UpvotesQuorum / 100.0,
                    ProposalsCount = row.ProposalsCount,
                    TopUpvotes = row.TopUpvotes,
                    TopVotingPower = row.TopVotingPower,
                    BallotsQuorum = row.BallotsQuorum == null ? null : row.BallotsQuorum / 100.0,
                    Supermajority = row.Supermajority == null ? null : row.Supermajority / 100.0,
                    YayBallots = row.YayBallots,
                    YayVotingPower = row.YayVotingPower,
                    NayBallots = row.NayBallots,
                    NayVotingPower = row.NayVotingPower,
                    PassBallots = row.PassBallots,
                    PassVotingPower = row.PassVotingPower
                }),
                Proposals = proposals
            };
        }

        public async Task<IEnumerable<VotingEpoch>> GetEpochs(EpochStatusParameter? status, SortParameter? sort, OffsetParameter? offset, int limit)
        {
            var sql = new SqlBuilder($"""
                WITH
                groups AS (
                	SELECT "Epoch", MIN("Index") AS "FirstPeriod", MAX("Index") AS "LastPeriod"
                	FROM "VotingPeriods"
                	GROUP BY "Epoch"
                ),
                epochs AS (
                	SELECT
                		g."Epoch" as "Id",
                		g."Epoch" as "Index",
                		fp."FirstLevel",
                		lp."LastLevel",
                		CASE
                			WHEN fp."Status" = {(int)Data.Models.PeriodStatus.NoProposals} THEN '{EpochStatuses.NoProposals}'
                			WHEN lp."Status" = {(int)Data.Models.PeriodStatus.Active} THEN '{EpochStatuses.Voting}'
                			WHEN lp."Status" = {(int)Data.Models.PeriodStatus.Success} THEN '{EpochStatuses.Completed}'
                			ELSE '{EpochStatuses.Failed}'
                		END as "Status"
                	FROM groups as g
                	INNER JOIN "VotingPeriods" as fp ON fp."Index" = g."FirstPeriod"
                	INNER JOIN "VotingPeriods" as lp ON lp."Index" = g."LastPeriod"
                )
                SELECT *
                FROM epochs
                """)
                .FilterA(@"""Status""", status)
                .Take(sort, offset, limit, x => ("Index", "Index"));

            await using var db = await DataSource.OpenConnectionAsync();
            var rows = await db.QueryAsync(sql.Query, sql.Params);
            if (!rows.Any()) return [];

            var epochs = rows.Select(x => (int)x.Index).ToList();

            var periods = (await GetPeriods(
                firstLevel: null,
                lastLevel: null,
                epoch: new Int32Parameter { In = epochs },
                sort: new SortParameter { Asc = "id" },
                offset: null,
                limit: limit * 10))
                .GroupBy(x => x.Epoch)
                .ToDictionary(k => k.Key, v => v.ToList());

            var proposals = (await GetProposals(
                hash: null,
                epoch: new Int32Parameter { In = epochs },
                sort: new SortParameter { Desc = "votingPower" },
                offset: null,
                limit: limit * 10))
                .GroupBy(x => x.Epoch)
                .ToDictionary(k => k.Key, v => v.ToList());

            return rows.Select(row =>
            {
                return new VotingEpoch
                {
                    Index = row.Index,
                    FirstLevel = row.FirstLevel,
                    StartTime = Time[row.FirstLevel],
                    LastLevel = row.LastLevel,
                    EndTime = Time[row.LastLevel],
                    Status = row.Status,
                    Periods = periods.GetValueOrDefault((int)row.Index) ?? [],
                    Proposals = proposals.GetValueOrDefault((int)row.Index) ?? []
                };
            });
        }

        public async Task<VotingEpoch?> GetLatestVoting()
        {
            var sql = $@"
                SELECT  period.*
                FROM    ""VotingPeriods"" as p
                INNER JOIN ""VotingPeriods"" as period on period.""Epoch"" = p.""Epoch""
                WHERE   p.""ProposalsCount"" IS NOT NULL
                AND     p.""ProposalsCount"" > 0
                ORDER BY p.""Index"" DESC
                LIMIT 5";

            await using var db = await DataSource.OpenConnectionAsync();
            var rows = await db.QueryAsync(sql);
            if (!rows.Any()) return null;

            var epoch = rows.First().Epoch;
            rows = rows.Where(x => x.Epoch == epoch).OrderBy(x => x.Index);

            var proposals = await GetProposals(
                hash: null,
                new Int32Parameter { Eq = epoch },
                new SortParameter { Desc = "votingPower" },
                null, 10);

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
                    Kind = PeriodKinds.ToString(row.Kind),
                    Status = PeriodStatuses.ToString(row.Status),
                    Dictator = PeriodDictatorStatuses.ToString(row.Dictator),
                    TotalBakers = row.TotalBakers,
                    TotalVotingPower = row.TotalVotingPower,
                    UpvotesQuorum = row.UpvotesQuorum == null ? null : row.UpvotesQuorum / 100.0,
                    ProposalsCount = row.ProposalsCount,
                    TopUpvotes = row.TopUpvotes,
                    TopVotingPower = row.TopVotingPower,
                    BallotsQuorum = row.BallotsQuorum == null ? null : row.BallotsQuorum / 100.0,
                    Supermajority = row.Supermajority == null ? null : row.Supermajority / 100.0,
                    YayBallots = row.YayBallots,
                    YayVotingPower = row.YayVotingPower,
                    NayBallots = row.NayBallots,
                    NayVotingPower = row.NayVotingPower,
                    PassBallots = row.PassBallots,
                    PassVotingPower = row.PassVotingPower
                }),
                Proposals = proposals
            };
        }

        string GetEpochStatus(IEnumerable<dynamic> periods)
        {
            if (periods.First().Status == (int)Data.Models.PeriodStatus.NoProposals)
                return EpochStatuses.NoProposals;

            if (periods.Last().Status == (int)Data.Models.PeriodStatus.Active)
                return EpochStatuses.Voting;

            if (periods.Last().Status == (int)Data.Models.PeriodStatus.Success &&
                periods.Last().Epoch != State.Current.VotingEpoch)
                return EpochStatuses.Completed;

            return EpochStatuses.Failed;
        }
        #endregion
    }
}
