using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Tzkt.Api.Models;
using Tzkt.Api.Repositories;
using Tzkt.Api.Services.Cache;
using Tzkt.Api.Services.Metadata;

namespace Tzkt.Api.Services.Stats
{
    public class StatsService : DbConnection
    {
        #region static
        public static object[][] AccountsTab { get; set; }
        public static readonly string[] AccountFields = new[]
        {
            "alias", "address", "type", "delegate", "firstActivityTime", "balance", "numTransactions", "lastActivityTime"
        };

        public static object[][] BakersTab { get; set; }
        public static readonly string[] BakerFields = new[]
        {
            "alias", "address", "firstActivityTime", "balance", "stakingBalance", "numDelegators", "lastActivityTime"
        };

        public static object[][] AssetsTab { get; set; }
        public static readonly string[] AssetFields = new[]
        {
            "alias", "address", "tzips", "creator", "firstActivityTime", "balance", "numTransactions", "lastActivityTime"
        };

        public static object[][] BlocksTab { get; set; }
        public static readonly string[] BlockFields = new[]
        {
            "timestamp", "level", "baker", "priority", "validations", "reward", "fees", "hash"
        };
        #endregion

        #region stats

        private static DailyData DailyData;
        private static CycleData CycleData;
        private static GovernanceData GovernanceData;
        private static StakingData StakingData;
        private static List<ChartPoint> StakingChart;
        private static ContractsData ContractsData;
        private static List<ChartPoint> ContractsChart;
        private static AccountsData AccountsData;
        private static List<ChartPoint> AccountsChart;
        private static TxsData TxsData;
        private static List<ChartPoint> TxsChart;
        private static MarketData MarketData;
        private static List<Quote> MarketChart;

        #endregion

        private readonly AccountMetadataService Metadata;

        private readonly AccountRepository AccountsRepo;
        private readonly BakingRightsRepository RightsRepo;
        private readonly BlockRepository BlocksRepo;
        private readonly QuotesRepository QuotesRepo;
        private readonly StatsConfig Config;
        private readonly VotingRepository VotingRepo;

        private readonly ProtocolsCache Protocols;
        private readonly StateCache State;
        private readonly TimeCache Times;
        
        private static int LastUpdate;

        public StatsService(AccountMetadataService metadata, BakingRightsRepository rights, TimeCache times, BlockRepository blocks,
            VotingRepository voting, AccountRepository accounts, ProtocolsCache protocols,
            StateCache state, QuotesRepository quotes, IConfiguration config, ILogger<StatsService> logger) : base(config)
        {
            logger.LogDebug("Initializing accounts cache...");

            Metadata = metadata;
            RightsRepo = rights;
            Times = times;
            BlocksRepo = blocks;
            VotingRepo = voting;
            AccountsRepo = accounts;
            Protocols = protocols;
            State = state;
            QuotesRepo = quotes;
            Config = config.GetStatsConfig();
        }
        
