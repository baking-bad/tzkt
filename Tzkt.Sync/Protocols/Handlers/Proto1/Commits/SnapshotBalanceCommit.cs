using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto1
{
    class SnapshotBalanceCommit : ProtocolCommit
    {
        public SnapshotBalanceCommit(ProtocolHandler protocol) : base(protocol) { }

        public virtual async Task Apply(JsonElement rawBlock, Block block)
        {
            if (!block.Events.HasFlag(BlockEvents.BalanceSnapshot))
                return;

            await TakeSnapshot(block);
            await TakeWeirdsSnapshot(block);
        }

        public virtual async Task Revert(Block block)
        {
            if (!block.Events.HasFlag(BlockEvents.BalanceSnapshot))
                return;

            await Db.Database.ExecuteSqlRawAsync($"""
                DELETE FROM "SnapshotBalances"
                WHERE "Level" = {block.Level}
                """);
        }

        protected virtual Task TakeSnapshot(Block block)
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
                    COALESCE("StakingBalance", "Balance") - COALESCE("DelegatedBalance", 0),
                    COALESCE("DelegatedBalance", 0),
                    COALESCE("DelegatorsCount", 0),
                    0,
                    0,
                    0,
                    0,
                    0
                FROM "Accounts"
                WHERE "Staked" = true
                """);
        }

        protected Task RemoveOutdated(Block block, Protocol protocol)
        {
            var level = block.Level - (protocol.PreservedCycles + 3) * protocol.BlocksPerCycle;
            return Db.Database.ExecuteSqlRawAsync($"""
                DELETE FROM "SnapshotBalances"
                WHERE "Level" <= {level}
                """);
        }

        protected virtual async Task TakeDeactivatedSnapshot(Block block)
        {
            var deactivated = await Db.Delegates
                .AsNoTracking()
                .Include(x => x.DelegatedAccounts)
                .Where(x => x.DeactivationLevel == block.Level)
                .ToListAsync();

            if (deactivated.Any())
            {
                var values = string.Join(",\n", deactivated
                    .SelectMany(baker =>
                        new[] { $"({block.Level}, {baker.Id}, {baker.Id}, {baker.StakingBalance - baker.DelegatedBalance}, {baker.DelegatedBalance}, {baker.DelegatorsCount}, 0, 0, 0, 0, 0)" }
                        .Concat(baker.DelegatedAccounts.Select(delegator => $"({block.Level}, {delegator.Id}, {delegator.DelegateId}, {delegator.Balance}, 0, 0, 0, 0, 0, 0, 0)"))));

                if (values.Length > 0)
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
                        {values}
                        """);
                }
            }
        }

        protected virtual async Task SubtractCycleRewards(JsonElement rawBlock, Block block)
        {
            if (!block.Events.HasFlag(BlockEvents.CycleEnd))
                return;

            var rewards = string.Join(",\n", GetBalanceUpdates(rawBlock)
                .Where(x => x.RequiredString("kind")[0] == 'f' &&
                            x.RequiredString("category")[0] == 'r' &&
                            x.RequiredInt64("change") < 0 &&
                            GetFreezerCycle(x) != block.Cycle)
                .Select(x => (x.RequiredString("delegate"), x.RequiredInt64("change")))
                .GroupBy(x => x.Item1)
                .Select(updates => $"({Cache.Accounts.GetDelegate(updates.Key).Id}, {updates.Sum(x => -x.Item2)}::bigint)"));

            if (rewards.Length > 0)
            {
                await Db.Database.ExecuteSqlRawAsync($"""
                    UPDATE "SnapshotBalances" as sb
                    SET "OwnDelegatedBalance" = "OwnDelegatedBalance" - reward.value
                    FROM (
                        VALUES
                        {rewards}
                    ) as reward(baker, value)
                    WHERE sb."Level" = {block.Level}
                    AND sb."AccountId" = reward.baker
                    AND sb."BakerId" = reward.baker
                    """);
            }
        }

        protected virtual int GetFreezerCycle(JsonElement el)
        {
            return el.RequiredInt32("level");
        }

        protected virtual IEnumerable<JsonElement> GetBalanceUpdates(JsonElement rawBlock)
        {
            return rawBlock
                .GetProperty("metadata")
                .GetProperty("balance_updates")
                .EnumerateArray();
        }

        async Task TakeWeirdsSnapshot(Block block)
        {
            var weirdDelegators = (await Db.Contracts
                .AsNoTracking()
                .Include(x => x.WeirdDelegate)
                .Where(x =>
                    x.DelegateId == null &&
                    x.WeirdDelegateId != null &&
                    x.WeirdDelegate.Type != AccountType.Delegate)
                .ToListAsync())
                .GroupBy(x => x.WeirdDelegateId);

            if (weirdDelegators.Any())
            {
                var values = string.Join(",\n", weirdDelegators
                    .Where(weirds => weirds.Sum(x => x.Balance) >= block.Protocol.MinimalStake)
                    .SelectMany(weirds =>
                        new[] { $"({block.Level}, {weirds.First().WeirdDelegateId}, {weirds.First().WeirdDelegateId}, 0, {weirds.Sum(x => x.Balance)}, {weirds.Count()}, 0, 0, 0, 0, 0)" }
                        .Concat(weirds.Select(x => $"({block.Level}, {x.Id}, {x.WeirdDelegateId}, {x.Balance}, 0, 0, 0, 0, 0, 0, 0)"))));

                if (values.Length > 0)
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
                        {values}
                        """);
                }
            }
        }
    }
}
