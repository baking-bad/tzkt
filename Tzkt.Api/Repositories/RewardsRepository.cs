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
    public class RewardsRepository : DbConnection
    {
        readonly AccountsCache Accounts;

        public RewardsRepository(AccountsCache accounts, IConfiguration config) : base(config)
        {
            Accounts = accounts;
        }

        public async Task<int> GetBakerRewardsCount(string address)
        {
            if (!(await Accounts.GetAsync(address) is RawDelegate baker))
                return 0;

            using var db = GetConnection();
            return await db.QueryFirstAsync<int>($@"SELECT COUNT(*) FROM ""BakerCycles"" WHERE ""BakerId"" = {baker.Id}");
        }

        public async Task<BakerRewards> GetBakerRewards(string address, int cycle)
        {
            if (!(await Accounts.GetAsync(address) is RawDelegate baker))
                return null;

            var sql = $@"
                SELECT  *
                FROM    ""BakerCycles""
                WHERE   ""BakerId"" = {baker.Id}
                AND     ""Cycle"" = {cycle}
                LIMIT   1";

            using var db = GetConnection();
            var row = await db.QueryFirstOrDefaultAsync(sql);
            if (row == null) return null;

            return new BakerRewards
            {
                AccusationLostDeposits = row.AccusationLostDeposits,
                AccusationLostFees = row.AccusationLostFees,
                AccusationLostRewards = row.AccusationLostRewards,
                AccusationRewards = row.AccusationRewards,
                BlockDeposits = row.BlockDeposits,
                Cycle = row.Cycle,
                DelegatedBalance = row.DelegatedBalance,
                EndorsementDeposits = row.EndorsementDeposits,
                EndorsementRewards = row.EndorsementRewards,
                Endorsements = row.Endorsements,
                ExpectedBlocks = Math.Round(row.ExpectedBlocks, 2),
                ExpectedEndorsements = Math.Round(row.ExpectedEndorsements, 2),
                ExtraBlockFees = row.ExtraBlockFees,
                ExtraBlockRewards = row.ExtraBlockRewards,
                ExtraBlocks = row.ExtraBlocks,
                FutureBlockDeposits = row.FutureBlockDeposits,
                FutureBlockRewards = row.FutureBlockRewards,
                FutureBlocks = row.FutureBlocks,
                FutureEndorsementDeposits = row.FutureEndorsementDeposits,
                FutureEndorsementRewards = row.FutureEndorsementRewards,
                FutureEndorsements = row.FutureEndorsements,
                MissedEndorsementRewards = row.MissedEndorsementRewards,
                MissedEndorsements = row.MissedEndorsements,
                MissedExtraBlockFees = row.MissedExtraBlockFees,
                MissedExtraBlockRewards = row.MissedExtraBlockRewards,
                MissedExtraBlocks = row.MissedExtraBlocks,
                MissedOwnBlockFees = row.MissedOwnBlockFees,
                MissedOwnBlockRewards = row.MissedOwnBlockRewards,
                MissedOwnBlocks = row.MissedOwnBlocks,
                NumDelegators = row.DelegatorsCount,
                OwnBlockFees = row.OwnBlockFees,
                OwnBlockRewards = row.OwnBlockRewards,
                OwnBlocks = row.OwnBlocks,
                RevelationLostFees = row.RevelationLostFees,
                RevelationLostRewards = row.RevelationLostRewards,
                RevelationRewards = row.RevelationRewards,
                StakingBalance = row.StakingBalance,
                UncoveredEndorsementRewards = row.UncoveredEndorsementRewards,
                UncoveredEndorsements = row.UncoveredEndorsements,
                UncoveredExtraBlockFees = row.UncoveredExtraBlockFees,
                UncoveredExtraBlockRewards = row.UncoveredExtraBlockRewards,
                UncoveredExtraBlocks = row.UncoveredExtraBlocks,
                UncoveredOwnBlockFees = row.UncoveredOwnBlockFees,
                UncoveredOwnBlockRewards = row.UncoveredOwnBlockRewards,
                UncoveredOwnBlocks = row.UncoveredOwnBlocks
            };
        }

        public async Task<IEnumerable<BakerRewards>> GetBakerRewards(string address, Int32Parameter cycle, SortParameter sort, OffsetParameter offset, int limit)
        {
            if (!(await Accounts.GetAsync(address) is RawDelegate baker))
                return null;

            var sql = new SqlBuilder(@"SELECT * FROM ""BakerCycles""")
                .Filter("BakerId", baker.Id)
                .Filter("Cycle", cycle)
                .Take(sort ?? new SortParameter { Desc = "cycle" }, offset, limit, x => "Cycle");

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            return rows.Select(row => new BakerRewards
            {
                AccusationLostDeposits = row.AccusationLostDeposits,
                AccusationLostFees = row.AccusationLostFees,
                AccusationLostRewards = row.AccusationLostRewards,
                AccusationRewards = row.AccusationRewards,
                BlockDeposits = row.BlockDeposits,
                Cycle = row.Cycle,
                DelegatedBalance = row.DelegatedBalance,
                EndorsementDeposits = row.EndorsementDeposits,
                EndorsementRewards = row.EndorsementRewards,
                Endorsements = row.Endorsements,
                ExpectedBlocks = Math.Round(row.ExpectedBlocks, 2),
                ExpectedEndorsements = Math.Round(row.ExpectedEndorsements, 2),
                ExtraBlockFees = row.ExtraBlockFees,
                ExtraBlockRewards = row.ExtraBlockRewards,
                ExtraBlocks = row.ExtraBlocks,
                FutureBlockDeposits = row.FutureBlockDeposits,
                FutureBlockRewards = row.FutureBlockRewards,
                FutureBlocks = row.FutureBlocks,
                FutureEndorsementDeposits = row.FutureEndorsementDeposits,
                FutureEndorsementRewards = row.FutureEndorsementRewards,
                FutureEndorsements = row.FutureEndorsements,
                MissedEndorsementRewards = row.MissedEndorsementRewards,
                MissedEndorsements = row.MissedEndorsements,
                MissedExtraBlockFees = row.MissedExtraBlockFees,
                MissedExtraBlockRewards = row.MissedExtraBlockRewards,
                MissedExtraBlocks = row.MissedExtraBlocks,
                MissedOwnBlockFees = row.MissedOwnBlockFees,
                MissedOwnBlockRewards = row.MissedOwnBlockRewards,
                MissedOwnBlocks = row.MissedOwnBlocks,
                NumDelegators = row.DelegatorsCount,
                OwnBlockFees = row.OwnBlockFees,
                OwnBlockRewards = row.OwnBlockRewards,
                OwnBlocks = row.OwnBlocks,
                RevelationLostFees = row.RevelationLostFees,
                RevelationLostRewards = row.RevelationLostRewards,
                RevelationRewards = row.RevelationRewards,
                StakingBalance = row.StakingBalance,
                UncoveredEndorsementRewards = row.UncoveredEndorsementRewards,
                UncoveredEndorsements = row.UncoveredEndorsements,
                UncoveredExtraBlockFees = row.UncoveredExtraBlockFees,
                UncoveredExtraBlockRewards = row.UncoveredExtraBlockRewards,
                UncoveredExtraBlocks = row.UncoveredExtraBlocks,
                UncoveredOwnBlockFees = row.UncoveredOwnBlockFees,
                UncoveredOwnBlockRewards = row.UncoveredOwnBlockRewards,
                UncoveredOwnBlocks = row.UncoveredOwnBlocks
            });
        }

        public async Task<object[][]> GetBakerRewards(string address, Int32Parameter cycle, SortParameter sort, OffsetParameter offset, int limit, string[] fields)
        {
            if (!(await Accounts.GetAsync(address) is RawDelegate baker))
                return null;

            var columns = new HashSet<string>(fields.Length);
            foreach (var field in fields)
            {
                switch (field)
                {
                    case "accusationLostDeposits": columns.Add(@"""AccusationLostDeposits"""); break;
                    case "accusationLostFees": columns.Add(@"""AccusationLostFees"""); break;
                    case "accusationLostRewards": columns.Add(@"""AccusationLostRewards"""); break;
                    case "accusationRewards": columns.Add(@"""AccusationRewards"""); break;
                    case "blockDeposits": columns.Add(@"""BlockDeposits"""); break;
                    case "cycle": columns.Add(@"""Cycle"""); break;
                    case "delegatedBalance": columns.Add(@"""DelegatedBalance"""); break;
                    case "endorsementDeposits": columns.Add(@"""EndorsementDeposits"""); break;
                    case "endorsementRewards": columns.Add(@"""EndorsementRewards"""); break;
                    case "endorsements": columns.Add(@"""Endorsements"""); break;
                    case "expectedBlocks": columns.Add(@"""ExpectedBlocks"""); break;
                    case "expectedEndorsements": columns.Add(@"""ExpectedEndorsements"""); break;
                    case "extraBlockFees": columns.Add(@"""ExtraBlockFees"""); break;
                    case "extraBlockRewards": columns.Add(@"""ExtraBlockRewards"""); break;
                    case "extraBlocks": columns.Add(@"""ExtraBlocks"""); break;
                    case "futureBlockDeposits": columns.Add(@"""FutureBlockDeposits"""); break;
                    case "futureBlockRewards": columns.Add(@"""FutureBlockRewards"""); break;
                    case "futureBlocks": columns.Add(@"""FutureBlocks"""); break;
                    case "futureEndorsementDeposits": columns.Add(@"""FutureEndorsementDeposits"""); break;
                    case "futureEndorsementRewards": columns.Add(@"""FutureEndorsementRewards"""); break;
                    case "futureEndorsements": columns.Add(@"""FutureEndorsements"""); break;
                    case "missedEndorsementRewards": columns.Add(@"""MissedEndorsementRewards"""); break;
                    case "missedEndorsements": columns.Add(@"""MissedEndorsements"""); break;
                    case "missedExtraBlockFees": columns.Add(@"""MissedExtraBlockFees"""); break;
                    case "missedExtraBlockRewards": columns.Add(@"""MissedExtraBlockRewards"""); break;
                    case "missedExtraBlocks": columns.Add(@"""MissedExtraBlocks"""); break;
                    case "missedOwnBlockFees": columns.Add(@"""MissedOwnBlockFees"""); break;
                    case "missedOwnBlockRewards": columns.Add(@"""MissedOwnBlockRewards"""); break;
                    case "missedOwnBlocks": columns.Add(@"""MissedOwnBlocks"""); break;
                    case "numDelegators": columns.Add(@"""DelegatorsCount"""); break;
                    case "ownBlockFees": columns.Add(@"""OwnBlockFees"""); break;
                    case "ownBlockRewards": columns.Add(@"""OwnBlockRewards"""); break;
                    case "ownBlocks": columns.Add(@"""OwnBlocks"""); break;
                    case "revelationLostFees": columns.Add(@"""RevelationLostFees"""); break;
                    case "revelationLostRewards": columns.Add(@"""RevelationLostRewards"""); break;
                    case "revelationRewards": columns.Add(@"""RevelationRewards"""); break;
                    case "stakingBalance": columns.Add(@"""StakingBalance"""); break;
                    case "uncoveredEndorsementRewards": columns.Add(@"""UncoveredEndorsementRewards"""); break;
                    case "uncoveredEndorsements": columns.Add(@"""UncoveredEndorsements"""); break;
                    case "uncoveredExtraBlockFees": columns.Add(@"""UncoveredExtraBlockFees"""); break;
                    case "uncoveredExtraBlockRewards": columns.Add(@"""UncoveredExtraBlockRewards"""); break;
                    case "uncoveredExtraBlocks": columns.Add(@"""UncoveredExtraBlocks"""); break;
                    case "uncoveredOwnBlockFees": columns.Add(@"""UncoveredOwnBlockFees"""); break;
                    case "uncoveredOwnBlockRewards": columns.Add(@"""UncoveredOwnBlockRewards"""); break;
                    case "uncoveredOwnBlocks": columns.Add(@"""UncoveredOwnBlocks"""); break;
                }
            }

            if (columns.Count == 0)
                return Array.Empty<object[]>();

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""BakerCycles""")
                .Filter("BakerId", baker.Id)
                .Filter("Cycle", cycle)
                .Take(sort ?? new SortParameter { Desc = "cycle" }, offset, limit, x => "Cycle");

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            var result = new object[rows.Count()][];
            for (int i = 0; i < result.Length; i++)
                result[i] = new object[fields.Length];

            for (int i = 0, j = 0; i < fields.Length; j = 0, i++)
            {
                switch (fields[i])
                {
                    case "accusationLostDeposits":
                        foreach (var row in rows)
                            result[j++][i] = row.AccusationLostDeposits;
                        break;
                    case "accusationLostFees":
                        foreach (var row in rows)
                            result[j++][i] = row.AccusationLostFees;
                        break;
                    case "accusationLostRewards":
                        foreach (var row in rows)
                            result[j++][i] = row.AccusationLostRewards;
                        break;
                    case "accusationRewards":
                        foreach (var row in rows)
                            result[j++][i] = row.AccusationRewards;
                        break;
                    case "blockDeposits":
                        foreach (var row in rows)
                            result[j++][i] = row.BlockDeposits;
                        break;
                    case "cycle":
                        foreach (var row in rows)
                            result[j++][i] = row.Cycle;
                        break;
                    case "delegatedBalance":
                        foreach (var row in rows)
                            result[j++][i] = row.DelegatedBalance;
                        break;
                    case "endorsementDeposits":
                        foreach (var row in rows)
                            result[j++][i] = row.EndorsementDeposits;
                        break;
                    case "endorsementRewards":
                        foreach (var row in rows)
                            result[j++][i] = row.EndorsementRewards;
                        break;
                    case "endorsements":
                        foreach (var row in rows)
                            result[j++][i] = row.Endorsements;
                        break;
                    case "expectedBlocks":
                        foreach (var row in rows)
                            result[j++][i] = Math.Round(row.ExpectedBlocks, 2);
                        break;
                    case "expectedEndorsements":
                        foreach (var row in rows)
                            result[j++][i] = Math.Round(row.ExpectedEndorsements, 2);
                        break;
                    case "extraBlockFees":
                        foreach (var row in rows)
                            result[j++][i] = row.ExtraBlockFees;
                        break;
                    case "extraBlockRewards":
                        foreach (var row in rows)
                            result[j++][i] = row.ExtraBlockRewards;
                        break;
                    case "extraBlocks":
                        foreach (var row in rows)
                            result[j++][i] = row.ExtraBlocks;
                        break;
                    case "futureBlockDeposits":
                        foreach (var row in rows)
                            result[j++][i] = row.FutureBlockDeposits;
                        break;
                    case "futureBlockRewards":
                        foreach (var row in rows)
                            result[j++][i] = row.FutureBlockRewards;
                        break;
                    case "futureBlocks":
                        foreach (var row in rows)
                            result[j++][i] = row.FutureBlocks;
                        break;
                    case "futureEndorsementDeposits":
                        foreach (var row in rows)
                            result[j++][i] = row.FutureEndorsementDeposits;
                        break;
                    case "futureEndorsementRewards":
                        foreach (var row in rows)
                            result[j++][i] = row.FutureEndorsementRewards;
                        break;
                    case "futureEndorsements":
                        foreach (var row in rows)
                            result[j++][i] = row.FutureEndorsements;
                        break;
                    case "missedEndorsementRewards":
                        foreach (var row in rows)
                            result[j++][i] = row.MissedEndorsementRewards;
                        break;
                    case "missedEndorsements":
                        foreach (var row in rows)
                            result[j++][i] = row.MissedEndorsements;
                        break;
                    case "missedExtraBlockFees":
                        foreach (var row in rows)
                            result[j++][i] = row.MissedExtraBlockFees;
                        break;
                    case "missedExtraBlockRewards":
                        foreach (var row in rows)
                            result[j++][i] = row.MissedExtraBlockRewards;
                        break;
                    case "missedExtraBlocks":
                        foreach (var row in rows)
                            result[j++][i] = row.MissedExtraBlocks;
                        break;
                    case "missedOwnBlockFees":
                        foreach (var row in rows)
                            result[j++][i] = row.MissedOwnBlockFees;
                        break;
                    case "missedOwnBlockRewards":
                        foreach (var row in rows)
                            result[j++][i] = row.MissedOwnBlockRewards;
                        break;
                    case "missedOwnBlocks":
                        foreach (var row in rows)
                            result[j++][i] = row.MissedOwnBlocks;
                        break;
                    case "numDelegators":
                        foreach (var row in rows)
                            result[j++][i] = row.DelegatorsCount;
                        break;
                    case "ownBlockFees":
                        foreach (var row in rows)
                            result[j++][i] = row.OwnBlockFees;
                        break;
                    case "ownBlockRewards":
                        foreach (var row in rows)
                            result[j++][i] = row.OwnBlockRewards;
                        break;
                    case "ownBlocks":
                        foreach (var row in rows)
                            result[j++][i] = row.OwnBlocks;
                        break;
                    case "revelationLostFees":
                        foreach (var row in rows)
                            result[j++][i] = row.RevelationLostFees;
                        break;
                    case "revelationLostRewards":
                        foreach (var row in rows)
                            result[j++][i] = row.RevelationLostRewards;
                        break;
                    case "revelationRewards":
                        foreach (var row in rows)
                            result[j++][i] = row.RevelationRewards;
                        break;
                    case "stakingBalance":
                        foreach (var row in rows)
                            result[j++][i] = row.StakingBalance;
                        break;
                    case "uncoveredEndorsementRewards":
                        foreach (var row in rows)
                            result[j++][i] = row.UncoveredEndorsementRewards;
                        break;
                    case "uncoveredEndorsements":
                        foreach (var row in rows)
                            result[j++][i] = row.UncoveredEndorsements;
                        break;
                    case "uncoveredExtraBlockFees":
                        foreach (var row in rows)
                            result[j++][i] = row.UncoveredExtraBlockFees;
                        break;
                    case "uncoveredExtraBlockRewards":
                        foreach (var row in rows)
                            result[j++][i] = row.UncoveredExtraBlockRewards;
                        break;
                    case "uncoveredExtraBlocks":
                        foreach (var row in rows)
                            result[j++][i] = row.UncoveredExtraBlocks;
                        break;
                    case "uncoveredOwnBlockFees":
                        foreach (var row in rows)
                            result[j++][i] = row.UncoveredOwnBlockFees;
                        break;
                    case "uncoveredOwnBlockRewards":
                        foreach (var row in rows)
                            result[j++][i] = row.UncoveredOwnBlockRewards;
                        break;
                    case "uncoveredOwnBlocks":
                        foreach (var row in rows)
                            result[j++][i] = row.UncoveredOwnBlocks;
                        break;
                }
            }

            return result;
        }

        public async Task<object[]> GetBakerRewards(string address, Int32Parameter cycle, SortParameter sort, OffsetParameter offset, int limit, string field)
        {
            if (!(await Accounts.GetAsync(address) is RawDelegate baker))
                return null;

            var columns = new HashSet<string>(1);
            switch (field)
            {
                case "accusationLostDeposits": columns.Add(@"""AccusationLostDeposits"""); break;
                case "accusationLostFees": columns.Add(@"""AccusationLostFees"""); break;
                case "accusationLostRewards": columns.Add(@"""AccusationLostRewards"""); break;
                case "accusationRewards": columns.Add(@"""AccusationRewards"""); break;
                case "blockDeposits": columns.Add(@"""BlockDeposits"""); break;
                case "cycle": columns.Add(@"""Cycle"""); break;
                case "delegatedBalance": columns.Add(@"""DelegatedBalance"""); break;
                case "endorsementDeposits": columns.Add(@"""EndorsementDeposits"""); break;
                case "endorsementRewards": columns.Add(@"""EndorsementRewards"""); break;
                case "endorsements": columns.Add(@"""Endorsements"""); break;
                case "expectedBlocks": columns.Add(@"""ExpectedBlocks"""); break;
                case "expectedEndorsements": columns.Add(@"""ExpectedEndorsements"""); break;
                case "extraBlockFees": columns.Add(@"""ExtraBlockFees"""); break;
                case "extraBlockRewards": columns.Add(@"""ExtraBlockRewards"""); break;
                case "extraBlocks": columns.Add(@"""ExtraBlocks"""); break;
                case "futureBlockDeposits": columns.Add(@"""FutureBlockDeposits"""); break;
                case "futureBlockRewards": columns.Add(@"""FutureBlockRewards"""); break;
                case "futureBlocks": columns.Add(@"""FutureBlocks"""); break;
                case "futureEndorsementDeposits": columns.Add(@"""FutureEndorsementDeposits"""); break;
                case "futureEndorsementRewards": columns.Add(@"""FutureEndorsementRewards"""); break;
                case "futureEndorsements": columns.Add(@"""FutureEndorsements"""); break;
                case "missedEndorsementRewards": columns.Add(@"""MissedEndorsementRewards"""); break;
                case "missedEndorsements": columns.Add(@"""MissedEndorsements"""); break;
                case "missedExtraBlockFees": columns.Add(@"""MissedExtraBlockFees"""); break;
                case "missedExtraBlockRewards": columns.Add(@"""MissedExtraBlockRewards"""); break;
                case "missedExtraBlocks": columns.Add(@"""MissedExtraBlocks"""); break;
                case "missedOwnBlockFees": columns.Add(@"""MissedOwnBlockFees"""); break;
                case "missedOwnBlockRewards": columns.Add(@"""MissedOwnBlockRewards"""); break;
                case "missedOwnBlocks": columns.Add(@"""MissedOwnBlocks"""); break;
                case "numDelegators": columns.Add(@"""DelegatorsCount"""); break;
                case "ownBlockFees": columns.Add(@"""OwnBlockFees"""); break;
                case "ownBlockRewards": columns.Add(@"""OwnBlockRewards"""); break;
                case "ownBlocks": columns.Add(@"""OwnBlocks"""); break;
                case "revelationLostFees": columns.Add(@"""RevelationLostFees"""); break;
                case "revelationLostRewards": columns.Add(@"""RevelationLostRewards"""); break;
                case "revelationRewards": columns.Add(@"""RevelationRewards"""); break;
                case "stakingBalance": columns.Add(@"""StakingBalance"""); break;
                case "uncoveredEndorsementRewards": columns.Add(@"""UncoveredEndorsementRewards"""); break;
                case "uncoveredEndorsements": columns.Add(@"""UncoveredEndorsements"""); break;
                case "uncoveredExtraBlockFees": columns.Add(@"""UncoveredExtraBlockFees"""); break;
                case "uncoveredExtraBlockRewards": columns.Add(@"""UncoveredExtraBlockRewards"""); break;
                case "uncoveredExtraBlocks": columns.Add(@"""UncoveredExtraBlocks"""); break;
                case "uncoveredOwnBlockFees": columns.Add(@"""UncoveredOwnBlockFees"""); break;
                case "uncoveredOwnBlockRewards": columns.Add(@"""UncoveredOwnBlockRewards"""); break;
                case "uncoveredOwnBlocks": columns.Add(@"""UncoveredOwnBlocks"""); break;
            }

            if (columns.Count == 0)
                return Array.Empty<object>();

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""BakerCycles""")
                .Filter("BakerId", baker.Id)
                .Filter("Cycle", cycle)
                .Take(sort ?? new SortParameter { Desc = "cycle" }, offset, limit, x => "Cycle");

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            //TODO: optimize memory allocation
            var result = new object[rows.Count()];
            var j = 0;

            switch (field)
            {
                case "accusationLostDeposits":
                    foreach (var row in rows)
                        result[j++] = row.AccusationLostDeposits;
                    break;
                case "accusationLostFees":
                    foreach (var row in rows)
                        result[j++] = row.AccusationLostFees;
                    break;
                case "accusationLostRewards":
                    foreach (var row in rows)
                        result[j++] = row.AccusationLostRewards;
                    break;
                case "accusationRewards":
                    foreach (var row in rows)
                        result[j++] = row.AccusationRewards;
                    break;
                case "blockDeposits":
                    foreach (var row in rows)
                        result[j++] = row.BlockDeposits;
                    break;
                case "cycle":
                    foreach (var row in rows)
                        result[j++] = row.Cycle;
                    break;
                case "delegatedBalance":
                    foreach (var row in rows)
                        result[j++] = row.DelegatedBalance;
                    break;
                case "endorsementDeposits":
                    foreach (var row in rows)
                        result[j++] = row.EndorsementDeposits;
                    break;
                case "endorsementRewards":
                    foreach (var row in rows)
                        result[j++] = row.EndorsementRewards;
                    break;
                case "endorsements":
                    foreach (var row in rows)
                        result[j++] = row.Endorsements;
                    break;
                case "expectedBlocks":
                    foreach (var row in rows)
                        result[j++] = Math.Round(row.ExpectedBlocks, 2);
                    break;
                case "expectedEndorsements":
                    foreach (var row in rows)
                        result[j++] = Math.Round(row.ExpectedEndorsements, 2);
                    break;
                case "extraBlockFees":
                    foreach (var row in rows)
                        result[j++] = row.ExtraBlockFees;
                    break;
                case "extraBlockRewards":
                    foreach (var row in rows)
                        result[j++] = row.ExtraBlockRewards;
                    break;
                case "extraBlocks":
                    foreach (var row in rows)
                        result[j++] = row.ExtraBlocks;
                    break;
                case "futureBlockDeposits":
                    foreach (var row in rows)
                        result[j++] = row.FutureBlockDeposits;
                    break;
                case "futureBlockRewards":
                    foreach (var row in rows)
                        result[j++] = row.FutureBlockRewards;
                    break;
                case "futureBlocks":
                    foreach (var row in rows)
                        result[j++] = row.FutureBlocks;
                    break;
                case "futureEndorsementDeposits":
                    foreach (var row in rows)
                        result[j++] = row.FutureEndorsementDeposits;
                    break;
                case "futureEndorsementRewards":
                    foreach (var row in rows)
                        result[j++] = row.FutureEndorsementRewards;
                    break;
                case "futureEndorsements":
                    foreach (var row in rows)
                        result[j++] = row.FutureEndorsements;
                    break;
                case "missedEndorsementRewards":
                    foreach (var row in rows)
                        result[j++] = row.MissedEndorsementRewards;
                    break;
                case "missedEndorsements":
                    foreach (var row in rows)
                        result[j++] = row.MissedEndorsements;
                    break;
                case "missedExtraBlockFees":
                    foreach (var row in rows)
                        result[j++] = row.MissedExtraBlockFees;
                    break;
                case "missedExtraBlockRewards":
                    foreach (var row in rows)
                        result[j++] = row.MissedExtraBlockRewards;
                    break;
                case "missedExtraBlocks":
                    foreach (var row in rows)
                        result[j++] = row.MissedExtraBlocks;
                    break;
                case "missedOwnBlockFees":
                    foreach (var row in rows)
                        result[j++] = row.MissedOwnBlockFees;
                    break;
                case "missedOwnBlockRewards":
                    foreach (var row in rows)
                        result[j++] = row.MissedOwnBlockRewards;
                    break;
                case "missedOwnBlocks":
                    foreach (var row in rows)
                        result[j++] = row.MissedOwnBlocks;
                    break;
                case "numDelegators":
                    foreach (var row in rows)
                        result[j++] = row.DelegatorsCount;
                    break;
                case "ownBlockFees":
                    foreach (var row in rows)
                        result[j++] = row.OwnBlockFees;
                    break;
                case "ownBlockRewards":
                    foreach (var row in rows)
                        result[j++] = row.OwnBlockRewards;
                    break;
                case "ownBlocks":
                    foreach (var row in rows)
                        result[j++] = row.OwnBlocks;
                    break;
                case "revelationLostFees":
                    foreach (var row in rows)
                        result[j++] = row.RevelationLostFees;
                    break;
                case "revelationLostRewards":
                    foreach (var row in rows)
                        result[j++] = row.RevelationLostRewards;
                    break;
                case "revelationRewards":
                    foreach (var row in rows)
                        result[j++] = row.RevelationRewards;
                    break;
                case "stakingBalance":
                    foreach (var row in rows)
                        result[j++] = row.StakingBalance;
                    break;
                case "uncoveredEndorsementRewards":
                    foreach (var row in rows)
                        result[j++] = row.UncoveredEndorsementRewards;
                    break;
                case "uncoveredEndorsements":
                    foreach (var row in rows)
                        result[j++] = row.UncoveredEndorsements;
                    break;
                case "uncoveredExtraBlockFees":
                    foreach (var row in rows)
                        result[j++] = row.UncoveredExtraBlockFees;
                    break;
                case "uncoveredExtraBlockRewards":
                    foreach (var row in rows)
                        result[j++] = row.UncoveredExtraBlockRewards;
                    break;
                case "uncoveredExtraBlocks":
                    foreach (var row in rows)
                        result[j++] = row.UncoveredExtraBlocks;
                    break;
                case "uncoveredOwnBlockFees":
                    foreach (var row in rows)
                        result[j++] = row.UncoveredOwnBlockFees;
                    break;
                case "uncoveredOwnBlockRewards":
                    foreach (var row in rows)
                        result[j++] = row.UncoveredOwnBlockRewards;
                    break;
                case "uncoveredOwnBlocks":
                    foreach (var row in rows)
                        result[j++] = row.UncoveredOwnBlocks;
                    break;
            }

            return result;
        }
    }
}
