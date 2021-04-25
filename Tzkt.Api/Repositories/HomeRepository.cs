using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Configuration;
using Org.BouncyCastle.Utilities;
using Tzkt.Api.Models;
using Tzkt.Api.Models.Home;
using Tzkt.Api.Services.Cache;
using Tzkt.Api.Services.Metadata;
using Delegate = Tzkt.Api.Models.Delegate;

namespace Tzkt.Api.Repositories
{
    public class HomeRepository : DbConnection
    {
        #region TabFields
        public readonly string[] AccountFields = new[]
        {
            "alias", "address", "type", "delegate", "firstActivityTime", "balance", "numTransactions", "numContracts",
            "lastActivityTime"
        };
        public readonly string[] AssetFields = new[]
        {
            "alias", "address", "tzips", "creator", "firstActivityTime", "balance", "numTransactions", "lastActivityTime"
        };

        public readonly string[] BlockFields = new[]
        {
            "timestamp", "level", "baker", "priority", "validations", "reward", "fees", "hash"
        };
        
        public readonly string[] RightsFields = new[]
        {
            "timestamp", "level", "baker", "priority"
        };
        #endregion

        private readonly AccountMetadataService Metadata;
        private readonly BakingRightsRepository Rights;
        private readonly TimeCache Time;
        private readonly BlockRepository Blocks;
        private readonly VotingRepository Voting;
        private readonly AccountRepository Accounts;
        private readonly AccountsCache AccountsCache;
        private readonly OperationRepository Operations;
        private readonly ProtocolsCache Protocols;
        private readonly StateCache State;
        private readonly QuotesRepository Quotes;
        private static HomeData Stats;
        private static Tabs Tabs;
        private static DateTime ExpirationTime;

        public HomeRepository(AccountMetadataService metadata, BakingRightsRepository rights, TimeCache time, BlockRepository blocks, VotingRepository voting, AccountRepository accounts, AccountsCache accountsCache, OperationRepository repo, 
            ProtocolsCache protocols, StateCache state, QuotesRepository quotes, IConfiguration config) : base(config)
        {
            Metadata = metadata;
            Rights = rights;
            Time = time;
            Blocks = blocks;
            Voting = voting;
            Accounts = accounts;
            AccountsCache = accountsCache;
            Operations = repo;
            Protocols = protocols;
            State = state;
            Quotes = quotes;
        }

        public static HomeData GetStatistic(Symbols symbols)
        {
            return Stats;
        }

        public static object GetTab(TabTypes tab)
        {
            return tab switch
            {
                TabTypes.Blocks => Tabs.Blocks,
                TabTypes.Accounts => Tabs.Accounts,
                TabTypes.Bakers => Tabs.Bakers,
                TabTypes.Assets => Tabs.Assets,
                _ => throw new ArgumentException("Missing tab")
            };
        }
        public async Task UpdateStats()
        {
            Tabs = await GetTabsData();
            var statistics = await GetStatistics();
            Stats = new HomeData
            {
                HeaderData = await GetHeaderData(),
                CycleData = GetCurrentCycleData(),
                TxsData = await GetTxsData(),
                StakingData = await GetStakingData(statistics),
                ContractsData = await GetContractsData(),
                MarketData = await GetMarketData(statistics),
                GovernanceData = await GetGovernanceData(),
                AccountsData = await GetAccountsData()
            };

            await UpdateCharts();
        }

        public async Task UpdateCharts()
        {
            await UpdateTxChart();
            await UpdateCallsChart();
            await UpdateStakingChart();
            await UpdateAccountsChart();
        }

        public async Task UpdateMinutesData()
        {
            Tabs.Blocks = await GetBlocks();
            Stats.CycleData = GetCurrentCycleData();
        }

