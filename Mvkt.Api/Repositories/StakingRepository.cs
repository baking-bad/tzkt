using Dapper;
using Npgsql;
using Mvkt.Api.Models;
using Mvkt.Api.Services.Cache;

namespace Mvkt.Api.Repositories
{
    public class StakingRepository
    {
        readonly NpgsqlDataSource DataSource;
        readonly AccountsCache Accounts;
        readonly ProtocolsCache Protocols;
        readonly StateCache State;
        readonly TimeCache Times;

        public StakingRepository(NpgsqlDataSource dataSource, AccountsCache accounts, ProtocolsCache protocols, StateCache state, TimeCache times)
        {
            DataSource = dataSource;
            Accounts = accounts;
            Protocols = protocols;
            State = state;
            Times = times;
        }

        #region staking updates
        async Task<IEnumerable<dynamic>> QueryStakingUpdatesAsync(StakingUpdateFilter filter, Pagination pagination, List<SelectionField> fields = null)
        {
            var select = "*";
            if (fields != null)
            {
                var columns = new HashSet<string>(fields.Count);
                foreach (var field in fields)
                {
                    switch (field.Field)
                    {
                        case "id": columns.Add(@"""Id"""); break;
                        case "level": columns.Add(@"""Level"""); break;
                        case "timestamp": columns.Add(@"""Level"""); break;
                        case "cycle": columns.Add(@"""Cycle"""); break;
                        case "baker": columns.Add(@"""BakerId"""); break;
                        case "staker": columns.Add(@"""StakerId"""); break;
                        case "type": columns.Add(@"""Type"""); break;
                        case "amount": columns.Add(@"""Amount"""); break;
                        case "pseudotokens": columns.Add(@"""Pseudotokens"""); break;
                        case "roundingError": columns.Add(@"""RoundingError"""); break;
                        case "autostakingOpId": columns.Add(@"""AutostakingOpId"""); break;
                        case "stakingOpId": columns.Add(@"""StakingOpId"""); break;
                        case "delegationOpId": columns.Add(@"""DelegationOpId"""); break;
                        case "doubleBakingOpId": columns.Add(@"""DoubleBakingOpId"""); break;
                        case "doubleEndorsingOpId": columns.Add(@"""DoubleEndorsingOpId"""); break;
                        case "doublePreendorsingOpId": columns.Add(@"""DoublePreendorsingOpId"""); break;
                    }
                }

                if (columns.Count == 0)
                    return Enumerable.Empty<dynamic>();

                select = string.Join(',', columns);
            }

            var sql = new SqlBuilder($@"
                SELECT {select} FROM ""StakingUpdates""")
                .FilterA(@"""Id""", filter.id)
                .FilterA(@"""Level""", filter.level)
                .FilterA(@"""Level""", filter.timestamp)
                .FilterA(@"""Cycle""", filter.cycle)
                .FilterA(@"""BakerId""", filter.baker)
                .FilterA(@"""StakerId""", filter.staker)
                .FilterA(@"""Type""", filter.type)
                .FilterA(@"""Amount""", filter.amount)
                .FilterA(@"""Pseudotokens""", filter.pseudotokens)
                .FilterA(@"""RoundingError""", filter.roundingError)
                .FilterA(@"""AutostakingOpId""", filter.autostakingOpId)
                .FilterA(@"""StakingOpId""", filter.stakingOpId)
                .FilterA(@"""DelegationOpId""", filter.delegationOpId)
                .FilterA(@"""DoubleBakingOpId""", filter.doubleBakingOpId)
                .FilterA(@"""DoubleEndorsingOpId""", filter.doubleEndorsingOpId)
                .FilterA(@"""DoublePreendorsingOpId""", filter.doublePreendorsingOpId)
                .Take(pagination, x => x switch
                {
                    "id" => (@"""Id""", @"""Id"""),
                    "level" => (@"""Level""", @"""Level"""),
                    _ => (@"""Id""", @"""Id""")
                }, @"""Id""");

            await using var db = await DataSource.OpenConnectionAsync();
            return await db.QueryAsync(sql.Query, sql.Params);
        }

        public async Task<int> GetStakingUpdatesCount(StakingUpdateFilter filter)
        {
            var sql = new SqlBuilder(@"
                SELECT COUNT(*) FROM ""StakingUpdates""")
                .FilterA(@"""Id""", filter.id)
                .FilterA(@"""Level""", filter.level)
                .FilterA(@"""Level""", filter.timestamp)
                .FilterA(@"""Cycle""", filter.cycle)
                .FilterA(@"""BakerId""", filter.baker)
                .FilterA(@"""StakerId""", filter.staker)
                .FilterA(@"""Type""", filter.type)
                .FilterA(@"""Amount""", filter.amount)
                .FilterA(@"""Pseudotokens""", filter.pseudotokens)
                .FilterA(@"""RoundingError""", filter.roundingError)
                .FilterA(@"""AutostakingOpId""", filter.autostakingOpId)
                .FilterA(@"""StakingOpId""", filter.stakingOpId)
                .FilterA(@"""DelegationOpId""", filter.delegationOpId)
                .FilterA(@"""DoubleBakingOpId""", filter.doubleBakingOpId)
                .FilterA(@"""DoubleEndorsingOpId""", filter.doubleEndorsingOpId)
                .FilterA(@"""DoublePreendorsingOpId""", filter.doublePreendorsingOpId);

            await using var db = await DataSource.OpenConnectionAsync();
            return await db.QueryFirstAsync<int>(sql.Query, sql.Params);
        }

