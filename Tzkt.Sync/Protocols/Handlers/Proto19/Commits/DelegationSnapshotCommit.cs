using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto19
{
    class DelegationSnapshotCommit(ProtocolHandler protocol) : ProtocolCommit(protocol)
    {
        public async Task Apply()
        {
            if (Context.Block.Events.HasFlag(BlockEvents.CycleBegin))
            {
                await Db.Database.ExecuteSqlRawAsync("""
                    DELETE FROM "DelegationSnapshots"
                    WHERE "Level" < {0}
                    """, Context.Block.Level - Context.Protocol.BlocksPerCycle);

                await Db.Database.ExecuteSqlRawAsync("""
                    INSERT INTO "DelegationSnapshots" (
                        "Level",
                        "BakerId",
                        "AccountId",
                        "OwnDelegatedBalance",
                        "ExternalDelegatedBalance",
                        "DelegatorsCount",
                        "PrevMinTotalDelegatedLevel",
                        "PrevMinTotalDelegated"
                    )

                    SELECT
                        {0},
                        "Id",
                        "Id",
                        "Balance" - "OwnStakedBalance" - (CASE
                                                          WHEN "UnstakedBakerId" IS NOT NULL AND  "UnstakedBakerId" != "Id"
                                                          THEN "UnstakedBalance"
                                                          ELSE 0
                                                          END),
                        "DelegatedBalance",
                        "DelegatorsCount",
                        "MinTotalDelegatedLevel",
                        "MinTotalDelegated"
                    FROM "Accounts"
                    WHERE "Type" = {1}

                    UNION ALL
                    
                    SELECT
                        {0},
                        "DelegateId",
                        "Id",
                        "Balance" - (CASE
                                     WHEN "UnstakedBakerId" IS NOT NULL AND  "UnstakedBakerId" != "DelegateId"
                                     THEN "UnstakedBalance"
                                     ELSE 0
                                     END),
                        NULL::bigint,
                        NULL::integer,
                        NULL::integer,
                        NULL::bigint
                    FROM "Accounts"
                    WHERE "Type" != {1}
                    AND "DelegateId" IS NOT NULL
                    
                    UNION ALL
                    
                    SELECT
                        {0},
                        "UnstakedBakerId",
                        "Id",
                        "UnstakedBalance",
                        NULL::bigint,
                        NULL::integer,
                        NULL::integer,
                        NULL::bigint
                    FROM "Accounts"
                    WHERE "UnstakedBakerId" IS NOT NULL
                    AND "UnstakedBakerId" IS DISTINCT FROM "DelegateId"
                    AND "UnstakedBakerId" != "Id"
                    """, Context.Block.Level, (int)AccountType.Delegate);

                foreach (var baker in Cache.Accounts.GetDelegates())
                {
                    baker.MinTotalDelegated = baker.TotalDelegated;
                    baker.MinTotalDelegatedLevel = Context.Block.Level;
                }

                await Db.Database.ExecuteSqlRawAsync("""
                    UPDATE "Accounts"
                    SET "MinTotalDelegated" = "StakingBalance" - "OwnStakedBalance" - "ExternalStakedBalance",
                        "MinTotalDelegatedLevel" = {0}
                    WHERE "Type" = {1}
                    """, Context.Block.Level, (int)AccountType.Delegate);

                await SetBlockEvent();
            }
            else if (Cache.Accounts.GetDelegates().Any(x => x.TotalDelegated < x.MinTotalDelegated))
            {
                var bakers = Cache.Accounts.GetDelegates()
                    .Where(x => x.TotalDelegated < x.MinTotalDelegated)
                    .ToList();

                var ids = bakers.Select(x => x.Id).ToList();

                await Db.Database.ExecuteSqlRawAsync("""
                    INSERT INTO "DelegationSnapshots" (
                        "Level",
                        "BakerId",
                        "AccountId",
                        "OwnDelegatedBalance",
                        "ExternalDelegatedBalance",
                        "DelegatorsCount",
                        "PrevMinTotalDelegatedLevel",
                        "PrevMinTotalDelegated"
                    )

                    SELECT
                        {0},
                        "Id",
                        "Id",
                        "Balance" - "OwnStakedBalance" - (CASE
                                                          WHEN "UnstakedBakerId" IS NOT NULL AND "UnstakedBakerId" != "Id"
                                                          THEN "UnstakedBalance"
                                                          ELSE 0
                                                          END),
                        "DelegatedBalance",
                        "DelegatorsCount",
                        "MinTotalDelegatedLevel",
                        "MinTotalDelegated"
                    FROM "Accounts"
                    WHERE "Id" = ANY({1})

                    UNION ALL
                    
                    SELECT
                        {0},
                        "DelegateId",
                        "Id",
                        "Balance" - (CASE
                                     WHEN "UnstakedBakerId" IS NOT NULL AND "UnstakedBakerId" != "DelegateId"
                                     THEN "UnstakedBalance"
                                     ELSE 0
                                     END),
                        NULL::bigint,
                        NULL::integer,
                        NULL::integer,
                        NULL::bigint
                    FROM "Accounts"
                    WHERE "DelegateId" = ANY({1})
                    AND "DelegateId" != "Id"
                    
                    UNION ALL
                    
                    SELECT
                        {0},
                        "UnstakedBakerId",
                        "Id",
                        "UnstakedBalance",
                        NULL::bigint,
                        NULL::integer,
                        NULL::integer,
                        NULL::bigint
                    FROM "Accounts"
                    WHERE "UnstakedBakerId" = ANY({1})
                    AND "UnstakedBakerId" IS DISTINCT FROM "DelegateId"
                    AND "UnstakedBakerId" != "Id"
                    """, Context.Block.Level, ids);

                foreach (var baker in bakers)
                {
                    baker.MinTotalDelegated = baker.TotalDelegated;
                    baker.MinTotalDelegatedLevel = Context.Block.Level;
                }

                await Db.Database.ExecuteSqlRawAsync("""
                    UPDATE "Accounts"
                    SET "MinTotalDelegated" = "StakingBalance" - "OwnStakedBalance" - "ExternalStakedBalance",
                        "MinTotalDelegatedLevel" = {0}
                    WHERE "Id" = ANY({1})
                    """, Context.Block.Level, ids);

                await SetBlockEvent();
            }
        }

        public async Task Revert()
        {
            if (!Context.Block.Events.HasFlag(BlockEvents.DelegationSnapshot))
                return;

            var bakerSnapshots = await Db.DelegationSnapshots
                .AsNoTracking()
                .Where(x => x.Level == Context.Block.Level && x.BakerId == x.AccountId)
                .ToListAsync();

            foreach (var snapshot in bakerSnapshots)
            {
                var baker = Cache.Accounts.GetDelegate(snapshot.BakerId);
                baker.MinTotalDelegated = snapshot.PrevMinTotalDelegated!.Value;
                baker.MinTotalDelegatedLevel = snapshot.PrevMinTotalDelegatedLevel!.Value;
            }

            await Db.Database.ExecuteSqlRawAsync("""
                UPDATE "Accounts" AS baker
                SET "MinTotalDelegated" = snapshot."PrevMinTotalDelegated",
                    "MinTotalDelegatedLevel" = snapshot."PrevMinTotalDelegatedLevel"
                FROM (
                    SELECT "BakerId", "PrevMinTotalDelegatedLevel", "PrevMinTotalDelegated"
                    FROM "DelegationSnapshots"
                    WHERE "Level" = {0}
                    AND "BakerId" = "AccountId"
                ) AS snapshot
                WHERE baker."Id" = snapshot."BakerId"
                """, Context.Block.Level);

            await Db.Database.ExecuteSqlRawAsync("""
                DELETE FROM "DelegationSnapshots"
                WHERE "Level" = {0}
                """, Context.Block.Level);
        }

        async Task SetBlockEvent()
        {
            var block = Cache.Blocks.GetCached(Context.Block.Level);
            block.Events |= BlockEvents.DelegationSnapshot;

            await Db.Database.ExecuteSqlRawAsync("""
                UPDATE "Blocks"
                SET "Events" = "Events" & {0}
                WHERE "Level" = {1}
                """, BlockEvents.DelegationSnapshot, block.Level);
        }
    }
}
