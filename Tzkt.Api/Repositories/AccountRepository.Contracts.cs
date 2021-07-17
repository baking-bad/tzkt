using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Netezos.Contracts;
using Netezos.Encoding;

using Tzkt.Api.Models;
using Tzkt.Api.Services.Cache;
using Tzkt.Api.Utils;

namespace Tzkt.Api.Repositories
{
    public partial class AccountRepository : DbConnection
    {
        public async Task<Contract> GetContract(string address)
        {
            var rawAccount = await Accounts.GetAsync(address);
            if (rawAccount is not RawContract contract)
                return null;

            var creator = contract.CreatorId == null ? null
                : await Accounts.GetAsync((int)contract.CreatorId);

            var manager = contract.ManagerId == null ? null
                : (RawUser)await Accounts.GetAsync((int)contract.ManagerId);

            var delegat = contract.DelegateId == null ? null
                : await Accounts.GetAsync((int)contract.DelegateId);

            return new Contract
            {
                Alias = contract.Alias,
                Address = contract.Address,
                Kind = ContractKinds.ToString(contract.Kind),
                Tzips = GetTzips(contract.Tzips),
                Balance = contract.Balance,
                Creator = creator == null ? null : new CreatorInfo
                {
                    Alias = creator.Alias,
                    Address = creator.Address
                },
                Manager = manager == null ? null : new ManagerInfo
                {
                    Alias = manager.Alias,
                    Address = manager.Address,
                    PublicKey = manager.PublicKey,
                },
                Delegate = delegat == null ? null : new DelegateInfo
                {
                    Alias = delegat.Alias,
                    Address = delegat.Address,
                    Active = delegat.Staked
                },
                DelegationLevel = delegat == null ? null : contract.DelegationLevel,
                DelegationTime = delegat == null ? null : Time[(int)contract.DelegationLevel],
                FirstActivity = contract.FirstLevel,
                FirstActivityTime = Time[contract.FirstLevel],
                LastActivity = contract.LastLevel,
                LastActivityTime = Time[contract.LastLevel],
                NumContracts = contract.ContractsCount,
                NumDelegations = contract.DelegationsCount,
                NumOriginations = contract.OriginationsCount,
                NumReveals = contract.RevealsCount,
                NumMigrations = contract.MigrationsCount,
                NumTransactions = contract.TransactionsCount,
                TypeHash = contract.TypeHash,
                CodeHash = contract.CodeHash,
            };
        }

        public async Task<int> GetContractsCount(ContractKindParameter kind)
        {
            var sql = new SqlBuilder(@"SELECT COUNT(*) FROM ""Accounts""")
                .Filter("Type", 2)
                .Filter("Kind", kind);

            using var db = GetConnection();
            return await db.QueryFirstAsync<int>(sql.Query, sql.Params);
        }

        public async Task<IEnumerable<Contract>> GetContracts(
            ContractKindParameter kind,
            AccountParameter creator,
            AccountParameter manager,
            AccountParameter @delegate,
            Int32Parameter lastActivity,
            Int32Parameter typeHash,
            Int32Parameter codeHash,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            bool includeStorage)
        {
            var query = includeStorage
                ? $@"
                    SELECT      acc.*, {AliasQuery}, st.""JsonValue""
                    FROM        ""Accounts"" AS acc
                    LEFT JOIN   ""Storages"" AS st
                           ON   st.""ContractId"" = acc.""Id"" AND st.""Current"" = true
                "
                : $@"SELECT *, {AliasQuery} FROM ""Accounts""";

            var sql = new SqlBuilder(query)
                .Filter("Type", 2)
                .Filter("CreatorId", creator, x => x == "manager" ? "ManagerId" : "DelegateId")
                .Filter("ManagerId", manager, x => x == "creator" ? "CreatorId" : "DelegateId")
                .Filter("DelegateId", @delegate, x => x == "manager" ? "ManagerId" : "CreatorId")
                .Filter("Kind", kind)
                .Filter("LastLevel", lastActivity)
                .Filter("TypeHash", typeHash)
                .Filter("CodeHash", codeHash)
                .Take(sort, offset, limit, x => x switch
                {
                    "balance" => ("Balance", "Balance"),
                    "firstActivity" => ("FirstLevel", "FirstLevel"),
                    "lastActivity" => ("LastLevel", "LastLevel"),
                    "numTransactions" => ("TransactionsCount", "TransactionsCount"),
                    _ => ("Id", "Id")
                });

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            return rows.Select(row =>
            {
                var creator = row.CreatorId == null ? null
                    : Accounts.Get((int)row.CreatorId);

                var manager = row.ManagerId == null ? null
                    : (RawUser)Accounts.Get((int)row.ManagerId);

                var contractDelegate = row.DelegateId == null ? null
                    : Accounts.Get((int)row.DelegateId);

                return new Contract
                {
                    Alias = row.Alias,
                    Address = row.Address,
                    Kind = ContractKinds.ToString(row.Kind),
                    Tzips = GetTzips(row.Tzips),
                    Balance = row.Balance,
                    Creator = creator == null ? null : new CreatorInfo
                    {
                        Alias = creator.Alias,
                        Address = creator.Address
                    },
                    Manager = manager == null ? null : new ManagerInfo
                    {
                        Alias = manager.Alias,
                        Address = manager.Address,
                        PublicKey = manager.PublicKey,
                    },
                    Delegate = contractDelegate == null ? null : new DelegateInfo
                    {
                        Alias = contractDelegate.Alias,
                        Address = contractDelegate.Address,
                        Active = contractDelegate.Staked
                    },
                    DelegationLevel = contractDelegate == null ? null : row.DelegationLevel,
                    DelegationTime = contractDelegate == null ? null : (DateTime?)Time[row.DelegationLevel],
                    FirstActivity = row.FirstLevel,
                    FirstActivityTime = Time[row.FirstLevel],
                    LastActivity = row.LastLevel,
                    LastActivityTime = Time[row.LastLevel],
                    NumContracts = row.ContractsCount,
                    NumDelegations = row.DelegationsCount,
                    NumOriginations = row.OriginationsCount,
                    NumReveals = row.RevealsCount,
                    NumMigrations = row.MigrationsCount,
                    NumTransactions = row.TransactionsCount,
                    TypeHash = row.TypeHash,
                    CodeHash = row.CodeHash,
                    Storage = row.Kind == 0 ? $"\"{manager.Address}\"" : (RawJson)row.JsonValue
                };
            });
        }