        public async Task<IEnumerable<StakingUpdate>> GetStakingUpdates(StakingUpdateFilter filter, Pagination pagination)
        {
            var rows = await QueryStakingUpdatesAsync(filter, pagination);
            return rows.Select(row => new StakingUpdate
            {
                Id = row.Id,
                Level = row.Level,
                Timestamp = Times[row.Level],
                Cycle = row.Cycle,
                Baker = Accounts.GetAlias((int)row.BakerId),
                Staker = Accounts.GetAlias((int)row.StakerId),
                Type = StakingUpdateTypes.ToString((int)row.Type),
                Amount = row.Amount,
                Pseudotokens = row.Pseudotokens,
                RoundingError = row.RoundingError,
                AutostakingOpId = row.AutostakingOpId,
                StakingOpId = row.StakingOpId,
                DelegationOpId = row.DelegationOpId,
                DoubleBakingOpId = row.DoubleBakingOpId,
                DoubleEndorsingOpId = row.DoubleEndorsingOpId,
                DoublePreendorsingOpId = row.DoublePreendorsingOpId
            });
        }

        public async Task<object[][]> GetStakingUpdates(StakingUpdateFilter filter, Pagination pagination, List<SelectionField> fields)
        {
            var rows = await QueryStakingUpdatesAsync(filter, pagination, fields);

            var result = new object[rows.Count()][];
            for (int i = 0; i < result.Length; i++)
                result[i] = new object[fields.Count];

            for (int i = 0, j = 0; i < fields.Count; j = 0, i++)
            {
                switch (fields[i].Full)
                {
                    case "id":
                        foreach (var row in rows)
                            result[j++][i] = row.Id;
                        break;
                    case "level":
                        foreach (var row in rows)
                            result[j++][i] = row.Level;
                        break;
                    case "timestamp":
                        foreach (var row in rows)
                            result[j++][i] = Times[row.Level];
                        break;
                    case "cycle":
                        foreach (var row in rows)
                            result[j++][i] = row.Cycle;
                        break;
                    case "baker":
                        foreach (var row in rows)
                            result[j++][i] = Accounts.GetAlias((int)row.BakerId);
                        break;
                    case "baker.alias":
                        foreach (var row in rows)
                            result[j++][i] = Accounts.GetAlias((int)row.BakerId).Name;
                        break;
                    case "baker.address":
                        foreach (var row in rows)
                            result[j++][i] = Accounts.GetAlias((int)row.BakerId).Address;
                        break;
                    case "staker":
                        foreach (var row in rows)
                            result[j++][i] = Accounts.GetAlias((int)row.StakerId);
                        break;
                    case "staker.alias":
                        foreach (var row in rows)
                            result[j++][i] = Accounts.GetAlias((int)row.StakerId).Name;
                        break;
                    case "staker.address":
                        foreach (var row in rows)
                            result[j++][i] = Accounts.GetAlias((int)row.StakerId).Address;
                        break;
                    case "type":
                        foreach (var row in rows)
                            result[j++][i] = StakingUpdateTypes.ToString((int)row.Type);
                        break;
                    case "amount":
                        foreach (var row in rows)
                            result[j++][i] = row.Amount;
                        break;
                    case "pseudotokens":
                        foreach (var row in rows)
                            result[j++][i] = row.Pseudotokens;
                        break;
                    case "roundingError":
                        foreach (var row in rows)
                            result[j++][i] = row.RoundingError;
                        break;
                    case "autostakingOpId":
                        foreach (var row in rows)
                            result[j++][i] = row.AutostakingOpId;
                        break;
                    case "stakingOpId":
                        foreach (var row in rows)
                            result[j++][i] = row.StakingOpId;
                        break;
                    case "delegationOpId":
                        foreach (var row in rows)
                            result[j++][i] = row.DelegationOpId;
                        break;
                    case "doubleBakingOpId":
                        foreach (var row in rows)
                            result[j++][i] = row.DoubleBakingOpId;
                        break;
                    case "doubleEndorsingOpId":
                        foreach (var row in rows)
                            result[j++][i] = row.DoubleEndorsingOpId;
                        break;
                    case "doublePreendorsingOpId":
                        foreach (var row in rows)
                            result[j++][i] = row.DoublePreendorsingOpId;
                        break;
                }
            }

            return result;
        }
        #endregion

