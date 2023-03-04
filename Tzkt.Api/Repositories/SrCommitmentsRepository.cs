using System.Data;
using Dapper;
using Tzkt.Api.Models;
using Tzkt.Api.Services.Cache;

namespace Tzkt.Api.Repositories
{
    public class SrCommitmentsRepository : DbConnection
    {
        readonly AccountsCache Accounts;
        readonly TimeCache Times;

        public SrCommitmentsRepository(AccountsCache accounts, TimeCache times, IConfiguration config) : base(config)
        {
            Accounts = accounts;
            Times = times;
        }

        async Task<IEnumerable<dynamic>> QueryAsync(SrCommitmentFilter filter, Pagination pagination, List<SelectionField> fields = null)
        {
            var select = """
                c."Id",
                c."SmartRollupId",
                c."InitiatorId",
                c."InboxLevel",
                c."State",
                c."Hash",
                c."Ticks",
                c."FirstLevel",
                c."LastLevel",
                c."Stakers",
                c."ActiveStakers",
                c."Successors",
                c."Status",
                c."PredecessorId" as "pId",
                p."InitiatorId" as "pInitiatorId",
                p."InboxLevel" as "pInboxLevel",
                p."State" as "pState",
                p."Hash" as "pHash",
                p."Ticks" as "pTicks",
                p."FirstLevel" as "pFirstLevel"
            """;

            if (fields != null)
            {
                var columns = new HashSet<string>(fields.Count);
                foreach (var field in fields)
                {
                    switch (field.Field)
                    {
                        case "id": columns.Add(@"c.""Id"""); break;
                        case "rollup": columns.Add(@"c.""SmartRollupId"""); break;
                        case "initiator": columns.Add(@"c.""InitiatorId"""); break;
                        case "inboxLevel": columns.Add(@"c.""InboxLevel"""); break;
                        case "state": columns.Add(@"c.""State"""); break;
                        case "hash": columns.Add(@"c.""Hash"""); break;
                        case "ticks": columns.Add(@"c.""Ticks"""); break;
                        case "firstLevel": columns.Add(@"c.""FirstLevel"""); break;
                        case "firstTime": columns.Add(@"c.""FirstLevel"""); break;
                        case "lastLevel": columns.Add(@"c.""LastLevel"""); break;
                        case "lastTime": columns.Add(@"c.""LastLevel"""); break;
                        case "stakers": columns.Add(@"c.""Stakers"""); break;
                        case "activeStakers": columns.Add(@"c.""ActiveStakers"""); break;
                        case "successors": columns.Add(@"c.""Successors"""); break;
                        case "status": columns.Add(@"c.""Status"""); break;
                        case "predecessor":
                            if (field.Path == null)
                            {
                                columns.Add(@"c.""PredecessorId"" as ""pId""");
                                columns.Add(@"p.""InitiatorId"" as ""pInitiatorId""");
                                columns.Add(@"p.""InboxLevel"" as ""pInboxLevel""");
                                columns.Add(@"p.""State"" as ""pState""");
                                columns.Add(@"p.""Hash"" as ""pHash""");
                                columns.Add(@"p.""Ticks"" as ""pTicks""");
                                columns.Add(@"p.""FirstLevel"" as ""pFirstLevel""");
                            }
                            else
                            {
                                switch (field.SubField().Field)
                                {
                                    case "id": columns.Add(@"c.""PredecessorId"" as ""pId"""); break;
                                    case "initiator": columns.Add(@"p.""InitiatorId"" as ""pInitiatorId"""); break;
                                    case "inboxLevel": columns.Add(@"p.""InboxLevel"" as ""pInboxLevel"""); break;
                                    case "state": columns.Add(@"p.""State"" as ""pState"""); break;
                                    case "hash": columns.Add(@"p.""Hash"" as ""pHash"""); break;
                                    case "ticks": columns.Add(@"p.""Ticks"" as ""pTicks"""); break;
                                    case "firstLevel": columns.Add(@"p.""FirstLevel"" as ""pFirstLevel"""); break;
                                    case "firstTime": columns.Add(@"p.""FirstLevel"" as ""pFirstLevel"""); break;
                                }
                            }
                            break;
                    }
                }

                if (columns.Count == 0)
                    return Enumerable.Empty<dynamic>();

                select = string.Join(',', columns);
            }

            var sql = new SqlBuilder($@"
                SELECT {select} FROM ""SmartRollupCommitments"" as c
                LEFT JOIN ""SmartRollupCommitments"" AS p ON p.""Id"" = c.""PredecessorId""")
                .FilterA(@"c.""Id""", filter.id)
                .FilterA(@"c.""InitiatorId""", filter.initiator)
                .FilterA(@"c.""SmartRollupId""", filter.rollup)
                .FilterA(@"c.""InboxLevel""", filter.inboxLevel)
                .FilterA(@"c.""Hash""", filter.hash)
                .FilterA(@"c.""FirstLevel""", filter.firstLevel)
                .FilterA(@"c.""FirstLevel""", filter.firstTime)
                .FilterA(@"c.""LastLevel""", filter.lastLevel)
                .FilterA(@"c.""LastLevel""", filter.lastTime)
                .FilterA(@"c.""Status""", filter.status)
                .FilterA(@"c.""PredecessorId""", filter.predecessor.id)
                .FilterA(@"p.""Hash""", filter.predecessor.hash)
                .Take(pagination, x => x switch
                {
                    "id" => (@"c.""Id""", @"c.""Id"""),
                    "inboxLevel" => (@"c.""InboxLevel""", @"c.""InboxLevel"""),
                    "firstLevel" => (@"c.""FirstLevel""", @"c.""FirstLevel"""),
                    "lastLevel" => (@"c.""LastLevel""", @"c.""LastLevel"""),
                    "stakers" => (@"c.""Stakers""", @"c.""Stakers"""),
                    "activeStakers" => (@"c.""ActiveStakers""", @"c.""ActiveStakers"""),
                    "successors" => (@"c.""Successors""", @"c.""Successors"""),
                    _ => (@"c.""Id""", @"c.""Id""")
                }, @"c.""Id""");

            using var db = GetConnection();
            return await db.QueryAsync(sql.Query, sql.Params);
        }

