using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Tzkt.Api.Models;

namespace Tzkt.Api.Repositories
{
    public partial class OperationRepository : DbConnection
    {
        public async Task<int> GetMigrationsCount(
            Int32Parameter level,
            DateTimeParameter timestamp)
        {
            var sql = new SqlBuilder(@"SELECT COUNT(*) FROM ""MigrationOps""")
                .Filter("Level", level)
                .Filter("Timestamp", timestamp);

            using var db = GetConnection();
            return await db.QueryFirstAsync<int>(sql.Query, sql.Params);
        }

        public async Task<MigrationOperation> GetMigration(long id, MichelineFormat format, Symbols quote)
        {
            var sql = $@"
                SELECT      o.*, b.""Hash""
                FROM        ""MigrationOps"" as o
                INNER JOIN  ""Blocks"" as b 
                        ON  b.""Level"" = o.""Level""
                WHERE       o.""Id"" = @id
                LIMIT       1";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { id });

            // TODO: optimize for QueryFirstOrDefaultAsync

            #region include storage
            var storages = await AccountRepository.GetStorages(db,
                rows.Where(x => x.StorageId != null)
                    .Select(x => (int)x.StorageId)
                    .Distinct()
                    .ToList(),
                format);
            #endregion

            #region include diffs
            var diffs = await BigMapsRepository.GetMigrationDiffs(db,
                rows.Where(x => x.BigMapUpdates != null)
                    .Select(x => (long)x.Id)
                    .ToList(),
                format);
            #endregion

            return rows.Select(row => new MigrationOperation
            {
                Id = row.Id,
                Level = row.Level,
                Block = row.Hash,
                Timestamp = row.Timestamp,
                Account = Accounts.GetAlias(row.AccountId),
                Kind = MigrationKinds.ToString(row.Kind),
                BalanceChange = row.BalanceChange,
                Storage = row.StorageId == null ? null : storages?[row.StorageId],
                Diffs = row.BigMapUpdates == null ? null : diffs?[row.Id],
                TokenTransfersCount = row.TokenTransfers,
                Quote = Quotes.Get(quote, row.Level)
            }).FirstOrDefault();
        }

        public async Task<IEnumerable<MigrationOperation>> GetMigrations(
            AccountParameter account,
            MigrationKindParameter kind,
            Int64Parameter balanceChange,
            Int64Parameter id,
            Int32Parameter level,
            DateTimeParameter timestamp,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            MichelineFormat format,
            Symbols quote,
            bool includeStorage = false,
            bool includeDiffs = false)
        {
            var sql = new SqlBuilder(@"SELECT o.*, b.""Hash"" FROM ""MigrationOps"" AS o INNER JOIN ""Blocks"" as b ON b.""Level"" = o.""Level""")
                .Filter("AccountId", account)
                .Filter("Kind", kind)
                .Filter("BalanceChange", balanceChange)
                .FilterA(@"o.""Id""", id)
                .FilterA(@"o.""Level""", level)
                .FilterA(@"o.""Timestamp""", timestamp)
                .Take(sort, offset, limit, x => x == "level" ? ("Id", "Level") : ("Id", "Id"), "o");

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
            var diffs = includeDiffs
                ? await BigMapsRepository.GetMigrationDiffs(db,
                    rows.Where(x => x.BigMapUpdates != null)
                        .Select(x => (long)x.Id)
                        .ToList(),
                    format)
                : null;
            #endregion

            return rows.Select(row => new MigrationOperation
            {
                Id = row.Id,
                Level = row.Level,
                Block = row.Hash,
                Timestamp = row.Timestamp,
                Account = Accounts.GetAlias(row.AccountId),
                Kind = MigrationKinds.ToString(row.Kind),
                BalanceChange = row.BalanceChange,
                Storage = row.StorageId == null ? null : storages?[row.StorageId],
                Diffs = row.BigMapUpdates == null ? null : diffs?[row.Id],
                TokenTransfersCount = row.TokenTransfers,
                Quote = Quotes.Get(quote, row.Level)
            });
        }

