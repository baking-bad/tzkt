﻿using Dapper;
using Mvkt.Api.Models;
using Mvkt.Api.Services.Cache;

namespace Mvkt.Api.Repositories
{
    public class BlockRepository : DbConnection
    {
        readonly AccountsCache Accounts;
        readonly OperationRepository Operations;
        readonly QuotesCache Quotes;
        readonly StateCache State;
        readonly SoftwareCache Software;

        public BlockRepository(AccountsCache accounts, OperationRepository operations, QuotesCache quotes, StateCache state, SoftwareCache software, IConfiguration config) : base(config)
        {
            Accounts = accounts;
            Operations = operations;
            Quotes = quotes;
            State = state;
            Software = software;
        }

        public Task<int> GetCount()
        {
            return Task.FromResult(State.Current.Level + 1);
        }

        public async Task<Block> Get(int level, bool operations, MichelineFormat format, Symbols quote)
        {
            var sql = """
                SELECT  *
                FROM    "Blocks"
                WHERE   "Level" = @level
                LIMIT   1
                """;

            using var db = GetConnection();
            var row = await db.QueryFirstOrDefaultAsync(sql, new { level });
            if (row == null) return null;

            var block = new Block
            {
                Cycle = row.Cycle,
                Level = level,
                Hash = row.Hash,
                Timestamp = row.Timestamp,
                Proto = row.ProtoCode,
                PayloadRound = row.PayloadRound,
                BlockRound = row.BlockRound,
                Validations = row.Validations,
                Deposit = row.Deposit,
                RewardLiquid = row.RewardLiquid,
                RewardStakedOwn = row.RewardStakedOwn,
                RewardStakedShared = row.RewardStakedShared,
                BonusLiquid = row.BonusLiquid,
                BonusStakedOwn = row.BonusStakedOwn,
                BonusStakedShared = row.BonusStakedShared,
                Fees = row.Fees,
                NonceRevealed = row.RevelationId != null,
                Proposer = row.ProposerId != null ? await Accounts.GetAliasAsync(row.ProposerId) : null,
                Producer = row.ProducerId != null ? await Accounts.GetAliasAsync(row.ProducerId) : null,
                Software = row.SoftwareId != null ? Software[row.SoftwareId] : null,
                LBToggle = row.LBToggle,
                LBToggleEma = row.LBToggleEma,
                AIToggle = row.AIToggle,
                AIToggleEma = row.AIToggleEma,
                Quote = Quotes.Get(quote, level)
            };

            if (operations)
                await LoadOperations(block, (Data.Models.Operations)row.Operations, format, quote);

            return block;
        }

        public async Task<Block> Get(string hash, bool operations, MichelineFormat format, Symbols quote)
        {
            var sql = """
                SELECT  *
                FROM    "Blocks"
                WHERE   "Hash" = @hash::character(51)
                LIMIT   1
                """;

            using var db = GetConnection();
            var row = await db.QueryFirstOrDefaultAsync(sql, new { hash });
            if (row == null) return null;

            var block = new Block
            {
                Cycle = row.Cycle,
                Level = row.Level,
                Hash = hash,
                Timestamp = row.Timestamp,
                Proto = row.ProtoCode,
                PayloadRound = row.PayloadRound,
                BlockRound = row.BlockRound,
                Validations = row.Validations,
                Deposit = row.Deposit,
                RewardLiquid = row.RewardLiquid,
                RewardStakedOwn = row.RewardStakedOwn,
                RewardStakedShared = row.RewardStakedShared,
                BonusLiquid = row.BonusLiquid,
                BonusStakedOwn = row.BonusStakedOwn,
                BonusStakedShared = row.BonusStakedShared,
                Fees = row.Fees,
                NonceRevealed = row.RevelationId != null,
                Proposer = row.ProposerId != null ? await Accounts.GetAliasAsync(row.ProposerId) : null,
                Producer = row.ProducerId != null ? await Accounts.GetAliasAsync(row.ProducerId) : null,
                Software = row.SoftwareId != null ? Software[row.SoftwareId] : null,
                LBToggle = row.LBToggle,
                LBToggleEma = row.LBToggleEma,
                AIToggle = row.AIToggle,
                AIToggleEma = row.AIToggleEma,
                Quote = Quotes.Get(quote, row.Level)
            };

            if (operations)
                await LoadOperations(block, (Data.Models.Operations)row.Operations, format, quote);

            return block;
        }