        public async Task<int> GetCount(SrCommitmentFilter filter)
        {
            var sql = new SqlBuilder(@"
                SELECT COUNT(*) FROM ""SmartRollupCommitments"" as c
                LEFT JOIN ""SmartRollupCommitments"" AS p ON p.""Id"" = c.""PredecessorId""")
                .FilterA(@"c.""Id""", filter.id)
                .FilterA(@"c.""InitiatorId""", filter.initiator)
                .FilterA(@"c.""SmartRollupId""", filter.rollup)
                .FilterA(@"c.""InboxLevel""", filter.inboxLevel)
                .FilterA(@"c.""Hash""", filter.hash)
                .FilterA(@"c.""FirstLevel""", filter.firstLevel)
                .FilterA(@"c.""FirstLevel""", filter.firstTime)
                .FilterA(@"c.""LastLevel""", filter.lastLevel)
                .FilterA(@"c.""LastLevel""", filter.lastTime)
                .FilterA(@"c.""Status""", filter.status)
                .FilterA(@"c.""PredecessorId""", filter.predecessor.id)
                .FilterA(@"p.""Hash""", filter.predecessor.hash);

            using var db = GetConnection();
            return await db.QueryFirstAsync<int>(sql.Query, sql.Params);
        }

        public async Task<IEnumerable<SrCommitment>> Get(SrCommitmentFilter filter, Pagination pagination)
        {
            var rows = await QueryAsync(filter, pagination);
            return rows.Select(row => new SrCommitment
            {
                Id = row.Id,
                Rollup = Accounts.GetAlias(row.SmartRollupId),
                Initiator = Accounts.GetAlias(row.InitiatorId),
                InboxLevel = row.InboxLevel,
                State = row.State,
                Hash = row.Hash,
                Ticks = row.Ticks,
                FirstLevel = row.FirstLevel,
                FirstTime = Times[row.FirstLevel],
                LastLevel = row.LastLevel,
                LastTime = Times[row.LastLevel],
                Stakers = row.Stakers,
                ActiveStakers = row.ActiveStakers,
                Successors = row.Successors,
                Status = SrCommitmentStatuses.ToString((int)row.Status),
                Predecessor = row.pId == null ? null : new()
                {
                    Id = row.pId,
                    Initiator = Accounts.GetAlias(row.pInitiatorId),
                    InboxLevel = row.pInboxLevel,
                    State = row.pState,
                    Hash = row.pHash,
                    Ticks = row.pTicks,
                    FirstLevel = row.pFirstLevel,
                    FirstTime = Times[row.pFirstLevel],
                }
            });
        }

