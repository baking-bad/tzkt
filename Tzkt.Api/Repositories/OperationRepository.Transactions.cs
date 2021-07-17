using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Netezos.Encoding;
using Tzkt.Api.Models;

namespace Tzkt.Api.Repositories
{
    public partial class OperationRepository : DbConnection
    {
        public async Task<int> GetTransactionsCount(
            Int32Parameter level,
            DateTimeParameter timestamp,
            OperationStatusParameter status)
        {
            var sql = new SqlBuilder(@"SELECT COUNT(*) FROM ""TransactionOps""")
                .Filter("Level", level)
                .Filter("Timestamp", timestamp)
                .Filter("Status", status);

            using var db = GetConnection();
            return await db.QueryFirstAsync<int>(sql.Query, sql.Params);
        }

        public async Task<IEnumerable<TransactionOperation>> GetTransactions(string hash, MichelineFormat format, Symbols quote)
        {
            var sql = $@"
                SELECT      o.*, b.""Hash""
                FROM        ""TransactionOps"" as o
                INNER JOIN  ""Blocks"" as b 
                        ON  b.""Level"" = o.""Level""
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
            var diffs = await BigMapsRepository.GetTransactionDiffs(db,
                rows.Where(x => x.BigMapUpdates != null)
                    .Select(x => (int)x.Id)
                    .ToList(),
                format);
            #endregion

            return rows.Select(row => new TransactionOperation
            {
                Id = row.Id,
                Level = row.Level,
                Block = row.Hash,
                Timestamp = row.Timestamp,
                Hash = hash,
                Initiator = row.InitiatorId != null ? Accounts.GetAlias(row.InitiatorId) : null,
                Sender = Accounts.GetAlias(row.SenderId),
                Counter = row.Counter,
                Nonce = row.Nonce,
                GasLimit = row.GasLimit,
                GasUsed = row.GasUsed,
                StorageLimit = row.StorageLimit,
                StorageUsed = row.StorageUsed,
                BakerFee = row.BakerFee,
                StorageFee = row.StorageFee ?? 0,
                AllocationFee = row.AllocationFee ?? 0,
                Target = row.TargetId != null ? Accounts.GetAlias(row.TargetId) : null,
                Amount = row.Amount,
                Parameter = row.Entrypoint == null ? null : new TxParameter
                {
                    Entrypoint = row.Entrypoint,
                    Value = format switch
                    {
                        MichelineFormat.Json => (RawJson)row.JsonParameters,
                        MichelineFormat.JsonString => row.JsonParameters,
                        MichelineFormat.Raw => row.RawParameters == null ? null : (RawJson)Micheline.ToJson(row.RawParameters),
                        MichelineFormat.RawString => row.RawParameters == null ? null : Micheline.ToJson(row.RawParameters),
                        _ => throw new Exception("Invalid MichelineFormat value")
                    }
                },
                Storage = row.StorageId == null ? null : storages?[row.StorageId],
                Diffs = diffs?.GetValueOrDefault((int)row.Id),
                Status = OpStatuses.ToString(row.Status),
                Errors = row.Errors != null ? OperationErrorSerializer.Deserialize(row.Errors) : null,
                HasInternals = row.InternalOperations > 0,
                Quote = Quotes.Get(quote, row.Level),
                Parameters = row.RawParameters == null ? null : $"{{\"entrypoint\":\"{row.Entrypoint}\",\"value\":{Micheline.ToJson(row.RawParameters)}}}"
            });
        }

