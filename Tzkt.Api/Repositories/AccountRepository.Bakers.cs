using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Tzkt.Api.Services.Cache;

namespace Tzkt.Api.Repositories
{
    public partial class AccountRepository : DbConnection
    {
        public async Task<Models.Delegate> GetDelegate(string address)
        {
            var rawAccount = await Accounts.GetAsync(address);
            if (rawAccount is not RawDelegate delegat)
                return null;

            return new Models.Delegate
            {
                Alias = delegat.Alias,
                Active = delegat.Staked,
                Address = delegat.Address,
                PublicKey = delegat.PublicKey,
                Revealed = delegat.Revealed,
                Balance = delegat.Balance,
                FrozenDeposits = delegat.FrozenDeposits,
                FrozenRewards = delegat.FrozenRewards,
                FrozenFees = delegat.FrozenFees,
                Counter = delegat.Counter,
                ActivationLevel = delegat.ActivationLevel,
                ActivationTime = Time[delegat.ActivationLevel],
                DeactivationLevel = delegat.Staked ? null : delegat.DeactivationLevel,
                DeactivationTime = delegat.Staked ? null : Time[delegat.DeactivationLevel],
                StakingBalance = delegat.StakingBalance,
                FirstActivity = delegat.FirstLevel,
                FirstActivityTime = Time[delegat.FirstLevel],
                LastActivity = delegat.LastLevel,
                LastActivityTime = Time[delegat.LastLevel],
                NumActivations = delegat.Activated == true ? 1 : 0,
                NumBallots = delegat.BallotsCount,
                NumContracts = delegat.ContractsCount,
                NumDelegators = delegat.DelegatorsCount,
                NumBlocks = delegat.BlocksCount,
                NumDelegations = delegat.DelegationsCount,
                NumDoubleBaking = delegat.DoubleBakingCount,
                NumDoubleEndorsing = delegat.DoubleEndorsingCount,
                NumEndorsements = delegat.EndorsementsCount,
                NumNonceRevelations = delegat.NonceRevelationsCount,
                NumRevelationPenalties = delegat.RevelationPenaltiesCount,
                NumOriginations = delegat.OriginationsCount,
                NumProposals = delegat.ProposalsCount,
                NumReveals = delegat.RevealsCount,
                NumMigrations = delegat.MigrationsCount,
                NumTransactions = delegat.TransactionsCount,
                Software = delegat.SoftwareId == null ? null : Software[(int)delegat.SoftwareId]
            };
        }

        public async Task<int> GetDelegatesCount(BoolParameter active)
        {
            var sql = new SqlBuilder(@"SELECT COUNT(*) FROM ""Accounts""")
                .Filter("Type", 1)
                .Filter("Staked", active);

            using var db = GetConnection();
            return await db.QueryFirstAsync<int>(sql.Query, sql.Params);
        }

        public async Task<IEnumerable<Models.Delegate>> GetDelegates(
            BoolParameter active,
            Int32Parameter lastActivity,
            SortParameter sort,
            OffsetParameter offset,
            int limit)
        {
            var sql = new SqlBuilder($@"SELECT *, {AliasQuery} FROM ""Accounts""")
                .Filter("Type", 1)
                .Filter("Staked", active)
                .Filter("LastLevel", lastActivity)
                .Take(sort, offset, limit, x => x switch
                {
                    "activationLevel" => ("ActivationLevel", "ActivationLevel"),
                    "deactivationLevel" => ("DeactivationLevel", "DeactivationLevel"),
                    "stakingBalance" => ("StakingBalance", "StakingBalance"),
                    "balance" => ("Balance", "Balance"),
                    "numDelegators" => ("DelegatorsCount", "DelegatorsCount"),
                    _ => ("Id", "Id")
                });

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            return rows.Select(row =>
            {
                return new Models.Delegate
                {
                    Alias = row.Alias,
                    Active = row.Staked,
                    Address = row.Address,
                    PublicKey = row.PublicKey,
                    Revealed = row.Revealed,
                    Balance = row.Balance,
                    FrozenDeposits = row.FrozenDeposits,
                    FrozenRewards = row.FrozenRewards,
                    FrozenFees = row.FrozenFees,
                    Counter = row.Counter,
                    ActivationLevel = row.ActivationLevel,
                    ActivationTime = Time[row.ActivationLevel],
                    DeactivationLevel = row.Staked ? null : (int?)row.DeactivationLevel,
                    DeactivationTime = row.Staked ? null : (DateTime?)Time[row.DeactivationLevel],
                    StakingBalance = row.StakingBalance,
                    FirstActivity = row.FirstLevel,
                    FirstActivityTime = Time[row.FirstLevel],
                    LastActivity = row.LastLevel,
                    LastActivityTime = Time[row.LastLevel],
                    NumActivations = row.Activated == true ? 1 : 0,
                    NumBallots = row.BallotsCount,
                    NumContracts = row.ContractsCount,
                    NumDelegators = row.DelegatorsCount,
                    NumBlocks = row.BlocksCount,
                    NumDelegations = row.DelegationsCount,
                    NumDoubleBaking = row.DoubleBakingCount,
                    NumDoubleEndorsing = row.DoubleEndorsingCount,
                    NumEndorsements = row.EndorsementsCount,
                    NumNonceRevelations = row.NonceRevelationsCount,
                    NumRevelationPenalties = row.RevelationPenaltiesCount,
                    NumOriginations = row.OriginationsCount,
                    NumProposals = row.ProposalsCount,
                    NumReveals = row.RevealsCount,
                    NumMigrations = row.MigrationsCount,
                    NumTransactions = row.TransactionsCount,
                    Software = row.SoftwareId == null ? null : Software[row.SoftwareId]
                };
            });
        }

