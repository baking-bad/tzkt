using System.Data;
using Dapper;
using Netezos.Encoding;
using Tzkt.Api.Models;
using Tzkt.Api.Services.Cache;

namespace Tzkt.Api.Repositories
{
    public class SmartRollupsRepository : DbConnection
    {
        readonly AccountsCache Accounts;
        readonly TimeCache Times;

        public SmartRollupsRepository(AccountsCache accounts, TimeCache times, IConfiguration config) : base(config)
        {
            Accounts = accounts;
            Times = times;
        }

        #region commitments
        async Task<IEnumerable<dynamic>> QueryCommitmentsAsync(SrCommitmentFilter filter, Pagination pagination, List<SelectionField> fields = null)
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
                .FilterA(@"c.""PredecessorId""", filter.predecessor?.id)
                .FilterA(@"p.""Hash""", filter.predecessor?.hash)
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

        public async Task<int> GetCommitmentsCount(SrCommitmentFilter filter)
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
                .FilterA(@"c.""PredecessorId""", filter.predecessor?.id)
                .FilterA(@"p.""Hash""", filter.predecessor?.hash);

            using var db = GetConnection();
            return await db.QueryFirstAsync<int>(sql.Query, sql.Params);
        }

        public async Task<IEnumerable<SrCommitment>> GetCommitments(SrCommitmentFilter filter, Pagination pagination)
        {
            var rows = await QueryCommitmentsAsync(filter, pagination);
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

        public async Task<object[][]> GetCommitments(SrCommitmentFilter filter, Pagination pagination, List<SelectionField> fields)
        {
            var rows = await QueryCommitmentsAsync(filter, pagination, fields);

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
        #endregion

        #region games
        async Task<IEnumerable<dynamic>> QueryGamesAsync(SrGameFilter filter, Pagination pagination, List<SelectionField> fields = null)
        {
            var select = """
                g."Id",
                g."SmartRollupId",
                g."InitiatorId",
                g."InitiatorCommitmentId" as "icId",
                g."OpponentId",
                g."OpponentCommitmentId" as "ocId",
                g."LastMoveId" as "mId",
                g."FirstLevel",
                g."LastLevel",
                g."InitiatorReward",
                g."InitiatorLoss",
                g."OpponentReward",
                g."OpponentLoss",

                ic."InitiatorId" as "icInitiatorId",
                ic."InboxLevel" as "icInboxLevel",
                ic."State" as "icState",
                ic."Hash" as "icHash",
                ic."Ticks" as "icTicks",
                ic."FirstLevel" as "icFirstLevel",
            
                oc."InitiatorId" as "ocInitiatorId",
                oc."InboxLevel" as "ocInboxLevel",
                oc."State" as "ocState",
                oc."Hash" as "ocHash",
                oc."Ticks" as "ocTicks",
                oc."FirstLevel" as "ocFirstLevel",
            
                m."Level" as "mLevel",
                m."Timestamp" as "mTimestamp",
                m."SenderId" as "mSenderId",
                m."Move" as "mMove",
                m."GameStatus" as "mGameStatus"
            """;

            if (fields != null)
            {
                var columns = new HashSet<string>(fields.Count);
                foreach (var field in fields)
                {
                    switch (field.Field)
                    {
                        case "id": columns.Add(@"g.""Id"""); break;
                        case "rollup": columns.Add(@"g.""SmartRollupId"""); break;
                        case "initiator": columns.Add(@"g.""InitiatorId"""); break;
                        case "initiatorCommitment":
                            if (field.Path == null)
                            {
                                columns.Add(@"g.""InitiatorCommitmentId"" as ""icId""");
                                columns.Add(@"ic.""InitiatorId"" as ""icInitiatorId""");
                                columns.Add(@"ic.""InboxLevel"" as ""icInboxLevel""");
                                columns.Add(@"ic.""State"" as ""icState""");
                                columns.Add(@"ic.""Hash"" as ""icHash""");
                                columns.Add(@"ic.""Ticks"" as ""icTicks""");
                                columns.Add(@"ic.""FirstLevel"" as ""icFirstLevel""");
                            }
                            else
                            {
                                switch (field.SubField().Field)
                                {
                                    case "id": columns.Add(@"g.""InitiatorCommitmentId"" as ""icId"""); break;
                                    case "initiator": columns.Add(@"ic.""InitiatorId"" as ""icInitiatorId"""); break;
                                    case "inboxLevel": columns.Add(@"ic.""InboxLevel"" as ""icInboxLevel"""); break;
                                    case "state": columns.Add(@"ic.""State"" as ""icState"""); break;
                                    case "hash": columns.Add(@"ic.""Hash"" as ""icHash"""); break;
                                    case "ticks": columns.Add(@"ic.""Ticks"" as ""icTicks"""); break;
                                    case "firstLevel": columns.Add(@"ic.""FirstLevel"" as ""icFirstLevel"""); break;
                                    case "firstTime": columns.Add(@"ic.""FirstLevel"" as ""icFirstLevel"""); break;
                                }
                            }
                            break;
                        case "opponent": columns.Add(@"g.""OpponentId"""); break;
                        case "opponentCommitment":
                            if (field.Path == null)
                            {
                                columns.Add(@"g.""OpponentCommitmentId"" as ""ocId""");
                                columns.Add(@"oc.""InitiatorId"" as ""ocInitiatorId""");
                                columns.Add(@"oc.""InboxLevel"" as ""ocInboxLevel""");
                                columns.Add(@"oc.""State"" as ""ocState""");
                                columns.Add(@"oc.""Hash"" as ""ocHash""");
                                columns.Add(@"oc.""Ticks"" as ""ocTicks""");
                                columns.Add(@"oc.""FirstLevel"" as ""ocFirstLevel""");
                            }
                            else
                            {
                                switch (field.SubField().Field)
                                {
                                    case "id": columns.Add(@"g.""OpponentCommitmentId"" as ""ocId"""); break;
                                    case "initiator": columns.Add(@"oc.""InitiatorId"" as ""ocInitiatorId"""); break;
                                    case "inboxLevel": columns.Add(@"oc.""InboxLevel"" as ""ocInboxLevel"""); break;
                                    case "state": columns.Add(@"oc.""State"" as ""ocState"""); break;
                                    case "hash": columns.Add(@"oc.""Hash"" as ""ocHash"""); break;
                                    case "ticks": columns.Add(@"oc.""Ticks"" as ""ocTicks"""); break;
                                    case "firstLevel": columns.Add(@"oc.""FirstLevel"" as ""ocFirstLevel"""); break;
                                    case "firstTime": columns.Add(@"oc.""FirstLevel"" as ""ocFirstLevel"""); break;
                                }
                            }
                            break;
                        case "lastMove":
                            if (field.Path == null)
                            {
                                columns.Add(@"g.""LastMoveId"" as ""mId""");
                                columns.Add(@"m.""Level"" as ""mLevel""");
                                columns.Add(@"m.""Timestamp"" as ""mTimestamp""");
                                columns.Add(@"m.""SenderId"" as ""mSenderId""");
                                columns.Add(@"m.""Move"" as ""mMove""");
                                columns.Add(@"m.""GameStatus"" as ""mGameStatus""");
                            }
                            else
                            {
                                switch (field.SubField().Field)
                                {
                                    case "id": columns.Add(@"g.""LastMoveId"" as ""mId"""); break;
                                    case "level": columns.Add(@"m.""Level"" as ""mLevel"""); break;
                                    case "timestamp": columns.Add(@"m.""Timestamp"" as ""mTimestamp"""); break;
                                    case "sender": columns.Add(@"m.""SenderId"" as ""mSenderId"""); break;
                                    case "move": columns.Add(@"m.""Move"" as ""mMove"""); break;
                                    case "gameStatus": columns.Add(@"m.""GameStatus"" as ""mGameStatus"""); break;
                                }
                            }
                            break;
                        case "firstLevel": columns.Add(@"g.""FirstLevel"""); break;
                        case "firstTime": columns.Add(@"g.""FirstLevel"""); break;
                        case "lastLevel": columns.Add(@"g.""LastLevel"""); break;
                        case "lastTime": columns.Add(@"g.""LastLevel"""); break;
                        case "initiatorReward": columns.Add(@"g.""InitiatorReward"""); break;
                        case "initiatorLoss": columns.Add(@"g.""InitiatorLoss"""); break;
                        case "opponentReward": columns.Add(@"g.""OpponentReward"""); break;
                        case "opponentLoss": columns.Add(@"g.""OpponentLoss"""); break;
                    }
                }

                if (columns.Count == 0)
                    return Enumerable.Empty<dynamic>();

                select = string.Join(',', columns);
            }

            var sql = new SqlBuilder($@"
                SELECT {select} FROM ""RefutationGames"" as g
                LEFT JOIN ""SmartRollupCommitments"" AS ic ON ic.""Id"" = g.""InitiatorCommitmentId""
                LEFT JOIN ""SmartRollupCommitments"" AS oc ON oc.""Id"" = g.""OpponentCommitmentId""
                LEFT JOIN ""SmartRollupRefuteOps"" AS m ON m.""Id"" = g.""LastMoveId""")
                .FilterA(@"g.""Id""", filter.id)
                .FilterA(@"g.""SmartRollupId""", filter.rollup)
                .FilterA(@"g.""InitiatorId""", filter.initiator)
                .FilterA(@"g.""InitiatorCommitmentId""", filter.initiatorCommitment?.id)
                .FilterA(@"ic.""Hash""", filter.initiatorCommitment?.hash)
                .FilterA(@"g.""OpponentId""", filter.opponent)
                .FilterA(@"g.""OpponentCommitmentId""", filter.opponentCommitment?.id)
                .FilterA(@"oc.""Hash""", filter.opponentCommitment?.hash)
                .FilterA(@"g.""FirstLevel""", filter.firstLevel)
                .FilterA(@"g.""FirstLevel""", filter.firstTime)
                .FilterA(@"g.""LastLevel""", filter.lastLevel)
                .FilterA(@"g.""LastLevel""", filter.lastTime)
                .Take(pagination, x => x switch
                {
                    "id" => (@"g.""Id""", @"g.""Id"""),
                    "firstLevel" => (@"g.""FirstLevel""", @"g.""FirstLevel"""),
                    "lastLevel" => (@"g.""LastLevel""", @"g.""LastLevel"""),
                    _ => (@"g.""Id""", @"g.""Id""")
                }, @"g.""Id""");

            using var db = GetConnection();
            return await db.QueryAsync(sql.Query, sql.Params);
        }

        public async Task<int> GetGamesCount(SrGameFilter filter)
        {
            var sql = new SqlBuilder(@"
                SELECT COUNT(*) FROM ""RefutationGames"" as g
                LEFT JOIN ""SmartRollupCommitments"" AS ic ON ic.""Id"" = g.""InitiatorCommitmentId""
                LEFT JOIN ""SmartRollupCommitments"" AS oc ON oc.""Id"" = g.""OpponentCommitmentId""
                LEFT JOIN ""SmartRollupRefuteOps"" AS m ON m.""Id"" = g.""LastMoveId""")
                .FilterA(@"g.""Id""", filter.id)
                .FilterA(@"g.""SmartRollupId""", filter.rollup)
                .FilterA(@"g.""InitiatorId""", filter.initiator)
                .FilterA(@"g.""InitiatorCommitmentId""", filter.initiatorCommitment?.id)
                .FilterA(@"ic.""Hash""", filter.initiatorCommitment?.hash)
                .FilterA(@"g.""OpponentCommitmentId""", filter.opponentCommitment?.id)
                .FilterA(@"oc.""Hash""", filter.opponentCommitment?.hash)
                .FilterA(@"g.""FirstLevel""", filter.firstLevel)
                .FilterA(@"g.""FirstLevel""", filter.firstTime)
                .FilterA(@"g.""LastLevel""", filter.lastLevel)
                .FilterA(@"g.""LastLevel""", filter.lastTime);

            using var db = GetConnection();
            return await db.QueryFirstAsync<int>(sql.Query, sql.Params);
        }

        public async Task<IEnumerable<SrGame>> GetGames(SrGameFilter filter, Pagination pagination)
        {
            var rows = await QueryGamesAsync(filter, pagination);
            return rows.Select(row => new SrGame
            {
                Id = row.Id,
                Rollup = Accounts.GetAlias(row.SmartRollupId),
                Initiator = Accounts.GetAlias(row.InitiatorId),
                InitiatorCommitment = row.icId == null ? null : new()
                {
                    Id = row.icId,
                    Initiator = Accounts.GetAlias(row.icInitiatorId),
                    InboxLevel = row.icInboxLevel,
                    State = row.icState,
                    Hash = row.icHash,
                    Ticks = row.icTicks,
                    FirstLevel = row.icFirstLevel,
                    FirstTime = Times[row.icFirstLevel]
                },
                Opponent = Accounts.GetAlias(row.OpponentId),
                OpponentCommitment = row.ocId == null ? null : new()
                {
                    Id = row.ocId,
                    Initiator = Accounts.GetAlias(row.ocInitiatorId),
                    InboxLevel = row.ocInboxLevel,
                    State = row.ocState,
                    Hash = row.ocHash,
                    Ticks = row.ocTicks,
                    FirstLevel = row.ocFirstLevel,
                    FirstTime = Times[row.ocFirstLevel]
                },
                LastMove = row.mId == null ? null : new()
                {
                    Id = row.mId,
                    Level = row.mLevel,
                    Timestamp = row.mTimestamp,
                    Sender = Accounts.GetAlias(row.mSenderId),
                    Move = RefutationMoves.ToString((int)row.mMove),
                    GameStatus = RefutationGameStatuses.ToString((int)row.mGameStatus),
                },
                FirstLevel = row.FirstLevel,
                FirstTime = Times[row.FirstLevel],
                LastLevel = row.LastLevel,
                LastTime = Times[row.LastLevel],
                InitiatorLoss = row.InitiatorLoss,
                InitiatorReward = row.InitiatorReward,
                OpponentLoss = row.OpponentLoss,
                OpponentReward = row.OpponentReward
            });
        }

        public async Task<object[][]> GetGames(SrGameFilter filter, Pagination pagination, List<SelectionField> fields)
        {
            var rows = await QueryGamesAsync(filter, pagination, fields);

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
                    case "initiatorCommitment":
                        foreach (var row in rows)
                            result[j++][i] = row.icId == null ? null : new SrCommitmentInfo
                            {
                                Id = row.icId,
                                Initiator = Accounts.GetAlias(row.icInitiatorId),
                                InboxLevel = row.icInboxLevel,
                                State = row.icState,
                                Hash = row.icHash,
                                Ticks = row.icTicks,
                                FirstLevel = row.icFirstLevel,
                                FirstTime = Times[row.icFirstLevel],
                            };
                        break;
                    case "initiatorCommitment.id":
                        foreach (var row in rows)
                            result[j++][i] = row.icId;
                        break;
                    case "initiatorCommitment.initiator":
                        foreach (var row in rows)
                            result[j++][i] = row.icInitiatorId == null ? null : Accounts.GetAlias(row.icInitiatorId);
                        break;
                    case "initiatorCommitment.initiator.alias":
                        foreach (var row in rows)
                            result[j++][i] = row.icInitiatorId == null ? null : Accounts.GetAlias(row.icInitiatorId).Name;
                        break;
                    case "initiatorCommitment.initiator.address":
                        foreach (var row in rows)
                            result[j++][i] = row.icInitiatorId == null ? null : Accounts.GetAlias(row.icInitiatorId).Address;
                        break;
                    case "initiatorCommitment.inboxLevel":
                        foreach (var row in rows)
                            result[j++][i] = row.icInboxLevel;
                        break;
                    case "initiatorCommitment.state":
                        foreach (var row in rows)
                            result[j++][i] = row.icState;
                        break;
                    case "initiatorCommitment.hash":
                        foreach (var row in rows)
                            result[j++][i] = row.icHash;
                        break;
                    case "initiatorCommitment.ticks":
                        foreach (var row in rows)
                            result[j++][i] = row.icTicks;
                        break;
                    case "initiatorCommitment.firstLevel":
                        foreach (var row in rows)
                            result[j++][i] = row.icFirstLevel;
                        break;
                    case "initiatorCommitment.firstTime":
                        foreach (var row in rows)
                            result[j++][i] = row.icFirstLevel == null ? null : Times[row.icFirstLevel];
                        break;
                    case "opponent":
                        foreach (var row in rows)
                            result[j++][i] = Accounts.GetAlias(row.OpponentId);
                        break;
                    case "opponent.alias":
                        foreach (var row in rows)
                            result[j++][i] = Accounts.GetAlias(row.OpponentId).Name;
                        break;
                    case "opponent.address":
                        foreach (var row in rows)
                            result[j++][i] = Accounts.GetAlias(row.OpponentId).Address;
                        break;
                    case "opponentCommitment":
                        foreach (var row in rows)
                            result[j++][i] = row.ocId == null ? null : new SrCommitmentInfo
                            {
                                Id = row.ocId,
                                Initiator = Accounts.GetAlias(row.ocInitiatorId),
                                InboxLevel = row.ocInboxLevel,
                                State = row.ocState,
                                Hash = row.ocHash,
                                Ticks = row.ocTicks,
                                FirstLevel = row.ocFirstLevel,
                                FirstTime = Times[row.ocFirstLevel],
                            };
                        break;
                    case "opponentCommitment.id":
                        foreach (var row in rows)
                            result[j++][i] = row.ocId;
                        break;
                    case "opponentCommitment.initiator":
                        foreach (var row in rows)
                            result[j++][i] = row.ocInitiatorId == null ? null : Accounts.GetAlias(row.ocInitiatorId);
                        break;
                    case "opponentCommitment.initiator.alias":
                        foreach (var row in rows)
                            result[j++][i] = row.ocInitiatorId == null ? null : Accounts.GetAlias(row.ocInitiatorId).Name;
                        break;
                    case "opponentCommitment.initiator.address":
                        foreach (var row in rows)
                            result[j++][i] = row.ocInitiatorId == null ? null : Accounts.GetAlias(row.ocInitiatorId).Address;
                        break;
                    case "opponentCommitment.inboxLevel":
                        foreach (var row in rows)
                            result[j++][i] = row.ocInboxLevel;
                        break;
                    case "opponentCommitment.state":
                        foreach (var row in rows)
                            result[j++][i] = row.ocState;
                        break;
                    case "opponentCommitment.hash":
                        foreach (var row in rows)
                            result[j++][i] = row.ocHash;
                        break;
                    case "opponentCommitment.ticks":
                        foreach (var row in rows)
                            result[j++][i] = row.ocTicks;
                        break;
                    case "opponentCommitment.firstLevel":
                        foreach (var row in rows)
                            result[j++][i] = row.ocFirstLevel;
                        break;
                    case "opponentCommitment.firstTime":
                        foreach (var row in rows)
                            result[j++][i] = row.ocFirstLevel == null ? null : Times[row.ocFirstLevel];
                        break;
                    case "lastMove":
                        foreach (var row in rows)
                            result[j++][i] = row.mId == null ? null : new SrGameMove
                            {
                                Id = row.mId,
                                Level = row.mLevel,
                                Timestamp = row.mTimestamp,
                                Sender = Accounts.GetAlias(row.mSenderId),
                                Move = RefutationMoves.ToString((int)row.mMove),
                                GameStatus = RefutationGameStatuses.ToString((int)row.mGameStatus),
                            };
                        break;
                    case "lastMove.id":
                        foreach (var row in rows)
                            result[j++][i] = row.mId;
                        break;
                    case "lastMove.level":
                        foreach (var row in rows)
                            result[j++][i] = row.mLevel;
                        break;
                    case "lastMove.timestamp":
                        foreach (var row in rows)
                            result[j++][i] = row.mTimestamp;
                        break;
                    case "lastMove.sender":
                        foreach (var row in rows)
                            result[j++][i] = Accounts.GetAlias(row.mSenderId);
                        break;
                    case "lastMove.sender.address":
                        foreach (var row in rows)
                            result[j++][i] = Accounts.GetAlias((int)row.mSenderId).Address;
                        break;
                    case "lastMove.sender.alias":
                        foreach (var row in rows)
                            result[j++][i] = Accounts.GetAlias((int)row.mSenderId).Name;
                        break;
                    case "lastMove.move":
                        foreach (var row in rows)
                            result[j++][i] = RefutationMoves.ToString((int)row.mMove);
                        break;
                    case "lastMove.gameStatus":
                        foreach (var row in rows)
                            result[j++][i] = RefutationGameStatuses.ToString((int)row.mGameStatus);
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
                    case "initiatorReward":
                        foreach (var row in rows)
                            result[j++][i] = row.InitiatorReward;
                        break;
                    case "initiatorLoss":
                        foreach (var row in rows)
                            result[j++][i] = row.InitiatorLoss;
                        break;
                    case "opponentReward":
                        foreach (var row in rows)
                            result[j++][i] = row.OpponentReward;
                        break;
                    case "opponentLoss":
                        foreach (var row in rows)
                            result[j++][i] = row.OpponentLoss;
                        break;
                }
            }

            return result;
        }
        #endregion