        public async Task<IEnumerable<TransactionOperation>> GetTransactions(string hash, int counter, MichelineFormat format, Symbols quote)
        {
            var sql = $@"
                SELECT      o.*, b.""Hash""
                FROM        ""TransactionOps"" as o
                INNER JOIN  ""Blocks"" as b 
                        ON  b.""Level"" = o.""Level""
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
            var diffs = await BigMapsRepository.GetTransactionDiffs(db,
                rows.Where(x => x.BigMapUpdates != null)
                    .Select(x => (int)x.Id)
                    .ToList(),
                format);
            #endregion

            return rows.Select(row => new TransactionOperation
            {
                Id = row.Id,
                Level = row.Level,
                Block = row.Hash,
                Timestamp = row.Timestamp,
                Hash = hash,
                Initiator = row.InitiatorId != null ? Accounts.GetAlias(row.InitiatorId) : null,
                Sender = Accounts.GetAlias(row.SenderId),
                Counter = counter,
                Nonce = row.Nonce,
                GasLimit = row.GasLimit,
                GasUsed = row.GasUsed,
                StorageLimit = row.StorageLimit,
                StorageUsed = row.StorageUsed,
                BakerFee = row.BakerFee,
                StorageFee = row.StorageFee ?? 0,
                AllocationFee = row.AllocationFee ?? 0,
                Target = row.TargetId != null ? Accounts.GetAlias(row.TargetId) : null,
                Amount = row.Amount,
                Parameter = row.Entrypoint == null ? null : new TxParameter
                {
                    Entrypoint = row.Entrypoint,
                    Value = format switch
                    {
                        MichelineFormat.Json => (RawJson)row.JsonParameters,
                        MichelineFormat.JsonString => row.JsonParameters,
                        MichelineFormat.Raw => row.RawParameters == null ? null : (RawJson)Micheline.ToJson(row.RawParameters),
                        MichelineFormat.RawString => row.RawParameters == null ? null : Micheline.ToJson(row.RawParameters),
                        _ => throw new Exception("Invalid MichelineFormat value")
                    }
                },
                Storage = row.StorageId == null ? null : storages?[row.StorageId],
                Diffs = diffs?.GetValueOrDefault((int)row.Id),
                Status = OpStatuses.ToString(row.Status),
                Errors = row.Errors != null ? OperationErrorSerializer.Deserialize(row.Errors) : null,
                HasInternals = row.InternalOperations > 0,
                Quote = Quotes.Get(quote, row.Level),
                Parameters = row.RawParameters == null ? null : $"{{\"entrypoint\":\"{row.Entrypoint}\",\"value\":{Micheline.ToJson(row.RawParameters)}}}"
            });
        }

        public async Task<IEnumerable<TransactionOperation>> GetTransactions(string hash, int counter, int nonce, MichelineFormat format, Symbols quote)
        {
            var sql = $@"
                SELECT      o.*, b.""Hash""
                FROM        ""TransactionOps"" as o
                INNER JOIN  ""Blocks"" as b 
                        ON  b.""Level"" = o.""Level""
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
            var diffs = await BigMapsRepository.GetTransactionDiffs(db,
                rows.Where(x => x.BigMapUpdates != null)
                    .Select(x => (int)x.Id)
                    .ToList(),
                format);
            #endregion

            return rows.Select(row => new TransactionOperation
            {
                Id = row.Id,
                Level = row.Level,
                Block = row.Hash,
                Timestamp = row.Timestamp,
                Hash = hash,
                Initiator = row.InitiatorId != null ? Accounts.GetAlias(row.InitiatorId) : null,
                Sender = Accounts.GetAlias(row.SenderId),
                Counter = counter,
                Nonce = nonce,
                GasLimit = row.GasLimit,
                GasUsed = row.GasUsed,
                StorageLimit = row.StorageLimit,
                StorageUsed = row.StorageUsed,
                BakerFee = row.BakerFee,
                StorageFee = row.StorageFee ?? 0,
                AllocationFee = row.AllocationFee ?? 0,
                Target = row.TargetId != null ? Accounts.GetAlias(row.TargetId) : null,
                Amount = row.Amount,
                Parameter = row.Entrypoint == null ? null : new TxParameter
                {
                    Entrypoint = row.Entrypoint,
                    Value = format switch
                    {
                        MichelineFormat.Json => (RawJson)row.JsonParameters,
                        MichelineFormat.JsonString => row.JsonParameters,
                        MichelineFormat.Raw => row.RawParameters == null ? null : (RawJson)Micheline.ToJson(row.RawParameters),
                        MichelineFormat.RawString => row.RawParameters == null ? null : Micheline.ToJson(row.RawParameters),
                        _ => throw new Exception("Invalid MichelineFormat value")
                    }
                },
                Storage = row.StorageId == null ? null : storages?[row.StorageId],
                Diffs = diffs?.GetValueOrDefault((int)row.Id),
                Status = OpStatuses.ToString(row.Status),
                Errors = row.Errors != null ? OperationErrorSerializer.Deserialize(row.Errors) : null,
                HasInternals = row.InternalOperations > 0,
                Quote = Quotes.Get(quote, row.Level),
                Parameters = row.RawParameters == null ? null : $"{{\"entrypoint\":\"{row.Entrypoint}\",\"value\":{Micheline.ToJson(row.RawParameters)}}}"
            });
        }

