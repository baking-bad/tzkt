using Dapper;
using Netmavryk.Encoding;
using Tzkt.Api.Models;
using Tzkt.Api.Services.Cache;

namespace Tzkt.Api.Repositories
{
    public class CyclesRepository : DbConnection
    {
        readonly QuotesCache Quotes;
        readonly TimeCache Times;

        public CyclesRepository(QuotesCache quotes, TimeCache times, IConfiguration config) : base(config)
        {
            Quotes = quotes;
            Times = times;
        }

        public async Task<int> GetCount()
        {
            using var db = GetConnection();
            return await db.QueryFirstAsync<int>(@"SELECT COUNT(*) FROM ""Cycles""");
        }

        public async Task<Cycle> Get(int index, Symbols quote)
        {
            var sql = """
                SELECT  *
                FROM    "Cycles"
                WHERE   "Index" = @index
                LIMIT   1
                """;

            using var db = GetConnection();
            var row = await db.QueryFirstOrDefaultAsync(sql, new { index });
            if (row == null) return null;

            return new Cycle
            {
                Index = row.Index,
                FirstLevel = row.FirstLevel,
                StartTime = Times[row.FirstLevel],
                LastLevel = row.LastLevel,
                EndTime = Times[row.LastLevel],
                RandomSeed = Hex.Convert(row.Seed),
                SnapshotIndex = row.SnapshotIndex,
                SnapshotLevel = row.SnapshotLevel,
                TotalBakers = row.TotalBakers,
                TotalBakingPower = row.TotalBakingPower,
                BlockReward = row.BlockReward,
                BlockBonusPerSlot = row.BlockBonusPerSlot,
                EndorsementRewardPerSlot = row.EndorsementRewardPerSlot,
                NonceRevelationReward = row.NonceRevelationReward,
                VdfRevelationReward = row.VdfRevelationReward,
                LBSubsidy = row.LBSubsidy,
                Quote = Quotes.Get(quote, row.LastLevel)
            };
        }

        public async Task<IEnumerable<Cycle>> Get(
            Int32Parameter snapshotIndex,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            Symbols quote)
        {
            var sql = new SqlBuilder(@"SELECT * FROM ""Cycles""")
                .Filter("SnapshotIndex", snapshotIndex)
                .Take(sort ?? new SortParameter { Desc = "index" }, offset, limit, x => ("Index", "Index"));

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            return rows.Select(row => new Cycle
            {
                Index = row.Index,
                FirstLevel = row.FirstLevel,
                StartTime = Times[row.FirstLevel],
                LastLevel = row.LastLevel,
                EndTime = Times[row.LastLevel],
                RandomSeed = Hex.Convert(row.Seed),
                SnapshotIndex = row.SnapshotIndex,
                SnapshotLevel = row.SnapshotLevel,
                TotalBakers = row.TotalBakers,
                TotalBakingPower = row.TotalBakingPower,
                BlockReward = row.BlockReward,
                BlockBonusPerSlot = row.BlockBonusPerSlot,
                EndorsementRewardPerSlot = row.EndorsementRewardPerSlot,
                NonceRevelationReward = row.NonceRevelationReward,
                VdfRevelationReward = row.VdfRevelationReward,
                LBSubsidy = row.LBSubsidy,
                Quote = Quotes.Get(quote, row.LastLevel)
            });
        }

        public async Task<object[][]> Get(
            Int32Parameter snapshotIndex,
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
                    case "index": columns.Add(@"""Index"""); break;
                    case "firstLevel": columns.Add(@"""FirstLevel"""); break;
                    case "startTime": columns.Add(@"""FirstLevel"""); break;
                    case "lastLevel": columns.Add(@"""LastLevel"""); break;
                    case "endTime": columns.Add(@"""LastLevel"""); break;
                    case "randomSeed": columns.Add(@"""Seed"""); break;
                    case "snapshotIndex": columns.Add(@"""SnapshotIndex"""); break;
                    case "snapshotLevel": columns.Add(@"""SnapshotLevel"""); break;
                    case "totalBakers": columns.Add(@"""TotalBakers"""); break;
                    case "totalBakingPower": columns.Add(@"""TotalBakingPower"""); break;
                    case "blockReward": columns.Add(@"""BlockReward"""); break;
                    case "blockBonusPerSlot": columns.Add(@"""BlockBonusPerSlot"""); break;
                    case "endorsementRewardPerSlot": columns.Add(@"""EndorsementRewardPerSlot"""); break;
                    case "nonceRevelationReward": columns.Add(@"""NonceRevelationReward"""); break;
                    case "vdfRevelationReward": columns.Add(@"""VdfRevelationReward"""); break;
                    case "lbSubsidy": columns.Add(@"""LBSubsidy"""); break;
                    case "quote": columns.Add(@"""LastLevel"""); break;
                    #region deprecated
                    case "totalDelegated": columns.Add(@"0"); break;
                    case "totalDelegators": columns.Add(@"0"); break;
                    case "totalStaking": columns.Add(@"""TotalBakingPower"""); break;
                    case "selectedBakers": columns.Add(@"""TotalBakers"""); break;
                    case "selectedStake": columns.Add(@"""TotalBakingPower"""); break;
                    #endregion
                }
            }

