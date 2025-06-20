using System.Data;
using Dapper;
using Dynamic.Json;
using Npgsql;
using Tzkt.Api.Models;
using Tzkt.Api.Repositories;
using Tzkt.Api.Services.Cache;

namespace Tzkt.Api.Services
{
    public class HomeService
    {
        #region static
        public static object?[][] AccountsTab { get; private set; } = [];
        public static readonly string[] AccountFields =
        [
            "alias", "address", "type", "delegate", "firstActivityTime", "balance", "numTransactions", "lastActivityTime"
        ];

        public static object?[][] BakersTab { get; private set; } = [];
        public static readonly string[] BakerFields =
        [
            "alias", "address", "firstActivityTime", "balance", "stakingBalance", "numDelegators", "lastActivityTime"
        ];

        public static object?[][] AssetsTab { get; private set; } = [];
        public static readonly string[] AssetFields =
        [
            "alias", "address", "tzips", "creator", "firstActivityTime", "balance", "numTransactions", "lastActivityTime"
        ];

        public static object?[][] BlocksTab { get; private set; } = [];
        public static readonly string[] BlockFields =
        [
            "timestamp", "level", "proposer", "producer", "payloadRound", "blockRound", "validations", "fees", "hash",
            "rewardDelegated", "rewardStakedOwn", "rewardStakedShared", "rewardStakedEdge",
            "bonusDelegated", "bonusStakedOwn", "bonusStakedShared", "bonusStakedEdge"
        ];
        #endregion

        #region stats
        static DailyData? DailyData;
        static CycleData? CycleData;
        static GovernanceData? GovernanceData;
        static StakingData? StakingData;
        static AccountsData? AccountsData;
        static TxsData? TxsData;
        static MarketData? MarketData;
        static List<Quote>? MarketChart;
        #endregion

        readonly NpgsqlDataSource DataSource;

        readonly AccountRepository AccountsRepo;
        readonly BakingRightsRepository RightsRepo;
        readonly BlockRepository BlocksRepo;
        readonly QuotesRepository QuotesRepo;
        readonly VotingRepository VotingRepo;

        readonly ProtocolsCache Protocols;
        readonly StateCache State;
        readonly TimeCache Times;

        readonly SemaphoreSlim Sema = new(1);
        readonly HomeConfig Config;
        readonly ILogger Logger;
        static int LastUpdate;

        public HomeService(NpgsqlDataSource dataSource, BakingRightsRepository rights, TimeCache times,
            BlockRepository blocks, VotingRepository voting, AccountRepository accounts, ProtocolsCache protocols,
            StateCache state, QuotesRepository quotes, IConfiguration config, ILogger<HomeService> logger)
        {
            DataSource = dataSource;
            RightsRepo = rights;
            Times = times;
            BlocksRepo = blocks;
            VotingRepo = voting;
            AccountsRepo = accounts;
            Protocols = protocols;
            State = state;
            QuotesRepo = quotes;
            Config = config.GetHomeConfig();
            Logger = logger;

            _ = UpdateAsync();
        }
        
        public static HomeStats? GetCurrentStats(Symbols quote)
        {
            if (LastUpdate <= 0)
                return null;
            
            var priceChart = quote switch
            {
                Symbols.Btc => MarketChart?.Select(x => new ChartPoint<double>
                {
                    Date = x.Timestamp,
                    Value = x.Btc
                }).ToList(),
                Symbols.Eur => MarketChart?.Select(x => new ChartPoint<double>
                {
                    Date = x.Timestamp,
                    Value = x.Eur
                }).ToList(),
                Symbols.Usd => MarketChart?.Select(x => new ChartPoint<double>
                {
                    Date = x.Timestamp,
                    Value = x.Usd
                }).ToList(),
                Symbols.Cny => MarketChart?.Select(x => new ChartPoint<double>
                {
                    Date = x.Timestamp,
                    Value = x.Cny
                }).ToList(),
                Symbols.Jpy => MarketChart?.Select(x => new ChartPoint<double>
                {
                    Date = x.Timestamp,
                    Value = x.Jpy
                }).ToList(),
                Symbols.Krw => MarketChart?.Select(x => new ChartPoint<double>
                {
                    Date = x.Timestamp,
                    Value = x.Krw
                }).ToList(),
                Symbols.Eth => MarketChart?.Select(x => new ChartPoint<double>
                {
                    Date = x.Timestamp,
                    Value = x.Eth
                }).ToList(),
                Symbols.Gbp => MarketChart?.Select(x => new ChartPoint<double>
                {
                    Date = x.Timestamp,
                    Value = x.Gbp
                }).ToList(),
                _ => MarketChart?.Select(x => new ChartPoint<double>
                {
                    Date = x.Timestamp,
                    Value = x.Usd
                }).ToList()
            };
            return new HomeStats
            {
                DailyData = DailyData!,
                CycleData = CycleData!,
                GovernanceData = GovernanceData!,
                StakingData = StakingData!,
                AccountsData = AccountsData!,
                TxsData = TxsData!,
                MarketData = MarketData!,
                PriceChart = priceChart!
            };
        }