        public async Task<IEnumerable<TransactionOperation>> GetTransactions(Block block, MichelineFormat format, Symbols quote)
        {
            var sql = @"
                SELECT    *
                FROM      ""TransactionOps""
                WHERE     ""Level"" = @level
                ORDER BY  ""Id""";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { level = block.Level });

            return rows.Select(row => new TransactionOperation
            {
                Id = row.Id,
                Level = block.Level,
                Block = block.Hash,
                Timestamp = row.Timestamp,
                Hash = row.OpHash,
                Initiator = row.InitiatorId != null ? Accounts.GetAlias(row.InitiatorId) : null,
                Sender = Accounts.GetAlias(row.SenderId),
                Counter = row.Counter,
                Nonce = row.Nonce,
                GasLimit = row.GasLimit,
                GasUsed = row.GasUsed,
                StorageLimit = row.StorageLimit,
                StorageUsed = row.StorageUsed,
                BakerFee = row.BakerFee,
                StorageFee = row.StorageFee ?? 0,
                AllocationFee = row.AllocationFee ?? 0,
                Target = row.TargetId != null ? Accounts.GetAlias(row.TargetId) : null,
                Amount = row.Amount,
                Parameter = row.Entrypoint == null ? null : new TxParameter
                {
                    Entrypoint = row.Entrypoint,
                    Value = format switch
                    {
                        MichelineFormat.Json => (RawJson)row.JsonParameters,
                        MichelineFormat.JsonString => row.JsonParameters,
                        MichelineFormat.Raw => row.RawParameters == null ? null : (RawJson)Micheline.ToJson(row.RawParameters),
                        MichelineFormat.RawString => row.RawParameters == null ? null : Micheline.ToJson(row.RawParameters),
                        _ => throw new Exception("Invalid MichelineFormat value")
                    }
                },
                Status = OpStatuses.ToString(row.Status),
                Errors = row.Errors != null ? OperationErrorSerializer.Deserialize(row.Errors) : null,
                HasInternals = row.InternalOperations > 0,
                Quote = Quotes.Get(quote, block.Level),
                Parameters = row.RawParameters == null ? null : $"{{\"entrypoint\":\"{row.Entrypoint}\",\"value\":{Micheline.ToJson(row.RawParameters)}}}"
            });
        }

        public async Task<IEnumerable<TransactionOperation>> GetTransactions(
            AnyOfParameter anyof,
            AccountParameter initiator,
            AccountParameter sender,
            AccountParameter target,
            Int64Parameter amount,
            Int32Parameter level,
            DateTimeParameter timestamp,
            StringParameter entrypoint,
            JsonParameter parameter,
            StringParameter parameters,
            BoolParameter hasInternals,
            OperationStatusParameter status,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            MichelineFormat format,
            Symbols quote,
            bool includeStorage = false,
            bool includeBigmaps = false)
        {
            #region backward compatibility
            // TODO: remove it asap
            var realSort = sort;
            var realOffset = offset;
            var realLimit = limit;
            if (parameters != null)
            {
                realSort = null;
                realOffset = null;
                realLimit = 1_000_000;
            }
            #endregion

            var sql = new SqlBuilder(@"
                SELECT      o.*, b.""Hash""
                FROM        ""TransactionOps"" AS o
                INNER JOIN  ""Blocks"" as b
                        ON  b.""Level"" = o.""Level""")
                .Filter(anyof, x => x == "sender" ? "SenderId" : x == "target" ? "TargetId" : "InitiatorId")
                .Filter("InitiatorId", initiator, x => "TargetId")
                .Filter("SenderId", sender, x => "TargetId")
                .Filter("TargetId", target, x => x == "sender" ? "SenderId" : "InitiatorId")
                .Filter("Amount", amount)
                .Filter("Entrypoint", entrypoint)
                .Filter("JsonParameters", parameter)
                .Filter("InternalOperations", hasInternals?.Eq == true
                    ? new Int32NullParameter { Gt = 0 }
                    : hasInternals?.Eq == false
                        ? new Int32NullParameter { Null = true }
                        : null)
                .Filter("Status", status)
                .FilterA(@"o.""Level""", level)
                .FilterA(@"o.""Timestamp""", timestamp)
                .Take(realSort, realOffset, realLimit, x => x switch
                {
                    "level" => ("Id", "Level"),
                    "gasUsed" => ("GasUsed", "GasUsed"),
                    "storageUsed" => ("StorageUsed", "StorageUsed"),
                    "bakerFee" => ("BakerFee", "BakerFee"),
                    "storageFee" => ("StorageFee", "StorageFee"),
                    "allocationFee" => ("AllocationFee", "AllocationFee"),
                    "amount" => ("Amount", "Amount"),
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
                ? await BigMapsRepository.GetTransactionDiffs(db,
                    rows.Where(x => x.BigMapUpdates != null)
                        .Select(x => (int)x.Id)
                        .ToList(),
                    format)
                : null;
            #endregion

            var res = rows.Select(row => new TransactionOperation
            {
                Id = row.Id,
                Level = row.Level,
                Block = row.Hash,
                Timestamp = row.Timestamp,
                Hash = row.OpHash,
                Initiator = row.InitiatorId != null ? Accounts.GetAlias(row.InitiatorId) : null,
                Sender = Accounts.GetAlias(row.SenderId),
                Counter = row.Counter,
                Nonce = row.Nonce,
                GasLimit = row.GasLimit,
                GasUsed = row.GasUsed,
                StorageLimit = row.StorageLimit,
                StorageUsed = row.StorageUsed,
                BakerFee = row.BakerFee,
                StorageFee = row.StorageFee ?? 0,
                AllocationFee = row.AllocationFee ?? 0,
                Target = row.TargetId != null ? Accounts.GetAlias(row.TargetId) : null,
                Amount = row.Amount,
                Parameter = row.Entrypoint == null ? null : new TxParameter
                {
                    Entrypoint = row.Entrypoint,
                    Value = format switch
                    {
                        MichelineFormat.Json => (RawJson)row.JsonParameters,
                        MichelineFormat.JsonString => row.JsonParameters,
                        MichelineFormat.Raw => row.RawParameters == null ? null : (RawJson)Micheline.ToJson(row.RawParameters),
                        MichelineFormat.RawString => row.RawParameters == null ? null : Micheline.ToJson(row.RawParameters),
                        _ => throw new Exception("Invalid MichelineFormat value")
                    }
                },
                Storage = row.StorageId == null ? null : storages?[row.StorageId],
                Diffs = diffs?.GetValueOrDefault((int)row.Id),
                Status = OpStatuses.ToString(row.Status),
                Errors = row.Errors != null ? OperationErrorSerializer.Deserialize(row.Errors) : null,
                HasInternals = row.InternalOperations > 0,
                Quote = Quotes.Get(quote, row.Level),
                Parameters = row.RawParameters == null ? null : $"{{\"entrypoint\":\"{row.Entrypoint}\",\"value\":{Micheline.ToJson(row.RawParameters)}}}"
            });

            #region backward compatibility
            // TODO: remove it asap
            if (parameters != null)
            {
                if (parameters.Eq != null)
                    res = res.Where(x => x.Parameters == parameters.Eq);
                if (parameters.Ne != null)
                    res = res.Where(x => x.Parameters != parameters.Ne);
                if (parameters.In != null)
                    res = res.Where(x => parameters.In.Contains(x.Parameters));
                if (parameters.Ni != null)
                    res = res.Where(x => !parameters.Ni.Contains(x.Parameters));
                if (parameters.Null == true)
                    res = res.Where(x => x.Parameters == null);
                if (parameters.Null == false)
                    res = res.Where(x => x.Parameters != null);
                if (parameters.As != null)
                {
                    var pattern = $"^{parameters.As.Replace("%", ".*").Replace("[", "\\[").Replace("]", "\\]").Replace("{", "\\{").Replace("}", "\\}")}$";
                    res = res.Where(x => x.Parameters != null && System.Text.RegularExpressions.Regex.IsMatch(x.Parameters, pattern));
                }
                if (parameters.Un != null)
                {
                    var pattern = $"^{parameters.Un.Replace("%", ".*").Replace("[", "\\[").Replace("]", "\\]").Replace("{", "\\{").Replace("}", "\\}")}$";
                    res = res.Where(x => x.Parameters != null && !System.Text.RegularExpressions.Regex.IsMatch(x.Parameters, pattern));
                }

                if (sort?.Asc != null)
                {
                    res = sort.Asc switch
                    {
                        "level" => res.OrderBy(x => x.Level),
                        "gasUsed" => res.OrderBy(x => x.GasUsed),
                        "storageUsed" => res.OrderBy(x => x.StorageUsed),
                        "bakerFee" => res.OrderBy(x => x.BakerFee),
                        "storageFee" => res.OrderBy(x => x.StorageFee),
                        "allocationFee" => res.OrderBy(x => x.AllocationFee),
                        "amount" => res.OrderBy(x => x.Amount),
                        _ => res.OrderBy(x => x.Id)
                    };
                }
                else if (sort?.Desc != null)
                {
                    res = sort.Desc switch
                    {
                        "level" => res.OrderByDescending(x => x.Level),
                        "gasUsed" => res.OrderByDescending(x => x.GasUsed),
                        "storageUsed" => res.OrderByDescending(x => x.StorageUsed),
                        "bakerFee" => res.OrderByDescending(x => x.BakerFee),
                        "storageFee" => res.OrderByDescending(x => x.StorageFee),
                        "allocationFee" => res.OrderByDescending(x => x.AllocationFee),
                        "amount" => res.OrderByDescending(x => x.Amount),
                        _ => res.OrderByDescending(x => x.Id)
                    };
                }

                if (offset?.El != null)
                    res = res.Skip((int)offset.El);
                else if (offset?.Pg != null)
                    res = res.Skip((int)offset.Pg * limit);

                return res.Take(limit);
            }
            #endregion

            return res;
        }

        public async Task<object[][]> GetTransactions(
            AnyOfParameter anyof,
            AccountParameter initiator,
            AccountParameter sender,
            AccountParameter target,
            Int64Parameter amount,
            Int32Parameter level,
            DateTimeParameter timestamp,
            StringParameter entrypoint,
            JsonParameter parameter,
            BoolParameter hasInternals,
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
                    case "counter": columns.Add(@"o.""Counter"""); break;
                    case "nonce": columns.Add(@"o.""Nonce"""); break;
                    case "gasLimit": columns.Add(@"o.""GasLimit"""); break;
                    case "gasUsed": columns.Add(@"o.""GasUsed"""); break;
                    case "storageLimit": columns.Add(@"o.""StorageLimit"""); break;
                    case "storageUsed": columns.Add(@"o.""StorageUsed"""); break;
                    case "bakerFee": columns.Add(@"o.""BakerFee"""); break;
                    case "storageFee": columns.Add(@"o.""StorageFee"""); break;
                    case "allocationFee": columns.Add(@"o.""AllocationFee"""); break;
                    case "target": columns.Add(@"o.""TargetId"""); break;
                    case "amount": columns.Add(@"o.""Amount"""); break;
                    case "parameter":
                        columns.Add(@"o.""Entrypoint""");
                        columns.Add(format switch
                        {
                            MichelineFormat.Json => $@"o.""JsonParameters""",
                            MichelineFormat.JsonString => $@"o.""JsonParameters""",
                            MichelineFormat.Raw => $@"o.""RawParameters""",
                            MichelineFormat.RawString => $@"o.""RawParameters""",
                            _ => throw new Exception("Invalid MichelineFormat value")
                        });
                        break;
                    case "storage": columns.Add(@"o.""StorageId"""); break;
                    case "diffs":
                        columns.Add(@"o.""Id""");
                        columns.Add(@"o.""BigMapUpdates""");
                        break;
                    case "status": columns.Add(@"o.""Status"""); break;
                    case "errors": columns.Add(@"o.""Errors"""); break;
                    case "hasInternals": columns.Add(@"o.""InternalOperations"""); break;
                    case "block":
                        columns.Add(@"b.""Hash""");
                        joins.Add(@"INNER JOIN ""Blocks"" as b ON b.""Level"" = o.""Level""");
                        break;
                    case "quote": columns.Add(@"o.""Level"""); break;
                    case "parameters": // backward compatibility
                        columns.Add($@"o.""Entrypoint""");
                        columns.Add($@"o.""RawParameters""");
                        break;
                }
            }

