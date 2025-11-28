using Dapper;
using Npgsql;
using Mvkt.Api.Models;
using Mvkt.Api.Services.Cache;

namespace Mvkt.Api.Repositories
{
    public class RewardsRepository
    {
        readonly NpgsqlDataSource DataSource;
        readonly AccountsCache Accounts;
        readonly ProtocolsCache Protocols;
        readonly QuotesCache Quotes;

        public RewardsRepository(NpgsqlDataSource dataSource, AccountsCache accounts, ProtocolsCache protocols, QuotesCache quotes)
        {
            DataSource = dataSource;
            Accounts = accounts;
            Protocols = protocols;
            Quotes = quotes;
        }

        #region baker
        public async Task<int> GetBakerRewardsCount(string address)
        {
            if (await Accounts.GetAsync(address) is not RawDelegate baker)
                return 0;

            await using var db = await DataSource.OpenConnectionAsync();
            return await db.QueryFirstAsync<int>($@"SELECT COUNT(*) FROM ""BakerCycles"" WHERE ""BakerId"" = {baker.Id}");
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

            await using var db = await DataSource.OpenConnectionAsync();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            return rows.Select(row => new BakerRewards
            {
                Cycle = row.Cycle,
                BakingPower = row.BakingPower,
                TotalBakingPower = row.TotalBakingPower,
                OwnDelegatedBalance = row.OwnDelegatedBalance,
                ExternalDelegatedBalance = row.ExternalDelegatedBalance,
                DelegatorsCount = row.DelegatorsCount,
                OwnStakedBalance = row.OwnStakedBalance,
                ExternalStakedBalance = row.ExternalStakedBalance,
                StakersCount = row.StakersCount,
                ExpectedBlocks = Math.Round(row.ExpectedBlocks, 2),
                ExpectedEndorsements = Math.Round(row.ExpectedEndorsements, 2),
                FutureBlocks = row.FutureBlocks,
                FutureBlockRewards = row.FutureBlockRewards,
                Blocks = row.Blocks,
                BlockRewardsDelegated = row.BlockRewardsDelegated,
                BlockRewardsStakedOwn = row.BlockRewardsStakedOwn,
                BlockRewardsStakedEdge = row.BlockRewardsStakedEdge,
                BlockRewardsStakedShared = row.BlockRewardsStakedShared,
                MissedBlocks = row.MissedBlocks,
                MissedBlockRewards = row.MissedBlockRewards,
                FutureEndorsements = row.FutureEndorsements,
                FutureEndorsementRewards = row.FutureEndorsementRewards,
                Endorsements = row.Endorsements,
                EndorsementRewardsDelegated = row.EndorsementRewardsDelegated,
                EndorsementRewardsStakedOwn = row.EndorsementRewardsStakedOwn,
                EndorsementRewardsStakedEdge = row.EndorsementRewardsStakedEdge,
                EndorsementRewardsStakedShared = row.EndorsementRewardsStakedShared,
                MissedEndorsements = row.MissedEndorsements,
                MissedEndorsementRewards = row.MissedEndorsementRewards,
                BlockFees = row.BlockFees,
                MissedBlockFees = row.MissedBlockFees,
                DoubleBakingRewards = row.DoubleBakingRewards,
                DoubleBakingLostStaked = row.DoubleBakingLostStaked,
                DoubleBakingLostUnstaked = row.DoubleBakingLostUnstaked,
                DoubleBakingLostExternalStaked = row.DoubleBakingLostExternalStaked,
                DoubleBakingLostExternalUnstaked = row.DoubleBakingLostExternalUnstaked,
                DoubleEndorsingRewards = row.DoubleEndorsingRewards,
                DoubleEndorsingLostStaked = row.DoubleEndorsingLostStaked,
                DoubleEndorsingLostUnstaked = row.DoubleEndorsingLostUnstaked,
                DoubleEndorsingLostExternalStaked = row.DoubleEndorsingLostExternalStaked,
                DoubleEndorsingLostExternalUnstaked = row.DoubleEndorsingLostExternalUnstaked,
                DoublePreendorsingRewards = row.DoublePreendorsingRewards,
                DoublePreendorsingLostStaked = row.DoublePreendorsingLostStaked,
                DoublePreendorsingLostUnstaked = row.DoublePreendorsingLostUnstaked,
                DoublePreendorsingLostExternalStaked = row.DoublePreendorsingLostExternalStaked,
                DoublePreendorsingLostExternalUnstaked = row.DoublePreendorsingLostExternalUnstaked,
                VdfRevelationRewardsDelegated = row.VdfRevelationRewardsDelegated,
                VdfRevelationRewardsStakedOwn = row.VdfRevelationRewardsStakedOwn,
                VdfRevelationRewardsStakedEdge = row.VdfRevelationRewardsStakedEdge,
                VdfRevelationRewardsStakedShared = row.VdfRevelationRewardsStakedShared,
                NonceRevelationRewardsDelegated = row.NonceRevelationRewardsDelegated,
                NonceRevelationRewardsStakedOwn = row.NonceRevelationRewardsStakedOwn,
                NonceRevelationRewardsStakedEdge = row.NonceRevelationRewardsStakedEdge,
                NonceRevelationRewardsStakedShared = row.NonceRevelationRewardsStakedShared,
                NonceRevelationLosses = row.NonceRevelationLosses,
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
                    case "cycle": columns.Add(@"""Cycle"""); break;
                    case "bakingPower": columns.Add(@"""BakingPower"""); break;
                    case "totalBakingPower": columns.Add(@"""TotalBakingPower"""); break;
                    case "ownDelegatedBalance": columns.Add(@"""OwnDelegatedBalance"""); break;
                    case "externalDelegatedBalance": columns.Add(@"""ExternalDelegatedBalance"""); break;
                    case "delegatorsCount": columns.Add(@"""DelegatorsCount"""); break;
                    case "ownStakedBalance": columns.Add(@"""OwnStakedBalance"""); break;
                    case "externalStakedBalance": columns.Add(@"""ExternalStakedBalance"""); break;
                    case "stakersCount": columns.Add(@"""StakersCount"""); break;
                    case "expectedBlocks": columns.Add(@"""ExpectedBlocks"""); break;
                    case "expectedEndorsements": columns.Add(@"""ExpectedEndorsements"""); break;
                    case "futureBlocks": columns.Add(@"""FutureBlocks"""); break;
                    case "futureBlockRewards": columns.Add(@"""FutureBlockRewards"""); break;
                    case "blocks": columns.Add(@"""Blocks"""); break;
                    case "blockRewardsDelegated": columns.Add(@"""BlockRewardsDelegated"""); break;
                    case "blockRewardsStakedOwn": columns.Add(@"""BlockRewardsStakedOwn"""); break;
                    case "blockRewardsStakedEdge": columns.Add(@"""BlockRewardsStakedEdge"""); break;
                    case "blockRewardsStakedShared": columns.Add(@"""BlockRewardsStakedShared"""); break;
                    case "missedBlocks": columns.Add(@"""MissedBlocks"""); break;
                    case "missedBlockRewards": columns.Add(@"""MissedBlockRewards"""); break;
                    case "futureEndorsements": columns.Add(@"""FutureEndorsements"""); break;
                    case "futureEndorsementRewards": columns.Add(@"""FutureEndorsementRewards"""); break;
                    case "endorsements": columns.Add(@"""Endorsements"""); break;
                    case "endorsementRewardsDelegated": columns.Add(@"""EndorsementRewardsDelegated"""); break;
                    case "endorsementRewardsStakedOwn": columns.Add(@"""EndorsementRewardsStakedOwn"""); break;
                    case "endorsementRewardsStakedEdge": columns.Add(@"""EndorsementRewardsStakedEdge"""); break;
                    case "endorsementRewardsStakedShared": columns.Add(@"""EndorsementRewardsStakedShared"""); break;
                    case "missedEndorsements": columns.Add(@"""MissedEndorsements"""); break;
                    case "missedEndorsementRewards": columns.Add(@"""MissedEndorsementRewards"""); break;
                    case "blockFees": columns.Add(@"""BlockFees"""); break;
                    case "missedBlockFees": columns.Add(@"""MissedBlockFees"""); break;
                    case "doubleBakingRewards": columns.Add(@"""DoubleBakingRewards"""); break;
                    case "doubleBakingLostStaked": columns.Add(@"""DoubleBakingLostStaked"""); break;
                    case "doubleBakingLostUnstaked": columns.Add(@"""DoubleBakingLostUnstaked"""); break;
                    case "doubleBakingLostExternalStaked": columns.Add(@"""DoubleBakingLostExternalStaked"""); break;
                    case "doubleBakingLostExternalUnstaked": columns.Add(@"""DoubleBakingLostExternalUnstaked"""); break;
                    case "doubleEndorsingRewards": columns.Add(@"""DoubleEndorsingRewards"""); break;
                    case "doubleEndorsingLostStaked": columns.Add(@"""DoubleEndorsingLostStaked"""); break;
                    case "doubleEndorsingLostUnstaked": columns.Add(@"""DoubleEndorsingLostUnstaked"""); break;
                    case "doubleEndorsingLostExternalStaked": columns.Add(@"""DoubleEndorsingLostExternalStaked"""); break;
                    case "doubleEndorsingLostExternalUnstaked": columns.Add(@"""DoubleEndorsingLostExternalUnstaked"""); break;
                    case "doublePreendorsingRewards": columns.Add(@"""DoublePreendorsingRewards"""); break;
                    case "doublePreendorsingLostStaked": columns.Add(@"""DoublePreendorsingLostStaked"""); break;
                    case "doublePreendorsingLostUnstaked": columns.Add(@"""DoublePreendorsingLostUnstaked"""); break;
                    case "doublePreendorsingLostExternalStaked": columns.Add(@"""DoublePreendorsingLostExternalStaked"""); break;
                    case "doublePreendorsingLostExternalUnstaked": columns.Add(@"""DoublePreendorsingLostExternalUnstaked"""); break;
                    case "vdfRevelationRewardsDelegated": columns.Add(@"""VdfRevelationRewardsDelegated"""); break;
                    case "vdfRevelationRewardsStakedOwn": columns.Add(@"""VdfRevelationRewardsStakedOwn"""); break;
                    case "vdfRevelationRewardsStakedEdge": columns.Add(@"""VdfRevelationRewardsStakedEdge"""); break;
                    case "vdfRevelationRewardsStakedShared": columns.Add(@"""VdfRevelationRewardsStakedShared"""); break;
                    case "nonceRevelationRewardsDelegated": columns.Add(@"""NonceRevelationRewardsDelegated"""); break;
                    case "nonceRevelationRewardsStakedOwn": columns.Add(@"""NonceRevelationRewardsStakedOwn"""); break;
                    case "nonceRevelationRewardsStakedEdge": columns.Add(@"""NonceRevelationRewardsStakedEdge"""); break;
                    case "nonceRevelationRewardsStakedShared": columns.Add(@"""NonceRevelationRewardsStakedShared"""); break;
                    case "nonceRevelationLosses": columns.Add(@"""NonceRevelationLosses"""); break;
                    case "quote": columns.Add(@"""Cycle"""); break;

                    #region deprecated
                    case "blockRewardsLiquid": columns.Add(@"""BlockRewardsDelegated"""); break;
                    case "endorsementRewardsLiquid": columns.Add(@"""EndorsementRewardsDelegated"""); break;
                    case "vdfRevelationRewardsLiquid": columns.Add(@"""VdfRevelationRewardsDelegated"""); break;
                    case "nonceRevelationRewardsLiquid": columns.Add(@"""NonceRevelationRewardsDelegated"""); break;
                    case "revelationRewards":
                        columns.Add(@"""NonceRevelationRewardsDelegated""");
                        columns.Add(@"""NonceRevelationRewardsStakedOwn""");
                        columns.Add(@"""NonceRevelationRewardsStakedEdge""");
                        columns.Add(@"""NonceRevelationRewardsStakedShared""");
                        columns.Add(@"""VdfRevelationRewardsDelegated""");
                        columns.Add(@"""VdfRevelationRewardsStakedOwn""");
                        columns.Add(@"""VdfRevelationRewardsStakedEdge""");
                        columns.Add(@"""VdfRevelationRewardsStakedShared""");
                        break;
                    case "revelationLosses":
                        columns.Add(@"""NonceRevelationLosses""");
                        break;
                    case "doublePreendorsingLosses":
                        columns.Add(@"""DoublePreendorsingLostStaked""");
                        columns.Add(@"""DoublePreendorsingLostExternalStaked""");
                        columns.Add(@"""DoublePreendorsingLostUnstaked""");
                        columns.Add(@"""DoublePreendorsingLostExternalUnstaked""");
                        break;
                    case "doubleEndorsingLosses":
                        columns.Add(@"""DoubleEndorsingLostStaked""");
                        columns.Add(@"""DoubleEndorsingLostExternalStaked""");
                        columns.Add(@"""DoubleEndorsingLostUnstaked""");
                        columns.Add(@"""DoubleEndorsingLostExternalUnstaked""");
                        break;
                    case "doubleBakingLosses":
                        columns.Add(@"""DoubleBakingLostStaked""");
                        columns.Add(@"""DoubleBakingLostExternalStaked""");
                        columns.Add(@"""DoubleBakingLostUnstaked""");
                        columns.Add(@"""DoubleBakingLostExternalUnstaked""");
                        break;
                    case "endorsementRewards":
                        columns.Add(@"""EndorsementRewardsDelegated""");
                        columns.Add(@"""EndorsementRewardsStakedOwn""");
                        columns.Add(@"""EndorsementRewardsStakedEdge""");
                        columns.Add(@"""EndorsementRewardsStakedShared""");
                        break;
                    case "blockRewards":
                        columns.Add(@"""BlockRewardsDelegated""");
                        columns.Add(@"""BlockRewardsStakedOwn""");
                        columns.Add(@"""BlockRewardsStakedEdge""");
                        columns.Add(@"""BlockRewardsStakedShared""");
                        break;
                    case "stakingBalance":
                        columns.Add(@"""OwnDelegatedBalance""");
                        columns.Add(@"""ExternalDelegatedBalance""");
                        columns.Add(@"""OwnStakedBalance""");
                        columns.Add(@"""ExternalStakedBalance""");
                        break;
                    case "activeStake":
                        columns.Add(@"""BakingPower""");
                        break;
                    case "selectedStake":
                        columns.Add(@"""BakingPower""");
                        break;
                    case "delegatedBalance":
                        columns.Add(@"""ExternalDelegatedBalance""");
                        break;
                    case "numDelegators":
                        columns.Add(@"""DelegatorsCount""");
                        break;
                    #endregion
                }
            }

            if (columns.Count == 0)
                return Array.Empty<object[]>();

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""BakerCycles""")
                .Filter("BakerId", baker.Id)
                .Filter("Cycle", cycle)
                .Take(sort ?? new SortParameter { Desc = "cycle" }, offset, limit, x => ("Cycle", "Cycle"));

            await using var db = await DataSource.OpenConnectionAsync();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            var result = new object[rows.Count()][];
            for (int i = 0; i < result.Length; i++)
                result[i] = new object[fields.Length];

            for (int i = 0, j = 0; i < fields.Length; j = 0, i++)
            {
                switch (fields[i])
                {
                    case "cycle":
                        foreach (var row in rows)
                            result[j++][i] = row.Cycle;
                        break;
                    case "bakingPower":
                        foreach (var row in rows)
                            result[j++][i] = row.BakingPower;
                        break;
                    case "totalBakingPower":
                        foreach (var row in rows)
                            result[j++][i] = row.TotalBakingPower;
                        break;
                    case "ownDelegatedBalance":
                        foreach (var row in rows)
                            result[j++][i] = row.OwnDelegatedBalance;
                        break;
                    case "externalDelegatedBalance":
                        foreach (var row in rows)
                            result[j++][i] = row.ExternalDelegatedBalance;
                        break;
                    case "delegatorsCount":
                        foreach (var row in rows)
                            result[j++][i] = row.DelegatorsCount;
                        break;
                    case "ownStakedBalance":
                        foreach (var row in rows)
                            result[j++][i] = row.OwnStakedBalance;
                        break;
                    case "externalStakedBalance":
                        foreach (var row in rows)
                            result[j++][i] = row.ExternalStakedBalance;
                        break;
                    case "stakersCount":
                        foreach (var row in rows)
                            result[j++][i] = row.StakersCount;
                        break;
                    case "expectedBlocks":
                        foreach (var row in rows)
                            result[j++][i] = Math.Round(row.ExpectedBlocks, 2);
                        break;
                    case "expectedEndorsements":
                        foreach (var row in rows)
                            result[j++][i] = Math.Round(row.ExpectedEndorsements, 2);
                        break;
                    case "futureBlocks":
                        foreach (var row in rows)
                            result[j++][i] = row.FutureBlocks;
                        break;
                    case "futureBlockRewards":
                        foreach (var row in rows)
                            result[j++][i] = row.FutureBlockRewards;
                        break;
                    case "blocks":
                        foreach (var row in rows)
                            result[j++][i] = row.Blocks;
                        break;
                    case "blockRewardsDelegated":
                        foreach (var row in rows)
                            result[j++][i] = row.BlockRewardsDelegated;
                        break;
                    case "blockRewardsStakedOwn":
                        foreach (var row in rows)
                            result[j++][i] = row.BlockRewardsStakedOwn;
                        break;
                    case "blockRewardsStakedEdge":
                        foreach (var row in rows)
                            result[j++][i] = row.BlockRewardsStakedEdge;
                        break;
                    case "blockRewardsStakedShared":
                        foreach (var row in rows)
                            result[j++][i] = row.BlockRewardsStakedShared;
                        break;
                    case "missedBlocks":
                        foreach (var row in rows)
                            result[j++][i] = row.MissedBlocks;
                        break;
                    case "missedBlockRewards":
                        foreach (var row in rows)
                            result[j++][i] = row.MissedBlockRewards;
                        break;
                    case "futureEndorsements":
                        foreach (var row in rows)
                            result[j++][i] = row.FutureEndorsements;
                        break;
                    case "futureEndorsementRewards":
                        foreach (var row in rows)
                            result[j++][i] = row.FutureEndorsementRewards;
                        break;
                    case "endorsements":
                        foreach (var row in rows)
                            result[j++][i] = row.Endorsements;
                        break;
                    case "endorsementRewardsDelegated":
                        foreach (var row in rows)
                            result[j++][i] = row.EndorsementRewardsDelegated;
                        break;
                    case "endorsementRewardsStakedOwn":
                        foreach (var row in rows)
                            result[j++][i] = row.EndorsementRewardsStakedOwn;
                        break;
                    case "endorsementRewardsStakedEdge":
                        foreach (var row in rows)
                            result[j++][i] = row.EndorsementRewardsStakedEdge;
                        break;
                    case "endorsementRewardsStakedShared":
                        foreach (var row in rows)
                            result[j++][i] = row.EndorsementRewardsStakedShared;
                        break;
                    case "missedEndorsements":
                        foreach (var row in rows)
                            result[j++][i] = row.MissedEndorsements;
                        break;
                    case "missedEndorsementRewards":
                        foreach (var row in rows)
                            result[j++][i] = row.MissedEndorsementRewards;
                        break;
                    case "blockFees":
                        foreach (var row in rows)
                            result[j++][i] = row.BlockFees;
                        break;
                    case "missedBlockFees":
                        foreach (var row in rows)
                            result[j++][i] = row.MissedBlockFees;
                        break;
                    case "doubleBakingRewards":
                        foreach (var row in rows)
                            result[j++][i] = row.DoubleBakingRewards;
                        break;
                    case "doubleBakingLostStaked":
                        foreach (var row in rows)
                            result[j++][i] = row.DoubleBakingLostStaked;
                        break;
                    case "doubleBakingLostUnstaked":
                        foreach (var row in rows)
                            result[j++][i] = row.DoubleBakingLostUnstaked;
                        break;
                    case "doubleBakingLostExternalStaked":
                        foreach (var row in rows)
                            result[j++][i] = row.DoubleBakingLostExternalStaked;
                        break;
                    case "doubleBakingLostExternalUnstaked":
                        foreach (var row in rows)
                            result[j++][i] = row.DoubleBakingLostExternalUnstaked;
                        break;
                    case "doubleEndorsingRewards":
                        foreach (var row in rows)
                            result[j++][i] = row.DoubleEndorsingRewards;
                        break;
                    case "doubleEndorsingLostStaked":
                        foreach (var row in rows)
                            result[j++][i] = row.DoubleEndorsingLostStaked;
                        break;
                    case "doubleEndorsingLostUnstaked":
                        foreach (var row in rows)
                            result[j++][i] = row.DoubleEndorsingLostUnstaked;
                        break;
                    case "doubleEndorsingLostExternalStaked":
                        foreach (var row in rows)
                            result[j++][i] = row.DoubleEndorsingLostExternalStaked;
                        break;
                    case "doubleEndorsingLostExternalUnstaked":
                        foreach (var row in rows)
                            result[j++][i] = row.DoubleEndorsingLostExternalUnstaked;
                        break;
                    case "doublePreendorsingRewards":
                        foreach (var row in rows)
                            result[j++][i] = row.DoublePreendorsingRewards;
                        break;
                    case "doublePreendorsingLostStaked":
                        foreach (var row in rows)
                            result[j++][i] = row.DoublePreendorsingLostStaked;
                        break;
                    case "doublePreendorsingLostUnstaked":
                        foreach (var row in rows)
                            result[j++][i] = row.DoublePreendorsingLostUnstaked;
                        break;
                    case "doublePreendorsingLostExternalStaked":
                        foreach (var row in rows)
                            result[j++][i] = row.DoublePreendorsingLostExternalStaked;
                        break;
                    case "doublePreendorsingLostExternalUnstaked":
                        foreach (var row in rows)
                            result[j++][i] = row.DoublePreendorsingLostExternalUnstaked;
                        break;
                    case "vdfRevelationRewardsDelegated":
                        foreach (var row in rows)
                            result[j++][i] = row.VdfRevelationRewardsDelegated;
                        break;
                    case "vdfRevelationRewardsStakedOwn":
                        foreach (var row in rows)
                            result[j++][i] = row.VdfRevelationRewardsStakedOwn;
                        break;
                    case "vdfRevelationRewardsStakedEdge":
                        foreach (var row in rows)
                            result[j++][i] = row.VdfRevelationRewardsStakedEdge;
                        break;
                    case "vdfRevelationRewardsStakedShared":
                        foreach (var row in rows)
                            result[j++][i] = row.VdfRevelationRewardsStakedShared;
                        break;
                    case "nonceRevelationRewardsDelegated":
                        foreach (var row in rows)
                            result[j++][i] = row.NonceRevelationRewardsDelegated;
                        break;
                    case "nonceRevelationRewardsStakedOwn":
                        foreach (var row in rows)
                            result[j++][i] = row.NonceRevelationRewardsStakedOwn;
                        break;
                    case "nonceRevelationRewardsStakedEdge":
                        foreach (var row in rows)
                            result[j++][i] = row.NonceRevelationRewardsStakedEdge;
                        break;
                    case "nonceRevelationRewardsStakedShared":
                        foreach (var row in rows)
                            result[j++][i] = row.NonceRevelationRewardsStakedShared;
                        break;
                    case "nonceRevelationLosses":
                        foreach (var row in rows)
                            result[j++][i] = row.NonceRevelationLosses;
                        break;
                    case "quote":
                        foreach (var row in rows)
                            result[j++][i] = Quotes.Get(quote, Protocols.FindByCycle((int)row.Cycle).GetCycleEnd((int)row.Cycle));
                        break;

                    #region deprecated
                    case "blockRewardsLiquid":
                        foreach (var row in rows)
                            result[j++][i] = row.BlockRewardsDelegated;
                        break;
                    case "endorsementRewardsLiquid":
                        foreach (var row in rows)
                            result[j++][i] = row.EndorsementRewardsDelegated;
                        break;
                    case "vdfRevelationRewardsLiquid":
                        foreach (var row in rows)
                            result[j++][i] = row.VdfRevelationRewardsDelegated;
                        break;
                    case "nonceRevelationRewardsLiquid":
                        foreach (var row in rows)
                            result[j++][i] = row.NonceRevelationRewardsDelegated;
                        break;
                    case "revelationRewards":
                        foreach (var row in rows)
                            result[j++][i] = row.NonceRevelationRewardsDelegated + row.NonceRevelationRewardsStakedOwn + row.NonceRevelationRewardsStakedEdge + row.NonceRevelationRewardsStakedShared + row.VdfRevelationRewardsDelegated + row.VdfRevelationRewardsStakedOwn + row.VdfRevelationRewardsStakedEdge + row.VdfRevelationRewardsStakedShared;
                        break;
                    case "revelationLosses":
                        foreach (var row in rows)
                            result[j++][i] = row.NonceRevelationLosses;
                        break;
                    case "doublePreendorsingLosses":
                        foreach (var row in rows)
                            result[j++][i] = row.DoublePreendorsingLostStaked + row.DoublePreendorsingLostExternalStaked + row.DoublePreendorsingLostUnstaked + row.DoublePreendorsingLostExternalUnstaked;
                        break;
                    case "doubleEndorsingLosses":
                        foreach (var row in rows)
                            result[j++][i] = row.DoubleEndorsingLostStaked + row.DoubleEndorsingLostExternalStaked + row.DoubleEndorsingLostUnstaked + row.DoubleEndorsingLostExternalUnstaked;
                        break;
                    case "doubleBakingLosses":
                        foreach (var row in rows)
                            result[j++][i] = row.DoubleBakingLostStaked + row.DoubleBakingLostExternalStaked + row.DoubleBakingLostUnstaked + row.DoubleBakingLostExternalUnstaked;
                        break;
                    case "endorsementRewards":
                        foreach (var row in rows)
                            result[j++][i] = row.EndorsementRewardsDelegated + row.EndorsementRewardsStakedOwn + row.EndorsementRewardsStakedEdge + row.EndorsementRewardsStakedShared;
                        break;
                    case "blockRewards":
                        foreach (var row in rows)
                            result[j++][i] = row.BlockRewardsDelegated + row.BlockRewardsStakedOwn + row.BlockRewardsStakedEdge + row.BlockRewardsStakedShared;
                        break;
                    case "stakingBalance":
                        foreach (var row in rows)
                            result[j++][i] = row.OwnDelegatedBalance + row.ExternalDelegatedBalance + row.OwnStakedBalance + row.ExternalStakedBalance;
                        break;
                    case "activeStake":
                        foreach (var row in rows)
                            result[j++][i] = row.BakingPower;
                        break;
                    case "selectedStake":
                        foreach (var row in rows)
                            result[j++][i] = row.BakingPower;
                        break;
                    case "delegatedBalance":
                        foreach (var row in rows)
                            result[j++][i] = row.ExternalDelegatedBalance;
                        break;
                    case "numDelegators":
                        foreach (var row in rows)
                            result[j++][i] = row.DelegatorsCount;
                        break;
                    #endregion
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
                case "cycle": columns.Add(@"""Cycle"""); break;
                case "bakingPower": columns.Add(@"""BakingPower"""); break;
                case "totalBakingPower": columns.Add(@"""TotalBakingPower"""); break;
                case "ownDelegatedBalance": columns.Add(@"""OwnDelegatedBalance"""); break;
                case "externalDelegatedBalance": columns.Add(@"""ExternalDelegatedBalance"""); break;
                case "delegatorsCount": columns.Add(@"""DelegatorsCount"""); break;
                case "ownStakedBalance": columns.Add(@"""OwnStakedBalance"""); break;
                case "externalStakedBalance": columns.Add(@"""ExternalStakedBalance"""); break;
                case "stakersCount": columns.Add(@"""StakersCount"""); break;
                case "expectedBlocks": columns.Add(@"""ExpectedBlocks"""); break;
                case "expectedEndorsements": columns.Add(@"""ExpectedEndorsements"""); break;
                case "futureBlocks": columns.Add(@"""FutureBlocks"""); break;
                case "futureBlockRewards": columns.Add(@"""FutureBlockRewards"""); break;
                case "blocks": columns.Add(@"""Blocks"""); break;
                case "blockRewardsDelegated": columns.Add(@"""BlockRewardsDelegated"""); break;
                case "blockRewardsStakedOwn": columns.Add(@"""BlockRewardsStakedOwn"""); break;
                case "blockRewardsStakedEdge": columns.Add(@"""BlockRewardsStakedEdge"""); break;
                case "blockRewardsStakedShared": columns.Add(@"""BlockRewardsStakedShared"""); break;
                case "missedBlocks": columns.Add(@"""MissedBlocks"""); break;
                case "missedBlockRewards": columns.Add(@"""MissedBlockRewards"""); break;
                case "futureEndorsements": columns.Add(@"""FutureEndorsements"""); break;
                case "futureEndorsementRewards": columns.Add(@"""FutureEndorsementRewards"""); break;
                case "endorsements": columns.Add(@"""Endorsements"""); break;
                case "endorsementRewardsDelegated": columns.Add(@"""EndorsementRewardsDelegated"""); break;
                case "endorsementRewardsStakedOwn": columns.Add(@"""EndorsementRewardsStakedOwn"""); break;
                case "endorsementRewardsStakedEdge": columns.Add(@"""EndorsementRewardsStakedEdge"""); break;
                case "endorsementRewardsStakedShared": columns.Add(@"""EndorsementRewardsStakedShared"""); break;
                case "missedEndorsements": columns.Add(@"""MissedEndorsements"""); break;
                case "missedEndorsementRewards": columns.Add(@"""MissedEndorsementRewards"""); break;
                case "blockFees": columns.Add(@"""BlockFees"""); break;
                case "missedBlockFees": columns.Add(@"""MissedBlockFees"""); break;
                case "doubleBakingRewards": columns.Add(@"""DoubleBakingRewards"""); break;
                case "doubleBakingLostStaked": columns.Add(@"""DoubleBakingLostStaked"""); break;
                case "doubleBakingLostUnstaked": columns.Add(@"""DoubleBakingLostUnstaked"""); break;
                case "doubleBakingLostExternalStaked": columns.Add(@"""DoubleBakingLostExternalStaked"""); break;
                case "doubleBakingLostExternalUnstaked": columns.Add(@"""DoubleBakingLostExternalUnstaked"""); break;
                case "doubleEndorsingRewards": columns.Add(@"""DoubleEndorsingRewards"""); break;
                case "doubleEndorsingLostStaked": columns.Add(@"""DoubleEndorsingLostStaked"""); break;
                case "doubleEndorsingLostUnstaked": columns.Add(@"""DoubleEndorsingLostUnstaked"""); break;
                case "doubleEndorsingLostExternalStaked": columns.Add(@"""DoubleEndorsingLostExternalStaked"""); break;
                case "doubleEndorsingLostExternalUnstaked": columns.Add(@"""DoubleEndorsingLostExternalUnstaked"""); break;
                case "doublePreendorsingRewards": columns.Add(@"""DoublePreendorsingRewards"""); break;
                case "doublePreendorsingLostStaked": columns.Add(@"""DoublePreendorsingLostStaked"""); break;
                case "doublePreendorsingLostUnstaked": columns.Add(@"""DoublePreendorsingLostUnstaked"""); break;
                case "doublePreendorsingLostExternalStaked": columns.Add(@"""DoublePreendorsingLostExternalStaked"""); break;
                case "doublePreendorsingLostExternalUnstaked": columns.Add(@"""DoublePreendorsingLostExternalUnstaked"""); break;
                case "vdfRevelationRewardsDelegated": columns.Add(@"""VdfRevelationRewardsDelegated"""); break;
                case "vdfRevelationRewardsStakedOwn": columns.Add(@"""VdfRevelationRewardsStakedOwn"""); break;
                case "vdfRevelationRewardsStakedEdge": columns.Add(@"""VdfRevelationRewardsStakedEdge"""); break;
                case "vdfRevelationRewardsStakedShared": columns.Add(@"""VdfRevelationRewardsStakedShared"""); break;
                case "nonceRevelationRewardsDelegated": columns.Add(@"""NonceRevelationRewardsDelegated"""); break;
                case "nonceRevelationRewardsStakedOwn": columns.Add(@"""NonceRevelationRewardsStakedOwn"""); break;
                case "nonceRevelationRewardsStakedEdge": columns.Add(@"""NonceRevelationRewardsStakedEdge"""); break;
                case "nonceRevelationRewardsStakedShared": columns.Add(@"""NonceRevelationRewardsStakedShared"""); break;
                case "nonceRevelationLosses": columns.Add(@"""NonceRevelationLosses"""); break;
                case "quote": columns.Add(@"""Cycle"""); break;

                #region deprecated
                case "blockRewardsLiquid": columns.Add(@"""BlockRewardsDelegated"""); break;
                case "endorsementRewardsLiquid": columns.Add(@"""EndorsementRewardsDelegated"""); break;
                case "vdfRevelationRewardsLiquid": columns.Add(@"""VdfRevelationRewardsDelegated"""); break;
                case "nonceRevelationRewardsLiquid": columns.Add(@"""NonceRevelationRewardsDelegated"""); break;
                case "revelationRewards":
                    columns.Add(@"""NonceRevelationRewardsDelegated""");
                    columns.Add(@"""NonceRevelationRewardsStakedOwn""");
                    columns.Add(@"""NonceRevelationRewardsStakedEdge""");
                    columns.Add(@"""NonceRevelationRewardsStakedShared""");
                    columns.Add(@"""VdfRevelationRewardsDelegated""");
                    columns.Add(@"""VdfRevelationRewardsStakedOwn""");
                    columns.Add(@"""VdfRevelationRewardsStakedEdge""");
                    columns.Add(@"""VdfRevelationRewardsStakedShared""");
                    break;
                case "revelationLosses":
                    columns.Add(@"""NonceRevelationLosses""");
                    break;
                case "doublePreendorsingLosses":
                    columns.Add(@"""DoublePreendorsingLostStaked""");
                    columns.Add(@"""DoublePreendorsingLostExternalStaked""");
                    columns.Add(@"""DoublePreendorsingLostUnstaked""");
                    columns.Add(@"""DoublePreendorsingLostExternalUnstaked""");
                    break;
                case "doubleEndorsingLosses":
                    columns.Add(@"""DoubleEndorsingLostStaked""");
                    columns.Add(@"""DoubleEndorsingLostExternalStaked""");
                    columns.Add(@"""DoubleEndorsingLostUnstaked""");
                    columns.Add(@"""DoubleEndorsingLostExternalUnstaked""");
                    break;
                case "doubleBakingLosses":
                    columns.Add(@"""DoubleBakingLostStaked""");
                    columns.Add(@"""DoubleBakingLostExternalStaked""");
                    columns.Add(@"""DoubleBakingLostUnstaked""");
                    columns.Add(@"""DoubleBakingLostExternalUnstaked""");
                    break;
                case "endorsementRewards":
                    columns.Add(@"""EndorsementRewardsDelegated""");
                    columns.Add(@"""EndorsementRewardsStakedOwn""");
                    columns.Add(@"""EndorsementRewardsStakedEdge""");
                    columns.Add(@"""EndorsementRewardsStakedShared""");
                    break;
                case "blockRewards":
                    columns.Add(@"""BlockRewardsDelegated""");
                    columns.Add(@"""BlockRewardsStakedOwn""");
                    columns.Add(@"""BlockRewardsStakedEdge""");
                    columns.Add(@"""BlockRewardsStakedShared""");
                    break;
                case "stakingBalance":
                    columns.Add(@"""OwnDelegatedBalance""");
                    columns.Add(@"""ExternalDelegatedBalance""");
                    columns.Add(@"""OwnStakedBalance""");
                    columns.Add(@"""ExternalStakedBalance""");
                    break;
                case "activeStake":
                    columns.Add(@"""BakingPower""");
                    break;
                case "selectedStake":
                    columns.Add(@"""BakingPower""");
                    break;
                case "delegatedBalance":
                    columns.Add(@"""ExternalDelegatedBalance""");
                    break;
                case "numDelegators":
                    columns.Add(@"""DelegatorsCount""");
                    break;
                #endregion
            }

            if (columns.Count == 0)
                return Array.Empty<object>();

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""BakerCycles""")
                .Filter("BakerId", baker.Id)
                .Filter("Cycle", cycle)
                .Take(sort ?? new SortParameter { Desc = "cycle" }, offset, limit, x => ("Cycle", "Cycle"));

