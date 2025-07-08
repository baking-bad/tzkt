using Dapper;
using Tzkt.Api.Models;

namespace Tzkt.Api.Repositories
{
    public partial class RewardsRepository
    {
        async Task<IEnumerable<dynamic>> QueryBakerRewardsAsync(CycleRewardsFilter filter, Pagination pagination, List<SelectionField>? fields = null)
        {
            var select = "bc.*";
            if (fields != null)
            {
                var columns = new HashSet<string>(fields.Count);
                foreach (var field in fields)
                    ProcessBakerRewardsField(field, columns);

                if (columns.Count == 0)
                    return [];

                select = string.Join(',', columns);
            }

            var sql = new SqlBuilder($"""
                SELECT {select} FROM "BakerCycles" AS bc
                """)
                .FilterA(@"bc.""Cycle""", filter.cycle)
                .FilterA(@"bc.""BakerId""", filter.baker)
                .Take(pagination, x => x switch
                {
                    "cycle" => (@"bc.""Cycle""", @"bc.""Cycle"""),
                    _ => (@"bc.""Id""", @"bc.""Id""")
                });

            await using var db = await dataSource.OpenConnectionAsync();
            return await db.QueryAsync(sql.Query, sql.Params);
        }

        public async Task<int> GetBakerRewardsCount(CycleRewardsFilter filter)
        {
            var sql = new SqlBuilder("""
                SELECT COUNT(*) FROM "BakerCycles"
                """)
                .FilterA(@"""Cycle""", filter.cycle)
                .FilterA(@"""BakerId""", filter.baker);

            await using var db = await dataSource.OpenConnectionAsync();
            return await db.QueryFirstAsync<int>(sql.Query, sql.Params);
        }

        public async Task<IEnumerable<BakerRewards>> GetBakerRewards(CycleRewardsFilter filter, Pagination pagination, Symbols quote)
        {
            var rows = await QueryBakerRewardsAsync(filter, pagination);
            return rows.Select(row => (ExtractBakerRewards(row, quote) as BakerRewards)!);
        }

        public async Task<object?[][]> GetBakerRewards(CycleRewardsFilter filter, Pagination pagination, List<SelectionField> fields, Symbols quote)
        {
            var rows = await QueryBakerRewardsAsync(filter, pagination, fields);

            var result = new object?[rows.Count()][];
            for (int i = 0; i < result.Length; i++)
                result[i] = new object?[fields.Count];

            for (int i = 0; i < fields.Count; i++)
                WriteBakerRewardsField(fields[i], rows, i, result, quote);

            return result;
        }

