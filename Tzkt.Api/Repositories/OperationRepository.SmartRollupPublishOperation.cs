﻿using Dapper;
using Tzkt.Api.Models;
using Tzkt.Api.Services.Cache;
using Tzkt.Data;

namespace Tzkt.Api.Repositories
{
    public partial class OperationRepository
    {
        public async Task<bool?> GetSmartRollupPublishStatus(string hash)
        {
            await using var db = await DataSource.OpenConnectionAsync();
            return await GetStatus(db, nameof(TzktContext.SmartRollupPublishOps), hash);
        }

        public async Task<IEnumerable<Activity>> GetSmartRollupPublishOpsActivity(
            List<RawAccount> accounts,
            ActivityRole roles,
            TimestampParameter? timestamp,
            Pagination pagination,
            Symbols quote)
        {
            List<int>? senderIds = null;
            List<int>? smartRollupIds = null;

            foreach (var account in accounts)
            {
                if (account.SmartRollupPublishCount == 0)
                    continue;

                if (account is RawUser && (roles & ActivityRole.Sender) != 0)
                {
                    senderIds ??= new(accounts.Count);
                    senderIds.Add(account.Id);
                }
                else if (account is RawSmartRollup && (roles & ActivityRole.Target) != 0)
                {
                    smartRollupIds ??= new(accounts.Count);
                    smartRollupIds.Add(account.Id);
                }
            }

            if (senderIds == null && smartRollupIds == null)
                return [];

            var or = new OrParameter(
                (@"o.""SenderId""", senderIds),
                (@"o.""SmartRollupId""", smartRollupIds));

            return await GetSmartRollupPublishOps(new() { or = or, timestamp = timestamp }, pagination, quote);
        }

        public async Task<int> GetSmartRollupPublishOpsCount(SrPublishOperationFilter filter)
        {
            var sql = new SqlBuilder(@"
                SELECT COUNT(*) FROM ""SmartRollupPublishOps"" as o
                LEFT JOIN ""SmartRollupCommitments"" AS c ON c.""Id"" = o.""CommitmentId""")
                .FilterA(filter.or)
                .FilterA(@"o.""Id""", filter.id)
                .FilterA(@"o.""OpHash""", filter.hash)
                .FilterA(@"o.""Counter""", filter.counter)
                .FilterA(@"o.""Level""", filter.level)
                .FilterA(@"o.""Level""", filter.timestamp)
                .FilterA(@"o.""SenderId""", filter.sender)
                .FilterA(@"o.""Status""", filter.status)
                .FilterA(@"o.""SmartRollupId""", filter.rollup)
                .FilterA(@"o.""CommitmentId""", filter.commitment.id)
                .FilterA(@"c.""Hash""", filter.commitment.hash);

            await using var db = await DataSource.OpenConnectionAsync();
            return await db.QueryFirstAsync<int>(sql.Query, sql.Params);
        }

        async Task<IEnumerable<dynamic>> QuerySmartRollupPublishOps(SrPublishOperationFilter filter, Pagination pagination, List<SelectionField>? fields = null)
        {
            var select = """
                o."Id",
                o."Level",
                o."Timestamp",
                o."OpHash",
                o."SenderId",
                o."Counter",
                o."GasLimit",
                o."GasUsed",
                o."StorageLimit",
                o."BakerFee",
                o."Status",
                o."SmartRollupId",
                o."CommitmentId" as "cId",
                o."Bond",
                o."Errors",
                o."Level",

                c."InitiatorId" as "cInitiatorId",
                c."InboxLevel" as "cInboxLevel",
                c."State" as "cState",
                c."Hash" as "cHash",
                c."Ticks" as "cTicks",
                c."FirstLevel" as "cFirstLevel"
            """;

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
                        case "commitment":
                            if (field.Path == null)
                            {
                                columns.Add(@"o.""CommitmentId"" as ""cId""");
                                columns.Add(@"c.""InitiatorId"" as ""cInitiatorId""");
                                columns.Add(@"c.""InboxLevel"" as ""cInboxLevel""");
                                columns.Add(@"c.""State"" as ""cState""");
                                columns.Add(@"c.""Hash"" as ""cHash""");
                                columns.Add(@"c.""Ticks"" as ""cTicks""");
                                columns.Add(@"c.""FirstLevel"" as ""cFirstLevel""");
                            }
                            else
                            {
                                switch (field.SubField()!.Field)
                                {
                                    case "id": columns.Add(@"o.""CommitmentId"" as ""cId"""); break;
                                    case "initiator": columns.Add(@"c.""InitiatorId"" as ""cInitiatorId"""); break;
                                    case "inboxLevel": columns.Add(@"c.""InboxLevel"" as ""cInboxLevel"""); break;
                                    case "state": columns.Add(@"c.""State"" as ""cState"""); break;
                                    case "hash": columns.Add(@"c.""Hash"" as ""cHash"""); break;
                                    case "ticks": columns.Add(@"c.""Ticks"" as ""cTicks"""); break;
                                    case "firstLevel": columns.Add(@"c.""FirstLevel"" as ""cFirstLevel"""); break;
                                    case "firstTime": columns.Add(@"c.""FirstLevel"" as ""cFirstLevel"""); break;
                                }
                            }
                            break;
                        case "bond": columns.Add(@"o.""Bond"""); break;
                        case "errors": columns.Add(@"o.""Errors"""); break;
                        case "quote": columns.Add(@"o.""Level"""); break;
                    }
                }