        #region unstake requests
        async Task<IEnumerable<dynamic>> QueryUnstakeRequestsAsync(int unfrozenCycle, UnstakeRequestFilter filter, Pagination pagination, List<SelectionField> fields = null)
        {
            var select = "*";
            if (fields != null)
            {
                var columns = new HashSet<string>(fields.Count);
                foreach (var field in fields)
                {
                    switch (field.Field)
                    {
                        case "id": columns.Add(@"""Id"""); break;
                        case "cycle": columns.Add(@"""Cycle"""); break;
                        case "baker": columns.Add(@"""BakerId"""); break;
                        case "staker": columns.Add(@"""StakerId"""); break;
                        case "requestedAmount": columns.Add(@"""RequestedAmount"""); break;
                        case "restakedAmount": columns.Add(@"""RestakedAmount"""); break;
                        case "finalizedAmount": columns.Add(@"""FinalizedAmount"""); break;
                        case "slashedAmount": columns.Add(@"""SlashedAmount"""); break;
                        case "roundingError": columns.Add(@"""RoundingError"""); break;
                        case "updatesCount": columns.Add(@"""UpdatesCount"""); break;
                        case "firstLevel": columns.Add(@"""FirstLevel"""); break;
                        case "firstTime": columns.Add(@"""FirstLevel"""); break;
                        case "lastLevel": columns.Add(@"""LastLevel"""); break;
                        case "lastTime": columns.Add(@"""LastLevel"""); break;

                        case "actualAmount":
                            columns.Add(@"""ActualAmount""");
                            break;
                        case "status":
                            columns.Add(@"""Cycle""");
                            columns.Add(@"""RemainingAmount""");
                            break;
                    }
                }

                if (columns.Count == 0)
                    return Enumerable.Empty<dynamic>();

                select = string.Join(',', columns);
            }


            var sql = new SqlBuilder($"""
                WITH "UnstakeRequestsExt" AS NOT MATERIALIZED (
                    SELECT  *,
                            "RequestedAmount" - "RestakedAmount" - "SlashedAmount" - COALESCE("RoundingError", 0) AS "ActualAmount",
                            "RequestedAmount" - "RestakedAmount" - "SlashedAmount" - COALESCE("RoundingError", 0) - "FinalizedAmount" AS "RemainingAmount"
                    FROM "UnstakeRequests"
                )
                SELECT {select} FROM "UnstakeRequestsExt"
                """)
                .FilterA(@"""Id""", filter.id)
                .FilterA(@"""Cycle""", filter.cycle)
                .FilterA(@"""BakerId""", filter.baker)
                .FilterA(@"""StakerId""", filter.staker)
                .FilterA(@"""RequestedAmount""", filter.requestedAmount)
                .FilterA(@"""RestakedAmount""", filter.restakedAmount)
                .FilterA(@"""FinalizedAmount""", filter.finalizedAmount)
                .FilterA(@"""SlashedAmount""", filter.slashedAmount)
                .FilterA(@"""RoundingError""", filter.roundingError)
                .FilterA(@"""ActualAmount""", filter.actualAmount)
                .FilterA(@"""Cycle""", @"""RemainingAmount""", filter.status, unfrozenCycle)
                .FilterA(@"""UpdatesCount""", filter.updatesCount)
                .FilterA(@"""FirstLevel""", filter.firstLevel)
                .FilterA(@"""FirstLevel""", filter.firstTime)
                .FilterA(@"""LastLevel""", filter.lastLevel)
                .FilterA(@"""LastLevel""", filter.lastTime)
                .Take(pagination, x => x switch
                {
                    "id" => (@"""Id""", @"""Id"""),
                    "cycle" => (@"""Cycle""", @"""Cycle"""),
                    "firstLevel" => (@"""FirstLevel""", @"""FirstLevel"""),
                    "lastLevel" => (@"""LastLevel""", @"""LastLevel"""),
                    _ => (@"""Id""", @"""Id""")
                }, @"""Id""");

            await using var db = await DataSource.OpenConnectionAsync();
            return await db.QueryAsync(sql.Query, sql.Params);
        }

