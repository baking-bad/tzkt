using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto18
{
    class SnapshotBalanceCommit(ProtocolHandler protocol) : Proto12.SnapshotBalanceCommit(protocol)
    {
        protected override Task TakeSnapshot(Block block)
        {
            return Db.Database.ExecuteSqlRawAsync("""
                INSERT INTO "SnapshotBalances" (
                    "Level",
                    "AccountId",
                    "BakerId",
                    "OwnDelegatedBalance",
                    "ExternalDelegatedBalance",
                    "DelegatorsCount",
                    "OwnStakedBalance",
                    "ExternalStakedBalance",
                    "StakersCount"
                )
                
                SELECT
                    {0},
                    "Id",
                    "Id",
                    "Balance" - "OwnStakedBalance" - (CASE
                                                      WHEN "UnstakedBakerId" IS NOT NULL
                                                      AND  "UnstakedBakerId" != "Id"
                                                      THEN "UnstakedBalance"
                                                      ELSE 0
                                                      END),
                    "DelegatedBalance",
                    "DelegatorsCount",
                    "OwnStakedBalance",
                    "ExternalStakedBalance",
                    "StakersCount"
                FROM "Accounts"
                WHERE "Staked" = true
                AND "Type" = {1}
                
                UNION ALL

                SELECT
                    {0},
                    staker."Id",
                    staker."DelegateId",
                    staker."Balance" - (CASE
                                        WHEN staker."UnstakedBakerId" IS NOT NULL
                                        AND  staker."UnstakedBakerId" != staker."DelegateId"
                                        THEN staker."UnstakedBalance"
                                        ELSE 0
                                        END),
                    0,
                    0,
                    FLOOR(baker."ExternalStakedBalance"
                        * COALESCE(staker."StakedPseudotokens", 0::numeric)
                        / COALESCE(baker."IssuedPseudotokens", 1::numeric))::bigint,
                    0,
                    0
                FROM "Accounts" AS staker
                INNER JOIN "Accounts" AS baker
                ON baker."Id" = staker."DelegateId"
                WHERE staker."Staked" = true
                AND staker."Type" != {1}

                UNION ALL
                
                SELECT
                    {0},
                    account."Id",
                    account."UnstakedBakerId",
                    account."UnstakedBalance",
                    0,
                    0,
                    0,
                    0,
                    0
                FROM "Accounts" as account
                INNER JOIN "Accounts" as unstakedBaker
                ON unstakedBaker."Id" = account."UnstakedBakerId"
                WHERE unstakedBaker."Staked" = true
                AND account."UnstakedBakerId" IS DISTINCT FROM account."DelegateId"
                AND account."UnstakedBakerId" != account."Id"
                """, block.Level, (int)AccountType.Delegate);
        }

        protected override async Task TakeDeactivatedSnapshot(Block block)
        {
            var deactivated = await Db.Delegates
                .AsNoTracking()
                .Where(x => x.DeactivationLevel == block.Level)
                .ToListAsync();

            if (deactivated.Count > 0)
            {
                var values = new List<string>();
                foreach (var baker in deactivated)
                {
                    var delegators = await Db.Accounts.Where(x => x.DelegateId == baker.Id).ToListAsync();
                    var unstakers = baker.ExternalUnstakedBalance > 0
                        ? await Db.Users
                            .Where(x =>
                                x.UnstakedBakerId != null &&
                                x.UnstakedBakerId == baker.Id &&
                                x.UnstakedBakerId != x.DelegateId &&
                                x.UnstakedBakerId != x.Id)
                            .ToListAsync()
                        : [];

                    values.Add("(" + string.Join(',',
                        block.Level,
                        baker.Id,
                        baker.Id,
                        baker.Balance - baker.OwnStakedBalance - (baker.UnstakedBakerId != null && baker.UnstakedBakerId != baker.Id ? baker.UnstakedBalance : 0),
                        baker.DelegatedBalance,
                        baker.DelegatorsCount,
                        baker.OwnStakedBalance,
                        baker.ExternalStakedBalance,
                        baker.StakersCount) + ")");

                    foreach (var delegator in delegators)
                    {
                        values.Add("(" + string.Join(',',
                            block.Level,
                            delegator.Id,
                            delegator.DelegateId,
                            delegator.Balance - (delegator is User user && user.UnstakedBakerId != null && user.UnstakedBakerId != user.DelegateId ? user.UnstakedBalance : 0),
                            0,
                            0,
                            delegator is User u && u.StakedPseudotokens != null
                                ? (long)(baker.ExternalStakedBalance * u.StakedPseudotokens.Value / baker.IssuedPseudotokens!.Value)
                                : 0L,
                            0,
                            0) + ")");
                    }

                    foreach (var unstaker in unstakers)
                    {
                        values.Add("(" + string.Join(',',
                            block.Level,
                            unstaker.Id,
                            unstaker.UnstakedBakerId,
                            unstaker.UnstakedBalance,
                            0,
                            0,
                            0,
                            0,
                            0) + ")");
                    }
                }
                if (values.Count > 0)
                {
#pragma warning disable EF1002 // Risk of vulnerability to SQL injection.
                    await Db.Database.ExecuteSqlRawAsync($"""
                        INSERT INTO "SnapshotBalances" (
                            "Level",
                            "AccountId",
                            "BakerId",
                            "OwnDelegatedBalance",
                            "ExternalDelegatedBalance",
                            "DelegatorsCount",
                            "OwnStakedBalance",
                            "ExternalStakedBalance",
                            "StakersCount"
                        )
                        VALUES
                        {string.Join(",\n", values)}
                        """);
#pragma warning restore EF1002 // Risk of vulnerability to SQL injection.
                }
            }
        }

        protected override async Task SubtractCycleRewards(JsonElement rawBlock, Block block)
        {
            if (!block.Events.HasFlag(BlockEvents.CycleEnd))
                return;

            await Db.Database.ExecuteSqlRawAsync("""
                UPDATE "SnapshotBalances" as sb
                SET 
                    "OwnDelegatedBalance" = "OwnDelegatedBalance" - bc."EndorsementRewardsDelegated",
                    "OwnStakedBalance" = "OwnStakedBalance" - bc."EndorsementRewardsStakedOwn" - bc."EndorsementRewardsStakedEdge",
                    "ExternalStakedBalance" = "ExternalStakedBalance" - bc."EndorsementRewardsStakedShared"
                FROM (
                    SELECT "BakerId", "EndorsementRewardsDelegated", "EndorsementRewardsStakedOwn", "EndorsementRewardsStakedEdge", "EndorsementRewardsStakedShared"
                    FROM "BakerCycles"
                    WHERE "Cycle" = {0}
                    AND ("EndorsementRewardsDelegated" != 0 OR "EndorsementRewardsStakedOwn" != 0 OR "EndorsementRewardsStakedEdge" != 0 OR "EndorsementRewardsStakedShared" != 0)
                ) as bc
                WHERE sb."Level" = {1}
                AND sb."AccountId" = bc."BakerId"
                AND sb."BakerId" = bc."BakerId"
                """, block.Cycle, block.Level);
        }
    }
}