        private async Task<Tabs> GetTabsData()
        {
            #region assets

            var assets = await Accounts.Get(null, new ContractKindParameter {Eq = 2}, null, null, null, null,
                new SortParameter {Desc = "numTransactions"}, null, 100, AssetFields);
           
            var assetsWithAliases = assets.Where(x => !string.IsNullOrWhiteSpace((string) x[Array.FindIndex(AssetFields, x => x.Contains("alias"))]));
            var assetsResult = assetsWithAliases as object[][] ?? assetsWithAliases.ToArray();
            if (assetsResult.Length < 10)
                assetsResult = assets.Take(10) as object[][];
            
            #endregion
            
            return new Tabs()
            {
                Blocks = await GetBlocks(),
                Accounts = await Accounts.Get(null,null,null,null,null,null,new SortParameter{Desc = "balance"},null,10, AccountFields),
                //TODO To be replaced by the BB API list
                Bakers = await Accounts.GetDelegates(new BoolParameter{Eq = true}, null,null,null,10),
                Assets = assetsResult.ToArray(),
            };
        }

        private async Task<object[][]> GetBlocks()
        {
            var level = State.Current.Level;
            var upcoming = await Rights.Get(new BakingRightTypeParameter {Eq = 0}, null, null,
                new Int32Parameter {In = Enumerable.Range(level + 1, 5).ToList()},
                null, new Int32NullParameter {Eq = 0}, new BakingRightStatusParameter {Eq = 0},
                new SortParameter {Desc = "level"}, null, 5, BlockFields);
            var bl = await Blocks.Get(null, null, null, null, new SortParameter {Desc = "level"}, null, 10, BlockFields,
                Symbols.None);

            return upcoming.Concat(bl).ToArray();
        }

        public async Task<Statistics> GetStatistics()
        {
            using var db = GetConnection();
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
                CirculatingSupply = row.TotalBootstrapped + row.TotalActivated + row.TotalCreated - row.TotalBurned -
                                    row.TotalVested - row.TotalFrozen,
            };
        }
        

        private async Task<HeaderData> GetHeaderData()
        {
            var period = 24 * 60 * 60 / Protocols.Current.TimeBetweenBlocks; //day
            var currentPeriod = State.Current.Level - period;
            var previousPeriod = currentPeriod - period;
            
            using var db = GetConnection();

            var txsAndVolume = await db.QueryFirstOrDefaultAsync($@"SELECT SUM(""Amount"") AS volume, COUNT(*) AS txs FROM ""TransactionOps"" WHERE ""Level"" >= {currentPeriod} AND ""Status"" = 1");
            var calls = (long) (await db.QueryFirstOrDefaultAsync($@"SELECT COUNT(*) AS count FROM ""TransactionOps"" WHERE ""Level"" >= {currentPeriod} AND   ""Entrypoint"" IS NOT NULL AND ""Status"" = 1")).count;
            var accounts = (long) (await db.QueryFirstOrDefaultAsync($@"SELECT COUNT(*) AS count FROM ""Accounts"" WHERE ""FirstLevel"" >= {currentPeriod}")).count;

            var prevTxsAndVolume = await db.QueryFirstOrDefaultAsync($@"SELECT SUM(""Amount"") AS volume, COUNT(*) AS txs FROM ""TransactionOps"" WHERE ""Level"" >= {previousPeriod} AND ""Level"" < {currentPeriod} AND ""Status"" = 1");
            var prevCalls = (long) (await db.QueryFirstOrDefaultAsync($@"SELECT COUNT(*) AS count FROM ""TransactionOps"" WHERE ""Level"" >= {previousPeriod} AND ""Level"" < {currentPeriod} AND ""Entrypoint"" IS NOT NULL AND ""Status"" = 1")).count;
            var prevAccounts = (long) (await db.QueryFirstOrDefaultAsync($@"SELECT COUNT(*) AS count FROM ""Accounts"" WHERE ""FirstLevel"" >= {previousPeriod} AND ""FirstLevel"" < {currentPeriod}")).count;
            
            var currentVolume = (long) txsAndVolume.volume;
            var currentTxsCount = (long) txsAndVolume.txs;

            return new HeaderData
            {
                Volume = currentVolume,
                VolumeDiff = CalculateDiff(currentVolume, (long) prevTxsAndVolume.volume),
                TxsCount = currentTxsCount,
                TxsDiff = CalculateDiff(currentTxsCount, (long) prevTxsAndVolume.txs),
                ContractCalls = calls,
                CallsDiff = CalculateDiff(calls, prevCalls),
                NewAccounts = accounts,
                NewAccountsDiff = CalculateDiff(accounts, (long) prevAccounts)
            };
        }