        #region inbox
        async Task<IEnumerable<dynamic>> QueryInboxMessagesAsync(SrMessageFilter filter, Pagination pagination, MichelineFormat micheline, List<SelectionField> fields = null)
        {
            var select = """
                m."Id",
                m."Level",
                m."Type",
                m."Payload",

                p."Hash" as "pHash",
                p."Timestamp" as "pTimestamp",
                
                t."InitiatorId" as "tInitiatorId",
                t."SenderId" as "tSenderId",
                t."TargetId" as "tTargetId",
                t."Entrypoint" as "tEntrypoint",
                t."RawParameters" as "tRawParameters",
                t."JsonParameters" as "tJsonParameters"
            """;

            if (fields != null)
            {
                var columns = new HashSet<string>(fields.Count);
                foreach (var field in fields)
                {
                    switch (field.Field)
                    {
                        case "id": columns.Add(@"m.""Id"""); break;
                        case "level": columns.Add(@"m.""Level"""); break;
                        case "timestamp": columns.Add(@"m.""Level"""); break;
                        case "type": columns.Add(@"m.""Type"""); break;
                        case "payload": columns.Add(@"m.""Payload"""); break;

                        case "predecessorHash": columns.Add(@"p.""Hash"" as ""pHash"""); break;
                        case "predecessorTimestamp": columns.Add(@"p.""Timestamp"" as ""pTimestamp"""); break;

                        case "initiator": columns.Add(@"t.""InitiatorId"" as ""tInitiatorId"""); break;
                        case "sender": columns.Add(@"t.""SenderId"" as ""tSenderId"""); break;
                        case "target": columns.Add(@"t.""TargetId"" as ""tTargetId"""); break;
                        case "entrypoint": columns.Add(@"t.""Entrypoint"" as ""tEntrypoint"""); break;
                        case "parameter":
                            columns.Add(micheline < MichelineFormat.RawString
                                ? @"t.""JsonParameters"" as ""tJsonParameters"""
                                : @"t.""RawParameters"" as ""tRawParameters""");
                            break;
                    }
                }

                if (columns.Count == 0)
                    return Enumerable.Empty<dynamic>();

                select = string.Join(',', columns);
            }

            var sql = new SqlBuilder($@"
                SELECT {select} FROM ""InboxMessages"" as m
                LEFT JOIN ""Blocks"" AS p ON p.""Level"" = m.""PredecessorLevel""
                LEFT JOIN ""TransactionOps"" AS t ON t.""Id"" = m.""OperationId""")
                .FilterA(@"m.""Id""", filter.id)
                .FilterA(@"m.""Level""", filter.level)
                .FilterA(@"m.""Level""", filter.timestamp)
                .FilterA(@"m.""Type""", filter.type)
                .Take(pagination, x => x switch
                {
                    "id" => (@"m.""Id""", @"m.""Id"""),
                    "level" => (@"m.""Level""", @"m.""Level"""),
                    _ => (@"m.""Id""", @"m.""Id""")
                }, @"m.""Id""");

            using var db = GetConnection();
            return await db.QueryAsync(sql.Query, sql.Params);
        }

