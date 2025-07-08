using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto19
{
    class SnapshotBalanceCommit(ProtocolHandler protocol) : ProtocolCommit(protocol)
    {
        public async Task Apply()
        {
            if (!Context.Block.Events.HasFlag(BlockEvents.CycleEnd))
                return;

            await Db.Database.ExecuteSqlRawAsync("""
                DELETE FROM "SnapshotBalances"
                WHERE "Level" < {0}
                """, Context.Block.Level - Context.Protocol.BlocksPerCycle);

            await Db.Database.ExecuteSqlRawAsync("""
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
                    0,
                    0,
                    0,
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
                    0,
                    NULL::bigint,
                    NULL::integer,
                    NULL::bigint,
                    NULL::bigint,
                    NULL::integer,
                    "StakedPseudotokens"
                FROM "Accounts"
                WHERE "Staked" = true
                AND "Type" != {1}
                AND "StakedPseudotokens" IS NOT NULL
                """, Context.Block.Level, (int)AccountType.Delegate);

            await Db.Database.ExecuteSqlRawAsync("""
                INSERT INTO "SnapshotBalances" (
                    "Level",
                    "BakerId",
                    "AccountId",
                    "OwnDelegatedBalance",
                    "ExternalDelegatedBalance",
                    "DelegatorsCount"
                )
                    
                SELECT
                    {0},
                    ds."BakerId",
                    ds."AccountId",
                    ds."OwnDelegatedBalance",
                    ds."ExternalDelegatedBalance",
                    ds."DelegatorsCount"
                FROM "Accounts" AS baker
                INNER JOIN "DelegationSnapshots" AS ds
                ON ds."Level" = baker."MinTotalDelegatedLevel" AND ds."BakerId" = baker."Id"
                WHERE baker."Staked" = true
                AND baker."Type" = {1}
                    
                ON CONFLICT ("Level", "BakerId", "AccountId")
                DO UPDATE
                SET
                    "OwnDelegatedBalance" = EXCLUDED."OwnDelegatedBalance",
                    "ExternalDelegatedBalance" = EXCLUDED."ExternalDelegatedBalance",
                    "DelegatorsCount" = EXCLUDED."DelegatorsCount"
                """, Context.Block.Level, (int)AccountType.Delegate);
        }

        public async Task Revert()
        {
            if (!Context.Block.Events.HasFlag(BlockEvents.CycleEnd))
                return;

            await Db.Database.ExecuteSqlRawAsync("""
                DELETE FROM "SnapshotBalances"
                WHERE "Level" = {0}
                """, Context.Block.Level);
        }
    }
}
