using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto19
{
    class StakerCycleCommit(ProtocolHandler protocol) : ProtocolCommit(protocol)
    {
        public async Task Apply()
        {
            if (!Context.Block.Events.HasFlag(BlockEvents.CycleBegin))
                return;

            #region finalize
            await Db.Database.ExecuteSqlRawAsync("""
                UPDATE "StakerCycles"
                SET "FinalStake" = snapshot.stake
                FROM (
                    SELECT
                        sc."Id" AS id,
                        FLOOR(baker."ExternalStakedBalance"
                            * COALESCE(staker."StakedPseudotokens", 0::numeric)
                            / COALESCE(baker."IssuedPseudotokens", 1::numeric))::bigint AS stake
                    FROM "StakerCycles" AS sc
                    INNER JOIN "Accounts" AS staker ON staker."Id" = sc."StakerId"
                    INNER JOIN "Accounts" AS baker ON baker."Id" = sc."BakerId"
                    WHERE sc."Cycle" = {0}
                ) AS snapshot
                WHERE "Id" = snapshot.id
                """, Context.Block.Cycle - 1);
            #endregion

            #region create
            await Db.Database.ExecuteSqlRawAsync("""
                INSERT INTO "StakerCycles" (
                    "Cycle",
                    "StakerId",
                    "BakerId",
                    "EdgeOfBakingOverStaking",
                    "InitialStake",
                    "AvgStake",
                    "AddedStake",
                    "RemovedStake",
                    "FinalStake"
                )
                SELECT
                    {0},
                    sc."StakerId",
                    sc."BakerId",
                    COALESCE(baker."EdgeOfBakingOverStaking", 1000000000::bigint),
                    sc."FinalStake",
                    sc."FinalStake",
                    0,
                    0,
                    NULL
                FROM "StakerCycles" AS sc
                INNER JOIN "Accounts" AS staker ON staker."Id" = sc."StakerId"
                INNER JOIN "Accounts" AS baker ON baker."Id" = sc."BakerId"
                WHERE sc."Cycle" = {1} AND staker."StakedPseudotokens" IS NOT NULL
                ORDER BY sc."Id"
                """, Context.Block.Cycle, Context.Block.Cycle - 1);
            #endregion
        }

        public async Task Revert()
        {
            if (!Context.Block.Events.HasFlag(BlockEvents.CycleBegin))
                return;

            #region revert create
            await Db.Database.ExecuteSqlRawAsync("""
                DELETE FROM "StakerCycles"
                WHERE "Cycle" = {0}
                """, Context.Block.Cycle);
            #endregion

            #region revert finalize
            await Db.Database.ExecuteSqlRawAsync("""
                UPDATE "StakerCycles"
                SET "FinalStake" = NULL
                WHERE "Cycle" = {0}
                """, Context.Block.Cycle - 1);
            #endregion
        }
    }
}