        public async Task UpdateAsync()
        {
            if (!Config.Enabled) return;

            try
            {
                await Sema.WaitAsync();
                Logger.LogDebug("Update home");

                await using var db = await DataSource.OpenConnectionAsync();
                
                BlocksTab = await GetBlocks(); // 60
                CycleData = GetCycleData(); // 1
                GovernanceData = await GetGovernanceData(); // 40
                await UpdateMarketChart(db); // 10

                if (LastUpdate < State.Current.Level - Config.UpdatePeriod)
                {
                    AccountsTab = await GetAccounts(); // 270
                    BakersTab = await GetBakers(); // 40
                    AssetsTab = await GetAssets(); // 100

                    var statistics = await GetStatistics(db); // 15

                    DailyData = await GetDailyData(db); // 260
                    TxsData = await GetTxsData(db); // 2800
                    StakingData = await GetStakingData(db, statistics.TotalSupply); // 50
                    MarketData = new MarketData
                    {
                        TotalSupply = statistics.TotalSupply,
                        CirculatingSupply = statistics.CirculatingSupply
                    };
                    AccountsData = await GetAccountsData(db); // 320
                    
                    LastUpdate = State.Current.Level;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to update home stats");
            }
            finally
            {
                Sema.Release();
            }
        }

        #region tabs
        async Task<object?[][]> GetBlocks()
        {
            var level = State.Current.Level;

            var upcoming = await RightsRepo.Get(
                new BakingRightTypeParameter { Eq = 0 },
                null, null,
                new Int32Parameter { In = Enumerable.Range(level + 1, 3).ToList() },
                null,
                new Int32NullParameter { Eq = 0 },
                null,
                new SortParameter { Desc = "level" },
                null, 5, [
                    "timestamp", "level", "proposer", "producer", "payloadRound", "blockRound", "validations", "fees", "hash",
                    "rewardDelegated", "rewardStakedOwn", "rewardStakedShared", "rewardStakedEdge",
                    "bonusDelegated", "bonusStakedOwn", "bonusStakedShared", "bonusStakedEdge"
                ]);

            var blocks = await BlocksRepo.Get(null, null, null, null, null, null, 
                new SortParameter { Desc = "level" }, null, 7, BlockFields, Symbols.None);

            return [.. upcoming, .. blocks];
        }

        async Task<object?[][]> GetAccounts()
        {
            return await AccountsRepo.Get(null, null, null, null, null, null, null, null, null,
                new SortParameter { Desc = "balance" }, null, 10, AccountFields);
        }

        async Task<object?[][]> GetBakers()
        {
            return await AccountsRepo.GetDelegates(new BoolParameter { Eq = true }, null,
                new SortParameter { Desc = "stakingBalance" }, null, 10, BakerFields);
        }

        async Task<object?[][]> GetAssets()
        {
            return (await AccountsRepo.Get(
                    null, null,
                    new AccountTypeParameter { Eq = 2 }, new ContractKindParameter { Eq = 2 }, 
                    null, null, null, null, null,
                    new SortParameter { Desc = "numTransactions" }, null, 100, AssetFields))
                .OrderBy(x => x[0] == null)
                .ThenByDescending(x => (int)x[6]!)
                .Take(10)
                .ToArray();
        }
        #endregion

        #region cards
        async Task<Statistics> GetStatistics(IDbConnection db)
        {
            var row = await db.QueryFirstAsync("""
                SELECT *
                FROM "Statistics"
                ORDER BY "Level" DESC
                LIMIT 1
                """);

            return new Statistics
            {
                Cycle = row.Cycle,
                Date = row.Date,
                Level = row.Level,
                TotalBootstrapped = row.TotalBootstrapped,
                TotalCommitments = row.TotalCommitments,
                TotalCreated = row.TotalCreated,
                TotalBurned = row.TotalBurned,
                TotalBanished = row.TotalBanished,
                TotalActivated = row.TotalActivated,
                TotalFrozen = row.TotalFrozen,
                TotalRollupBonds = row.TotalRollupBonds,
                TotalSmartRollupBonds = row.TotalSmartRollupBonds,
                TotalLost = row.TotalLost,
                TotalSupply = row.TotalBootstrapped + row.TotalCommitments + row.TotalCreated
                            - row.TotalBurned - row.TotalBanished,
                CirculatingSupply = row.TotalBootstrapped + row.TotalActivated + row.TotalCreated
                                  - row.TotalBurned - row.TotalBanished - row.TotalLost,
            };
        }

        async Task<DailyData> GetDailyData(IDbConnection db)
        {
            var currPeriod = Times.FindLevel(State.Current.Timestamp.AddDays(-1), SearchMode.ExactOrHigher);
            var prevPeriod = Times.FindLevel(State.Current.Timestamp.AddDays(-2), SearchMode.ExactOrHigher);

            var txs = await db.QueryFirstAsync(
                $@"SELECT COALESCE(SUM(""Amount""), 0)::bigint AS volume, COUNT(*)::integer AS count FROM ""TransactionOps"" WHERE ""Status"" = 1 AND ""Level"" >= {currPeriod}");
            var calls = await db.ExecuteScalarAsync<int>(
                $@"SELECT COUNT(*)::integer FROM ""TransactionOps"" WHERE ""Status"" = 1  AND ""Entrypoint"" IS NOT NULL AND ""Level"" >= {currPeriod}");
            var accounts = await db.ExecuteScalarAsync<int>(
                $@"SELECT COUNT(*)::integer FROM ""Accounts"" WHERE ""FirstLevel"" >= {currPeriod}");

            if (prevPeriod == currPeriod)
            {
                return new DailyData
                {
                    Volume = txs.volume ?? 0,
                    Txs = txs.count,
                    Calls = calls,
                    Accounts = accounts,
                };
            }
            
            var prevTxs = await db.QueryFirstAsync(
                $@"SELECT COALESCE(SUM(""Amount""), 0)::bigint AS volume, COUNT(*)::integer AS count FROM ""TransactionOps"" WHERE ""Status"" = 1 AND ""Level"" >= {prevPeriod} AND ""Level"" < {currPeriod}");
            var prevCalls = await db.ExecuteScalarAsync<int>(
                $@"SELECT COUNT(*)::integer AS count FROM ""TransactionOps"" WHERE ""Status"" = 1  AND ""Entrypoint"" IS NOT NULL AND ""Level"" >= {prevPeriod} AND ""Level"" < {currPeriod}");
            var prevAccounts = await db.ExecuteScalarAsync<int>(
                $@"SELECT COUNT(*)::integer AS count FROM ""Accounts"" WHERE ""FirstLevel"" >= {prevPeriod} AND ""FirstLevel"" < {currPeriod}");
            
            return new DailyData
            {
                Volume = txs.volume,
                VolumeDiff = Diff(txs.volume, prevTxs.volume),
                Txs = txs.count,
                TxsDiff = Diff(txs.count, prevTxs.count),
                Calls = calls,
                CallsDiff = Diff(calls, prevCalls),
                Accounts = accounts,
                AccountsDiff = Diff(accounts, prevAccounts)
            };
        }

        CycleData GetCycleData()
        {
            var state = State.Current;
            var cycle = state.Cycle;
            var level = state.Level;
            var cycleSize = Protocols.FindByCycle(cycle).BlocksPerCycle;
            var firstLevel = Protocols.FindByCycle(cycle).GetCycleStart(cycle);
            var lastLevel = Protocols.FindByCycle(cycle).GetCycleEnd(cycle);

            return new CycleData
            {
                Cycle = cycle,
                Level = level,
                Timestamp = state.Timestamp,
                FirstLevel = firstLevel,
                StartTime = Times[firstLevel],
                LastLevel = lastLevel,
                EndTime = Times[lastLevel],
                Progress = Math.Round(100.0 * (level - firstLevel + 1) / cycleSize, 2)
            };
        }

        async Task<TxsData> GetTxsData(IDbConnection db)
        {
            var currPeriod = Times.FindLevel(State.Current.Timestamp.AddMonths(-1), SearchMode.ExactOrHigher);
            var prevPeriod = Times.FindLevel(State.Current.Timestamp.AddMonths(-2), SearchMode.ExactOrHigher);

            var fees = await db.QueryFirstAsync($@"
                SELECT COALESCE(SUM(fee), 0)::bigint AS paid, COALESCE(SUM(burn), 0)::bigint AS burned FROM
                (
                    SELECT SUM(""BakerFee"")::bigint AS fee, 0::bigint AS burn FROM ""DalPublishCommitmentOps"" WHERE ""Level"" >= {currPeriod}
                    UNION ALL
                    SELECT SUM(""BakerFee"")::bigint AS fee, 0::bigint AS burn FROM ""DelegationOps"" WHERE ""Level"" >= {currPeriod}
                    UNION ALL
                    SELECT SUM(""Fee"")::bigint AS fee, SUM(""AllocationFee"")::bigint AS burn FROM ""DrainDelegateOps"" WHERE ""Level"" >= {currPeriod}
                    UNION ALL
                    SELECT SUM(""BakerFee"")::bigint AS fee, SUM(COALESCE(""StorageFee"", 0))::bigint AS burn FROM ""IncreasePaidStorageOps"" WHERE ""Level"" >= {currPeriod}
                    UNION ALL
                    SELECT SUM(""BakerFee"")::bigint AS fee, SUM(COALESCE(""AllocationFee"", 0) + COALESCE(""StorageFee"", 0))::bigint AS burn FROM ""OriginationOps"" WHERE ""Level"" >= {currPeriod}
                    UNION ALL
                    SELECT SUM(""BakerFee"")::bigint AS fee, SUM(COALESCE(""StorageFee"", 0))::bigint AS burn FROM ""RegisterConstantOps"" WHERE ""Level"" >= {currPeriod}
                    UNION ALL
                    SELECT SUM(""BakerFee"")::bigint AS fee, 0::bigint AS burn FROM ""RevealOps"" WHERE ""Level"" >= {currPeriod}
                    UNION ALL
                    SELECT SUM(""BakerFee"")::bigint AS fee, 0::bigint AS burn FROM ""SetDelegateParametersOps"" WHERE ""Level"" >= {currPeriod}
                    UNION ALL
                    SELECT SUM(""BakerFee"")::bigint AS fee, 0::bigint AS burn FROM ""SetDepositsLimitOps"" WHERE ""Level"" >= {currPeriod}
                    UNION ALL
                    SELECT SUM(""BakerFee"")::bigint AS fee, 0::bigint AS burn FROM ""SmartRollupAddMessagesOps"" WHERE ""Level"" >= {currPeriod}
                    UNION ALL
                    SELECT SUM(""BakerFee"")::bigint AS fee, 0::bigint AS burn FROM ""SmartRollupCementOps"" WHERE ""Level"" >= {currPeriod}
                    UNION ALL
                    SELECT SUM(""BakerFee"")::bigint AS fee, SUM(COALESCE(""StorageFee"", 0))::bigint AS burn FROM ""SmartRollupExecuteOps"" WHERE ""Level"" >= {currPeriod}
                    UNION ALL
                    SELECT SUM(""BakerFee"")::bigint AS fee, SUM(COALESCE(""StorageFee"", 0))::bigint AS burn FROM ""SmartRollupOriginateOps"" WHERE ""Level"" >= {currPeriod}
                    UNION ALL
                    SELECT SUM(""BakerFee"")::bigint AS fee, 0::bigint AS burn FROM ""SmartRollupPublishOps"" WHERE ""Level"" >= {currPeriod}
                    UNION ALL
                    SELECT SUM(""BakerFee"")::bigint AS fee, 0::bigint AS burn FROM ""SmartRollupRecoverBondOps"" WHERE ""Level"" >= {currPeriod}
                    UNION ALL
                    SELECT SUM(""BakerFee"")::bigint AS fee, 0::bigint AS burn FROM ""SmartRollupRefuteOps"" WHERE ""Level"" >= {currPeriod}
                    UNION ALL
                    SELECT SUM(""BakerFee"")::bigint AS fee, 0::bigint AS burn FROM ""StakingOps"" WHERE ""Level"" >= {currPeriod}
                    UNION ALL
                    SELECT SUM(""BakerFee"")::bigint AS fee, SUM(COALESCE(""AllocationFee"", 0) + COALESCE(""StorageFee"", 0))::bigint AS burn FROM ""TransactionOps"" WHERE ""Level"" >= {currPeriod}
                    UNION ALL
                    SELECT SUM(""BakerFee"")::bigint AS fee, SUM(COALESCE(""StorageFee"", 0))::bigint AS burn FROM ""TransferTicketOps"" WHERE ""Level"" >= {currPeriod}
                    UNION ALL
                    SELECT SUM(""BakerFee"")::bigint AS fee, 0::bigint AS burn FROM ""TxRollupCommitOps"" WHERE ""Level"" >= {currPeriod}
                    UNION ALL
                    SELECT SUM(""BakerFee"")::bigint AS fee, SUM(COALESCE(""StorageFee"", 0))::bigint AS burn FROM ""TxRollupDispatchTicketsOps"" WHERE ""Level"" >= {currPeriod}
                    UNION ALL
                    SELECT SUM(""BakerFee"")::bigint AS fee, 0::bigint AS burn FROM ""TxRollupFinalizeCommitmentOps"" WHERE ""Level"" >= {currPeriod}
                    UNION ALL
                    SELECT SUM(""BakerFee"")::bigint AS fee, SUM(COALESCE(""AllocationFee"", 0))::bigint AS burn FROM ""TxRollupOriginationOps"" WHERE ""Level"" >= {currPeriod}
                    UNION ALL
                    SELECT SUM(""BakerFee"")::bigint AS fee, 0::bigint AS burn FROM ""TxRollupRejectionOps"" WHERE ""Level"" >= {currPeriod}
                    UNION ALL
                    SELECT SUM(""BakerFee"")::bigint AS fee, 0::bigint AS burn FROM ""TxRollupRemoveCommitmentOps"" WHERE ""Level"" >= {currPeriod}
                    UNION ALL
                    SELECT SUM(""BakerFee"")::bigint AS fee, 0::bigint AS burn FROM ""TxRollupReturnBondOps"" WHERE ""Level"" >= {currPeriod}
                    UNION ALL
                    SELECT SUM(""BakerFee"")::bigint AS fee, SUM(COALESCE(""StorageFee"", 0))::bigint AS burn FROM ""TxRollupSubmitBatchOps"" WHERE ""Level"" >= {currPeriod}
                    UNION ALL
                    SELECT SUM(""BakerFee"")::bigint AS fee, 0::bigint AS burn FROM ""UpdateConsensusKeyOps"" WHERE ""Level"" >= {currPeriod}
                ) AS current");
            
            var txs = await db.QueryFirstAsync(
                $@"SELECT COUNT(*)::integer AS count, COALESCE(SUM(""Amount""), 0)::bigint AS volume FROM ""TransactionOps"" WHERE ""Status"" = 1 AND ""Level"" >= {currPeriod}");
            
            var calls = await db.ExecuteScalarAsync<int>(
                $@"SELECT COUNT(*)::integer FROM ""TransactionOps"" WHERE ""Status"" = 1 AND ""Entrypoint"" IS NOT NULL AND ""Level"" >= {currPeriod}");

            if (prevPeriod == currPeriod)
            {
                return new TxsData
                {
                    Burned = fees.burned ?? 0,
                    Fees = fees.paid ?? 0,
                    Txs = txs.count,
                    Volume = txs.volume ?? 0,
                    Calls = calls,
                };
            }
            
            var prevFees = await db.QueryFirstAsync($@"
                SELECT COALESCE(SUM(fee), 0)::bigint AS paid, COALESCE(SUM(burn), 0)::bigint AS burned FROM
                (
                    SELECT SUM(""BakerFee"")::bigint AS fee, 0::bigint AS burn FROM ""DalPublishCommitmentOps"" WHERE ""Level"" >= {prevPeriod} AND ""Level"" < {currPeriod}
                    UNION ALL
                    SELECT SUM(""BakerFee"")::bigint AS fee, 0::bigint AS burn FROM ""DelegationOps"" WHERE ""Level"" >= {prevPeriod} AND ""Level"" < {currPeriod}
                    UNION ALL
                    SELECT SUM(""Fee"")::bigint AS fee, SUM(""AllocationFee"")::bigint AS burn FROM ""DrainDelegateOps"" WHERE ""Level"" >= {prevPeriod} AND ""Level"" < {currPeriod}
                    UNION ALL
                    SELECT SUM(""BakerFee"")::bigint AS fee, SUM(COALESCE(""StorageFee"", 0))::bigint AS burn FROM ""IncreasePaidStorageOps"" WHERE ""Level"" >= {prevPeriod} AND ""Level"" < {currPeriod}
                    UNION ALL
                    SELECT SUM(""BakerFee"")::bigint AS fee, SUM(COALESCE(""AllocationFee"", 0) + COALESCE(""StorageFee"", 0))::bigint AS burn FROM ""OriginationOps"" WHERE ""Level"" >= {prevPeriod} AND ""Level"" < {currPeriod}
                    UNION ALL
                    SELECT SUM(""BakerFee"")::bigint AS fee, SUM(COALESCE(""StorageFee"", 0))::bigint AS burn FROM ""RegisterConstantOps"" WHERE ""Level"" >= {prevPeriod} AND ""Level"" < {currPeriod}
                    UNION ALL
                    SELECT SUM(""BakerFee"")::bigint AS fee, 0::bigint AS burn FROM ""RevealOps"" WHERE ""Level"" >= {prevPeriod} AND ""Level"" < {currPeriod}
                    UNION ALL
                    SELECT SUM(""BakerFee"")::bigint AS fee, 0::bigint AS burn FROM ""SetDelegateParametersOps"" WHERE ""Level"" >= {prevPeriod} AND ""Level"" < {currPeriod}
                    UNION ALL
                    SELECT SUM(""BakerFee"")::bigint AS fee, 0::bigint AS burn FROM ""SetDepositsLimitOps"" WHERE ""Level"" >= {prevPeriod} AND ""Level"" < {currPeriod}
                    UNION ALL
                    SELECT SUM(""BakerFee"")::bigint AS fee, 0::bigint AS burn FROM ""SmartRollupAddMessagesOps"" WHERE ""Level"" >= {prevPeriod} AND ""Level"" < {currPeriod}
                    UNION ALL
                    SELECT SUM(""BakerFee"")::bigint AS fee, 0::bigint AS burn FROM ""SmartRollupCementOps"" WHERE ""Level"" >= {prevPeriod} AND ""Level"" < {currPeriod}
                    UNION ALL
                    SELECT SUM(""BakerFee"")::bigint AS fee, SUM(COALESCE(""StorageFee"", 0))::bigint AS burn FROM ""SmartRollupExecuteOps"" WHERE ""Level"" >= {prevPeriod} AND ""Level"" < {currPeriod}
                    UNION ALL
                    SELECT SUM(""BakerFee"")::bigint AS fee, SUM(COALESCE(""StorageFee"", 0))::bigint AS burn FROM ""SmartRollupOriginateOps"" WHERE ""Level"" >= {prevPeriod} AND ""Level"" < {currPeriod}
                    UNION ALL
                    SELECT SUM(""BakerFee"")::bigint AS fee, 0::bigint AS burn FROM ""SmartRollupPublishOps"" WHERE ""Level"" >= {prevPeriod} AND ""Level"" < {currPeriod}
                    UNION ALL
                    SELECT SUM(""BakerFee"")::bigint AS fee, 0::bigint AS burn FROM ""SmartRollupRecoverBondOps"" WHERE ""Level"" >= {prevPeriod} AND ""Level"" < {currPeriod}
                    UNION ALL
                    SELECT SUM(""BakerFee"")::bigint AS fee, 0::bigint AS burn FROM ""SmartRollupRefuteOps"" WHERE ""Level"" >= {prevPeriod} AND ""Level"" < {currPeriod}
                    UNION ALL
                    SELECT SUM(""BakerFee"")::bigint AS fee, 0::bigint AS burn FROM ""StakingOps"" WHERE ""Level"" >= {prevPeriod} AND ""Level"" < {currPeriod}
                    UNION ALL
                    SELECT SUM(""BakerFee"")::bigint AS fee, SUM(COALESCE(""AllocationFee"", 0) + COALESCE(""StorageFee"", 0))::bigint AS burn FROM ""TransactionOps"" WHERE ""Level"" >= {prevPeriod} AND ""Level"" < {currPeriod}
                    UNION ALL
                    SELECT SUM(""BakerFee"")::bigint AS fee, SUM(COALESCE(""StorageFee"", 0))::bigint AS burn FROM ""TransferTicketOps"" WHERE ""Level"" >= {prevPeriod} AND ""Level"" < {currPeriod}
                    UNION ALL
                    SELECT SUM(""BakerFee"")::bigint AS fee, 0::bigint AS burn FROM ""TxRollupCommitOps"" WHERE ""Level"" >= {prevPeriod} AND ""Level"" < {currPeriod}
                    UNION ALL
                    SELECT SUM(""BakerFee"")::bigint AS fee, SUM(COALESCE(""StorageFee"", 0))::bigint AS burn FROM ""TxRollupDispatchTicketsOps"" WHERE ""Level"" >= {prevPeriod} AND ""Level"" < {currPeriod}
                    UNION ALL
                    SELECT SUM(""BakerFee"")::bigint AS fee, 0::bigint AS burn FROM ""TxRollupFinalizeCommitmentOps"" WHERE ""Level"" >= {prevPeriod} AND ""Level"" < {currPeriod}
                    UNION ALL
                    SELECT SUM(""BakerFee"")::bigint AS fee, SUM(COALESCE(""AllocationFee"", 0))::bigint AS burn FROM ""TxRollupOriginationOps"" WHERE ""Level"" >= {prevPeriod} AND ""Level"" < {currPeriod}
                    UNION ALL
                    SELECT SUM(""BakerFee"")::bigint AS fee, 0::bigint AS burn FROM ""TxRollupRejectionOps"" WHERE ""Level"" >= {prevPeriod} AND ""Level"" < {currPeriod}
                    UNION ALL
                    SELECT SUM(""BakerFee"")::bigint AS fee, 0::bigint AS burn FROM ""TxRollupRemoveCommitmentOps"" WHERE ""Level"" >= {prevPeriod} AND ""Level"" < {currPeriod}
                    UNION ALL
                    SELECT SUM(""BakerFee"")::bigint AS fee, 0::bigint AS burn FROM ""TxRollupReturnBondOps"" WHERE ""Level"" >= {prevPeriod} AND ""Level"" < {currPeriod}
                    UNION ALL
                    SELECT SUM(""BakerFee"")::bigint AS fee, SUM(COALESCE(""StorageFee"", 0))::bigint AS burn FROM ""TxRollupSubmitBatchOps"" WHERE ""Level"" >= {prevPeriod} AND ""Level"" < {currPeriod}
                    UNION ALL
                    SELECT SUM(""BakerFee"")::bigint AS fee, 0::bigint AS burn FROM ""UpdateConsensusKeyOps"" WHERE ""Level"" >= {prevPeriod} AND ""Level"" < {currPeriod}
                ) AS previous");
            
            var prevTxs = await db.QueryFirstAsync(
                $@"SELECT COUNT(*)::integer AS count, COALESCE(SUM(""Amount""), 0)::bigint AS volume FROM ""TransactionOps"" WHERE ""Status"" = 1 AND ""Level"" >= {prevPeriod} AND ""Level"" < {currPeriod}");
            
            var prevCalls = await db.ExecuteScalarAsync<int>(
                $@"SELECT COUNT(*)::integer FROM ""TransactionOps"" WHERE ""Status"" = 1 AND ""Entrypoint"" IS NOT NULL AND ""Level"" >= {prevPeriod} AND ""Level"" < {currPeriod}");
            
            return new TxsData
            {
                Burned = fees.burned,
                BurnedDiff = Diff(fees.burned, prevFees.burned),
                Fees = fees.paid,
                FeesDiff = Diff(fees.paid, prevFees.paid),
                Txs = txs.count,
                TxsDiff = Diff(txs.count, prevTxs.count),
                Volume = txs.volume,
                VolumeDiff = Diff(txs.volume, prevTxs.volume),
                Calls = calls,
                CallsDiff = Diff(calls, prevCalls)
            };
        }

        async Task<StakingData> GetStakingData(IDbConnection db, long totalSupply)
        {
            var protocol = Protocols.Current;

            var total = await db.QueryFirstAsync($"""
                SELECT  COUNT(*)::integer as "ActiveBakers",
                        COALESCE(SUM("StakingBalance"), 0)::bigint AS "TotalStaking",
                        COALESCE(SUM("OwnStakedBalance"), 0)::bigint AS "OwnStaked",
                        COALESCE(SUM("ExternalStakedBalance"), 0)::bigint AS "ExternalStaked",
                        COALESCE(SUM("Balance" - "OwnStakedBalance"), 0)::bigint AS "OwnDelegated",
                        COALESCE(SUM("DelegatedBalance"), 0)::bigint AS "ExternalDelegated"
                FROM "Accounts"
                WHERE "Type" = 1
                AND "Staked" = true
            """);

            var funded = await db.ExecuteScalarAsync<int>($"""
                SELECT COUNT(*)
                FROM "Accounts"
                WHERE "Type" = 1
                AND "Staked" = true
                AND "StakingBalance" >= {protocol.MinimalStake}
                """);

            var futureCycle = await db.QueryFirstAsync<Data.Models.Cycle>("""
                SELECT *
                FROM "Cycles"
                ORDER BY "Index" DESC
                LIMIT 1
                """);

            var lbSubsidyPerBlock = 5_000_000 * protocol.TimeBetweenBlocks / 60;
            
            var maxRewardsPerBlock = futureCycle.BlockReward
                + futureCycle.BlockBonusPerSlot * (protocol.EndorsersPerBlock - protocol.ConsensusThreshold)
                + futureCycle.EndorsementRewardPerSlot * protocol.EndorsersPerBlock
                + futureCycle.DalAttestationRewardPerShard * protocol.NumberOfShards
                + futureCycle.NonceRevelationReward / protocol.BlocksPerCommitment
                + futureCycle.VdfRevelationReward / protocol.BlocksPerCycle;

            var blocksPerYear = 365 * 24 * 60 * 60 / protocol.TimeBetweenBlocks;
            var totalRewardsPerYear = maxRewardsPerBlock * blocksPerYear;
            var totalCreatedPerYear = (maxRewardsPerBlock + lbSubsidyPerBlock) * blocksPerYear;

            var totalStaked = (long)total.OwnStaked + (long)total.ExternalStaked;
            var totalDelegated = (long)total.OwnDelegated + (long)total.ExternalDelegated;
            var totalBakingPower = totalStaked + totalDelegated / protocol.StakePowerMultiplier;

            return new StakingData
            {
                TotalStaking = total.TotalStaking,
                StakingPercentage = Math.Round(100.0 * total.TotalStaking / totalSupply, 2),
                AvgRoi = Math.Round(100.0 * totalRewardsPerYear / total.TotalStaking, 2),
                Inflation = Math.Round(100.0 * totalCreatedPerYear / totalSupply, 2),
                Bakers = total.ActiveBakers,
                FundedBakers = funded,
                OwnStaked = total.OwnStaked,
                OwnStakedPercentage = Math.Round(100.0 * total.OwnStaked / totalSupply, 2),
                ExternalStaked = total.ExternalStaked,
                ExternalStakedPercentage = Math.Round(100.0 * total.ExternalStaked / totalSupply, 2),
                TotalStaked = totalStaked,
                TotalStakedPercentage = Math.Round(100.0 * totalStaked / totalSupply, 2),
                OwnDelegated = total.OwnDelegated,
                OwnDelegatedPercentage = Math.Round(100.0 * total.OwnDelegated / totalSupply, 2),
                ExternalDelegated = total.ExternalDelegated,
                ExternalDelegatedPercentage = Math.Round(100.0 * total.ExternalDelegated / totalSupply, 2),
                TotalDelegated = totalDelegated,
                TotalDelegatedPercentage = Math.Round(100.0 * totalDelegated / totalSupply, 2),
                StakingApy = Math.Round(100.0 * totalRewardsPerYear / totalBakingPower, 2),
                DelegationApy = Math.Round(100.0 * totalRewardsPerYear / totalBakingPower, 2) / protocol.StakePowerMultiplier
            };
        }

        async Task<AccountsData> GetAccountsData(IDbConnection db)
        {
            var currPeriod = Times.FindLevel(State.Current.Timestamp.AddMonths(-1), SearchMode.ExactOrHigher);

            return new AccountsData
            {
                TotalAccounts = State.Current.AccountCounter,
                FundedAccounts = await AccountsRepo.GetCount(null, null, new Int64Parameter { Ge = 1_000_000 }, null, null),
                ActiveAccounts = await db.ExecuteScalarAsync<int>(
                    $@"SELECT COUNT(*)::integer FROM ""Accounts"" WHERE ""LastLevel"" >= {currPeriod}"),
                PublicAccounts = await db.ExecuteScalarAsync<int>(
                    $@"SELECT COUNT(*)::integer FROM ""Accounts"" WHERE ""Extras"" @> '{{""profile"":{{}}}}'"),
                TotalContracts = await AccountsRepo.GetContractsCount(new ContractFilter
                {
                    kind = new ContractKindParameter
                    {
                        In = [1, 2],
                    }
                })
            };
        }
        
        async Task<GovernanceData> GetGovernanceData()
        {
            var epoch = (await VotingRepo.GetEpoch(State.Current.VotingEpoch))!;
            var period = epoch.Periods.Last();
            var proposals = epoch.Proposals.OrderByDescending(x => x.VotingPower).ToList();
            var proposal = proposals.FirstOrDefault();
            var proposalExtras = proposal?.Extras == null ? null : DJson.Parse(proposal.Extras);
            
            if (period.Kind == PeriodKinds.Proposal)
            {
                return new GovernanceData
                {
                    Epoch = period.Epoch,
                    Period = period.Kind,
                    Protocol = proposals.Any() ? null : State.Current.Protocol,
                    Proposals = proposals.Select(x => new ProposalData
                    {
                        Hash = x.Hash,
                        Extras = x.Extras,
                        VotingPower = x.VotingPower,
                        VotingPowerPercentage = Math.Round(100.0 * x.VotingPower / period.TotalVotingPower, 2)
                    }).ToList(),
                    UpvotesQuorum = period.UpvotesQuorum,
                    PeriodEndTime = period.EndTime,
                    EpochStartTime = Times[epoch.FirstLevel],
                    EpochEndTime = Times[epoch.FirstLevel + (Protocols.Current.BlocksPerVoting * 5)],
                };
            }

            var result = new GovernanceData
            {
                Epoch = period.Epoch,
                Proposal = proposal!.Hash,
                Protocol = proposalExtras?.alias,
                Period = period.Kind,
                PeriodEndTime = period.EndTime,
                EpochStartTime = Times[epoch.FirstLevel],
                EpochEndTime = Times[epoch.FirstLevel + (Protocols.Current.BlocksPerVoting * 5)],
            };

            if (period.Kind is PeriodKinds.Exploration or PeriodKinds.Promotion)
            {
                var yayNaySum = (long)period.YayVotingPower! + (long)period.NayVotingPower!;
                var totalVoted = yayNaySum + (long)period.PassVotingPower!;

                result.YayVotes = yayNaySum > 0
                    ? Math.Round(100.0 * (long)period.YayVotingPower / yayNaySum, 2)
                    : 0;

                result.Participation = period.TotalVotingPower > 0
                    ? Math.Round(100.0 * totalVoted / period.TotalVotingPower, 2)
                    : 0;

                result.BallotsQuorum = Math.Round((double)period.BallotsQuorum!, 2);
                result.Supermajority = Math.Round((double)period.Supermajority!, 2);
            }

            return result;
        }
        #endregion

        async Task UpdateMarketChart(IDbConnection db)
        {
            var period = -29;
            var end = Times[State.Current.QuoteLevel];
            var start = new DateTime(end.AddDays(period).Year, end.AddDays(period).Month, end.AddDays(period).Day, (end.AddDays(period).Hour / 12) * 12, 0, 0, DateTimeKind.Utc);
            var levels = Enumerable.Range(0, 59)
                .Select(offset =>  Times.FindLevel(start.AddHours(offset * 12), SearchMode.ExactOrHigher))
                .ToList();
            MarketChart = (await db.QueryAsync<Quote>($@"
                SELECT * FROM ""Quotes"" WHERE ""Level"" IN ({string.Join(", ", levels)})
            ")).ToList();
            MarketChart.Add(QuotesRepo.GetLast());
        }


        static double Diff(long current, long previous)
        {
            return previous == 0 ? 0 : Math.Round(100.0 * (current - previous) / previous, 2);
        }
    }
    
    public static class HomeServiceExt
    {
        public static void AddHomeService(this IServiceCollection services)
        {
            services.AddSingleton<HomeService>();
        }
    }
}