        public async Task<object[][]> GetContracts(
            ContractKindParameter kind,
            AccountParameter creator,
            AccountParameter manager,
            AccountParameter @delegate,
            Int32Parameter lastActivity,
            Int32Parameter typeHash,
            Int32Parameter codeHash,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            string[] fields,
            bool includeStorage)
        {
            var columns = new HashSet<string>(fields.Length + 2);
            var joins = new HashSet<string>(1);

            foreach (var field in fields)
            {
                switch (field)
                {
                    case "alias": columns.Add(AliasQuery); break;
                    case "type": columns.Add(@"acc.""Type"""); break;
                    case "kind": columns.Add(@"acc.""Kind"""); break;
                    case "tzips": columns.Add(@"acc.""Tzips"""); break;
                    case "address": columns.Add(@"acc.""Address"""); break;
                    case "balance": columns.Add(@"acc.""Balance"""); break;
                    case "creator": columns.Add(@"acc.""CreatorId"""); break;
                    case "manager": columns.Add(@"acc.""ManagerId"""); break;
                    case "delegate": columns.Add(@"acc.""DelegateId"""); break;
                    case "delegationLevel": columns.Add(@"acc.""DelegationLevel"""); columns.Add(@"acc.""DelegateId"""); break;
                    case "delegationTime": columns.Add(@"acc.""DelegationLevel"""); columns.Add(@"acc.""DelegateId"""); break;
                    case "numContracts": columns.Add(@"acc.""ContractsCount"""); break;
                    case "numDelegations": columns.Add(@"acc.""DelegationsCount"""); break;
                    case "numOriginations": columns.Add(@"acc.""OriginationsCount"""); break;
                    case "numTransactions": columns.Add(@"acc.""TransactionsCount"""); break;
                    case "numReveals": columns.Add(@"acc.""RevealsCount"""); break;
                    case "numMigrations": columns.Add(@"acc.""MigrationsCount"""); break;
                    case "firstActivity": columns.Add(@"acc.""FirstLevel"""); break;
                    case "firstActivityTime": columns.Add(@"acc.""FirstLevel"""); break;
                    case "lastActivity": columns.Add(@"acc.""LastLevel"""); break;
                    case "lastActivityTime": columns.Add(@"acc.""LastLevel"""); break;
                    case "typeHash": columns.Add(@"acc.""TypeHash"""); break;
                    case "codeHash": columns.Add(@"acc.""CodeHash"""); break;
                    case "storage" when includeStorage:
                        columns.Add(@"acc.""Kind""");
                        columns.Add(@"acc.""ManagerId""");
                        columns.Add(@"st.""JsonValue""");
                        joins.Add(@"LEFT JOIN ""Storages"" as st ON st.""ContractId"" = acc.""Id"" AND st.""Current"" = true");
                        break;
                }
            }

            if (columns.Count == 0)
                return Array.Empty<object[]>();

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""Accounts"" as acc {string.Join(' ', joins)}")
                .Filter("Type", 2)
                .Filter("CreatorId", creator, x => x == "manager" ? "ManagerId" : "DelegateId")
                .Filter("ManagerId", manager, x => x == "creator" ? "CreatorId" : "DelegateId")
                .Filter("DelegateId", @delegate, x => x == "manager" ? "ManagerId" : "CreatorId")
                .Filter("Kind", kind)
                .Filter("LastLevel", lastActivity)
                .Filter("TypeHash", typeHash)
                .Filter("CodeHash", codeHash)
                .Take(sort, offset, limit, x => x switch
                {
                    "balance" => ("Balance", "Balance"),
                    "firstActivity" => ("FirstLevel", "FirstLevel"),
                    "lastActivity" => ("LastLevel", "LastLevel"),
                    "numTransactions" => ("TransactionsCount", "TransactionsCount"),
                    _ => ("Id", "Id")
                }, "acc");

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
                    case "kind":
                        foreach (var row in rows)
                            result[j++][i] = ContractKinds.ToString(row.Kind);
                        break;
                    case "tzips":
                        foreach (var row in rows)
                            result[j++][i] = GetTzips(row.Tzips);
                        break;
                    case "address":
                        foreach (var row in rows)
                            result[j++][i] = row.Address;
                        break;
                    case "balance":
                        foreach (var row in rows)
                            result[j++][i] = row.Balance;
                        break;
                    case "creator":
                        foreach (var row in rows)
                        {
                            var _creator = row.CreatorId == null ? null : Accounts.Get((int)row.CreatorId);
                            result[j++][i] = _creator == null ? null : new CreatorInfo
                            {
                                Alias = _creator.Alias,
                                Address = _creator.Address
                            };
                        }
                        break;
                    case "manager":
                        foreach (var row in rows)
                        {
                            var _manager = row.ManagerId == null ? null : (RawUser)Accounts.Get((int)row.ManagerId);
                            result[j++][i] = _manager == null ? null : new ManagerInfo
                            {
                                Alias = _manager.Alias,
                                Address = _manager.Address,
                                PublicKey = _manager.PublicKey,
                            };
                        }
                        break;
                    case "delegate":
                        foreach (var row in rows)
                        {
                            var delegat = row.DelegateId == null ? null : Accounts.Get((int)row.DelegateId);
                            result[j++][i] = delegat == null ? null : new DelegateInfo
                            {
                                Alias = delegat.Alias,
                                Address = delegat.Address,
                                Active = delegat.Staked
                            };
                        }
                        break;
                    case "delegationLevel":
                        foreach (var row in rows)
                            result[j++][i] = row.DelegateId == null ? null : row.DelegationLevel;
                        break;
                    case "delegationTime":
                        foreach (var row in rows)
                            result[j++][i] = row.DelegateId == null ? null : Time[row.DelegationLevel];
                        break;
                    case "numContracts":
                        foreach (var row in rows)
                            result[j++][i] = row.ContractsCount;
                        break;
                    case "numDelegations":
                        foreach (var row in rows)
                            result[j++][i] = row.DelegationsCount;
                        break;
                    case "numOriginations":
                        foreach (var row in rows)
                            result[j++][i] = row.OriginationsCount;
                        break;
                    case "numTransactions":
                        foreach (var row in rows)
                            result[j++][i] = row.TransactionsCount;
                        break;
                    case "numReveals":
                        foreach (var row in rows)
                            result[j++][i] = row.RevealsCount;
                        break;
                    case "numMigrations":
                        foreach (var row in rows)
                            result[j++][i] = row.MigrationsCount;
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
                    case "typeHash":
                        foreach (var row in rows)
                            result[j++][i] = row.TypeHash;
                        break;
                    case "codeHash":
                        foreach (var row in rows)
                            result[j++][i] = row.CodeHash;
                        break;
                    case "storage":
                        foreach (var row in rows)
                        {
                            if (row.Kind == 0)
                            {
                                var _manager = Accounts.Get((int)row.ManagerId);
                                result[j++][i] = _manager.Address;
                            }
                            else
                            {
                                result[j++][i] = (RawJson)row.JsonValue;
                            }
                        }
                        break;
                }
            }

