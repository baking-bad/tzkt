using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Netezos.Encoding;
using Tzkt.Api.Models;
using Tzkt.Api.Services.Cache;
using Tzkt.Data;

namespace Tzkt.Api.Repositories
{
    public partial class OperationRepository : DbConnection
    {
        public async Task<bool?> GetOriginationStatus(string hash)
        {
            using var db = GetConnection();
            return await GetStatus(db, nameof(TzktContext.OriginationOps), hash);
        }

        public async Task<int> GetOriginationsCount(
            Int32Parameter level,
            DateTimeParameter timestamp)
        {
            var sql = new SqlBuilder(@"SELECT COUNT(*) FROM ""OriginationOps""")
                .Filter("Level", level)
                .Filter("Timestamp", timestamp);

            using var db = GetConnection();
            return await db.QueryFirstAsync<int>(sql.Query, sql.Params);
        }

        public async Task<IEnumerable<OriginationOperation>> GetOriginations(string hash, MichelineFormat format, Symbols quote)
        {
            var sql = $@"
                SELECT      o.*, b.""Hash"", sc.""ParameterSchema"", sc.""StorageSchema"", sc.""CodeSchema"", sc.""Views""
                FROM        ""OriginationOps"" as o
                INNER JOIN  ""Blocks"" as b 
                        ON  b.""Level"" = o.""Level""
                LEFT  JOIN  ""Scripts"" as sc
                        ON  sc.""Id"" = o.""ScriptId""
                WHERE       o.""OpHash"" = @hash::character(51)
                ORDER BY    o.""Id""";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { hash });

            #region include storage
            var storages = await AccountRepository.GetStorages(db,
                rows.Where(x => x.StorageId != null)
                    .Select(x => (int)x.StorageId)
                    .Distinct()
                    .ToList(),
                format);
            #endregion

            #region include diffs
            var diffs = await BigMapsRepository.GetOriginationDiffs(db,
                rows.Where(x => x.BigMapUpdates != null)
                    .Select(x => (long)x.Id)
                    .ToList(),
                format);
            #endregion

            return rows.Select(row =>
            {
                var contract = row.ContractId == null ? null
                    : (RawContract)Accounts.Get((int)row.ContractId);

                MichelineArray code = null;
                if (row.ParameterSchema != null)
                {
                    code = new();
                    code.Add(Micheline.FromBytes(row.ParameterSchema));
                    code.Add(Micheline.FromBytes(row.StorageSchema));
                    if (row.Views != null)
                        code.AddRange(((byte[][])row.Views).Select(x => Micheline.FromBytes(x)));
                    code.Add(Micheline.FromBytes(row.CodeSchema));
                }

                return new OriginationOperation
                {
                    Id = row.Id,
                    Level = row.Level,
                    Block = row.Hash,
                    Timestamp = row.Timestamp,
                    Hash = hash,
                    Initiator = row.InitiatorId != null ? Accounts.GetAlias(row.InitiatorId) : null,
                    Sender = Accounts.GetAlias(row.SenderId),
                    SenderCodeHash = row.SenderCodeHash,
                    Counter = row.Counter,
                    Nonce = row.Nonce,
                    GasLimit = row.GasLimit,
                    GasUsed = row.GasUsed,
                    StorageLimit = row.StorageLimit,
                    StorageUsed = row.StorageUsed,
                    BakerFee = row.BakerFee,
                    StorageFee = row.StorageFee ?? 0,
                    AllocationFee = row.AllocationFee ?? 0,
                    ContractDelegate = row.DelegateId != null ? Accounts.GetAlias(row.DelegateId) : null,
                    ContractBalance = row.Balance,
                    Code = (int)format % 2 == 0 ? code : code.ToJson(),
                    Storage = row.StorageId == null ? null : storages?[row.StorageId],
                    Diffs = diffs?.GetValueOrDefault((long)row.Id),
                    Status = OpStatuses.ToString(row.Status),
                    OriginatedContract = contract == null ? null :
                        new OriginatedContract
                        {
                            Alias = contract.Alias,
                            Address = contract.Address,
                            Kind = contract.KindString,
                            TypeHash = contract.TypeHash,
                            CodeHash = contract.CodeHash,
                            Tzips = ContractTags.ToList((Data.Models.ContractTags)contract.Tags)
                        },
                    ContractManager = row.ManagerId != null ? Accounts.GetAlias(row.ManagerId) : null,
                    Errors = row.Errors != null ? OperationErrorSerializer.Deserialize(row.Errors) : null,
                    TokenTransfersCount = row.TokenTransfers,
                    Quote = Quotes.Get(quote, row.Level)
                };
            });
        }

        public async Task<IEnumerable<OriginationOperation>> GetOriginations(string hash, int counter, MichelineFormat format, Symbols quote)
        {
            var sql = $@"
                SELECT      o.*, b.""Hash"", sc.""ParameterSchema"", sc.""StorageSchema"", sc.""CodeSchema"", sc.""Views""
                FROM        ""OriginationOps"" as o
                INNER JOIN  ""Blocks"" as b 
                        ON  b.""Level"" = o.""Level""
                LEFT  JOIN  ""Scripts"" as sc
                        ON  sc.""Id"" = o.""ScriptId""
                WHERE       o.""OpHash"" = @hash::character(51) AND o.""Counter"" = @counter
                ORDER BY    o.""Id""";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { hash, counter });

