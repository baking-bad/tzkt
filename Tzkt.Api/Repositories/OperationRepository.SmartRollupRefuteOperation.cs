using Dapper;
using Tzkt.Api.Models;
using Tzkt.Data;

namespace Tzkt.Api.Repositories
{
    public partial class OperationRepository : DbConnection
    {
        public async Task<bool?> GetSmartRollupRefuteStatus(string hash)
        {
            using var db = GetConnection();
            return await GetStatus(db, nameof(TzktContext.SmartRollupRefuteOps), hash);
        }

        public async Task<int> GetSmartRollupRefuteOpsCount(SrRefuteOperationFilter filter)
        {
            var sql = new SqlBuilder(@"
                SELECT COUNT(*) FROM ""SmartRollupRefuteOps"" AS o
                LEFT JOIN ""RefutationGames"" AS g ON g.""Id"" = o.""GameId""
                LEFT JOIN ""SmartRollupCommitments"" AS ic ON ic.""Id"" = g.""InitiatorCommitmentId""
                LEFT JOIN ""SmartRollupCommitments"" AS oc ON oc.""Id"" = g.""OpponentCommitmentId""")
                .FilterA(@"o.""Id""", filter.id)
                .FilterA(@"o.""OpHash""", filter.hash)
                .FilterA(@"o.""Counter""", filter.counter)
                .FilterA(@"o.""Level""", filter.level)
                .FilterA(@"o.""Level""", filter.timestamp)
                .FilterA(@"o.""SenderId""", filter.sender)
                .FilterA(@"o.""Status""", filter.status)
                .FilterA(@"o.""SmartRollupId""", filter.rollup)
                .FilterA(filter.anyof, x => x switch
                {
                    "sender" => @"o.""SenderId""",
                    "initiator" => @"g.""InitiatorId""",
                    _ => @"g.""OpponentId"""
                })
                .FilterA(@"o.""GameId""", filter.game?.id)
                .FilterA(@"g.""InitiatorId""", filter.game?.initiator)
                .FilterA(@"g.""InitiatorCommitmentId""", filter.game?.initiatorCommitment?.id)
                .FilterA(@"ic.""Hash""", filter.game?.initiatorCommitment?.hash)
                .FilterA(@"g.""OpponentId""", filter.game?.opponent)
                .FilterA(@"g.""OpponentCommitmentId""", filter.game?.opponentCommitment?.id)
                .FilterA(@"oc.""Hash""", filter.game?.opponentCommitment?.hash)
                .FilterA(@"o.""Move""", filter.move)
                .FilterA(@"o.""GameStatus""", filter.gameStatus);

            using var db = GetConnection();
            return await db.QueryFirstAsync<int>(sql.Query, sql.Params);
        }

        async Task<IEnumerable<dynamic>> QuerySmartRollupRefuteOps(SrRefuteOperationFilter filter, Pagination pagination, List<SelectionField> fields = null)
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
                o."GameId" as "gId",
                o."Move",
                o."GameStatus",
                o."Errors",
                o."Level",