            if (columns.Count == 0)
                return Array.Empty<object[]>();

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""TransactionOps"" as o {string.Join(' ', joins)}")
                .Filter(anyof, x => x == "sender" ? "SenderId" : x == "target" ? "TargetId" : "InitiatorId")
                .Filter("InitiatorId", initiator, x => "TargetId")
                .Filter("SenderId", sender, x => "TargetId")
                .Filter("TargetId", target, x => x == "sender" ? "SenderId" : "InitiatorId")
                .Filter("Amount", amount)
                .Filter("Entrypoint", entrypoint)
                .Filter("JsonParameters", parameter)
                .Filter("InternalOperations", hasInternals?.Eq == true
                    ? new Int32NullParameter { Gt = 0 }
                    : hasInternals?.Eq == false
                        ? new Int32NullParameter { Null = true }
                        : null)
                .Filter("Status", status)
                .FilterA(@"o.""Level""", level)
                .FilterA(@"o.""Timestamp""", timestamp)
                .Take(sort, offset, limit, x => x switch
                {
                    "level" => ("Id", "Level"),
                    "gasUsed" => ("GasUsed", "GasUsed"),
                    "storageUsed" => ("StorageUsed", "StorageUsed"),
                    "bakerFee" => ("BakerFee", "BakerFee"),
                    "storageFee" => ("StorageFee", "StorageFee"),
                    "allocationFee" => ("AllocationFee", "AllocationFee"),
                    "amount" => ("Amount", "Amount"),
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
                    case "target":
                        foreach (var row in rows)
                            result[j++][i] = row.TargetId != null ? await Accounts.GetAliasAsync(row.TargetId) : null;
                        break;
                    case "amount":
                        foreach (var row in rows)
                            result[j++][i] = row.Amount;
                        break;
                    case "parameter":
                        foreach (var row in rows)
                            result[j++][i] = row.Entrypoint == null ? null : new TxParameter
                            {
                                Entrypoint = row.Entrypoint,
                                Value = format switch
                                {
                                    MichelineFormat.Json => (RawJson)row.JsonParameters,
                                    MichelineFormat.JsonString => row.JsonParameters,
                                    MichelineFormat.Raw => row.RawParameters == null ? null : (RawJson)Micheline.ToJson(row.RawParameters),
                                    MichelineFormat.RawString => row.RawParameters == null ? null : Micheline.ToJson(row.RawParameters),
                                    _ => throw new Exception("Invalid MichelineFormat value")
                                }
                            };
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
                        var diffs = await BigMapsRepository.GetTransactionDiffs(db,
                            rows.Where(x => x.BigMapUpdates != null)
                                .Select(x => (int)x.Id)
                                .ToList(),
                            format);
                        if (diffs != null)
                            foreach (var row in rows)
                                result[j++][i] = diffs.GetValueOrDefault((int)row.Id);
                        break;
                    case "status":
                        foreach (var row in rows)
                            result[j++][i] = OpStatuses.ToString(row.Status);
                        break;
                    case "errors":
                        foreach (var row in rows)
                            result[j++][i] = row.Errors != null ? OperationErrorSerializer.Deserialize(row.Errors) : null;
                        break;
                    case "hasInternals":
                        foreach (var row in rows)
                            result[j++][i] = row.InternalOperations > 0;
                        break;
                    case "quote":
                        foreach (var row in rows)
                            result[j++][i] = Quotes.Get(quote, row.Level);
                        break;
                    case "parameters":
                        foreach (var row in rows)
                            result[j++][i] = row.RawParameters == null ? null
                                : $"{{\"entrypoint\":\"{row.Entrypoint}\",\"value\":{Micheline.ToJson(row.RawParameters)}}}";
                        break;
                }
            }

