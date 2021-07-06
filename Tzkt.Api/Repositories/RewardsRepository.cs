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
        readonly ProtocolsCache Protocols;
        readonly QuotesCache Quotes;

        public RewardsRepository(AccountsCache accounts, ProtocolsCache protocols, QuotesCache quotes, IConfiguration config) : base(config)
        {
            Accounts = accounts;
            Protocols = protocols;
            Quotes = quotes;
        }

        #region baker
        public async Task<int> GetBakerRewardsCount(string address)
        {
            if (await Accounts.GetAsync(address) is not RawDelegate baker)
                return 0;

            using var db = GetConnection();
            return await db.QueryFirstAsync<int>($@"SELECT COUNT(*) FROM ""BakerCycles"" WHERE ""BakerId"" = {baker.Id}");
        }

        public async Task<BakerRewards> GetBakerRewards(string address, int cycle, Symbols quote)
        {
            if (await Accounts.GetAsync(address) is not RawDelegate baker)
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
                DoubleBakingRewards = row.DoubleBakingRewards,
                DoubleBakingLostDeposits = row.DoubleBakingLostDeposits,
                DoubleBakingLostRewards = row.DoubleBakingLostRewards,
                DoubleBakingLostFees = row.DoubleBakingLostFees,
                DoubleEndorsingRewards = row.DoubleEndorsingRewards,
                DoubleEndorsingLostDeposits = row.DoubleEndorsingLostDeposits,
                DoubleEndorsingLostRewards = row.DoubleEndorsingLostRewards,
                DoubleEndorsingLostFees = row.DoubleEndorsingLostFees,
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
                UncoveredOwnBlocks = row.UncoveredOwnBlocks,
                Quote = Quotes.Get(quote, Protocols.FindByCycle((int)row.Cycle).GetCycleEnd((int)row.Cycle))
            };
        }

        public async Task<IEnumerable<BakerRewards>> GetBakerRewards(
            string address,
            Int32Parameter cycle,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            Symbols quote)
        {
            if (await Accounts.GetAsync(address) is not RawDelegate baker)
                return null;

            var sql = new SqlBuilder(@"SELECT * FROM ""BakerCycles""")
                .Filter("BakerId", baker.Id)
                .Filter("Cycle", cycle)
                .Take(sort ?? new SortParameter { Desc = "cycle" }, offset, limit, x => ("Cycle", "Cycle"));

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            return rows.Select(row => new BakerRewards
            {
                DoubleBakingRewards = row.DoubleBakingRewards,
                DoubleBakingLostDeposits = row.DoubleBakingLostDeposits,
                DoubleBakingLostRewards = row.DoubleBakingLostRewards,
                DoubleBakingLostFees = row.DoubleBakingLostFees,
                DoubleEndorsingRewards = row.DoubleEndorsingRewards,
                DoubleEndorsingLostDeposits = row.DoubleEndorsingLostDeposits,
                DoubleEndorsingLostRewards = row.DoubleEndorsingLostRewards,
                DoubleEndorsingLostFees = row.DoubleEndorsingLostFees,
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
                UncoveredOwnBlocks = row.UncoveredOwnBlocks,
                Quote = Quotes.Get(quote, Protocols.FindByCycle((int)row.Cycle).GetCycleEnd((int)row.Cycle))
            });
        }

        public async Task<object[][]> GetBakerRewards(
            string address,
            Int32Parameter cycle,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            string[] fields,
            Symbols quote)
        {
            if (await Accounts.GetAsync(address) is not RawDelegate baker)
                return null;

            var columns = new HashSet<string>(fields.Length);
            foreach (var field in fields)
            {
                switch (field)
                {
                    case "doubleBakingRewards": columns.Add(@"""DoubleBakingRewards"""); break;
                    case "doubleBakingLostDeposits": columns.Add(@"""DoubleBakingLostDeposits"""); break;
                    case "doubleBakingLostRewards": columns.Add(@"""DoubleBakingLostRewards"""); break;
                    case "doubleBakingLostFees": columns.Add(@"""DoubleBakingLostFees"""); break;
                    case "doubleEndorsingRewards": columns.Add(@"""DoubleEndorsingRewards"""); break;
                    case "doubleEndorsingLostDeposits": columns.Add(@"""DoubleEndorsingLostDeposits"""); break;
                    case "doubleEndorsingLostRewards": columns.Add(@"""DoubleEndorsingLostRewards"""); break;
                    case "doubleEndorsingLostFees": columns.Add(@"""DoubleEndorsingLostFees"""); break;
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
                    case "quote": columns.Add(@"""Cycle"""); break;
                }
            }

            if (columns.Count == 0)
                return Array.Empty<object[]>();

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""BakerCycles""")
                .Filter("BakerId", baker.Id)
                .Filter("Cycle", cycle)
                .Take(sort ?? new SortParameter { Desc = "cycle" }, offset, limit, x => ("Cycle", "Cycle"));

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            var result = new object[rows.Count()][];
            for (int i = 0; i < result.Length; i++)
                result[i] = new object[fields.Length];

            for (int i = 0, j = 0; i < fields.Length; j = 0, i++)
            {
                switch (fields[i])
                {
                    case "doubleBakingRewards":
                        foreach (var row in rows)
                            result[j++][i] = row.DoubleBakingRewards;
                        break;
                    case "doubleBakingLostDeposits":
                        foreach (var row in rows)
                            result[j++][i] = row.DoubleBakingLostDeposits;
                        break;
                    case "doubleBakingLostRewards":
                        foreach (var row in rows)
                            result[j++][i] = row.DoubleBakingLostRewards;
                        break;
                    case "doubleBakingLostFees":
                        foreach (var row in rows)
                            result[j++][i] = row.DoubleBakingLostFees;
                        break;
                    case "doubleEndorsingRewards":
                        foreach (var row in rows)
                            result[j++][i] = row.DoubleEndorsingRewards;
                        break;
                    case "doubleEndorsingLostDeposits":
                        foreach (var row in rows)
                            result[j++][i] = row.DoubleEndorsingLostDeposits;
                        break;
                    case "doubleEndorsingLostRewards":
                        foreach (var row in rows)
                            result[j++][i] = row.DoubleEndorsingLostRewards;
                        break;
                    case "doubleEndorsingLostFees":
                        foreach (var row in rows)
                            result[j++][i] = row.DoubleEndorsingLostFees;
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
                    case "quote":
                        foreach (var row in rows)
                            result[j++][i] = Quotes.Get(quote, Protocols.FindByCycle((int)row.Cycle).GetCycleEnd((int)row.Cycle));
                        break;
                }
            }

            return result;
        }

        public async Task<object[]> GetBakerRewards(
            string address,
            Int32Parameter cycle,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            string field,
            Symbols quote)
        {
            if (await Accounts.GetAsync(address) is not RawDelegate baker)
                return null;

            var columns = new HashSet<string>(1);
            switch (field)
            {
                case "doubleBakingRewards": columns.Add(@"""DoubleBakingRewards"""); break;
                case "doubleBakingLostDeposits": columns.Add(@"""DoubleBakingLostDeposits"""); break;
                case "doubleBakingLostRewards": columns.Add(@"""DoubleBakingLostRewards"""); break;
                case "doubleBakingLostFees": columns.Add(@"""DoubleBakingLostFees"""); break;
                case "doubleEndorsingRewards": columns.Add(@"""DoubleEndorsingRewards"""); break;
                case "doubleEndorsingLostDeposits": columns.Add(@"""DoubleEndorsingLostDeposits"""); break;
                case "doubleEndorsingLostRewards": columns.Add(@"""DoubleEndorsingLostRewards"""); break;
                case "doubleEndorsingLostFees": columns.Add(@"""DoubleEndorsingLostFees"""); break;
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
                case "quote": columns.Add(@"""Cycle"""); break;
            }

            if (columns.Count == 0)
                return Array.Empty<object>();

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""BakerCycles""")
                .Filter("BakerId", baker.Id)
                .Filter("Cycle", cycle)
                .Take(sort ?? new SortParameter { Desc = "cycle" }, offset, limit, x => ("Cycle", "Cycle"));

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            //TODO: optimize memory allocation
            var result = new object[rows.Count()];
            var j = 0;

            switch (field)
            {
                case "doubleBakingRewards":
                    foreach (var row in rows)
                        result[j++] = row.DoubleBakingRewards;
                    break;
                case "doubleBakingLostDeposits":
                    foreach (var row in rows)
                        result[j++] = row.DoubleBakingLostDeposits;
                    break;
                case "doubleBakingLostRewards":
                    foreach (var row in rows)
                        result[j++] = row.DoubleBakingLostRewards;
                    break;
                case "doubleBakingLostFees":
                    foreach (var row in rows)
                        result[j++] = row.DoubleBakingLostFees;
                    break;
                case "doubleEndorsingRewards":
                    foreach (var row in rows)
                        result[j++] = row.DoubleEndorsingRewards;
                    break;
                case "doubleEndorsingLostDeposits":
                    foreach (var row in rows)
                        result[j++] = row.DoubleEndorsingLostDeposits;
                    break;
                case "doubleEndorsingLostRewards":
                    foreach (var row in rows)
                        result[j++] = row.DoubleEndorsingLostRewards;
                    break;
                case "doubleEndorsingLostFees":
                    foreach (var row in rows)
                        result[j++] = row.DoubleEndorsingLostFees;
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
                case "quote":
                    foreach (var row in rows)
                        result[j++] = Quotes.Get(quote, Protocols.FindByCycle((int)row.Cycle).GetCycleEnd((int)row.Cycle));
                    break;
            }

            return result;
        }
        #endregion

        #region delegator
        public async Task<int> GetDelegatorRewardsCount(string address)
        {
            var acc = await Accounts.GetAsync(address);
            if (acc == null) return 0;

            using var db = GetConnection();
            return await db.QueryFirstAsync<int>($@"SELECT COUNT(*) FROM ""DelegatorCycles"" WHERE ""DelegatorId"" = {acc.Id}");
        }

        public async Task<DelegatorRewards> GetDelegatorRewards(string address, int cycle, Symbols quote)
        {
            var acc = await Accounts.GetAsync(address);
            if (acc == null) return null;

            var sql = $@"
                SELECT      bc.*, dc.""Balance""
                FROM        ""DelegatorCycles"" as dc
                INNER JOIN  ""BakerCycles"" as bc
                        ON  bc.""BakerId"" = dc.""BakerId""
                       AND  bc.""Cycle"" = dc.""Cycle""
                WHERE       dc.""DelegatorId"" = {acc.Id}
                AND         dc.""Cycle"" = {cycle}
                LIMIT       1";

            using var db = GetConnection();
            var row = await db.QueryFirstOrDefaultAsync(sql);
            if (row == null) return null;

            return new DelegatorRewards
            {
                Baker = Accounts.GetAlias(row.BakerId),
                Balance = row.Balance,
                DoubleBakingRewards = row.DoubleBakingRewards,
                DoubleBakingLostDeposits = row.DoubleBakingLostDeposits,
                DoubleBakingLostRewards = row.DoubleBakingLostRewards,
                DoubleBakingLostFees = row.DoubleBakingLostFees,
                DoubleEndorsingRewards = row.DoubleEndorsingRewards,
                DoubleEndorsingLostDeposits = row.DoubleEndorsingLostDeposits,
                DoubleEndorsingLostRewards = row.DoubleEndorsingLostRewards,
                DoubleEndorsingLostFees = row.DoubleEndorsingLostFees,
                Cycle = row.Cycle,
                EndorsementRewards = row.EndorsementRewards,
                Endorsements = row.Endorsements,
                ExpectedBlocks = Math.Round(row.ExpectedBlocks, 2),
                ExpectedEndorsements = Math.Round(row.ExpectedEndorsements, 2),
                ExtraBlockFees = row.ExtraBlockFees,
                ExtraBlockRewards = row.ExtraBlockRewards,
                ExtraBlocks = row.ExtraBlocks,
                FutureBlockRewards = row.FutureBlockRewards,
                FutureBlocks = row.FutureBlocks,
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
                UncoveredOwnBlocks = row.UncoveredOwnBlocks,
                Quote = Quotes.Get(quote, Protocols.FindByCycle((int)row.Cycle).GetCycleEnd((int)row.Cycle))
            };
        }

        public async Task<IEnumerable<DelegatorRewards>> GetDelegatorRewards(
            string address,
            Int32Parameter cycle,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            Symbols quote)
        {
            var acc = await Accounts.GetAsync(address);
            if (acc == null) return null;

            var sql = new SqlBuilder(@"
                SELECT      bc.*, dc.""Balance""
                FROM        ""DelegatorCycles"" as dc
                INNER JOIN  ""BakerCycles"" as bc
                        ON  bc.""BakerId"" = dc.""BakerId""
                       AND  bc.""Cycle"" = dc.""Cycle""
                ")
                .FilterA(@"dc.""DelegatorId""", acc.Id)
                .FilterA(@"dc.""Cycle""", cycle)
                .Take(sort ?? new SortParameter { Desc = "cycle" }, offset, limit, x => ("Cycle", "Cycle"), "dc");

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            return rows.Select(row => new DelegatorRewards
            {
                Baker = Accounts.GetAlias(row.BakerId),
                Balance = row.Balance,
                DoubleBakingRewards = row.DoubleBakingRewards,
                DoubleBakingLostDeposits = row.DoubleBakingLostDeposits,
                DoubleBakingLostRewards = row.DoubleBakingLostRewards,
                DoubleBakingLostFees = row.DoubleBakingLostFees,
                DoubleEndorsingRewards = row.DoubleEndorsingRewards,
                DoubleEndorsingLostDeposits = row.DoubleEndorsingLostDeposits,
                DoubleEndorsingLostRewards = row.DoubleEndorsingLostRewards,
                DoubleEndorsingLostFees = row.DoubleEndorsingLostFees,
                Cycle = row.Cycle,
                EndorsementRewards = row.EndorsementRewards,
                Endorsements = row.Endorsements,
                ExpectedBlocks = Math.Round(row.ExpectedBlocks, 2),
                ExpectedEndorsements = Math.Round(row.ExpectedEndorsements, 2),
                ExtraBlockFees = row.ExtraBlockFees,
                ExtraBlockRewards = row.ExtraBlockRewards,
                ExtraBlocks = row.ExtraBlocks,
                FutureBlockRewards = row.FutureBlockRewards,
                FutureBlocks = row.FutureBlocks,
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
                UncoveredOwnBlocks = row.UncoveredOwnBlocks,
                Quote = Quotes.Get(quote, Protocols.FindByCycle((int)row.Cycle).GetCycleEnd((int)row.Cycle))
            });
        }

        public async Task<object[][]> GetDelegatorRewards(
            string address,
            Int32Parameter cycle,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            string[] fields,
            Symbols quote)
        {
            var acc = await Accounts.GetAsync(address);
            if (acc == null) return null;

            var columns = new HashSet<string>(fields.Length);
            var join = false;

            foreach (var field in fields)
            {
                switch (field)
                {
                    case "baker": columns.Add(@"dc.""BakerId"""); break;
                    case "balance": columns.Add(@"dc.""Balance"""); break;
                    case "cycle": columns.Add(@"dc.""Cycle"""); break;
                    case "quote": columns.Add(@"dc.""Cycle"""); break;
                    case "doubleBakingRewards": columns.Add(@"bc.""DoubleBakingRewards"""); join = true; break;
                    case "doubleBakingLostDeposits": columns.Add(@"bc.""DoubleBakingLostDeposits"""); join = true; break;
                    case "doubleBakingLostRewards": columns.Add(@"bc.""DoubleBakingLostRewards"""); join = true; break;
                    case "doubleBakingLostFees": columns.Add(@"bc.""DoubleBakingLostFees"""); join = true; break;
                    case "doubleEndorsingRewards": columns.Add(@"bc.""DoubleEndorsingRewards"""); join = true; break;
                    case "doubleEndorsingLostDeposits": columns.Add(@"bc.""DoubleEndorsingLostDeposits"""); join = true; break;
                    case "doubleEndorsingLostRewards": columns.Add(@"bc.""DoubleEndorsingLostRewards"""); join = true; break;
                    case "doubleEndorsingLostFees": columns.Add(@"bc.""DoubleEndorsingLostFees"""); join = true; break;
                    case "endorsementRewards": columns.Add(@"bc.""EndorsementRewards"""); join = true; break;
                    case "endorsements": columns.Add(@"bc.""Endorsements"""); join = true; break;
                    case "expectedBlocks": columns.Add(@"bc.""ExpectedBlocks"""); join = true; break;
                    case "expectedEndorsements": columns.Add(@"bc.""ExpectedEndorsements"""); join = true; break;
                    case "extraBlockFees": columns.Add(@"bc.""ExtraBlockFees"""); join = true; break;
                    case "extraBlockRewards": columns.Add(@"bc.""ExtraBlockRewards"""); join = true; break;
                    case "extraBlocks": columns.Add(@"bc.""ExtraBlocks"""); join = true; break;
                    case "futureBlockRewards": columns.Add(@"bc.""FutureBlockRewards"""); join = true; break;
                    case "futureBlocks": columns.Add(@"bc.""FutureBlocks"""); join = true; break;
                    case "futureEndorsementRewards": columns.Add(@"bc.""FutureEndorsementRewards"""); join = true; break;
                    case "futureEndorsements": columns.Add(@"bc.""FutureEndorsements"""); join = true; break;
                    case "missedEndorsementRewards": columns.Add(@"bc.""MissedEndorsementRewards"""); join = true; break;
                    case "missedEndorsements": columns.Add(@"bc.""MissedEndorsements"""); join = true; break;
                    case "missedExtraBlockFees": columns.Add(@"bc.""MissedExtraBlockFees"""); join = true; break;
                    case "missedExtraBlockRewards": columns.Add(@"bc.""MissedExtraBlockRewards"""); join = true; break;
                    case "missedExtraBlocks": columns.Add(@"bc.""MissedExtraBlocks"""); join = true; break;
                    case "missedOwnBlockFees": columns.Add(@"bc.""MissedOwnBlockFees"""); join = true; break;
                    case "missedOwnBlockRewards": columns.Add(@"bc.""MissedOwnBlockRewards"""); join = true; break;
                    case "missedOwnBlocks": columns.Add(@"bc.""MissedOwnBlocks"""); join = true; break;
                    case "ownBlockFees": columns.Add(@"bc.""OwnBlockFees"""); join = true; break;
                    case "ownBlockRewards": columns.Add(@"bc.""OwnBlockRewards"""); join = true; break;
                    case "ownBlocks": columns.Add(@"bc.""OwnBlocks"""); join = true; break;
                    case "revelationLostFees": columns.Add(@"bc.""RevelationLostFees"""); join = true; break;
                    case "revelationLostRewards": columns.Add(@"bc.""RevelationLostRewards"""); join = true; break;
                    case "revelationRewards": columns.Add(@"bc.""RevelationRewards"""); join = true; break;
                    case "stakingBalance": columns.Add(@"bc.""StakingBalance"""); join = true; break;
                    case "uncoveredEndorsementRewards": columns.Add(@"bc.""UncoveredEndorsementRewards"""); join = true; break;
                    case "uncoveredEndorsements": columns.Add(@"bc.""UncoveredEndorsements"""); join = true; break;
                    case "uncoveredExtraBlockFees": columns.Add(@"bc.""UncoveredExtraBlockFees"""); join = true; break;
                    case "uncoveredExtraBlockRewards": columns.Add(@"bc.""UncoveredExtraBlockRewards"""); join = true; break;
                    case "uncoveredExtraBlocks": columns.Add(@"bc.""UncoveredExtraBlocks"""); join = true; break;
                    case "uncoveredOwnBlockFees": columns.Add(@"bc.""UncoveredOwnBlockFees"""); join = true; break;
                    case "uncoveredOwnBlockRewards": columns.Add(@"bc.""UncoveredOwnBlockRewards"""); join = true; break;
                    case "uncoveredOwnBlocks": columns.Add(@"bc.""UncoveredOwnBlocks"""); join = true; break;
                }
            }

            if (columns.Count == 0)
                return Array.Empty<object[]>();

            var joinStr = join
                ? @"INNER JOIN ""BakerCycles"" as bc ON  bc.""BakerId"" = dc.""BakerId"" AND  bc.""Cycle"" = dc.""Cycle"""
                : string.Empty;

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""DelegatorCycles"" as dc {joinStr}")
                .FilterA(@"dc.""DelegatorId""", acc.Id)
                .FilterA(@"dc.""Cycle""", cycle)
                .Take(sort ?? new SortParameter { Desc = "cycle" }, offset, limit, x => ("Cycle", "Cycle"), "dc");

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            var result = new object[rows.Count()][];
            for (int i = 0; i < result.Length; i++)
                result[i] = new object[fields.Length];

            for (int i = 0, j = 0; i < fields.Length; j = 0, i++)
            {
                switch (fields[i])
                {
                    case "baker":
                        foreach (var row in rows)
                            result[j++][i] = Accounts.GetAlias(row.BakerId);
                        break;
                    case "balance":
                        foreach (var row in rows)
                            result[j++][i] = row.Balance;
                        break;
                    case "doubleBakingRewards":
                        foreach (var row in rows)
                            result[j++][i] = row.DoubleBakingRewards;
                        break;
                    case "doubleBakingLostDeposits":
                        foreach (var row in rows)
                            result[j++][i] = row.DoubleBakingLostDeposits;
                        break;
                    case "doubleBakingLostRewards":
                        foreach (var row in rows)
                            result[j++][i] = row.DoubleBakingLostRewards;
                        break;
                    case "doubleBakingLostFees":
                        foreach (var row in rows)
                            result[j++][i] = row.DoubleBakingLostFees;
                        break;
                    case "doubleEndorsingRewards":
                        foreach (var row in rows)
                            result[j++][i] = row.DoubleEndorsingRewards;
                        break;
                    case "doubleEndorsingLostDeposits":
                        foreach (var row in rows)
                            result[j++][i] = row.DoubleEndorsingLostDeposits;
                        break;
                    case "doubleEndorsingLostRewards":
                        foreach (var row in rows)
                            result[j++][i] = row.DoubleEndorsingLostRewards;
                        break;
                    case "doubleEndorsingLostFees":
                        foreach (var row in rows)
                            result[j++][i] = row.DoubleEndorsingLostFees;
                        break;
                    case "cycle":
                        foreach (var row in rows)
                            result[j++][i] = row.Cycle;
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
                    case "futureBlockRewards":
                        foreach (var row in rows)
                            result[j++][i] = row.FutureBlockRewards;
                        break;
                    case "futureBlocks":
                        foreach (var row in rows)
                            result[j++][i] = row.FutureBlocks;
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
                    case "quote":
                        foreach (var row in rows)
                            result[j++][i] = Quotes.Get(quote, Protocols.FindByCycle((int)row.Cycle).GetCycleEnd((int)row.Cycle));
                        break;
                }
            }

            return result;
        }

        public async Task<object[]> GetDelegatorRewards(
            string address,
            Int32Parameter cycle,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            string field,
            Symbols quote)
        {
            var acc = await Accounts.GetAsync(address);
            if (acc == null) return null;

            var columns = new HashSet<string>(1);
            var join = false;

            switch (field)
            {
                case "baker": columns.Add(@"dc.""BakerId"""); break;
                case "balance": columns.Add(@"dc.""Balance"""); break;
                case "cycle": columns.Add(@"dc.""Cycle"""); break;
                case "quote": columns.Add(@"dc.""Cycle"""); break;
                case "doubleBakingRewards": columns.Add(@"bc.""DoubleBakingRewards"""); join = true; break;
                case "doubleBakingLostDeposits": columns.Add(@"bc.""DoubleBakingLostDeposits"""); join = true; break;
                case "doubleBakingLostRewards": columns.Add(@"bc.""DoubleBakingLostRewards"""); join = true; break;
                case "doubleBakingLostFees": columns.Add(@"bc.""DoubleBakingLostFees"""); join = true; break;
                case "doubleEndorsingRewards": columns.Add(@"bc.""DoubleEndorsingRewards"""); join = true; break;
                case "doubleEndorsingLostDeposits": columns.Add(@"bc.""DoubleEndorsingLostDeposits"""); join = true; break;
                case "doubleEndorsingLostRewards": columns.Add(@"bc.""DoubleEndorsingLostRewards"""); join = true; break;
                case "doubleEndorsingLostFees": columns.Add(@"bc.""DoubleEndorsingLostFees"""); join = true; break;
                case "endorsementRewards": columns.Add(@"bc.""EndorsementRewards"""); join = true; break;
                case "endorsements": columns.Add(@"bc.""Endorsements"""); join = true; break;
                case "expectedBlocks": columns.Add(@"bc.""ExpectedBlocks"""); join = true; break;
                case "expectedEndorsements": columns.Add(@"bc.""ExpectedEndorsements"""); join = true; break;
                case "extraBlockFees": columns.Add(@"bc.""ExtraBlockFees"""); join = true; break;
                case "extraBlockRewards": columns.Add(@"bc.""ExtraBlockRewards"""); join = true; break;
                case "extraBlocks": columns.Add(@"bc.""ExtraBlocks"""); join = true; break;
                case "futureBlockRewards": columns.Add(@"bc.""FutureBlockRewards"""); join = true; break;
                case "futureBlocks": columns.Add(@"bc.""FutureBlocks"""); join = true; break;
                case "futureEndorsementRewards": columns.Add(@"bc.""FutureEndorsementRewards"""); join = true; break;
                case "futureEndorsements": columns.Add(@"bc.""FutureEndorsements"""); join = true; break;
                case "missedEndorsementRewards": columns.Add(@"bc.""MissedEndorsementRewards"""); join = true; break;
                case "missedEndorsements": columns.Add(@"bc.""MissedEndorsements"""); join = true; break;
                case "missedExtraBlockFees": columns.Add(@"bc.""MissedExtraBlockFees"""); join = true; break;
                case "missedExtraBlockRewards": columns.Add(@"bc.""MissedExtraBlockRewards"""); join = true; break;
                case "missedExtraBlocks": columns.Add(@"bc.""MissedExtraBlocks"""); join = true; break;
                case "missedOwnBlockFees": columns.Add(@"bc.""MissedOwnBlockFees"""); join = true; break;
                case "missedOwnBlockRewards": columns.Add(@"bc.""MissedOwnBlockRewards"""); join = true; break;
                case "missedOwnBlocks": columns.Add(@"bc.""MissedOwnBlocks"""); join = true; break;
                case "ownBlockFees": columns.Add(@"bc.""OwnBlockFees"""); join = true; break;
                case "ownBlockRewards": columns.Add(@"bc.""OwnBlockRewards"""); join = true; break;
                case "ownBlocks": columns.Add(@"bc.""OwnBlocks"""); join = true; break;
                case "revelationLostFees": columns.Add(@"bc.""RevelationLostFees"""); join = true; break;
                case "revelationLostRewards": columns.Add(@"bc.""RevelationLostRewards"""); join = true; break;
                case "revelationRewards": columns.Add(@"bc.""RevelationRewards"""); join = true; break;
                case "stakingBalance": columns.Add(@"bc.""StakingBalance"""); join = true; break;
                case "uncoveredEndorsementRewards": columns.Add(@"bc.""UncoveredEndorsementRewards"""); join = true; break;
                case "uncoveredEndorsements": columns.Add(@"bc.""UncoveredEndorsements"""); join = true; break;
                case "uncoveredExtraBlockFees": columns.Add(@"bc.""UncoveredExtraBlockFees"""); join = true; break;
                case "uncoveredExtraBlockRewards": columns.Add(@"bc.""UncoveredExtraBlockRewards"""); join = true; break;
                case "uncoveredExtraBlocks": columns.Add(@"bc.""UncoveredExtraBlocks"""); join = true; break;
                case "uncoveredOwnBlockFees": columns.Add(@"bc.""UncoveredOwnBlockFees"""); join = true; break;
                case "uncoveredOwnBlockRewards": columns.Add(@"bc.""UncoveredOwnBlockRewards"""); join = true; break;
                case "uncoveredOwnBlocks": columns.Add(@"bc.""UncoveredOwnBlocks"""); join = true; break;
            }

            if (columns.Count == 0)
                return Array.Empty<object>();

            var joinStr = join
                ? @"INNER JOIN ""BakerCycles"" as bc ON  bc.""BakerId"" = dc.""BakerId"" AND  bc.""Cycle"" = dc.""Cycle"""
                : string.Empty;

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""DelegatorCycles"" as dc {joinStr}")
                .FilterA(@"dc.""DelegatorId""", acc.Id)
                .FilterA(@"dc.""Cycle""", cycle)
                .Take(sort ?? new SortParameter { Desc = "cycle" }, offset, limit, x => ("Cycle", "Cycle"), "dc");

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            //TODO: optimize memory allocation
            var result = new object[rows.Count()];
            var j = 0;

            switch (field)
            {
                case "baker":
                    foreach (var row in rows)
                        result[j++] = Accounts.GetAlias(row.BakerId);
                    break;
                case "balance":
                    foreach (var row in rows)
                        result[j++] = row.Balance;
                    break;
                case "doubleBakingRewards":
                    foreach (var row in rows)
                        result[j++] = row.DoubleBakingRewards;
                    break;
                case "doubleBakingLostDeposits":
                    foreach (var row in rows)
                        result[j++] = row.DoubleBakingLostDeposits;
                    break;
                case "doubleBakingLostRewards":
                    foreach (var row in rows)
                        result[j++] = row.DoubleBakingLostRewards;
                    break;
                case "doubleBakingLostFees":
                    foreach (var row in rows)
                        result[j++] = row.DoubleBakingLostFees;
                    break;
                case "doubleEndorsingRewards":
                    foreach (var row in rows)
                        result[j++] = row.DoubleEndorsingRewards;
                    break;
                case "doubleEndorsingLostDeposits":
                    foreach (var row in rows)
                        result[j++] = row.DoubleEndorsingLostDeposits;
                    break;
                case "doubleEndorsingLostRewards":
                    foreach (var row in rows)
                        result[j++] = row.DoubleEndorsingLostRewards;
                    break;
                case "doubleEndorsingLostFees":
                    foreach (var row in rows)
                        result[j++] = row.DoubleEndorsingLostFees;
                    break;
                case "cycle":
                    foreach (var row in rows)
                        result[j++] = row.Cycle;
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
                case "futureBlockRewards":
                    foreach (var row in rows)
                        result[j++] = row.FutureBlockRewards;
                    break;
                case "futureBlocks":
                    foreach (var row in rows)
                        result[j++] = row.FutureBlocks;
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
                case "quote":
                    foreach (var row in rows)
                        result[j++] = Quotes.Get(quote, Protocols.FindByCycle((int)row.Cycle).GetCycleEnd((int)row.Cycle));
                    break;
            }

            return result;
        }
        #endregion

        #region split
        public async Task<RewardSplit> GetRewardSplit(string address, int cycle, int offset, int limit)
        {
            if (await Accounts.GetAsync(address) is not RawDelegate baker)
                return null;

            var sqlRewards = $@"
                SELECT  *
                FROM    ""BakerCycles""
                WHERE   ""BakerId"" = {baker.Id}
                AND     ""Cycle"" = {cycle}
                LIMIT   1";

            var sqlDelegators = $@"
                SELECT      ""DelegatorId"", ""Balance""
                FROM        ""DelegatorCycles""
                WHERE       ""BakerId"" = {baker.Id}
                AND         ""Cycle"" = {cycle}
                ORDER BY    ""Balance"" DESC
                OFFSET      {offset}
                LIMIT       {limit}";

            using var db = GetConnection();
            using var result = await db.QueryMultipleAsync($@"
                {sqlRewards};
                {sqlDelegators};");

            var rewards = result.ReadFirstOrDefault();
            if (rewards == null) return null;

            var delegators = result.Read();

            return new RewardSplit
            {
                DoubleBakingRewards = rewards.DoubleBakingRewards,
                DoubleBakingLostDeposits = rewards.DoubleBakingLostDeposits,
                DoubleBakingLostRewards = rewards.DoubleBakingLostRewards,
                DoubleBakingLostFees = rewards.DoubleBakingLostFees,
                DoubleEndorsingRewards = rewards.DoubleEndorsingRewards,
                DoubleEndorsingLostDeposits = rewards.DoubleEndorsingLostDeposits,
                DoubleEndorsingLostRewards = rewards.DoubleEndorsingLostRewards,
                DoubleEndorsingLostFees = rewards.DoubleEndorsingLostFees,
                BlockDeposits = rewards.BlockDeposits,
                Cycle = rewards.Cycle,
                DelegatedBalance = rewards.DelegatedBalance,
                EndorsementDeposits = rewards.EndorsementDeposits,
                EndorsementRewards = rewards.EndorsementRewards,
                Endorsements = rewards.Endorsements,
                ExpectedBlocks = Math.Round(rewards.ExpectedBlocks, 2),
                ExpectedEndorsements = Math.Round(rewards.ExpectedEndorsements, 2),
                ExtraBlockFees = rewards.ExtraBlockFees,
                ExtraBlockRewards = rewards.ExtraBlockRewards,
                ExtraBlocks = rewards.ExtraBlocks,
                FutureBlockDeposits = rewards.FutureBlockDeposits,
                FutureBlockRewards = rewards.FutureBlockRewards,
                FutureBlocks = rewards.FutureBlocks,
                FutureEndorsementDeposits = rewards.FutureEndorsementDeposits,
                FutureEndorsementRewards = rewards.FutureEndorsementRewards,
                FutureEndorsements = rewards.FutureEndorsements,
                MissedEndorsementRewards = rewards.MissedEndorsementRewards,
                MissedEndorsements = rewards.MissedEndorsements,
                MissedExtraBlockFees = rewards.MissedExtraBlockFees,
                MissedExtraBlockRewards = rewards.MissedExtraBlockRewards,
                MissedExtraBlocks = rewards.MissedExtraBlocks,
                MissedOwnBlockFees = rewards.MissedOwnBlockFees,
                MissedOwnBlockRewards = rewards.MissedOwnBlockRewards,
                MissedOwnBlocks = rewards.MissedOwnBlocks,
                NumDelegators = rewards.DelegatorsCount,
                OwnBlockFees = rewards.OwnBlockFees,
                OwnBlockRewards = rewards.OwnBlockRewards,
                OwnBlocks = rewards.OwnBlocks,
                RevelationLostFees = rewards.RevelationLostFees,
                RevelationLostRewards = rewards.RevelationLostRewards,
                RevelationRewards = rewards.RevelationRewards,
                StakingBalance = rewards.StakingBalance,
                UncoveredEndorsementRewards = rewards.UncoveredEndorsementRewards,
                UncoveredEndorsements = rewards.UncoveredEndorsements,
                UncoveredExtraBlockFees = rewards.UncoveredExtraBlockFees,
                UncoveredExtraBlockRewards = rewards.UncoveredExtraBlockRewards,
                UncoveredExtraBlocks = rewards.UncoveredExtraBlocks,
                UncoveredOwnBlockFees = rewards.UncoveredOwnBlockFees,
                UncoveredOwnBlockRewards = rewards.UncoveredOwnBlockRewards,
                UncoveredOwnBlocks = rewards.UncoveredOwnBlocks,
                Delegators = delegators.Select(x => 
                {
                    var delegator = Accounts.Get((int)x.DelegatorId);
                    return new SplitDelegator
                    {
                        Address = delegator.Address,
                        Balance = x.Balance,
                        CurrentBalance = delegator.Balance,
                        Emptied = delegator is RawUser && delegator.Balance == 0
                    };
                })
            };
        }

        public async Task<SplitDelegator> GetRewardSplitDelegator(string baker, int cycle, string delegator)
        {
            if (await Accounts.GetAsync(baker) is not RawDelegate bakerAccount)
                return null;

            if (await Accounts.GetAsync(delegator) is not RawAccount delegatorAccount)
                return null;

            var sql = $@"
                SELECT      ""Balance""
                FROM        ""DelegatorCycles""
                WHERE       ""BakerId"" = {bakerAccount.Id}
                AND         ""Cycle"" = {cycle}
                AND         ""DelegatorId"" = {delegatorAccount.Id}
                LIMIT       1";

            using var db = GetConnection();
            var result = await db.ExecuteScalarAsync(sql);
            if (result == null) return null;

            return new SplitDelegator
            {
                Balance = (long)result,
                CurrentBalance = delegatorAccount.Balance,
                Emptied = delegatorAccount is RawUser && delegatorAccount.Balance == 0
            };
        }
        #endregion
    }
}