        public async Task<IEnumerable<Block>> Get(
            AnyOfParameter anyof,
            AccountParameter proposer,
            AccountParameter producer,
            Int32Parameter level,
            DateTimeParameter timestamp,
            Int32Parameter blockRound,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            Symbols quote)
        {
            var sql = new SqlBuilder(@"SELECT * FROM ""Blocks""")
                .Filter(anyof, x => x == "proposer" ? "ProposerId" : "ProducerId")
                .Filter("ProposerId", proposer)
                .Filter("ProducerId", producer)
                .Filter("Level", level)
                .Filter("Timestamp", timestamp)
                .Filter("BlockRound", blockRound)
                .Take(sort, offset, limit, x => x switch
                {
                    "level" => ("Level", "Level"),
                    "payloadRound" => ("PayloadRound", "PayloadRound"),
                    "blockRound" => ("BlockRound", "BlockRound"),
                    "validations" => ("Validations", "Validations"),
                    "reward" => ("RewardLiquid", "RewardLiquid"),
                    "rewardLiquid" => ("RewardLiquid", "RewardLiquid"),
                    "rewardStakedOwn" => ("RewardStakedOwn", "RewardStakedOwn"),
                    "rewardStakedShared" => ("RewardStakedShared", "RewardStakedShared"),
                    "bonus" => ("BonusLiquid", "BonusLiquid"),
                    "bonusLiquid" => ("BonusLiquid", "BonusLiquid"),
                    "bonusStakedOwn" => ("BonusStakedOwn", "BonusStakedOwn"),
                    "bonusStakedShared" => ("BonusStakedShared", "BonusStakedShared"),
                    "fees" => ("Fees", "Fees"),
                    _ => ("Id", "Id")
                });

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            return rows.Select(row => new Block
            {
                Cycle = row.Cycle,
                Level = row.Level,
                Hash = row.Hash,
                Timestamp = row.Timestamp,
                Proto = row.ProtoCode,
                PayloadRound = row.PayloadRound,
                BlockRound = row.BlockRound,
                Validations = row.Validations,
                Deposit = row.Deposit,
                RewardLiquid = row.RewardLiquid,
                RewardStakedOwn = row.RewardStakedOwn,
                RewardStakedShared = row.RewardStakedShared,
                BonusLiquid = row.BonusLiquid,
                BonusStakedOwn = row.BonusStakedOwn,
                BonusStakedShared = row.BonusStakedShared,
                Fees = row.Fees,
                NonceRevealed = row.RevelationId != null,
                Proposer = row.ProposerId != null ? Accounts.GetAlias(row.ProposerId) : null,
                Producer = row.ProducerId != null ? Accounts.GetAlias(row.ProducerId) : null,
                Software = row.SoftwareId != null ? Software[row.SoftwareId] : null,
                LBToggle = row.LBToggle,
                LBToggleEma = row.LBToggleEma,
                AIToggle = row.AIToggle,
                AIToggleEma = row.AIToggleEma,
                Quote = Quotes.Get(quote, row.Level)
            });
        }

