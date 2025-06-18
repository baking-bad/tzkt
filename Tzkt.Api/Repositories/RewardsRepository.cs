using Dapper;
using Npgsql;
using Tzkt.Api.Models;
using Tzkt.Api.Services.Cache;

namespace Tzkt.Api.Repositories
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
            Int32Parameter? cycle,
            SortParameter? sort,
            OffsetParameter? offset,
            int limit,
            Symbols quote)
        {
            if (await Accounts.GetAsync(address) is not RawDelegate baker)
                return [];

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
                ExpectedAttestations = Math.Round(row.ExpectedAttestations, 2),
                ExpectedDalShards = row.ExpectedDalShards,
                FutureBlocks = row.FutureBlocks,
                FutureBlockRewards = row.FutureBlockRewards,
                Blocks = row.Blocks,
                BlockRewardsDelegated = row.BlockRewardsDelegated,
                BlockRewardsStakedOwn = row.BlockRewardsStakedOwn,
                BlockRewardsStakedEdge = row.BlockRewardsStakedEdge,
                BlockRewardsStakedShared = row.BlockRewardsStakedShared,
                MissedBlocks = row.MissedBlocks,
                MissedBlockRewards = row.MissedBlockRewards,
                FutureAttestations = row.FutureAttestations,
                FutureAttestationRewards = row.FutureAttestationRewards,
                Attestations = row.Attestations,
                AttestationRewardsDelegated = row.AttestationRewardsDelegated,
                AttestationRewardsStakedOwn = row.AttestationRewardsStakedOwn,
                AttestationRewardsStakedEdge = row.AttestationRewardsStakedEdge,
                AttestationRewardsStakedShared = row.AttestationRewardsStakedShared,
                MissedAttestations = row.MissedAttestations,
                MissedAttestationRewards = row.MissedAttestationRewards,
                FutureDalAttestationRewards = row.FutureDalAttestationRewards,
                DalAttestationRewardsDelegated = row.DalAttestationRewardsDelegated,
                DalAttestationRewardsStakedOwn = row.DalAttestationRewardsStakedOwn,
                DalAttestationRewardsStakedEdge = row.DalAttestationRewardsStakedEdge,
                DalAttestationRewardsStakedShared = row.DalAttestationRewardsStakedShared,
                MissedDalAttestationRewards = row.MissedDalAttestationRewards,
                BlockFees = row.BlockFees,
                MissedBlockFees = row.MissedBlockFees,
                DoubleBakingRewards = row.DoubleBakingRewards,
                DoubleBakingLostStaked = row.DoubleBakingLostStaked,
                DoubleBakingLostUnstaked = row.DoubleBakingLostUnstaked,
                DoubleBakingLostExternalStaked = row.DoubleBakingLostExternalStaked,
                DoubleBakingLostExternalUnstaked = row.DoubleBakingLostExternalUnstaked,
                DoubleAttestationRewards = row.DoubleAttestationRewards,
                DoubleAttestationLostStaked = row.DoubleAttestationLostStaked,
                DoubleAttestationLostUnstaked = row.DoubleAttestationLostUnstaked,
                DoubleAttestationLostExternalStaked = row.DoubleAttestationLostExternalStaked,
                DoubleAttestationLostExternalUnstaked = row.DoubleAttestationLostExternalUnstaked,
                DoublePreattestationRewards = row.DoublePreattestationRewards,
                DoublePreattestationLostStaked = row.DoublePreattestationLostStaked,
                DoublePreattestationLostUnstaked = row.DoublePreattestationLostUnstaked,
                DoublePreattestationLostExternalStaked = row.DoublePreattestationLostExternalStaked,
                DoublePreattestationLostExternalUnstaked = row.DoublePreattestationLostExternalUnstaked,
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

        public async Task<object?[][]> GetBakerRewards(
            string address,
            Int32Parameter? cycle,
            SortParameter? sort,
            OffsetParameter? offset,
            int limit,
            string[] fields,
            Symbols quote)
        {
            if (await Accounts.GetAsync(address) is not RawDelegate baker)
                return [];

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
                    case "expectedAttestations": columns.Add(@"""ExpectedAttestations"""); break;
                    case "expectedDalShards": columns.Add(@"""ExpectedDalShards"""); break;
                    case "futureBlocks": columns.Add(@"""FutureBlocks"""); break;
                    case "futureBlockRewards": columns.Add(@"""FutureBlockRewards"""); break;
                    case "blocks": columns.Add(@"""Blocks"""); break;
                    case "blockRewardsDelegated": columns.Add(@"""BlockRewardsDelegated"""); break;
                    case "blockRewardsStakedOwn": columns.Add(@"""BlockRewardsStakedOwn"""); break;
                    case "blockRewardsStakedEdge": columns.Add(@"""BlockRewardsStakedEdge"""); break;
                    case "blockRewardsStakedShared": columns.Add(@"""BlockRewardsStakedShared"""); break;
                    case "missedBlocks": columns.Add(@"""MissedBlocks"""); break;
                    case "missedBlockRewards": columns.Add(@"""MissedBlockRewards"""); break;
                    case "futureAttestations": columns.Add(@"""FutureAttestations"""); break;
                    case "futureAttestationRewards": columns.Add(@"""FutureAttestationRewards"""); break;
                    case "attestations": columns.Add(@"""Attestations"""); break;
                    case "attestationRewardsDelegated": columns.Add(@"""AttestationRewardsDelegated"""); break;
                    case "attestationRewardsStakedOwn": columns.Add(@"""AttestationRewardsStakedOwn"""); break;
                    case "attestationRewardsStakedEdge": columns.Add(@"""AttestationRewardsStakedEdge"""); break;
                    case "attestationRewardsStakedShared": columns.Add(@"""AttestationRewardsStakedShared"""); break;
                    case "missedAttestations": columns.Add(@"""MissedAttestations"""); break;
                    case "missedAttestationRewards": columns.Add(@"""MissedAttestationRewards"""); break;
                    case "futureDalAttestationRewards": columns.Add(@"""FutureDalAttestationRewards"""); break;
                    case "dalAttestationRewardsDelegated": columns.Add(@"""DalAttestationRewardsDelegated"""); break;
                    case "dalAttestationRewardsStakedOwn": columns.Add(@"""DalAttestationRewardsStakedOwn"""); break;
                    case "dalAttestationRewardsStakedEdge": columns.Add(@"""DalAttestationRewardsStakedEdge"""); break;
                    case "dalAttestationRewardsStakedShared": columns.Add(@"""DalAttestationRewardsStakedShared"""); break;
                    case "missedDalAttestationRewards": columns.Add(@"""MissedDalAttestationRewards"""); break;
                    case "blockFees": columns.Add(@"""BlockFees"""); break;
                    case "missedBlockFees": columns.Add(@"""MissedBlockFees"""); break;
                    case "doubleBakingRewards": columns.Add(@"""DoubleBakingRewards"""); break;
                    case "doubleBakingLostStaked": columns.Add(@"""DoubleBakingLostStaked"""); break;
                    case "doubleBakingLostUnstaked": columns.Add(@"""DoubleBakingLostUnstaked"""); break;
                    case "doubleBakingLostExternalStaked": columns.Add(@"""DoubleBakingLostExternalStaked"""); break;
                    case "doubleBakingLostExternalUnstaked": columns.Add(@"""DoubleBakingLostExternalUnstaked"""); break;
                    case "doubleAttestationRewards": columns.Add(@"""DoubleAttestationRewards"""); break;
                    case "doubleAttestationLostStaked": columns.Add(@"""DoubleAttestationLostStaked"""); break;
                    case "doubleAttestationLostUnstaked": columns.Add(@"""DoubleAttestationLostUnstaked"""); break;
                    case "doubleAttestationLostExternalStaked": columns.Add(@"""DoubleAttestationLostExternalStaked"""); break;
                    case "doubleAttestationLostExternalUnstaked": columns.Add(@"""DoubleAttestationLostExternalUnstaked"""); break;
                    case "doublePreattestationRewards": columns.Add(@"""DoublePreattestationRewards"""); break;
                    case "doublePreattestationLostStaked": columns.Add(@"""DoublePreattestationLostStaked"""); break;
                    case "doublePreattestationLostUnstaked": columns.Add(@"""DoublePreattestationLostUnstaked"""); break;
                    case "doublePreattestationLostExternalStaked": columns.Add(@"""DoublePreattestationLostExternalStaked"""); break;
                    case "doublePreattestationLostExternalUnstaked": columns.Add(@"""DoublePreattestationLostExternalUnstaked"""); break;
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
                }
            }

            if (columns.Count == 0)
                return [];

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""BakerCycles""")
                .Filter("BakerId", baker.Id)
                .Filter("Cycle", cycle)
                .Take(sort ?? new SortParameter { Desc = "cycle" }, offset, limit, x => ("Cycle", "Cycle"));

            await using var db = await DataSource.OpenConnectionAsync();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            var result = new object?[rows.Count()][];
            for (int i = 0; i < result.Length; i++)
                result[i] = new object?[fields.Length];

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
                    case "expectedAttestations":
                        foreach (var row in rows)
                            result[j++][i] = Math.Round(row.ExpectedAttestations, 2);
                        break;
                    case "expectedDalShards":
                        foreach (var row in rows)
                            result[j++][i] = row.ExpectedDalShards;
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
                    case "futureAttestations":
                        foreach (var row in rows)
                            result[j++][i] = row.FutureAttestations;
                        break;
                    case "futureAttestationRewards":
                        foreach (var row in rows)
                            result[j++][i] = row.FutureAttestationRewards;
                        break;
                    case "attestations":
                        foreach (var row in rows)
                            result[j++][i] = row.Attestations;
                        break;
                    case "attestationRewardsDelegated":
                        foreach (var row in rows)
                            result[j++][i] = row.AttestationRewardsDelegated;
                        break;
                    case "attestationRewardsStakedOwn":
                        foreach (var row in rows)
                            result[j++][i] = row.AttestationRewardsStakedOwn;
                        break;
                    case "attestationRewardsStakedEdge":
                        foreach (var row in rows)
                            result[j++][i] = row.AttestationRewardsStakedEdge;
                        break;
                    case "attestationRewardsStakedShared":
                        foreach (var row in rows)
                            result[j++][i] = row.AttestationRewardsStakedShared;
                        break;
                    case "missedAttestations":
                        foreach (var row in rows)
                            result[j++][i] = row.MissedAttestations;
                        break;
                    case "missedAttestationRewards":
                        foreach (var row in rows)
                            result[j++][i] = row.MissedAttestationRewards;
                        break;
                    case "futureDalAttestationRewards":
                        foreach (var row in rows)
                            result[j++][i] = row.FutureDalAttestationRewards;
                        break;
                    case "dalAttestationRewardsDelegated":
                        foreach (var row in rows)
                            result[j++][i] = row.DalAttestationRewardsDelegated;
                        break;
                    case "dalAttestationRewardsStakedOwn":
                        foreach (var row in rows)
                            result[j++][i] = row.DalAttestationRewardsStakedOwn;
                        break;
                    case "dalAttestationRewardsStakedEdge":
                        foreach (var row in rows)
                            result[j++][i] = row.DalAttestationRewardsStakedEdge;
                        break;
                    case "dalAttestationRewardsStakedShared":
                        foreach (var row in rows)
                            result[j++][i] = row.DalAttestationRewardsStakedShared;
                        break;
                    case "missedDalAttestationRewards":
                        foreach (var row in rows)
                            result[j++][i] = row.MissedDalAttestationRewards;
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
                    case "doubleAttestationRewards":
                        foreach (var row in rows)
                            result[j++][i] = row.DoubleAttestationRewards;
                        break;
                    case "doubleAttestationLostStaked":
                        foreach (var row in rows)
                            result[j++][i] = row.DoubleAttestationLostStaked;
                        break;
                    case "doubleAttestationLostUnstaked":
                        foreach (var row in rows)
                            result[j++][i] = row.DoubleAttestationLostUnstaked;
                        break;
                    case "doubleAttestationLostExternalStaked":
                        foreach (var row in rows)
                            result[j++][i] = row.DoubleAttestationLostExternalStaked;
                        break;
                    case "doubleAttestationLostExternalUnstaked":
                        foreach (var row in rows)
                            result[j++][i] = row.DoubleAttestationLostExternalUnstaked;
                        break;
                    case "doublePreattestationRewards":
                        foreach (var row in rows)
                            result[j++][i] = row.DoublePreattestationRewards;
                        break;
                    case "doublePreattestationLostStaked":
                        foreach (var row in rows)
                            result[j++][i] = row.DoublePreattestationLostStaked;
                        break;
                    case "doublePreattestationLostUnstaked":
                        foreach (var row in rows)
                            result[j++][i] = row.DoublePreattestationLostUnstaked;
                        break;
                    case "doublePreattestationLostExternalStaked":
                        foreach (var row in rows)
                            result[j++][i] = row.DoublePreattestationLostExternalStaked;
                        break;
                    case "doublePreattestationLostExternalUnstaked":
                        foreach (var row in rows)
                            result[j++][i] = row.DoublePreattestationLostExternalUnstaked;
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
                }
            }

            return result;
        }

        public async Task<object?[]> GetBakerRewards(
            string address,
            Int32Parameter? cycle,
            SortParameter? sort,
            OffsetParameter? offset,
            int limit,
            string field,
            Symbols quote)
        {
            if (await Accounts.GetAsync(address) is not RawDelegate baker)
                return [];

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
                case "expectedAttestations": columns.Add(@"""ExpectedAttestations"""); break;
                case "expectedDalShards": columns.Add(@"""ExpectedDalShards"""); break;
                case "futureBlocks": columns.Add(@"""FutureBlocks"""); break;
                case "futureBlockRewards": columns.Add(@"""FutureBlockRewards"""); break;
                case "blocks": columns.Add(@"""Blocks"""); break;
                case "blockRewardsDelegated": columns.Add(@"""BlockRewardsDelegated"""); break;
                case "blockRewardsStakedOwn": columns.Add(@"""BlockRewardsStakedOwn"""); break;
                case "blockRewardsStakedEdge": columns.Add(@"""BlockRewardsStakedEdge"""); break;
                case "blockRewardsStakedShared": columns.Add(@"""BlockRewardsStakedShared"""); break;
                case "missedBlocks": columns.Add(@"""MissedBlocks"""); break;
                case "missedBlockRewards": columns.Add(@"""MissedBlockRewards"""); break;
                case "futureAttestations": columns.Add(@"""FutureAttestations"""); break;
                case "futureAttestationRewards": columns.Add(@"""FutureAttestationRewards"""); break;
                case "attestations": columns.Add(@"""Attestations"""); break;
                case "attestationRewardsDelegated": columns.Add(@"""AttestationRewardsDelegated"""); break;
                case "attestationRewardsStakedOwn": columns.Add(@"""AttestationRewardsStakedOwn"""); break;
                case "attestationRewardsStakedEdge": columns.Add(@"""AttestationRewardsStakedEdge"""); break;
                case "attestationRewardsStakedShared": columns.Add(@"""AttestationRewardsStakedShared"""); break;
                case "missedAttestations": columns.Add(@"""MissedAttestations"""); break;
                case "missedAttestationRewards": columns.Add(@"""MissedAttestationRewards"""); break;
                case "futureDalAttestationRewards": columns.Add(@"""FutureDalAttestationRewards"""); break;
                case "dalAttestationRewardsDelegated": columns.Add(@"""DalAttestationRewardsDelegated"""); break;
                case "dalAttestationRewardsStakedOwn": columns.Add(@"""DalAttestationRewardsStakedOwn"""); break;
                case "dalAttestationRewardsStakedEdge": columns.Add(@"""DalAttestationRewardsStakedEdge"""); break;
                case "dalAttestationRewardsStakedShared": columns.Add(@"""DalAttestationRewardsStakedShared"""); break;
                case "missedDalAttestationRewards": columns.Add(@"""MissedDalAttestationRewards"""); break;
                case "blockFees": columns.Add(@"""BlockFees"""); break;
                case "missedBlockFees": columns.Add(@"""MissedBlockFees"""); break;
                case "doubleBakingRewards": columns.Add(@"""DoubleBakingRewards"""); break;
                case "doubleBakingLostStaked": columns.Add(@"""DoubleBakingLostStaked"""); break;
                case "doubleBakingLostUnstaked": columns.Add(@"""DoubleBakingLostUnstaked"""); break;
                case "doubleBakingLostExternalStaked": columns.Add(@"""DoubleBakingLostExternalStaked"""); break;
                case "doubleBakingLostExternalUnstaked": columns.Add(@"""DoubleBakingLostExternalUnstaked"""); break;
                case "doubleAttestationRewards": columns.Add(@"""DoubleAttestationRewards"""); break;
                case "doubleAttestationLostStaked": columns.Add(@"""DoubleAttestationLostStaked"""); break;
                case "doubleAttestationLostUnstaked": columns.Add(@"""DoubleAttestationLostUnstaked"""); break;
                case "doubleAttestationLostExternalStaked": columns.Add(@"""DoubleAttestationLostExternalStaked"""); break;
                case "doubleAttestationLostExternalUnstaked": columns.Add(@"""DoubleAttestationLostExternalUnstaked"""); break;
                case "doublePreattestationRewards": columns.Add(@"""DoublePreattestationRewards"""); break;
                case "doublePreattestationLostStaked": columns.Add(@"""DoublePreattestationLostStaked"""); break;
                case "doublePreattestationLostUnstaked": columns.Add(@"""DoublePreattestationLostUnstaked"""); break;
                case "doublePreattestationLostExternalStaked": columns.Add(@"""DoublePreattestationLostExternalStaked"""); break;
                case "doublePreattestationLostExternalUnstaked": columns.Add(@"""DoublePreattestationLostExternalUnstaked"""); break;
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
            }

            if (columns.Count == 0)
                return [];

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""BakerCycles""")
                .Filter("BakerId", baker.Id)
                .Filter("Cycle", cycle)
                .Take(sort ?? new SortParameter { Desc = "cycle" }, offset, limit, x => ("Cycle", "Cycle"));

            await using var db = await DataSource.OpenConnectionAsync();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            //TODO: optimize memory allocation
            var result = new object?[rows.Count()];
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
                case "expectedAttestations":
                    foreach (var row in rows)
                        result[j++] = Math.Round(row.ExpectedAttestations, 2);
                    break;
                case "expectedDalShards":
                    foreach (var row in rows)
                        result[j++] = row.ExpectedDalShards;
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
                case "futureAttestations":
                    foreach (var row in rows)
                        result[j++] = row.FutureAttestations;
                    break;
                case "futureAttestationRewards":
                    foreach (var row in rows)
                        result[j++] = row.FutureAttestationRewards;
                    break;
                case "attestations":
                    foreach (var row in rows)
                        result[j++] = row.Attestations;
                    break;
                case "attestationRewardsDelegated":
                    foreach (var row in rows)
                        result[j++] = row.AttestationRewardsDelegated;
                    break;
                case "attestationRewardsStakedOwn":
                    foreach (var row in rows)
                        result[j++] = row.AttestationRewardsStakedOwn;
                    break;
                case "attestationRewardsStakedEdge":
                    foreach (var row in rows)
                        result[j++] = row.AttestationRewardsStakedEdge;
                    break;
                case "attestationRewardsStakedShared":
                    foreach (var row in rows)
                        result[j++] = row.AttestationRewardsStakedShared;
                    break;
                case "missedAttestations":
                    foreach (var row in rows)
                        result[j++] = row.MissedAttestations;
                    break;
                case "missedAttestationRewards":
                    foreach (var row in rows)
                        result[j++] = row.MissedAttestationRewards;
                    break;
                case "futureDalAttestationRewards":
                    foreach (var row in rows)
                        result[j++] = row.FutureDalAttestationRewards;
                    break;
                case "dalAttestationRewardsDelegated":
                    foreach (var row in rows)
                        result[j++] = row.DalAttestationRewardsDelegated;
                    break;
                case "dalAttestationRewardsStakedOwn":
                    foreach (var row in rows)
                        result[j++] = row.DalAttestationRewardsStakedOwn;
                    break;
                case "dalAttestationRewardsStakedEdge":
                    foreach (var row in rows)
                        result[j++] = row.DalAttestationRewardsStakedEdge;
                    break;
                case "dalAttestationRewardsStakedShared":
                    foreach (var row in rows)
                        result[j++] = row.DalAttestationRewardsStakedShared;
                    break;
                case "missedDalAttestationRewards":
                    foreach (var row in rows)
                        result[j++] = row.MissedDalAttestationRewards;
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
                case "doubleAttestationRewards":
                    foreach (var row in rows)
                        result[j++] = row.DoubleAttestationRewards;
                    break;
                case "doubleAttestationLostStaked":
                    foreach (var row in rows)
                        result[j++] = row.DoubleAttestationLostStaked;
                    break;
                case "doubleAttestationLostUnstaked":
                    foreach (var row in rows)
                        result[j++] = row.DoubleAttestationLostUnstaked;
                    break;
                case "doubleAttestationLostExternalStaked":
                    foreach (var row in rows)
                        result[j++] = row.DoubleAttestationLostExternalStaked;
                    break;
                case "doubleAttestationLostExternalUnstaked":
                    foreach (var row in rows)
                        result[j++] = row.DoubleAttestationLostExternalUnstaked;
                    break;
                case "doublePreattestationRewards":
                    foreach (var row in rows)
                        result[j++] = row.DoublePreattestationRewards;
                    break;
                case "doublePreattestationLostStaked":
                    foreach (var row in rows)
                        result[j++] = row.DoublePreattestationLostStaked;
                    break;
                case "doublePreattestationLostUnstaked":
                    foreach (var row in rows)
                        result[j++] = row.DoublePreattestationLostUnstaked;
                    break;
                case "doublePreattestationLostExternalStaked":
                    foreach (var row in rows)
                        result[j++] = row.DoublePreattestationLostExternalStaked;
                    break;
                case "doublePreattestationLostExternalUnstaked":
                    foreach (var row in rows)
                        result[j++] = row.DoublePreattestationLostExternalUnstaked;
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
            Int32Parameter? cycle,
            SortParameter? sort,
            OffsetParameter? offset,
            int limit,
            Symbols quote)
        {
            var acc = await Accounts.GetAsync(address);
            if (acc == null) return [];

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
                ExpectedAttestations = Math.Round(row.ExpectedAttestations, 2),
                ExpectedDalShards = row.ExpectedDalShards,
                FutureBlocks = row.FutureBlocks,
                FutureBlockRewards = row.FutureBlockRewards,
                Blocks = row.Blocks,
                BlockRewardsDelegated = row.BlockRewardsDelegated,
                BlockRewardsStakedOwn = row.BlockRewardsStakedOwn,
                BlockRewardsStakedEdge = row.BlockRewardsStakedEdge,
                BlockRewardsStakedShared = row.BlockRewardsStakedShared,
                MissedBlocks = row.MissedBlocks,
                MissedBlockRewards = row.MissedBlockRewards,
                FutureAttestations = row.FutureAttestations,
                FutureAttestationRewards = row.FutureAttestationRewards,
                Attestations = row.Attestations,
                AttestationRewardsDelegated = row.AttestationRewardsDelegated,
                AttestationRewardsStakedOwn = row.AttestationRewardsStakedOwn,
                AttestationRewardsStakedEdge = row.AttestationRewardsStakedEdge,
                AttestationRewardsStakedShared = row.AttestationRewardsStakedShared,
                MissedAttestations = row.MissedAttestations,
                MissedAttestationRewards = row.MissedAttestationRewards,
                FutureDalAttestationRewards = row.FutureDalAttestationRewards,
                DalAttestationRewardsDelegated = row.DalAttestationRewardsDelegated,
                DalAttestationRewardsStakedOwn = row.DalAttestationRewardsStakedOwn,
                DalAttestationRewardsStakedEdge = row.DalAttestationRewardsStakedEdge,
                DalAttestationRewardsStakedShared = row.DalAttestationRewardsStakedShared,
                MissedDalAttestationRewards = row.MissedDalAttestationRewards,
                BlockFees = row.BlockFees,
                MissedBlockFees = row.MissedBlockFees,
                DoubleBakingRewards = row.DoubleBakingRewards,
                DoubleBakingLostStaked = row.DoubleBakingLostStaked,
                DoubleBakingLostUnstaked = row.DoubleBakingLostUnstaked,
                DoubleBakingLostExternalStaked = row.DoubleBakingLostExternalStaked,
                DoubleBakingLostExternalUnstaked = row.DoubleBakingLostExternalUnstaked,
                DoubleAttestationRewards = row.DoubleAttestationRewards,
                DoubleAttestationLostStaked = row.DoubleAttestationLostStaked,
                DoubleAttestationLostUnstaked = row.DoubleAttestationLostUnstaked,
                DoubleAttestationLostExternalStaked = row.DoubleAttestationLostExternalStaked,
                DoubleAttestationLostExternalUnstaked = row.DoubleAttestationLostExternalUnstaked,
                DoublePreattestationRewards = row.DoublePreattestationRewards,
                DoublePreattestationLostStaked = row.DoublePreattestationLostStaked,
                DoublePreattestationLostUnstaked = row.DoublePreattestationLostUnstaked,
                DoublePreattestationLostExternalStaked = row.DoublePreattestationLostExternalStaked,
                DoublePreattestationLostExternalUnstaked = row.DoublePreattestationLostExternalUnstaked,
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

        public async Task<object?[][]> GetDelegatorRewards(
            string address,
            Int32Parameter? cycle,
            SortParameter? sort,
            OffsetParameter? offset,
            int limit,
            string[] fields,
            Symbols quote)
        {
            var acc = await Accounts.GetAsync(address);
            if (acc == null) return [];

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
                    case "expectedAttestations": columns.Add(@"""ExpectedAttestations"""); break;
                    case "expectedDalShards": columns.Add(@"""ExpectedDalShards"""); break;
                    case "futureBlocks": columns.Add(@"""FutureBlocks"""); break;
                    case "futureBlockRewards": columns.Add(@"""FutureBlockRewards"""); break;
                    case "blocks": columns.Add(@"""Blocks"""); break;
                    case "blockRewardsDelegated": columns.Add(@"""BlockRewardsDelegated"""); break;
                    case "blockRewardsStakedOwn": columns.Add(@"""BlockRewardsStakedOwn"""); break;
                    case "blockRewardsStakedEdge": columns.Add(@"""BlockRewardsStakedEdge"""); break;
                    case "blockRewardsStakedShared": columns.Add(@"""BlockRewardsStakedShared"""); break;
                    case "missedBlocks": columns.Add(@"""MissedBlocks"""); break;
                    case "missedBlockRewards": columns.Add(@"""MissedBlockRewards"""); break;
                    case "futureAttestations": columns.Add(@"""FutureAttestations"""); break;
                    case "futureAttestationRewards": columns.Add(@"""FutureAttestationRewards"""); break;
                    case "attestations": columns.Add(@"""Attestations"""); break;
                    case "attestationRewardsDelegated": columns.Add(@"""AttestationRewardsDelegated"""); break;
                    case "attestationRewardsStakedOwn": columns.Add(@"""AttestationRewardsStakedOwn"""); break;
                    case "attestationRewardsStakedEdge": columns.Add(@"""AttestationRewardsStakedEdge"""); break;
                    case "attestationRewardsStakedShared": columns.Add(@"""AttestationRewardsStakedShared"""); break;
                    case "missedAttestations": columns.Add(@"""MissedAttestations"""); break;
                    case "missedAttestationRewards": columns.Add(@"""MissedAttestationRewards"""); break;
                    case "futureDalAttestationRewards": columns.Add(@"""FutureDalAttestationRewards"""); break;
                    case "dalAttestationRewardsDelegated": columns.Add(@"""DalAttestationRewardsDelegated"""); break;
                    case "dalAttestationRewardsStakedOwn": columns.Add(@"""DalAttestationRewardsStakedOwn"""); break;
                    case "dalAttestationRewardsStakedEdge": columns.Add(@"""DalAttestationRewardsStakedEdge"""); break;
                    case "dalAttestationRewardsStakedShared": columns.Add(@"""DalAttestationRewardsStakedShared"""); break;
                    case "missedDalAttestationRewards": columns.Add(@"""MissedDalAttestationRewards"""); break;
                    case "blockFees": columns.Add(@"""BlockFees"""); break;
                    case "missedBlockFees": columns.Add(@"""MissedBlockFees"""); break;
                    case "doubleBakingRewards": columns.Add(@"""DoubleBakingRewards"""); break;
                    case "doubleBakingLostStaked": columns.Add(@"""DoubleBakingLostStaked"""); break;
                    case "doubleBakingLostUnstaked": columns.Add(@"""DoubleBakingLostUnstaked"""); break;
                    case "doubleBakingLostExternalStaked": columns.Add(@"""DoubleBakingLostExternalStaked"""); break;
                    case "doubleBakingLostExternalUnstaked": columns.Add(@"""DoubleBakingLostExternalUnstaked"""); break;
                    case "doubleAttestationRewards": columns.Add(@"""DoubleAttestationRewards"""); break;
                    case "doubleAttestationLostStaked": columns.Add(@"""DoubleAttestationLostStaked"""); break;
                    case "doubleAttestationLostUnstaked": columns.Add(@"""DoubleAttestationLostUnstaked"""); break;
                    case "doubleAttestationLostExternalStaked": columns.Add(@"""DoubleAttestationLostExternalStaked"""); break;
                    case "doubleAttestationLostExternalUnstaked": columns.Add(@"""DoubleAttestationLostExternalUnstaked"""); break;
                    case "doublePreattestationRewards": columns.Add(@"""DoublePreattestationRewards"""); break;
                    case "doublePreattestationLostStaked": columns.Add(@"""DoublePreattestationLostStaked"""); break;
                    case "doublePreattestationLostUnstaked": columns.Add(@"""DoublePreattestationLostUnstaked"""); break;
                    case "doublePreattestationLostExternalStaked": columns.Add(@"""DoublePreattestationLostExternalStaked"""); break;
                    case "doublePreattestationLostExternalUnstaked": columns.Add(@"""DoublePreattestationLostExternalUnstaked"""); break;
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
                }
            }

            if (columns.Count == 0)
                return [];

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

            var result = new object?[rows.Count()][];
            for (int i = 0; i < result.Length; i++)
                result[i] = new object?[fields.Length];

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
                    case "expectedAttestations":
                        foreach (var row in rows)
                            result[j++][i] = Math.Round(row.ExpectedAttestations, 2);
                        break;
                    case "expectedDalShards":
                        foreach (var row in rows)
                            result[j++][i] = row.ExpectedDalShards;
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
                    case "futureAttestations":
                        foreach (var row in rows)
                            result[j++][i] = row.FutureAttestations;
                        break;
                    case "futureAttestationRewards":
                        foreach (var row in rows)
                            result[j++][i] = row.FutureAttestationRewards;
                        break;
                    case "attestations":
                        foreach (var row in rows)
                            result[j++][i] = row.Attestations;
                        break;
                    case "attestationRewardsDelegated":
                        foreach (var row in rows)
                            result[j++][i] = row.AttestationRewardsDelegated;
                        break;
                    case "attestationRewardsStakedOwn":
                        foreach (var row in rows)
                            result[j++][i] = row.AttestationRewardsStakedOwn;
                        break;
                    case "attestationRewardsStakedEdge":
                        foreach (var row in rows)
                            result[j++][i] = row.AttestationRewardsStakedEdge;
                        break;
                    case "attestationRewardsStakedShared":
                        foreach (var row in rows)
                            result[j++][i] = row.AttestationRewardsStakedShared;
                        break;
                    case "missedAttestations":
                        foreach (var row in rows)
                            result[j++][i] = row.MissedAttestations;
                        break;
                    case "missedAttestationRewards":
                        foreach (var row in rows)
                            result[j++][i] = row.MissedAttestationRewards;
                        break;
                    case "futureDalAttestationRewards":
                        foreach (var row in rows)
                            result[j++][i] = row.FutureDalAttestationRewards;
                        break;
                    case "dalAttestationRewardsDelegated":
                        foreach (var row in rows)
                            result[j++][i] = row.DalAttestationRewardsDelegated;
                        break;
                    case "dalAttestationRewardsStakedOwn":
                        foreach (var row in rows)
                            result[j++][i] = row.DalAttestationRewardsStakedOwn;
                        break;
                    case "dalAttestationRewardsStakedEdge":
                        foreach (var row in rows)
                            result[j++][i] = row.DalAttestationRewardsStakedEdge;
                        break;
                    case "dalAttestationRewardsStakedShared":
                        foreach (var row in rows)
                            result[j++][i] = row.DalAttestationRewardsStakedShared;
                        break;
                    case "missedDalAttestationRewards":
                        foreach (var row in rows)
                            result[j++][i] = row.MissedDalAttestationRewards;
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
                    case "doubleAttestationRewards":
                        foreach (var row in rows)
                            result[j++][i] = row.DoubleAttestationRewards;
                        break;
                    case "doubleAttestationLostStaked":
                        foreach (var row in rows)
                            result[j++][i] = row.DoubleAttestationLostStaked;
                        break;
                    case "doubleAttestationLostUnstaked":
                        foreach (var row in rows)
                            result[j++][i] = row.DoubleAttestationLostUnstaked;
                        break;
                    case "doubleAttestationLostExternalStaked":
                        foreach (var row in rows)
                            result[j++][i] = row.DoubleAttestationLostExternalStaked;
                        break;
                    case "doubleAttestationLostExternalUnstaked":
                        foreach (var row in rows)
                            result[j++][i] = row.DoubleAttestationLostExternalUnstaked;
                        break;
                    case "doublePreattestationRewards":
                        foreach (var row in rows)
                            result[j++][i] = row.DoublePreattestationRewards;
                        break;
                    case "doublePreattestationLostStaked":
                        foreach (var row in rows)
                            result[j++][i] = row.DoublePreattestationLostStaked;
                        break;
                    case "doublePreattestationLostUnstaked":
                        foreach (var row in rows)
                            result[j++][i] = row.DoublePreattestationLostUnstaked;
                        break;
                    case "doublePreattestationLostExternalStaked":
                        foreach (var row in rows)
                            result[j++][i] = row.DoublePreattestationLostExternalStaked;
                        break;
                    case "doublePreattestationLostExternalUnstaked":
                        foreach (var row in rows)
                            result[j++][i] = row.DoublePreattestationLostExternalUnstaked;
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
                }
            }

            return result;
        }

        public async Task<object?[]> GetDelegatorRewards(
            string address,
            Int32Parameter? cycle,
            SortParameter? sort,
            OffsetParameter? offset,
            int limit,
            string field,
            Symbols quote)
        {
            var acc = await Accounts.GetAsync(address);
            if (acc == null) return [];

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
                case "expectedAttestations": columns.Add(@"""ExpectedAttestations"""); break;
                case "expectedDalShards": columns.Add(@"""ExpectedDalShards"""); break;
                case "futureBlocks": columns.Add(@"""FutureBlocks"""); break;
                case "futureBlockRewards": columns.Add(@"""FutureBlockRewards"""); break;
                case "blocks": columns.Add(@"""Blocks"""); break;
                case "blockRewardsDelegated": columns.Add(@"""BlockRewardsDelegated"""); break;
                case "blockRewardsStakedOwn": columns.Add(@"""BlockRewardsStakedOwn"""); break;
                case "blockRewardsStakedEdge": columns.Add(@"""BlockRewardsStakedEdge"""); break;
                case "blockRewardsStakedShared": columns.Add(@"""BlockRewardsStakedShared"""); break;
                case "missedBlocks": columns.Add(@"""MissedBlocks"""); break;
                case "missedBlockRewards": columns.Add(@"""MissedBlockRewards"""); break;
                case "futureAttestations": columns.Add(@"""FutureAttestations"""); break;
                case "futureAttestationRewards": columns.Add(@"""FutureAttestationRewards"""); break;
                case "attestations": columns.Add(@"""Attestations"""); break;
                case "attestationRewardsDelegated": columns.Add(@"""AttestationRewardsDelegated"""); break;
                case "attestationRewardsStakedOwn": columns.Add(@"""AttestationRewardsStakedOwn"""); break;
                case "attestationRewardsStakedEdge": columns.Add(@"""AttestationRewardsStakedEdge"""); break;
                case "attestationRewardsStakedShared": columns.Add(@"""AttestationRewardsStakedShared"""); break;
                case "missedAttestations": columns.Add(@"""MissedAttestations"""); break;
                case "missedAttestationRewards": columns.Add(@"""MissedAttestationRewards"""); break;
                case "futureDalAttestationRewards": columns.Add(@"""FutureDalAttestationRewards"""); break;
                case "dalAttestationRewardsDelegated": columns.Add(@"""DalAttestationRewardsDelegated"""); break;
                case "dalAttestationRewardsStakedOwn": columns.Add(@"""DalAttestationRewardsStakedOwn"""); break;
                case "dalAttestationRewardsStakedEdge": columns.Add(@"""DalAttestationRewardsStakedEdge"""); break;
                case "dalAttestationRewardsStakedShared": columns.Add(@"""DalAttestationRewardsStakedShared"""); break;
                case "missedDalAttestationRewards": columns.Add(@"""MissedDalAttestationRewards"""); break;
                case "blockFees": columns.Add(@"""BlockFees"""); break;
                case "missedBlockFees": columns.Add(@"""MissedBlockFees"""); break;
                case "doubleBakingRewards": columns.Add(@"""DoubleBakingRewards"""); break;
                case "doubleBakingLostStaked": columns.Add(@"""DoubleBakingLostStaked"""); break;
                case "doubleBakingLostUnstaked": columns.Add(@"""DoubleBakingLostUnstaked"""); break;
                case "doubleBakingLostExternalStaked": columns.Add(@"""DoubleBakingLostExternalStaked"""); break;
                case "doubleBakingLostExternalUnstaked": columns.Add(@"""DoubleBakingLostExternalUnstaked"""); break;
                case "doubleAttestationRewards": columns.Add(@"""DoubleAttestationRewards"""); break;
                case "doubleAttestationLostStaked": columns.Add(@"""DoubleAttestationLostStaked"""); break;
                case "doubleAttestationLostUnstaked": columns.Add(@"""DoubleAttestationLostUnstaked"""); break;
                case "doubleAttestationLostExternalStaked": columns.Add(@"""DoubleAttestationLostExternalStaked"""); break;
                case "doubleAttestationLostExternalUnstaked": columns.Add(@"""DoubleAttestationLostExternalUnstaked"""); break;
                case "doublePreattestationRewards": columns.Add(@"""DoublePreattestationRewards"""); break;
                case "doublePreattestationLostStaked": columns.Add(@"""DoublePreattestationLostStaked"""); break;
                case "doublePreattestationLostUnstaked": columns.Add(@"""DoublePreattestationLostUnstaked"""); break;
                case "doublePreattestationLostExternalStaked": columns.Add(@"""DoublePreattestationLostExternalStaked"""); break;
                case "doublePreattestationLostExternalUnstaked": columns.Add(@"""DoublePreattestationLostExternalUnstaked"""); break;
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
            }

            if (columns.Count == 0)
                return [];

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
            var result = new object?[rows.Count()];
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
                case "expectedAttestations":
                    foreach (var row in rows)
                        result[j++] = Math.Round(row.ExpectedAttestations, 2);
                    break;
                case "expectedDalShards":
                    foreach (var row in rows)
                        result[j++] = row.ExpectedDalShards;
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
                case "futureAttestations":
                    foreach (var row in rows)
                        result[j++] = row.FutureAttestations;
                    break;
                case "futureAttestationRewards":
                    foreach (var row in rows)
                        result[j++] = row.FutureAttestationRewards;
                    break;
                case "attestations":
                    foreach (var row in rows)
                        result[j++] = row.Attestations;
                    break;
                case "attestationRewardsDelegated":
                    foreach (var row in rows)
                        result[j++] = row.AttestationRewardsDelegated;
                    break;
                case "attestationRewardsStakedOwn":
                    foreach (var row in rows)
                        result[j++] = row.AttestationRewardsStakedOwn;
                    break;
                case "attestationRewardsStakedEdge":
                    foreach (var row in rows)
                        result[j++] = row.AttestationRewardsStakedEdge;
                    break;
                case "attestationRewardsStakedShared":
                    foreach (var row in rows)
                        result[j++] = row.AttestationRewardsStakedShared;
                    break;
                case "missedAttestations":
                    foreach (var row in rows)
                        result[j++] = row.MissedAttestations;
                    break;
                case "missedAttestationRewards":
                    foreach (var row in rows)
                        result[j++] = row.MissedAttestationRewards;
                    break;
                case "futureDalAttestationRewards":
                    foreach (var row in rows)
                        result[j++] = row.FutureDalAttestationRewards;
                    break;
                case "dalAttestationRewardsDelegated":
                    foreach (var row in rows)
                        result[j++] = row.DalAttestationRewardsDelegated;
                    break;
                case "dalAttestationRewardsStakedOwn":
                    foreach (var row in rows)
                        result[j++] = row.DalAttestationRewardsStakedOwn;
                    break;
                case "dalAttestationRewardsStakedEdge":
                    foreach (var row in rows)
                        result[j++] = row.DalAttestationRewardsStakedEdge;
                    break;
                case "dalAttestationRewardsStakedShared":
                    foreach (var row in rows)
                        result[j++] = row.DalAttestationRewardsStakedShared;
                    break;
                case "missedDalAttestationRewards":
                    foreach (var row in rows)
                        result[j++] = row.MissedDalAttestationRewards;
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
                case "doubleAttestationRewards":
                    foreach (var row in rows)
                        result[j++] = row.DoubleAttestationRewards;
                    break;
                case "doubleAttestationLostStaked":
                    foreach (var row in rows)
                        result[j++] = row.DoubleAttestationLostStaked;
                    break;
                case "doubleAttestationLostUnstaked":
                    foreach (var row in rows)
                        result[j++] = row.DoubleAttestationLostUnstaked;
                    break;
                case "doubleAttestationLostExternalStaked":
                    foreach (var row in rows)
                        result[j++] = row.DoubleAttestationLostExternalStaked;
                    break;
                case "doubleAttestationLostExternalUnstaked":
                    foreach (var row in rows)
                        result[j++] = row.DoubleAttestationLostExternalUnstaked;
                    break;
                case "doublePreattestationRewards":
                    foreach (var row in rows)
                        result[j++] = row.DoublePreattestationRewards;
                    break;
                case "doublePreattestationLostStaked":
                    foreach (var row in rows)
                        result[j++] = row.DoublePreattestationLostStaked;
                    break;
                case "doublePreattestationLostUnstaked":
                    foreach (var row in rows)
                        result[j++] = row.DoublePreattestationLostUnstaked;
                    break;
                case "doublePreattestationLostExternalStaked":
                    foreach (var row in rows)
                        result[j++] = row.DoublePreattestationLostExternalStaked;
                    break;
                case "doublePreattestationLostExternalUnstaked":
                    foreach (var row in rows)
                        result[j++] = row.DoublePreattestationLostExternalUnstaked;
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
            }

            return result;
        }
        #endregion

        #region split
        public async Task<RewardSplit?> GetRewardSplit(string address, int cycle, int offset, int limit)
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
                ExpectedAttestations = Math.Round(rewards.ExpectedAttestations, 2),
                ExpectedDalShards = rewards.ExpectedDalShards,
                FutureBlocks = rewards.FutureBlocks,
                FutureBlockRewards = rewards.FutureBlockRewards,
                Blocks = rewards.Blocks,
                BlockRewardsDelegated = rewards.BlockRewardsDelegated,
                BlockRewardsStakedOwn = rewards.BlockRewardsStakedOwn,
                BlockRewardsStakedEdge = rewards.BlockRewardsStakedEdge,
                BlockRewardsStakedShared = rewards.BlockRewardsStakedShared,
                MissedBlocks = rewards.MissedBlocks,
                MissedBlockRewards = rewards.MissedBlockRewards,
                FutureAttestations = rewards.FutureAttestations,
                FutureAttestationRewards = rewards.FutureAttestationRewards,
                Attestations = rewards.Attestations,
                AttestationRewardsDelegated = rewards.AttestationRewardsDelegated,
                AttestationRewardsStakedOwn = rewards.AttestationRewardsStakedOwn,
                AttestationRewardsStakedEdge = rewards.AttestationRewardsStakedEdge,
                AttestationRewardsStakedShared = rewards.AttestationRewardsStakedShared,
                MissedAttestations = rewards.MissedAttestations,
                MissedAttestationRewards = rewards.MissedAttestationRewards,
                FutureDalAttestationRewards = rewards.FutureDalAttestationRewards,
                DalAttestationRewardsDelegated = rewards.DalAttestationRewardsDelegated,
                DalAttestationRewardsStakedOwn = rewards.DalAttestationRewardsStakedOwn,
                DalAttestationRewardsStakedEdge = rewards.DalAttestationRewardsStakedEdge,
                DalAttestationRewardsStakedShared = rewards.DalAttestationRewardsStakedShared,
                MissedDalAttestationRewards = rewards.MissedDalAttestationRewards,
                BlockFees = rewards.BlockFees,
                MissedBlockFees = rewards.MissedBlockFees,
                DoubleBakingRewards = rewards.DoubleBakingRewards,
                DoubleBakingLostStaked = rewards.DoubleBakingLostStaked,
                DoubleBakingLostUnstaked = rewards.DoubleBakingLostUnstaked,
                DoubleBakingLostExternalStaked = rewards.DoubleBakingLostExternalStaked,
                DoubleBakingLostExternalUnstaked = rewards.DoubleBakingLostExternalUnstaked,
                DoubleAttestationRewards = rewards.DoubleAttestationRewards,
                DoubleAttestationLostStaked = rewards.DoubleAttestationLostStaked,
                DoubleAttestationLostUnstaked = rewards.DoubleAttestationLostUnstaked,
                DoubleAttestationLostExternalStaked = rewards.DoubleAttestationLostExternalStaked,
                DoubleAttestationLostExternalUnstaked = rewards.DoubleAttestationLostExternalUnstaked,
                DoublePreattestationRewards = rewards.DoublePreattestationRewards,
                DoublePreattestationLostStaked = rewards.DoublePreattestationLostStaked,
                DoublePreattestationLostUnstaked = rewards.DoublePreattestationLostUnstaked,
                DoublePreattestationLostExternalStaked = rewards.DoublePreattestationLostExternalStaked,
                DoublePreattestationLostExternalUnstaked = rewards.DoublePreattestationLostExternalUnstaked,
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
                    var delegator = Accounts.Get((int)x.DelegatorId)!;
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

        public async Task<SplitDelegator?> GetRewardSplitDelegator(string bakerAddress, int cycle, string delegatorAddress)
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
    }
}