        public async Task<int> GetUnstakeRequestsCount(UnstakeRequestFilter filter)
        {
            var unfrozenCycle = State.Current.Cycle - Protocols.Current.ConsensusRightsDelay - 2;

            var sql = new SqlBuilder("""
                WITH "UnstakeRequestsExt" AS NOT MATERIALIZED (
                    SELECT  *,
                            "RequestedAmount" - "RestakedAmount" - "SlashedAmount" - COALESCE("RoundingError", 0) AS "ActualAmount",
                            "RequestedAmount" - "RestakedAmount" - "SlashedAmount" - COALESCE("RoundingError", 0) - "FinalizedAmount" AS "RemainingAmount"
                    FROM "UnstakeRequests"
                )
                SELECT COUNT(*) FROM "UnstakeRequestsExt"
                """)
                .FilterA(@"""Id""", filter.id)
                .FilterA(@"""Cycle""", filter.cycle)
                .FilterA(@"""BakerId""", filter.baker)
                .FilterA(@"""StakerId""", filter.staker)
                .FilterA(@"""RequestedAmount""", filter.requestedAmount)
                .FilterA(@"""RestakedAmount""", filter.restakedAmount)
                .FilterA(@"""FinalizedAmount""", filter.finalizedAmount)
                .FilterA(@"""SlashedAmount""", filter.slashedAmount)
                .FilterA(@"""RoundingError""", filter.roundingError)
                .FilterA(@"""ActualAmount""", filter.actualAmount)
                .FilterA(@"""Cycle""", @"""RemainingAmount""", filter.status, unfrozenCycle)
                .FilterA(@"""UpdatesCount""", filter.updatesCount)
                .FilterA(@"""FirstLevel""", filter.firstLevel)
                .FilterA(@"""FirstLevel""", filter.firstTime)
                .FilterA(@"""LastLevel""", filter.lastLevel)
                .FilterA(@"""LastLevel""", filter.lastTime);

            await using var db = await DataSource.OpenConnectionAsync();
            return await db.QueryFirstAsync<int>(sql.Query, sql.Params);
        }

        public async Task<IEnumerable<UnstakeRequest>> GetUnstakeRequests(UnstakeRequestFilter filter, Pagination pagination)
        {
            var unfrozenCycle = State.Current.Cycle - Protocols.Current.ConsensusRightsDelay - 2;
            var rows = await QueryUnstakeRequestsAsync(unfrozenCycle, filter, pagination);
            return rows.Select(row => new UnstakeRequest
            {
                Id = row.Id,
                Cycle = row.Cycle,
                Baker = Accounts.GetAlias((int)row.BakerId),
                Staker = row.StakerId == null ? null : Accounts.GetAlias((int)row.StakerId),
                RequestedAmount = row.RequestedAmount,
                RestakedAmount = row.RestakedAmount,
                FinalizedAmount = row.FinalizedAmount,
                SlashedAmount = row.SlashedAmount,
                RoundingError = row.RoundingError,
                ActualAmount = row.ActualAmount,
                Status = UnstakeRequestStatuses.ToString(row.Cycle, row.RemainingAmount, unfrozenCycle),
                UpdatesCount = row.UpdatesCount,
                FirstLevel = row.FirstLevel,
                FirstTime = Times[row.FirstLevel],
                LastLevel = row.LastLevel,
                LastTime = Times[row.LastLevel]
            });
        }

