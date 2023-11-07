using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto18
{
    class SnapshotBalanceCommit : Proto12.SnapshotBalanceCommit
    {
        public SnapshotBalanceCommit(ProtocolHandler protocol) : base(protocol) { }

        protected override Task TakeSnapshot(Block block)
        {
            return Db.Database.ExecuteSqlRawAsync($"""
                INSERT INTO "SnapshotBalances" (
                    "Level",
                    "AccountId",
                    "BakerId",
                    "OwnDelegatedBalance",
                    "ExternalDelegatedBalance",
                    "DelegatorsCount",
                    "OwnStakedBalance",
                    "ExternalStakedBalance",
                    "StakersCount",
                    "StakedPseudotokens",
                    "IssuedPseudotokens"
                )

                SELECT
                    {block.Level},
                    "Id",
                    COALESCE("DelegateId", "Id"),
                    "Balance" - COALESCE("StakedBalance", 0) - (CASE
                                                                WHEN "UnstakedBakerId" IS NOT NULL
                                                                AND "UnstakedBakerId" != "DelegateId"
                                                                AND "UnstakedBakerId" != "Id"
                                                                THEN "UnstakedBalance"
                                                                ELSE 0
                                                                END),
                    COALESCE("DelegatedBalance", 0),
                    COALESCE("DelegatorsCount", 0),
                    COALESCE("StakedBalance", 0),
                    COALESCE("ExternalStakedBalance", 0),
                    COALESCE("StakersCount", 0),
                    COALESCE("StakedPseudotokens", 0),
                    COALESCE("IssuedPseudotokens", 0)
                FROM "Accounts"
                WHERE "Staked" = true

                UNION ALL
                
                SELECT
                    {block.Level},
                    account."Id",
                    account."UnstakedBakerId",
                    account."UnstakedBalance",
                    0,
                    0,
                    0,
                    0,
                    0,
                    0,
                    0
                FROM "Accounts" as account
                INNER JOIN "Accounts" as unstakedBaker
                ON unstakedBaker."Id" = account."UnstakedBakerId"
                WHERE unstakedBaker."Staked" = true
                AND account."UnstakedBakerId" != account."DelegateId"
                AND account."UnstakedBakerId" != account."Id"
                """);
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
                        ? await Db.Users.Where(x => x.UnstakedBakerId == baker.Id).ToListAsync()
                        : new(0);

                    values.Add("(" + string.Join(',',
                        block.Level,
                        baker.Id,
                        baker.Id,
                        baker.Balance - baker.StakedBalance - (baker.UnstakedBakerId != null && baker.UnstakedBakerId != baker.Id ? baker.UnstakedBalance : 0),
                        baker.DelegatedBalance,
                        baker.DelegatorsCount,
                        baker.StakedBalance,
                        baker.ExternalStakedBalance,
                        baker.StakersCount,
                        baker.StakedPseudotokens,
                        baker.IssuedPseudotokens) + ")");

                    foreach (var delegator in delegators)
                    {
                        values.Add("(" + string.Join(',',
                            block.Level,
                            delegator.Id,
                            delegator.DelegateId,
                            delegator.Balance - (delegator is User user
                                ? (user.StakedBalance - (user.UnstakedBakerId != null && user.UnstakedBakerId != user.DelegateId ? user.UnstakedBalance : 0))
                                : 0),
                            0,
                            0,
                            (delegator as User)?.StakedBalance,
                            0,
                            0,
                            (delegator as User)?.StakedPseudotokens ?? 0,
                            (delegator as Data.Models.Delegate)?.IssuedPseudotokens ?? 0) + ")");
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
                            0,
                            0,
                            0) + ")");
                    }
                }
                if (values.Count > 0)
                {
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
                            "StakersCount",
                            "StakedPseudotokens",
                            "IssuedPseudotokens"
                        )
                        VALUES
                        {string.Join(",\n", values)}
                        """);
                }
            }
        }

        protected override async Task SubtractCycleRewards(JsonElement rawBlock, Block block)
        {
            if (!block.Events.HasFlag(BlockEvents.CycleEnd))
                return;

            await Db.Database.ExecuteSqlRawAsync($"""
                UPDATE "SnapshotBalances" as sb
                SET 
                    "OwnDelegatedBalance" = "OwnDelegatedBalance" - bc."EndorsementRewardsLiquid",
                    "OwnStakedBalance" = "OwnStakedBalance" - bc."EndorsementRewardsStakedOwn",
                    "ExternalStakedBalance" = "ExternalStakedBalance" - bc."EndorsementRewardsStakedShared"
                FROM (
                    SELECT "BakerId", "EndorsementRewardsLiquid", "EndorsementRewardsStakedOwn", "EndorsementRewardsStakedShared"
                    FROM "BakerCycles"
                    WHERE "Cycle" = {block.Cycle}
                    AND ("EndorsementRewardsLiquid" != 0 OR "EndorsementRewardsStakedOwn" != 0 OR "EndorsementRewardsStakedShared" != 0)
                ) as bc
                WHERE sb."Level" = {block.Level}
                AND sb."AccountId" = bc."BakerId"
                AND sb."BakerId" = bc."BakerId"
                """);
        }
    }
}