        public async Task<object[][]> Get(SrCommitmentFilter filter, Pagination pagination, List<SelectionField> fields)
        {
            var rows = await QueryAsync(filter, pagination, fields);

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
                    case "initiator":
                        foreach (var row in rows)
                            result[j++][i] = Accounts.GetAlias(row.InitiatorId);
                        break;
                    case "initiator.alias":
                        foreach (var row in rows)
                            result[j++][i] = Accounts.GetAlias(row.InitiatorId).Name;
                        break;
                    case "initiator.address":
                        foreach (var row in rows)
                            result[j++][i] = Accounts.GetAlias(row.InitiatorId).Address;
                        break;
                    case "rollup":
                        foreach (var row in rows)
                            result[j++][i] = Accounts.GetAlias(row.SmartRollupId);
                        break;
                    case "rollup.alias":
                        foreach (var row in rows)
                            result[j++][i] = Accounts.GetAlias(row.SmartRollupId).Name;
                        break;
                    case "rollup.address":
                        foreach (var row in rows)
                            result[j++][i] = Accounts.GetAlias(row.SmartRollupId).Address;
                        break;
                    case "inboxLevel":
                        foreach (var row in rows)
                            result[j++][i] = row.InboxLevel;
                        break;
                    case "state":
                        foreach (var row in rows)
                            result[j++][i] = row.State;
                        break;
                    case "hash":
                        foreach (var row in rows)
                            result[j++][i] = row.Hash;
                        break;
                    case "ticks":
                        foreach (var row in rows)
                            result[j++][i] = row.Ticks;
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
                    case "stakers":
                        foreach (var row in rows)
                            result[j++][i] = row.Stakers;
                        break;
                    case "activeStakers":
                        foreach (var row in rows)
                            result[j++][i] = row.ActiveStakers;
                        break;
                    case "successors":
                        foreach (var row in rows)
                            result[j++][i] = row.Successors;
                        break;
                    case "status":
                        foreach (var row in rows)
                            result[j++][i] = SrCommitmentStatuses.ToString((int)row.Status);
                        break;
                    case "predecessor":
                        foreach (var row in rows)
                            result[j++][i] = row.pId == null ? null : new SrCommitmentInfo
                            {
                                Id = row.pId,
                                Initiator = Accounts.GetAlias(row.pInitiatorId),
                                InboxLevel = row.pInboxLevel,
                                State = row.pState,
                                Hash = row.pHash,
                                Ticks = row.pTicks,
                                FirstLevel = row.pFirstLevel,
                                FirstTime = Times[row.pFirstLevel],
                            };
                        break;
                    case "predecessor.id":
                        foreach (var row in rows)
                            result[j++][i] = row.pId;
                        break;
                    case "predecessor.initiator":
                        foreach (var row in rows)
                            result[j++][i] = row.pInitiatorId == null ? null : Accounts.GetAlias(row.pInitiatorId);
                        break;
                    case "predecessor.initiator.alias":
                        foreach (var row in rows)
                            result[j++][i] = row.pInitiatorId == null ? null : Accounts.GetAlias(row.pInitiatorId).Name;
                        break;
                    case "predecessor.initiator.address":
                        foreach (var row in rows)
                            result[j++][i] = row.pInitiatorId == null ? null : Accounts.GetAlias(row.pInitiatorId).Address;
                        break;
                    case "predecessor.inboxLevel":
                        foreach (var row in rows)
                            result[j++][i] = row.pInboxLevel;
                        break;
                    case "predecessor.state":
                        foreach (var row in rows)
                            result[j++][i] = row.pState;
                        break;
                    case "predecessor.hash":
                        foreach (var row in rows)
                            result[j++][i] = row.pHash;
                        break;
                    case "predecessor.ticks":
                        foreach (var row in rows)
                            result[j++][i] = row.pTicks;
                        break;
                    case "predecessor.firstLevel":
                        foreach (var row in rows)
                            result[j++][i] = row.pFirstLevel;
                        break;
                    case "predecessor.firstTime":
                        foreach (var row in rows)
                            result[j++][i] = row.pFirstLevel == null ? null : Times[row.pFirstLevel];
                        break;
                }
            }

            return result;
        }
    }
}