            #region include storage
            var storages = await AccountRepository.GetStorages(db,
                rows.Where(x => x.StorageId != null)
                    .Select(x => (int)x.StorageId)
                    .Distinct()
                    .ToList(),
                format);
            #endregion

            #region include diffs
            var diffs = await BigMapsRepository.GetOriginationDiffs(db,
                rows.Where(x => x.BigMapUpdates != null)
                    .Select(x => (long)x.Id)
                    .ToList(),
                format);
            #endregion

            return rows.Select(row =>
            {
                var contract = row.ContractId == null ? null
                    : (RawContract)Accounts.Get((int)row.ContractId);

                MichelineArray code = null;
                if (row.ParameterSchema != null)
                {
                    code = new();
                    code.Add(Micheline.FromBytes(row.ParameterSchema));
                    code.Add(Micheline.FromBytes(row.StorageSchema));
                    if (row.Views != null)
                        code.AddRange(((byte[][])row.Views).Select(x => Micheline.FromBytes(x)));
                    code.Add(Micheline.FromBytes(row.CodeSchema));
                }

                return new OriginationOperation
                {
                    Id = row.Id,
                    Level = row.Level,
                    Block = row.Hash,
                    Timestamp = row.Timestamp,
                    Hash = hash,
                    Initiator = row.InitiatorId != null ? Accounts.GetAlias(row.InitiatorId) : null,
                    Sender = Accounts.GetAlias(row.SenderId),
                    SenderCodeHash = row.SenderCodeHash,
                    Counter = counter,
                    Nonce = row.Nonce,
                    GasLimit = row.GasLimit,
                    GasUsed = row.GasUsed,
                    StorageLimit = row.StorageLimit,
                    StorageUsed = row.StorageUsed,
                    BakerFee = row.BakerFee,
                    StorageFee = row.StorageFee ?? 0,
                    AllocationFee = row.AllocationFee ?? 0,
                    ContractDelegate = row.DelegateId != null ? Accounts.GetAlias(row.DelegateId) : null,
                    ContractBalance = row.Balance,
                    Code = (int)format % 2 == 0 ? code : code.ToJson(),
                    Storage = row.StorageId == null ? null : storages?[row.StorageId],
                    Diffs = diffs?.GetValueOrDefault((long)row.Id),
                    Status = OpStatuses.ToString(row.Status),
                    OriginatedContract = contract == null ? null :
                        new OriginatedContract
                        {
                            Alias = contract.Alias,
                            Address = contract.Address,
                            Kind = contract.KindString,
                            TypeHash = contract.TypeHash,
                            CodeHash = contract.CodeHash,
                            Tzips = ContractTags.ToList((Data.Models.ContractTags)contract.Tags)
                        },
                    ContractManager = row.ManagerId != null ? Accounts.GetAlias(row.ManagerId) : null,
                    Errors = row.Errors != null ? OperationErrorSerializer.Deserialize(row.Errors) : null,
                    TokenTransfersCount = row.TokenTransfers,
                    Quote = Quotes.Get(quote, row.Level)
                };
            });
        }

        public async Task<IEnumerable<OriginationOperation>> GetOriginations(string hash, int counter, int nonce, MichelineFormat format, Symbols quote)
        {
            var sql = $@"
                SELECT      o.*, b.""Hash"", sc.""ParameterSchema"", sc.""StorageSchema"", sc.""CodeSchema"", sc.""Views""
                FROM        ""OriginationOps"" as o
                INNER JOIN  ""Blocks"" as b 
                        ON  b.""Level"" = o.""Level""
                LEFT  JOIN  ""Scripts"" as sc
                        ON  sc.""Id"" = o.""ScriptId""
                WHERE       o.""OpHash"" = @hash::character(51) AND o.""Counter"" = @counter AND o.""Nonce"" = @nonce
                LIMIT       1";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { hash, counter, nonce });

            #region include storage
            var storages = await AccountRepository.GetStorages(db,
                rows.Where(x => x.StorageId != null)
                    .Select(x => (int)x.StorageId)
                    .Distinct()
                    .ToList(),
                format);
            #endregion

            #region include diffs
            var diffs = await BigMapsRepository.GetOriginationDiffs(db,
                rows.Where(x => x.BigMapUpdates != null)
                    .Select(x => (long)x.Id)
                    .ToList(),
                format);
            #endregion

            return rows.Select(row =>
            {
                var contract = row.ContractId == null ? null
                    : (RawContract)Accounts.Get((int)row.ContractId);

                MichelineArray code = null;
                if (row.ParameterSchema != null)
                {
                    code = new();
                    code.Add(Micheline.FromBytes(row.ParameterSchema));
                    code.Add(Micheline.FromBytes(row.StorageSchema));
                    if (row.Views != null)
                        code.AddRange(((byte[][])row.Views).Select(x => Micheline.FromBytes(x)));
                    code.Add(Micheline.FromBytes(row.CodeSchema));
                }

                return new OriginationOperation
                {
                    Id = row.Id,
                    Level = row.Level,
                    Block = row.Hash,
                    Timestamp = row.Timestamp,
                    Hash = hash,
                    Initiator = row.InitiatorId != null ? Accounts.GetAlias(row.InitiatorId) : null,
                    Sender = Accounts.GetAlias(row.SenderId),
                    SenderCodeHash = row.SenderCodeHash,
                    Counter = counter,
                    Nonce = nonce,
                    GasLimit = row.GasLimit,
                    GasUsed = row.GasUsed,
                    StorageLimit = row.StorageLimit,
                    StorageUsed = row.StorageUsed,
                    BakerFee = row.BakerFee,
                    StorageFee = row.StorageFee ?? 0,
                    AllocationFee = row.AllocationFee ?? 0,
                    ContractDelegate = row.DelegateId != null ? Accounts.GetAlias(row.DelegateId) : null,
                    ContractBalance = row.Balance,
                    Code = (int)format % 2 == 0 ? code : code.ToJson(),
                    Storage = row.StorageId == null ? null : storages?[row.StorageId],
                    Diffs = diffs?.GetValueOrDefault((long)row.Id),
                    Status = OpStatuses.ToString(row.Status),
                    OriginatedContract = contract == null ? null :
                        new OriginatedContract
                        {
                            Alias = contract.Alias,
                            Address = contract.Address,
                            Kind = contract.KindString,
                            TypeHash = contract.TypeHash,
                            CodeHash = contract.CodeHash,
                            Tzips = ContractTags.ToList((Data.Models.ContractTags)contract.Tags)
                        },
                    ContractManager = row.ManagerId != null ? Accounts.GetAlias(row.ManagerId) : null,
                    Errors = row.Errors != null ? OperationErrorSerializer.Deserialize(row.Errors) : null,
                    TokenTransfersCount = row.TokenTransfers,
                    Quote = Quotes.Get(quote, row.Level)
                };
            });
        }

