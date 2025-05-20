using Dapper;
using Tzkt.Api.Models;
using Tzkt.Api.Services.Cache;
using Tzkt.Data;

namespace Tzkt.Api.Repositories
{
    public partial class OperationRepository
    {
        public async Task<bool?> GetSmartRollupRecoverBondStatus(string hash)
        {
            await using var db = await DataSource.OpenConnectionAsync();
            return await GetStatus(db, nameof(TzktContext.SmartRollupRecoverBondOps), hash);
        }

        public async Task<IEnumerable<Activity>> GetSmartRollupRecoverBondOpsActivity(
            List<RawAccount> accounts,
            ActivityRole roles,
            TimestampParameter? timestamp,
            Pagination pagination,
            Symbols quote)
        {
            List<int>? senderIds = null;
            List<int>? stakerIds = null;
            List<int>? smartRollupIds = null;

            foreach (var account in accounts)
            {
                if (account.SmartRollupRecoverBondCount == 0)
                    continue;

                if (account is RawUser)
                {
                    if ((roles & ActivityRole.Sender) != 0)
                    {
                        senderIds ??= new(accounts.Count);
                        senderIds.Add(account.Id);
                    }

                    if ((roles & ActivityRole.Target) != 0)
                    {
                        stakerIds ??= new(accounts.Count);
                        stakerIds.Add(account.Id);
                    }
                }
                else if (account is RawSmartRollup && (roles & ActivityRole.Target) != 0)
                {
                    smartRollupIds ??= new(accounts.Count);
                    smartRollupIds.Add(account.Id);
                }
            }

            if (senderIds == null && stakerIds == null && smartRollupIds == null)
                return [];

            var or = new OrParameter(
                ("SenderId", senderIds),
                ("StakerId", stakerIds),
                ("SmartRollupId", smartRollupIds));

            return await GetSmartRollupRecoverBondOps(new() { or = or, timestamp = timestamp }, pagination, quote);
        }

        public async Task<int> GetSmartRollupRecoverBondOpsCount(SrRecoverBondOperationFilter filter)
        {
            var sql = new SqlBuilder(@"SELECT COUNT(*) FROM ""SmartRollupRecoverBondOps""")
                .Filter(filter.or)
                .Filter("Id", filter.id)
                .Filter("OpHash", filter.hash)
                .Filter("Counter", filter.counter)
                .Filter("Level", filter.level)
                .Filter("Level", filter.timestamp)
                .Filter("SenderId", filter.sender)
                .Filter("Status", filter.status)
                .Filter("SmartRollupId", filter.rollup)
                .Filter("StakerId", filter.staker)
                .Filter(filter.anyof, x => x == "sender" ? "SenderId" : "StakerId");

            await using var db = await DataSource.OpenConnectionAsync();
            return await db.QueryFirstAsync<int>(sql.Query, sql.Params);
        }

        async Task<IEnumerable<dynamic>> QuerySmartRollupRecoverBondOps(SrRecoverBondOperationFilter filter, Pagination pagination, List<SelectionField>? fields = null)
        {
            var select = "*";
            if (fields != null)
            {
                var columns = new HashSet<string>(fields.Count);
                foreach (var field in fields)
                {
                    switch (field.Field)
                    {
                        case "id": columns.Add(@"o.""Id"""); break;
                        case "level": columns.Add(@"o.""Level"""); break;
                        case "timestamp": columns.Add(@"o.""Timestamp"""); break;
                        case "hash": columns.Add(@"o.""OpHash"""); break;
                        case "sender": columns.Add(@"o.""SenderId"""); break;
                        case "counter": columns.Add(@"o.""Counter"""); break;
                        case "gasLimit": columns.Add(@"o.""GasLimit"""); break;
                        case "gasUsed": columns.Add(@"o.""GasUsed"""); break;
                        case "storageLimit": columns.Add(@"o.""StorageLimit"""); break;
                        case "bakerFee": columns.Add(@"o.""BakerFee"""); break;
                        case "status": columns.Add(@"o.""Status"""); break;
                        case "rollup": columns.Add(@"o.""SmartRollupId"""); break;
                        case "staker": columns.Add(@"o.""StakerId"""); break;
                        case "bond": columns.Add(@"o.""Bond"""); break;
                        case "errors": columns.Add(@"o.""Errors"""); break;
                        case "quote": columns.Add(@"o.""Level"""); break;
                    }
                }

                if (columns.Count == 0)
                    return [];

                select = string.Join(',', columns);
            }

            var sql = new SqlBuilder($@"SELECT {select} FROM ""SmartRollupRecoverBondOps"" as o")
                .Filter(filter.or)
                .Filter("Id", filter.id)
                .Filter("OpHash", filter.hash)
                .Filter("Counter", filter.counter)
                .Filter("Level", filter.level)
                .Filter("Level", filter.timestamp)
                .Filter("SenderId", filter.sender)
                .Filter("Status", filter.status)
                .Filter("SmartRollupId", filter.rollup)
                .Filter("StakerId", filter.staker)
                .Filter(filter.anyof, x => x == "sender" ? "SenderId" : "StakerId")
                .Take(pagination, x => (@"""Id""", @"""Id"""));

            await using var db = await DataSource.OpenConnectionAsync();
            return await db.QueryAsync(sql.Query, sql.Params);
        }