            if (columns.Count == 0)
                return Array.Empty<object[]>();

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""Cycles""")
                .Filter("SnapshotIndex", snapshotIndex)
                .Take(sort ?? new SortParameter { Desc = "index" }, offset, limit, x => ("Index", "Index"));

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            var result = new object[rows.Count()][];
            for (int i = 0; i < result.Length; i++)
                result[i] = new object[fields.Length];

            for (int i = 0, j = 0; i < fields.Length; j = 0, i++)
            {
                switch (fields[i])
                {
                    case "index":
                        foreach (var row in rows)
                            result[j++][i] = row.Index;
                        break;
                    case "firstLevel":
                        foreach (var row in rows)
                            result[j++][i] = row.FirstLevel;
                        break;
                    case "startTime":
                        foreach (var row in rows)
                            result[j++][i] = Times[row.FirstLevel];
                        break;
                    case "lastLevel":
                        foreach (var row in rows)
                            result[j++][i] = row.LastLevel;
                        break;
                    case "endTime":
                        foreach (var row in rows)
                            result[j++][i] = Times[row.LastLevel];
                        break;
                    case "randomSeed":
                        foreach (var row in rows)
                            result[j++][i] = Hex.Convert(row.Seed);
                        break;
                    case "snapshotIndex":
                        foreach (var row in rows)
                            result[j++][i] = row.SnapshotIndex;
                        break;
                    case "snapshotLevel":
                        foreach (var row in rows)
                            result[j++][i] = row.SnapshotLevel;
                        break;
                    case "totalBakers":
                        foreach (var row in rows)
                            result[j++][i] = row.TotalBakers;
                        break;
                    case "totalBakingPower":
                        foreach (var row in rows)
                            result[j++][i] = row.TotalBakingPower;
                        break;
                    case "blockReward":
                        foreach (var row in rows)
                            result[j++][i] = row.BlockReward;
                        break;
                    case "blockBonusPerSlot":
                        foreach (var row in rows)
                            result[j++][i] = row.BlockBonusPerSlot;
                        break;
                    case "endorsementRewardPerSlot":
                        foreach (var row in rows)
                            result[j++][i] = row.EndorsementRewardPerSlot;
                        break;
                    case "nonceRevelationReward":
                        foreach (var row in rows)
                            result[j++][i] = row.NonceRevelationReward;
                        break;
                    case "vdfRevelationReward":
                        foreach (var row in rows)
                            result[j++][i] = row.VdfRevelationReward;
                        break;
                    case "lbSubsidy":
                        foreach (var row in rows)
                            result[j++][i] = row.LBSubsidy;
                        break;
                    case "quote":
                        foreach (var row in rows)
                            result[j++][i] = Quotes.Get(quote, row.LastLevel);
                        break;
                    #region deprecated
                    case "totalDelegated":
                        foreach (var row in rows)
                            result[j++][i] = 0;
                        break;
                    case "totalDelegators":
                        foreach (var row in rows)
                            result[j++][i] = 0;
                        break;
                    case "totalStaking":
                        foreach (var row in rows)
                            result[j++][i] = row.TotalBakingPower;
                        break;
                    case "selectedBakers":
                        foreach (var row in rows)
                            result[j++][i] = row.TotalBakers;
                        break;
                    case "selectedStake":
                        foreach (var row in rows)
                            result[j++][i] = row.TotalBakingPower;
                        break;
                    #endregion
                }
            }

            return result;
        }

        public async Task<object[]> Get(
            Int32Parameter snapshotIndex,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            string field,
            Symbols quote)
        {
            var columns = new HashSet<string>(1);
            switch (field)
            {
                case "index": columns.Add(@"""Index"""); break;
                case "firstLevel": columns.Add(@"""FirstLevel"""); break;
                case "startTime": columns.Add(@"""FirstLevel"""); break;
                case "lastLevel": columns.Add(@"""LastLevel"""); break;
                case "endTime": columns.Add(@"""LastLevel"""); break;
                case "randomSeed": columns.Add(@"""Seed"""); break;
                case "snapshotIndex": columns.Add(@"""SnapshotIndex"""); break;
                case "snapshotLevel": columns.Add(@"""SnapshotLevel"""); break;
                case "totalBakers": columns.Add(@"""TotalBakers"""); break;
                case "totalBakingPower": columns.Add(@"""TotalBakingPower"""); break;
                case "blockReward": columns.Add(@"""BlockReward"""); break;
                case "blockBonusPerSlot": columns.Add(@"""BlockBonusPerSlot"""); break;
                case "endorsementRewardPerSlot": columns.Add(@"""EndorsementRewardPerSlot"""); break;
                case "nonceRevelationReward": columns.Add(@"""NonceRevelationReward"""); break;
                case "vdfRevelationReward": columns.Add(@"""VdfRevelationReward"""); break;
                case "lbSubsidy": columns.Add(@"""LBSubsidy"""); break;
                case "quote": columns.Add(@"""LastLevel"""); break;
                #region deprecated
                case "totalDelegated": columns.Add(@"0"); break;
                case "totalDelegators": columns.Add(@"0"); break;
                case "totalStaking": columns.Add(@"""TotalBakingPower"""); break;
                case "selectedBakers": columns.Add(@"""TotalBakers"""); break;
                case "selectedStake": columns.Add(@"""TotalBakingPower"""); break;
                #endregion
            }

            if (columns.Count == 0)
                return Array.Empty<object>();

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""Cycles""")
                .Filter("SnapshotIndex", snapshotIndex)
                .Take(sort ?? new SortParameter { Desc = "index" }, offset, limit, x => ("Index", "Index"));

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            //TODO: optimize memory allocation
            var result = new object[rows.Count()];
            var j = 0;

            switch (field)
            {
                case "index":
                    foreach (var row in rows)
                        result[j++] = row.Index;
                    break;
                case "firstLevel":
                    foreach (var row in rows)
                        result[j++] = row.FirstLevel;
                    break;
                case "startTime":
                    foreach (var row in rows)
                        result[j++] = Times[row.FirstLevel];
                    break;
                case "lastLevel":
                    foreach (var row in rows)
                        result[j++] = row.LastLevel;
                    break;
                case "endTime":
                    foreach (var row in rows)
                        result[j++] = Times[row.LastLevel];
                    break;
                case "randomSeed":
                    foreach (var row in rows)
                        result[j++] = Hex.Convert(row.Seed);
                    break;
                case "snapshotIndex":
                    foreach (var row in rows)
                        result[j++] = row.SnapshotIndex;
                    break;
                case "snapshotLevel":
                    foreach (var row in rows)
                        result[j++] = row.SnapshotLevel;
                    break;
                case "totalBakers":
                    foreach (var row in rows)
                        result[j++] = row.TotalBakers;
                    break;
                case "totalBakingPower":
                    foreach (var row in rows)
                        result[j++] = row.TotalBakingPower;
                    break;
                case "blockReward":
                    foreach (var row in rows)
                        result[j++] = row.BlockReward;
                    break;
                case "blockBonusPerSlot":
                    foreach (var row in rows)
                        result[j++] = row.BlockBonusPerSlot;
                    break;
                case "endorsementRewardPerSlot":
                    foreach (var row in rows)
                        result[j++] = row.EndorsementRewardPerSlot;
                    break;
                case "nonceRevelationReward":
                    foreach (var row in rows)
                        result[j++] = row.NonceRevelationReward;
                    break;
                case "vdfRevelationReward":
                    foreach (var row in rows)
                        result[j++] = row.VdfRevelationReward;
                    break;
                case "lbSubsidy":
                    foreach (var row in rows)
                        result[j++] = row.LBSubsidy;
                    break;
                case "quote":
                    foreach (var row in rows)
                        result[j++] = Quotes.Get(quote, row.LastLevel);
                    break;
                #region deprecated
                case "totalDelegated":
                    foreach (var row in rows)
                        result[j++] = 0;
                    break;
                case "totalDelegators":
                    foreach (var row in rows)
                        result[j++] = 0;
                    break;
                case "totalStaking":
                    foreach (var row in rows)
                        result[j++] = row.TotalBakingPower;
                    break;
                case "selectedBakers":
                    foreach (var row in rows)
                        result[j++] = row.TotalBakers;
                    break;
                case "selectedStake":
                    foreach (var row in rows)
                        result[j++] = row.TotalBakingPower;
                    break;
                #endregion
            }

            return result;
        }
    }
}