        public async Task<object[][]> GetDelegates(
            BoolParameter active,
            Int32Parameter lastActivity,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            string[] fields)
        {
            var columns = new HashSet<string>(fields.Length + 2);
            foreach (var field in fields)
            {
                switch (field)
                {
                    case "alias": columns.Add(AliasQuery); break;
                    case "type": columns.Add(@"""Type"""); break;
                    case "active": columns.Add(@"""Staked"""); break;
                    case "address": columns.Add(@"""Address"""); break;
                    case "publicKey": columns.Add(@"""PublicKey"""); break;
                    case "revealed": columns.Add(@"""Revealed"""); break;
                    case "balance": columns.Add(@"""Balance"""); break;
                    case "frozenDeposits": columns.Add(@"""FrozenDeposits"""); break;
                    case "frozenRewards": columns.Add(@"""FrozenRewards"""); break;
                    case "frozenFees": columns.Add(@"""FrozenFees"""); break;
                    case "counter": columns.Add(@"""Counter"""); break;
                    case "activationLevel": columns.Add(@"""ActivationLevel"""); break;
                    case "activationTime": columns.Add(@"""ActivationLevel"""); break;
                    case "deactivationLevel": columns.Add(@"""DeactivationLevel"""); columns.Add(@"""Staked"""); break;
                    case "deactivationTime": columns.Add(@"""DeactivationLevel"""); columns.Add(@"""Staked"""); break;
                    case "stakingBalance": columns.Add(@"""StakingBalance"""); break;
                    case "firstActivity": columns.Add(@"""FirstLevel"""); break;
                    case "firstActivityTime": columns.Add(@"""FirstLevel"""); break;
                    case "lastActivity": columns.Add(@"""LastLevel"""); break;
                    case "lastActivityTime": columns.Add(@"""LastLevel"""); break;
                    case "numActivations": columns.Add(@"""Activated"""); break;
                    case "numBallots": columns.Add(@"""BallotsCount"""); break;
                    case "numContracts": columns.Add(@"""ContractsCount"""); break;
                    case "numDelegators": columns.Add(@"""DelegatorsCount"""); break;
                    case "numBlocks": columns.Add(@"""BlocksCount"""); break;
                    case "numDelegations": columns.Add(@"""DelegationsCount"""); break;
                    case "numDoubleBaking": columns.Add(@"""DoubleBakingCount"""); break;
                    case "numDoubleEndorsing": columns.Add(@"""DoubleEndorsingCount"""); break;
                    case "numEndorsements": columns.Add(@"""EndorsementsCount"""); break;
                    case "numNonceRevelations": columns.Add(@"""NonceRevelationsCount"""); break;
                    case "numRevelationPenalties": columns.Add(@"""RevelationPenaltiesCount"""); break;
                    case "numOriginations": columns.Add(@"""OriginationsCount"""); break;
                    case "numProposals": columns.Add(@"""ProposalsCount"""); break;
                    case "numReveals": columns.Add(@"""RevealsCount"""); break;
                    case "numMigrations": columns.Add(@"""MigrationsCount"""); break;
                    case "numTransactions": columns.Add(@"""TransactionsCount"""); break;
                    case "software": columns.Add(@"""SoftwareId"""); break;
                }
            }

            if (columns.Count == 0)
                return Array.Empty<object[]>();

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""Accounts""")
                .Filter("Type", 1)
                .Filter("Staked", active)
                .Filter("LastLevel", lastActivity)
                .Take(sort, offset, limit, x => x switch
                {
                    "activationLevel" => ("ActivationLevel", "ActivationLevel"),
                    "deactivationLevel" => ("DeactivationLevel", "DeactivationLevel"),
                    "stakingBalance" => ("StakingBalance", "StakingBalance"),
                    "balance" => ("Balance", "Balance"),
                    "numDelegators" => ("DelegatorsCount", "DelegatorsCount"),
                    _ => ("Id", "Id")
                });

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            var result = new object[rows.Count()][];
            for (int i = 0; i < result.Length; i++)
                result[i] = new object[fields.Length];

            for (int i = 0, j = 0; i < fields.Length; j = 0, i++)
            {
                switch (fields[i])
                {
                    case "alias":
                        foreach (var row in rows)
                            result[j++][i] = row.Alias;
                        break;
                    case "type":
                        foreach (var row in rows)
                            result[j++][i] = AccountTypes.ToString(row.Type);
                        break;
                    case "active":
                        foreach (var row in rows)
                            result[j++][i] = row.Staked;
                        break;
                    case "address":
                        foreach (var row in rows)
                            result[j++][i] = row.Address;
                        break;
                    case "publicKey":
                        foreach (var row in rows)
                            result[j++][i] = row.PublicKey;
                        break;
                    case "revealed":
                        foreach (var row in rows)
                            result[j++][i] = row.Revealed;
                        break;
                    case "balance":
                        foreach (var row in rows)
                            result[j++][i] = row.Balance;
                        break;
                    case "frozenDeposits":
                        foreach (var row in rows)
                            result[j++][i] = row.FrozenDeposits;
                        break;
                    case "frozenRewards":
                        foreach (var row in rows)
                            result[j++][i] = row.FrozenRewards;
                        break;
                    case "frozenFees":
                        foreach (var row in rows)
                            result[j++][i] = row.FrozenFees;
                        break;
                    case "counter":
                        foreach (var row in rows)
                            result[j++][i] = row.Counter;
                        break;
                    case "activationLevel":
                        foreach (var row in rows)
                            result[j++][i] = row.ActivationLevel;
                        break;
                    case "activationTime":
                        foreach (var row in rows)
                            result[j++][i] = Time[row.ActivationLevel];
                        break;
                    case "deactivationLevel":
                        foreach (var row in rows)
                            result[j++][i] = row.Staked ? null : (int?)row.DeactivationLevel;
                        break;
                    case "deactivationTime":
                        foreach (var row in rows)
                            result[j++][i] = row.Staked ? null : (DateTime?)Time[row.DeactivationLevel];
                        break;
                    case "stakingBalance":
                        foreach (var row in rows)
                            result[j++][i] = row.StakingBalance;
                        break;
                    case "firstActivity":
                        foreach (var row in rows)
                            result[j++][i] = row.FirstLevel;
                        break;
                    case "firstActivityTime":
                        foreach (var row in rows)
                            result[j++][i] = Time[row.FirstLevel];
                        break;
                    case "lastActivity":
                        foreach (var row in rows)
                            result[j++][i] = row.LastLevel;
                        break;
                    case "lastActivityTime":
                        foreach (var row in rows)
                            result[j++][i] = Time[row.LastLevel];
                        break;
                    case "numActivations":
                        foreach (var row in rows)
                            result[j++][i] = row.Activated == true ? 1 : 0;
                        break;
                    case "numBallots":
                        foreach (var row in rows)
                            result[j++][i] = row.BallotsCount;
                        break;
                    case "numContracts":
                        foreach (var row in rows)
                            result[j++][i] = row.ContractsCount;
                        break;
                    case "numDelegators":
                        foreach (var row in rows)
                            result[j++][i] = row.DelegatorsCount;
                        break;
                    case "numBlocks":
                        foreach (var row in rows)
                            result[j++][i] = row.BlocksCount;
                        break;
                    case "numDelegations":
                        foreach (var row in rows)
                            result[j++][i] = row.DelegationsCount;
                        break;
                    case "numDoubleBaking":
                        foreach (var row in rows)
                            result[j++][i] = row.DoubleBakingCount;
                        break;
                    case "numDoubleEndorsing":
                        foreach (var row in rows)
                            result[j++][i] = row.DoubleEndorsingCount;
                        break;
                    case "numEndorsements":
                        foreach (var row in rows)
                            result[j++][i] = row.EndorsementsCount;
                        break;
                    case "numNonceRevelations":
                        foreach (var row in rows)
                            result[j++][i] = row.NonceRevelationsCount;
                        break;
                    case "numRevelationPenalties":
                        foreach (var row in rows)
                            result[j++][i] = row.RevelationPenaltiesCount;
                        break;
                    case "numOriginations":
                        foreach (var row in rows)
                            result[j++][i] = row.OriginationsCount;
                        break;
                    case "numProposals":
                        foreach (var row in rows)
                            result[j++][i] = row.ProposalsCount;
                        break;
                    case "numReveals":
                        foreach (var row in rows)
                            result[j++][i] = row.RevealsCount;
                        break;
                    case "numMigrations":
                        foreach (var row in rows)
                            result[j++][i] = row.MigrationsCount;
                        break;
                    case "numTransactions":
                        foreach (var row in rows)
                            result[j++][i] = row.TransactionsCount;
                        break;
                    case "software":
                        foreach (var row in rows)
                            result[j++][i] = row.SoftwareId != null ? Software[row.SoftwareId] : null;
                        break;
                }
            }

            return result;
        }

        public async Task<object[]> GetDelegates(
            BoolParameter active,
            Int32Parameter lastActivity,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            string field)
        {
            var columns = new HashSet<string>(3);
            switch (field)
            {
                case "alias": columns.Add(AliasQuery); break;
                case "type": columns.Add(@"""Type"""); break;
                case "active": columns.Add(@"""Staked"""); break;
                case "address": columns.Add(@"""Address"""); break;
                case "publicKey": columns.Add(@"""PublicKey"""); break;
                case "revealed": columns.Add(@"""Revealed"""); break;
                case "balance": columns.Add(@"""Balance"""); break;
                case "frozenDeposits": columns.Add(@"""FrozenDeposits"""); break;
                case "frozenRewards": columns.Add(@"""FrozenRewards"""); break;
                case "frozenFees": columns.Add(@"""FrozenFees"""); break;
                case "counter": columns.Add(@"""Counter"""); break;
                case "activationLevel": columns.Add(@"""ActivationLevel"""); break;
                case "activationTime": columns.Add(@"""ActivationLevel"""); break;
                case "deactivationLevel": columns.Add(@"""DeactivationLevel"""); columns.Add(@"""Staked"""); break;
                case "deactivationTime": columns.Add(@"""DeactivationLevel"""); columns.Add(@"""Staked"""); break;
                case "stakingBalance": columns.Add(@"""StakingBalance"""); break;
                case "firstActivity": columns.Add(@"""FirstLevel"""); break;
                case "firstActivityTime": columns.Add(@"""FirstLevel"""); break;
                case "lastActivity": columns.Add(@"""LastLevel"""); break;
                case "lastActivityTime": columns.Add(@"""LastLevel"""); break;
                case "numActivations": columns.Add(@"""Activated"""); break;
                case "numBallots": columns.Add(@"""BallotsCount"""); break;
                case "numContracts": columns.Add(@"""ContractsCount"""); break;
                case "numDelegators": columns.Add(@"""DelegatorsCount"""); break;
                case "numBlocks": columns.Add(@"""BlocksCount"""); break;
                case "numDelegations": columns.Add(@"""DelegationsCount"""); break;
                case "numDoubleBaking": columns.Add(@"""DoubleBakingCount"""); break;
                case "numDoubleEndorsing": columns.Add(@"""DoubleEndorsingCount"""); break;
                case "numEndorsements": columns.Add(@"""EndorsementsCount"""); break;
                case "numNonceRevelations": columns.Add(@"""NonceRevelationsCount"""); break;
                case "numRevelationPenalties": columns.Add(@"""RevelationPenaltiesCount"""); break;
                case "numOriginations": columns.Add(@"""OriginationsCount"""); break;
                case "numProposals": columns.Add(@"""ProposalsCount"""); break;
                case "numReveals": columns.Add(@"""RevealsCount"""); break;
                case "numMigrations": columns.Add(@"""MigrationsCount"""); break;
                case "numTransactions": columns.Add(@"""TransactionsCount"""); break;
                case "software": columns.Add(@"""SoftwareId"""); break;
            }

            if (columns.Count == 0)
                return Array.Empty<object>();

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""Accounts""")
                .Filter("Type", 1)
                .Filter("Staked", active)
                .Filter("LastLevel", lastActivity)
                .Take(sort, offset, limit, x => x switch
                {
                    "activationLevel" => ("ActivationLevel", "ActivationLevel"),
                    "deactivationLevel" => ("DeactivationLevel", "DeactivationLevel"),
                    "stakingBalance" => ("StakingBalance", "StakingBalance"),
                    "balance" => ("Balance", "Balance"),
                    "numDelegators" => ("DelegatorsCount", "DelegatorsCount"),
                    _ => ("Id", "Id")
                });

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            var result = new object[rows.Count()];
            var j = 0;