        public async Task<object[][]> GetUnstakeRequests(UnstakeRequestFilter filter, Pagination pagination, List<SelectionField> fields)
        {
            var unfrozenCycle = State.Current.Cycle - Protocols.Current.ConsensusRightsDelay - 2;
            var rows = await QueryUnstakeRequestsAsync(unfrozenCycle, filter, pagination, fields);

            var result = new object[rows.Count()][];
            for (int i = 0; i < result.Length; i++)
                result[i] = new object[fields.Count];

            for (int i = 0, j = 0; i < fields.Count; j = 0, i++)
            {
                switch (fields[i].Full)
                {
                    case "id":
                        foreach (var row in rows)
                            result[j++][i] = row.Id;
                        break;
                    case "cycle":
                        foreach (var row in rows)
                            result[j++][i] = row.Cycle;
                        break;
                    case "baker":
                        foreach (var row in rows)
                            result[j++][i] = Accounts.GetAlias((int)row.BakerId);
                        break;
                    case "baker.alias":
                        foreach (var row in rows)
                            result[j++][i] = Accounts.GetAlias((int)row.BakerId).Name;
                        break;
                    case "baker.address":
                        foreach (var row in rows)
                            result[j++][i] = Accounts.GetAlias((int)row.BakerId).Address;
                        break;
                    case "staker":
                        foreach (var row in rows)
                            result[j++][i] = row.StakerId == null ? null : Accounts.GetAlias((int)row.StakerId);
                        break;
                    case "staker.alias":
                        foreach (var row in rows)
                            result[j++][i] = row.StakerId == null ? null : Accounts.GetAlias((int)row.StakerId).Name;
                        break;
                    case "staker.address":
                        foreach (var row in rows)
                            result[j++][i] = row.StakerId == null ? null : Accounts.GetAlias((int)row.StakerId).Address;
                        break;
                    case "requestedAmount":
                        foreach (var row in rows)
                            result[j++][i] = row.RequestedAmount;
                        break;
                    case "restakedAmount":
                        foreach (var row in rows)
                            result[j++][i] = row.RestakedAmount;
                        break;
                    case "finalizedAmount":
                        foreach (var row in rows)
                            result[j++][i] = row.FinalizedAmount;
                        break;
                    case "slashedAmount":
                        foreach (var row in rows)
                            result[j++][i] = row.SlashedAmount;
                        break;
                    case "roundingError":
                        foreach (var row in rows)
                            result[j++][i] = row.RoundingError;
                        break;
                    case "actualAmount":
                        foreach (var row in rows)
                            result[j++][i] = row.ActualAmount;
                        break;
                    case "status":
                        foreach (var row in rows)
                            result[j++][i] = UnstakeRequestStatuses.ToString(row.Cycle, row.RemainingAmount, unfrozenCycle);
                        break;
                    case "updatesCount":
                        foreach (var row in rows)
                            result[j++][i] = row.UpdatesCount;
                        break;
                    case "firstLevel":
                        foreach (var row in rows)
                            result[j++][i] = row.FirstLevel;
                        break;
                    case "firstTime":
                        foreach (var row in rows)
                            result[j++][i] = Times[row.FirstLevel];
                        break;
                    case "lastLevel":
                        foreach (var row in rows)
                            result[j++][i] = row.LastLevel;
                        break;
                    case "lastTime":
                        foreach (var row in rows)
                            result[j++][i] = Times[row.LastLevel];
                        break;
                }
            }

            return result;
        }
        #endregion