        #region shared
        static void ProcessBakerRewardsField(SelectionField field, HashSet<string> columns)
        {
            switch (field.Field)
            {
                case "cycle": columns.Add(@"bc.""Cycle"""); break;
                case "ownDelegatedBalance": columns.Add(@"bc.""OwnDelegatedBalance"""); break;
                case "externalDelegatedBalance": columns.Add(@"bc.""ExternalDelegatedBalance"""); break;
                case "delegatorsCount": columns.Add(@"bc.""DelegatorsCount"""); break;
                case "ownStakedBalance": columns.Add(@"bc.""OwnStakedBalance"""); break;
                case "externalStakedBalance": columns.Add(@"bc.""ExternalStakedBalance"""); break;
                case "stakersCount": columns.Add(@"bc.""StakersCount"""); break;
                case "issuedPseudotokens": columns.Add(@"bc.""IssuedPseudotokens"""); break;
                case "bakingPower": columns.Add(@"bc.""BakingPower"""); break;
                case "totalBakingPower": columns.Add(@"bc.""TotalBakingPower"""); break;
                case "expectedBlocks": columns.Add(@"bc.""ExpectedBlocks"""); break;
                case "futureBlocks": columns.Add(@"bc.""FutureBlocks"""); break;
                case "futureBlockRewards": columns.Add(@"bc.""FutureBlockRewards"""); break;
                case "blocks": columns.Add(@"bc.""Blocks"""); break;
                case "blockRewardsDelegated": columns.Add(@"bc.""BlockRewardsDelegated"""); break;
                case "blockRewardsStakedOwn": columns.Add(@"bc.""BlockRewardsStakedOwn"""); break;
                case "blockRewardsStakedEdge": columns.Add(@"bc.""BlockRewardsStakedEdge"""); break;
                case "blockRewardsStakedShared": columns.Add(@"bc.""BlockRewardsStakedShared"""); break;
                case "missedBlocks": columns.Add(@"bc.""MissedBlocks"""); break;
                case "missedBlockRewards": columns.Add(@"bc.""MissedBlockRewards"""); break;
                case "expectedAttestations": columns.Add(@"bc.""ExpectedAttestations"""); break;
                case "futureAttestations": columns.Add(@"bc.""FutureAttestations"""); break;
                case "futureAttestationRewards": columns.Add(@"bc.""FutureAttestationRewards"""); break;
                case "attestations": columns.Add(@"bc.""Attestations"""); break;
                case "attestationRewardsDelegated": columns.Add(@"bc.""AttestationRewardsDelegated"""); break;
                case "attestationRewardsStakedOwn": columns.Add(@"bc.""AttestationRewardsStakedOwn"""); break;
                case "attestationRewardsStakedEdge": columns.Add(@"bc.""AttestationRewardsStakedEdge"""); break;
                case "attestationRewardsStakedShared": columns.Add(@"bc.""AttestationRewardsStakedShared"""); break;
                case "missedAttestations": columns.Add(@"bc.""MissedAttestations"""); break;
                case "missedAttestationRewards": columns.Add(@"bc.""MissedAttestationRewards"""); break;
                case "expectedDalAttestations": columns.Add(@"bc.""ExpectedDalAttestations"""); break;
                case "futureDalAttestationRewards": columns.Add(@"bc.""FutureDalAttestationRewards"""); break;
                case "dalAttestationRewardsDelegated": columns.Add(@"bc.""DalAttestationRewardsDelegated"""); break;
                case "dalAttestationRewardsStakedOwn": columns.Add(@"bc.""DalAttestationRewardsStakedOwn"""); break;
                case "dalAttestationRewardsStakedEdge": columns.Add(@"bc.""DalAttestationRewardsStakedEdge"""); break;
                case "dalAttestationRewardsStakedShared": columns.Add(@"bc.""DalAttestationRewardsStakedShared"""); break;
                case "missedDalAttestationRewards": columns.Add(@"bc.""MissedDalAttestationRewards"""); break;
                case "blockFees": columns.Add(@"bc.""BlockFees"""); break;
                case "missedBlockFees": columns.Add(@"bc.""MissedBlockFees"""); break;
                case "doubleBakingRewards": columns.Add(@"bc.""DoubleBakingRewards"""); break;
                case "doubleBakingLostStaked": columns.Add(@"bc.""DoubleBakingLostStaked"""); break;
                case "doubleBakingLostUnstaked": columns.Add(@"bc.""DoubleBakingLostUnstaked"""); break;
                case "doubleBakingLostExternalStaked": columns.Add(@"bc.""DoubleBakingLostExternalStaked"""); break;
                case "doubleBakingLostExternalUnstaked": columns.Add(@"bc.""DoubleBakingLostExternalUnstaked"""); break;
                case "doubleAttestationRewards": columns.Add(@"bc.""DoubleAttestationRewards"""); break;
                case "doubleAttestationLostStaked": columns.Add(@"bc.""DoubleAttestationLostStaked"""); break;
                case "doubleAttestationLostUnstaked": columns.Add(@"bc.""DoubleAttestationLostUnstaked"""); break;
                case "doubleAttestationLostExternalStaked": columns.Add(@"bc.""DoubleAttestationLostExternalStaked"""); break;
                case "doubleAttestationLostExternalUnstaked": columns.Add(@"bc.""DoubleAttestationLostExternalUnstaked"""); break;
                case "doublePreattestationRewards": columns.Add(@"bc.""DoublePreattestationRewards"""); break;
                case "doublePreattestationLostStaked": columns.Add(@"bc.""DoublePreattestationLostStaked"""); break;
                case "doublePreattestationLostUnstaked": columns.Add(@"bc.""DoublePreattestationLostUnstaked"""); break;
                case "doublePreattestationLostExternalStaked": columns.Add(@"bc.""DoublePreattestationLostExternalStaked"""); break;
                case "doublePreattestationLostExternalUnstaked": columns.Add(@"bc.""DoublePreattestationLostExternalUnstaked"""); break;
                case "vdfRevelationRewardsDelegated": columns.Add(@"bc.""VdfRevelationRewardsDelegated"""); break;
                case "vdfRevelationRewardsStakedOwn": columns.Add(@"bc.""VdfRevelationRewardsStakedOwn"""); break;
                case "vdfRevelationRewardsStakedEdge": columns.Add(@"bc.""VdfRevelationRewardsStakedEdge"""); break;
                case "vdfRevelationRewardsStakedShared": columns.Add(@"bc.""VdfRevelationRewardsStakedShared"""); break;
                case "nonceRevelationRewardsDelegated": columns.Add(@"bc.""NonceRevelationRewardsDelegated"""); break;
                case "nonceRevelationRewardsStakedOwn": columns.Add(@"bc.""NonceRevelationRewardsStakedOwn"""); break;
                case "nonceRevelationRewardsStakedEdge": columns.Add(@"bc.""NonceRevelationRewardsStakedEdge"""); break;
                case "nonceRevelationRewardsStakedShared": columns.Add(@"bc.""NonceRevelationRewardsStakedShared"""); break;
                case "nonceRevelationLosses": columns.Add(@"bc.""NonceRevelationLosses"""); break;
                case "quote": columns.Add(@"bc.""Cycle"""); break;
            }
        }