            switch (field)
            {
                case "alias":
                    foreach (var row in rows)
                        result[j++] = row.Alias;
                    break;
                case "type":
                    foreach (var row in rows)
                        result[j++] = AccountTypes.ToString(row.Type);
                    break;
                case "active":
                    foreach (var row in rows)
                        result[j++] = row.Staked;
                    break;
                case "address":
                    foreach (var row in rows)
                        result[j++] = row.Address;
                    break;
                case "publicKey":
                    foreach (var row in rows)
                        result[j++] = row.PublicKey;
                    break;
                case "revealed":
                    foreach (var row in rows)
                        result[j++] = row.Revealed;
                    break;
                case "balance":
                    foreach (var row in rows)
                        result[j++] = row.Balance;
                    break;
                case "frozenDeposits":
                    foreach (var row in rows)
                        result[j++] = row.FrozenDeposits;
                    break;
                case "frozenRewards":
                    foreach (var row in rows)
                        result[j++] = row.FrozenRewards;
                    break;
                case "frozenFees":
                    foreach (var row in rows)
                        result[j++] = row.FrozenFees;
                    break;
                case "counter":
                    foreach (var row in rows)
                        result[j++] = row.Counter;
                    break;
                case "activationLevel":
                    foreach (var row in rows)
                        result[j++] = row.ActivationLevel;
                    break;
                case "activationTime":
                    foreach (var row in rows)
                        result[j++] = Time[row.ActivationLevel];
                    break;
                case "deactivationLevel":
                    foreach (var row in rows)
                        result[j++] = row.Staked ? null : (int?)row.DeactivationLevel;
                    break;
                case "deactivationTime":
                    foreach (var row in rows)
                        result[j++] = row.Staked ? null : (DateTime?)Time[row.DeactivationLevel];
                    break;
                case "stakingBalance":
                    foreach (var row in rows)
                        result[j++] = row.StakingBalance;
                    break;
                case "firstActivity":
                    foreach (var row in rows)
                        result[j++] = row.FirstLevel;
                    break;
                case "firstActivityTime":
                    foreach (var row in rows)
                        result[j++] = Time[row.FirstLevel];
                    break;
                case "lastActivity":
                    foreach (var row in rows)
                        result[j++] = row.LastLevel;
                    break;
                case "lastActivityTime":
                    foreach (var row in rows)
                        result[j++] = Time[row.LastLevel];
                    break;
                case "numActivations":
                    foreach (var row in rows)
                        result[j++] = row.Activated == true ? 1 : 0;
                    break;
                case "numBallots":
                    foreach (var row in rows)
                        result[j++] = row.BallotsCount;
                    break;
                case "numContracts":
                    foreach (var row in rows)
                        result[j++] = row.ContractsCount;
                    break;
                case "numDelegators":
                    foreach (var row in rows)
                        result[j++] = row.DelegatorsCount;
                    break;
                case "numBlocks":
                    foreach (var row in rows)
                        result[j++] = row.BlocksCount;
                    break;
                case "numDelegations":
                    foreach (var row in rows)
                        result[j++] = row.DelegationsCount;
                    break;
                case "numDoubleBaking":
                    foreach (var row in rows)
                        result[j++] = row.DoubleBakingCount;
                    break;
                case "numDoubleEndorsing":
                    foreach (var row in rows)
                        result[j++] = row.DoubleEndorsingCount;
                    break;
                case "numEndorsements":
                    foreach (var row in rows)
                        result[j++] = row.EndorsementsCount;
                    break;
                case "numNonceRevelations":
                    foreach (var row in rows)
                        result[j++] = row.NonceRevelationsCount;
                    break;
                case "numRevelationPenalties":
                    foreach (var row in rows)
                        result[j++] = row.RevelationPenaltiesCount;
                    break;
                case "numOriginations":
                    foreach (var row in rows)
                        result[j++] = row.OriginationsCount;
                    break;
                case "numProposals":
                    foreach (var row in rows)
                        result[j++] = row.ProposalsCount;
                    break;
                case "numReveals":
                    foreach (var row in rows)
                        result[j++] = row.RevealsCount;
                    break;
                case "numMigrations":
                    foreach (var row in rows)
                        result[j++] = row.MigrationsCount;
                    break;
                case "numTransactions":
                    foreach (var row in rows)
                        result[j++] = row.TransactionsCount;
                    break;
                case "software":
                    foreach (var row in rows)
                        result[j++] = row.SoftwareId != null ? Software[row.SoftwareId] : null;
                    break;
            }

            return result;
        }
    }
}