            return result;
        }

        public async Task<object[]> GetTransactions(
            AnyOfParameter anyof,
            AccountParameter initiator,
            AccountParameter sender,
            AccountParameter target,
            Int64Parameter amount,
            Int32Parameter level,
            DateTimeParameter timestamp,
            StringParameter entrypoint,
            JsonParameter parameter,
            BoolParameter hasInternals,
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
                case "counter": columns.Add(@"o.""Counter"""); break;
                case "nonce": columns.Add(@"o.""Nonce"""); break;
                case "gasLimit": columns.Add(@"o.""GasLimit"""); break;
                case "gasUsed": columns.Add(@"o.""GasUsed"""); break;
                case "storageLimit": columns.Add(@"o.""StorageLimit"""); break;
                case "storageUsed": columns.Add(@"o.""StorageUsed"""); break;
                case "bakerFee": columns.Add(@"o.""BakerFee"""); break;
                case "storageFee": columns.Add(@"o.""StorageFee"""); break;
                case "allocationFee": columns.Add(@"o.""AllocationFee"""); break;
                case "target": columns.Add(@"o.""TargetId"""); break;
                case "amount": columns.Add(@"o.""Amount"""); break;
                case "parameter":
                    columns.Add(@"o.""Entrypoint""");
                    columns.Add(format switch
                    {
                        MichelineFormat.Json => $@"o.""JsonParameters""",
                        MichelineFormat.JsonString => $@"o.""JsonParameters""",
                        MichelineFormat.Raw => $@"o.""RawParameters""",
                        MichelineFormat.RawString => $@"o.""RawParameters""",
                        _ => throw new Exception("Invalid MichelineFormat value")
                    });
                    break;
                case "storage": columns.Add(@"o.""StorageId"""); break;
                case "diffs":
                    columns.Add(@"o.""Id""");
                    columns.Add(@"o.""BigMapUpdates""");
                    break;
                case "status": columns.Add(@"o.""Status"""); break;
                case "errors": columns.Add(@"o.""Errors"""); break;
                case "hasInternals": columns.Add(@"o.""InternalOperations"""); break;
                case "block":
                    columns.Add(@"b.""Hash""");
                    joins.Add(@"INNER JOIN ""Blocks"" as b ON b.""Level"" = o.""Level""");
                    break;
                case "quote": columns.Add(@"o.""Level"""); break;
                case "parameters": // backward compatibility
                    columns.Add($@"o.""Entrypoint""");
                    columns.Add($@"o.""RawParameters""");
                    break;
            }