        public async Task<IEnumerable<OriginationOperation>> GetOriginations(Block block, Symbols quote)
        {
            var sql = @"
                SELECT    *
                FROM      ""OriginationOps""
                WHERE     ""Level"" = @level
                ORDER BY  ""Id""";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { level = block.Level });

            return rows.Select(row =>
            {
                var contract = row.ContractId == null ? null
                    : (RawContract)Accounts.Get((int)row.ContractId);

                return new OriginationOperation
                {
                    Id = row.Id,
                    Level = block.Level,
                    Block = block.Hash,
                    Timestamp = row.Timestamp,
                    Hash = row.OpHash,
                    Initiator = row.InitiatorId != null ? Accounts.GetAlias(row.InitiatorId) : null,
                    Sender = Accounts.GetAlias(row.SenderId),
                    SenderCodeHash = row.SenderCodeHash,
                    Counter = row.Counter,
                    Nonce = row.Nonce,
                    GasLimit = row.GasLimit,
                    GasUsed = row.GasUsed,
                    StorageLimit = row.StorageLimit,
                    StorageUsed = row.StorageUsed,
                    BakerFee = row.BakerFee,
                    StorageFee = row.StorageFee ?? 0,
                    AllocationFee = row.AllocationFee ?? 0,
                    ContractDelegate = row.DelegateId != null ? Accounts.GetAlias(row.DelegateId) : null,
                    ContractBalance = row.Balance,
                    Status = OpStatuses.ToString(row.Status),
                    OriginatedContract = contract == null ? null :
                        new OriginatedContract
                        {
                            Alias = contract.Alias,
                            Address = contract.Address,
                            Kind = contract.KindString,
                            TypeHash = contract.TypeHash,
                            CodeHash = contract.CodeHash,
                            Tzips = ContractTags.ToList((Data.Models.ContractTags)contract.Tags)
                        },
                    ContractManager = row.ManagerId != null ? Accounts.GetAlias(row.ManagerId) : null,
                    Errors = row.Errors != null ? OperationErrorSerializer.Deserialize(row.Errors) : null,
                    TokenTransfersCount = row.TokenTransfers,
                    Quote = Quotes.Get(quote, block.Level)
                };
            });
        }

        public async Task<IEnumerable<OriginationOperation>> GetOriginations(
            AnyOfParameter anyof,
            AccountParameter initiator,
            AccountParameter sender,
            AccountParameter contractManager,
            AccountParameter contractDelegate,
            AccountParameter originatedContract,
            Int64Parameter id,
            Int32Parameter typeHash,
            Int32Parameter codeHash,
            Int32Parameter level,
            DateTimeParameter timestamp,
            Int32Parameter anyCodeHash,
            Int32Parameter senderCodeHash,
            OperationStatusParameter status,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            MichelineFormat format,
            Symbols quote,
            bool includeStorage = false,
            bool includeBigmaps = false)
        {
            var sql = new SqlBuilder($@"
                SELECT      o.*, b.""Hash""
                FROM        ""OriginationOps"" AS o
                INNER JOIN  ""Blocks"" as b
                        ON  b.""Level"" = o.""Level""
                {(typeHash != null || codeHash != null ? @"LEFT JOIN ""Accounts"" as c ON c.""Id"" = o.""ContractId""" : "")}")
                .Filter(anyof, x => x switch
                {
                    "initiator" => "InitiatorId",
                    "sender" => "SenderId",
                    "contractManager" => "ManagerId",
                    "contractDelegate" => "DelegateId",
                    _ => "ContractId"
                })
                .Filter("InitiatorId", initiator, x => x == "contractManager" ? "ManagerId" : "DelegateId")
                .Filter("SenderId", sender, x => x == "contractManager" ? "ManagerId" : "DelegateId")
                .Filter("ManagerId", contractManager, x => x == "initiator" ? "InitiatorId" : x == "sender" ? "SenderId" : "DelegateId")
                .Filter("DelegateId", contractDelegate, x => x == "initiator" ? "InitiatorId" : x == "sender" ? "SenderId" : "ManagerId")
                .Filter("ContractId", originatedContract)
                .FilterA(@"o.""Id""", id)
                .FilterA(@"c.""TypeHash""", typeHash)
                .FilterA(@"o.""ContractCodeHash""", codeHash)
                .FilterA(@"o.""Level""", level)
                .FilterA(@"o.""Timestamp""", timestamp)
                .FilterA(@"o.""SenderCodeHash""", senderCodeHash)
                .Filter("Status", status)
                .FilterOrA(new[] { @"o.""SenderCodeHash""", @"o.""ContractCodeHash""" }, anyCodeHash)
                .Take(sort, offset, limit, x => x switch
                {
                    "level" => ("Id", "Level"),
                    "gasUsed" => ("GasUsed", "GasUsed"),
                    "storageUsed" => ("StorageUsed", "StorageUsed"),
                    "bakerFee" => ("BakerFee", "BakerFee"),
                    "storageFee" => ("StorageFee", "StorageFee"),
                    "allocationFee" => ("AllocationFee", "AllocationFee"),
                    "contractBalance" => ("Balance", "Balance"),
                    _ => ("Id", "Id")
                }, "o");

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            #region include storage
            var storages = includeStorage
                ? await AccountRepository.GetStorages(db,
                    rows.Where(x => x.StorageId != null)
                        .Select(x => (int)x.StorageId)
                        .Distinct()
                        .ToList(),
                    format)
                : null;
            #endregion

            #region include diffs
            var diffs = includeBigmaps
                ? await BigMapsRepository.GetOriginationDiffs(db,
                    rows.Where(x => x.BigMapUpdates != null)
                        .Select(x => (long)x.Id)
                        .ToList(),
                    format)
                : null;
            #endregion

            return rows.Select(row =>
            {
                var contract = row.ContractId == null ? null
                    : (RawContract)Accounts.Get((int)row.ContractId);

                return new OriginationOperation
                {
                    Id = row.Id,
                    Level = row.Level,
                    Block = row.Hash,
                    Timestamp = row.Timestamp,
                    Hash = row.OpHash,
                    Initiator = row.InitiatorId != null ? Accounts.GetAlias(row.InitiatorId) : null,
                    Sender = Accounts.GetAlias(row.SenderId),
                    SenderCodeHash = row.SenderCodeHash,
                    Counter = row.Counter,
                    Nonce = row.Nonce,
                    GasLimit = row.GasLimit,
                    GasUsed = row.GasUsed,
                    StorageLimit = row.StorageLimit,
                    StorageUsed = row.StorageUsed,
                    BakerFee = row.BakerFee,
                    StorageFee = row.StorageFee ?? 0,
                    AllocationFee = row.AllocationFee ?? 0,
                    ContractDelegate = row.DelegateId != null ? Accounts.GetAlias(row.DelegateId) : null,
                    ContractBalance = row.Balance,
                    Status = OpStatuses.ToString(row.Status),
                    OriginatedContract = contract == null ? null : new OriginatedContract
                    {
                        Alias = contract.Alias,
                        Address = contract.Address,
                        Kind = contract.KindString,
                        TypeHash = contract.TypeHash,
                        CodeHash = contract.CodeHash,
                        Tzips = ContractTags.ToList((Data.Models.ContractTags)contract.Tags)
                    },
                    Storage = row.StorageId == null ? null : storages?[row.StorageId],
                    Diffs = diffs?.GetValueOrDefault((long)row.Id),
                    ContractManager = row.ManagerId != null ? Accounts.GetAlias(row.ManagerId) : null,
                    Errors = row.Errors != null ? OperationErrorSerializer.Deserialize(row.Errors) : null,
                    TokenTransfersCount = row.TokenTransfers,
                    Quote = Quotes.Get(quote, row.Level)
                };
            });
        }

        public async Task<object[][]> GetOriginations(
            AnyOfParameter anyof,
            AccountParameter initiator,
            AccountParameter sender,
            AccountParameter contractManager,
            AccountParameter contractDelegate,
            AccountParameter originatedContract,
            Int64Parameter id,
            Int32Parameter typeHash,
            Int32Parameter codeHash,
            Int32Parameter level,
            DateTimeParameter timestamp,
            Int32Parameter anyCodeHash,
            Int32Parameter senderCodeHash,
            OperationStatusParameter status,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            string[] fields,
            MichelineFormat format,
            Symbols quote)
        {
            var columns = new HashSet<string>(fields.Length);
            var joins = new HashSet<string>(1);

            foreach (var field in fields)
            {
                switch (field)
                {
                    case "id": columns.Add(@"o.""Id"""); break;
                    case "level": columns.Add(@"o.""Level"""); break;
                    case "timestamp": columns.Add(@"o.""Timestamp"""); break;
                    case "hash": columns.Add(@"o.""OpHash"""); break;
                    case "initiator": columns.Add(@"o.""InitiatorId"""); break;
                    case "sender": columns.Add(@"o.""SenderId"""); break;
                    case "senderCodeHash": columns.Add(@"o.""SenderCodeHash"""); break;
                    case "counter": columns.Add(@"o.""Counter"""); break;
                    case "nonce": columns.Add(@"o.""Nonce"""); break;
                    case "gasLimit": columns.Add(@"o.""GasLimit"""); break;
                    case "gasUsed": columns.Add(@"o.""GasUsed"""); break;
                    case "storageLimit": columns.Add(@"o.""StorageLimit"""); break;
                    case "storageUsed": columns.Add(@"o.""StorageUsed"""); break;
                    case "bakerFee": columns.Add(@"o.""BakerFee"""); break;
                    case "storageFee": columns.Add(@"o.""StorageFee"""); break;
                    case "allocationFee": columns.Add(@"o.""AllocationFee"""); break;
                    case "contractDelegate": columns.Add(@"o.""DelegateId"""); break;
                    case "contractBalance": columns.Add(@"o.""Balance"""); break;
                    case "status": columns.Add(@"o.""Status"""); break;
                    case "originatedContract": columns.Add(@"o.""ContractId"""); break;
                    case "contractManager": columns.Add(@"o.""ManagerId"""); break;
                    case "errors": columns.Add(@"o.""Errors"""); break;
                    case "block":
                        columns.Add(@"b.""Hash""");
                        joins.Add(@"INNER JOIN ""Blocks"" as b ON b.""Level"" = o.""Level""");
                        break;
                    case "code":
                        columns.Add(@"sc.""ParameterSchema""");
                        columns.Add(@"sc.""StorageSchema""");
                        columns.Add(@"sc.""CodeSchema""");
                        columns.Add(@"sc.""Views""");
                        joins.Add(@"LEFT JOIN ""Scripts"" as sc ON sc.""Id"" = o.""ScriptId""");
                        break;
                    case "storage": columns.Add(@"o.""StorageId"""); break;
                    case "diffs":
                        columns.Add(@"o.""Id""");
                        columns.Add(@"o.""BigMapUpdates""");
                        break;
                    case "tokenTransfersCount": columns.Add(@"o.""TokenTransfers"""); break;
                    case "quote": columns.Add(@"o.""Level"""); break;
                }
            }

            if (columns.Count == 0)
                return Array.Empty<object[]>();

            if (typeHash != null || codeHash != null)
                joins.Add(@"LEFT JOIN ""Accounts"" as c ON c.""Id"" = o.""ContractId""");

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""OriginationOps"" as o {string.Join(' ', joins)}")
                .Filter(anyof, x => x switch
                {
                    "initiator" => "InitiatorId",
                    "sender" => "SenderId",
                    "contractManager" => "ManagerId",
                    "contractDelegate" => "DelegateId",
                    _ => "ContractId"
                })
                .Filter("InitiatorId", initiator, x => x == "contractManager" ? "ManagerId" : "DelegateId")
                .Filter("SenderId", sender, x => x == "contractManager" ? "ManagerId" : "DelegateId")
                .Filter("ManagerId", contractManager, x => x == "initiator" ? "InitiatorId" : x == "sender" ? "SenderId" : "DelegateId")
                .Filter("DelegateId", contractDelegate, x => x == "initiator" ? "InitiatorId" : x == "sender" ? "SenderId" : "ManagerId")
                .Filter("ContractId", originatedContract)
                .FilterA(@"o.""Id""", id)
                .FilterA(@"c.""TypeHash""", typeHash)
                .FilterA(@"o.""ContractCodeHash""", codeHash)
                .FilterA(@"o.""Level""", level)
                .FilterA(@"o.""Timestamp""", timestamp)
                .FilterA(@"o.""SenderCodeHash""", senderCodeHash)
                .Filter("Status", status)
                .FilterOrA(new[] { @"o.""SenderCodeHash""", @"o.""ContractCodeHash""" }, anyCodeHash)
                .Take(sort, offset, limit, x => x switch
                {
                    "level" => ("Id", "Level"),
                    "gasUsed" => ("GasUsed", "GasUsed"),
                    "storageUsed" => ("StorageUsed", "StorageUsed"),
                    "bakerFee" => ("BakerFee", "BakerFee"),
                    "storageFee" => ("StorageFee", "StorageFee"),
                    "allocationFee" => ("AllocationFee", "AllocationFee"),
                    "contractBalance" => ("Balance", "Balance"),
                    _ => ("Id", "Id")
                }, "o");

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            var result = new object[rows.Count()][];
            for (int i = 0; i < result.Length; i++)
                result[i] = new object[fields.Length];

            for (int i = 0, j = 0; i < fields.Length; j = 0, i++)
            {
                switch (fields[i])
                {
                    case "id":
                        foreach (var row in rows)
                            result[j++][i] = row.Id;
                        break;
                    case "level":
                        foreach (var row in rows)
                            result[j++][i] = row.Level;
                        break;
                    case "block":
                        foreach (var row in rows)
                            result[j++][i] = row.Hash;
                        break;
                    case "timestamp":
                        foreach (var row in rows)
                            result[j++][i] = row.Timestamp;
                        break;
                    case "hash":
                        foreach (var row in rows)
                            result[j++][i] = row.OpHash;
                        break;
                    case "initiator":
                        foreach (var row in rows)
                            result[j++][i] = row.InitiatorId != null ? await Accounts.GetAliasAsync(row.InitiatorId) : null;
                        break;
                    case "sender":
                        foreach (var row in rows)
                            result[j++][i] = await Accounts.GetAliasAsync(row.SenderId);
                        break;
                    case "senderCodeHash":
                        foreach (var row in rows)
                            result[j++][i] = row.SenderCodeHash;
                        break; 
                    case "counter":
                        foreach (var row in rows)
                            result[j++][i] = row.Counter;
                        break;
                    case "nonce":
                        foreach (var row in rows)
                            result[j++][i] = row.Nonce;
                        break;
                    case "gasLimit":
                        foreach (var row in rows)
                            result[j++][i] = row.GasLimit;
                        break;
                    case "gasUsed":
                        foreach (var row in rows)
                            result[j++][i] = row.GasUsed;
                        break;
                    case "storageLimit":
                        foreach (var row in rows)
                            result[j++][i] = row.StorageLimit;
                        break;
                    case "storageUsed":
                        foreach (var row in rows)
                            result[j++][i] = row.StorageUsed;
                        break;
                    case "bakerFee":
                        foreach (var row in rows)
                            result[j++][i] = row.BakerFee;
                        break;
                    case "storageFee":
                        foreach (var row in rows)
                            result[j++][i] = row.StorageFee ?? 0;
                        break;
                    case "allocationFee":
                        foreach (var row in rows)
                            result[j++][i] = row.AllocationFee ?? 0;
                        break;
                    case "contractDelegate":
                        foreach (var row in rows)
                            result[j++][i] = row.DelegateId != null ? await Accounts.GetAliasAsync(row.DelegateId) : null;
                        break;
                    case "contractBalance":
                        foreach (var row in rows)
                            result[j++][i] = row.Balance;
                        break;
                    case "code":
                        foreach (var row in rows)
                        {
                            MichelineArray code = null;
                            if (row.ParameterSchema != null)
                            {
                                code = new();
                                code.Add(Micheline.FromBytes(row.ParameterSchema));
                                code.Add(Micheline.FromBytes(row.StorageSchema));
                                if (row.Views != null)
                                    code.AddRange(((byte[][])row.Views).Select(x => Micheline.FromBytes(x)));
                                code.Add(Micheline.FromBytes(row.CodeSchema));
                            }
                            result[j++][i] = (int)format % 2 == 0 ? code : code.ToJson();
                        }
                        break;
                    case "storage":
                        var storages = await AccountRepository.GetStorages(db,
                            rows.Where(x => x.StorageId != null)
                                .Select(x => (int)x.StorageId)
                                .Distinct()
                                .ToList(),
                            format);
                        if (storages != null)
                            foreach (var row in rows)
                                result[j++][i] = row.StorageId == null ? null : storages[row.StorageId];
                        break;
                    case "diffs":
                        var diffs = await BigMapsRepository.GetOriginationDiffs(db,
                            rows.Where(x => x.BigMapUpdates != null)
                                .Select(x => (long)x.Id)
                                .ToList(),
                            format);
                        if (diffs != null)
                            foreach (var row in rows)
                                result[j++][i] = diffs.GetValueOrDefault((long)row.Id);
                        break;
                    case "status":
                        foreach (var row in rows)
                            result[j++][i] = OpStatuses.ToString(row.Status);
                        break;
                    case "originatedContract":
                        foreach (var row in rows)
                        {
                            var contract = row.ContractId == null ? null : (RawContract)Accounts.Get((int)row.ContractId);
                            result[j++][i] = contract == null ? null : new OriginatedContract
                            {
                                Alias = contract.Alias,
                                Address = contract.Address,
                                Kind = contract.KindString,
                                TypeHash = contract.TypeHash,
                                CodeHash = contract.CodeHash,
                                Tzips = ContractTags.ToList((Data.Models.ContractTags)contract.Tags)
                            };
                        }
                        break;
                    case "contractManager":
                        foreach (var row in rows)
                            result[j++][i] = row.ManagerId != null ? await Accounts.GetAliasAsync(row.ManagerId) : null;
                        break;
                    case "errors":
                        foreach (var row in rows)
                            result[j++][i] = row.Errors != null ? OperationErrorSerializer.Deserialize(row.Errors) : null;
                        break;
                    case "tokenTransfersCount":
                        foreach (var row in rows)
                            result[j++][i] = row.TokenTransfers;
                        break;
                    case "quote":
                        foreach (var row in rows)
                            result[j++][i] = Quotes.Get(quote, row.Level);
                        break;
                }
            }

            return result;
        }

        public async Task<object[]> GetOriginations(
            AnyOfParameter anyof,
            AccountParameter initiator,
            AccountParameter sender,
            AccountParameter contractManager,
            AccountParameter contractDelegate,
            AccountParameter originatedContract,
            Int64Parameter id,
            Int32Parameter typeHash,
            Int32Parameter codeHash,
            Int32Parameter level,
            DateTimeParameter timestamp,
            Int32Parameter anyCodeHash,
            Int32Parameter senderCodeHash,
            OperationStatusParameter status,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            string field,
            MichelineFormat format,
            Symbols quote)
        {
            var columns = new HashSet<string>(1);
            var joins = new HashSet<string>(1);
            
            switch (field)
            {
                case "id": columns.Add(@"o.""Id"""); break;
                case "level": columns.Add(@"o.""Level"""); break;
                case "timestamp": columns.Add(@"o.""Timestamp"""); break;
                case "hash": columns.Add(@"o.""OpHash"""); break;
                case "initiator": columns.Add(@"o.""InitiatorId"""); break;
                case "sender": columns.Add(@"o.""SenderId"""); break;
                case "senderCodeHash": columns.Add(@"o.""SenderCodeHash"""); break;
                case "counter": columns.Add(@"o.""Counter"""); break;
                case "nonce": columns.Add(@"o.""Nonce"""); break;
                case "gasLimit": columns.Add(@"o.""GasLimit"""); break;
                case "gasUsed": columns.Add(@"o.""GasUsed"""); break;
                case "storageLimit": columns.Add(@"o.""StorageLimit"""); break;
                case "storageUsed": columns.Add(@"o.""StorageUsed"""); break;
                case "bakerFee": columns.Add(@"o.""BakerFee"""); break;
                case "storageFee": columns.Add(@"o.""StorageFee"""); break;
                case "allocationFee": columns.Add(@"o.""AllocationFee"""); break;
                case "contractDelegate": columns.Add(@"o.""DelegateId"""); break;
                case "contractBalance": columns.Add(@"o.""Balance"""); break;
                case "status": columns.Add(@"o.""Status"""); break;
                case "originatedContract": columns.Add(@"o.""ContractId"""); break;
                case "contractManager": columns.Add(@"o.""ManagerId"""); break;
                case "errors": columns.Add(@"o.""Errors"""); break;
                case "block":
                    columns.Add(@"b.""Hash""");
                    joins.Add(@"INNER JOIN ""Blocks"" as b ON b.""Level"" = o.""Level""");
                    break;
                case "code":
                    columns.Add(@"sc.""ParameterSchema""");
                    columns.Add(@"sc.""StorageSchema""");
                    columns.Add(@"sc.""CodeSchema""");
                    columns.Add(@"sc.""Views""");
                    joins.Add(@"LEFT JOIN ""Scripts"" as sc ON sc.""Id"" = o.""ScriptId""");
                    break;
                case "storage": columns.Add(@"o.""StorageId"""); break;
                case "diffs":
                    columns.Add(@"o.""Id""");
                    columns.Add(@"o.""BigMapUpdates""");
                    break;
                case "tokenTransfersCount": columns.Add(@"o.""TokenTransfers"""); break;
                case "quote": columns.Add(@"o.""Level"""); break;
            }

            if (columns.Count == 0)
                return Array.Empty<object>();

            if (typeHash != null || codeHash != null)
                joins.Add(@"LEFT JOIN ""Accounts"" as c ON c.""Id"" = o.""ContractId""");

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""OriginationOps"" as o {string.Join(' ', joins)}")
                .Filter(anyof, x => x switch
                {
                    "initiator" => "InitiatorId",
                    "sender" => "SenderId",
                    "contractManager" => "ManagerId",
                    "contractDelegate" => "DelegateId",
                    _ => "ContractId"
                })
                .Filter("InitiatorId", initiator, x => x == "contractManager" ? "ManagerId" : "DelegateId")
                .Filter("SenderId", sender, x => x == "contractManager" ? "ManagerId" : "DelegateId")
                .Filter("ManagerId", contractManager, x => x == "initiator" ? "InitiatorId" : x == "sender" ? "SenderId" : "DelegateId")
                .Filter("DelegateId", contractDelegate, x => x == "initiator" ? "InitiatorId" : x == "sender" ? "SenderId" : "ManagerId")
                .Filter("ContractId", originatedContract)
                .FilterA(@"o.""Id""", id)
                .FilterA(@"c.""TypeHash""", typeHash)
                .FilterA(@"o.""ContractCodeHash""", codeHash)
                .FilterA(@"o.""Level""", level)
                .FilterA(@"o.""Timestamp""", timestamp)
                .FilterA(@"o.""SenderCodeHash""", senderCodeHash)
                .Filter("Status", status)
                .FilterOrA(new[] { @"o.""SenderCodeHash""", @"o.""ContractCodeHash""" }, anyCodeHash)
                .Take(sort, offset, limit, x => x switch
                {
                    "level" => ("Id", "Level"),
                    "gasUsed" => ("GasUsed", "GasUsed"),
                    "storageUsed" => ("StorageUsed", "StorageUsed"),
                    "bakerFee" => ("BakerFee", "BakerFee"),
                    "storageFee" => ("StorageFee", "StorageFee"),
                    "allocationFee" => ("AllocationFee", "AllocationFee"),
                    "contractBalance" => ("Balance", "Balance"),
                    _ => ("Id", "Id")
                }, "o");

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            //TODO: optimize memory allocation
            var result = new object[rows.Count()];
            var j = 0;

            switch (field)
            {
                case "id":
                    foreach (var row in rows)
                        result[j++] = row.Id;
                    break;
                case "level":
                    foreach (var row in rows)
                        result[j++] = row.Level;
                    break;
                case "block":
                    foreach (var row in rows)
                        result[j++] = row.Hash;
                    break;
                case "timestamp":
                    foreach (var row in rows)
                        result[j++] = row.Timestamp;
                    break;
                case "hash":
                    foreach (var row in rows)
                        result[j++] = row.OpHash;
                    break;
                case "initiator":
                    foreach (var row in rows)
                        result[j++] = row.InitiatorId != null ? await Accounts.GetAliasAsync(row.InitiatorId) : null;
                    break;
                case "sender":
                    foreach (var row in rows)
                        result[j++] = await Accounts.GetAliasAsync(row.SenderId);
                    break;
                case "senderCodeHash":
                    foreach (var row in rows)
                        result[j++] = row.SenderCodeHash;
                    break;
                case "counter":
                    foreach (var row in rows)
                        result[j++] = row.Counter;
                    break;
                case "nonce":
                    foreach (var row in rows)
                        result[j++] = row.Nonce;
                    break;
                case "gasLimit":
                    foreach (var row in rows)
                        result[j++] = row.GasLimit;
                    break;
                case "gasUsed":
                    foreach (var row in rows)
                        result[j++] = row.GasUsed;
                    break;
                case "storageLimit":
                    foreach (var row in rows)
                        result[j++] = row.StorageLimit;
                    break;
                case "storageUsed":
                    foreach (var row in rows)
                        result[j++] = row.StorageUsed;
                    break;
                case "bakerFee":
                    foreach (var row in rows)
                        result[j++] = row.BakerFee;
                    break;
                case "storageFee":
                    foreach (var row in rows)
                        result[j++] = row.StorageFee ?? 0;
                    break;
                case "allocationFee":
                    foreach (var row in rows)
                        result[j++] = row.AllocationFee ?? 0;
                    break;
                case "contractDelegate":
                    foreach (var row in rows)
                        result[j++] = row.DelegateId != null ? await Accounts.GetAliasAsync(row.DelegateId) : null;
                    break;
                case "contractBalance":
                    foreach (var row in rows)
                        result[j++] = row.Balance;
                    break;
                case "code":
                    foreach (var row in rows)
                    {
                        MichelineArray code = null;
                        if (row.ParameterSchema != null)
                        {
                            code = new();
                            code.Add(Micheline.FromBytes(row.ParameterSchema));
                            code.Add(Micheline.FromBytes(row.StorageSchema));
                            if (row.Views != null)
                                code.AddRange(((byte[][])row.Views).Select(x => Micheline.FromBytes(x)));
                            code.Add(Micheline.FromBytes(row.CodeSchema));
                        }
                        result[j++] = (int)format % 2 == 0 ? code : code.ToJson();
                    }
                    break;
                case "storage":
                    var storages = await AccountRepository.GetStorages(db,
                        rows.Where(x => x.StorageId != null)
                            .Select(x => (int)x.StorageId)
                            .Distinct()
                            .ToList(),
                        format);
                    if (storages != null)
                        foreach (var row in rows)
                            result[j++] = row.StorageId == null ? null : storages[row.StorageId];
                    break;
                case "diffs":
                    var diffs = await BigMapsRepository.GetOriginationDiffs(db,
                        rows.Where(x => x.BigMapUpdates != null)
                            .Select(x => (long)x.Id)
                            .ToList(),
                        format);
                    if (diffs != null)
                        foreach (var row in rows)
                            result[j++] = diffs.GetValueOrDefault((long)row.Id);
                    break;
                case "status":
                    foreach (var row in rows)
                        result[j++] = OpStatuses.ToString(row.Status);
                    break;
                case "originatedContract":
                    foreach (var row in rows)
                    {
                        var contract = row.ContractId == null ? null : (RawContract)Accounts.Get((int)row.ContractId);
                        result[j++] = contract == null ? null : new OriginatedContract
                        {
                            Alias = contract.Alias,
                            Address = contract.Address,
                            Kind = contract.KindString,
                            TypeHash = contract.TypeHash,
                            CodeHash = contract.CodeHash,
                            Tzips = ContractTags.ToList((Data.Models.ContractTags)contract.Tags)
                        };
                    }
                    break;
                case "contractManager":
                    foreach (var row in rows)
                        result[j++] = row.ManagerId != null ? await Accounts.GetAliasAsync(row.ManagerId) : null;
                    break;
                case "errors":
                    foreach (var row in rows)
                        result[j++] = row.Errors != null ? OperationErrorSerializer.Deserialize(row.Errors) : null;
                    break;
                case "tokenTransfersCount":
                    foreach (var row in rows)
                        result[j++] = row.TokenTransfers;
                    break;
                case "quote":
                    foreach (var row in rows)
                        result[j++] = Quotes.Get(quote, row.Level);
                    break;
            }

            return result;
        }
    }
}
