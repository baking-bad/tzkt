using System.Numerics;
using Dapper;
using Npgsql;
using Tzkt.Api.Models;
using Tzkt.Api.Services.Cache;

namespace Tzkt.Api.Repositories
{
    public partial class RewardsRepository(NpgsqlDataSource dataSource, AccountsCache accounts, ProtocolsCache protocols, QuotesCache quotes)
    {
        public async Task<RewardSplit?> GetRewardSplit(string address, int cycle, int offset, int limit)
        {
            if (await accounts.GetAsync(address) is not RawDelegate baker)
                return null;

            var sqlRewards = $"""
                SELECT  *
                FROM    "BakerCycles"
                WHERE   "BakerId" = {baker.Id}
                AND     "Cycle" = {cycle}
                LIMIT   1
                """;

            var sqlDelegators = $"""
                SELECT      "DelegatorId", "DelegatedBalance"
                FROM        "DelegatorCycles"
                WHERE       "BakerId" = {baker.Id}
                AND         "Cycle" = {cycle}
                ORDER BY    "DelegatedBalance" DESC, "Id" DESC
                OFFSET      {offset}
                LIMIT       {limit}
                """;

            var sqlStakers = $"""
                SELECT      "DelegatorId", "StakedPseudotokens"
                FROM        "DelegatorCycles"
                WHERE       "BakerId" = {baker.Id}
                AND         "Cycle" = {cycle}
                AND         "StakedPseudotokens" IS NOT NULL
                ORDER BY    "StakedPseudotokens" DESC, "Id" DESC
                OFFSET      {offset}
                LIMIT       {limit}
                """;

            var sqlActualStakers = $"""
                SELECT      sc."StakerId", sc."InitialStake", {FinalStakeColumn}, {RewardsColumn}
                FROM        "StakerCycles" AS sc
                INNER JOIN  "Accounts" AS baker ON baker."Id" = sc."BakerId"
                INNER JOIN  "Accounts" AS staker ON staker."Id" = sc."StakerId"
                WHERE       sc."BakerId" = {baker.Id}
                AND         sc."Cycle" = {cycle}
                ORDER BY    "_Rewards" DESC, sc."Id" DESC
                OFFSET      {offset}
                LIMIT       {limit}
                """;

            await using var db = await dataSource.OpenConnectionAsync();
            using var result = await db.QueryMultipleAsync($"""
                {sqlRewards};
                {sqlDelegators};
                {sqlStakers};
                {sqlActualStakers};
                """);

            var rewards = result.ReadFirstOrDefault();
            if (rewards == null) return null;
            var delegators = result.Read();
            var stakers = result.Read();
            var actualStakers = result.Read();

            return new RewardSplit
            {
                Cycle = rewards.Cycle,
                OwnDelegatedBalance = rewards.OwnDelegatedBalance,
                ExternalDelegatedBalance = rewards.ExternalDelegatedBalance,
                DelegatorsCount = rewards.DelegatorsCount,
                OwnStakedBalance = rewards.OwnStakedBalance,
                ExternalStakedBalance = rewards.ExternalStakedBalance,
                StakersCount = rewards.StakersCount,
                IssuedPseudotokens = rewards.IssuedPseudotokens,
                BakingPower = rewards.BakingPower,
                TotalBakingPower = rewards.TotalBakingPower,
                ExpectedBlocks = Math.Round(rewards.ExpectedBlocks, 2),
                FutureBlocks = rewards.FutureBlocks,
                FutureBlockRewards = rewards.FutureBlockRewards,
                Blocks = rewards.Blocks,
                BlockRewardsDelegated = rewards.BlockRewardsDelegated,
                BlockRewardsStakedOwn = rewards.BlockRewardsStakedOwn,
                BlockRewardsStakedEdge = rewards.BlockRewardsStakedEdge,
                BlockRewardsStakedShared = rewards.BlockRewardsStakedShared,
                MissedBlocks = rewards.MissedBlocks,
                MissedBlockRewards = rewards.MissedBlockRewards,
                ExpectedAttestations = Math.Round(rewards.ExpectedAttestations, 2),
                FutureAttestations = rewards.FutureAttestations,
                FutureAttestationRewards = rewards.FutureAttestationRewards,
                Attestations = rewards.Attestations,
                AttestationRewardsDelegated = rewards.AttestationRewardsDelegated,
                AttestationRewardsStakedOwn = rewards.AttestationRewardsStakedOwn,
                AttestationRewardsStakedEdge = rewards.AttestationRewardsStakedEdge,
                AttestationRewardsStakedShared = rewards.AttestationRewardsStakedShared,
                MissedAttestations = rewards.MissedAttestations,
                MissedAttestationRewards = rewards.MissedAttestationRewards,
                ExpectedDalAttestations = rewards.ExpectedDalAttestations,
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
                DoubleConsensusRewards = rewards.DoubleConsensusRewards,
                DoubleConsensusLostStaked = rewards.DoubleConsensusLostStaked,
                DoubleConsensusLostUnstaked = rewards.DoubleConsensusLostUnstaked,
                DoubleConsensusLostExternalStaked = rewards.DoubleConsensusLostExternalStaked,
                DoubleConsensusLostExternalUnstaked = rewards.DoubleConsensusLostExternalUnstaked,
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
                    var delegator = accounts.Get((int)x.DelegatorId)!;
                    return new SplitDelegator
                    {
                        Address = delegator.Address,
                        DelegatedBalance = x.DelegatedBalance,
                        Emptied = delegator is not RawDelegate && delegator is RawUser user && user.Balance == 0 && user.StakedPseudotokens == null
                    };
                }),
                Stakers = stakers.Select(x =>
                {
                    var delegator = accounts.Get((int)x.DelegatorId)!;
                    return new SplitStaker
                    {
                        Address = delegator.Address,
                        StakedPseudotokens = x.StakedPseudotokens,
                        StakedBalance = (long)((long)rewards.ExternalStakedBalance * (BigInteger)x.StakedPseudotokens / (BigInteger)rewards.IssuedPseudotokens)
                    };
                }),
                ActualStakers = actualStakers.Select(x =>
                {
                    var staker = accounts.Get((int)x.StakerId)!;
                    return new SplitActualStaker
                    {
                        Address = staker.Address,
                        InitialStake = x.InitialStake,
                        FinalStake = x._FinalStake,
                        Rewards = x._Rewards,
                    };
                })
            };
        }

        public async Task<SplitMember?> GetRewardSplitMember(string bakerAddress, int cycle, string memberAddress)
        {
            if (await accounts.GetAsync(bakerAddress) is not RawDelegate baker)
                return null;

            if (await accounts.GetAsync(memberAddress) is not RawAccount member)
                return null;

            var sql = $"""
                SELECT
                    dc."DelegatedBalance",
                    dc."StakedPseudotokens",
                    bc."ExternalStakedBalance",
                    bc."IssuedPseudotokens"
                FROM "DelegatorCycles" AS dc
                INNER JOIN "BakerCycles" AS bc ON bc."Cycle" = dc."Cycle" AND bc."BakerId" = dc."BakerId"
                WHERE   dc."Cycle" = {cycle}
                AND     dc."BakerId" = {baker.Id}
                AND     dc."DelegatorId" = {member.Id}
                LIMIT   1
                """;

            await using var db = await dataSource.OpenConnectionAsync();
            var row = await db.QueryFirstOrDefaultAsync(sql);
            if (row == null) return null;

            return new SplitMember
            {
                Address = member.Address,
                DelegatedBalance = row.DelegatedBalance,
                StakedPseudotokens = row.StakedPseudotokens ?? BigInteger.Zero,
                StakedBalance = row.StakedPseudotokens != null
                    ? (long)((long)row.ExternalStakedBalance * (BigInteger)row.StakedPseudotokens / (BigInteger)row.IssuedPseudotokens)
                    : 0,
                Emptied = member is not RawDelegate && member is RawUser u && u.Balance == 0 && u.StakedPseudotokens == null
            };
        }
    }
}