            if (columns.Count == 0)
                return Array.Empty<object>();

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""TransactionOps"" as o {string.Join(' ', joins)}")
                .Filter(anyof, x => x == "sender" ? "SenderId" : x == "target" ? "TargetId" : "InitiatorId")
                .Filter("InitiatorId", initiator, x => "TargetId")
                .Filter("SenderId", sender, x => "TargetId")
                .Filter("TargetId", target, x => x == "sender" ? "SenderId" : "InitiatorId")
                .Filter("Amount", amount)
                .Filter("Entrypoint", entrypoint)
                .Filter("JsonParameters", parameter)
                .Filter("InternalOperations", hasInternals?.Eq == true
                    ? new Int32NullParameter { Gt = 0 }
                    : hasInternals?.Eq == false
                        ? new Int32NullParameter { Null = true }
                        : null)
                .Filter("Status", status)
                .FilterA(@"o.""Level""", level)
                .FilterA(@"o.""Timestamp""", timestamp)
                .Take(sort, offset, limit, x => x switch
                {
                    "level" => ("Id", "Level"),
                    "gasUsed" => ("GasUsed", "GasUsed"),
                    "storageUsed" => ("StorageUsed", "StorageUsed"),
                    "bakerFee" => ("BakerFee", "BakerFee"),
                    "storageFee" => ("StorageFee", "StorageFee"),
                    "allocationFee" => ("AllocationFee", "AllocationFee"),
                    "amount" => ("Amount", "Amount"),
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
                case "target":
                    foreach (var row in rows)
                        result[j++] = row.TargetId != null ? await Accounts.GetAliasAsync(row.TargetId) : null;
                    break;
                case "amount":
                    foreach (var row in rows)
                        result[j++] = row.Amount;
                    break;
                case "parameter":
                    foreach (var row in rows)
                        result[j++] = row.Entrypoint == null ? null : new TxParameter
                        {
                            Entrypoint = row.Entrypoint,
                            Value = format switch
                            {
                                MichelineFormat.Json => (RawJson)row.JsonParameters,
                                MichelineFormat.JsonString => row.JsonParameters,
                                MichelineFormat.Raw => row.RawParameters == null ? null : (RawJson)Micheline.ToJson(row.RawParameters),
                                MichelineFormat.RawString => row.RawParameters == null ? null : Micheline.ToJson(row.RawParameters),
                                _ => throw new Exception("Invalid MichelineFormat value")
                            }
                        };
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
                    var diffs = await BigMapsRepository.GetTransactionDiffs(db,
                        rows.Where(x => x.BigMapUpdates != null)
                            .Select(x => (int)x.Id)
                            .ToList(),
                        format);
                    if (diffs != null)
                        foreach (var row in rows)
                            result[j++] = diffs.GetValueOrDefault((int)row.Id);
                    break;
                case "status":
                    foreach (var row in rows)
                        result[j++] = OpStatuses.ToString(row.Status);
                    break;
                case "errors":
                    foreach (var row in rows)
                        result[j++] = row.Errors != null ? OperationErrorSerializer.Deserialize(row.Errors) : null;
                    break;
                case "hasInternals":
                    foreach (var row in rows)
                        result[j++] = row.InternalOperations > 0;
                    break;
                case "quote":
                    foreach (var row in rows)
                        result[j++] = Quotes.Get(quote, row.Level);
                    break;
                case "parameters":
                    foreach (var row in rows)
                        result[j++] = row.RawParameters == null ? null
                            : $"{{\"entrypoint\":\"{row.Entrypoint}\",\"value\":{Micheline.ToJson(row.RawParameters)}}}";
                    break;
            }

            return result;
        }
    }
}