            await using var db = await DataSource.OpenConnectionAsync();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            //TODO: optimize memory allocation
            var result = new object[rows.Count()];
            var j = 0;

            switch (field)
            {
                case "cycle":
                    foreach (var row in rows)
                        result[j++] = row.Cycle;
                    break;
                case "bakingPower":
                    foreach (var row in rows)
                        result[j++] = row.BakingPower;
                    break;
                case "totalBakingPower":
                    foreach (var row in rows)
                        result[j++] = row.TotalBakingPower;
                    break;
                case "ownDelegatedBalance":
                    foreach (var row in rows)
                        result[j++] = row.OwnDelegatedBalance;
                    break;
                case "externalDelegatedBalance":
                    foreach (var row in rows)
                        result[j++] = row.ExternalDelegatedBalance;
                    break;
                case "delegatorsCount":
                    foreach (var row in rows)
                        result[j++] = row.DelegatorsCount;
                    break;
                case "ownStakedBalance":
                    foreach (var row in rows)
                        result[j++] = row.OwnStakedBalance;
                    break;
                case "externalStakedBalance":
                    foreach (var row in rows)
                        result[j++] = row.ExternalStakedBalance;
                    break;
                case "stakersCount":
                    foreach (var row in rows)
                        result[j++] = row.StakersCount;
                    break;
                case "expectedBlocks":
                    foreach (var row in rows)
                        result[j++] = Math.Round(row.ExpectedBlocks, 2);
                    break;
                case "expectedEndorsements":
                    foreach (var row in rows)
                        result[j++] = Math.Round(row.ExpectedEndorsements, 2);
                    break;
                case "futureBlocks":
                    foreach (var row in rows)
                        result[j++] = row.FutureBlocks;
                    break;
                case "futureBlockRewards":
                    foreach (var row in rows)
                        result[j++] = row.FutureBlockRewards;
                    break;
                case "blocks":
                    foreach (var row in rows)
                        result[j++] = row.Blocks;
                    break;
                case "blockRewardsDelegated":
                    foreach (var row in rows)
                        result[j++] = row.BlockRewardsDelegated;
                    break;
                case "blockRewardsStakedOwn":
                    foreach (var row in rows)
                        result[j++] = row.BlockRewardsStakedOwn;
                    break;
                case "blockRewardsStakedEdge":
                    foreach (var row in rows)
                        result[j++] = row.BlockRewardsStakedEdge;
                    break;
                case "blockRewardsStakedShared":
                    foreach (var row in rows)
                        result[j++] = row.BlockRewardsStakedShared;
                    break;
                case "missedBlocks":
                    foreach (var row in rows)
                        result[j++] = row.MissedBlocks;
                    break;
                case "missedBlockRewards":
                    foreach (var row in rows)
                        result[j++] = row.MissedBlockRewards;
                    break;
                case "futureEndorsements":
                    foreach (var row in rows)
                        result[j++] = row.FutureEndorsements;
                    break;
                case "futureEndorsementRewards":
                    foreach (var row in rows)
                        result[j++] = row.FutureEndorsementRewards;
                    break;
                case "endorsements":
                    foreach (var row in rows)
                        result[j++] = row.Endorsements;
                    break;
                case "endorsementRewardsDelegated":
                    foreach (var row in rows)
                        result[j++] = row.EndorsementRewardsDelegated;
                    break;
                case "endorsementRewardsStakedOwn":
                    foreach (var row in rows)
                        result[j++] = row.EndorsementRewardsStakedOwn;
                    break;
                case "endorsementRewardsStakedEdge":
                    foreach (var row in rows)
                        result[j++] = row.EndorsementRewardsStakedEdge;
                    break;
                case "endorsementRewardsStakedShared":
                    foreach (var row in rows)
                        result[j++] = row.EndorsementRewardsStakedShared;
                    break;
                case "missedEndorsements":
                    foreach (var row in rows)
                        result[j++] = row.MissedEndorsements;
                    break;
                case "missedEndorsementRewards":
                    foreach (var row in rows)
                        result[j++] = row.MissedEndorsementRewards;
                    break;
                case "blockFees":
                    foreach (var row in rows)
                        result[j++] = row.BlockFees;
                    break;
                case "missedBlockFees":
                    foreach (var row in rows)
                        result[j++] = row.MissedBlockFees;
                    break;
                case "doubleBakingRewards":
                    foreach (var row in rows)
                        result[j++] = row.DoubleBakingRewards;
                    break;
                case "doubleBakingLostStaked":
                    foreach (var row in rows)
                        result[j++] = row.DoubleBakingLostStaked;
                    break;
                case "doubleBakingLostUnstaked":
                    foreach (var row in rows)
                        result[j++] = row.DoubleBakingLostUnstaked;
                    break;
                case "doubleBakingLostExternalStaked":
                    foreach (var row in rows)
                        result[j++] = row.DoubleBakingLostExternalStaked;
                    break;
                case "doubleBakingLostExternalUnstaked":
                    foreach (var row in rows)
                        result[j++] = row.DoubleBakingLostExternalUnstaked;
                    break;
                case "doubleEndorsingRewards":
                    foreach (var row in rows)
                        result[j++] = row.DoubleEndorsingRewards;
                    break;
                case "doubleEndorsingLostStaked":
                    foreach (var row in rows)
                        result[j++] = row.DoubleEndorsingLostStaked;
                    break;
                case "doubleEndorsingLostUnstaked":
                    foreach (var row in rows)
                        result[j++] = row.DoubleEndorsingLostUnstaked;
                    break;
                case "doubleEndorsingLostExternalStaked":
                    foreach (var row in rows)
                        result[j++] = row.DoubleEndorsingLostExternalStaked;
                    break;
                case "doubleEndorsingLostExternalUnstaked":
                    foreach (var row in rows)
                        result[j++] = row.DoubleEndorsingLostExternalUnstaked;
                    break;
                case "doublePreendorsingRewards":
                    foreach (var row in rows)
                        result[j++] = row.DoublePreendorsingRewards;
                    break;
                case "doublePreendorsingLostStaked":
                    foreach (var row in rows)
                        result[j++] = row.DoublePreendorsingLostStaked;
                    break;
                case "doublePreendorsingLostUnstaked":
                    foreach (var row in rows)
                        result[j++] = row.DoublePreendorsingLostUnstaked;
                    break;
                case "doublePreendorsingLostExternalStaked":
                    foreach (var row in rows)
                        result[j++] = row.DoublePreendorsingLostExternalStaked;
                    break;
                case "doublePreendorsingLostExternalUnstaked":
                    foreach (var row in rows)
                        result[j++] = row.DoublePreendorsingLostExternalUnstaked;
                    break;
                case "vdfRevelationRewardsDelegated":
                    foreach (var row in rows)
                        result[j++] = row.VdfRevelationRewardsDelegated;
                    break;
                case "vdfRevelationRewardsStakedOwn":
                    foreach (var row in rows)
                        result[j++] = row.VdfRevelationRewardsStakedOwn;
                    break;
                case "vdfRevelationRewardsStakedEdge":
                    foreach (var row in rows)
                        result[j++] = row.VdfRevelationRewardsStakedEdge;
                    break;
                case "vdfRevelationRewardsStakedShared":
                    foreach (var row in rows)
                        result[j++] = row.VdfRevelationRewardsStakedShared;
                    break;
                case "nonceRevelationRewardsDelegated":
                    foreach (var row in rows)
                        result[j++] = row.NonceRevelationRewardsDelegated;
                    break;
                case "nonceRevelationRewardsStakedOwn":
                    foreach (var row in rows)
                        result[j++] = row.NonceRevelationRewardsStakedOwn;
                    break;
                case "nonceRevelationRewardsStakedEdge":
                    foreach (var row in rows)
                        result[j++] = row.NonceRevelationRewardsStakedEdge;
                    break;
                case "nonceRevelationRewardsStakedShared":
                    foreach (var row in rows)
                        result[j++] = row.NonceRevelationRewardsStakedShared;
                    break;
                case "nonceRevelationLosses":
                    foreach (var row in rows)
                        result[j++] = row.NonceRevelationLosses;
                    break;
                case "quote":
                    foreach (var row in rows)
                        result[j++] = Quotes.Get(quote, Protocols.FindByCycle((int)row.Cycle).GetCycleEnd((int)row.Cycle));
                    break;

                #region deprecated
                case "blockRewardsLiquid":
                    foreach (var row in rows)
                        result[j++] = row.BlockRewardsDelegated;
                    break;
                case "endorsementRewardsLiquid":
                    foreach (var row in rows)
                        result[j++] = row.EndorsementRewardsDelegated;
                    break;
                case "vdfRevelationRewardsLiquid":
                    foreach (var row in rows)
                        result[j++] = row.VdfRevelationRewardsDelegated;
                    break;
                case "nonceRevelationRewardsLiquid":
                    foreach (var row in rows)
                        result[j++] = row.NonceRevelationRewardsDelegated;
                    break;
                case "revelationRewards":
                    foreach (var row in rows)
                        result[j++] = row.NonceRevelationRewardsDelegated + row.NonceRevelationRewardsStakedOwn + row.NonceRevelationRewardsStakedEdge + row.NonceRevelationRewardsStakedShared + row.VdfRevelationRewardsDelegated + row.VdfRevelationRewardsStakedOwn + row.VdfRevelationRewardsStakedEdge + row.VdfRevelationRewardsStakedShared;
                    break;
                case "revelationLosses":
                    foreach (var row in rows)
                        result[j++] = row.NonceRevelationLosses;
                    break;
                case "doublePreendorsingLosses":
                    foreach (var row in rows)
                        result[j++] = row.DoublePreendorsingLostStaked + row.DoublePreendorsingLostExternalStaked + row.DoublePreendorsingLostUnstaked + row.DoublePreendorsingLostExternalUnstaked;
                    break;
                case "doubleEndorsingLosses":
                    foreach (var row in rows)
                        result[j++] = row.DoubleEndorsingLostStaked + row.DoubleEndorsingLostExternalStaked + row.DoubleEndorsingLostUnstaked + row.DoubleEndorsingLostExternalUnstaked;
                    break;
                case "doubleBakingLosses":
                    foreach (var row in rows)
                        result[j++] = row.DoubleBakingLostStaked + row.DoubleBakingLostExternalStaked + row.DoubleBakingLostUnstaked + row.DoubleBakingLostExternalUnstaked;
                    break;
                case "endorsementRewards":
                    foreach (var row in rows)
                        result[j++] = row.EndorsementRewardsDelegated + row.EndorsementRewardsStakedOwn + row.EndorsementRewardsStakedEdge + row.EndorsementRewardsStakedShared;
                    break;
                case "blockRewards":
                    foreach (var row in rows)
                        result[j++] = row.BlockRewardsDelegated + row.BlockRewardsStakedOwn + row.BlockRewardsStakedEdge + row.BlockRewardsStakedShared;
                    break;
                case "stakingBalance":
                    foreach (var row in rows)
                        result[j++] = row.OwnDelegatedBalance + row.ExternalDelegatedBalance + row.OwnStakedBalance + row.ExternalStakedBalance;
                    break;
                case "activeStake":
                    foreach (var row in rows)
                        result[j++] = row.BakingPower;
                    break;
                case "selectedStake":
                    foreach (var row in rows)
                        result[j++] = row.BakingPower;
                    break;
                case "delegatedBalance":
                    foreach (var row in rows)
                        result[j++] = row.ExternalDelegatedBalance;
                    break;
                case "numDelegators":
                    foreach (var row in rows)
                        result[j++] = row.DelegatorsCount;
                    break;
                #endregion
            }

