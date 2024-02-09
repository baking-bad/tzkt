using System.Data;
using Dapper;
using Netmavryk.Contracts;
using Netmavryk.Encoding;
using Tzkt.Api.Models;
using Tzkt.Api.Services.Cache;
using Tzkt.Api.Utils;

namespace Tzkt.Api.Repositories
{
    public partial class AccountRepository : DbConnection
    {
        async Task<IEnumerable<dynamic>> QueryContractsAsync(bool includeStorage, ContractFilter filter, Pagination pagination, List<SelectionField> fields = null)
        {
            var select = $@"
                c.*,
                c.""Extras""#>>'{{profile,alias}}' as ""Alias""";

            if (includeStorage)
                select += @",
                s.""JsonValue"" as ""Storage""";

            if (fields != null)
            {
                var counter = 0;
                var columns = new HashSet<string>(fields.Count);
                foreach (var field in fields)
                {
                    switch (field.Field)
                    {
                        case "id": columns.Add(@"c.""Id"""); break;
                        case "type": columns.Add(@"c.""Type"""); break;
                        case "address": columns.Add(@"c.""Address"""); break;
                        case "kind": columns.Add(@"c.""Kind"""); break;
                        case "tzips": columns.Add(@"c.""Tags"""); break;
                        case "alias": columns.Add(@"c.""Extras""#>>'{profile,alias}' as ""Alias"""); break;
                        case "balance": columns.Add(@"c.""Balance"""); break;
                        case "creator": columns.Add(@"c.""CreatorId"""); break;
                        case "manager": columns.Add(@"c.""ManagerId"""); break;
                        case "delegate": columns.Add(@"c.""DelegateId"""); break;
                        case "delegationLevel": columns.Add(@"c.""DelegationLevel"""); columns.Add(@"c.""DelegateId"""); break;
                        case "delegationTime": columns.Add(@"c.""DelegationLevel"""); columns.Add(@"c.""DelegateId"""); break;
                        case "numContracts": columns.Add(@"c.""ContractsCount"""); break;
                        case "activeTokensCount": columns.Add(@"c.""ActiveTokensCount"""); break;
                        case "tokensCount": columns.Add(@"c.""TokensCount"""); break;
                        case "tokenBalancesCount": columns.Add(@"c.""TokenBalancesCount"""); break;
                        case "tokenTransfersCount": columns.Add(@"c.""TokenTransfersCount"""); break;
                        case "ticketsCount": columns.Add(@"c.""TicketsCount"""); break;
                        case "activeTicketsCount": columns.Add(@"c.""ActiveTicketsCount"""); break;
                        case "ticketBalancesCount": columns.Add(@"c.""TicketBalancesCount"""); break;
                        case "ticketTransfersCount": columns.Add(@"c.""TicketTransfersCount"""); break;
                        case "numDelegations": columns.Add(@"c.""DelegationsCount"""); break;
                        case "numOriginations": columns.Add(@"c.""OriginationsCount"""); break;
                        case "numTransactions": columns.Add(@"c.""TransactionsCount"""); break;
                        case "numReveals": columns.Add(@"c.""RevealsCount"""); break;
                        case "numMigrations": columns.Add(@"c.""MigrationsCount"""); break;
                        case "transferTicketCount": columns.Add(@"c.""TransferTicketCount"""); break;
                        case "increasePaidStorageCount": columns.Add(@"c.""IncreasePaidStorageCount"""); break;
                        case "eventsCount": columns.Add(@"c.""EventsCount"""); break;
                        case "firstActivity": columns.Add(@"c.""FirstLevel"""); break;
                        case "firstActivityTime": columns.Add(@"c.""FirstLevel"""); break;
                        case "lastActivity": columns.Add(@"c.""LastLevel"""); break;
                        case "lastActivityTime": columns.Add(@"c.""LastLevel"""); break;
                        case "typeHash": columns.Add(@"c.""TypeHash"""); break;
                        case "codeHash": columns.Add(@"c.""CodeHash"""); break;
                        case "storage" when includeStorage:
                            if (field.Path == null)
                            {   
                                columns.Add(@"c.""Kind""");
                                columns.Add(@"c.""ManagerId""");
                                columns.Add(@"s.""JsonValue"" as ""Storage""");
                            }
                            else
                            {
                                field.Column = $"c{counter++}";
                                columns.Add($@"s.""JsonValue"" #> '{{{field.PathString}}}' as {field.Column}");
                            }
                            break;
                    }
                }

                if (columns.Count == 0)
                    return Enumerable.Empty<dynamic>();

                select = string.Join(',', columns);
            }

            var sql = new SqlBuilder($@"
                SELECT {select} FROM ""Accounts"" as c
                LEFT JOIN ""Storages"" AS s ON s.""ContractId"" = c.""Id"" AND s.""Current"" = true")
                .FilterA(@"c.""Type""", 2)
                .FilterA(@"c.""Id""", filter.id)
                .FilterA(@"c.""Address""", filter.address)
                .FilterA(@"c.""Kind""", filter.kind)
                .FilterA(@"c.""Tags""", filter.tzips)
                .FilterA(@"c.""Balance""", filter.balance)
                .FilterA(@"c.""CreatorId""", filter.creator, x => x == "manager" ? @"c.""ManagerId""" : @"c.""DelegateId""")
                .FilterA(@"c.""ManagerId""", filter.manager, x => x == "creator" ? @"c.""CreatorId""" : @"c.""DelegateId""")
                .FilterA(@"c.""DelegateId""", filter.@delegate, x => x == "manager" ? @"c.""ManagerId""" : @"c.""CreatorId""")
                .FilterA(@"c.""FirstLevel""", filter.firstActivity)
                .FilterA(@"c.""FirstLevel""", filter.firstActivityTime)
                .FilterA(@"c.""LastLevel""", filter.lastActivity)
                .FilterA(@"c.""LastLevel""", filter.lastActivityTime)
                .FilterA(@"c.""TypeHash""", filter.typeHash)
                .FilterA(@"c.""CodeHash""", filter.codeHash)
                .Take(pagination, x => x switch
                {
                    "balance" => (@"c.""Balance""", @"c.""Balance"""),
                    "firstActivity" => (@"c.""FirstLevel""", @"c.""FirstLevel"""),
                    "lastActivity" => (@"c.""LastLevel""", @"c.""LastLevel"""),
                    "numTransactions" => (@"c.""TransactionsCount""", @"c.""TransactionsCount"""),
                    _ => (@"c.""Id""", @"c.""Id""")
                }, @"c.""Id""");

            using var db = GetConnection();
            return await db.QueryAsync(sql.Query, sql.Params);
        }

        public async Task<Contract> GetContract(string address, bool legacy)
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
                Id = contract.Id,
                Alias = contract.Alias,
                Address = contract.Address,
                Kind = ContractKinds.ToString(contract.Kind),
                Tzips = ContractTags.ToList((Data.Models.ContractTags)contract.Tags),
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
                ActiveTokensCount = contract.ActiveTokensCount,
                TokenBalancesCount = contract.TokenBalancesCount,
                TokenTransfersCount = contract.TokenTransfersCount,
                TicketsCount = contract.TicketsCount,
                ActiveTicketsCount = contract.ActiveTicketsCount,
                TicketBalancesCount = contract.TicketBalancesCount,
                TicketTransfersCount = contract.TicketTransfersCount,
                NumDelegations = contract.DelegationsCount,
                NumOriginations = contract.OriginationsCount,
                NumReveals = contract.RevealsCount,
                NumMigrations = contract.MigrationsCount,
                NumTransactions = contract.TransactionsCount,
                TransferTicketCount = contract.TransferTicketCount,
                IncreasePaidStorageCount = contract.IncreasePaidStorageCount,
                TokensCount = contract.TokensCount,
                EventsCount = contract.EventsCount,
                TypeHash = contract.TypeHash,
                CodeHash = contract.CodeHash,
                Metadata = legacy ? contract.Profile : contract.Metadata,
                Extras = legacy ? null : contract.Extras
            };
        }

        public async Task<int> GetContractsCount(ContractFilter filter)
        {
            var sql = new SqlBuilder($@"
                SELECT COUNT(*) FROM ""Accounts"" as c")
                .FilterA(@"c.""Type""", 2)
                .FilterA(@"c.""Id""", filter.id)
                .FilterA(@"c.""Address""", filter.address)
                .FilterA(@"c.""Kind""", filter.kind)
                .FilterA(@"c.""Tags""", filter.tzips)
                .FilterA(@"c.""Balance""", filter.balance)
                .FilterA(@"c.""CreatorId""", filter.creator, x => x == "manager" ? @"c.""ManagerId""" : @"c.""DelegateId""")
                .FilterA(@"c.""ManagerId""", filter.manager, x => x == "creator" ? @"c.""CreatorId""" : @"c.""DelegateId""")
                .FilterA(@"c.""DelegateId""", filter.@delegate, x => x == "manager" ? @"c.""ManagerId""" : @"c.""CreatorId""")
                .FilterA(@"c.""FirstLevel""", filter.firstActivity)
                .FilterA(@"c.""FirstLevel""", filter.firstActivityTime)
                .FilterA(@"c.""LastLevel""", filter.lastActivity)
                .FilterA(@"c.""LastLevel""", filter.lastActivityTime)
                .FilterA(@"c.""TypeHash""", filter.typeHash)
                .FilterA(@"c.""CodeHash""", filter.codeHash);

            using var db = GetConnection();
            return await db.QueryFirstAsync<int>(sql.Query, sql.Params);
        }

        public async Task<IEnumerable<Contract>> GetContracts(bool includeStorage, ContractFilter filter, Pagination pagination)
        {
            var rows = await QueryContractsAsync(includeStorage, filter, pagination);
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
                    Id = row.Id,
                    Alias = row.Alias,
                    Address = row.Address,
                    Kind = ContractKinds.ToString(row.Kind),
                    Tzips = ContractTags.ToList((Data.Models.ContractTags)row.Tags),
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
                    ActiveTokensCount = row.ActiveTokensCount,
                    TokenBalancesCount = row.TokenBalancesCount,
                    TokenTransfersCount = row.TokenTransfersCount,
                    TicketsCount = row.TicketsCount,
                    ActiveTicketsCount = row.ActiveTicketsCount,
                    TicketBalancesCount = row.TicketBalancesCount,
                    TicketTransfersCount = row.TicketTransfersCount,
                    NumDelegations = row.DelegationsCount,
                    NumOriginations = row.OriginationsCount,
                    NumReveals = row.RevealsCount,
                    NumMigrations = row.MigrationsCount,
                    NumTransactions = row.TransactionsCount,
                    TransferTicketCount = row.TransferTicketCount,
                    IncreasePaidStorageCount = row.IncreasePaidStorageCount,
                    TokensCount = row.TokensCount,
                    EventsCount = row.EventsCount,
                    TypeHash = row.TypeHash,
                    CodeHash = row.CodeHash,
                    Storage = row.Kind == 0 ? $"\"{manager.Address}\"" : (RawJson)row.Storage
                };
            });
        }

        public async Task<object[][]> GetContracts(bool includeStorage, ContractFilter filter, Pagination pagination, List<SelectionField> fields)
        {
            var rows = await QueryContractsAsync(includeStorage, filter, pagination, fields);

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
                    case "type":
                        foreach (var row in rows)
                            result[j++][i] = AccountTypes.ToString(row.Type);
                        break;
                    case "address":
                        foreach (var row in rows)
                            result[j++][i] = row.Address;
                        break;
                    case "kind":
                        foreach (var row in rows)
                            result[j++][i] = ContractKinds.ToString(row.Kind);
                        break;
                    case "tzips":
                        foreach (var row in rows)
                            result[j++][i] = ContractTags.ToList((Data.Models.ContractTags)row.Tags);
                        break;
                    case "alias":
                        foreach (var row in rows)
                            result[j++][i] = row.Alias;
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
                    case "creator.alias":
                        foreach (var row in rows)
                            result[j++][i] = row.CreatorId == null ? null : Accounts.Get((int)row.CreatorId).Alias;
                        break;
                    case "creator.address":
                        foreach (var row in rows)
                            result[j++][i] = row.CreatorId == null ? null : Accounts.Get((int)row.CreatorId).Address;
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
                    case "manager.alias":
                        foreach (var row in rows)
                            result[j++][i] = row.ManagerId == null ? null : Accounts.Get((int)row.ManagerId).Alias;
                        break;
                    case "manager.address":
                        foreach (var row in rows)
                            result[j++][i] = row.ManagerId == null ? null : Accounts.Get((int)row.ManagerId).Address;
                        break;
                    case "manager.publicKey":
                        foreach (var row in rows)
                            result[j++][i] = row.ManagerId == null ? null : ((RawUser)Accounts.Get((int)row.ManagerId)).PublicKey;
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
                    case "delegate.alias":
                        foreach (var row in rows)
                            result[j++][i] = row.DelegateId == null ? null : Accounts.Get((int)row.DelegateId).Alias;
                        break;
                    case "delegate.address":
                        foreach (var row in rows)
                            result[j++][i] = row.DelegateId == null ? null : Accounts.Get((int)row.DelegateId).Address;
                        break;
                    case "delegate.active":
                        foreach (var row in rows)
                            result[j++][i] = row.DelegateId == null ? null : Accounts.Get((int)row.DelegateId).Staked;
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
                    case "activeTokensCount":
                        foreach (var row in rows)
                            result[j++][i] = row.ActiveTokensCount;
                        break;
                    case "tokensCount":
                        foreach (var row in rows)
                            result[j++][i] = row.TokensCount;
                        break;
                    case "tokenBalancesCount":
                        foreach (var row in rows)
                            result[j++][i] = row.TokenBalancesCount;
                        break;
                    case "tokenTransfersCount":
                        foreach (var row in rows)
                            result[j++][i] = row.TokenTransfersCount;
                        break;
                    case "ticketsCount":
                        foreach (var row in rows)
                            result[j++][i] = row.TicketsCount;
                        break;
                    case "activeTicketsCount":
                        foreach (var row in rows)
                            result[j++][i] = row.ActiveTicketsCount;
                        break;
                    case "ticketBalancesCount":
                        foreach (var row in rows)
                            result[j++][i] = row.TicketBalancesCount;
                        break;
                    case "ticketTransfersCount":
                        foreach (var row in rows)
                            result[j++][i] = row.TicketTransfersCount;
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
                    case "transferTicketCount":
                        foreach (var row in rows)
                            result[j++][i] = row.TransferTicketCount;
                        break;
                    case "increasePaidStorageCount":
                        foreach (var row in rows)
                            result[j++][i] = row.IncreasePaidStorageCount;
                        break;
                    case "eventsCount":
                        foreach (var row in rows)
                            result[j++][i] = row.EventsCount;
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
                                result[j++][i] = $"\"{Accounts.Get((int)row.ManagerId).Address}\"";
                            }
                            else
                            {
                                result[j++][i] = (RawJson)row.Storage;
                            }
                        }
                        break;
                    default:
                        if (fields[i].Full.StartsWith("storage."))
                            foreach (var row in rows)
                                result[j++][i] = (RawJson)((row as IDictionary<string, object>)[fields[i].Column] as string);
                        break;
                }
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

            var code = new MichelineArray();
            code.Add(Micheline.FromBytes(row.ParameterSchema));
            code.Add(Micheline.FromBytes(row.StorageSchema));
            if (row.Views != null)
                code.AddRange(((byte[][])row.Views).Select(x => Micheline.FromBytes(x)));
            code.Add(Micheline.FromBytes(row.CodeSchema));

            return code.ToBytes();
        }

        public async Task<byte[]> GetByteCode(string address, int level)
        {
            var rawAccount = await Accounts.GetAsync(address);
            if (rawAccount is not RawContract contract) return null;

            if (level < contract.FirstLevel)
                return null;

            if (contract.Kind == 0)
                return level < 655_360 && State.Current.Chain == "mainnet"
                    ? null
                    : Data.Models.Script.ManagerTzBytes;

            if (contract.MigrationsCount == 0 || level >= contract.LastLevel)
                return await GetByteCode(address);

            using var db = GetConnection();
            var row = await db.QueryFirstOrDefaultAsync($@"
                SELECT *
                FROM ""Scripts""
                WHERE ""ContractId"" = {contract.Id}
                AND ""Level"" <= {level}
                ORDER BY ""Level"" DESC, ""Id"" DESC
                LIMIT 1");
            if (row == null) return null;

            var code = new MichelineArray();
            code.Add(Micheline.FromBytes(row.ParameterSchema));
            code.Add(Micheline.FromBytes(row.StorageSchema));
            if (row.Views != null)
                code.AddRange(((byte[][])row.Views).Select(x => Micheline.FromBytes(x)));
            code.Add(Micheline.FromBytes(row.CodeSchema));

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

            var code = new MichelineArray();
            code.Add(Micheline.FromBytes(row.ParameterSchema));
            code.Add(Micheline.FromBytes(row.StorageSchema));
            if (row.Views != null)
                code.AddRange(((byte[][])row.Views).Select(x => Micheline.FromBytes(x)));
            code.Add(Micheline.FromBytes(row.CodeSchema));

            return code;
        }

        public async Task<IMicheline> GetMichelineCode(string address, int level)
        {
            var rawAccount = await Accounts.GetAsync(address);
            if (rawAccount is not RawContract contract) return null;

            if (level < contract.FirstLevel)
                return null;

            if (contract.Kind == 0)
                return level < 655_360 && State.Current.Chain == "mainnet"
                    ? null
                    : Micheline.FromBytes(Data.Models.Script.ManagerTzBytes);

            if (contract.MigrationsCount == 0 || level >= contract.LastLevel)
                return await GetMichelineCode(address);

            using var db = GetConnection();
            var row = await db.QueryFirstOrDefaultAsync($@"
                SELECT *
                FROM ""Scripts""
                WHERE ""ContractId"" = {contract.Id}
                AND ""Level"" <= {level}
                ORDER BY ""Level"" DESC, ""Id"" DESC
                LIMIT 1");
            if (row == null) return null;

            var code = new MichelineArray();
            code.Add(Micheline.FromBytes(row.ParameterSchema));
            code.Add(Micheline.FromBytes(row.StorageSchema));
            if (row.Views != null)
                code.AddRange(((byte[][])row.Views).Select(x => Micheline.FromBytes(x)));
            code.Add(Micheline.FromBytes(row.CodeSchema));

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

            var code = new MichelineArray();
            code.Add(Micheline.FromBytes(row.ParameterSchema));
            code.Add(Micheline.FromBytes(row.StorageSchema));
            if (row.Views != null)
                code.AddRange(((byte[][])row.Views).Select(x => Micheline.FromBytes(x)));
            code.Add(Micheline.FromBytes(row.CodeSchema));

            return code.ToMichelson();
        }

        public async Task<string> GetMichelsonCode(string address, int level)
        {
            var rawAccount = await Accounts.GetAsync(address);
            if (rawAccount is not RawContract contract) return null;

            if (level < contract.FirstLevel)
                return null;

            if (contract.Kind == 0)
                return level < 655_360 && State.Current.Chain == "mainnet"
                    ? null
                    : Micheline.FromBytes(Data.Models.Script.ManagerTzBytes).ToMichelson();

            if (contract.MigrationsCount == 0 || level >= contract.LastLevel)
                return await GetMichelsonCode(address);

            using var db = GetConnection();
            var row = await db.QueryFirstOrDefaultAsync($@"
                SELECT *
                FROM ""Scripts""
                WHERE ""ContractId"" = {contract.Id}
                AND ""Level"" <= {level}
                ORDER BY ""Level"" DESC, ""Id"" DESC
                LIMIT 1");
            if (row == null) return null;

            var code = new MichelineArray();
            code.Add(Micheline.FromBytes(row.ParameterSchema));
            code.Add(Micheline.FromBytes(row.StorageSchema));
            if (row.Views != null)
                code.AddRange(((byte[][])row.Views).Select(x => Micheline.FromBytes(x)));
            code.Add(Micheline.FromBytes(row.CodeSchema));

            return code.ToMichelson();
        }

        public async Task<ContractInterface> GetContractInterface(string address)
        {
            var rawAccount = await Accounts.GetAsync(address);
            if (rawAccount is not RawContract contract) return null;

            ContractParameter param;
            ContractStorage storage;
            IMicheline code;

            if (contract.Kind == 0)
            {
                param = Data.Models.Script.ManagerTz.Parameter;
                storage = Data.Models.Script.ManagerTz.Storage;
                code = new MichelineArray();
            }
            else
            {
                using var db = GetConnection();
                var script = await db.QueryFirstOrDefaultAsync($@"
                    SELECT      ""StorageSchema"", ""ParameterSchema"", ""CodeSchema""
                    FROM        ""Scripts""
                    WHERE       ""ContractId"" = {contract.Id} AND ""Current"" = true
                    LIMIT       1"
                );
                if (script == null) return null;
                param = new ContractParameter(Micheline.FromBytes(script.ParameterSchema));
                storage = new ContractStorage(Micheline.FromBytes(script.StorageSchema));
                code = Micheline.FromBytes(script.CodeSchema);
            }

            var rawStorage = await GetRawStorageValue(address);
            var storageTreeView = storage.Schema.ToTreeView(rawStorage);

            return new ContractInterface
            {
                StorageSchema = storage.GetJsonSchema(),
                Entrypoints = param.Entrypoints
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
                    .ToList(),
                Events = code
                    .FindPrimNodes(x => x.Prim == PrimType.EMIT && x.Annots?.Count == 1 && x.Args?.Count == 1)
                    .Select(x => new EventInterface()
                    {
                        Tag = x.Annots[0].Value,
                        EventSchema = Schema.Create(x.Args[0] as MichelinePrim).GetJsonSchema()
                    })
                    .ToList()
            };
        }
        
        public async Task<MichelinePrim> GetViewMicheline(string address, string name)
        {
            var rawAccount = await Accounts.GetAsync(address);
            if (rawAccount is not RawContract contract) return null;
            
            using var db = GetConnection();
            var row = await db.QueryFirstOrDefaultAsync("""
                SELECT "Views"
                FROM "Scripts"
                WHERE "ContractId" = @id
                AND "Current" = true
                """, new { id = contract.Id });
            
            return row?.Views == null ? null : ((byte[][])row.Views)
                .Select(x => Micheline.FromBytes(x) as MichelinePrim)
                .FirstOrDefault(x => (x.Args[0] as MichelineString)?.Value == name);
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
                JsonParameters = json ? (RawJson)ep.Humanize() : null,
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
                        JsonParameters = json ? (RawJson)x.Value.Humanize() : null,
                        MichelineParameters = mich,
                        MichelsonParameters = michelson ? (mich ?? x.Value.ToMicheline()).ToMichelson() : null,
                        Unused = all && !param.IsEntrypointUseful(x.Key)
                    };
                });
        }

        public async Task<ContractView> GetView(string address, string name, bool json, bool micheline, bool michelson)
        {
            var rawAccount = await Accounts.GetAsync(address);
            if (rawAccount is not RawContract contract || contract.Kind == 0) return null;

            using var db = GetConnection();
            var row = await db.QueryFirstOrDefaultAsync($@"SELECT ""Views"" FROM ""Scripts"" WHERE ""ContractId"" = {contract.Id} AND ""Current"" = true");
            if (row == null || row.Views == null) return null;

            var view = ((byte[][])row.Views)
                .Select(x => Micheline.FromBytes(x) as MichelinePrim)
                .Where(x => (x.Args[0] as MichelineString).Value == name)
                .FirstOrDefault();
            if (view == null) return null;

            return new ContractView
            {
                Name = (view.Args[0] as MichelineString).Value,
                JsonParameterType = json ? Schema.Create(view.Args[1] as MichelinePrim).Humanize() : null,
                JsonReturnType = json ? Schema.Create(view.Args[2] as MichelinePrim).Humanize() : null,
                MichelineParameterType = micheline ? view.Args[1] : null,
                MichelineReturnType = micheline ? view.Args[2] : null,
                MichelsonParameterType = michelson ? view.Args[1].ToMichelson() : null,
                MichelsonReturnType = michelson ? view.Args[2].ToMichelson() : null
            };
        }

        public async Task<IEnumerable<ContractView>> GetViews(string address, bool json, bool micheline, bool michelson)
        {
            var rawAccount = await Accounts.GetAsync(address);
            if (rawAccount is not RawContract contract || contract.Kind == 0) return Enumerable.Empty<ContractView>();

            using var db = GetConnection();
            var row = await db.QueryFirstOrDefaultAsync($@"SELECT ""Views"" FROM ""Scripts"" WHERE ""ContractId"" = {contract.Id} AND ""Current"" = true");
            if (row == null || row.Views == null) return Enumerable.Empty<ContractView>();

            return ((byte[][])row.Views)
                .Select(x => Micheline.FromBytes(x) as MichelinePrim)
                .Select(view => new ContractView
                {
                    Name = (view.Args[0] as MichelineString).Value,
                    JsonParameterType = json ? Schema.Create(view.Args[1] as MichelinePrim).Humanize() : null,
                    JsonReturnType = json ? Schema.Create(view.Args[2] as MichelinePrim).Humanize() : null,
                    MichelineParameterType = micheline ? view.Args[1] : null,
                    MichelineReturnType = micheline ? view.Args[2] : null,
                    MichelsonParameterType = michelson ? view.Args[1].ToMichelson() : null,
                    MichelsonReturnType = michelson ? view.Args[2].ToMichelson() : null
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

            if (contract.Kind == 0)
            {
                if (level < 655_360 && State.Current.Chain == "mainnet")
                    return null;
                var manager = await Accounts.GetAsync((int)contract.ManagerId);
                return path?.Length > 0 ? null : $"\"{manager.Address}\"";
            }

            if (level >= contract.LastLevel)
                return await GetStorageValue(address, path);

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

            if (contract.Kind == 0)
            {
                if (level < 655_360 && State.Current.Chain == "mainnet")
                    return null;
                var manager = await Accounts.GetAsync((int)contract.ManagerId);
                return new MichelineString(manager.Address);
            }

            if (level >= contract.LastLevel)
                return await GetRawStorageValue(address);

            using var db = GetConnection();
            var row = await db.QueryFirstOrDefaultAsync($@"
                SELECT   ""RawValue""
                FROM     ""Storages""
                WHERE    ""ContractId"" = {contract.Id}
                AND      ""Level"" <= {level}
                ORDER BY ""Level"" DESC, ""Id"" DESC
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

            if (contract.Kind == 0)
                return level < 655_360 && State.Current.Chain == "mainnet"
                    ? null
                    : Data.Models.Script.ManagerTz.Storage.Humanize();

            if (contract.MigrationsCount == 0 || level >= contract.LastLevel)
                return await GetStorageSchema(address);

            using var db = GetConnection();
            var row = await db.QueryFirstOrDefaultAsync($@"
                SELECT      ""StorageSchema""
                FROM        ""Scripts""
                WHERE       ""ContractId"" = {contract.Id}
                AND         ""Level"" <= {level}
                ORDER BY    ""Level"" DESC, ""Id"" DESC
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

            if (contract.Kind == 0)
                return level < 655_360 && State.Current.Chain == "mainnet"
                    ? null
                    : Data.Models.Script.ManagerTz.Storage.Schema.ToMicheline();

            if (contract.MigrationsCount == 0 || level >= contract.LastLevel)
                return await GetRawStorageSchema(address);

            using var db = GetConnection();
            var row = await db.QueryFirstOrDefaultAsync($@"
                SELECT      ""StorageSchema""
                FROM        ""Scripts""
                WHERE       ""ContractId"" = {contract.Id}
                AND         ""Level"" <= {level}
                ORDER BY    ""Level"" DESC, ""Id"" DESC
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
    }
}