        public async Task<int> GetInboxMessagesCount(SrMessageFilter filter)
        {
            var sql = new SqlBuilder(@"
                SELECT COUNT(*) FROM ""InboxMessages"" as m")
                .FilterA(@"m.""Id""", filter.id)
                .FilterA(@"m.""Level""", filter.level)
                .FilterA(@"m.""Level""", filter.timestamp)
                .FilterA(@"m.""Type""", filter.type);

            using var db = GetConnection();
            return await db.QueryFirstAsync<int>(sql.Query, sql.Params);
        }

        public async Task<IEnumerable<SrMessage>> GetInboxMessages(SrMessageFilter filter, Pagination pagination, MichelineFormat micheline)
        {
            var rows = await QueryInboxMessagesAsync(filter, pagination, micheline);
            return rows.Select(row => new SrMessage
            {
                Id = row.Id,
                Level = row.Level,
                Timestamp = Times[(int)row.Level],
                Type = SrMessageTypes.ToString((int)row.Type),
                PredecessorHash = row.pHash,
                PredecessorTimestamp = row.pTimestamp,
                Initiator = row.tInitiatorId is int initiatorId ? Accounts.GetAlias(initiatorId) : null,
                Sender = row.tSenderId is int senderId ? Accounts.GetAlias(senderId) : null,
                Target = row.tTargetId is int targetId ? Accounts.GetAlias(targetId) : null,
                Entrypoint = row.tEntrypoint,
                Parameter = micheline switch
                {
                    MichelineFormat.Json => (RawJson)row.tJsonParameters,
                    MichelineFormat.JsonString => row.tJsonParameters,
                    MichelineFormat.Raw => row.tRawParameters == null ? null : (RawJson)Micheline.ToJson(row.tRawParameters),
                    MichelineFormat.RawString => row.tRawParameters == null ? null : Micheline.ToJson(row.tRawParameters),
                    _ => throw new Exception("Invalid MichelineFormat value")
                },
                Payload = row.Payload,
            });
        }

        public async Task<object[][]> GetInboxMessages(SrMessageFilter filter, Pagination pagination, MichelineFormat micheline, List<SelectionField> fields)
        {
            var rows = await QueryInboxMessagesAsync(filter, pagination, micheline, fields);

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
                            result[j++][i] = Times[(int)row.Level];
                        break;
                    case "type":
                        foreach (var row in rows)
                            result[j++][i] = SrMessageTypes.ToString((int)row.Type);
                        break;
                    case "predecessorHash":
                        foreach (var row in rows)
                            result[j++][i] = row.pHash;
                        break;
                    case "predecessorTimestamp":
                        foreach (var row in rows)
                            result[j++][i] = row.pTimestamp;
                        break;
                    case "initiator":
                        foreach (var row in rows)
                            result[j++][i] = row.tInitiatorId is int initiatorId ? Accounts.GetAlias(initiatorId) : null;
                        break;
                    case "initiator.alias":
                        foreach (var row in rows)
                            result[j++][i] = row.tInitiatorId is int initiatorId ? Accounts.GetAlias(initiatorId).Name : null;
                        break;
                    case "initiator.address":
                        foreach (var row in rows)
                            result[j++][i] = row.tInitiatorId is int initiatorId ? Accounts.GetAlias(initiatorId).Address : null;
                        break;
                    case "sender":
                        foreach (var row in rows)
                            result[j++][i] = row.tSenderId is int senderId ? Accounts.GetAlias(senderId) : null;
                        break;
                    case "sender.alias":
                        foreach (var row in rows)
                            result[j++][i] = row.tSenderId is int senderId ? Accounts.GetAlias(senderId).Name : null;
                        break;
                    case "sender.address":
                        foreach (var row in rows)
                            result[j++][i] = row.tSenderId is int senderId ? Accounts.GetAlias(senderId).Address : null;
                        break;
                    case "target":
                        foreach (var row in rows)
                            result[j++][i] = row.tTargetId is int targetId ? Accounts.GetAlias(targetId) : null;
                        break;
                    case "target.alias":
                        foreach (var row in rows)
                            result[j++][i] = row.tTargetId is int targetId ? Accounts.GetAlias(targetId).Name : null;
                        break;
                    case "target.address":
                        foreach (var row in rows)
                            result[j++][i] = row.tTargetId is int targetId ? Accounts.GetAlias(targetId).Address : null;
                        break;
                    case "entrypoint":
                        foreach (var row in rows)
                            result[j++][i] = row.tEntrypoint;
                        break;
                    case "parameter":
                        foreach (var row in rows)
                            result[j++][i] = micheline switch
                            {
                                MichelineFormat.Json => (RawJson)row.tJsonParameters,
                                MichelineFormat.JsonString => row.tJsonParameters,
                                MichelineFormat.Raw => row.tRawParameters == null ? null : (RawJson)Micheline.ToJson(row.tRawParameters),
                                MichelineFormat.RawString => row.tRawParameters == null ? null : Micheline.ToJson(row.tRawParameters),
                                _ => throw new Exception("Invalid MichelineFormat value")
                            };
                        break;
                    case "payload":
                        foreach (var row in rows)
                            result[j++][i] = row.Payload;
                        break;
                }
            }

            return result;
        }
        #endregion
    }
}