        public async Task<object[][]> Get(
            AnyOfParameter anyof,
            AccountParameter proposer,
            AccountParameter producer,
            Int32Parameter level,
            DateTimeParameter timestamp,
            Int32Parameter blockRound,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            string[] fields,
            Symbols quote)
        {
            var columns = new HashSet<string>(fields.Length);
            foreach (var field in fields)
            {
                switch (field)
                {
                    case "cycle": columns.Add(@"""Cycle"""); break;
                    case "level": columns.Add(@"""Level"""); break;
                    case "hash": columns.Add(@"""Hash"""); break;
                    case "timestamp": columns.Add(@"""Timestamp"""); break;
                    case "proto": columns.Add(@"""ProtoCode"""); break;
                    case "payloadRound": columns.Add(@"""PayloadRound"""); break;
                    case "blockRound": columns.Add(@"""BlockRound"""); break;
                    case "validations": columns.Add(@"""Validations"""); break;
                    case "deposit": columns.Add(@"""Deposit"""); break;
                    case "rewardLiquid": columns.Add(@"""RewardLiquid"""); break;
                    case "rewardStakedOwn": columns.Add(@"""RewardStakedOwn"""); break;
                    case "rewardStakedShared": columns.Add(@"""RewardStakedShared"""); break;
                    case "bonusLiquid": columns.Add(@"""BonusLiquid"""); break;
                    case "bonusStakedOwn": columns.Add(@"""BonusStakedOwn"""); break;
                    case "bonusStakedShared": columns.Add(@"""BonusStakedShared"""); break;
                    case "fees": columns.Add(@"""Fees"""); break;
                    case "nonceRevealed": columns.Add(@"""RevelationId"""); break;
                    case "proposer": columns.Add(@"""ProposerId"""); break;
                    case "producer": columns.Add(@"""ProducerId"""); break;
                    case "software": columns.Add(@"""SoftwareId"""); break;
                    case "quote": columns.Add(@"""Level"""); break;
                    case "lbToggle": columns.Add(@"""LBToggle"""); break; 
                    case "lbToggleEma": columns.Add(@"""LBToggleEma"""); break;
                    case "aiToggle": columns.Add(@"""AIToggle"""); break;
                    case "aiToggleEma": columns.Add(@"""AIToggleEma"""); break;
                    #region deprecated
                    case "reward":
                        columns.Add(@"""RewardLiquid""");
                        columns.Add(@"""RewardStakedOwn""");
                        columns.Add(@"""RewardStakedShared""");
                        break;
                    case "bonus":
                        columns.Add(@"""BonusLiquid""");
                        columns.Add(@"""BonusStakedOwn""");
                        columns.Add(@"""BonusStakedShared""");
                        break;
                    #endregion
                }
            }

            if (columns.Count == 0)
                return Array.Empty<object[]>();

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""Blocks""")
                .Filter(anyof, x => x == "proposer" ? "ProposerId" : "ProducerId")
                .Filter("ProposerId", proposer)
                .Filter("ProducerId", producer)
                .Filter("Level", level)
                .Filter("Timestamp", timestamp)
                .Filter("BlockRound", blockRound)
                .Take(sort, offset, limit, x => x switch
                {
                    "level" => ("Level", "Level"),
                    "payloadRound" => ("PayloadRound", "PayloadRound"),
                    "blockRound" => ("BlockRound", "BlockRound"),
                    "validations" => ("Validations", "Validations"),
                    "rewardLiquid" => ("RewardLiquid", "RewardLiquid"),
                    "rewardStakedOwn" => ("RewardStakedOwn", "RewardStakedOwn"),
                    "rewardStakedShared" => ("RewardStakedShared", "RewardStakedShared"),
                    "bonusLiquid" => ("BonusLiquid", "BonusLiquid"),
                    "bonusStakedOwn" => ("BonusStakedOwn", "BonusStakedOwn"),
                    "bonusStakedShared" => ("BonusStakedShared", "BonusStakedShared"),
                    "fees" => ("Fees", "Fees"),
                    #region deprecated
                    "reward" => ("RewardLiquid", "RewardLiquid"),
                    "bonus" => ("BonusLiquid", "BonusLiquid"),
                    #endregion
                    _ => ("Id", "Id")
                });

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            var result = new object[rows.Count()][];
            for (int i = 0; i < result.Length; i++)
                result[i] = new object[fields.Length];

            for (int i = 0, j = 0; i < fields.Length; j = 0, i++)
            {
                switch (fields[i])
                {
                    case "cycle":
                        foreach (var row in rows)
                            result[j++][i] = row.Cycle;
                        break;
                    case "level":
                        foreach (var row in rows)
                            result[j++][i] = row.Level;
                        break;
                    case "hash":
                        foreach (var row in rows)
                            result[j++][i] = row.Hash;
                        break;
                    case "timestamp":
                        foreach (var row in rows)
                            result[j++][i] = row.Timestamp;
                        break;
                    case "proto":
                        foreach (var row in rows)
                            result[j++][i] = row.ProtoCode;
                        break;
                    case "payloadRound":
                        foreach (var row in rows)
                            result[j++][i] = row.PayloadRound;
                        break;
                    case "blockRound":
                        foreach (var row in rows)
                            result[j++][i] = row.BlockRound;
                        break;
                    case "validations":
                        foreach (var row in rows)
                            result[j++][i] = row.Validations;
                        break;
                    case "deposit":
                        foreach (var row in rows)
                            result[j++][i] = row.Deposit;
                        break;
                    case "rewardLiquid":
                        foreach (var row in rows)
                            result[j++][i] = row.RewardLiquid;
                        break;
                    case "rewardStakedOwn":
                        foreach (var row in rows)
                            result[j++][i] = row.RewardStakedOwn;
                        break;
                    case "rewardStakedShared":
                        foreach (var row in rows)
                            result[j++][i] = row.RewardStakedShared;
                        break;
                    case "bonusLiquid":
                        foreach (var row in rows)
                            result[j++][i] = row.BonusLiquid;
                        break;
                    case "bonusStakedOwn":
                        foreach (var row in rows)
                            result[j++][i] = row.BonusStakedOwn;
                        break;
                    case "bonusStakedShared":
                        foreach (var row in rows)
                            result[j++][i] = row.BonusStakedShared;
                        break;
                    case "fees":
                        foreach (var row in rows)
                            result[j++][i] = row.Fees;
                        break;
                    case "nonceRevealed":
                        foreach (var row in rows)
                            result[j++][i] = row.RevelationId != null;
                        break;
                    case "proposer":
                        foreach (var row in rows)
                            result[j++][i] = row.ProposerId != null ? await Accounts.GetAliasAsync(row.ProposerId) : null;
                        break;
                    case "producer":
                        foreach (var row in rows)
                            result[j++][i] = row.ProducerId != null ? await Accounts.GetAliasAsync(row.ProducerId) : null;
                        break;
                    case "software":
                        foreach (var row in rows)
                            result[j++][i] = row.SoftwareId != null ? Software[row.SoftwareId] : null;
                        break;
                    case "lbToggle":
                        foreach (var row in rows)
                            result[j++][i] = row.LBToggle;
                        break;
                    case "lbToggleEma":
                        foreach (var row in rows)
                            result[j++][i] = row.LBToggleEma;
                        break;
                    case "aiToggle":
                        foreach (var row in rows)
                            result[j++][i] = row.AIToggle;
                        break;
                    case "aiToggleEma":
                        foreach (var row in rows)
                            result[j++][i] = row.AIToggleEma;
                        break;
                    case "quote":
                        foreach (var row in rows)
                            result[j++][i] = Quotes.Get(quote, row.Level);
                        break;

                    #region deprecated
                    case "reward":
                        foreach (var row in rows)
                            result[j++][i] = row.RewardLiquid + row.RewardStakedOwn + row.RewardStakedShared;
                        break;
                    case "bonus":
                        foreach (var row in rows)
                            result[j++][i] = row.BonusLiquid + row.BonusStakedOwn + row.BonusStakedShared;
                        break;
                    #endregion
                }
            }

            return result;
        }

        public async Task<object[]> Get(
            AnyOfParameter anyof,
            AccountParameter proposer,
            AccountParameter producer,
            Int32Parameter level,
            DateTimeParameter timestamp,
            Int32Parameter blockRound,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            string field,
            Symbols quote)
        {
            var columns = new HashSet<string>(1);
            switch (field)
            {
                case "cycle": columns.Add(@"""Cycle"""); break;
                case "level": columns.Add(@"""Level"""); break;
                case "hash": columns.Add(@"""Hash"""); break;
                case "timestamp": columns.Add(@"""Timestamp"""); break;
                case "proto": columns.Add(@"""ProtoCode"""); break;
                case "payloadRound": columns.Add(@"""PayloadRound"""); break;
                case "blockRound": columns.Add(@"""BlockRound"""); break;
                case "validations": columns.Add(@"""Validations"""); break;
                case "deposit": columns.Add(@"""Deposit"""); break;
                case "rewardLiquid": columns.Add(@"""RewardLiquid"""); break;
                case "rewardStakedOwn": columns.Add(@"""RewardStakedOwn"""); break;
                case "rewardStakedShared": columns.Add(@"""RewardStakedShared"""); break;
                case "bonusLiquid": columns.Add(@"""BonusLiquid"""); break;
                case "bonusStakedOwn": columns.Add(@"""BonusStakedOwn"""); break;
                case "bonusStakedShared": columns.Add(@"""BonusStakedShared"""); break;
                case "fees": columns.Add(@"""Fees"""); break;
                case "nonceRevealed": columns.Add(@"""RevelationId"""); break;
                case "proposer": columns.Add(@"""ProposerId"""); break;
                case "producer": columns.Add(@"""ProducerId"""); break;
                case "software": columns.Add(@"""SoftwareId"""); break;
                case "quote": columns.Add(@"""Level"""); break;
                case "lbToggle": columns.Add(@"""LBToggle"""); break;
                case "lbToggleEma": columns.Add(@"""LBToggleEma"""); break;
                case "aiToggle": columns.Add(@"""AIToggle"""); break;
                case "aiToggleEma": columns.Add(@"""AIToggleEma"""); break;
                #region deprecated
                case "reward":
                    columns.Add(@"""RewardLiquid""");
                    columns.Add(@"""RewardStakedOwn""");
                    columns.Add(@"""RewardStakedShared""");
                    break;
                case "bonus":
                    columns.Add(@"""BonusLiquid""");
                    columns.Add(@"""BonusStakedOwn""");
                    columns.Add(@"""BonusStakedShared""");
                    break;
                #endregion
            }

            if (columns.Count == 0)
                return Array.Empty<object>();

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""Blocks""")
                .Filter(anyof, x => x == "proposer" ? "ProposerId" : "ProducerId")
                .Filter("ProposerId", proposer)
                .Filter("ProducerId", producer)
                .Filter("Level", level)
                .Filter("Timestamp", timestamp)
                .Filter("BlockRound", blockRound)
                .Take(sort, offset, limit, x => x switch
                {
                    "level" => ("Level", "Level"),
                    "payloadRound" => ("PayloadRound", "PayloadRound"),
                    "blockRound" => ("BlockRound", "BlockRound"),
                    "validations" => ("Validations", "Validations"),
                    "reward" => ("RewardLiquid", "RewardLiquid"),
                    "rewardLiquid" => ("RewardLiquid", "RewardLiquid"),
                    "rewardStakedOwn" => ("RewardStakedOwn", "RewardStakedOwn"),
                    "rewardStakedShared" => ("RewardStakedShared", "RewardStakedShared"),
                    "bonus" => ("BonusLiquid", "BonusLiquid"),
                    "bonusLiquid" => ("BonusLiquid", "BonusLiquid"),
                    "bonusStakedOwn" => ("BonusStakedOwn", "BonusStakedOwn"),
                    "bonusStakedShared" => ("BonusStakedShared", "BonusStakedShared"),
                    "fees" => ("Fees", "Fees"),
                    _ => ("Id", "Id")
                });

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            //TODO: optimize memory allocation
            var result = new object[rows.Count()];
            var j = 0;