        private async Task<TxsData> GetTxsData()
        {
            var period = 43200; //month
            var currentPeriod = State.Current.Level - period;
            var previousPeriod = currentPeriod - period;
           
            using var db = GetConnection();
            
            var currentData = (await db.QueryFirstOrDefaultAsync($@"
                SELECT SUM(""fees"") AS paid, SUM(""burn"") AS burned FROM
                (
                    SELECT SUM(""BakerFee"") AS fees, 0 AS burn FROM ""DelegationOps"" WHERE ""Level"" >= {currentPeriod}
                    UNION ALL
                    SELECT SUM(""BakerFee"") AS fees, 0 AS burn FROM ""RevealOps"" WHERE ""Level"" >= {currentPeriod}
                    UNION ALL
                    SELECT SUM(""BakerFee"") AS fees, SUM(COALESCE(""AllocationFee"", 0) + COALESCE(""StorageFee"", 0)) AS burn FROM ""TransactionOps"" WHERE ""Level"" >= {currentPeriod}
                    UNION ALL
                    SELECT SUM(""BakerFee"") AS fees, SUM(COALESCE(""AllocationFee"", 0) + COALESCE(""StorageFee"", 0)) AS burn FROM ""OriginationOps"" WHERE ""Level"" >= {currentPeriod}
                ) AS current
                "));
            var prevData = (await db.QueryFirstOrDefaultAsync($@"
                SELECT SUM(""fees"") AS paid, SUM(""burn"") AS burned FROM
                (
                    SELECT SUM(""BakerFee"") AS fees, 0 AS burn FROM ""DelegationOps"" WHERE ""Level"" >= {previousPeriod} AND ""Level"" < {currentPeriod}
                    UNION ALL
                    SELECT SUM(""BakerFee"") AS fees, 0 AS burn FROM ""RevealOps"" WHERE ""Level"" >= {previousPeriod} AND ""Level"" < {currentPeriod}
                    UNION ALL
                    SELECT SUM(""BakerFee"") AS fees, SUM(COALESCE(""AllocationFee"", 0) + COALESCE(""StorageFee"", 0)) AS burn FROM ""TransactionOps"" WHERE ""Level"" >= {previousPeriod} AND ""Level"" < {currentPeriod}
                    UNION ALL
                    SELECT SUM(""BakerFee"") AS fees, SUM(COALESCE(""AllocationFee"", 0) + COALESCE(""StorageFee"", 0)) AS burn FROM ""OriginationOps"" WHERE ""Level"" >= {previousPeriod} AND ""Level"" < {currentPeriod}
                ) AS previous
                "));

            var txAndVol = await db.QueryFirstOrDefaultAsync($@"SELECT COUNT(*) AS txs, SUM(""Amount"") AS volume FROM ""TransactionOps"" WHERE ""Level"" >= {currentPeriod} AND ""Status"" = 1");
            var prevTxAndVol = await db.QueryFirstOrDefaultAsync($@"SELECT COUNT(*) AS txs, SUM(""Amount"") AS volume FROM ""TransactionOps"" WHERE ""Level"" >= {previousPeriod} AND ""Level"" < {currentPeriod} AND ""Status"" = 1");
            
            var currentBurned = (long) currentData.burned;
            var currentPaid = (long) (currentData.paid);
            var currentVolume = (long) (txAndVol.volume);
            var currentTxsCount = (long) (txAndVol.txs);
            
            return new TxsData
            {
                BurnedForMonth = currentBurned,
                BurnedDiff = CalculateDiff(currentBurned, (long) (prevData.burned)),
                PaidFeesForMonth = currentPaid,
                PaidDiff = CalculateDiff(currentPaid, (long) (prevData.paid)),
                TxsForMonth = currentTxsCount,
                TxsDiff = CalculateDiff(currentTxsCount, (long) prevTxAndVol.txs),
                Volume = currentVolume,
                VolumeDiff = CalculateDiff(currentVolume, (long) (prevTxAndVol.volume)),
                Chart = Stats?.TxsData?.Chart
            };
        }

        #region Charts

        
        private async Task UpdateCallsChart()
        {
            if (Stats.ContractsData.Chart == null)
            {
                
                var result = new List<ChartPoint>();
                var initialDate = new DateTime(DateTime.UtcNow.AddMonths(-11).Year, DateTime.UtcNow.AddMonths(-11).Month, 1, 0,0,0,0);

                var totalNumber = await GetCallsCountForPeriod(DateTime.MinValue, initialDate);
                for (var i = 11; i >= 0; i--)
                {
                    var start = new DateTime(DateTime.UtcNow.AddMonths(-i).Year, DateTime.UtcNow.AddMonths(-i).Month, 1, 0,0,0,0);

                    totalNumber += await GetCallsCountForPeriod(start, start.AddMonths(1));
                    result.Add(new ChartPoint
                    {
                        Month = start,
                        Value = totalNumber
                    });
                }

                Stats.ContractsData.Chart = result;
                return;
            }

            var currentMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0,0,0,0);
            
            if (Stats.ContractsData.Chart[^1].Month.Month < DateTime.UtcNow.Month)
            {

                Stats.ContractsData.Chart[^1].Value = Stats.ContractsData.Chart[^2].Value + await GetCallsCountForPeriod(currentMonth.AddMonths(-1), currentMonth);
                Stats.ContractsData.Chart.RemoveAt(0);
                Stats.ContractsData.Chart.Add(new ChartPoint
                {
                    Value = Stats.ContractsData.Chart[^1].Value + await GetCallsCountForPeriod(currentMonth, currentMonth.AddMonths(1)),
                    Month = currentMonth.AddMonths(1)
                });
                
                return;
            }
            
            Stats.ContractsData.Chart[^1].Value = Stats.ContractsData.Chart[^2].Value + await GetCallsCountForPeriod(currentMonth, currentMonth.AddMonths(1));
        }

        private async Task UpdateStakingChart()
        {
            var currentMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0,0,0,0);

            if (Stats.StakingData.Chart == null)
            {
                
                var result = new List<ChartPoint>();
                var initialDate = new DateTime(DateTime.UtcNow.AddMonths(-11).Year, DateTime.UtcNow.AddMonths(-11).Month, 1, 0,0,0,0);

                var totalNumber = await GetTotalStakingForPeriod(initialDate);
                for (var i = 11; i > 0; i--)
                {
                    var start = new DateTime(DateTime.UtcNow.AddMonths(-i).Year, DateTime.UtcNow.AddMonths(-i).Month, 1, 0,0,0,0);

                    result.Add(new ChartPoint
                    {
                        Month = start,
                        Value = await GetTotalStakingForPeriod(start.AddMonths(1))
                    });
                }
                
                result.Add(new ChartPoint
                {
                    Month = currentMonth,
                    Value = Stats.StakingData.TotalStaking
                });

                Stats.StakingData.Chart = result;
                return;
            }

            
            if (Stats.StakingData.Chart[^1].Month.Month < DateTime.UtcNow.Month)
            {

                Stats.StakingData.Chart[^1].Value = await GetTotalStakingForPeriod(currentMonth);
                Stats.StakingData.Chart.RemoveAt(0);
                Stats.StakingData.Chart.Add(new ChartPoint
                {
                    Value = Stats.StakingData.TotalStaking,
                    Month = currentMonth.AddMonths(1)
                });
                
                return;
            }
            
            Stats.StakingData.Chart[^1].Value = Stats.StakingData.TotalStaking;
        }

        private async Task UpdateAccountsChart()
        {
            var currentMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0,0,0,0);

            if (Stats.AccountsData.Chart == null)
            {
                
                var result = new List<ChartPoint>();
                var initialDate = new DateTime(DateTime.UtcNow.AddMonths(-11).Year, DateTime.UtcNow.AddMonths(-11).Month, 1, 0,0,0,0);

                var totalNumber = await GetTotalAccountsForPeriod(initialDate);
                for (var i = 11; i > 0; i--)
                {
                    var start = new DateTime(DateTime.UtcNow.AddMonths(-i).Year, DateTime.UtcNow.AddMonths(-i).Month, 1, 0,0,0,0);

                    result.Add(new ChartPoint
                    {
                        Month = start,
                        Value = await GetTotalAccountsForPeriod(start.AddMonths(1))
                    });
                }
                
                result.Add(new ChartPoint
                {
                    Month = currentMonth,
                    Value = Stats.AccountsData.TotalAccounts
                });

                Stats.AccountsData.Chart = result;
                return;
            }

            
            if (Stats.AccountsData.Chart[^1].Month.Month < DateTime.UtcNow.Month)
            {

                Stats.AccountsData.Chart[^1].Value = await GetTotalAccountsForPeriod(currentMonth);
                Stats.AccountsData.Chart.RemoveAt(0);
                Stats.AccountsData.Chart.Add(new ChartPoint
                {
                    Value = Stats.AccountsData.TotalAccounts,
                    Month = currentMonth.AddMonths(1)
                });
                
                return;
            }
            
            Stats.AccountsData.Chart[^1].Value = Stats.AccountsData.TotalAccounts;
        }
        
        private async Task UpdateTxChart()
        {
            //Get data for a year if there's no data
            if (Stats.TxsData.Chart == null)
            {
                
                var result = new List<ChartPoint>();
                var initialDate = new DateTime(DateTime.UtcNow.AddMonths(-11).Year, DateTime.UtcNow.AddMonths(-11).Month, 1, 0,0,0,0);

                long totalNumber = await GetTxCountForPeriod(DateTime.MinValue, initialDate);
                for (var i = 11; i >= 0; i--)
                {
                    var start = new DateTime(DateTime.UtcNow.AddMonths(-i).Year, DateTime.UtcNow.AddMonths(-i).Month, 1, 0,0,0,0);

                    totalNumber += await GetTxCountForPeriod(start, start.AddMonths(1));
                    result.Add(new ChartPoint
                    {
                        Month = start,
                        Value = totalNumber
                    });
                }

                Stats.TxsData.Chart = result;
                return;
            }

            var currentMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0,0,0,0);
            
            if (Stats.TxsData.Chart[^1].Month.Month < DateTime.UtcNow.Month)
            {

                Stats.TxsData.Chart[^1].Value = Stats.TxsData.Chart[^2].Value + await GetTxCountForPeriod(currentMonth.AddMonths(-1), currentMonth);
                Stats.TxsData.Chart.RemoveAt(0);
                Stats.TxsData.Chart.Add(new ChartPoint
                {
                    Value = Stats.TxsData.Chart[^1].Value + await GetTxCountForPeriod(currentMonth, currentMonth.AddMonths(1)),
                    Month = currentMonth.AddMonths(1)
                });
                
                return;
            }
            
            Stats.TxsData.Chart[^1].Value = Stats.TxsData.Chart[^2].Value + await GetTxCountForPeriod(currentMonth, currentMonth.AddMonths(1));
        }