        public async Task<object[][]> GetMigrations(
            AccountParameter account,
            MigrationKindParameter kind,
            Int64Parameter balanceChange,
            Int64Parameter id,
            Int32Parameter level,
            DateTimeParameter timestamp,
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
                    case "account": columns.Add(@"o.""AccountId"""); break;
                    case "kind": columns.Add(@"o.""Kind"""); break;
                    case "balanceChange": columns.Add(@"o.""BalanceChange"""); break;
                    case "block":
                        columns.Add(@"b.""Hash""");
                        joins.Add(@"INNER JOIN ""Blocks"" as b ON b.""Level"" = o.""Level""");
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

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""MigrationOps"" as o {string.Join(' ', joins)}")
                .Filter("AccountId", account)
                .Filter("Kind", kind)
                .Filter("BalanceChange", balanceChange)
                .FilterA(@"o.""Id""", id)
                .FilterA(@"o.""Level""", level)
                .FilterA(@"o.""Timestamp""", timestamp)
                .Take(sort, offset, limit, x => x == "level" ? ("Id", "Level") : ("Id", "Id"), "o");

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
                    case "account":
                        foreach (var row in rows)
                            result[j++][i] = await Accounts.GetAliasAsync(row.AccountId);
                        break;
                    case "kind":
                        foreach (var row in rows)
                            result[j++][i] = MigrationKinds.ToString(row.Kind);
                        break;
                    case "balanceChange":
                        foreach (var row in rows)
                            result[j++][i] = row.BalanceChange;
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
                        var diffs = await BigMapsRepository.GetMigrationDiffs(db,
                            rows.Where(x => x.BigMapUpdates != null)
                                .Select(x => (long)x.Id)
                                .ToList(),
                            format);
                        if (diffs != null)
                            foreach (var row in rows)
                                result[j++][i] = row.BigMapUpdates == null ? null : diffs[row.Id];
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

        public async Task<object[]> GetMigrations(
            AccountParameter account,
            MigrationKindParameter kind,
            Int64Parameter balanceChange,
            Int64Parameter id,
            Int32Parameter level,
            DateTimeParameter timestamp,
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
                case "account": columns.Add(@"o.""AccountId"""); break;
                case "kind": columns.Add(@"o.""Kind"""); break;
                case "balanceChange": columns.Add(@"o.""BalanceChange"""); break;
                case "block":
                    columns.Add(@"b.""Hash""");
                    joins.Add(@"INNER JOIN ""Blocks"" as b ON b.""Level"" = o.""Level""");
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

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""MigrationOps"" as o {string.Join(' ', joins)}")
                .Filter("AccountId", account)
                .Filter("Kind", kind)
                .Filter("BalanceChange", balanceChange)
                .FilterA(@"o.""Id""", id)
                .FilterA(@"o.""Level""", level)
                .FilterA(@"o.""Timestamp""", timestamp)
                .Take(sort, offset, limit, x => x == "level" ? ("Id", "Level") : ("Id", "Id"), "o");

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
                case "account":
                    foreach (var row in rows)
                        result[j++] = await Accounts.GetAliasAsync(row.AccountId);
                    break;
                case "kind":
                    foreach (var row in rows)
                        result[j++] = MigrationKinds.ToString(row.Kind);
                    break;
                case "balanceChange":
                    foreach (var row in rows)
                        result[j++] = row.BalanceChange;
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
                    var diffs = await BigMapsRepository.GetMigrationDiffs(db,
                        rows.Where(x => x.BigMapUpdates != null)
                            .Select(x => (long)x.Id)
                            .ToList(),
                        format);
                    if (diffs != null)
                        foreach (var row in rows)
                            result[j++] = row.BigMapUpdates == null ? null : diffs[row.Id];
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