        public static Models.Stats GetCurrentStats(Symbols quote)
        {
            if (LastUpdate <= 0)
                return null;
            
            var marketData = quote switch
            {
                Symbols.Btc => MarketChart?.Select(x => new ChartPoint<double>
                {
                    Date = x.Timestamp,
                    Value = x.Btc
                }).ToList(),
                Symbols.Cny => MarketChart?.Select(x => new ChartPoint<double>
                {
                    Date = x.Timestamp,
                    Value = x.Cny
                }).ToList(),
                Symbols.Eth => MarketChart?.Select(x => new ChartPoint<double>
                {
                    Date = x.Timestamp,
                    Value = x.Eth
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
                Symbols.None => null,
                _ => throw new ArgumentOutOfRangeException(quote.ToString())
            };
            return new Models.Stats
            {
                DailyData = DailyData,
                CycleData = CycleData,
                GovernanceData = GovernanceData,
                StakingData = StakingData,
                StakingChart = StakingChart,
                ContractsData = ContractsData,
                ContractsChart = ContractsChart,
                AccountsData = AccountsData,
                AccountsChart = AccountsChart,
                TxsData = TxsData,
                TxsChart = TxsChart,
                MarketData = MarketData,
                MarketChart = marketData
            };
        }

        public async Task UpdateAsync()
        {
            using var db = GetConnection();

            if (LastUpdate < State.Current.Level - Config.UpdatePeriod)
            {
                BlocksTab = await GetBlocks(); // 60
                AccountsTab = await GetAccounts(); // 270
                BakersTab = await GetBakers(); // 40
                AssetsTab = await GetAssets(); // 100

                var statistics = await GetStatistics(db); // 15
                
                DailyData = await GetDailyData(db); // 260
                CycleData = GetCycleData(); // 1
                TxsData = await GetTxsData(db); // 2800
                StakingData = await GetStakingData(db, statistics.TotalSupply); // 50
                ContractsData = await GetContractsData(db); // 3000
                MarketData = GetMarketData(statistics.TotalSupply, statistics.CirculatingSupply); // 1
                GovernanceData = await GetGovernanceData(); // 40
                AccountsData = await GetAccountsData(db); // 320

                await UpdateTxChart(db); // 2000
                await UpdateCallsChart(db); // 2800
                await UpdateStakingChart(db); // 30
                await UpdateAccountsChart(db); // 700
                await UpdateMarketChart(db); // 10

                LastUpdate = State.Current.Level;
            }
            else
            {
                BlocksTab = await GetBlocks();
                CycleData = GetCycleData();
            }
        }

        #region tabs
        private async Task<object[][]> GetBlocks()
        {
            var level = State.Current.Level;

            var upcoming = await RightsRepo.Get(
                new BakingRightTypeParameter { Eq = 0 },
                null, null,
                new Int32Parameter { In = Enumerable.Range(level + 1, 5).ToList() },
                null,
                new Int32NullParameter { Eq = 0 },
                null,
                new SortParameter { Desc = "level" },
                null, 5, BlockFields);

            var blocks = await BlocksRepo.Get(null, null, null, null, new SortParameter { Desc = "level" }, null, 10, BlockFields,
                Symbols.None);

            return upcoming.Concat(blocks).ToArray();
        }

        private async Task<object[][]> GetAccounts()
        {
            return await AccountsRepo.Get(null, null, null, null, null, null,
                new SortParameter { Desc = "balance" }, null, 10, AccountFields);
        }

        private async Task<object[][]> GetBakers()
        {
            return await AccountsRepo.GetDelegates(new BoolParameter { Eq = true }, null,
                new SortParameter { Desc = "stakingBalance" }, null, 10, BakerFields);
        }

        private async Task<object[][]> GetAssets()
        {
            return (await AccountsRepo.Get(new AccountTypeParameter { Eq = 2 }, new ContractKindParameter { Eq = 2 }, null, null, null, null,
                new SortParameter { Desc = "numTransactions" }, null, 100, AssetFields))
                .OrderBy(x => (string)x[0] == null)
                .ThenByDescending(x => (int)x[6])
                .Take(10)
                .ToArray();
        }
        #endregion

        #region cards
        public async Task<Statistics> GetStatistics(IDbConnection db)
        {
            var row = await db.QueryFirstOrDefaultAsync($@"SELECT * FROM ""Statistics"" WHERE ""Level"" = {State.Current.Level}");

            return new Statistics
            {
                Cycle = row.Cycle,
                Date = row.Date,
                Level = row.Level,
                TotalBootstrapped = row.TotalBootstrapped,
                TotalCommitments = row.TotalCommitments,
                TotalCreated = row.TotalCreated,
                TotalBurned = row.TotalBurned,
                TotalActivated = row.TotalActivated,
                TotalVested = row.TotalVested,
                TotalFrozen = row.TotalFrozen,
                TotalSupply = row.TotalBootstrapped + row.TotalCommitments + row.TotalCreated - row.TotalBurned,
                CirculatingSupply = row.TotalBootstrapped + row.TotalActivated + row.TotalCreated
                                  - row.TotalBurned - row.TotalVested - row.TotalFrozen,
            };
        }

        private async Task<DailyData> GetDailyData(IDbConnection db)
        {
            var period = 24 * 60 * 60 / Protocols.Current.TimeBetweenBlocks; //day
            var currPeriod = State.Current.Level - period;
            var prevPeriod = currPeriod - period;


            var txs = await db.QueryFirstOrDefaultAsync(
                $@"SELECT SUM(""Amount"")::bigint AS volume, COUNT(*)::integer AS count FROM ""TransactionOps"" WHERE ""Status"" = 1 AND ""Level"" >= {currPeriod}");
            //TODO To Scalar Execution
            var calls = await db.QueryFirstOrDefaultAsync(
                $@"SELECT COUNT(*)::integer AS count FROM ""TransactionOps"" WHERE ""Status"" = 1  AND ""Entrypoint"" IS NOT NULL AND ""Level"" >= {currPeriod}");
            var accounts = await db.QueryFirstOrDefaultAsync(
                $@"SELECT COUNT(*)::integer AS count FROM ""Accounts"" WHERE ""FirstLevel"" >= {currPeriod}");

            if (prevPeriod <= 0)
            {
                return new DailyData
                {
                    Volume = txs.volume,
                    Txs = txs.count,
                    Calls = calls.count,
                    Accounts = accounts.count,
                };
            }
            
            var prevTxs = await db.QueryFirstOrDefaultAsync(
                $@"SELECT SUM(""Amount"")::bigint AS volume, COUNT(*)::integer AS count FROM ""TransactionOps"" WHERE ""Status"" = 1 AND ""Level"" >= {prevPeriod} AND ""Level"" < {currPeriod}");
            var prevCalls = await db.QueryFirstOrDefaultAsync(
                $@"SELECT COUNT(*)::integer AS count FROM ""TransactionOps"" WHERE ""Status"" = 1  AND ""Entrypoint"" IS NOT NULL AND ""Level"" >= {prevPeriod} AND ""Level"" < {currPeriod}");
            var prevAccounts = await db.QueryFirstOrDefaultAsync(
                $@"SELECT COUNT(*)::integer AS count FROM ""Accounts"" WHERE ""FirstLevel"" >= {prevPeriod} AND ""FirstLevel"" < {currPeriod}");
            
            return new DailyData
            {
                Volume = txs.volume,
                VolumeDiff = Diff(txs.volume, prevTxs.volume),
                Txs = txs.count,
                TxsDiff = Diff(txs.count, prevTxs.count),
                Calls = calls.count,
                CallsDiff = Diff(calls.count, prevCalls.count),
                Accounts = accounts.count,
                AccountsDiff = Diff(accounts.count, prevAccounts.count)
            };
        }

        private CycleData GetCycleData()
        {
            var cycle = State.Current.Cycle;
            var cycleSize = Protocols.Current.BlocksPerCycle;
            var firstLevel = cycle * cycleSize + 1;
            var lastLevel = (cycle + 1) * cycleSize;

            return new CycleData
            {
                Cycle = cycle,
                FirstLevel = firstLevel,
                StartTime = Times[firstLevel],
                LastLevel = lastLevel,
                EndTime = Times[lastLevel],
                Progress = Math.Round(100.0 * (State.Current.Level - firstLevel) / cycleSize, 2)
            };
        }

        private async Task<TxsData> GetTxsData(IDbConnection db)
        {
            const int period = 30 * 24 * 60 * 60; //month
            var currPeriod = State.Current.Level - period;
            var prevPeriod = currPeriod - period;


            var fees = await db.QueryFirstOrDefaultAsync($@"
                SELECT SUM(fee)::bigint AS paid, SUM(burn)::bigint AS burned FROM
                (
                    SELECT SUM(""BakerFee"")::bigint AS fee, 0::bigint AS burn FROM ""DelegationOps"" WHERE ""Level"" >= {currPeriod}
                    UNION ALL
                    SELECT SUM(""BakerFee"")::bigint AS fee, 0::bigint AS burn FROM ""RevealOps"" WHERE ""Level"" >= {currPeriod}
                    UNION ALL
                    SELECT SUM(""BakerFee"")::bigint AS fee, SUM(COALESCE(""AllocationFee"", 0) + COALESCE(""StorageFee"", 0))::bigint AS burn FROM ""TransactionOps"" WHERE ""Level"" >= {currPeriod}
                    UNION ALL
                    SELECT SUM(""BakerFee"")::bigint AS fee, SUM(COALESCE(""AllocationFee"", 0) + COALESCE(""StorageFee"", 0))::bigint AS burn FROM ""OriginationOps"" WHERE ""Level"" >= {currPeriod}
                ) AS current");
            
            var txs = await db.QueryFirstOrDefaultAsync(
                $@"SELECT COUNT(*)::integer AS count, SUM(""Amount"")::bigint AS volume FROM ""TransactionOps"" WHERE ""Status"" = 1 AND ""Level"" >= {currPeriod}");

            if (prevPeriod <= 0)
            {
                return new TxsData
                {
                    Burned = fees.burned,
                    Fees = fees.paid,
                    Txs = txs.count,
                    Volume = txs.volume,
                };
            }
            
            var prevFees = await db.QueryFirstOrDefaultAsync($@"
                SELECT SUM(fee)::bigint AS paid, SUM(burn)::bigint AS burned FROM
                (
                    SELECT SUM(""BakerFee"")::bigint AS fee, 0::bigint AS burn FROM ""DelegationOps"" WHERE ""Level"" >= {prevPeriod} AND ""Level"" < {currPeriod}
                    UNION ALL
                    SELECT SUM(""BakerFee"")::bigint AS fee, 0::bigint AS burn FROM ""RevealOps"" WHERE ""Level"" >= {prevPeriod} AND ""Level"" < {currPeriod}
                    UNION ALL
                    SELECT SUM(""BakerFee"")::bigint AS fee, SUM(COALESCE(""AllocationFee"", 0) + COALESCE(""StorageFee"", 0))::bigint AS burn FROM ""TransactionOps"" WHERE ""Level"" >= {prevPeriod} AND ""Level"" < {currPeriod}
                    UNION ALL
                    SELECT SUM(""BakerFee"")::bigint AS fee, SUM(COALESCE(""AllocationFee"", 0) + COALESCE(""StorageFee"", 0))::bigint AS burn FROM ""OriginationOps"" WHERE ""Level"" >= {prevPeriod} AND ""Level"" < {currPeriod}
                ) AS previous");
            
            var prevTxs = await db.QueryFirstOrDefaultAsync(
                $@"SELECT COUNT(*)::integer AS count, SUM(""Amount"")::bigint AS volume FROM ""TransactionOps"" WHERE ""Status"" = 1 AND ""Level"" >= {prevPeriod} AND ""Level"" < {currPeriod}");
            
            return new TxsData
            {
                Burned = fees.burned,
                BurnedDiff = Diff(fees.burned, prevFees.burned),
                Fees = fees.paid,
                FeesDiff = Diff(fees.paid, prevFees.paid),
                Txs = txs.count,
                TxsDiff = Diff(txs.count, prevTxs.count),
                Volume = txs.volume,
                VolumeDiff = Diff(txs.volume, prevTxs.volume)
            };
        }

        private async Task<StakingData> GetStakingData(IDbConnection db, long totalSupply)
        {
            var protocol = Protocols.Current;

            var total = await db.QueryFirstOrDefaultAsync(
                $@"SELECT COUNT(*)::integer as bakers, SUM(""StakingBalance"")::bigint AS staking, SUM((""StakingBalance"" / {protocol.TokensPerRoll})::integer)::integer as rolls FROM ""Accounts"" WHERE ""Type"" = 1 AND ""Staked"" = true");

            var blocksPerYear = 365 * 24 * 60 * 60 / protocol.TimeBetweenBlocks;
            var maxBlockReward = protocol.EndorsersPerBlock * (protocol.BlockReward0 + protocol.EndorsementReward0); //microtez
            var totalRewardsPerYear = maxBlockReward * blocksPerYear;

            return new StakingData
            {
                TotalStaking = total.staking,
                StakingPercentage = Math.Round(100.0 * total.staking / totalSupply, 2),
                AvgRoi = Math.Round(100.0 * totalRewardsPerYear / (protocol.TokensPerRoll * total.rolls), 2),
                Inflation = Math.Round(100.0 * totalRewardsPerYear / totalSupply, 2),
                Bakers = total.bakers
            };
        }

        private async Task<ContractsData> GetContractsData(IDbConnection db)
        {
            const int period = 30 * 24 * 60 * 60; //month
            var currPeriod = State.Current.Level - period;
            var prevPeriod = currPeriod - period;

            var fees = await db.ExecuteScalarAsync<long>($@"
                SELECT SUM(burn)::bigint FROM
                (
                    SELECT SUM(""StorageFee"")::bigint AS burn FROM ""TransactionOps"" WHERE ""Level"" >= {currPeriod}
                    UNION ALL
                    SELECT SUM(""StorageFee"")::bigint AS burn FROM ""OriginationOps"" WHERE ""Level"" >= {currPeriod}
                ) AS result");

            var calls = await db.ExecuteScalarAsync<int>(
                $@"SELECT COUNT(*)::integer FROM ""TransactionOps"" WHERE ""Status"" = 1 AND ""Entrypoint"" IS NOT NULL AND ""Level"" >= {currPeriod}");

            var transfers = await db.ExecuteScalarAsync<int>(
                $@"SELECT COUNT(*)::integer FROM ""TransactionOps"" WHERE ""Status"" = 1 AND ""Entrypoint"" = 'transfer' AND ""Level"" >= {currPeriod}");

            var contractsCount = await AccountsRepo.GetContractsCount(new ContractKindParameter
            {
                In = new List<int> { 1, 2 },
            });

            if (prevPeriod <= 0)
            {
                return new ContractsData
                {
                    TotalContracts = contractsCount,
                    Calls = calls,
                    Burned = fees,
                    Transfers = transfers,
                };
            }
            
            var prevFees = await db.QueryFirstOrDefaultAsync($@"
                SELECT SUM(burn)::bigint AS burned FROM
                (
                    SELECT SUM(""StorageFee"")::bigint AS burn FROM ""TransactionOps"" WHERE ""Level"" >= {prevPeriod} AND ""Level"" < {currPeriod}
                    UNION ALL
                    SELECT SUM(""StorageFee"")::bigint AS burn FROM ""OriginationOps"" WHERE ""Level"" >= {prevPeriod} AND ""Level"" < {currPeriod}
                ) AS result");
            
            var prevCalls = await db.ExecuteScalarAsync<int>(
                $@"SELECT COUNT(*)::integer FROM ""TransactionOps"" WHERE ""Status"" = 1 AND ""Entrypoint"" IS NOT NULL AND ""Level"" >= {prevPeriod} AND ""Level"" < {currPeriod}");
            
            var prevTransfers = await db.ExecuteScalarAsync<int>(
                $@"SELECT COUNT(*)::integer FROM ""TransactionOps"" WHERE ""Status"" = 1 AND ""Entrypoint"" = 'transfer' AND ""Level"" >= {prevPeriod} AND ""Level"" < {currPeriod}");

            return new ContractsData
            {
                TotalContracts = contractsCount,
                Calls = calls,
                CallsDiff = Diff(calls, prevCalls),
                Burned = fees,
                BurnedDiff = Diff(fees, prevFees.burned),
                Transfers = transfers,
                TransfersDiff = Diff(transfers, prevTransfers)
            };
        }

        private async Task<AccountsData> GetAccountsData(IDbConnection db)
        {
            const int period = 30 * 24 * 60 * 60; //month
            var currPeriod = State.Current.Level - period;

            return new AccountsData
            {
                TotalAccounts = State.Current.AccountsCount,
                FundedAccounts = await AccountsRepo.GetCount(null, null, new Int64Parameter { Ge = 1_000_000 }, null),
                ActiveAccounts = (int)(await db.QueryFirstOrDefaultAsync(
                    $@"SELECT COUNT(*)::integer AS count FROM ""Accounts"" WHERE ""LastLevel"" >= {currPeriod}")).count,
                PublicAccounts = Metadata.Aliases.Count
            };
        }

        private MarketData GetMarketData(long totalSupply, long circulatingSupply)
        {
            return new()
            {
                TotalSupply = totalSupply,
                CirculatingSupply = circulatingSupply,
                Quote = QuotesRepo.GetLast()
            };
        }

        private async Task<GovernanceData> GetGovernanceData()
        {
            var epoch = await VotingRepo.GetEpoch(State.Current.VotingEpoch);
            var period = epoch.Periods.Last();
            var proposals = await VotingRepo.GetProposals(
                new Int32Parameter { Eq = epoch.Index },
                new SortParameter { Desc = "upvotes" },
                null, 10);
            var proposal = proposals.FirstOrDefault();

            var result = new GovernanceData
            {
                Proposal = proposal?.Hash,
                Protocol = proposal?.Metadata?.Alias,
                Period = period.Kind,
                PeriodEndTime = period.EndTime,
                EpochEndTime = Times[epoch.FirstLevel + (Protocols.Current.BlocksPerVoting * 5)],
            };

            if (period.Kind != "promotion" && period.Kind != "exploration") return result;

            var yayNaySum = (period.YayRolls ?? 0) + (period.NayRolls ?? 0);
            var totalVoted = yayNaySum + (period.PassRolls ?? 0);

            result.YayVotes = yayNaySum > 0
                ? Math.Round(100.0 * (period.YayRolls ?? 0) / yayNaySum, 2)
                : 0;

            result.Participation = period.TotalRolls > 0
                ? Math.Round(100.0 * totalVoted / period.TotalRolls ?? 0, 2)
                : 0;

            result.Quorum = Math.Round(period.BallotsQuorum ?? 0, 2);
            result.Supermajority = Math.Round(period.Supermajority ?? 0, 2);

            return result;
        }
        #endregion

        #region charts
        private async Task UpdateCallsChart(IDbConnection db)
        {
            var currentTime = State.Current.Timestamp;
            var currentMonth = new DateTime(currentTime.Year, currentTime.Month, 1, 0, 0, 0, 0, DateTimeKind.Unspecified);
            var points = GetPointsNumber(currentMonth);
            
            if (ContractsChart == null)
            {
                var result = new List<ChartPoint>(points);
                var totalNumber = await GetCallsCountForPeriod(db, DateTime.MinValue, currentMonth.AddMonths(-10));

                result.Add(new ChartPoint
                {
                    Date = currentMonth.AddMonths(-(points - 1)),
                    Value = totalNumber
                });

                for (var i = points - 2; i >= 0; i--)
                {
                    var start = currentMonth.AddMonths(-i);

                    totalNumber += await GetCallsCountForPeriod(db, start, start.AddMonths(1));
                    result.Add(new ChartPoint
                    {
                        Date = start,
                        Value = totalNumber
                    });
                }

                ContractsChart = result;
            }
            else if (ContractsChart[^1].Date < currentMonth)
            {
                ContractsChart[^1].Value = (ContractsChart.Count < 2 ? 0 : ContractsChart[^2].Value) + await GetCallsCountForPeriod(db, currentMonth.AddMonths(-1), currentMonth);
                ContractsChart.RemoveAt(0);
                ContractsChart.Add(new ChartPoint
                {
                    Value = ContractsChart[^1].Value + await GetCallsCountForPeriod(db, currentMonth, currentMonth.AddMonths(1)),
                    Date = currentMonth
                });
            }
            else
            {
                ContractsChart[^1].Value = (ContractsChart.Count < 2 ? 0 : ContractsChart[^2].Value) + await GetCallsCountForPeriod(db, currentMonth, currentMonth.AddMonths(1));
            }
        }

        private async Task UpdateStakingChart(IDbConnection db)
        {
            var currentTime = State.Current.Timestamp;
            var currentMonth = new DateTime(currentTime.Year, currentTime.Month, 1, 0, 0, 0, 0, DateTimeKind.Unspecified);
            var points = GetPointsNumber(currentMonth);
            
            if (StakingChart == null)
            {
                var result = new List<ChartPoint>(points);
                for (var i = points - 1; i > 0; i--)
                {
                    var start = currentMonth.AddMonths(-i);
                    result.Add(new ChartPoint
                    {
                        Date = start,
                        Value = await GetTotalStakingForPeriod(db, start.AddMonths(1))
                    });
                }

                result.Add(new ChartPoint
                {
                    Date = currentMonth,
                    Value = StakingData?.TotalStaking ?? 0
                });

                StakingChart = result;
            }
            else if (StakingChart[^1].Date < currentMonth)
            {
                StakingChart[^1].Value = await GetTotalStakingForPeriod(db, currentMonth);
                StakingChart.RemoveAt(0);
                StakingChart.Add(new ChartPoint
                {
                    Date = currentMonth,
                    Value = StakingData?.TotalStaking ?? 0
                });
            }
            else
            {
                StakingChart[^1].Value = StakingData?.TotalStaking ?? 0;
            }
        }

        private async Task UpdateAccountsChart(IDbConnection db)
        {
            var currentTime = State.Current.Timestamp;
            var currentMonth = new DateTime(currentTime.Year, currentTime.Month, 1, 0, 0, 0, 0, DateTimeKind.Unspecified);
            var points = GetPointsNumber(currentMonth);

            if (AccountsChart == null)
            {
                var result = new List<ChartPoint>(points);
                long totalNumber = State.Current.AccountsCount;
                
                result.Add(new ChartPoint
                {
                    Date = currentMonth,
                    Value = totalNumber
                });
                
                for (var i = 1; i < points; i++)
                {
                    var start = currentMonth.AddMonths(-i);

                    totalNumber -= await GetTotalAccountsForPeriod(db, start, start.AddMonths(1));
                    result.Insert(0, new ChartPoint
                    {
                        Date = start,
                        Value = totalNumber
                    });
                }

                AccountsChart = result;
            }
            else if (AccountsChart[^1].Date < currentMonth)
            {
                AccountsChart[^1].Value = (AccountsChart.Count < 2 ? 0 : AccountsChart[^2].Value) + await GetTotalAccountsForPeriod(db, currentMonth.AddMonths(-1), currentMonth);
                AccountsChart.RemoveAt(0);
                AccountsChart.Add(new ChartPoint
                {
                    Date = currentMonth,
                    Value = AccountsData.TotalAccounts
                });
            }
            else
            {
                AccountsChart[^1].Value = AccountsData.TotalAccounts;
            }
        }

        private int GetPointsNumber(DateTime currentMonth)
        {
            var genesisDate = Times[0];
            var monthsDiff = (currentMonth.Year - genesisDate.Year) * 12 + currentMonth.Month - genesisDate.Month + 1;
            return monthsDiff > 12 ? 12 : monthsDiff ;
        }

        private async Task UpdateTxChart(IDbConnection db)
        {
            var currentTime = State.Current.Timestamp;
            var currentMonth = new DateTime(currentTime.Year, currentTime.Month, 1, 0, 0, 0, 0, DateTimeKind.Unspecified);
            var points = GetPointsNumber(currentMonth);

            //Get data for a year if there's no data
            if (TxsChart == null)
            {
                var result = new List<ChartPoint>(points);
                long totalNumber = State.Current.TransactionOpsCount;

                result.Add(new ChartPoint
                {
                    Date = currentMonth,
                    Value = totalNumber
                });

                for (var i = 1; i < points; i++)
                {
                    var start = currentMonth.AddMonths(-i);

                    totalNumber -= await GetTxCountForPeriod(db, start, start.AddMonths(1));
                    result.Insert(0, new ChartPoint
                    {
                        Date = start,
                        Value = totalNumber
                    });
                }

                TxsChart = result;
            }
            else if (TxsChart[^1].Date < currentMonth)
            {
                TxsChart[^1].Value = (TxsChart.Count < 2 ? 0 : TxsChart[^2].Value) + await GetTxCountForPeriod(db, currentMonth.AddMonths(-1), currentMonth);
                TxsChart.RemoveAt(0);
                TxsChart.Add(new ChartPoint
                {
                    Date = currentMonth,
                    Value = State.Current.TransactionOpsCount
                });
            }
            else
            {
                TxsChart[^1].Value = State.Current.TransactionOpsCount;
            }
        }

        private async Task UpdateMarketChart(IDbConnection db)
        {
            var end = Times[State.Current.QuoteLevel];
            var start = new DateTime(end.AddMonths(-1).Year, end.AddMonths(-1).Month, end.AddMonths(-1).Day, (end.AddMonths(-1).Hour / 12) * 12, 0, 0, DateTimeKind.Unspecified) ;
            var levels = Enumerable.Range(0, 60)
                .Select(offset =>  Times.FindLevel(start.AddHours(offset * 12), SearchMode.ExactOrHigher))
                .ToList();
            //TODO IN WHERE Level < QuotesLevel
            MarketChart = (await db.QueryAsync<Quote>($@"
                SELECT * FROM ""Quotes"" WHERE ""Level"" IN ({string.Join(", ", levels)})
            ")).ToList();
        }

        private Task<int> GetTxCountForPeriod(IDbConnection db, DateTime from, DateTime to)
        {
            return db.ExecuteScalarAsync<int>($@"
                SELECT COUNT(*)::integer FROM ""TransactionOps"" 
                WHERE ""Level"" < {Times.FindLevel(to, SearchMode.ExactOrLower)}
                AND ""Level"" >= {Times.FindLevel(from, SearchMode.ExactOrHigher)}
                AND ""Status"" = 1
            ");
        }

        private Task<int> GetCallsCountForPeriod(IDbConnection db, DateTime from, DateTime to)
        {
            return db.ExecuteScalarAsync<int>($@"
                SELECT COUNT(*)::integer FROM ""TransactionOps"" 
                WHERE ""Level"" < {Times.FindLevel(to, SearchMode.ExactOrLower)}
                AND ""Level"" >= {Times.FindLevel(from, SearchMode.ExactOrHigher)}
                AND ""Status"" = 1
                AND ""Entrypoint"" IS NOT NULL
            ");
        }

        private Task<int> GetTotalAccountsForPeriod(IDbConnection db, DateTime from, DateTime to)
        {
            return db.ExecuteScalarAsync<int>($@"
                SELECT COUNT(*)::integer FROM ""Accounts""
                WHERE ""FirstLevel"" < {Times.FindLevel(to, SearchMode.ExactOrLower)}
                AND ""FirstLevel"" >= {Times.FindLevel(from, SearchMode.ExactOrHigher)}
            ");
        }

        private Task<long> GetTotalStakingForPeriod(IDbConnection db, DateTime to)
        {
            return db.ExecuteScalarAsync<long>($@"
                SELECT SUM(""StakingBalance"")::bigint FROM ""BakerCycles""
                WHERE ""Cycle"" = {(Times.FindLevel(to, SearchMode.ExactOrHigher) - 1) / Protocols.Current.BlocksPerCycle}
            ");
        }
        #endregion

        private static double Diff(long current, long previous)
        {
            return previous == 0 ? 0 : Math.Round(100.0 * (current - previous) / previous, 2);
        }
    }
    
    public static class HomeCacheExt
    {
        public static void AddHomeCache(this IServiceCollection services)
        {
            services.AddSingleton<StatsService>();
        }
    }
}