            switch (field)
            {
                case "cycle":
                    foreach (var row in rows)
                        result[j++] = row.Cycle;
                    break;
                case "level":
                    foreach (var row in rows)
                        result[j++] = row.Level;
                    break;
                case "hash":
                    foreach (var row in rows)
                        result[j++] = row.Hash;
                    break;
                case "timestamp":
                    foreach (var row in rows)
                        result[j++] = row.Timestamp;
                    break;
                case "proto":
                    foreach (var row in rows)
                        result[j++] = row.ProtoCode;
                    break;
                case "payloadRound":
                    foreach (var row in rows)
                        result[j++] = row.PayloadRound;
                    break;
                case "blockRound":
                    foreach (var row in rows)
                        result[j++] = row.BlockRound;
                    break;
                case "validations":
                    foreach (var row in rows)
                        result[j++] = row.Validations;
                    break;
                case "deposit":
                    foreach (var row in rows)
                        result[j++] = row.Deposit;
                    break;
                case "rewardLiquid":
                    foreach (var row in rows)
                        result[j++] = row.RewardLiquid;
                    break;
                case "rewardStakedOwn":
                    foreach (var row in rows)
                        result[j++] = row.RewardStakedOwn;
                    break;
                case "rewardStakedShared":
                    foreach (var row in rows)
                        result[j++] = row.RewardStakedShared;
                    break;
                case "bonusLiquid":
                    foreach (var row in rows)
                        result[j++] = row.BonusLiquid;
                    break;
                case "bonusStakedOwn":
                    foreach (var row in rows)
                        result[j++] = row.BonusStakedOwn;
                    break;
                case "bonusStakedShared":
                    foreach (var row in rows)
                        result[j++] = row.BonusStakedShared;
                    break;
                case "fees":
                    foreach (var row in rows)
                        result[j++] = row.Fees;
                    break;
                case "nonceRevealed":
                    foreach (var row in rows)
                        result[j++] = row.RevelationId != null;
                    break;
                case "proposer":
                    foreach (var row in rows)
                        result[j++] = row.ProposerId != null ? await Accounts.GetAliasAsync(row.ProposerId) : null;
                    break;
                case "producer":
                    foreach (var row in rows)
                        result[j++] = row.ProducerId != null ? await Accounts.GetAliasAsync(row.ProducerId) : null;
                    break;
                case "software":
                    foreach (var row in rows)
                        result[j++] = row.SoftwareId != null ? Software[row.SoftwareId] : null;
                    break;
                case "lbToggle":
                    foreach (var row in rows)
                        result[j++] = row.LBToggle;
                    break;
                case "lbToggleEma":
                    foreach (var row in rows)
                        result[j++] = row.LBToggleEma;
                    break;
                case "aiToggle":
                    foreach (var row in rows)
                        result[j++] = row.AIToggle;
                    break;
                case "aiToggleEma":
                    foreach (var row in rows)
                        result[j++] = row.AIToggleEma;
                    break;
                case "quote":
                    foreach (var row in rows)
                        result[j++] = Quotes.Get(quote, row.Level);
                    break;

                #region deprecated
                case "reward":
                    foreach (var row in rows)
                        result[j++] = row.RewardLiquid + row.RewardStakedOwn + row.RewardStakedShared;
                    break;
                case "bonus":
                    foreach (var row in rows)
                        result[j++] = row.BonusLiquid + row.BonusStakedOwn + row.BonusStakedShared;
                    break;
                #endregion
            }