            return result;
        }

        public async Task<object[]> GetContracts(
            ContractKindParameter kind,
            AccountParameter creator,
            AccountParameter manager,
            AccountParameter @delegate,
            Int32Parameter lastActivity,
            Int32Parameter typeHash,
            Int32Parameter codeHash,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            string field,
            bool includeStorage)
        {
            var columns = new HashSet<string>(3);
            var joins = new HashSet<string>(1);

            switch (field)
            {
                case "alias": columns.Add(AliasQuery); break;
                case "type": columns.Add(@"acc.""Type"""); break;
                case "kind": columns.Add(@"acc.""Kind"""); break;
                case "tzips": columns.Add(@"acc.""Tzips"""); break;
                case "address": columns.Add(@"acc.""Address"""); break;
                case "balance": columns.Add(@"acc.""Balance"""); break;
                case "creator": columns.Add(@"acc.""CreatorId"""); break;
                case "manager": columns.Add(@"acc.""ManagerId"""); break;
                case "delegate": columns.Add(@"acc.""DelegateId"""); break;
                case "delegationLevel": columns.Add(@"acc.""DelegationLevel"""); columns.Add(@"acc.""DelegateId"""); break;
                case "delegationTime": columns.Add(@"acc.""DelegationLevel"""); columns.Add(@"acc.""DelegateId"""); break;
                case "numContracts": columns.Add(@"acc.""ContractsCount"""); break;
                case "numDelegations": columns.Add(@"acc.""DelegationsCount"""); break;
                case "numOriginations": columns.Add(@"acc.""OriginationsCount"""); break;
                case "numTransactions": columns.Add(@"acc.""TransactionsCount"""); break;
                case "numReveals": columns.Add(@"acc.""RevealsCount"""); break;
                case "numMigrations": columns.Add(@"acc.""MigrationsCount"""); break;
                case "firstActivity": columns.Add(@"acc.""FirstLevel"""); break;
                case "firstActivityTime": columns.Add(@"acc.""FirstLevel"""); break;
                case "lastActivity": columns.Add(@"acc.""LastLevel"""); break;
                case "lastActivityTime": columns.Add(@"acc.""LastLevel"""); break;
                case "typeHash": columns.Add(@"acc.""TypeHash"""); break;
                case "codeHash": columns.Add(@"acc.""CodeHash"""); break;
                case "storage" when includeStorage:
                    columns.Add(@"acc.""Kind""");
                    columns.Add(@"acc.""ManagerId""");
                    columns.Add(@"st.""JsonValue""");
                    joins.Add(@"LEFT JOIN ""Storages"" as st ON st.""ContractId"" = acc.""Id"" AND st.""Current"" = true");
                    break;
            }

            if (columns.Count == 0)
                return Array.Empty<object>();

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""Accounts"" as acc {string.Join(' ', joins)}")
                .Filter("Type", 2)
                .Filter("CreatorId", creator, x => x == "manager" ? "ManagerId" : "DelegateId")
                .Filter("ManagerId", manager, x => x == "creator" ? "CreatorId" : "DelegateId")
                .Filter("DelegateId", @delegate, x => x == "manager" ? "ManagerId" : "CreatorId")
                .Filter("Kind", kind)
                .Filter("LastLevel", lastActivity)
                .Filter("TypeHash", typeHash)
                .Filter("CodeHash", codeHash)
                .Take(sort, offset, limit, x => x switch
                {
                    "balance" => ("Balance", "Balance"),
                    "firstActivity" => ("FirstLevel", "FirstLevel"),
                    "lastActivity" => ("LastLevel", "LastLevel"),
                    "numTransactions" => ("TransactionsCount", "TransactionsCount"),
                    _ => ("Id", "Id")
                }, "acc");

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
                case "kind":
                    foreach (var row in rows)
                        result[j++] = ContractKinds.ToString(row.Kind);
                    break;
                case "tzips":
                    foreach (var row in rows)
                        result[j++] = GetTzips(row.Tzips);
                    break;
                case "address":
                    foreach (var row in rows)
                        result[j++] = row.Address;
                    break;
                case "balance":
                    foreach (var row in rows)
                        result[j++] = row.Balance;
                    break;
                case "creator":
                    foreach (var row in rows)
                    {
                        var _creator = row.CreatorId == null ? null : Accounts.Get((int)row.CreatorId);
                        result[j++] = _creator == null ? null : new CreatorInfo
                        {
                            Alias = _creator.Alias,
                            Address = _creator.Address
                        };
                    }
                    break;
                case "manager":
                    foreach (var row in rows)
                    {
                        var _manager = row.ManagerId == null ? null : (RawUser)Accounts.Get((int)row.ManagerId);
                        result[j++] = _manager == null ? null : new ManagerInfo
                        {
                            Alias = _manager.Alias,
                            Address = _manager.Address,
                            PublicKey = _manager.PublicKey,
                        };
                    }
                    break;
                case "delegate":
                    foreach (var row in rows)
                    {
                        var delegat = row.DelegateId == null ? null : Accounts.Get((int)row.DelegateId);
                        result[j++] = delegat == null ? null : new DelegateInfo
                        {
                            Alias = delegat.Alias,
                            Address = delegat.Address,
                            Active = delegat.Staked
                        };
                    }
                    break;
                case "delegationLevel":
                    foreach (var row in rows)
                        result[j++] = row.DelegateId == null ? null : row.DelegationLevel;
                    break;
                case "delegationTime":
                    foreach (var row in rows)
                        result[j++] = row.DelegateId == null ? null : Time[row.DelegationLevel];
                    break;
                case "numContracts":
                    foreach (var row in rows)
                        result[j++] = row.ContractsCount;
                    break;
                case "numDelegations":
                    foreach (var row in rows)
                        result[j++] = row.DelegationsCount;
                    break;
                case "numOriginations":
                    foreach (var row in rows)
                        result[j++] = row.OriginationsCount;
                    break;
                case "numTransactions":
                    foreach (var row in rows)
                        result[j++] = row.TransactionsCount;
                    break;
                case "numReveals":
                    foreach (var row in rows)
                        result[j++] = row.RevealsCount;
                    break;
                case "numMigrations":
                    foreach (var row in rows)
                        result[j++] = row.MigrationsCount;
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
                case "typeHash":
                    foreach (var row in rows)
                        result[j++] = row.TypeHash;
                    break;
                case "codeHash":
                    foreach (var row in rows)
                        result[j++] = row.CodeHash;
                    break;
                case "storage":
                    foreach (var row in rows)
                    {
                        if (row.Kind == 0)
                        {
                            var _manager = Accounts.Get((int)row.ManagerId);
                            result[j++] = _manager.Address;
                        }
                        else
                        {
                            result[j++] = (RawJson)row.JsonValue;
                        }
                    }
                    break;
            }

            return result;
        }

        public async Task<byte[]> GetByteCode(string address)
        {
            var rawAccount = await Accounts.GetAsync(address);
            if (rawAccount is not RawContract contract) return null;

            if (contract.Kind == 0)
                return Data.Models.Script.ManagerTzBytes;

            using var db = GetConnection();
            var row = await db.QueryFirstOrDefaultAsync($@"SELECT * FROM ""Scripts"" WHERE ""ContractId"" = {contract.Id} AND ""Current"" = true");
            if (row == null) return null;

            var code = new MichelineArray
            {
                Micheline.FromBytes(row.ParameterSchema),
                Micheline.FromBytes(row.StorageSchema),
                Micheline.FromBytes(row.CodeSchema)
            };

            return code.ToBytes();
        }

        public async Task<IMicheline> GetMichelineCode(string address)
        {
            var rawAccount = await Accounts.GetAsync(address);
            if (rawAccount is not RawContract contract) return null;

            if (contract.Kind == 0)
                return Micheline.FromBytes(Data.Models.Script.ManagerTzBytes);

            using var db = GetConnection();
            var row = await db.QueryFirstOrDefaultAsync($@"SELECT * FROM ""Scripts"" WHERE ""ContractId"" = {contract.Id} AND ""Current"" = true");
            if (row == null) return null;

            var code = new MichelineArray
            {
                Micheline.FromBytes(row.ParameterSchema),
                Micheline.FromBytes(row.StorageSchema),
                Micheline.FromBytes(row.CodeSchema)
            };

            return code;
        }

        public async Task<string> GetMichelsonCode(string address)
        {
            var rawAccount = await Accounts.GetAsync(address);
            if (rawAccount is not RawContract contract) return null;

            if (contract.Kind == 0)
                return Micheline.FromBytes(Data.Models.Script.ManagerTzBytes).ToMichelson();

            using var db = GetConnection();
            var row = await db.QueryFirstOrDefaultAsync($@"SELECT * FROM ""Scripts"" WHERE ""ContractId"" = {contract.Id} AND ""Current"" = true");
            if (row == null) return null;

            var code = new MichelineArray
            {
                Micheline.FromBytes(row.ParameterSchema),
                Micheline.FromBytes(row.StorageSchema),
                Micheline.FromBytes(row.CodeSchema)
            };

            return code.ToMichelson();
        }

        public async Task<ContractInterface> GetContractInterface(string address)
        {
            var rawAccount = await Accounts.GetAsync(address);
            if (rawAccount is not RawContract contract) return null;

            ContractParameter param;
            ContractStorage storage;

            if (contract.Kind == 0)
            {
                param = Data.Models.Script.ManagerTz.Parameter;
                storage = Data.Models.Script.ManagerTz.Storage;
            }
            else
            {
                using var db = GetConnection();
                var script = await db.QueryFirstOrDefaultAsync($@"
                    SELECT      ""StorageSchema"", ""ParameterSchema""
                    FROM        ""Scripts""
                    WHERE       ""ContractId"" = {contract.Id} AND ""Current"" = true
                    LIMIT       1"
                );
                if (script == null) return null;
                param = new ContractParameter(Micheline.FromBytes(script.ParameterSchema));
                storage = new ContractStorage(Micheline.FromBytes(script.StorageSchema));
            }

            var rawStorage = await GetRawStorageValue(address);
            var storageTreeView = storage.Schema.ToTreeView(rawStorage);

            return new ContractInterface
            {
                StorageSchema = storage.GetJsonSchema(),
                Entrypoints = param.Entrypoints
                    .Where(x => param.IsEntrypointUseful(x.Key))
                    .Select(x => new EntrypointInterface
                    {
                        Name = x.Key,
                        ParameterSchema = x.Value.GetJsonSchema()
                    })
                    .ToList(),
                BigMaps = storageTreeView.Nodes()
                    .Where(x => x.Schema is BigMapSchema)
                    .Select(x => new BigMapInterface
                    {
                        Name = x.Name,
                        Path = x.Path,
                        KeySchema = (x.Schema as BigMapSchema).Key.GetJsonSchema(),
                        ValueSchema = (x.Schema as BigMapSchema).Value.GetJsonSchema()
                    })
                    .ToList()
            };
        }

        public async Task<IMicheline> BuildEntrypointParameters(string address, string name, object value)
        {
            var rawAccount = await Accounts.GetAsync(address);
            if (rawAccount is not RawContract contract) return null;

            ContractParameter param;
            if (contract.Kind == 0)
            {
                param = Data.Models.Script.ManagerTz.Parameter;
            }
            else
            {
                using var db = GetConnection();
                var row = await db.QueryFirstOrDefaultAsync($@"SELECT ""ParameterSchema"" FROM ""Scripts"" WHERE ""ContractId"" = {contract.Id} AND ""Current"" = true");
                if (row == null) return null;
                param = new ContractParameter(Micheline.FromBytes(row.ParameterSchema));
            }
            if (!param.Entrypoints.ContainsKey(name)) return null;

            return param.BuildOptimized(name, value);
        }

        public async Task<Entrypoint> GetEntrypoint(string address, string name, bool json, bool micheline, bool michelson)
        {
            var rawAccount = await Accounts.GetAsync(address);
            if (rawAccount is not RawContract contract) return null;

            ContractParameter param;
            if (contract.Kind == 0)
            {
                param = Data.Models.Script.ManagerTz.Parameter;
            }
            else
            {
                using var db = GetConnection();
                var row = await db.QueryFirstOrDefaultAsync($@"SELECT ""ParameterSchema"" FROM ""Scripts"" WHERE ""ContractId"" = {contract.Id} AND ""Current"" = true");
                if (row == null) return null;
                param = new ContractParameter(Micheline.FromBytes(row.ParameterSchema));
            }
            if (!param.Entrypoints.TryGetValue(name, out var ep)) return null;

            var mich = micheline ? ep.ToMicheline() : null;
            return new Entrypoint
            {
                Name = name,
                JsonParameters = json ? ep.Humanize() : null,
                MichelineParameters = mich,
                MichelsonParameters = michelson ? (mich ?? ep.ToMicheline()).ToMichelson() : null,
                Unused = !param.IsEntrypointUseful(name)
            };
        }

        public async Task<IEnumerable<Entrypoint>> GetEntrypoints(string address, bool all, bool json, bool micheline, bool michelson)
        {
            var rawAccount = await Accounts.GetAsync(address);
            if (rawAccount is not RawContract contract) return Enumerable.Empty<Entrypoint>();

            ContractParameter param;
            if (contract.Kind == 0)
            {
                param = Data.Models.Script.ManagerTz.Parameter;
            }
            else
            {
                using var db = GetConnection();
                var row = await db.QueryFirstOrDefaultAsync($@"SELECT ""ParameterSchema"" FROM ""Scripts"" WHERE ""ContractId"" = {contract.Id} AND ""Current"" = true");
                if (row == null) return Enumerable.Empty<Entrypoint>();
                param = new ContractParameter(Micheline.FromBytes(row.ParameterSchema));
            }

            return param.Entrypoints
                .Where(x => all || param.IsEntrypointUseful(x.Key))
                .Select(x =>
                {
                    var mich = micheline ? x.Value.ToMicheline() : null;
                    return new Entrypoint
                    {
                        Name = x.Key,
                        JsonParameters = json ? x.Value.Humanize() : null,
                        MichelineParameters = mich,
                        MichelsonParameters = michelson ? (mich ?? x.Value.ToMicheline()).ToMichelson() : null,
                        Unused = all && !param.IsEntrypointUseful(x.Key)
                    };
                });
        }

        public async Task<string> GetStorageValue(string address, JsonPath[] path)
        {
            var rawAccount = await Accounts.GetAsync(address);
            if (rawAccount is not RawContract contract) return null;

            if (contract.Kind == 0)
            {
                var manager = await Accounts.GetAsync((int)contract.ManagerId);
                return path?.Length > 0 ? null : $"\"{manager.Address}\"";
            }

            var pathSelector = path == null ? string.Empty : " #> @path";
            var pathParam = path == null ? null : new { path = JsonPath.Select(path) };

            using var db = GetConnection();
            var row = await db.QueryFirstOrDefaultAsync($@"
                SELECT   ""JsonValue""{pathSelector} as ""JsonValue""
                FROM     ""Storages""
                WHERE    ""ContractId"" = {contract.Id} AND ""Current"" = true
                LIMIT    1",
                pathParam);

            return row?.JsonValue;
        }

        public async Task<string> GetStorageValue(string address, JsonPath[] path, int level)
        {
            var rawAccount = await Accounts.GetAsync(address);
            if (rawAccount is not RawContract contract) return null;

            if (level < contract.FirstLevel)
                return null;

            if (level >= contract.LastLevel)
                return await GetStorageValue(address, path);

            if (contract.Kind == 0)
            {
                var manager = await Accounts.GetAsync((int)contract.ManagerId);
                return path?.Length > 0 ? null : $"\"{manager.Address}\"";
            }

            var pathSelector = path == null ? string.Empty : " #> @path";
            var pathParam = path == null ? null : new { path = JsonPath.Select(path) };

            using var db = GetConnection();
            var row = await db.QueryFirstOrDefaultAsync($@"
                SELECT   ""JsonValue""{pathSelector} as ""JsonValue""
                FROM     ""Storages""
                WHERE    ""ContractId"" = {contract.Id}
                AND      ""Level"" <= {level}
                ORDER BY ""Level"" DESC, ""Id"" DESC
                LIMIT    1",
                pathParam);

            return row?.JsonValue;
        }

        public async Task<IMicheline> GetRawStorageValue(string address)
        {
            var rawAccount = await Accounts.GetAsync(address);
            if (rawAccount is not RawContract contract) return null;

            if (contract.Kind == 0)
            {
                var manager = await Accounts.GetAsync((int)contract.ManagerId);
                return new MichelineString(manager.Address);
            }

            using var db = GetConnection();
            var row = await db.QueryFirstOrDefaultAsync($@"
                SELECT   ""RawValue""
                FROM     ""Storages""
                WHERE    ""ContractId"" = {contract.Id} AND ""Current"" = true
                LIMIT    1");

            if (row == null) return null;
            return Micheline.FromBytes(row.RawValue);
        }

        public async Task<IMicheline> GetRawStorageValue(string address, int level)
        {
            var rawAccount = await Accounts.GetAsync(address);
            if (rawAccount is not RawContract contract) return null;

            if (level < contract.FirstLevel)
                return null;

            if (level >= contract.LastLevel)
                return await GetRawStorageValue(address);

            if (contract.Kind == 0)
            {
                var manager = await Accounts.GetAsync((int)contract.ManagerId);
                return new MichelineString(manager.Address);
            }

            using var db = GetConnection();
            var row = await db.QueryFirstOrDefaultAsync($@"
                SELECT   ""RawValue""
                FROM     ""Storages""
                WHERE    ""ContractId"" = {contract.Id}
                AND      ""Level"" <= {level}
                ORDER BY ""Level"" DESC, ""TransactionId"" DESC
                LIMIT    1");

            if (row == null) return null;
            return Micheline.FromBytes(row.RawValue);
        }

        public async Task<string> GetStorageSchema(string address)
        {
            var rawAccount = await Accounts.GetAsync(address);
            if (rawAccount is not RawContract contract) return null;

            if (contract.Kind == 0)
                return Data.Models.Script.ManagerTz.Storage.Humanize();

            using var db = GetConnection();
            var row = await db.QueryFirstOrDefaultAsync($@"
                SELECT      ""StorageSchema""
                FROM        ""Scripts""
                WHERE       ""ContractId"" = {contract.Id} AND ""Current"" = true
                LIMIT       1");

            if (row == null) return null;
            var schema = new ContractStorage(Micheline.FromBytes(row.StorageSchema));
            return schema.Humanize();
        }

        public async Task<string> GetStorageSchema(string address, int level)
        {
            var rawAccount = await Accounts.GetAsync(address);
            if (rawAccount is not RawContract contract) return null;

            if (level < contract.FirstLevel)
                return null;

            if (contract.MigrationsCount == 0 || level >= contract.LastLevel)
                return await GetStorageSchema(address);

            if (contract.Kind == 0)
                return Data.Models.Script.ManagerTz.Storage.Humanize();

            using var db = GetConnection();
            var row = await db.QueryFirstOrDefaultAsync($@"
                SELECT      ""StorageSchema""
                FROM        ""Scripts""
                WHERE       ""ContractId"" = {contract.Id}
                AND         ""Level"" <= {level}
                ORDER BY    ""Level"" DESC
                LIMIT       1");

            if (row == null) return null;
            var schema = new ContractStorage(Micheline.FromBytes(row.StorageSchema));
            return schema.Humanize();
        }

        public async Task<IMicheline> GetRawStorageSchema(string address)
        {
            var rawAccount = await Accounts.GetAsync(address);
            if (rawAccount is not RawContract contract) return null;

            if (contract.Kind == 0)
                return Data.Models.Script.ManagerTz.Storage.Schema.ToMicheline();

            using var db = GetConnection();
            var row = await db.QueryFirstOrDefaultAsync($@"
                SELECT      ""StorageSchema""
                FROM        ""Scripts""
                WHERE       ""ContractId"" = {contract.Id} AND ""Current"" = true
                LIMIT       1");

            if (row == null) return null;
            return (Micheline.FromBytes(row.StorageSchema) as MichelinePrim).Args[0];
        }

        public async Task<IMicheline> GetRawStorageSchema(string address, int level)
        {
            var rawAccount = await Accounts.GetAsync(address);
            if (rawAccount is not RawContract contract) return null;

            if (level < contract.FirstLevel)
                return null;

            if (contract.MigrationsCount == 0 || level >= contract.LastLevel)
                return await GetRawStorageSchema(address);

            if (contract.Kind == 0)
                return Data.Models.Script.ManagerTz.Storage.Schema.ToMicheline();

            using var db = GetConnection();
            var row = await db.QueryFirstOrDefaultAsync($@"
                SELECT      ""StorageSchema""
                FROM        ""Scripts""
                WHERE       ""ContractId"" = {contract.Id}
                AND         ""Level"" <= {level}
                ORDER BY    ""Level"" DESC
                LIMIT       1");

            if (row == null) return null;
            return (Micheline.FromBytes(row.StorageSchema) as MichelinePrim).Args[0];
        }

        public async Task<IEnumerable<StorageRecord>> GetStorageHistory(string address, int lastId, int limit)
        {
            var rawAccount = await Accounts.GetAsync(address);
            if (rawAccount is not RawContract contract) return Enumerable.Empty<StorageRecord>();

            using var db = GetConnection();
            var rows = await db.QueryAsync($@"
                SELECT      ss.""Id"",
                            ss.""Level"",
                            ss.""JsonValue"",
                            ss.""MigrationId"",
                            ss.""TransactionId"",
                            ss.""OriginationId"",
                            o_op.""Timestamp""  as ""OriginationTimestamp"",
                            o_op.""OpHash""     as ""OriginationHash"",
                            o_op.""Counter""    as ""OriginationCounter"",
                            o_op.""Nonce""      as ""OriginationNonce"",
                            t_op.""Timestamp""  as ""TransactionTimestamp"",
                            t_op.""OpHash""     as ""TransactionHash"",
                            t_op.""Counter""    as ""TransactionCounter"",
                            t_op.""Nonce""      as ""TransactionNonce"",
                            t_op.""Entrypoint"" as ""TransactionEntrypoint"",
                            t_op.""JsonParameters"" as ""TransactionJsonParameters"",
                            m_op.""Timestamp""  as ""MigrationTimestamp""
                FROM        ""Storages"" as ss
                LEFT JOIN   ""MigrationOps"" as m_op
                       ON   m_op.""Id"" = ss.""MigrationId""
                LEFT JOIN   ""TransactionOps"" as t_op
                       ON   t_op.""Id"" = ss.""TransactionId""
                LEFT JOIN   ""OriginationOps"" as o_op
                       ON   o_op.""Id"" = ss.""OriginationId""
                WHERE       ss.""ContractId"" = {contract.Id}
                {(lastId > 0 ? $@"AND ss.""Id"" < {lastId}" : "")}
                ORDER BY    ss.""Id"" DESC
                LIMIT       {limit}");
            if (!rows.Any()) return Enumerable.Empty<StorageRecord>();

            return rows.Select(row =>
            {
                DateTime timestamp;
                SourceOperation source;

                if (row.TransactionId != null)
                {
                    timestamp = row.TransactionTimestamp;
                    source = new SourceOperation
                    {
                        Type = OpTypes.Transaction,
                        Hash = row.TransactionHash,
                        Counter = row.TransactionCounter,
                        Nonce = row.TransactionNonce,
                        Parameter = row.TransactionEntrypoint == null ? null : new TxParameter
                        {
                            Entrypoint = row.TransactionEntrypoint,
                            Value = (RawJson)row.TransactionJsonParameters
                        }
                    };
                }
                else if (row.OriginationId != null)
                {
                    timestamp = row.OriginationTimestamp;
                    source = new SourceOperation
                    {
                        Type = OpTypes.Origination,
                        Hash = row.OriginationHash,
                        Counter = row.OriginationCounter,
                        Nonce = row.OriginationNonce
                    };
                }
                else
                {
                    timestamp = row.MigrationTimestamp;
                    source = new SourceOperation
                    {
                        Type = OpTypes.Migration
                    };
                }

                return new StorageRecord
                {
                    Id = row.Id,
                    Timestamp = timestamp,
                    Operation = source,
                    Level = row.Level,
                    Value = (RawJson)row.JsonValue,
                };
            });
        }

        public async Task<IEnumerable<StorageRecord>> GetRawStorageHistory(string address, int lastId, int limit)
        {
            var rawAccount = await Accounts.GetAsync(address);
            if (rawAccount is not RawContract contract) return Enumerable.Empty<StorageRecord>();

            using var db = GetConnection();
            var rows = await db.QueryAsync($@"
                SELECT      ss.""Id"",
                            ss.""Level"",
                            ss.""RawValue"",
                            ss.""MigrationId"",
                            ss.""TransactionId"",
                            ss.""OriginationId"",
                            o_op.""Timestamp""  as ""OriginationTimestamp"",
                            o_op.""OpHash""     as ""OriginationHash"",
                            o_op.""Counter""    as ""OriginationCounter"",
                            o_op.""Nonce""      as ""OriginationNonce"",
                            t_op.""Timestamp""  as ""TransactionTimestamp"",
                            t_op.""OpHash""     as ""TransactionHash"",
                            t_op.""Counter""    as ""TransactionCounter"",
                            t_op.""Nonce""      as ""TransactionNonce"",
                            t_op.""Entrypoint"" as ""TransactionEntrypoint"",
                            t_op.""RawParameters"" as ""TransactionRawParameters"",
                            m_op.""Timestamp""  as ""MigrationTimestamp""
                FROM        ""Storages"" as ss
                LEFT JOIN   ""MigrationOps"" as m_op
                       ON   m_op.""Id"" = ss.""MigrationId""
                LEFT JOIN   ""TransactionOps"" as t_op
                       ON   t_op.""Id"" = ss.""TransactionId""
                LEFT JOIN   ""OriginationOps"" as o_op
                       ON   o_op.""Id"" = ss.""OriginationId""
                WHERE       ss.""ContractId"" = {contract.Id}
                {(lastId > 0 ? $@"AND ss.""Id"" < {lastId}" : "")}
                ORDER BY    ss.""Id"" DESC
                LIMIT       {limit}");
            if (!rows.Any()) return Enumerable.Empty<StorageRecord>();

            return rows.Select(row =>
            {
                DateTime timestamp;
                SourceOperation source;

                if (row.TransactionId != null)
                {
                    timestamp = row.TransactionTimestamp;
                    source = new SourceOperation
                    {
                        Type = OpTypes.Transaction,
                        Hash = row.TransactionHash,
                        Counter = row.TransactionCounter,
                        Nonce = row.TransactionNonce,
                        Parameter = row.TransactionEntrypoint == null ? null : new TxParameter
                        {
                            Entrypoint = row.TransactionEntrypoint,
                            Value = row.TransactionRawParameters != null
                                ? (RawJson)Micheline.ToJson(row.TransactionRawParameters)
                                : null
                        }
                    };
                }
                else if (row.OriginationId != null)
                {
                    timestamp = row.OriginationTimestamp;
                    source = new SourceOperation
                    {
                        Type = OpTypes.Origination,
                        Hash = row.OriginationHash,
                        Counter = row.OriginationCounter,
                        Nonce = row.OriginationNonce
                    };
                }
                else
                {
                    timestamp = row.MigrationTimestamp;
                    source = new SourceOperation
                    {
                        Type = OpTypes.Migration
                    };
                }

                return new StorageRecord
                {
                    Id = row.Id,
                    Timestamp = timestamp,
                    Operation = source,
                    Level = row.Level,
                    Value = (RawJson)Micheline.ToJson(row.RawValue),
                };
            });
        }

        public static async Task<Dictionary<int, object>> GetStorages(IDbConnection db, List<int> ids, MichelineFormat format)
        {
            if (ids.Count == 0) return null;

            var rows = await db.QueryAsync($@"
                SELECT ""Id"", ""{((int)format < 2 ? "Json" : "Raw")}Value""
                FROM ""Storages""
                WHERE ""Id"" = ANY(@ids)",
                new { ids });

            return rows.Any()
                ? rows.ToDictionary(x => (int)x.Id, x => format switch
                {
                    MichelineFormat.Json => (RawJson)x.JsonValue,
                    MichelineFormat.JsonString => x.JsonValue,
                    MichelineFormat.Raw => (RawJson)Micheline.ToJson(x.RawValue),
                    MichelineFormat.RawString => Micheline.ToJson(x.RawValue),
                    _ => throw new Exception("Invalid MichelineFormat value")
                })
                : null;
        }

        IEnumerable<string> GetTzips(int? value)
        {
            if (value == null || value == 0) return null;
            var res = new List<string>(1);

            if (((int)value & (int)Data.Models.Tzip.FA2) == (int)Data.Models.Tzip.FA2)
                res.Add("fa2");
            else if (((int)value & (int)Data.Models.Tzip.FA12) == (int)Data.Models.Tzip.FA12)
                res.Add("fa12");
            else if (((int)value & (int)Data.Models.Tzip.FA1) == (int)Data.Models.Tzip.FA1)
                res.Add("fa1");

            return res;
        }
    }
}