                g."InitiatorId" as "gInitiatorId",
                g."InitiatorCommitmentId" as "icId",
                g."OpponentId" as "gOpponentId",
                g."OpponentCommitmentId" as "ocId",
                g."InitiatorReward" as "gInitiatorReward",
                g."InitiatorLoss" as "gInitiatorLoss",
                g."OpponentReward" as "gOpponentReward",
                g."OpponentLoss" as "gOpponentLoss",

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
                oc."FirstLevel" as "ocFirstLevel"
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
                        case "game":
                            if (field.Path == null)
                            {
                                columns.Add(@"o.""GameStatus""");
                                columns.Add(@"o.""GameId"" as ""gId""");
                                columns.Add(@"g.""InitiatorId"" as ""gInitiatorId""");
                                columns.Add(@"g.""InitiatorCommitmentId"" as ""icId""");
                                columns.Add(@"g.""OpponentId"" as ""gOpponentId""");
                                columns.Add(@"g.""OpponentCommitmentId"" as ""ocId""");
                                columns.Add(@"g.""InitiatorReward"" as ""gInitiatorReward""");
                                columns.Add(@"g.""InitiatorLoss"" as ""gInitiatorLoss""");
                                columns.Add(@"g.""OpponentReward"" as ""gOpponentReward""");
                                columns.Add(@"g.""OpponentLoss"" as ""gOpponentLoss""");
                                columns.Add(@"ic.""InitiatorId"" as ""icInitiatorId""");
                                columns.Add(@"ic.""InboxLevel"" as ""icInboxLevel""");
                                columns.Add(@"ic.""State"" as ""icState""");
                                columns.Add(@"ic.""Hash"" as ""icHash""");
                                columns.Add(@"ic.""Ticks"" as ""icTicks""");
                                columns.Add(@"ic.""FirstLevel"" as ""icFirstLevel""");
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
                                    case "id": columns.Add(@"o.""GameId"" as ""gId"""); break;
                                    case "initiator": columns.Add(@"g.""InitiatorId"" as ""gInitiatorId"""); break;
                                    case "initiatorCommitment":
                                        if (field.SubField().Path == null)
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
                                            switch (field.SubField().SubField().Field)
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
                                    case "opponent": columns.Add(@"g.""OpponentId"" as ""gOpponentId"""); break;
                                    case "opponentCommitment":
                                        if (field.SubField().Path == null)
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
                                            switch (field.SubField().SubField().Field)
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
                                    case "initiatorReward":
                                        columns.Add(@"o.""GameStatus""");
                                        columns.Add(@"g.""InitiatorReward"" as ""gInitiatorReward""");
                                        break;
                                    case "initiatorLoss":
                                        columns.Add(@"o.""GameStatus""");
                                        columns.Add(@"g.""InitiatorLoss"" as ""gInitiatorLoss""");
                                        break;
                                    case "opponentReward":
                                        columns.Add(@"o.""GameStatus""");
                                        columns.Add(@"g.""OpponentReward"" as ""gOpponentReward""");
                                        break;
                                    case "opponentLoss":
                                        columns.Add(@"o.""GameStatus""");
                                        columns.Add(@"g.""OpponentLoss"" as ""gOpponentLoss""");
                                        break;
                                }
                            }
                            break;
                        case "move": columns.Add(@"o.""Move"""); break;
                        case "gameStatus": columns.Add(@"o.""GameStatus"""); break;
                        case "errors": columns.Add(@"o.""Errors"""); break;
                        case "quote": columns.Add(@"o.""Level"""); break;
                    }
                }

                if (columns.Count == 0)
                    return Enumerable.Empty<dynamic>();

                select = string.Join(',', columns);
            }

            var sql = new SqlBuilder($@"
                SELECT {select} FROM ""SmartRollupRefuteOps"" as o
                LEFT JOIN ""RefutationGames"" AS g ON g.""Id"" = o.""GameId""
                LEFT JOIN ""SmartRollupCommitments"" AS ic ON ic.""Id"" = g.""InitiatorCommitmentId""
                LEFT JOIN ""SmartRollupCommitments"" AS oc ON oc.""Id"" = g.""OpponentCommitmentId""")
                .FilterA(@"o.""Id""", filter.id)
                .FilterA(@"o.""OpHash""", filter.hash)
                .FilterA(@"o.""Counter""", filter.counter)
                .FilterA(@"o.""Level""", filter.level)
                .FilterA(@"o.""Level""", filter.timestamp)
                .FilterA(@"o.""SenderId""", filter.sender)
                .FilterA(@"o.""Status""", filter.status)
                .FilterA(@"o.""SmartRollupId""", filter.rollup)
                .FilterA(filter.anyof, x => x switch
                {
                    "sender" => @"o.""SenderId""",
                    "initiator" => @"g.""InitiatorId""",
                    _ => @"g.""OpponentId"""
                })
                .FilterA(@"o.""GameId""", filter.game?.id)
                .FilterA(@"g.""InitiatorId""", filter.game?.initiator)
                .FilterA(@"g.""InitiatorCommitmentId""", filter.game?.initiatorCommitment?.id)
                .FilterA(@"ic.""Hash""", filter.game?.initiatorCommitment?.hash)
                .FilterA(@"g.""OpponentId""", filter.game?.opponent)
                .FilterA(@"g.""OpponentCommitmentId""", filter.game?.opponentCommitment?.id)
                .FilterA(@"oc.""Hash""", filter.game?.opponentCommitment?.hash)
                .FilterA(@"o.""Move""", filter.move)
                .FilterA(@"o.""GameStatus""", filter.gameStatus)
                .Take(pagination, x => (@"o.""Id""", @"o.""Id"""), @"o.""Id""");

            using var db = GetConnection();
            return await db.QueryAsync(sql.Query, sql.Params);
        }

        public async Task<IEnumerable<SmartRollupRefuteOperation>> GetSmartRollupRefuteOps(SrRefuteOperationFilter filter, Pagination pagination, Symbols quote)
        {
            var rows = await QuerySmartRollupRefuteOps(filter, pagination);
            return rows.Select(row => new SmartRollupRefuteOperation
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
                Rollup = Accounts.GetAlias(row.SmartRollupId),
                Game = row.gId == null ? null : new()
                {
                    Id = row.gId,
                    Initiator = Accounts.GetAlias(row.gInitiatorId),
                    InitiatorCommitment = new()
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
                    Opponent = Accounts.GetAlias(row.gOpponentId),
                    OpponentCommitment = new()
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
                    InitiatorReward = RefutationGameStatuses.IsEnd((int)row.GameStatus) ? row.gInitiatorReward : null,
                    InitiatorLoss = RefutationGameStatuses.IsEnd((int)row.GameStatus) ? row.gInitiatorLoss : null,
                    OpponentReward = RefutationGameStatuses.IsEnd((int)row.GameStatus) ? row.gOpponentReward : null,
                    OpponentLoss = RefutationGameStatuses.IsEnd((int)row.GameStatus) ? row.gOpponentLoss : null
                },
                Move = RefutationMoves.ToString((int)row.Move),
                GameStatus = RefutationGameStatuses.ToString((int)row.GameStatus),
                Errors = row.Errors != null ? OperationErrorSerializer.Deserialize(row.Errors) : null,
                Quote = Quotes.Get(quote, row.Level)
            });
        }

        public async Task<object[][]> GetSmartRollupRefuteOps(SrRefuteOperationFilter filter, Pagination pagination, List<SelectionField> fields, Symbols quote)
        {
            var rows = await QuerySmartRollupRefuteOps(filter, pagination, fields);

            var result = new object[rows.Count()][];
            for (int i = 0; i < result.Length; i++)
                result[i] = new object[fields.Count];

            for (int i = 0, j = 0; i < fields.Count; j = 0, i++)
            {
                switch (fields[i].Full)
                {
                    case "type":
                        foreach (var row in rows)
                            result[j++][i] = OpTypes.SmartRollupRefute;
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
                    case "game":
                        foreach (var row in rows)
                            result[j++][i] = row.gId == null ? null : new SrGame
                            {
                                Id = row.gId,
                                Initiator = Accounts.GetAlias(row.gInitiatorId),
                                InitiatorCommitment = new()
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
                                Opponent = Accounts.GetAlias(row.gOpponentId),
                                OpponentCommitment = new()
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
                                InitiatorReward = RefutationGameStatuses.IsEnd((int)row.GameStatus) ? row.gInitiatorReward : null,
                                InitiatorLoss = RefutationGameStatuses.IsEnd((int)row.GameStatus) ? row.gInitiatorLoss : null,
                                OpponentReward = RefutationGameStatuses.IsEnd((int)row.GameStatus) ? row.gOpponentReward : null,
                                OpponentLoss = RefutationGameStatuses.IsEnd((int)row.GameStatus) ? row.gOpponentLoss : null
                            };
                        break;
                    case "game.id":
                        foreach (var row in rows)
                            result[j++][i] = row.gId;
                        break;
                    case "game.initiator":
                        foreach (var row in rows)
                            result[j++][i] = row.gInitiatorId == null ? null : Accounts.GetAlias(row.gInitiatorId);
                        break;
                    case "game.initiator.address":
                        foreach (var row in rows)
                            result[j++][i] = row.gInitiatorId == null ? null : Accounts.GetAlias((int)row.gInitiatorId).Address;
                        break;
                    case "game.initiator.alias":
                        foreach (var row in rows)
                            result[j++][i] = row.gInitiatorId == null ? null : Accounts.GetAlias((int)row.gInitiatorId).Name;
                        break;
                    case "game.initiatorCommitment":
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
                    case "game.initiatorCommitment.id":
                        foreach (var row in rows)
                            result[j++][i] = row.icId;
                        break;
                    case "game.initiatorCommitment.initiator":
                        foreach (var row in rows)
                            result[j++][i] = row.icInitiatorId == null ? null : Accounts.GetAlias(row.icInitiatorId);
                        break;
                    case "game.initiatorCommitment.initiator.alias":
                        foreach (var row in rows)
                            result[j++][i] = row.icInitiatorId == null ? null : Accounts.GetAlias(row.icInitiatorId).Name;
                        break;
                    case "game.initiatorCommitment.initiator.address":
                        foreach (var row in rows)
                            result[j++][i] = row.icInitiatorId == null ? null : Accounts.GetAlias(row.icInitiatorId).Address;
                        break;
                    case "game.initiatorCommitment.inboxLevel":
                        foreach (var row in rows)
                            result[j++][i] = row.icInboxLevel;
                        break;
                    case "game.initiatorCommitment.state":
                        foreach (var row in rows)
                            result[j++][i] = row.icState;
                        break;
                    case "game.initiatorCommitment.hash":
                        foreach (var row in rows)
                            result[j++][i] = row.icHash;
                        break;
                    case "game.initiatorCommitment.ticks":
                        foreach (var row in rows)
                            result[j++][i] = row.icTicks;
                        break;
                    case "game.initiatorCommitment.firstLevel":
                        foreach (var row in rows)
                            result[j++][i] = row.icFirstLevel;
                        break;
                    case "game.initiatorCommitment.firstTime":
                        foreach (var row in rows)
                            result[j++][i] = row.icFirstLevel == null ? null : Times[row.icFirstLevel];
                        break;
                    case "game.opponent":
                        foreach (var row in rows)
                            result[j++][i] = row.gOpponentId == null ? null : Accounts.GetAlias(row.gOpponentId);
                        break;
                    case "game.opponent.address":
                        foreach (var row in rows)
                            result[j++][i] = row.gOpponentId == null ? null : Accounts.GetAlias((int)row.gOpponentId).Address;
                        break;
                    case "game.opponent.alias":
                        foreach (var row in rows)
                            result[j++][i] = row.gOpponentId == null ? null : Accounts.GetAlias((int)row.gOpponentId).Name;
                        break;
                    case "game.opponentCommitment":
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
                    case "game.opponentCommitment.id":
                        foreach (var row in rows)
                            result[j++][i] = row.ocId;
                        break;
                    case "game.opponentCommitment.initiator":
                        foreach (var row in rows)
                            result[j++][i] = row.ocInitiatorId == null ? null : Accounts.GetAlias(row.ocInitiatorId);
                        break;
                    case "game.opponentCommitment.initiator.alias":
                        foreach (var row in rows)
                            result[j++][i] = row.ocInitiatorId == null ? null : Accounts.GetAlias(row.ocInitiatorId).Name;
                        break;
                    case "game.opponentCommitment.initiator.address":
                        foreach (var row in rows)
                            result[j++][i] = row.ocInitiatorId == null ? null : Accounts.GetAlias(row.ocInitiatorId).Address;
                        break;
                    case "game.opponentCommitment.inboxLevel":
                        foreach (var row in rows)
                            result[j++][i] = row.ocInboxLevel;
                        break;
                    case "game.opponentCommitment.state":
                        foreach (var row in rows)
                            result[j++][i] = row.ocState;
                        break;
                    case "game.opponentCommitment.hash":
                        foreach (var row in rows)
                            result[j++][i] = row.ocHash;
                        break;
                    case "game.opponentCommitment.ticks":
                        foreach (var row in rows)
                            result[j++][i] = row.ocTicks;
                        break;
                    case "game.opponentCommitment.firstLevel":
                        foreach (var row in rows)
                            result[j++][i] = row.ocFirstLevel;
                        break;
                    case "game.opponentCommitment.firstTime":
                        foreach (var row in rows)
                            result[j++][i] = row.ocFirstLevel == null ? null : Times[row.ocFirstLevel];
                        break;
                    case "game.initiatorReward":
                        foreach (var row in rows)
                            result[j++][i] = RefutationGameStatuses.IsEnd((int)row.GameStatus) ? row.gInitiatorReward : null;
                        break;
                    case "game.initiatorLoss":
                        foreach (var row in rows)
                            result[j++][i] = RefutationGameStatuses.IsEnd((int)row.GameStatus) ? row.gInitiatorLoss : null;
                        break;
                    case "game.opponentReward":
                        foreach (var row in rows)
                            result[j++][i] = RefutationGameStatuses.IsEnd((int)row.GameStatus) ? row.gOpponentReward : null;
                        break;
                    case "game.opponentLoss":
                        foreach (var row in rows)
                            result[j++][i] = RefutationGameStatuses.IsEnd((int)row.GameStatus) ? row.gOpponentLoss : null;
                        break;
                    case "move":
                        foreach (var row in rows)
                            result[j++][i] = RefutationMoves.ToString((int)row.Move);
                        break;
                    case "gameStatus":
                        foreach (var row in rows)
                            result[j++][i] = RefutationGameStatuses.ToString((int)row.GameStatus);
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
