using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models;
using Tzkt.Data.Models.Base;

namespace Tzkt.Sync.Protocols.Proto1
{
    class SnapshotBalanceCommit(ProtocolHandler protocol) : ProtocolCommit(protocol)
    {
        public virtual async Task Apply(JsonElement rawBlock, Block block)
        {
            if (!block.Events.HasFlag(BlockEvents.BalanceSnapshot))
                return;

            await TakeSnapshot(block);
            await TakeWeirdsSnapshot(block, Context.Protocol);
        }

        public virtual async Task Revert(Block block)
        {
            if (!block.Events.HasFlag(BlockEvents.BalanceSnapshot))
                return;

            await Db.Database.ExecuteSqlRawAsync("""
                DELETE FROM "SnapshotBalances"
                WHERE "Level" = {0}
                """, block.Level);
        }

        protected virtual Task TakeSnapshot(Block block)
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
                    COALESCE("DelegateId", "Id"),
                    COALESCE("StakingBalance", "Balance") - COALESCE("DelegatedBalance", 0),
                    COALESCE("DelegatedBalance", 0),
                    COALESCE("DelegatorsCount", 0),
                    0,
                    0,
                    0
                FROM "Accounts"
                WHERE "Staked" = true
                """, block.Level);
        }

        protected Task RemoveOutdated(Block block, Protocol protocol)
        {
            if (!block.Events.HasFlag(BlockEvents.CycleEnd))
                return Task.CompletedTask;

            var level = block.Level - (protocol.ConsensusRightsDelay + 3) * protocol.BlocksPerCycle;
            return Db.Database.ExecuteSqlRawAsync("""
                DELETE FROM "SnapshotBalances"
                WHERE "Level" <= {0}
                """, level);
        }

        protected virtual async Task TakeDeactivatedSnapshot(Block block)
        {
            var deactivated = await Db.Delegates
                .AsNoTracking()
                .GroupJoin(Db.Accounts, x => x.Id, x => x.DelegateId, (baker, delegators) => new { baker, delegators })
                .Where(x => x.baker.DeactivationLevel == block.Level)
                .ToListAsync();

            if (deactivated.Count != 0)
            {
                var values = string.Join(",\n", deactivated
                    .SelectMany(row =>
                        new[] { $"({block.Level}, {row.baker.Id}, {row.baker.Id}, {row.baker.StakingBalance - row.baker.DelegatedBalance}, {row.baker.DelegatedBalance}, {row.baker.DelegatorsCount}, 0, 0, 0)" }
                        .Concat(row.delegators.Select(delegator => $"({block.Level}, {delegator.Id}, {delegator.DelegateId}, {delegator.Balance}, 0, 0, 0, 0, 0)"))));

                if (values.Length > 0)
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
                        {values}
                        """);
#pragma warning restore EF1002 // Risk of vulnerability to SQL injection.
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
                .Select(updates => $"({Cache.Accounts.GetExistingDelegate(updates.Key).Id}, {updates.Sum(x => -x.Item2)}::bigint)"));

            if (rewards.Length > 0)
            {
#pragma warning disable EF1002 // Risk of vulnerability to SQL injection.
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
#pragma warning restore EF1002 // Risk of vulnerability to SQL injection.
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

        async Task TakeWeirdsSnapshot(Block block, Protocol protocol)
        {
            var weirdOriginations = (await Db.OriginationOps
                .AsNoTracking()
                .Join(Db.Accounts, x => x.DelegateId, x => x.Id, (op, delegat) => new { op, delegat })
                .Join(Db.Accounts, x => x.op.ContractId, x => x.Id, (opDelegat, contract) => new { opDelegat.op, opDelegat.delegat, contract })
                .Where(x =>
                    x.op.Status == OperationStatus.Applied &&
                    x.op.DelegateId != null &&
                    x.delegat.Type != AccountType.Delegate &&
                    x.contract.DelegateId == null)
                .ToListAsync())
                .GroupBy(x => x.delegat.Id);

            if (weirdOriginations.Any())
            {
                var values = string.Join(",\n", weirdOriginations
                    .Where(weirds => weirds.Sum(x => x.contract.Balance) >= protocol.MinimalStake)
                    .SelectMany(weirds =>
                        new[] { $"({block.Level}, {weirds.Key}, {weirds.Key}, 0, {weirds.Sum(x => x.contract.Balance)}, {weirds.Count()}, 0, 0, 0)" }
                        .Concat(weirds.Select(x => $"({block.Level}, {x.contract.Id}, {weirds.Key}, {x.contract.Balance}, 0, 0, 0, 0, 0)"))));

                if (values.Length > 0)
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
                        {values}
                        """);
#pragma warning restore EF1002 // Risk of vulnerability to SQL injection.
                }
            }
        }
    }
}