            return result;
        }
        #endregion

        #region delegator
        public async Task<int> GetDelegatorRewardsCount(string address)
        {
            var acc = await Accounts.GetAsync(address);
            if (acc == null) return 0;

            await using var db = await DataSource.OpenConnectionAsync();
            return await db.QueryFirstAsync<int>($@"SELECT COUNT(*) FROM ""DelegatorCycles"" WHERE ""DelegatorId"" = {acc.Id}");
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

            var sql = new SqlBuilder("""
                SELECT      bc.*, dc."DelegatedBalance", dc."StakedBalance"
                FROM        "DelegatorCycles" as dc
                INNER JOIN  "BakerCycles" as bc
                        ON  bc."BakerId" = dc."BakerId"
                       AND  bc."Cycle" = dc."Cycle"
                """)
                .FilterA(@"dc.""DelegatorId""", acc.Id)
                .FilterA(@"dc.""Cycle""", cycle)
                .Take(sort ?? new SortParameter { Desc = "cycle" }, offset, limit, x => ("Cycle", "Cycle"), "dc");

            await using var db = await DataSource.OpenConnectionAsync();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            return rows.Select(row => new DelegatorRewards
            {
                Cycle = row.Cycle,
                DelegatedBalance = row.DelegatedBalance,
                StakedBalance = row.StakedBalance,
                Baker = Accounts.GetAlias(row.BakerId),
                BakingPower = row.BakingPower,
                TotalBakingPower = row.TotalBakingPower,
                BakerDelegatedBalance = row.OwnDelegatedBalance,
                ExternalDelegatedBalance = row.ExternalDelegatedBalance,
                BakerStakedBalance = row.OwnStakedBalance,
                ExternalStakedBalance = row.ExternalStakedBalance,
                ExpectedBlocks = Math.Round(row.ExpectedBlocks, 2),
                ExpectedEndorsements = Math.Round(row.ExpectedEndorsements, 2),
                FutureBlocks = row.FutureBlocks,
                FutureBlockRewards = row.FutureBlockRewards,
                Blocks = row.Blocks,
                BlockRewardsDelegated = row.BlockRewardsDelegated,
                BlockRewardsStakedOwn = row.BlockRewardsStakedOwn,
                BlockRewardsStakedEdge = row.BlockRewardsStakedEdge,
                BlockRewardsStakedShared = row.BlockRewardsStakedShared,
                MissedBlocks = row.MissedBlocks,
                MissedBlockRewards = row.MissedBlockRewards,
                FutureEndorsements = row.FutureEndorsements,
                FutureEndorsementRewards = row.FutureEndorsementRewards,
                Endorsements = row.Endorsements,
                EndorsementRewardsDelegated = row.EndorsementRewardsDelegated,
                EndorsementRewardsStakedOwn = row.EndorsementRewardsStakedOwn,
                EndorsementRewardsStakedEdge = row.EndorsementRewardsStakedEdge,
                EndorsementRewardsStakedShared = row.EndorsementRewardsStakedShared,
                MissedEndorsements = row.MissedEndorsements,
                MissedEndorsementRewards = row.MissedEndorsementRewards,
                BlockFees = row.BlockFees,
                MissedBlockFees = row.MissedBlockFees,
                DoubleBakingRewards = row.DoubleBakingRewards,
                DoubleBakingLostStaked = row.DoubleBakingLostStaked,
                DoubleBakingLostUnstaked = row.DoubleBakingLostUnstaked,
                DoubleBakingLostExternalStaked = row.DoubleBakingLostExternalStaked,
                DoubleBakingLostExternalUnstaked = row.DoubleBakingLostExternalUnstaked,
                DoubleEndorsingRewards = row.DoubleEndorsingRewards,
                DoubleEndorsingLostStaked = row.DoubleEndorsingLostStaked,
                DoubleEndorsingLostUnstaked = row.DoubleEndorsingLostUnstaked,
                DoubleEndorsingLostExternalStaked = row.DoubleEndorsingLostExternalStaked,
                DoubleEndorsingLostExternalUnstaked = row.DoubleEndorsingLostExternalUnstaked,
                DoublePreendorsingRewards = row.DoublePreendorsingRewards,
                DoublePreendorsingLostStaked = row.DoublePreendorsingLostStaked,
                DoublePreendorsingLostUnstaked = row.DoublePreendorsingLostUnstaked,
                DoublePreendorsingLostExternalStaked = row.DoublePreendorsingLostExternalStaked,
                DoublePreendorsingLostExternalUnstaked = row.DoublePreendorsingLostExternalUnstaked,
                VdfRevelationRewardsDelegated = row.VdfRevelationRewardsDelegated,
                VdfRevelationRewardsStakedOwn = row.VdfRevelationRewardsStakedOwn,
                VdfRevelationRewardsStakedEdge = row.VdfRevelationRewardsStakedEdge,
                VdfRevelationRewardsStakedShared = row.VdfRevelationRewardsStakedShared,
                NonceRevelationRewardsDelegated = row.NonceRevelationRewardsDelegated,
                NonceRevelationRewardsStakedOwn = row.NonceRevelationRewardsStakedOwn,
                NonceRevelationRewardsStakedEdge = row.NonceRevelationRewardsStakedEdge,
                NonceRevelationRewardsStakedShared = row.NonceRevelationRewardsStakedShared,
                NonceRevelationLosses = row.NonceRevelationLosses,
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
            foreach (var field in fields)
            {
                switch (field)
                {
                    case "cycle": columns.Add(@"dc.""Cycle"""); break;
                    case "delegatedBalance": columns.Add(@"dc.""DelegatedBalance"""); break;
                    case "stakedBalance": columns.Add(@"dc.""StakedBalance"""); break;
                    case "baker": columns.Add(@"dc.""BakerId"""); break;
                    case "bakingPower": columns.Add(@"""BakingPower"""); break;
                    case "totalBakingPower": columns.Add(@"""TotalBakingPower"""); break;
                    case "bakerDelegatedBalance": columns.Add(@"""OwnDelegatedBalance"""); break;
                    case "externalDelegatedBalance": columns.Add(@"""ExternalDelegatedBalance"""); break;
                    case "bakerStakedBalance": columns.Add(@"""OwnStakedBalance"""); break;
                    case "externalStakedBalance": columns.Add(@"""ExternalStakedBalance"""); break;
                    case "expectedBlocks": columns.Add(@"""ExpectedBlocks"""); break;
                    case "expectedEndorsements": columns.Add(@"""ExpectedEndorsements"""); break;
                    case "futureBlocks": columns.Add(@"""FutureBlocks"""); break;
                    case "futureBlockRewards": columns.Add(@"""FutureBlockRewards"""); break;
                    case "blocks": columns.Add(@"""Blocks"""); break;
                    case "blockRewardsDelegated": columns.Add(@"""BlockRewardsDelegated"""); break;
                    case "blockRewardsStakedOwn": columns.Add(@"""BlockRewardsStakedOwn"""); break;
                    case "blockRewardsStakedEdge": columns.Add(@"""BlockRewardsStakedEdge"""); break;
                    case "blockRewardsStakedShared": columns.Add(@"""BlockRewardsStakedShared"""); break;
                    case "missedBlocks": columns.Add(@"""MissedBlocks"""); break;
                    case "missedBlockRewards": columns.Add(@"""MissedBlockRewards"""); break;
                    case "futureEndorsements": columns.Add(@"""FutureEndorsements"""); break;
                    case "futureEndorsementRewards": columns.Add(@"""FutureEndorsementRewards"""); break;
                    case "endorsements": columns.Add(@"""Endorsements"""); break;
                    case "endorsementRewardsDelegated": columns.Add(@"""EndorsementRewardsDelegated"""); break;
                    case "endorsementRewardsStakedOwn": columns.Add(@"""EndorsementRewardsStakedOwn"""); break;
                    case "endorsementRewardsStakedEdge": columns.Add(@"""EndorsementRewardsStakedEdge"""); break;
                    case "endorsementRewardsStakedShared": columns.Add(@"""EndorsementRewardsStakedShared"""); break;
                    case "missedEndorsements": columns.Add(@"""MissedEndorsements"""); break;
                    case "missedEndorsementRewards": columns.Add(@"""MissedEndorsementRewards"""); break;
                    case "blockFees": columns.Add(@"""BlockFees"""); break;
                    case "missedBlockFees": columns.Add(@"""MissedBlockFees"""); break;
                    case "doubleBakingRewards": columns.Add(@"""DoubleBakingRewards"""); break;
                    case "doubleBakingLostStaked": columns.Add(@"""DoubleBakingLostStaked"""); break;
                    case "doubleBakingLostUnstaked": columns.Add(@"""DoubleBakingLostUnstaked"""); break;
                    case "doubleBakingLostExternalStaked": columns.Add(@"""DoubleBakingLostExternalStaked"""); break;
                    case "doubleBakingLostExternalUnstaked": columns.Add(@"""DoubleBakingLostExternalUnstaked"""); break;
                    case "doubleEndorsingRewards": columns.Add(@"""DoubleEndorsingRewards"""); break;
                    case "doubleEndorsingLostStaked": columns.Add(@"""DoubleEndorsingLostStaked"""); break;
                    case "doubleEndorsingLostUnstaked": columns.Add(@"""DoubleEndorsingLostUnstaked"""); break;
                    case "doubleEndorsingLostExternalStaked": columns.Add(@"""DoubleEndorsingLostExternalStaked"""); break;
                    case "doubleEndorsingLostExternalUnstaked": columns.Add(@"""DoubleEndorsingLostExternalUnstaked"""); break;
                    case "doublePreendorsingRewards": columns.Add(@"""DoublePreendorsingRewards"""); break;
                    case "doublePreendorsingLostStaked": columns.Add(@"""DoublePreendorsingLostStaked"""); break;
                    case "doublePreendorsingLostUnstaked": columns.Add(@"""DoublePreendorsingLostUnstaked"""); break;
                    case "doublePreendorsingLostExternalStaked": columns.Add(@"""DoublePreendorsingLostExternalStaked"""); break;
                    case "doublePreendorsingLostExternalUnstaked": columns.Add(@"""DoublePreendorsingLostExternalUnstaked"""); break;
                    case "vdfRevelationRewardsDelegated": columns.Add(@"""VdfRevelationRewardsDelegated"""); break;
                    case "vdfRevelationRewardsStakedOwn": columns.Add(@"""VdfRevelationRewardsStakedOwn"""); break;
                    case "vdfRevelationRewardsStakedEdge": columns.Add(@"""VdfRevelationRewardsStakedEdge"""); break;
                    case "vdfRevelationRewardsStakedShared": columns.Add(@"""VdfRevelationRewardsStakedShared"""); break;
                    case "nonceRevelationRewardsDelegated": columns.Add(@"""NonceRevelationRewardsDelegated"""); break;
                    case "nonceRevelationRewardsStakedOwn": columns.Add(@"""NonceRevelationRewardsStakedOwn"""); break;
                    case "nonceRevelationRewardsStakedEdge": columns.Add(@"""NonceRevelationRewardsStakedEdge"""); break;
                    case "nonceRevelationRewardsStakedShared": columns.Add(@"""NonceRevelationRewardsStakedShared"""); break;
                    case "nonceRevelationLosses": columns.Add(@"""NonceRevelationLosses"""); break;
                    case "quote": columns.Add(@"dc.""Cycle"""); break;

                    #region deprecated
                    case "blockRewardsLiquid": columns.Add(@"""BlockRewardsDelegated"""); break;
                    case "endorsementRewardsLiquid": columns.Add(@"""EndorsementRewardsDelegated"""); break;
                    case "vdfRevelationRewardsLiquid": columns.Add(@"""VdfRevelationRewardsDelegated"""); break;
                    case "nonceRevelationRewardsLiquid": columns.Add(@"""NonceRevelationRewardsDelegated"""); break;
                    case "revelationRewards":
                        columns.Add(@"""NonceRevelationRewardsDelegated""");
                        columns.Add(@"""NonceRevelationRewardsStakedOwn""");
                        columns.Add(@"""NonceRevelationRewardsStakedEdge""");
                        columns.Add(@"""NonceRevelationRewardsStakedShared""");
                        columns.Add(@"""VdfRevelationRewardsDelegated""");
                        columns.Add(@"""VdfRevelationRewardsStakedOwn""");
                        columns.Add(@"""VdfRevelationRewardsStakedEdge""");
                        columns.Add(@"""VdfRevelationRewardsStakedShared""");
                        break;
                    case "revelationLosses":
                        columns.Add(@"""NonceRevelationLosses""");
                        break;
                    case "doublePreendorsingLosses":
                        columns.Add(@"""DoublePreendorsingLostStaked""");
                        columns.Add(@"""DoublePreendorsingLostExternalStaked""");
                        columns.Add(@"""DoublePreendorsingLostUnstaked""");
                        columns.Add(@"""DoublePreendorsingLostExternalUnstaked""");
                        break;
                    case "doubleEndorsingLosses":
                        columns.Add(@"""DoubleEndorsingLostStaked""");
                        columns.Add(@"""DoubleEndorsingLostExternalStaked""");
                        columns.Add(@"""DoubleEndorsingLostUnstaked""");
                        columns.Add(@"""DoubleEndorsingLostExternalUnstaked""");
                        break;
                    case "doubleBakingLosses":
                        columns.Add(@"""DoubleBakingLostStaked""");
                        columns.Add(@"""DoubleBakingLostExternalStaked""");
                        columns.Add(@"""DoubleBakingLostUnstaked""");
                        columns.Add(@"""DoubleBakingLostExternalUnstaked""");
                        break;
                    case "endorsementRewards":
                        columns.Add(@"""EndorsementRewardsDelegated""");
                        columns.Add(@"""EndorsementRewardsStakedOwn""");
                        columns.Add(@"""EndorsementRewardsStakedEdge""");
                        columns.Add(@"""EndorsementRewardsStakedShared""");
                        break;
                    case "blockRewards":
                        columns.Add(@"""BlockRewardsDelegated""");
                        columns.Add(@"""BlockRewardsStakedOwn""");
                        columns.Add(@"""BlockRewardsStakedEdge""");
                        columns.Add(@"""BlockRewardsStakedShared""");
                        break;
                    case "stakingBalance":
                        columns.Add(@"""OwnDelegatedBalance""");
                        columns.Add(@"""ExternalDelegatedBalance""");
                        columns.Add(@"""OwnStakedBalance""");
                        columns.Add(@"""ExternalStakedBalance""");
                        break;
                    case "activeStake":
                        columns.Add(@"""BakingPower""");
                        break;
                    case "selectedStake":
                        columns.Add(@"""BakingPower""");
                        break;
                    case "balance":
                        columns.Add(@"dc.""DelegatedBalance""");
                        columns.Add(@"dc.""StakedBalance""");
                        break;
                    #endregion
                }
            }

            if (columns.Count == 0)
                return Array.Empty<object[]>();

            var sql = new SqlBuilder($"""
                SELECT      {string.Join(',', columns)}
                FROM        "DelegatorCycles" as dc
                INNER JOIN  "BakerCycles" as bc
                        ON  bc."BakerId" = dc."BakerId"
                       AND  bc."Cycle" = dc."Cycle"
                """)
                .FilterA(@"dc.""DelegatorId""", acc.Id)
                .FilterA(@"dc.""Cycle""", cycle)
                .Take(sort ?? new SortParameter { Desc = "cycle" }, offset, limit, x => ("Cycle", "Cycle"), "dc");

            await using var db = await DataSource.OpenConnectionAsync();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            var result = new object[rows.Count()][];
            for (int i = 0; i < result.Length; i++)
                result[i] = new object[fields.Length];

            for (int i = 0, j = 0; i < fields.Length; j = 0, i++)
            {
                switch (fields[i])
                {
                    case "cycle":
                        foreach (var row in rows)
                            result[j++][i] = row.Cycle;
                        break;
                    case "delegatedBalance":
                        foreach (var row in rows)
                            result[j++][i] = row.DelegatedBalance;
                        break;
                    case "stakedBalance":
                        foreach (var row in rows)
                            result[j++][i] = row.StakedBalance;
                        break;
                    case "baker":
                        foreach (var row in rows)
                            result[j++][i] = Accounts.GetAlias(row.BakerId);
                        break;
                    case "bakingPower":
                        foreach (var row in rows)
                            result[j++][i] = row.BakingPower;
                        break;
                    case "totalBakingPower":
                        foreach (var row in rows)
                            result[j++][i] = row.TotalBakingPower;
                        break;
                    case "bakerDelegatedBalance":
                        foreach (var row in rows)
                            result[j++][i] = row.OwnDelegatedBalance;
                        break;
                    case "externalDelegatedBalance":
                        foreach (var row in rows)
                            result[j++][i] = row.ExternalDelegatedBalance;
                        break;
                    case "bakerStakedBalance":
                        foreach (var row in rows)
                            result[j++][i] = row.OwnStakedBalance;
                        break;
                    case "externalStakedBalance":
                        foreach (var row in rows)
                            result[j++][i] = row.ExternalStakedBalance;
                        break;
                    case "expectedBlocks":
                        foreach (var row in rows)
                            result[j++][i] = Math.Round(row.ExpectedBlocks, 2);
                        break;
                    case "expectedEndorsements":
                        foreach (var row in rows)
                            result[j++][i] = Math.Round(row.ExpectedEndorsements, 2);
                        break;
                    case "futureBlocks":
                        foreach (var row in rows)
                            result[j++][i] = row.FutureBlocks;
                        break;
                    case "futureBlockRewards":
                        foreach (var row in rows)
                            result[j++][i] = row.FutureBlockRewards;
                        break;
                    case "blocks":
                        foreach (var row in rows)
                            result[j++][i] = row.Blocks;
                        break;
                    case "blockRewardsDelegated":
                        foreach (var row in rows)
                            result[j++][i] = row.BlockRewardsDelegated;
                        break;
                    case "blockRewardsStakedOwn":
                        foreach (var row in rows)
                            result[j++][i] = row.BlockRewardsStakedOwn;
                        break;
                    case "blockRewardsStakedEdge":
                        foreach (var row in rows)
                            result[j++][i] = row.BlockRewardsStakedEdge;
                        break;
                    case "blockRewardsStakedShared":
                        foreach (var row in rows)
                            result[j++][i] = row.BlockRewardsStakedShared;
                        break;
                    case "missedBlocks":
                        foreach (var row in rows)
                            result[j++][i] = row.MissedBlocks;
                        break;
                    case "missedBlockRewards":
                        foreach (var row in rows)
                            result[j++][i] = row.MissedBlockRewards;
                        break;
                    case "futureEndorsements":
                        foreach (var row in rows)
                            result[j++][i] = row.FutureEndorsements;
                        break;
                    case "futureEndorsementRewards":
                        foreach (var row in rows)
                            result[j++][i] = row.FutureEndorsementRewards;
                        break;
                    case "endorsements":
                        foreach (var row in rows)
                            result[j++][i] = row.Endorsements;
                        break;
                    case "endorsementRewardsDelegated":
                        foreach (var row in rows)
                            result[j++][i] = row.EndorsementRewardsDelegated;
                        break;
                    case "endorsementRewardsStakedOwn":
                        foreach (var row in rows)
                            result[j++][i] = row.EndorsementRewardsStakedOwn;
                        break;
                    case "endorsementRewardsStakedEdge":
                        foreach (var row in rows)
                            result[j++][i] = row.EndorsementRewardsStakedEdge;
                        break;
                    case "endorsementRewardsStakedShared":
                        foreach (var row in rows)
                            result[j++][i] = row.EndorsementRewardsStakedShared;
                        break;
                    case "missedEndorsements":
                        foreach (var row in rows)
                            result[j++][i] = row.MissedEndorsements;
                        break;
                    case "missedEndorsementRewards":
                        foreach (var row in rows)
                            result[j++][i] = row.MissedEndorsementRewards;
                        break;
                    case "blockFees":
                        foreach (var row in rows)
                            result[j++][i] = row.BlockFees;
                        break;
                    case "missedBlockFees":
                        foreach (var row in rows)
                            result[j++][i] = row.MissedBlockFees;
                        break;
                    case "doubleBakingRewards":
                        foreach (var row in rows)
                            result[j++][i] = row.DoubleBakingRewards;
                        break;
                    case "doubleBakingLostStaked":
                        foreach (var row in rows)
                            result[j++][i] = row.DoubleBakingLostStaked;
                        break;
                    case "doubleBakingLostUnstaked":
                        foreach (var row in rows)
                            result[j++][i] = row.DoubleBakingLostUnstaked;
                        break;
                    case "doubleBakingLostExternalStaked":
                        foreach (var row in rows)
                            result[j++][i] = row.DoubleBakingLostExternalStaked;
                        break;
                    case "doubleBakingLostExternalUnstaked":
                        foreach (var row in rows)
                            result[j++][i] = row.DoubleBakingLostExternalUnstaked;
                        break;
                    case "doubleEndorsingRewards":
                        foreach (var row in rows)
                            result[j++][i] = row.DoubleEndorsingRewards;
                        break;
                    case "doubleEndorsingLostStaked":
                        foreach (var row in rows)
                            result[j++][i] = row.DoubleEndorsingLostStaked;
                        break;
                    case "doubleEndorsingLostUnstaked":
                        foreach (var row in rows)
                            result[j++][i] = row.DoubleEndorsingLostUnstaked;
                        break;
                    case "doubleEndorsingLostExternalStaked":
                        foreach (var row in rows)
                            result[j++][i] = row.DoubleEndorsingLostExternalStaked;
                        break;
                    case "doubleEndorsingLostExternalUnstaked":
                        foreach (var row in rows)
                            result[j++][i] = row.DoubleEndorsingLostExternalUnstaked;
                        break;
                    case "doublePreendorsingRewards":
                        foreach (var row in rows)
                            result[j++][i] = row.DoublePreendorsingRewards;
                        break;
                    case "doublePreendorsingLostStaked":
                        foreach (var row in rows)
                            result[j++][i] = row.DoublePreendorsingLostStaked;
                        break;
                    case "doublePreendorsingLostUnstaked":
                        foreach (var row in rows)
                            result[j++][i] = row.DoublePreendorsingLostUnstaked;
                        break;
                    case "doublePreendorsingLostExternalStaked":
                        foreach (var row in rows)
                            result[j++][i] = row.DoublePreendorsingLostExternalStaked;
                        break;
                    case "doublePreendorsingLostExternalUnstaked":
                        foreach (var row in rows)
                            result[j++][i] = row.DoublePreendorsingLostExternalUnstaked;
                        break;
                    case "vdfRevelationRewardsDelegated":
                        foreach (var row in rows)
                            result[j++][i] = row.VdfRevelationRewardsDelegated;
                        break;
                    case "vdfRevelationRewardsStakedOwn":
                        foreach (var row in rows)
                            result[j++][i] = row.VdfRevelationRewardsStakedOwn;
                        break;
                    case "vdfRevelationRewardsStakedEdge":
                        foreach (var row in rows)
                            result[j++][i] = row.VdfRevelationRewardsStakedEdge;
                        break;
                    case "vdfRevelationRewardsStakedShared":
                        foreach (var row in rows)
                            result[j++][i] = row.VdfRevelationRewardsStakedShared;
                        break;
                    case "nonceRevelationRewardsDelegated":
                        foreach (var row in rows)
                            result[j++][i] = row.NonceRevelationRewardsDelegated;
                        break;
                    case "nonceRevelationRewardsStakedOwn":
                        foreach (var row in rows)
                            result[j++][i] = row.NonceRevelationRewardsStakedOwn;
                        break;
                    case "nonceRevelationRewardsStakedEdge":
                        foreach (var row in rows)
                            result[j++][i] = row.NonceRevelationRewardsStakedEdge;
                        break;
                    case "nonceRevelationRewardsStakedShared":
                        foreach (var row in rows)
                            result[j++][i] = row.NonceRevelationRewardsStakedShared;
                        break;
                    case "nonceRevelationLosses":
                        foreach (var row in rows)
                            result[j++][i] = row.NonceRevelationLosses;
                        break;
                    case "quote":
                        foreach (var row in rows)
                            result[j++][i] = Quotes.Get(quote, Protocols.FindByCycle((int)row.Cycle).GetCycleEnd((int)row.Cycle));
                        break;

                    #region deprecated
                    case "blockRewardsLiquid":
                        foreach (var row in rows)
                            result[j++][i] = row.BlockRewardsDelegated;
                        break;
                    case "endorsementRewardsLiquid":
                        foreach (var row in rows)
                            result[j++][i] = row.EndorsementRewardsDelegated;
                        break;
                    case "vdfRevelationRewardsLiquid":
                        foreach (var row in rows)
                            result[j++][i] = row.VdfRevelationRewardsDelegated;
                        break;
                    case "nonceRevelationRewardsLiquid":
                        foreach (var row in rows)
                            result[j++][i] = row.NonceRevelationRewardsDelegated;
                        break;
                    case "revelationRewards":
                        foreach (var row in rows)
                            result[j++][i] = row.NonceRevelationRewardsDelegated + row.NonceRevelationRewardsStakedOwn + row.NonceRevelationRewardsStakedEdge + row.NonceRevelationRewardsStakedShared + row.VdfRevelationRewardsDelegated + row.VdfRevelationRewardsStakedOwn + row.VdfRevelationRewardsStakedEdge + row.VdfRevelationRewardsStakedShared;
                        break;
                    case "revelationLosses":
                        foreach (var row in rows)
                            result[j++][i] = row.NonceRevelationLosses;
                        break;
                    case "doublePreendorsingLosses":
                        foreach (var row in rows)
                            result[j++][i] = row.DoublePreendorsingLostStaked + row.DoublePreendorsingLostExternalStaked + row.DoublePreendorsingLostUnstaked + row.DoublePreendorsingLostExternalUnstaked;
                        break;
                    case "doubleEndorsingLosses":
                        foreach (var row in rows)
                            result[j++][i] = row.DoubleEndorsingLostStaked + row.DoubleEndorsingLostExternalStaked + row.DoubleEndorsingLostUnstaked + row.DoubleEndorsingLostExternalUnstaked;
                        break;
                    case "doubleBakingLosses":
                        foreach (var row in rows)
                            result[j++][i] = row.DoubleBakingLostStaked + row.DoubleBakingLostExternalStaked + row.DoubleBakingLostUnstaked + row.DoubleBakingLostExternalUnstaked;
                        break;
                    case "endorsementRewards":
                        foreach (var row in rows)
                            result[j++][i] = row.EndorsementRewardsDelegated + row.EndorsementRewardsStakedOwn + row.EndorsementRewardsStakedEdge + row.EndorsementRewardsStakedShared;
                        break;
                    case "blockRewards":
                        foreach (var row in rows)
                            result[j++][i] = row.BlockRewardsDelegated + row.BlockRewardsStakedOwn + row.BlockRewardsStakedEdge + row.BlockRewardsStakedShared;
                        break;
                    case "stakingBalance":
                        foreach (var row in rows)
                            result[j++][i] = row.OwnDelegatedBalance + row.ExternalDelegatedBalance + row.OwnStakedBalance + row.ExternalStakedBalance;
                        break;
                    case "activeStake":
                        foreach (var row in rows)
                            result[j++][i] = row.BakingPower;
                        break;
                    case "selectedStake":
                        foreach (var row in rows)
                            result[j++][i] = row.BakingPower;
                        break;
                    case "balance":
                        foreach (var row in rows)
                            result[j++][i] = row.DelegatedBalance + row.StakedBalance;
                        break;
                    #endregion
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
                case "cycle": columns.Add(@"dc.""Cycle"""); break;
                case "delegatedBalance": columns.Add(@"dc.""DelegatedBalance"""); break;
                case "stakedBalance": columns.Add(@"dc.""StakedBalance"""); break;
                case "baker": columns.Add(@"dc.""BakerId"""); break;
                case "bakingPower": columns.Add(@"""BakingPower"""); break;
                case "totalBakingPower": columns.Add(@"""TotalBakingPower"""); break;
                case "bakerDelegatedBalance": columns.Add(@"""OwnDelegatedBalance"""); break;
                case "externalDelegatedBalance": columns.Add(@"""ExternalDelegatedBalance"""); break;
                case "bakerStakedBalance": columns.Add(@"""OwnStakedBalance"""); break;
                case "externalStakedBalance": columns.Add(@"""ExternalStakedBalance"""); break;
                case "expectedBlocks": columns.Add(@"""ExpectedBlocks"""); break;
                case "expectedEndorsements": columns.Add(@"""ExpectedEndorsements"""); break;
                case "futureBlocks": columns.Add(@"""FutureBlocks"""); break;
                case "futureBlockRewards": columns.Add(@"""FutureBlockRewards"""); break;
                case "blocks": columns.Add(@"""Blocks"""); break;
                case "blockRewardsDelegated": columns.Add(@"""BlockRewardsDelegated"""); break;
                case "blockRewardsStakedOwn": columns.Add(@"""BlockRewardsStakedOwn"""); break;
                case "blockRewardsStakedEdge": columns.Add(@"""BlockRewardsStakedEdge"""); break;
                case "blockRewardsStakedShared": columns.Add(@"""BlockRewardsStakedShared"""); break;
                case "missedBlocks": columns.Add(@"""MissedBlocks"""); break;
                case "missedBlockRewards": columns.Add(@"""MissedBlockRewards"""); break;
                case "futureEndorsements": columns.Add(@"""FutureEndorsements"""); break;
                case "futureEndorsementRewards": columns.Add(@"""FutureEndorsementRewards"""); break;
                case "endorsements": columns.Add(@"""Endorsements"""); break;
                case "endorsementRewardsDelegated": columns.Add(@"""EndorsementRewardsDelegated"""); break;
                case "endorsementRewardsStakedOwn": columns.Add(@"""EndorsementRewardsStakedOwn"""); break;
                case "endorsementRewardsStakedEdge": columns.Add(@"""EndorsementRewardsStakedEdge"""); break;
                case "endorsementRewardsStakedShared": columns.Add(@"""EndorsementRewardsStakedShared"""); break;
                case "missedEndorsements": columns.Add(@"""MissedEndorsements"""); break;
                case "missedEndorsementRewards": columns.Add(@"""MissedEndorsementRewards"""); break;
                case "blockFees": columns.Add(@"""BlockFees"""); break;
                case "missedBlockFees": columns.Add(@"""MissedBlockFees"""); break;
                case "doubleBakingRewards": columns.Add(@"""DoubleBakingRewards"""); break;
                case "doubleBakingLostStaked": columns.Add(@"""DoubleBakingLostStaked"""); break;
                case "doubleBakingLostUnstaked": columns.Add(@"""DoubleBakingLostUnstaked"""); break;
                case "doubleBakingLostExternalStaked": columns.Add(@"""DoubleBakingLostExternalStaked"""); break;
                case "doubleBakingLostExternalUnstaked": columns.Add(@"""DoubleBakingLostExternalUnstaked"""); break;
                case "doubleEndorsingRewards": columns.Add(@"""DoubleEndorsingRewards"""); break;
                case "doubleEndorsingLostStaked": columns.Add(@"""DoubleEndorsingLostStaked"""); break;
                case "doubleEndorsingLostUnstaked": columns.Add(@"""DoubleEndorsingLostUnstaked"""); break;
                case "doubleEndorsingLostExternalStaked": columns.Add(@"""DoubleEndorsingLostExternalStaked"""); break;
                case "doubleEndorsingLostExternalUnstaked": columns.Add(@"""DoubleEndorsingLostExternalUnstaked"""); break;
                case "doublePreendorsingRewards": columns.Add(@"""DoublePreendorsingRewards"""); break;
                case "doublePreendorsingLostStaked": columns.Add(@"""DoublePreendorsingLostStaked"""); break;
                case "doublePreendorsingLostUnstaked": columns.Add(@"""DoublePreendorsingLostUnstaked"""); break;
                case "doublePreendorsingLostExternalStaked": columns.Add(@"""DoublePreendorsingLostExternalStaked"""); break;
                case "doublePreendorsingLostExternalUnstaked": columns.Add(@"""DoublePreendorsingLostExternalUnstaked"""); break;
                case "vdfRevelationRewardsDelegated": columns.Add(@"""VdfRevelationRewardsDelegated"""); break;
                case "vdfRevelationRewardsStakedOwn": columns.Add(@"""VdfRevelationRewardsStakedOwn"""); break;
                case "vdfRevelationRewardsStakedEdge": columns.Add(@"""VdfRevelationRewardsStakedEdge"""); break;
                case "vdfRevelationRewardsStakedShared": columns.Add(@"""VdfRevelationRewardsStakedShared"""); break;
                case "nonceRevelationRewardsDelegated": columns.Add(@"""NonceRevelationRewardsDelegated"""); break;
                case "nonceRevelationRewardsStakedOwn": columns.Add(@"""NonceRevelationRewardsStakedOwn"""); break;
                case "nonceRevelationRewardsStakedEdge": columns.Add(@"""NonceRevelationRewardsStakedEdge"""); break;
                case "nonceRevelationRewardsStakedShared": columns.Add(@"""NonceRevelationRewardsStakedShared"""); break;
                case "nonceRevelationLosses": columns.Add(@"""NonceRevelationLosses"""); break;
                case "quote": columns.Add(@"dc.""Cycle"""); break;

                #region deprecated
                case "blockRewardsLiquid": columns.Add(@"""BlockRewardsDelegated"""); break;
                case "endorsementRewardsLiquid": columns.Add(@"""EndorsementRewardsDelegated"""); break;
                case "vdfRevelationRewardsLiquid": columns.Add(@"""VdfRevelationRewardsDelegated"""); break;
                case "nonceRevelationRewardsLiquid": columns.Add(@"""NonceRevelationRewardsDelegated"""); break;
                case "revelationRewards":
                    columns.Add(@"""NonceRevelationRewardsDelegated""");
                    columns.Add(@"""NonceRevelationRewardsStakedOwn""");
                    columns.Add(@"""NonceRevelationRewardsStakedEdge""");
                    columns.Add(@"""NonceRevelationRewardsStakedShared""");
                    columns.Add(@"""VdfRevelationRewardsDelegated""");
                    columns.Add(@"""VdfRevelationRewardsStakedOwn""");
                    columns.Add(@"""VdfRevelationRewardsStakedEdge""");
                    columns.Add(@"""VdfRevelationRewardsStakedShared""");
                    break;
                case "revelationLosses":
                    columns.Add(@"""NonceRevelationLosses""");
                    break;
                case "doublePreendorsingLosses":
                    columns.Add(@"""DoublePreendorsingLostStaked""");
                    columns.Add(@"""DoublePreendorsingLostExternalStaked""");
                    columns.Add(@"""DoublePreendorsingLostUnstaked""");
                    columns.Add(@"""DoublePreendorsingLostExternalUnstaked""");
                    break;
                case "doubleEndorsingLosses":
                    columns.Add(@"""DoubleEndorsingLostStaked""");
                    columns.Add(@"""DoubleEndorsingLostExternalStaked""");
                    columns.Add(@"""DoubleEndorsingLostUnstaked""");
                    columns.Add(@"""DoubleEndorsingLostExternalUnstaked""");
                    break;
                case "doubleBakingLosses":
                    columns.Add(@"""DoubleBakingLostStaked""");
                    columns.Add(@"""DoubleBakingLostExternalStaked""");
                    columns.Add(@"""DoubleBakingLostUnstaked""");
                    columns.Add(@"""DoubleBakingLostExternalUnstaked""");
                    break;
                case "endorsementRewards":
                    columns.Add(@"""EndorsementRewardsDelegated""");
                    columns.Add(@"""EndorsementRewardsStakedOwn""");
                    columns.Add(@"""EndorsementRewardsStakedEdge""");
                    columns.Add(@"""EndorsementRewardsStakedShared""");
                    break;
                case "blockRewards":
                    columns.Add(@"""BlockRewardsDelegated""");
                    columns.Add(@"""BlockRewardsStakedOwn""");
                    columns.Add(@"""BlockRewardsStakedEdge""");
                    columns.Add(@"""BlockRewardsStakedShared""");
                    break;
                case "stakingBalance":
                    columns.Add(@"""OwnDelegatedBalance""");
                    columns.Add(@"""ExternalDelegatedBalance""");
                    columns.Add(@"""OwnStakedBalance""");
                    columns.Add(@"""ExternalStakedBalance""");
                    break;
                case "activeStake":
                    columns.Add(@"""BakingPower""");
                    break;
                case "selectedStake":
                    columns.Add(@"""BakingPower""");
                    break;
                case "balance":
                    columns.Add(@"dc.""DelegatedBalance""");
                    columns.Add(@"dc.""StakedBalance""");
                    break;
                #endregion
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

            await using var db = await DataSource.OpenConnectionAsync();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            //TODO: optimize memory allocation
            var result = new object[rows.Count()];
            var j = 0;

            switch (field)
            {
                case "cycle":
                    foreach (var row in rows)
                        result[j++] = row.Cycle;
                    break;
                case "delegatedBalance":
                    foreach (var row in rows)
                        result[j++] = row.DelegatedBalance;
                    break;
                case "stakedBalance":
                    foreach (var row in rows)
                        result[j++] = row.StakedBalance;
                    break;
                case "baker":
                    foreach (var row in rows)
                        result[j++] = Accounts.GetAlias(row.BakerId);
                    break;
                case "bakingPower":
                    foreach (var row in rows)
                        result[j++] = row.BakingPower;
                    break;
                case "totalBakingPower":
                    foreach (var row in rows)
                        result[j++] = row.TotalBakingPower;
                    break;
                case "bakerDelegatedBalance":
                    foreach (var row in rows)
                        result[j++] = row.OwnDelegatedBalance;
                    break;
                case "externalDelegatedBalance":
                    foreach (var row in rows)
                        result[j++] = row.ExternalDelegatedBalance;
                    break;
                case "bakerStakedBalance":
                    foreach (var row in rows)
                        result[j++] = row.OwnStakedBalance;
                    break;
                case "externalStakedBalance":
                    foreach (var row in rows)
                        result[j++] = row.ExternalStakedBalance;
                    break;
                case "expectedBlocks":
                    foreach (var row in rows)
                        result[j++] = Math.Round(row.ExpectedBlocks, 2);
                    break;
                case "expectedEndorsements":
                    foreach (var row in rows)
                        result[j++] = Math.Round(row.ExpectedEndorsements, 2);
                    break;
                case "futureBlocks":
                    foreach (var row in rows)
                        result[j++] = row.FutureBlocks;
                    break;
                case "futureBlockRewards":
                    foreach (var row in rows)
                        result[j++] = row.FutureBlockRewards;
                    break;
                case "blocks":
                    foreach (var row in rows)
                        result[j++] = row.Blocks;
                    break;
                case "blockRewardsDelegated":
                    foreach (var row in rows)
                        result[j++] = row.BlockRewardsDelegated;
                    break;
                case "blockRewardsStakedOwn":
                    foreach (var row in rows)
                        result[j++] = row.BlockRewardsStakedOwn;
                    break;
                case "blockRewardsStakedEdge":
                    foreach (var row in rows)
                        result[j++] = row.BlockRewardsStakedEdge;
                    break;
                case "blockRewardsStakedShared":
                    foreach (var row in rows)
                        result[j++] = row.BlockRewardsStakedShared;
                    break;
                case "missedBlocks":
                    foreach (var row in rows)
                        result[j++] = row.MissedBlocks;
                    break;
                case "missedBlockRewards":
                    foreach (var row in rows)
                        result[j++] = row.MissedBlockRewards;
                    break;
                case "futureEndorsements":
                    foreach (var row in rows)
                        result[j++] = row.FutureEndorsements;
                    break;
                case "futureEndorsementRewards":
                    foreach (var row in rows)
                        result[j++] = row.FutureEndorsementRewards;
                    break;
                case "endorsements":
                    foreach (var row in rows)
                        result[j++] = row.Endorsements;
                    break;
                case "endorsementRewardsDelegated":
                    foreach (var row in rows)
                        result[j++] = row.EndorsementRewardsDelegated;
                    break;
                case "endorsementRewardsStakedOwn":
                    foreach (var row in rows)
                        result[j++] = row.EndorsementRewardsStakedOwn;
                    break;
                case "endorsementRewardsStakedEdge":
                    foreach (var row in rows)
                        result[j++] = row.EndorsementRewardsStakedEdge;
                    break;
                case "endorsementRewardsStakedShared":
                    foreach (var row in rows)
                        result[j++] = row.EndorsementRewardsStakedShared;
                    break;
                case "missedEndorsements":
                    foreach (var row in rows)
                        result[j++] = row.MissedEndorsements;
                    break;
                case "missedEndorsementRewards":
                    foreach (var row in rows)
                        result[j++] = row.MissedEndorsementRewards;
                    break;
                case "blockFees":
                    foreach (var row in rows)
                        result[j++] = row.BlockFees;
                    break;
                case "missedBlockFees":
                    foreach (var row in rows)
                        result[j++] = row.MissedBlockFees;
                    break;
                case "doubleBakingRewards":
                    foreach (var row in rows)
                        result[j++] = row.DoubleBakingRewards;
                    break;
                case "doubleBakingLostStaked":
                    foreach (var row in rows)
                        result[j++] = row.DoubleBakingLostStaked;
                    break;
                case "doubleBakingLostUnstaked":
                    foreach (var row in rows)
                        result[j++] = row.DoubleBakingLostUnstaked;
                    break;
                case "doubleBakingLostExternalStaked":
                    foreach (var row in rows)
                        result[j++] = row.DoubleBakingLostExternalStaked;
                    break;
                case "doubleBakingLostExternalUnstaked":
                    foreach (var row in rows)
                        result[j++] = row.DoubleBakingLostExternalUnstaked;
                    break;
                case "doubleEndorsingRewards":
                    foreach (var row in rows)
                        result[j++] = row.DoubleEndorsingRewards;
                    break;
                case "doubleEndorsingLostStaked":
                    foreach (var row in rows)
                        result[j++] = row.DoubleEndorsingLostStaked;
                    break;
                case "doubleEndorsingLostUnstaked":
                    foreach (var row in rows)
                        result[j++] = row.DoubleEndorsingLostUnstaked;
                    break;
                case "doubleEndorsingLostExternalStaked":
                    foreach (var row in rows)
                        result[j++] = row.DoubleEndorsingLostExternalStaked;
                    break;
                case "doubleEndorsingLostExternalUnstaked":
                    foreach (var row in rows)
                        result[j++] = row.DoubleEndorsingLostExternalUnstaked;
                    break;
                case "doublePreendorsingRewards":
                    foreach (var row in rows)
                        result[j++] = row.DoublePreendorsingRewards;
                    break;
                case "doublePreendorsingLostStaked":
                    foreach (var row in rows)
                        result[j++] = row.DoublePreendorsingLostStaked;
                    break;
                case "doublePreendorsingLostUnstaked":
                    foreach (var row in rows)
                        result[j++] = row.DoublePreendorsingLostUnstaked;
                    break;
                case "doublePreendorsingLostExternalStaked":
                    foreach (var row in rows)
                        result[j++] = row.DoublePreendorsingLostExternalStaked;
                    break;
                case "doublePreendorsingLostExternalUnstaked":
                    foreach (var row in rows)
                        result[j++] = row.DoublePreendorsingLostExternalUnstaked;
                    break;
                case "vdfRevelationRewardsDelegated":
                    foreach (var row in rows)
                        result[j++] = row.VdfRevelationRewardsDelegated;
                    break;
                case "vdfRevelationRewardsStakedOwn":
                    foreach (var row in rows)
                        result[j++] = row.VdfRevelationRewardsStakedOwn;
                    break;
                case "vdfRevelationRewardsStakedEdge":
                    foreach (var row in rows)
                        result[j++] = row.VdfRevelationRewardsStakedEdge;
                    break;
                case "vdfRevelationRewardsStakedShared":
                    foreach (var row in rows)
                        result[j++] = row.VdfRevelationRewardsStakedShared;
                    break;
                case "nonceRevelationRewardsDelegated":
                    foreach (var row in rows)
                        result[j++] = row.NonceRevelationRewardsDelegated;
                    break;
                case "nonceRevelationRewardsStakedOwn":
                    foreach (var row in rows)
                        result[j++] = row.NonceRevelationRewardsStakedOwn;
                    break;
                case "nonceRevelationRewardsStakedEdge":
                    foreach (var row in rows)
                        result[j++] = row.NonceRevelationRewardsStakedEdge;
                    break;
                case "nonceRevelationRewardsStakedShared":
                    foreach (var row in rows)
                        result[j++] = row.NonceRevelationRewardsStakedShared;
                    break;
                case "nonceRevelationLosses":
                    foreach (var row in rows)
                        result[j++] = row.NonceRevelationLosses;
                    break;
                case "quote":
                    foreach (var row in rows)
                        result[j++] = Quotes.Get(quote, Protocols.FindByCycle((int)row.Cycle).GetCycleEnd((int)row.Cycle));
                    break;

                #region deprecated
                case "blockRewardsLiquid":
                    foreach (var row in rows)
                        result[j++] = row.BlockRewardsDelegated;
                    break;
                case "endorsementRewardsLiquid":
                    foreach (var row in rows)
                        result[j++] = row.EndorsementRewardsDelegated;
                    break;
                case "vdfRevelationRewardsLiquid":
                    foreach (var row in rows)
                        result[j++] = row.VdfRevelationRewardsDelegated;
                    break;
                case "nonceRevelationRewardsLiquid":
                    foreach (var row in rows)
                        result[j++] = row.NonceRevelationRewardsDelegated;
                    break;
                case "revelationRewards":
                    foreach (var row in rows)
                        result[j++] = row.NonceRevelationRewardsDelegated + row.NonceRevelationRewardsStakedOwn + row.NonceRevelationRewardsStakedEdge + row.NonceRevelationRewardsStakedShared + row.VdfRevelationRewardsDelegated + row.VdfRevelationRewardsStakedOwn + row.VdfRevelationRewardsStakedEdge + row.VdfRevelationRewardsStakedShared;
                    break;
                case "revelationLosses":
                    foreach (var row in rows)
                        result[j++] = row.NonceRevelationLosses;
                    break;
                case "doublePreendorsingLosses":
                    foreach (var row in rows)
                        result[j++] = row.DoublePreendorsingLostStaked + row.DoublePreendorsingLostExternalStaked + row.DoublePreendorsingLostUnstaked + row.DoublePreendorsingLostExternalUnstaked;
                    break;
                case "doubleEndorsingLosses":
                    foreach (var row in rows)
                        result[j++] = row.DoubleEndorsingLostStaked + row.DoubleEndorsingLostExternalStaked + row.DoubleEndorsingLostUnstaked + row.DoubleEndorsingLostExternalUnstaked;
                    break;
                case "doubleBakingLosses":
                    foreach (var row in rows)
                        result[j++] = row.DoubleBakingLostStaked + row.DoubleBakingLostExternalStaked + row.DoubleBakingLostUnstaked + row.DoubleBakingLostExternalUnstaked;
                    break;
                case "endorsementRewards":
                    foreach (var row in rows)
                        result[j++] = row.EndorsementRewardsDelegated + row.EndorsementRewardsStakedOwn + row.EndorsementRewardsStakedEdge + row.EndorsementRewardsStakedShared;
                    break;
                case "blockRewards":
                    foreach (var row in rows)
                        result[j++] = row.BlockRewardsDelegated + row.BlockRewardsStakedOwn + row.BlockRewardsStakedEdge + row.BlockRewardsStakedShared;
                    break;
                case "stakingBalance":
                    foreach (var row in rows)
                        result[j++] = row.OwnDelegatedBalance + row.ExternalDelegatedBalance + row.OwnStakedBalance + row.ExternalStakedBalance;
                    break;
                case "activeStake":
                    foreach (var row in rows)
                        result[j++] = row.BakingPower;
                    break;
                case "selectedStake":
                    foreach (var row in rows)
                        result[j++] = row.BakingPower;
                    break;
                case "balance":
                    foreach (var row in rows)
                        result[j++] = row.DelegatedBalance + row.StakedBalance;
                    break;
                    #endregion
            }

            return result;
        }
        #endregion

        #region split
        public async Task<RewardSplit> GetRewardSplit(string address, int cycle, int offset, int limit)
        {
            if (await Accounts.GetAsync(address) is not RawDelegate baker)
                return null;

            var sqlRewards = $"""
                SELECT  *
                FROM    "BakerCycles"
                WHERE   "BakerId" = {baker.Id}
                AND     "Cycle" = {cycle}
                LIMIT   1
                """;

            var sqlDelegators = $"""
                SELECT      "DelegatorId", "DelegatedBalance", "StakedBalance"
                FROM        "DelegatorCycles"
                WHERE       "BakerId" = {baker.Id}
                AND         "Cycle" = {cycle}
                ORDER BY    "StakedBalance" DESC, "DelegatedBalance" DESC
                OFFSET      {offset}
                LIMIT       {limit}
                """;

            await using var db = await DataSource.OpenConnectionAsync();
            using var result = await db.QueryMultipleAsync($"""
                {sqlRewards};
                {sqlDelegators};
                """);

            var rewards = result.ReadFirstOrDefault();
            if (rewards == null) return null;

            var delegators = result.Read();

            return new RewardSplit
            {
                Cycle = rewards.Cycle,
                BakingPower = rewards.BakingPower,
                TotalBakingPower = rewards.TotalBakingPower,
                OwnDelegatedBalance = rewards.OwnDelegatedBalance,
                ExternalDelegatedBalance = rewards.ExternalDelegatedBalance,
                DelegatorsCount = rewards.DelegatorsCount,
                OwnStakedBalance = rewards.OwnStakedBalance,
                ExternalStakedBalance = rewards.ExternalStakedBalance,
                StakersCount = rewards.StakersCount,
                ExpectedBlocks = Math.Round(rewards.ExpectedBlocks, 2),
                ExpectedEndorsements = Math.Round(rewards.ExpectedEndorsements, 2),
                FutureBlocks = rewards.FutureBlocks,
                FutureBlockRewards = rewards.FutureBlockRewards,
                Blocks = rewards.Blocks,
                BlockRewardsDelegated = rewards.BlockRewardsDelegated,
                BlockRewardsStakedOwn = rewards.BlockRewardsStakedOwn,
                BlockRewardsStakedEdge = rewards.BlockRewardsStakedEdge,
                BlockRewardsStakedShared = rewards.BlockRewardsStakedShared,
                MissedBlocks = rewards.MissedBlocks,
                MissedBlockRewards = rewards.MissedBlockRewards,
                FutureEndorsements = rewards.FutureEndorsements,
                FutureEndorsementRewards = rewards.FutureEndorsementRewards,
                Endorsements = rewards.Endorsements,
                EndorsementRewardsDelegated = rewards.EndorsementRewardsDelegated,
                EndorsementRewardsStakedOwn = rewards.EndorsementRewardsStakedOwn,
                EndorsementRewardsStakedEdge = rewards.EndorsementRewardsStakedEdge,
                EndorsementRewardsStakedShared = rewards.EndorsementRewardsStakedShared,
                MissedEndorsements = rewards.MissedEndorsements,
                MissedEndorsementRewards = rewards.MissedEndorsementRewards,
                BlockFees = rewards.BlockFees,
                MissedBlockFees = rewards.MissedBlockFees,
                DoubleBakingRewards = rewards.DoubleBakingRewards,
                DoubleBakingLostStaked = rewards.DoubleBakingLostStaked,
                DoubleBakingLostUnstaked = rewards.DoubleBakingLostUnstaked,
                DoubleBakingLostExternalStaked = rewards.DoubleBakingLostExternalStaked,
                DoubleBakingLostExternalUnstaked = rewards.DoubleBakingLostExternalUnstaked,
                DoubleEndorsingRewards = rewards.DoubleEndorsingRewards,
                DoubleEndorsingLostStaked = rewards.DoubleEndorsingLostStaked,
                DoubleEndorsingLostUnstaked = rewards.DoubleEndorsingLostUnstaked,
                DoubleEndorsingLostExternalStaked = rewards.DoubleEndorsingLostExternalStaked,
                DoubleEndorsingLostExternalUnstaked = rewards.DoubleEndorsingLostExternalUnstaked,
                DoublePreendorsingRewards = rewards.DoublePreendorsingRewards,
                DoublePreendorsingLostStaked = rewards.DoublePreendorsingLostStaked,
                DoublePreendorsingLostUnstaked = rewards.DoublePreendorsingLostUnstaked,
                DoublePreendorsingLostExternalStaked = rewards.DoublePreendorsingLostExternalStaked,
                DoublePreendorsingLostExternalUnstaked = rewards.DoublePreendorsingLostExternalUnstaked,
                VdfRevelationRewardsDelegated = rewards.VdfRevelationRewardsDelegated,
                VdfRevelationRewardsStakedOwn = rewards.VdfRevelationRewardsStakedOwn,
                VdfRevelationRewardsStakedEdge = rewards.VdfRevelationRewardsStakedEdge,
                VdfRevelationRewardsStakedShared = rewards.VdfRevelationRewardsStakedShared,
                NonceRevelationRewardsDelegated = rewards.NonceRevelationRewardsDelegated,
                NonceRevelationRewardsStakedOwn = rewards.NonceRevelationRewardsStakedOwn,
                NonceRevelationRewardsStakedEdge = rewards.NonceRevelationRewardsStakedEdge,
                NonceRevelationRewardsStakedShared = rewards.NonceRevelationRewardsStakedShared,
                NonceRevelationLosses = rewards.NonceRevelationLosses,
                Delegators = delegators.Select(x => 
                {
                    var delegator = Accounts.Get((int)x.DelegatorId);
                    return new SplitDelegator
                    {
                        Address = delegator.Address,
                        DelegatedBalance = x.DelegatedBalance,
                        StakedBalance = x.StakedBalance,
                        Emptied = delegator is RawUser user && user.Balance == 0 && user.StakedPseudotokens == null
                    };
                })
            };
        }

        public async Task<SplitDelegator> GetRewardSplitDelegator(string bakerAddress, int cycle, string delegatorAddress)
        {
            if (await Accounts.GetAsync(bakerAddress) is not RawDelegate baker)
                return null;

            if (await Accounts.GetAsync(delegatorAddress) is not RawAccount delegator)
                return null;

            var sql = $"""
                SELECT  "DelegatedBalance", "StakedBalance"
                FROM    "DelegatorCycles"
                WHERE   "BakerId" = {baker.Id}
                AND     "Cycle" = {cycle}
                AND     "DelegatorId" = {delegator.Id}
                LIMIT   1
                """;

            await using var db = await DataSource.OpenConnectionAsync();
            var row = await db.QueryFirstOrDefaultAsync(sql);
            if (row == null) return null;

            return new SplitDelegator
            {
                Address = delegator.Address,
                DelegatedBalance = row.DelegatedBalance,
                StakedBalance = row.StakedBalance,
                Emptied = delegator is RawUser user && user.Balance == 0 && user.StakedPseudotokens == null
            };
        }
        #endregion

        #region baker stats
        public async Task<BakerStats> GetBakerStats(string address)
        {
            if (await Accounts.GetAsync(address) is not RawDelegate baker)
                return null;

            var rewards = await GetBakerRewards(address, null, null, null, 10000, Symbols.None);
            var rewardsList = rewards.ToList();

            if (!rewardsList.Any())
                return null;

            var alias = Accounts.GetAlias(baker.Id);

            var totalExpectedBlocks = rewardsList.Sum(r => r.ExpectedBlocks);
            var totalBlocks = rewardsList.Sum(r => (long)r.Blocks);
            var totalMissedBlocks = rewardsList.Sum(r => (long)r.MissedBlocks);

            var totalExpectedEndorsements = rewardsList.Sum(r => r.ExpectedEndorsements);
            var totalEndorsements = rewardsList.Sum(r => (long)r.Endorsements);
            var totalMissedEndorsements = rewardsList.Sum(r => (long)r.MissedEndorsementRewards

            var totalActualRewards = rewardsList.Sum(r =>
                r.BlockRewardsDelegated + r.BlockRewardsStakedOwn + r.BlockRewardsStakedEdge + r.BlockRewardsStakedShared +
                r.EndorsementRewardsDelegated + r.EndorsementRewardsStakedOwn + r.EndorsementRewardsStakedEdge + r.EndorsementRewardsStakedShared);
            
            var totalMissedRewards = rewardsList.Sum(r => r.MissedBlockRewards + r.MissedEndorsementRewards);
            
            var totalExpectedRewards = totalActualRewards + totalMissedRewards;

            var luck = totalExpectedRewards > 0
                ? Math.Round((double)totalActualRewards / totalExpectedRewards * 100, 2)
                : 0.0;

            var totalOpportunities = totalBlocks + totalMissedBlocks + totalEndorsements + totalMissedEndorsements;
            var successfulOperations = totalBlocks + totalEndorsements;
            var performance = totalOpportunities > 0
                ? Math.Round((double)successfulOperations / totalOpportunities * 100, 2)
                : 0.0;

            var totalExpectedOperations = totalExpectedBlocks + totalExpectedEndorsements;
            var totalActualOperations = totalBlocks + totalEndorsements;
            var reliability = totalExpectedOperations > 0
                ? Math.Round((double)totalActualOperations / totalExpectedOperations * 100, 2)
                : 0.0;

            return new BakerStats
            {
                Address = baker.Address,
                Alias = alias?.Name,
                Luck = luck,
                Performance = performance,
                Reliability = reliability,
                TotalExpectedRewards = totalExpectedRewards,
                TotalActualRewards = totalActualRewards
            };
        }
        #endregion
    }
}
