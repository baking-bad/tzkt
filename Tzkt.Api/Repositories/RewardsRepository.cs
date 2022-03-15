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
                ActiveStake = row.ActiveStake,
                SelectedStake = row.SelectedStake,
                DoubleBakingRewards = row.DoubleBakingRewards,
                DoubleBakingLosses = row.DoubleBakingLosses,
                DoubleEndorsingRewards = row.DoubleEndorsingRewards,
                DoubleEndorsingLosses = row.DoubleEndorsingLosses,
                DoublePreendorsingRewards = row.DoublePreendorsingRewards,
                DoublePreendorsingLosses = row.DoublePreendorsingLosses,
                Cycle = row.Cycle,
                DelegatedBalance = row.DelegatedBalance,
                EndorsementRewards = row.EndorsementRewards,
                Endorsements = row.Endorsements,
                ExpectedBlocks = Math.Round(row.ExpectedBlocks, 2),
                ExpectedEndorsements = Math.Round(row.ExpectedEndorsements, 2),
                FutureBlockRewards = row.FutureBlockRewards,
                FutureBlocks = row.FutureBlocks,
                FutureEndorsementRewards = row.FutureEndorsementRewards,
                FutureEndorsements = row.FutureEndorsements,
                MissedEndorsementRewards = row.MissedEndorsementRewards,
                MissedEndorsements = row.MissedEndorsements,
                MissedBlockFees = row.MissedBlockFees,
                MissedBlockRewards = row.MissedBlockRewards,
                MissedBlocks = row.MissedBlocks,
                NumDelegators = row.DelegatorsCount,
                BlockFees = row.BlockFees,
                BlockRewards = row.BlockRewards,
                Blocks = row.Blocks,
                RevelationLosses = row.RevelationLosses,
                RevelationRewards = row.RevelationRewards,
                StakingBalance = row.StakingBalance,
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
                return Enumerable.Empty<BakerRewards>();

            var sql = new SqlBuilder(@"SELECT * FROM ""BakerCycles""")
                .Filter("BakerId", baker.Id)
                .Filter("Cycle", cycle)
                .Take(sort ?? new SortParameter { Desc = "cycle" }, offset, limit, x => ("Cycle", "Cycle"));

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            return rows.Select(row => new BakerRewards
            {
                ActiveStake = row.ActiveStake,
                SelectedStake = row.SelectedStake,
                DoubleBakingRewards = row.DoubleBakingRewards,
                DoubleBakingLosses = row.DoubleBakingLosses,
                DoubleEndorsingRewards = row.DoubleEndorsingRewards,
                DoubleEndorsingLosses = row.DoubleEndorsingLosses,
                DoublePreendorsingRewards = row.DoublePreendorsingRewards,
                DoublePreendorsingLosses = row.DoublePreendorsingLosses,
                Cycle = row.Cycle,
                DelegatedBalance = row.DelegatedBalance,
                EndorsementRewards = row.EndorsementRewards,
                Endorsements = row.Endorsements,
                ExpectedBlocks = Math.Round(row.ExpectedBlocks, 2),
                ExpectedEndorsements = Math.Round(row.ExpectedEndorsements, 2),
                FutureBlockRewards = row.FutureBlockRewards,
                FutureBlocks = row.FutureBlocks,
                FutureEndorsementRewards = row.FutureEndorsementRewards,
                FutureEndorsements = row.FutureEndorsements,
                MissedEndorsementRewards = row.MissedEndorsementRewards,
                MissedEndorsements = row.MissedEndorsements,
                MissedBlockFees = row.MissedBlockFees,
                MissedBlockRewards = row.MissedBlockRewards,
                MissedBlocks = row.MissedBlocks,
                NumDelegators = row.DelegatorsCount,
                BlockFees = row.BlockFees,
                BlockRewards = row.BlockRewards,
                Blocks = row.Blocks,
                RevelationLosses = row.RevelationLosses,
                RevelationRewards = row.RevelationRewards,
                StakingBalance = row.StakingBalance,
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
                return Array.Empty<object[]>();

            var columns = new HashSet<string>(fields.Length);
            foreach (var field in fields)
            {
                switch (field)
                {
                    case "activeStake": columns.Add(@"""ActiveStake"""); break;
                    case "selectedStake": columns.Add(@"""SelectedStake"""); break;
                    case "doubleBakingRewards": columns.Add(@"""DoubleBakingRewards"""); break;
                    case "doubleBakingLosses": columns.Add(@"""DoubleBakingLosses"""); break;
                    case "doubleEndorsingRewards": columns.Add(@"""DoubleEndorsingRewards"""); break;
                    case "doubleEndorsingLosses": columns.Add(@"""DoubleEndorsingLosses"""); break;
                    case "doublePreendorsingRewards": columns.Add(@"""DoublePreendorsingRewards"""); break;
                    case "doublePreendorsingLosses": columns.Add(@"""DoublePreendorsingLosses"""); break;
                    case "cycle": columns.Add(@"""Cycle"""); break;
                    case "delegatedBalance": columns.Add(@"""DelegatedBalance"""); break;
                    case "endorsementRewards": columns.Add(@"""EndorsementRewards"""); break;
                    case "endorsements": columns.Add(@"""Endorsements"""); break;
                    case "expectedBlocks": columns.Add(@"""ExpectedBlocks"""); break;
                    case "expectedEndorsements": columns.Add(@"""ExpectedEndorsements"""); break;
                    case "futureBlockRewards": columns.Add(@"""FutureBlockRewards"""); break;
                    case "futureBlocks": columns.Add(@"""FutureBlocks"""); break;
                    case "futureEndorsementRewards": columns.Add(@"""FutureEndorsementRewards"""); break;
                    case "futureEndorsements": columns.Add(@"""FutureEndorsements"""); break;
                    case "missedEndorsementRewards": columns.Add(@"""MissedEndorsementRewards"""); break;
                    case "missedEndorsements": columns.Add(@"""MissedEndorsements"""); break;
                    case "missedBlockFees": columns.Add(@"""MissedBlockFees"""); break;
                    case "missedBlockRewards": columns.Add(@"""MissedBlockRewards"""); break;
                    case "missedBlocks": columns.Add(@"""MissedBlocks"""); break;
                    case "numDelegators": columns.Add(@"""DelegatorsCount"""); break;
                    case "blockFees": columns.Add(@"""BlockFees"""); break;
                    case "blockRewards": columns.Add(@"""BlockRewards"""); break;
                    case "blocks": columns.Add(@"""Blocks"""); break;
                    case "revelationLosses": columns.Add(@"""RevelationLosses"""); break;
                    case "revelationRewards": columns.Add(@"""RevelationRewards"""); break;
                    case "stakingBalance": columns.Add(@"""StakingBalance"""); break;
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
                    case "activeStake":
                        foreach (var row in rows)
                            result[j++][i] = row.ActiveStake;
                        break;
                    case "selectedStake":
                        foreach (var row in rows)
                            result[j++][i] = row.SelectedStake;
                        break;
                    case "doubleBakingRewards":
                        foreach (var row in rows)
                            result[j++][i] = row.DoubleBakingRewards;
                        break;
                    case "doubleBakingLosses":
                        foreach (var row in rows)
                            result[j++][i] = row.DoubleBakingLosses;
                        break;
                    case "doubleEndorsingRewards":
                        foreach (var row in rows)
                            result[j++][i] = row.DoubleEndorsingRewards;
                        break;
                    case "doubleEndorsingLosses":
                        foreach (var row in rows)
                            result[j++][i] = row.DoubleEndorsingLosses;
                        break;
                    case "doublePreendorsingRewards":
                        foreach (var row in rows)
                            result[j++][i] = row.DoublePreendorsingRewards;
                        break;
                    case "doublePreendorsingLosses":
                        foreach (var row in rows)
                            result[j++][i] = row.DoublePreendorsingLosses;
                        break;
                    case "cycle":
                        foreach (var row in rows)
                            result[j++][i] = row.Cycle;
                        break;
                    case "delegatedBalance":
                        foreach (var row in rows)
                            result[j++][i] = row.DelegatedBalance;
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
                    case "missedBlockFees":
                        foreach (var row in rows)
                            result[j++][i] = row.MissedBlockFees;
                        break;
                    case "missedBlockRewards":
                        foreach (var row in rows)
                            result[j++][i] = row.MissedBlockRewards;
                        break;
                    case "missedBlocks":
                        foreach (var row in rows)
                            result[j++][i] = row.MissedBlocks;
                        break;
                    case "numDelegators":
                        foreach (var row in rows)
                            result[j++][i] = row.DelegatorsCount;
                        break;
                    case "blockFees":
                        foreach (var row in rows)
                            result[j++][i] = row.BlockFees;
                        break;
                    case "blockRewards":
                        foreach (var row in rows)
                            result[j++][i] = row.OwnRewards;
                        break;
                    case "blocks":
                        foreach (var row in rows)
                            result[j++][i] = row.Blocks;
                        break;
                    case "revelationLosses":
                        foreach (var row in rows)
                            result[j++][i] = row.RevelationLosses;
                        break;
                    case "revelationRewards":
                        foreach (var row in rows)
                            result[j++][i] = row.RevelationRewards;
                        break;
                    case "stakingBalance":
                        foreach (var row in rows)
                            result[j++][i] = row.StakingBalance;
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
                return Array.Empty<object>();

            var columns = new HashSet<string>(1);
            switch (field)
            {
                case "activeStake": columns.Add(@"""ActiveStake"""); break;
                case "selectedStake": columns.Add(@"""SelectedStake"""); break;
                case "doubleBakingRewards": columns.Add(@"""DoubleBakingRewards"""); break;
                case "doubleBakingLosses": columns.Add(@"""DoubleBakingLosses"""); break;
                case "doubleEndorsingRewards": columns.Add(@"""DoubleEndorsingRewards"""); break;
                case "doubleEndorsingLosses": columns.Add(@"""DoubleEndorsingLosses"""); break;
                case "doublePreendorsingRewards": columns.Add(@"""DoublePreendorsingRewards"""); break;
                case "doublePreendorsingLosses": columns.Add(@"""DoublePreendorsingLosses"""); break;
                case "cycle": columns.Add(@"""Cycle"""); break;
                case "delegatedBalance": columns.Add(@"""DelegatedBalance"""); break;
                case "endorsementRewards": columns.Add(@"""EndorsementRewards"""); break;
                case "endorsements": columns.Add(@"""Endorsements"""); break;
                case "expectedBlocks": columns.Add(@"""ExpectedBlocks"""); break;
                case "expectedEndorsements": columns.Add(@"""ExpectedEndorsements"""); break;
                case "futureBlockRewards": columns.Add(@"""FutureBlockRewards"""); break;
                case "futureBlocks": columns.Add(@"""FutureBlocks"""); break;
                case "futureEndorsementRewards": columns.Add(@"""FutureEndorsementRewards"""); break;
                case "futureEndorsements": columns.Add(@"""FutureEndorsements"""); break;
                case "missedEndorsementRewards": columns.Add(@"""MissedEndorsementRewards"""); break;
                case "missedEndorsements": columns.Add(@"""MissedEndorsements"""); break;
                case "missedBlockFees": columns.Add(@"""MissedBlockFees"""); break;
                case "missedBlockRewards": columns.Add(@"""MissedBlockRewards"""); break;
                case "missedBlocks": columns.Add(@"""MissedBlocks"""); break;
                case "numDelegators": columns.Add(@"""DelegatorsCount"""); break;
                case "blockFees": columns.Add(@"""BlockFees"""); break;
                case "blockRewards": columns.Add(@"""BlockRewards"""); break;
                case "blocks": columns.Add(@"""Blocks"""); break;
                case "revelationLosses": columns.Add(@"""RevelationLosses"""); break;
                case "revelationRewards": columns.Add(@"""RevelationRewards"""); break;
                case "stakingBalance": columns.Add(@"""StakingBalance"""); break;
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
                case "activeStake":
                    foreach (var row in rows)
                        result[j++] = row.ActiveStake;
                    break;
                case "selectedStake":
                    foreach (var row in rows)
                        result[j++] = row.SelectedStake;
                    break;
                case "doubleBakingRewards":
                    foreach (var row in rows)
                        result[j++] = row.DoubleBakingRewards;
                    break;
                case "doubleBakingLosses":
                    foreach (var row in rows)
                        result[j++] = row.DoubleBakingLosses;
                    break;
                case "doubleEndorsingRewards":
                    foreach (var row in rows)
                        result[j++] = row.DoubleEndorsingRewards;
                    break;
                case "doubleEndorsingLosses":
                    foreach (var row in rows)
                        result[j++] = row.DoubleEndorsingLosses;
                    break;
                case "doublePreendorsingRewards":
                    foreach (var row in rows)
                        result[j++] = row.DoublePreendorsingRewards;
                    break;
                case "doublePreendorsingLosses":
                    foreach (var row in rows)
                        result[j++] = row.DoublePreendorsingLosses;
                    break;
                case "cycle":
                    foreach (var row in rows)
                        result[j++] = row.Cycle;
                    break;
                case "delegatedBalance":
                    foreach (var row in rows)
                        result[j++] = row.DelegatedBalance;
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
                case "missedBlockFees":
                    foreach (var row in rows)
                        result[j++] = row.MissedBlockFees;
                    break;
                case "missedBlockRewards":
                    foreach (var row in rows)
                        result[j++] = row.MissedBlockRewards;
                    break;
                case "missedBlocks":
                    foreach (var row in rows)
                        result[j++] = row.MissedBlocks;
                    break;
                case "numDelegators":
                    foreach (var row in rows)
                        result[j++] = row.DelegatorsCount;
                    break;
                case "blockFees":
                    foreach (var row in rows)
                        result[j++] = row.BlockFees;
                    break;
                case "blockRewards":
                    foreach (var row in rows)
                        result[j++] = row.BlockRewards;
                    break;
                case "blocks":
                    foreach (var row in rows)
                        result[j++] = row.Blocks;
                    break;
                case "revelationLosses":
                    foreach (var row in rows)
                        result[j++] = row.RevelationLosses;
                    break;
                case "revelationRewards":
                    foreach (var row in rows)
                        result[j++] = row.RevelationRewards;
                    break;
                case "stakingBalance":
                    foreach (var row in rows)
                        result[j++] = row.StakingBalance;
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
                ActiveStake = row.ActiveStake,
                SelectedStake = row.SelectedStake,
                DoubleBakingRewards = row.DoubleBakingRewards,
                DoubleBakingLosses = row.DoubleBakingLosses,
                DoubleEndorsingRewards = row.DoubleEndorsingRewards,
                DoubleEndorsingLosses = row.DoubleEndorsingLosses,
                DoublePreendorsingRewards = row.DoublePreendorsingRewards,
                DoublePreendorsingLosses = row.DoublePreendorsingLosses,
                Cycle = row.Cycle,
                EndorsementRewards = row.EndorsementRewards,
                Endorsements = row.Endorsements,
                ExpectedBlocks = Math.Round(row.ExpectedBlocks, 2),
                ExpectedEndorsements = Math.Round(row.ExpectedEndorsements, 2),
                FutureBlockRewards = row.FutureBlockRewards,
                FutureBlocks = row.FutureBlocks,
                FutureEndorsementRewards = row.FutureEndorsementRewards,
                FutureEndorsements = row.FutureEndorsements,
                MissedEndorsementRewards = row.MissedEndorsementRewards,
                MissedEndorsements = row.MissedEndorsements,
                MissedBlockFees = row.MissedBlockFees,
                MissedBlockRewards = row.MissedBlockRewards,
                MissedBlocks = row.MissedBlocks,
                BlockFees = row.BlockFees,
                BlockRewards = row.BlockRewards,
                Blocks = row.Blocks,
                RevelationLosses = row.RevelationLosses,
                RevelationRewards = row.RevelationRewards,
                StakingBalance = row.StakingBalance,
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
            if (acc == null) return Enumerable.Empty<DelegatorRewards>();

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
                ActiveStake = row.ActiveStake,
                SelectedStake = row.SelectedStake,
                DoubleBakingRewards = row.DoubleBakingRewards,
                DoubleBakingLosses = row.DoubleBakingLosses,
                DoubleEndorsingRewards = row.DoubleEndorsingRewards,
                DoubleEndorsingLosses = row.DoubleEndorsingLosses,
                DoublePreendorsingRewards = row.DoublePreendorsingRewards,
                DoublePreendorsingLosses = row.DoublePreendorsingLosses,
                Cycle = row.Cycle,
                EndorsementRewards = row.EndorsementRewards,
                Endorsements = row.Endorsements,
                ExpectedBlocks = Math.Round(row.ExpectedBlocks, 2),
                ExpectedEndorsements = Math.Round(row.ExpectedEndorsements, 2),
                FutureBlockRewards = row.FutureBlockRewards,
                FutureBlocks = row.FutureBlocks,
                FutureEndorsementRewards = row.FutureEndorsementRewards,
                FutureEndorsements = row.FutureEndorsements,
                MissedEndorsementRewards = row.MissedEndorsementRewards,
                MissedEndorsements = row.MissedEndorsements,
                MissedBlockFees = row.MissedBlockFees,
                MissedBlockRewards = row.MissedBlockRewards,
                MissedBlocks = row.MissedBlocks,
                BlockFees = row.BlockFees,
                BlockRewards = row.BlockRewards,
                Blocks = row.Blocks,
                RevelationLosses = row.RevelationLosses,
                RevelationRewards = row.RevelationRewards,
                StakingBalance = row.StakingBalance,
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
            if (acc == null) return Array.Empty<object[]>();

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
                    case "activeStake": columns.Add(@"bc.""ActiveStake"""); join = true; break;
                    case "selectedStake": columns.Add(@"bc.""SelectedStake"""); join = true; break;
                    case "doubleBakingRewards": columns.Add(@"bc.""DoubleBakingRewards"""); join = true; break;
                    case "doubleBakingLosses": columns.Add(@"bc.""DoubleBakingLosses"""); join = true; break;
                    case "doubleEndorsingRewards": columns.Add(@"bc.""DoubleEndorsingRewards"""); join = true; break;
                    case "doubleEndorsingLosses": columns.Add(@"bc.""DoubleEndorsingLosses"""); join = true; break;
                    case "doublePreendorsingRewards": columns.Add(@"bc.""DoublePreendorsingRewards"""); join = true; break;
                    case "doublePreendorsingLosses": columns.Add(@"bc.""DoublePreendorsingLosses"""); join = true; break;
                    case "endorsementRewards": columns.Add(@"bc.""EndorsementRewards"""); join = true; break;
                    case "endorsements": columns.Add(@"bc.""Endorsements"""); join = true; break;
                    case "expectedBlocks": columns.Add(@"bc.""ExpectedBlocks"""); join = true; break;
                    case "expectedEndorsements": columns.Add(@"bc.""ExpectedEndorsements"""); join = true; break;
                    case "futureBlockRewards": columns.Add(@"bc.""FutureBlockRewards"""); join = true; break;
                    case "futureBlocks": columns.Add(@"bc.""FutureBlocks"""); join = true; break;
                    case "futureEndorsementRewards": columns.Add(@"bc.""FutureEndorsementRewards"""); join = true; break;
                    case "futureEndorsements": columns.Add(@"bc.""FutureEndorsements"""); join = true; break;
                    case "missedEndorsementRewards": columns.Add(@"bc.""MissedEndorsementRewards"""); join = true; break;
                    case "missedEndorsements": columns.Add(@"bc.""MissedEndorsements"""); join = true; break;
                    case "missedBlockFees": columns.Add(@"bc.""MissedBlockFees"""); join = true; break;
                    case "missedBlockRewards": columns.Add(@"bc.""MissedBlockRewards"""); join = true; break;
                    case "missedBlocks": columns.Add(@"bc.""MissedBlocks"""); join = true; break;
                    case "blockFees": columns.Add(@"bc.""BlockFees"""); join = true; break;
                    case "blockRewards": columns.Add(@"bc.""BlockRewards"""); join = true; break;
                    case "blocks": columns.Add(@"bc.""Blocks"""); join = true; break;
                    case "revelationLosses": columns.Add(@"bc.""RevelationLosses"""); join = true; break;
                    case "revelationRewards": columns.Add(@"bc.""RevelationRewards"""); join = true; break;
                    case "stakingBalance": columns.Add(@"bc.""StakingBalance"""); join = true; break;
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
                    case "activeStake":
                        foreach (var row in rows)
                            result[j++][i] = row.ActiveStake;
                        break;
                    case "selectedStake":
                        foreach (var row in rows)
                            result[j++][i] = row.SelectedStake;
                        break;
                    case "doubleBakingRewards":
                        foreach (var row in rows)
                            result[j++][i] = row.DoubleBakingRewards;
                        break;
                    case "doubleBakingLosses":
                        foreach (var row in rows)
                            result[j++][i] = row.DoubleBakingLosses;
                        break;
                    case "doubleEndorsingRewards":
                        foreach (var row in rows)
                            result[j++][i] = row.DoubleEndorsingRewards;
                        break;
                    case "doubleEndorsingLosses":
                        foreach (var row in rows)
                            result[j++][i] = row.DoubleEndorsingLosses;
                        break;
                    case "doublePreendorsingRewards":
                        foreach (var row in rows)
                            result[j++][i] = row.DoublePreendorsingRewards;
                        break;
                    case "doublePreendorsingLosses":
                        foreach (var row in rows)
                            result[j++][i] = row.DoublePreendorsingLosses;
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
                    case "missedBlockFees":
                        foreach (var row in rows)
                            result[j++][i] = row.MissedBlockFees;
                        break;
                    case "missedBlockRewards":
                        foreach (var row in rows)
                            result[j++][i] = row.MissedBlockRewards;
                        break;
                    case "missedBlocks":
                        foreach (var row in rows)
                            result[j++][i] = row.MissedBlocks;
                        break;
                    case "blockFees":
                        foreach (var row in rows)
                            result[j++][i] = row.BlockFees;
                        break;
                    case "blockRewards":
                        foreach (var row in rows)
                            result[j++][i] = row.BlockRewards;
                        break;
                    case "blocks":
                        foreach (var row in rows)
                            result[j++][i] = row.Blocks;
                        break;
                    case "revelationLosses":
                        foreach (var row in rows)
                            result[j++][i] = row.RevelationLosses;
                        break;
                    case "revelationRewards":
                        foreach (var row in rows)
                            result[j++][i] = row.RevelationRewards;
                        break;
                    case "stakingBalance":
                        foreach (var row in rows)
                            result[j++][i] = row.StakingBalance;
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
            if (acc == null) return Array.Empty<object>();

            var columns = new HashSet<string>(1);
            var join = false;

            switch (field)
            {
                case "baker": columns.Add(@"dc.""BakerId"""); break;
                case "balance": columns.Add(@"dc.""Balance"""); break;
                case "cycle": columns.Add(@"dc.""Cycle"""); break;
                case "quote": columns.Add(@"dc.""Cycle"""); break;
                case "activeStake": columns.Add(@"bc.""ActiveStake"""); join = true; break;
                case "selectedStake": columns.Add(@"bc.""SelectedStake"""); join = true; break;
                case "doubleBakingRewards": columns.Add(@"bc.""DoubleBakingRewards"""); join = true; break;
                case "doubleBakingLosses": columns.Add(@"bc.""DoubleBakingLosses"""); join = true; break;
                case "doubleEndorsingRewards": columns.Add(@"bc.""DoubleEndorsingRewards"""); join = true; break;
                case "doubleEndorsingLosses": columns.Add(@"bc.""DoubleEndorsingLosses"""); join = true; break;
                case "doublePreendorsingRewards": columns.Add(@"bc.""DoublePreendorsingRewards"""); join = true; break;
                case "doublePreendorsingLosses": columns.Add(@"bc.""DoublePreendorsingLosses"""); join = true; break;
                case "endorsementRewards": columns.Add(@"bc.""EndorsementRewards"""); join = true; break;
                case "endorsements": columns.Add(@"bc.""Endorsements"""); join = true; break;
                case "expectedBlocks": columns.Add(@"bc.""ExpectedBlocks"""); join = true; break;
                case "expectedEndorsements": columns.Add(@"bc.""ExpectedEndorsements"""); join = true; break;
                case "futureBlockRewards": columns.Add(@"bc.""FutureBlockRewards"""); join = true; break;
                case "futureBlocks": columns.Add(@"bc.""FutureBlocks"""); join = true; break;
                case "futureEndorsementRewards": columns.Add(@"bc.""FutureEndorsementRewards"""); join = true; break;
                case "futureEndorsements": columns.Add(@"bc.""FutureEndorsements"""); join = true; break;
                case "missedEndorsementRewards": columns.Add(@"bc.""MissedEndorsementRewards"""); join = true; break;
                case "missedEndorsements": columns.Add(@"bc.""MissedEndorsements"""); join = true; break;
                case "missedBlockFees": columns.Add(@"bc.""MissedBlockFees"""); join = true; break;
                case "missedBlockRewards": columns.Add(@"bc.""MissedBlockRewards"""); join = true; break;
                case "missedBlocks": columns.Add(@"bc.""MissedBlocks"""); join = true; break;
                case "blockFees": columns.Add(@"bc.""BlockFees"""); join = true; break;
                case "blockRewards": columns.Add(@"bc.""BlockRewards"""); join = true; break;
                case "blocks": columns.Add(@"bc.""Blocks"""); join = true; break;
                case "revelationLosses": columns.Add(@"bc.""RevelationLosses"""); join = true; break;
                case "revelationRewards": columns.Add(@"bc.""RevelationRewards"""); join = true; break;
                case "stakingBalance": columns.Add(@"bc.""StakingBalance"""); join = true; break;
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
                case "activeStake":
                    foreach (var row in rows)
                        result[j++] = row.ActiveStake;
                    break;
                case "selectedStake":
                    foreach (var row in rows)
                        result[j++] = row.SelectedStake;
                    break;
                case "doubleBakingRewards":
                    foreach (var row in rows)
                        result[j++] = row.DoubleBakingRewards;
                    break;
                case "doubleBakingLosses":
                    foreach (var row in rows)
                        result[j++] = row.DoubleBakingLosses;
                    break;
                case "doubleEndorsingRewards":
                    foreach (var row in rows)
                        result[j++] = row.DoubleEndorsingRewards;
                    break;
                case "doubleEndorsingLosses":
                    foreach (var row in rows)
                        result[j++] = row.DoubleEndorsingLosses;
                    break;
                case "doublePreendorsingRewards":
                    foreach (var row in rows)
                        result[j++] = row.DoublePreendorsingRewards;
                    break;
                case "doublePreendorsingLosses":
                    foreach (var row in rows)
                        result[j++] = row.DoublePreendorsingLosses;
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
                case "missedBlockFees":
                    foreach (var row in rows)
                        result[j++] = row.MissedBlockFees;
                    break;
                case "missedBlockRewards":
                    foreach (var row in rows)
                        result[j++] = row.MissedBlockRewards;
                    break;
                case "missedBlocks":
                    foreach (var row in rows)
                        result[j++] = row.MissedBlocks;
                    break;
                case "blockFees":
                    foreach (var row in rows)
                        result[j++] = row.BlockFees;
                    break;
                case "blockRewards":
                    foreach (var row in rows)
                        result[j++] = row.BlockRewards;
                    break;
                case "blocks":
                    foreach (var row in rows)
                        result[j++] = row.Blocks;
                    break;
                case "revelationLosses":
                    foreach (var row in rows)
                        result[j++] = row.RevelationLosses;
                    break;
                case "revelationRewards":
                    foreach (var row in rows)
                        result[j++] = row.RevelationRewards;
                    break;
                case "stakingBalance":
                    foreach (var row in rows)
                        result[j++] = row.StakingBalance;
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
                ActiveStake = rewards.ActiveStake,
                SelectedStake = rewards.SelectedStake,
                DoubleBakingRewards = rewards.DoubleBakingRewards,
                DoubleBakingLosses = rewards.DoubleBakingLosses,
                DoubleEndorsingRewards = rewards.DoubleEndorsingRewards,
                DoubleEndorsingLosses = rewards.DoubleEndorsingLosses,
                DoublePreendorsingRewards = rewards.DoublePreendorsingRewards,
                DoublePreendorsingLosses = rewards.DoublePreendorsingLosses,
                Cycle = rewards.Cycle,
                DelegatedBalance = rewards.DelegatedBalance,
                EndorsementRewards = rewards.EndorsementRewards,
                Endorsements = rewards.Endorsements,
                ExpectedBlocks = Math.Round(rewards.ExpectedBlocks, 2),
                ExpectedEndorsements = Math.Round(rewards.ExpectedEndorsements, 2),
                FutureBlockRewards = rewards.FutureBlockRewards,
                FutureBlocks = rewards.FutureBlocks,
                FutureEndorsementRewards = rewards.FutureEndorsementRewards,
                FutureEndorsements = rewards.FutureEndorsements,
                MissedEndorsementRewards = rewards.MissedEndorsementRewards,
                MissedEndorsements = rewards.MissedEndorsements,
                MissedBlockFees = rewards.MissedBlockFees,
                MissedBlockRewards = rewards.MissedBlockRewards,
                MissedBlocks = rewards.MissedBlocks,
                NumDelegators = rewards.DelegatorsCount,
                BlockFees = rewards.BlockFees,
                BlockRewards = rewards.BlockRewards,
                Blocks = rewards.Blocks,
                RevelationLosses = rewards.RevelationLosses,
                RevelationRewards = rewards.RevelationRewards,
                StakingBalance = rewards.StakingBalance,
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