            return result;
        }

        public async Task<IEnumerable<int>> GetEventLevels(Data.Models.BlockEvents @event, OffsetParameter offset, int limit = 100)
        {
            var sql = new SqlBuilder(@"SELECT ""Level"" FROM ""Blocks""")
                .Filter($@"""Events"" & {(int)@event} > 0")
                .Take(new SortParameter { Asc = "level" }, offset, limit, x => ("Level", "Level"));

            using var db = GetConnection();
            return await db.QueryAsync<int>(sql.Query, sql.Params);
        }

        public async Task<IEnumerable<DateTime>> GetTimestamps(int offset = 0, int limit = 100)
        {
            var sql = $@"
                SELECT  ""Timestamp""
                FROM    ""Blocks""
                ORDER BY ""Id""
                OFFSET   @offset
                LIMIT    @limit";

            using var db = GetConnection();
            return await db.QueryAsync<DateTime>(sql, new { limit, offset });
        }

        async Task LoadOperations(Block block, Data.Models.Operations operations, MichelineFormat format, Symbols quote)
        {
            var endorsements = operations.HasFlag(Data.Models.Operations.Endorsements)
                ? Operations.GetEndorsements(block, quote)
                : Task.FromResult(Enumerable.Empty<EndorsementOperation>());

            var preendorsements = operations.HasFlag(Data.Models.Operations.Preendorsements)
                ? Operations.GetPreendorsements(block, quote)
                : Task.FromResult(Enumerable.Empty<PreendorsementOperation>());

            var proposals = operations.HasFlag(Data.Models.Operations.Proposals)
                ? Operations.GetProposals(block, quote)
                : Task.FromResult(Enumerable.Empty<ProposalOperation>());

            var ballots = operations.HasFlag(Data.Models.Operations.Ballots)
                ? Operations.GetBallots(block, quote)
                : Task.FromResult(Enumerable.Empty<BallotOperation>());

            var activations = operations.HasFlag(Data.Models.Operations.Activations)
                ? Operations.GetActivations(block, quote)
                : Task.FromResult(Enumerable.Empty<ActivationOperation>());

            var doubleBaking = operations.HasFlag(Data.Models.Operations.DoubleBakings)
                ? Operations.GetDoubleBakings(block, quote)
                : Task.FromResult(Enumerable.Empty<DoubleBakingOperation>());

            var doubleEndorsing = operations.HasFlag(Data.Models.Operations.DoubleEndorsings)
                ? Operations.GetDoubleEndorsings(block, quote)
                : Task.FromResult(Enumerable.Empty<DoubleEndorsingOperation>());

            var doublePreendorsing = operations.HasFlag(Data.Models.Operations.DoublePreendorsings)
                ? Operations.GetDoublePreendorsings(block, quote)
                : Task.FromResult(Enumerable.Empty<DoublePreendorsingOperation>());

            var nonceRevelations = operations.HasFlag(Data.Models.Operations.Revelations)
                ? Operations.GetNonceRevelations(block, quote)
                : Task.FromResult(Enumerable.Empty<NonceRevelationOperation>());

            var vdfRevelations = operations.HasFlag(Data.Models.Operations.VdfRevelation)
                ? Operations.GetVdfRevelations(block, quote)
                : Task.FromResult(Enumerable.Empty<VdfRevelationOperation>());

            var delegations = operations.HasFlag(Data.Models.Operations.Delegations)
                ? Operations.GetDelegations(block, quote)
                : Task.FromResult(Enumerable.Empty<DelegationOperation>());

            var originations = operations.HasFlag(Data.Models.Operations.Originations)
                ? Operations.GetOriginations(block, quote)
                : Task.FromResult(Enumerable.Empty<OriginationOperation>());

            var transactions = operations.HasFlag(Data.Models.Operations.Transactions)
                ? Operations.GetTransactions(block, format, quote)
                : Task.FromResult(Enumerable.Empty<TransactionOperation>());

            var reveals = operations.HasFlag(Data.Models.Operations.Reveals)
                ? Operations.GetReveals(block, quote)
                : Task.FromResult(Enumerable.Empty<RevealOperation>());

            var registerConstants = operations.HasFlag(Data.Models.Operations.RegisterConstant)
                ? Operations.GetRegisterConstants(block, format, quote)
                : Task.FromResult(Enumerable.Empty<RegisterConstantOperation>());

            var setDepositsLimits = operations.HasFlag(Data.Models.Operations.SetDepositsLimits)
                ? Operations.GetSetDepositsLimits(block, quote)
                : Task.FromResult(Enumerable.Empty<SetDepositsLimitOperation>());

            var transferTicketOps = operations.HasFlag(Data.Models.Operations.TransferTicket)
                ? Operations.GetTransferTicketOps(block, format, quote)
                : Task.FromResult(Enumerable.Empty<TransferTicketOperation>());

            var txRollupCommitOps = operations.HasFlag(Data.Models.Operations.TxRollupCommit)
                ? Operations.GetTxRollupCommitOps(block, quote)
                : Task.FromResult(Enumerable.Empty<TxRollupCommitOperation>());
            
            var txRollupDispatchTicketsOps = operations.HasFlag(Data.Models.Operations.TxRollupDispatchTickets)
                ? Operations.GetTxRollupDispatchTicketsOps(block, quote)
                : Task.FromResult(Enumerable.Empty<TxRollupDispatchTicketsOperation>());

            var txRollupFinalizeCommitmentOps = operations.HasFlag(Data.Models.Operations.TxRollupFinalizeCommitment)
                ? Operations.GetTxRollupFinalizeCommitmentOps(block, quote)
                : Task.FromResult(Enumerable.Empty<TxRollupFinalizeCommitmentOperation>());

            var txRollupOriginationOps = operations.HasFlag(Data.Models.Operations.TxRollupOrigination)
                ? Operations.GetTxRollupOriginationOps(block, quote)
                : Task.FromResult(Enumerable.Empty<TxRollupOriginationOperation>());

            var txRollupRejectionOps = operations.HasFlag(Data.Models.Operations.TxRollupRejection)
                ? Operations.GetTxRollupRejectionOps(block, quote)
                : Task.FromResult(Enumerable.Empty<TxRollupRejectionOperation>());

            var txRollupRemoveCommitmentOps = operations.HasFlag(Data.Models.Operations.TxRollupRemoveCommitment)
                ? Operations.GetTxRollupRemoveCommitmentOps(block, quote)
                : Task.FromResult(Enumerable.Empty<TxRollupRemoveCommitmentOperation>());

            var txRollupReturnBondOps = operations.HasFlag(Data.Models.Operations.TxRollupReturnBond)
                ? Operations.GetTxRollupReturnBondOps(block, quote)
                : Task.FromResult(Enumerable.Empty<TxRollupReturnBondOperation>());

            var txRollupSubmitBatchOps = operations.HasFlag(Data.Models.Operations.TxRollupSubmitBatch)
                ? Operations.GetTxRollupSubmitBatchOps(block, quote)
                : Task.FromResult(Enumerable.Empty<TxRollupSubmitBatchOperation>());

            var increasePaidStorageOps = operations.HasFlag(Data.Models.Operations.IncreasePaidStorage)
                ? Operations.GetIncreasePaidStorageOps(block, quote)
                : Task.FromResult(Enumerable.Empty<IncreasePaidStorageOperation>());

            var updateConsensusKeyOps = operations.HasFlag(Data.Models.Operations.UpdateConsensusKey)
                ? Operations.GetUpdateConsensusKeys(block, quote)
                : Task.FromResult(Enumerable.Empty<UpdateConsensusKeyOperation>());

            var drainDelegateOps = operations.HasFlag(Data.Models.Operations.DrainDelegate)
                ? Operations.GetDrainDelegates(block, quote)
                : Task.FromResult(Enumerable.Empty<DrainDelegateOperation>());

            var srAddMessageOps = operations.HasFlag(Data.Models.Operations.SmartRollupAddMessages)
                ? Operations.GetSmartRollupAddMessagesOps(new() { level = block.Level }, new() { limit = -1 }, quote)
                : Task.FromResult(Enumerable.Empty<SmartRollupAddMessagesOperation>());

            var srCementOps = operations.HasFlag(Data.Models.Operations.SmartRollupCement)
                ? Operations.GetSmartRollupCementOps(new() { level = block.Level }, new() { limit = -1 }, quote)
                : Task.FromResult(Enumerable.Empty<SmartRollupCementOperation>());

            var srExecuteOps = operations.HasFlag(Data.Models.Operations.SmartRollupExecute)
                ? Operations.GetSmartRollupExecuteOps(new() { level = block.Level }, new() { limit = -1 }, quote)
                : Task.FromResult(Enumerable.Empty<SmartRollupExecuteOperation>());

            var srOriginateOps = operations.HasFlag(Data.Models.Operations.SmartRollupOriginate)
                ? Operations.GetSmartRollupOriginateOps(new() { level = block.Level }, new() { limit = -1 }, quote, format)
                : Task.FromResult(Enumerable.Empty<SmartRollupOriginateOperation>());

            var srPublishOps = operations.HasFlag(Data.Models.Operations.SmartRollupPublish)
                ? Operations.GetSmartRollupPublishOps(new() { level = block.Level }, new() { limit = -1 }, quote)
                : Task.FromResult(Enumerable.Empty<SmartRollupPublishOperation>());

            var srRecoverBondOps = operations.HasFlag(Data.Models.Operations.SmartRollupRecoverBond)
                ? Operations.GetSmartRollupRecoverBondOps(new() { level = block.Level }, new() { limit = -1 }, quote)
                : Task.FromResult(Enumerable.Empty<SmartRollupRecoverBondOperation>());

            var srRefuteOps = operations.HasFlag(Data.Models.Operations.SmartRollupRefute)
                ? Operations.GetSmartRollupRefuteOps(new() { level = block.Level }, new() { limit = -1 }, quote)
                : Task.FromResult(Enumerable.Empty<SmartRollupRefuteOperation>());

            var staking = operations.HasFlag(Data.Models.Operations.Staking)
                ? Operations.GetStakingOps(new() { level = block.Level }, new() { limit = -1 }, quote)
                : Task.FromResult(Enumerable.Empty<StakingOperation>());

            var migrations = operations.HasFlag(Data.Models.Operations.Migrations)
                ? Operations.GetMigrations(null, null, null, null, new Int32Parameter { Eq = block.Level }, null, null, null, 10_000, format, quote)
                : Task.FromResult(Enumerable.Empty<MigrationOperation>());

            var penalties = operations.HasFlag(Data.Models.Operations.RevelationPenalty)
                ? Operations.GetRevelationPenalties(null, null, new Int32Parameter { Eq = block.Level }, null, null, null, 10_000, quote)
                : Task.FromResult(Enumerable.Empty<RevelationPenaltyOperation>());

            var endorsingRewards = operations.HasFlag(Data.Models.Operations.EndorsingRewards)
                ? Operations.GetEndorsingRewards(null, null, new Int32Parameter { Eq = block.Level }, null, null, null, 10_000, quote)
                : Task.FromResult(Enumerable.Empty<EndorsingRewardOperation>());

            var autostakingOps = operations.HasFlag(Data.Models.Operations.Autostaking)
                ? Operations.GetAutostakingOps(new() { level = block.Level }, new() { limit = -1 }, quote)
                : Task.FromResult(Enumerable.Empty<AutostakingOperation>());

            await Task.WhenAll(
                endorsements,
                preendorsements,
                proposals,
                ballots,
                activations,
                doubleBaking,
                doubleEndorsing,
                doublePreendorsing,
                nonceRevelations,
                vdfRevelations,
                delegations,
                originations,
                transactions,
                reveals,
                registerConstants,
                setDepositsLimits,
                transferTicketOps,
                txRollupCommitOps,
                txRollupDispatchTicketsOps,
                txRollupFinalizeCommitmentOps,
                txRollupOriginationOps,
                txRollupRejectionOps,
                txRollupRemoveCommitmentOps,
                txRollupReturnBondOps,
                txRollupSubmitBatchOps,
                increasePaidStorageOps,
                updateConsensusKeyOps,
                drainDelegateOps,
                srAddMessageOps,
                srCementOps,
                srExecuteOps,
                srOriginateOps,
                srPublishOps,
                srRecoverBondOps,
                srRefuteOps,
                staking,
                migrations,
                penalties,
                endorsingRewards,
                autostakingOps);

            block.Endorsements = endorsements.Result;
            block.Preendorsements = preendorsements.Result;
            block.Proposals = proposals.Result;
            block.Ballots = ballots.Result;
            block.Activations = activations.Result;
            block.DoubleBaking = doubleBaking.Result;
            block.DoubleEndorsing = doubleEndorsing.Result;
            block.DoublePreendorsing = doublePreendorsing.Result;
            block.NonceRevelations = nonceRevelations.Result;
            block.VdfRevelations = vdfRevelations.Result;
            block.Delegations = delegations.Result;
            block.Originations = originations.Result;
            block.Transactions = transactions.Result;
            block.Reveals = reveals.Result;
            block.RegisterConstants = registerConstants.Result;
            block.SetDepositsLimits = setDepositsLimits.Result;
            block.TransferTicketOps = transferTicketOps.Result;
            block.TxRollupCommitOps = txRollupCommitOps.Result;
            block.TxRollupDispatchTicketsOps = txRollupDispatchTicketsOps.Result;
            block.TxRollupFinalizeCommitmentOps = txRollupFinalizeCommitmentOps.Result;
            block.TxRollupOriginationOps = txRollupOriginationOps.Result;
            block.TxRollupRejectionOps = txRollupRejectionOps.Result;
            block.TxRollupRemoveCommitmentOps = txRollupRemoveCommitmentOps.Result;
            block.TxRollupReturnBondOps = txRollupReturnBondOps.Result;
            block.TxRollupSubmitBatchOps = txRollupSubmitBatchOps.Result;
            block.IncreasePaidStorageOps = increasePaidStorageOps.Result;
            block.UpdateConsensusKeyOps = updateConsensusKeyOps.Result;
            block.DrainDelegateOps = drainDelegateOps.Result;
            block.SrAddMessagesOps = srAddMessageOps.Result;
            block.SrCementOps = srCementOps.Result;
            block.SrExecuteOps = srExecuteOps.Result;
            block.SrOriginateOps = srOriginateOps.Result;
            block.SrPublishOps = srPublishOps.Result;
            block.SrRecoverBondOps = srRecoverBondOps.Result;
            block.SrRefuteOps = srRefuteOps.Result;
            block.StakingOps = staking.Result;
            block.Migrations = migrations.Result;
            block.RevelationPenalties = penalties.Result;
            block.EndorsingRewards = endorsingRewards.Result;
            block.AutostakingOps = autostakingOps.Result;
        }
    }
}