        void WriteBakerRewardsField(SelectionField field, IEnumerable<dynamic> rows, int i, object?[][] result, Symbols quote)
        {
            var j = 0;
            switch (field.Full)
            {
                case "cycle":
                    foreach (var row in rows)
                        result[j++][i] = row.Cycle;
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
                case "issuedPseudotokens":
                    foreach (var row in rows)
                        result[j++][i] = row.IssuedPseudotokens;
                    break;
                case "bakingPower":
                    foreach (var row in rows)
                        result[j++][i] = row.BakingPower;
                    break;
                case "totalBakingPower":
                    foreach (var row in rows)
                        result[j++][i] = row.TotalBakingPower;
                    break;
                case "expectedBlocks":
                    foreach (var row in rows)
                        result[j++][i] = Math.Round(row.ExpectedBlocks, 2);
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
                case "expectedAttestations":
                    foreach (var row in rows)
                        result[j++][i] = Math.Round(row.ExpectedAttestations, 2);
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
                case "expectedDalAttestations":
                    foreach (var row in rows)
                        result[j++][i] = row.ExpectedDalAttestations;
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
                        result[j++][i] = quotes.Get(quote, protocols.FindByCycle((int)row.Cycle).GetCycleEnd((int)row.Cycle));
                    break;
            }
        }

        BakerRewards ExtractBakerRewards(dynamic row, Symbols quote) => new()
        {
            Cycle = row.Cycle,
            OwnDelegatedBalance = row.OwnDelegatedBalance,
            ExternalDelegatedBalance = row.ExternalDelegatedBalance,
            DelegatorsCount = row.DelegatorsCount,
            OwnStakedBalance = row.OwnStakedBalance,
            ExternalStakedBalance = row.ExternalStakedBalance,
            StakersCount = row.StakersCount,
            IssuedPseudotokens = row.IssuedPseudotokens,
            BakingPower = row.BakingPower,
            TotalBakingPower = row.TotalBakingPower,
            ExpectedBlocks = Math.Round(row.ExpectedBlocks, 2),
            FutureBlocks = row.FutureBlocks,
            FutureBlockRewards = row.FutureBlockRewards,
            Blocks = row.Blocks,
            BlockRewardsDelegated = row.BlockRewardsDelegated,
            BlockRewardsStakedOwn = row.BlockRewardsStakedOwn,
            BlockRewardsStakedEdge = row.BlockRewardsStakedEdge,
            BlockRewardsStakedShared = row.BlockRewardsStakedShared,
            MissedBlocks = row.MissedBlocks,
            MissedBlockRewards = row.MissedBlockRewards,
            ExpectedAttestations = Math.Round(row.ExpectedAttestations, 2),
            FutureAttestations = row.FutureAttestations,
            FutureAttestationRewards = row.FutureAttestationRewards,
            Attestations = row.Attestations,
            AttestationRewardsDelegated = row.AttestationRewardsDelegated,
            AttestationRewardsStakedOwn = row.AttestationRewardsStakedOwn,
            AttestationRewardsStakedEdge = row.AttestationRewardsStakedEdge,
            AttestationRewardsStakedShared = row.AttestationRewardsStakedShared,
            MissedAttestations = row.MissedAttestations,
            MissedAttestationRewards = row.MissedAttestationRewards,
            ExpectedDalAttestations = row.ExpectedDalAttestations,
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
            Quote = quotes.Get(quote, protocols.FindByCycle((int)row.Cycle).GetCycleEnd((int)row.Cycle))
        };
        #endregion
    }
}