        private async Task<long> GetTxCountForPeriod(DateTime from, DateTime to)
        {
            using var db = GetConnection();
            return (await db.QueryFirstOrDefaultAsync($@"SELECT COUNT(*) AS count FROM ""TransactionOps"" 
                                                WHERE ""Level"" < {Time.FindLevel(to, SearchMode.ExactOrLower)}
                                                AND ""Level"" >= {Time.FindLevel(from, SearchMode.ExactOrHigher)}
                                                AND ""Status"" = 1")).count;
        }   

        private async Task<long> GetCallsCountForPeriod(DateTime from, DateTime to)
        {
            using var db = GetConnection();
            return (await db.QueryFirstOrDefaultAsync($@"SELECT COUNT(*) AS count FROM ""TransactionOps"" 
                                                WHERE ""Level"" < {Time.FindLevel(to, SearchMode.ExactOrLower)}
                                                AND ""Level"" >= {Time.FindLevel(from, SearchMode.ExactOrHigher)}
                                                AND ""Status"" = 1
                                                AND ""Entrypoint"" IS NOT NULL")).count;
        }

        private async Task<long> GetTotalStakingForPeriod(DateTime to)
        {
            //TODO To be replaced by TimeCache service?
            var cycle = Time.FindLevel(to, SearchMode.ExactOrHigher) / 4096 + 1;
            using var db = GetConnection();
            return (long) (await db.QueryFirstOrDefaultAsync($@"SELECT SUM(""StakingBalance"") AS sum FROM ""BakerCycles"" WHERE ""Cycle"" = {cycle}")).sum;
        }

        private async Task<long> GetTotalAccountsForPeriod(DateTime to)
        {
            using var db = GetConnection();
            return (long) (await db.QueryFirstOrDefaultAsync($@"SELECT COUNT(*) AS count FROM ""Accounts"" WHERE ""FirstLevel"" <= {Time.FindLevel(to, SearchMode.ExactOrLower)}")).count;
        }

        #endregion



        private CycleData GetCurrentCycleData()
        {
            var cycle = State.Current.Cycle;
            var firstLevel = cycle * Protocols.Current.BlocksPerCycle + 1;
            var lastLevel = (cycle + 1) * Protocols.Current.BlocksPerCycle;
            return new CycleData
            {
                CurrentCycle = cycle,
                FirstLevel = firstLevel,
                LastLevel = lastLevel,
                CycleEndDate = Time[lastLevel],
                Progress = (State.Current.Level - firstLevel) * 100 / Protocols.Current.BlocksPerCycle
            };
        }

        private async Task<StakingData> GetStakingData(Statistics statistics)
        {
            var protocol = Protocols.Current;
            using var db = GetConnection();

            var totalStaking = (long) (await db.QueryFirstOrDefaultAsync($@"SELECT SUM(""StakingBalance"") AS sum FROM ""Accounts"" WHERE  ""Type"" = 1 AND ""Staked"" = TRUE")).sum;

            var blocksPerYear = 60 * 24 * 365 * 60 / protocol.TimeBetweenBlocks;
            var maxBlockReward = protocol.EndorsersPerBlock * (protocol.BlockReward0 + protocol.EndorsementReward0); //microtez
            var totalRewardsPerYear = (long) maxBlockReward * blocksPerYear;

            var totalRolls = (int) (await db.QueryFirstOrDefaultAsync($@"SELECT SUM((COALESCE(""StakingBalance"", 0) / {protocol.TokensPerRoll}) ::integer ) AS sum FROM ""Accounts""
            WHERE  ""Type"" = 1 AND ""Staked"" = TRUE")).sum;
            
            return new StakingData
            {
                TotalStaking = totalStaking,
                StakingPercentage = (int) (totalStaking * 100 / statistics.TotalSupply),
                AvgRoi = Math.Round((double) totalRewardsPerYear * 100 / (protocol.TokensPerRoll * totalRolls), 2),
                Inflation = Math.Round((double) totalRewardsPerYear * 100 / statistics.TotalSupply, 2),
                BakersCount = await Accounts.GetDelegatesCount(new BoolParameter{Eq = true})
            };
        }

        private async Task<ContractsData> GetContractsData()
        {
            //TODO Used storage KB with diff maybe someday

            var period = 43200; //month
            var currentPeriod = State.Current.Level - period;
            var previousMonth = currentPeriod - period;

            using var db = GetConnection();

            var burned = (long) (await db.QueryFirstOrDefaultAsync($@"
                                    SELECT SUM(""burn"") AS burned FROM
                                    (
                                        SELECT SUM(""StorageFee"") AS burn FROM ""TransactionOps"" WHERE ""Level"" >= {currentPeriod}
                                        UNION ALL
                                        SELECT SUM(""StorageFee"") AS burn FROM ""OriginationOps"" WHERE ""Level"" >= {currentPeriod}
                                    ) AS result
                                    ")).burned;

            var prevBurned = (long) (await db.QueryFirstOrDefaultAsync($@"
                                    SELECT SUM(""burn"") AS burned FROM
                                    (
                                        SELECT SUM(""StorageFee"") AS burn FROM ""TransactionOps"" WHERE ""Level"" >= {previousMonth} AND ""Level"" < {currentPeriod}
                                        UNION ALL
                                        SELECT SUM(""StorageFee"") AS burn FROM ""OriginationOps"" WHERE ""Level"" >= {previousMonth} AND ""Level"" < {currentPeriod}
                                    ) AS result
                                    ")).burned;
            
            var newCalls = (long) (await db.QueryFirstOrDefaultAsync($@"SELECT COUNT(*) AS count FROM ""TransactionOps"" 
                                                                           WHERE ""Level"" >= {currentPeriod} AND ""Entrypoint"" IS NOT NULL AND ""Status"" = 1")).count;
            var prevCalls = (long) (await db.QueryFirstOrDefaultAsync($@"SELECT COUNT(*) AS count FROM ""TransactionOps"" 
                                                                            WHERE ""Level"" >= {previousMonth} AND ""Level"" < {currentPeriod} AND ""Entrypoint"" IS NOT NULL AND ""Status"" = 1")).count;
            
            var transfers = (long) (await db.QueryFirstOrDefaultAsync($@"SELECT COUNT(*) AS count FROM ""TransactionOps"" WHERE ""Level"" >= {currentPeriod} AND   ""Entrypoint"" = 'transfer' AND ""Status"" = 1")).count;
            var prevTransfers = (long) (await db.QueryFirstOrDefaultAsync($@"SELECT COUNT(*) AS count FROM ""TransactionOps"" WHERE
                                                         ""Level"" >= {previousMonth} AND ""Level"" < {currentPeriod} AND   ""Entrypoint"" = 'transfer' AND ""Status"" = 1")).count;
            
            var contractsCount = await Accounts.GetContractsCount(new ContractKindParameter
            {
                In = new List<int> {1, 2},
            });
            
            return new ContractsData
            {
                TotalContracts = contractsCount,
                NewCalls = newCalls,
                CallsDiff = CalculateDiff(newCalls, prevCalls),
                Burned = burned,
                BurnedDiff = CalculateDiff(burned, prevBurned),
                Transfers = transfers,
                TransfersDiff = CalculateDiff(transfers, prevTransfers)
            };
        }


        private async Task<AccountsData> GetAccountsData()
        {
            var period = 43200; //month
            var currentPeriod = State.Current.Level - period;
            using var db = GetConnection();

            return new AccountsData
            {
                TotalAccounts = State.Current.AccountsCount,
                FundedAccounts = await Accounts.GetCount(null,null,new Int64Parameter{Ge = 1_000_000}, null),
                ActiveAccounts = (long) ((await db.QueryFirstOrDefaultAsync($@"SELECT COUNT(*) AS count FROM ""Accounts"" WHERE ""LastLevel"" >= {currentPeriod}")).count),
                PublicAccounts = Metadata.Aliases.Count
            };
        }

        private async Task<MarketData> GetMarketData(Statistics statistics)
        {
            return new()
            {
                TotalSupply = statistics.TotalSupply,
                CirculationSupply = statistics.CirculatingSupply,
                Quote = Quotes.GetLast(),
                PriceData = await Quotes.Get(new Int32Parameter{Ge = State.Current.Level - 50}, null, null, null, 10000)
            };
        }

        private async Task<GovernanceData> GetGovernanceData()
        {
            const int votingPeriodsCount = 5;

            var currentEpoch = await Voting.GetEpoch(State.Current.VotingEpoch);
            var period = await Voting.GetPeriod(State.Current.VotingPeriod);

            var proposals = await Voting.GetProposals(null, null, null, 100);

            var proposal = proposals.FirstOrDefault(x => x.LastPeriod == period.Index);

            var result = new GovernanceData
            {
                Hash = proposal.Hash,
                Proposal = proposal.Metadata?.Alias,
                CurrentPeriod = period.Kind,
                PeriodEnds = period.EndTime,
                ProtocolWillBeApplied = State.Current.Timestamp.AddMinutes(currentEpoch.FirstLevel + (Protocols.Current.BlocksPerVoting * votingPeriodsCount) - State.Current.Level),
            };

            if (period.Kind != "promotion" && period.Kind != "exploration") return result;
            
            var yayNaySum = (period.YayRolls ?? 0) + (period.NayRolls ?? 0);
            var totalVoted = yayNaySum + (period.PassRolls ?? 0);

            result.InFavor = yayNaySum > 0
                ? Math.Round((double) (period.YayRolls ?? 0) * 100 / yayNaySum, 2)
                : 0;

            result.Participation = period.TotalRolls > 0
                ? Math.Round(((double) totalVoted * 100 / period.TotalRolls ?? 0), 2)
                : 0;

            result.Quorum = Math.Round(period.BallotsQuorum ?? 0, 2);
            result.Supermajority = Math.Round(period.Supermajority ?? 0, 2);

            return result;
        }

        private static double CalculateDiff(long current, long previous)
        {
            return previous == 0 ? 0 : Math.Round(((((double) current - previous) / previous) * 100), 2);
        }
    }

























}