        #region baker apy
        public async Task<BakerApy> GetBakerApy(string address)
        {
            var baker = await Accounts.GetAsync(address);
            if (baker is not Services.Cache.RawDelegate delegat)
                return null;

            if (!delegat.Staked || delegat.OwnStakedBalance == 0)
                return null;

            var protocol = Protocols.Current;
            var currentCycle = State.Current.Cycle;
            
            var secondsPerCycle = protocol.TimeBetweenBlocks * protocol.BlocksPerCycle;
            var cyclesPerYear = 365.0 * 24 * 60 * 60 / secondsPerCycle;
            
            // Get last N completed cycles of rewards data for this baker (use 12 cycles for better averaging)
            const int cyclesToAnalyze = 12;
            
            await using var db = await DataSource.OpenConnectionAsync();
            
            var cycles = (await db.QueryAsync<Data.Models.BakerCycle>("""
                SELECT 
                    "OwnStakedBalance",
                    "ExternalStakedBalance",
                    "OwnDelegatedBalance",
                    "ExternalDelegatedBalance",
                    "BlockRewardsStakedOwn",
                    "BlockRewardsStakedEdge",
                    "BlockRewardsStakedShared",
                    "BlockRewardsDelegated",
                    "EndorsementRewardsStakedOwn",
                    "EndorsementRewardsStakedEdge",
                    "EndorsementRewardsStakedShared",
                    "EndorsementRewardsDelegated",
                    "VdfRevelationRewardsStakedOwn",
                    "VdfRevelationRewardsStakedEdge",
                    "VdfRevelationRewardsStakedShared",
                    "VdfRevelationRewardsDelegated",
                    "NonceRevelationRewardsStakedOwn",
                    "NonceRevelationRewardsStakedEdge",
                    "NonceRevelationRewardsStakedShared",
                    "NonceRevelationRewardsDelegated"
                FROM "BakerCycles"
                WHERE "BakerId" = @bakerId
                AND "Cycle" < @currentCycle
                ORDER BY "Cycle" DESC
                LIMIT @limit
                """, new { bakerId = delegat.Id, currentCycle, limit = cyclesToAnalyze })).ToList();
            
            if (cycles.Count == 0)
                return null;

            var totalOwnStakeRewards = cycles.Sum(r =>
                r.BlockRewardsStakedOwn + r.BlockRewardsStakedEdge +
                r.EndorsementRewardsStakedOwn + r.EndorsementRewardsStakedEdge +
                r.VdfRevelationRewardsStakedOwn + r.VdfRevelationRewardsStakedEdge +
                r.NonceRevelationRewardsStakedOwn + r.NonceRevelationRewardsStakedEdge);
            
            var totalExternalStakeRewards = cycles.Sum(r =>
                r.BlockRewardsStakedShared +
                r.EndorsementRewardsStakedShared +
                r.VdfRevelationRewardsStakedShared +
                r.NonceRevelationRewardsStakedShared);
            
            var totalDelegationRewards = cycles.Sum(r =>
                r.BlockRewardsDelegated +
                r.EndorsementRewardsDelegated +
                r.VdfRevelationRewardsDelegated +
                r.NonceRevelationRewardsDelegated);
            
            var avgOwnStakedBalance = cycles.Average(r => (double)r.OwnStakedBalance);
            var avgExternalStakedBalance = cycles.Average(r => (double)r.ExternalStakedBalance);
            var avgDelegatedBalance = cycles.Average(r => (double)(r.OwnDelegatedBalance + r.ExternalDelegatedBalance));
            
            var ownStakeCycleYield = avgOwnStakedBalance > 0
                ? totalOwnStakeRewards / cycles.Count / avgOwnStakedBalance
                : 0.0;
            var ownStakeApy = ownStakeCycleYield > 0
                ? (Math.Pow(1 + ownStakeCycleYield, cyclesPerYear) - 1) * 100
                : 0.0;
            
            var externalStakeCycleYield = avgExternalStakedBalance > 0
                ? totalExternalStakeRewards / cycles.Count / avgExternalStakedBalance
                : 0.0;
            var externalStakeApy = externalStakeCycleYield > 0
                ? (Math.Pow(1 + externalStakeCycleYield, cyclesPerYear) - 1) * 100
                : 0.0;
            
            var delegationCycleYield = avgDelegatedBalance > 0
                ? totalDelegationRewards / cycles.Count / avgDelegatedBalance
                : 0.0;
            var delegationApy = delegationCycleYield > 0
                ? (Math.Pow(1 + delegationCycleYield, cyclesPerYear) - 1) * 100
                : 0.0;

            return new BakerApy
            {
                OwnStakeApy = Math.Round(ownStakeApy, 2),
                ExternalStakeApy = Math.Round(externalStakeApy, 2),
                DelegationApy = Math.Round(delegationApy, 2)
            };
        }
        #endregion
    }
}