        public async Task<IEnumerable<SmartRollupRecoverBondOperation>> GetSmartRollupRecoverBondOps(SrRecoverBondOperationFilter filter, Pagination pagination, Symbols quote)
        {
            var rows = await QuerySmartRollupRecoverBondOps(filter, pagination);
            return rows.Select(row => new SmartRollupRecoverBondOperation
            {
                Id = row.Id,
                Level = row.Level,
                Timestamp = row.Timestamp,
                Hash = row.OpHash,
                Sender = Accounts.GetAlias(row.SenderId),
                Counter = row.Counter,
                GasLimit = row.GasLimit,
                GasUsed = row.GasUsed,
                StorageLimit = row.StorageLimit,
                BakerFee = row.BakerFee,
                Status = OpStatuses.ToString(row.Status),
                Rollup = row.SmartRollupId == null ? null : Accounts.GetAlias(row.SmartRollupId),
                Staker = row.StakerId == null ? null : Accounts.GetAlias(row.StakerId),
                Bond = row.Bond,
                Errors = row.Errors != null ? OperationErrorSerializer.Deserialize(row.Errors) : null,
                Quote = Quotes.Get(quote, row.Level)
            });
        }

        public async Task<object?[][]> GetSmartRollupRecoverBondOps(SrRecoverBondOperationFilter filter, Pagination pagination, List<SelectionField> fields, Symbols quote)
        {
            var rows = await QuerySmartRollupRecoverBondOps(filter, pagination, fields);

            var result = new object?[rows.Count()][];
            for (int i = 0; i < result.Length; i++)
                result[i] = new object?[fields.Count];

            for (int i = 0, j = 0; i < fields.Count; j = 0, i++)
            {
                switch (fields[i].Full)
                {
                    case "type":
                        foreach (var row in rows)
                            result[j++][i] = ActivityTypes.SmartRollupRecoverBond;
                        break;
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
                            result[j++][i] = row.Timestamp;
                        break;
                    case "hash":
                        foreach (var row in rows)
                            result[j++][i] = row.OpHash;
                        break;
                    case "sender":
                        foreach (var row in rows)
                            result[j++][i] = Accounts.GetAlias(row.SenderId);
                        break;
                    case "sender.alias":
                        foreach (var row in rows)
                            result[j++][i] = Accounts.GetAlias(row.SenderId).Name;
                        break;
                    case "sender.address":
                        foreach (var row in rows)
                            result[j++][i] = Accounts.GetAlias(row.SenderId).Address;
                        break;
                    case "counter":
                        foreach (var row in rows)
                            result[j++][i] = row.Counter;
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
                    case "bakerFee":
                        foreach (var row in rows)
                            result[j++][i] = row.BakerFee;
                        break;
                    case "status":
                        foreach (var row in rows)
                            result[j++][i] = OpStatuses.ToString(row.Status);
                        break;
                    case "rollup":
                        foreach (var row in rows)
                            result[j++][i] = row.SmartRollupId == null ? null : Accounts.GetAlias(row.SmartRollupId);
                        break;
                    case "rollup.alias":
                        foreach (var row in rows)
                            result[j++][i] = row.SmartRollupId == null ? null : Accounts.GetAlias(row.SmartRollupId).Name;
                        break;
                    case "rollup.address":
                        foreach (var row in rows)
                            result[j++][i] = row.SmartRollupId == null ? null : Accounts.GetAlias(row.SmartRollupId).Address;
                        break;
                    case "staker":
                        foreach (var row in rows)
                            result[j++][i] = row.StakerId == null ? null : Accounts.GetAlias(row.StakerId);
                        break;
                    case "staker.alias":
                        foreach (var row in rows)
                            result[j++][i] = row.StakerId == null ? null : Accounts.GetAlias(row.StakerId).Name;
                        break;
                    case "staker.address":
                        foreach (var row in rows)
                            result[j++][i] = row.StakerId == null ? null : Accounts.GetAlias(row.StakerId).Address;
                        break;
                    case "bond":
                        foreach (var row in rows)
                            result[j++][i] = row.Bond;
                        break;
                    case "errors":
                        foreach (var row in rows)
                            result[j++][i] = row.Errors != null ? OperationErrorSerializer.Deserialize(row.Errors) : null;
                        break;
                    case "quote":
                        foreach (var row in rows)
                            result[j++][i] = Quotes.Get(quote, row.Level);
                        break;
                }
            }

            return result;
        }
    }
}
