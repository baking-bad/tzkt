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
                    "BakerId",
                    "AccountId",
                    "OwnDelegatedBalance",
                    "ExternalDelegatedBalance",
                    "DelegatorsCount",
                    "OwnStakedBalance",
                    "ExternalStakedBalance",
                    "StakersCount",
                    "Pseudotokens"
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
                    "StakersCount",
                    "IssuedPseudotokens"
                FROM "Accounts"
                WHERE "Staked" = true
                AND "Type" = {1}
                
                UNION ALL

                SELECT
                    {0},
                    "DelegateId",
                    "Id",
                    "Balance" - (CASE
                                 WHEN "UnstakedBakerId" IS NOT NULL
                                 AND  "UnstakedBakerId" != "DelegateId"
                                 THEN "UnstakedBalance"
                                 ELSE 0
                                 END),
                    NULL::bigint,
                    NULL::integer,
                    NULL::bigint,
                    NULL::bigint,
                    NULL::integer,
                    "StakedPseudotokens"
                FROM "Accounts"
                WHERE "Staked" = true
                AND "Type" != {1}

                UNION ALL
                
                SELECT
                    {0},
                    account."UnstakedBakerId",
                    account."Id",
                    account."UnstakedBalance",
                    NULL::bigint,
                    NULL::integer,
                    NULL::bigint,
                    NULL::bigint,
                    NULL::integer,
                    NULL::numeric
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
                        baker.StakersCount,
                        baker.IssuedPseudotokens ?? (object)"NULL::numeric") + ")");

                    foreach (var delegator in delegators)
                    {
                        values.Add("(" + string.Join(',',
                            block.Level,
                            delegator.DelegateId,
                            delegator.Id,
                            delegator.Balance - (delegator is User user && user.UnstakedBakerId != null && user.UnstakedBakerId != user.DelegateId ? user.UnstakedBalance : 0),
                            "NULL::bigint",
                            "NULL::integer",
                            "NULL::bigint",
                            "NULL::bigint",
                            "NULL::integer",
                            (delegator as User)?.StakedPseudotokens ?? (object)"NULL::numeric") + ")");
                    }

                    foreach (var unstaker in unstakers)
                    {
                        values.Add("(" + string.Join(',',
                            block.Level,
                            unstaker.UnstakedBakerId,
                            unstaker.Id,
                            unstaker.UnstakedBalance,
                            "NULL::bigint",
                            "NULL::integer",
                            "NULL::bigint",
                            "NULL::bigint",
                            "NULL::integer",
                            "NULL::numeric") + ")");
                    }
                }
                if (values.Count > 0)
                {
#pragma warning disable EF1002 // Risk of vulnerability to SQL injection.
                    await Db.Database.ExecuteSqlRawAsync($"""
                        INSERT INTO "SnapshotBalances" (
                            "Level",
                            "BakerId",
                            "AccountId",
                            "OwnDelegatedBalance",
                            "ExternalDelegatedBalance",
                            "DelegatorsCount",
                            "OwnStakedBalance",
                            "ExternalStakedBalance",
                            "StakersCount",
                            "Pseudotokens"
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
                    "OwnDelegatedBalance" = "OwnDelegatedBalance" - bc."AttestationRewardsDelegated",
                    "OwnStakedBalance" = "OwnStakedBalance" - bc."AttestationRewardsStakedOwn" - bc."AttestationRewardsStakedEdge",
                    "ExternalStakedBalance" = "ExternalStakedBalance" - bc."AttestationRewardsStakedShared"
                FROM (
                    SELECT "BakerId", "AttestationRewardsDelegated", "AttestationRewardsStakedOwn", "AttestationRewardsStakedEdge", "AttestationRewardsStakedShared"
                    FROM "BakerCycles"
                    WHERE "Cycle" = {0}
                    AND ("AttestationRewardsDelegated" != 0 OR "AttestationRewardsStakedOwn" != 0 OR "AttestationRewardsStakedEdge" != 0 OR "AttestationRewardsStakedShared" != 0)
                ) as bc
                WHERE sb."Level" = {1}
                AND sb."BakerId" = bc."BakerId"
                AND sb."AccountId" = bc."BakerId"
                """, block.Cycle, block.Level);
        }
    }
}
