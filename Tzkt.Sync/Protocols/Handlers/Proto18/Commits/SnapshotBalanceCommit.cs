using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto18
{
    class SnapshotBalanceCommit : ProtocolCommit
    {
        public SnapshotBalanceCommit(ProtocolHandler protocol) : base(protocol) { }

        public virtual async Task Apply(Block block, JsonElement rawBlock)
        {
            if (block.Events.HasFlag(BlockEvents.BalanceSnapshot))
            {
                #region remove outdated
                var delete = string.Empty;
                var outdatedLevel = block.Level - (block.Protocol.PreservedCycles + 3) * block.Protocol.BlocksPerCycle;
                if (outdatedLevel > 0)
                {
                    delete += block.Cycle <= block.Protocol.FirstCycle + block.Protocol.PreservedCycles + 4
                        ? $"""DELETE FROM "SnapshotBalances" WHERE "Level" <= {outdatedLevel};"""
                        : $"""DELETE FROM "SnapshotBalances" WHERE "Level" = {outdatedLevel};""";
                }
                #endregion

                #region make snapshot
                await Db.Database.ExecuteSqlRawAsync($"""
                    {delete}
                    INSERT INTO "SnapshotBalances" ("Level", "Balance", "StakedBalance", "AccountId", "DelegateId", "StakingBalance", "DelegatedBalance", "DelegatorsCount", "TotalStakedBalance", "ExternalStakedBalance", "StakersCount")
                    SELECT {block.Level}, "Balance", COALESCE("StakedBalance", 0), "Id", "DelegateId", "StakingBalance", "DelegatedBalance", "DelegatorsCount", "TotalStakedBalance", "ExternalStakedBalance", "StakersCount"
                    FROM "Accounts"
                    WHERE "Staked" = true
                    """);
                #endregion

                #region ignore just deactivated
                if (block.Events.HasFlag(BlockEvents.Deactivations))
                {
                    var deactivated = await Db.Delegates
                        .AsNoTracking()
                        .Include(x => x.DelegatedAccounts)
                        .Where(x => x.DeactivationLevel == block.Level)
                        .ToListAsync();

                    if (deactivated.Any())
                    {
                        var sql = """
                            INSERT INTO "SnapshotBalances" ("Level", "Balance", "StakedBalance", "AccountId", "DelegateId", "StakingBalance", "DelegatedBalance", "DelegatorsCount", "TotalStakedBalance", "ExternalStakedBalance", "StakersCount") VALUES 
                            """;

                        foreach (var baker in deactivated)
                        {
                            sql += $"""
                                ({block.Level}, {baker.Balance}, {baker.StakedBalance}, {baker.Id}, NULL, {baker.StakingBalance}, {baker.DelegatedBalance}, {baker.DelegatorsCount}, {baker.TotalStakedBalance}, {baker.ExternalStakedBalance}, {baker.StakersCount}),
                                """;

                            foreach (var delegator in baker.DelegatedAccounts)
                                sql += $"""
                                    ({block.Level}, {delegator.Balance}, {(delegator as User)?.StakedBalance ?? 0}, {delegator.Id}, {delegator.DelegateId}, NULL, NULL, NULL, NULL, NULL, NULL),
                                    """;
                        }

                        await Db.Database.ExecuteSqlRawAsync(sql[..^1]);
                    }
                }
                #endregion

                #region revert endorsing rewards
                if (block.Events.HasFlag(BlockEvents.CycleEnd))
                {
                    await Db.Database.ExecuteSqlRawAsync($"""
                        UPDATE "SnapshotBalances" as sb
                        SET "Balance" = "Balance" - bc."EndorsementRewardsLiquid" - bc."EndorsementRewardsStakedOwn" - bc."EndorsementRewardsStakedShared",
                            "StakingBalance" = "StakingBalance" - bc."EndorsementRewardsLiquid" - bc."EndorsementRewardsStakedOwn" - bc."EndorsementRewardsStakedShared",
                            "StakedBalance" = "StakedBalance" - bc."EndorsementRewardsStakedOwn",
                            "ExternalStakedBalance" = "ExternalStakedBalance" - bc."EndorsementRewardsStakedShared",
                            "TotalStakedBalance" = "TotalStakedBalance" - bc."EndorsementRewardsStakedOwn" - bc."EndorsementRewardsStakedShared"
                        FROM (
                            SELECT "BakerId", "EndorsementRewardsLiquid", "EndorsementRewardsStakedOwn", "EndorsementRewardsStakedShared"
                            FROM "BakerCycles"
                            WHERE "Cycle" = {block.Cycle}
                            AND ("EndorsementRewardsLiquid" != 0 OR "EndorsementRewardsStakedOwn" != 0 OR "EndorsementRewardsStakedShared" != 0)
                        ) as bc
                        WHERE sb."Level" = {block.Level}
                        AND sb."DelegateId" IS NULL
                        AND sb."AccountId" = bc."BakerId"
                        """);
                }
                #endregion
            }
        }

        public async Task Revert(Block block)
        {
            if (block.Events.HasFlag(BlockEvents.BalanceSnapshot))
            {
                await Db.Database.ExecuteSqlRawAsync($"""
                    DELETE FROM "SnapshotBalances"
                    WHERE "Level" = {block.Level}
                    """);
            }
        }
    }
}