                if (columns.Count == 0)
                    return [];

                select = string.Join(',', columns);
            }

            var sql = new SqlBuilder($@"
                SELECT {select} FROM ""SmartRollupPublishOps"" as o
                LEFT JOIN ""SmartRollupCommitments"" AS c ON c.""Id"" = o.""CommitmentId""")
                .FilterA(filter.or)
                .FilterA(@"o.""Id""", filter.id)
                .FilterA(@"o.""OpHash""", filter.hash)
                .FilterA(@"o.""Counter""", filter.counter)
                .FilterA(@"o.""Level""", filter.level)
                .FilterA(@"o.""Level""", filter.timestamp)
                .FilterA(@"o.""SenderId""", filter.sender)
                .FilterA(@"o.""Status""", filter.status)
                .FilterA(@"o.""SmartRollupId""", filter.rollup)
                .FilterA(@"o.""CommitmentId""", filter.commitment.id)
                .FilterA(@"c.""Hash""", filter.commitment.hash)
                .Take(pagination, x => (@"o.""Id""", @"o.""Id"""), @"o.""Id""");

            await using var db = await DataSource.OpenConnectionAsync();
            return await db.QueryAsync(sql.Query, sql.Params);
        }

        public async Task<IEnumerable<SmartRollupPublishOperation>> GetSmartRollupPublishOps(SrPublishOperationFilter filter, Pagination pagination, Symbols quote)
        {
            var rows = await QuerySmartRollupPublishOps(filter, pagination);
            return rows.Select(row => new SmartRollupPublishOperation
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
                Commitment = row.cId == null ? null : new()
                {
                    Id = row.cId,
                    Initiator = Accounts.GetAlias(row.cInitiatorId),
                    InboxLevel = row.cInboxLevel,
                    State = row.cState,
                    Hash = row.cHash,
                    Ticks = row.cTicks,
                    FirstLevel = row.cFirstLevel,
                    FirstTime = Times[row.cFirstLevel],
                },
                Bond = row.Bond,
                Errors = row.Errors != null ? OperationErrorSerializer.Deserialize(row.Errors) : null,
                Quote = Quotes.Get(quote, row.Level)
            });
        }

        public async Task<object?[][]> GetSmartRollupPublishOps(SrPublishOperationFilter filter, Pagination pagination, List<SelectionField> fields, Symbols quote)
        {
            var rows = await QuerySmartRollupPublishOps(filter, pagination, fields);

            var result = new object?[rows.Count()][];
            for (int i = 0; i < result.Length; i++)
                result[i] = new object?[fields.Count];

            for (int i = 0, j = 0; i < fields.Count; j = 0, i++)
            {
                switch (fields[i].Full)
                {
                    case "type":
                        foreach (var row in rows)
                            result[j++][i] = ActivityTypes.SmartRollupPublish;
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
                    case "commitment":
                        foreach (var row in rows)
                            result[j++][i] = row.cId == null ? null : new SrCommitmentInfo
                            {
                                Id = row.cId,
                                Initiator = Accounts.GetAlias(row.cInitiatorId),
                                InboxLevel = row.cInboxLevel,
                                State = row.cState,
                                Hash = row.cHash,
                                Ticks = row.cTicks,
                                FirstLevel = row.cFirstLevel,
                                FirstTime = Times[row.cFirstLevel],
                            };
                        break;
                    case "commitment.id":
                        foreach (var row in rows)
                            result[j++][i] = row.cId;
                        break;
                    case "commitment.initiator":
                        foreach (var row in rows)
                            result[j++][i] = row.cInitiatorId == null ? null : Accounts.GetAlias(row.cInitiatorId);
                        break;
                    case "commitment.initiator.alias":
                        foreach (var row in rows)
                            result[j++][i] = row.cInitiatorId == null ? null : Accounts.GetAlias(row.cInitiatorId).Name;
                        break;
                    case "commitment.initiator.address":
                        foreach (var row in rows)
                            result[j++][i] = row.cInitiatorId == null ? null : Accounts.GetAlias(row.cInitiatorId).Address;
                        break;
                    case "commitment.inboxLevel":
                        foreach (var row in rows)
                            result[j++][i] = row.cInboxLevel;
                        break;
                    case "commitment.state":
                        foreach (var row in rows)
                            result[j++][i] = row.cState;
                        break;
                    case "commitment.hash":
                        foreach (var row in rows)
                            result[j++][i] = row.cHash;
                        break;
                    case "commitment.ticks":
                        foreach (var row in rows)
                            result[j++][i] = row.cTicks;
                        break;
                    case "commitment.firstLevel":
                        foreach (var row in rows)
                            result[j++][i] = row.cFirstLevel;
                        break;
                    case "commitment.firstTime":
                        foreach (var row in rows)
                            result[j++][i] = row.cFirstLevel == null ? null : Times[row.cFirstLevel];
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
