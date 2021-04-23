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
        #endregion

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

        public HomeRepository(TimeCache time, BlockRepository blocks, VotingRepository voting, AccountRepository accounts, AccountsCache accountsCache, OperationRepository repo, 
            ProtocolsCache protocols, StateCache state, QuotesRepository quotes, IConfiguration config) : base(config)
        {
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
            //TODO 12 months charts
            //TODO Reconsider all operations only for applied
            Tabs = await GetTabsData();
            var statistics = await GetStatistics();
//TODO Don't create new objects
            Stats = new HomeData
            {
                HeaderData = await GetHeaderData(),
                CycleData = GetCurrentCycleData(),
                TxsData = await GetTxsData(),
                StakingData = await GetStakingData(statistics),
                ContractsData = await GetContractsData(),
                MarketData = await GetMarketData(statistics),
                GovernanceData = await GetGovernanceData()
            };

            await UpdateCharts();
        }

        public async Task UpdateCharts()
        {
            await GetTxChart();
        }

        public async Task UpdateMinutesData()
        {
            Tabs.Blocks = await Blocks.Get(null, null, null, null, new SortParameter {Desc = "level"}, null, 10,
                BlockFields, Symbols.None);
            Stats.CycleData = GetCurrentCycleData();
        }

        private async Task<Tabs> GetTabsData()
        {
            #region accounts

            var assets = await Accounts.Get(null, new ContractKindParameter {Eq = 2}, null, null, null, null,
                new SortParameter {Desc = "numTransactions"}, null, 100, AssetFields);
           
            var assetsWithAliases = assets.Where(x => !string.IsNullOrWhiteSpace((string) x[Array.FindIndex(AssetFields, x => x.Contains("alias"))]));
            var assetsResult = assetsWithAliases as object[][] ?? assetsWithAliases.ToArray();
            if (assetsResult.Length < 10)
                assetsResult = assets.Take(10) as object[][];
            
            #endregion
            
            return new Tabs()
            {
                Blocks = await Blocks.Get(null, null, null, null, new SortParameter{Desc = "level"}, null, 10, BlockFields, Symbols.None),
                Accounts = await Accounts.Get(null,null,null,null,null,null,new SortParameter{Desc = "balance"},null,10, AccountFields),
                //TODO To be replaced by the BB API list
                Bakers = await Accounts.GetDelegates(new BoolParameter{Eq = true}, null,null,null,10),
                Assets = assetsResult.ToArray(),
            };
        }

        public async Task<Statistics> GetStatistics()
        {
            
            //TODO Reconsider using statistics
            
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

            var txsAndVolume = await db.QueryFirstOrDefaultAsync($@"SELECT SUM(""Amount"") AS volume, COUNT(*) AS txs FROM ""TransactionOps"" WHERE ""Level"" >= {currentPeriod}");
            var calls = (long) (await db.QueryFirstOrDefaultAsync($@"SELECT COUNT(*) AS count FROM ""TransactionOps"" WHERE ""Level"" >= {currentPeriod} AND   ""Entrypoint"" IS NOT NULL")).count;
            var accounts = (long) (await db.QueryFirstOrDefaultAsync($@"SELECT COUNT(*) AS count FROM ""Accounts"" WHERE ""FirstLevel"" >= {currentPeriod}")).count;

            var prevTxsAndVolume = await db.QueryFirstOrDefaultAsync($@"SELECT SUM(""Amount"") AS volume, COUNT(*) AS txs FROM ""TransactionOps"" WHERE ""Level"" >= {previousPeriod} AND ""Level"" < {currentPeriod}");
            var prevCalls = (long) (await db.QueryFirstOrDefaultAsync($@"SELECT COUNT(*) AS count FROM ""TransactionOps"" WHERE ""Level"" >= {previousPeriod} AND ""Level"" < {currentPeriod} AND ""Entrypoint"" IS NOT NULL")).count;
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
                SELECT SUM(""fees"") AS paid, SUM(""burn"") AS burned, SUM(""txs"") AS txs, SUM(""volume"") AS volume  FROM
                (
                    SELECT SUM(""BakerFee"") AS fees, 0 AS burn, 0 AS txs, 0 AS volume FROM ""DelegationOps"" WHERE ""Level"" >= {currentPeriod}
                    UNION ALL
                    SELECT SUM(""BakerFee"") AS fees, 0 AS burn, 0 AS txs, 0 AS volume FROM ""RevealOps"" WHERE ""Level"" >= {currentPeriod}
                    UNION ALL
                    SELECT SUM(""BakerFee"") AS fees, SUM(COALESCE(""AllocationFee"", 0) + COALESCE(""StorageFee"", 0)) AS burn, COUNT(*) AS txs, SUM(""Amount"") AS volume FROM ""TransactionOps"" WHERE ""Level"" >= {currentPeriod}
                    UNION ALL
                    SELECT SUM(""BakerFee"") AS fees, SUM(COALESCE(""AllocationFee"", 0) + COALESCE(""StorageFee"", 0)) AS burn, 0 AS txs, 0 AS volume FROM ""OriginationOps"" WHERE ""Level"" >= {currentPeriod}
                ) AS current
                "));
            var prevData = (await db.QueryFirstOrDefaultAsync($@"
                SELECT SUM(""fees"") AS paid, SUM(""burn"") AS burned, SUM(""txs"") AS txs, SUM(""volume"") AS volume  FROM
                (
                    SELECT SUM(""BakerFee"") AS fees, 0 AS burn, 0 AS txs, 0 AS volume FROM ""DelegationOps"" WHERE ""Level"" >= {previousPeriod} AND ""Level"" < {currentPeriod}
                    UNION ALL
                    SELECT SUM(""BakerFee"") AS fees, 0 AS burn, 0 AS txs, 0 AS volume FROM ""RevealOps"" WHERE ""Level"" >= {previousPeriod} AND ""Level"" < {currentPeriod}
                    UNION ALL
                    SELECT SUM(""BakerFee"") AS fees, SUM(COALESCE(""AllocationFee"", 0) + COALESCE(""StorageFee"", 0)) AS burn, COUNT(*) AS txs, SUM(""Amount"") AS volume FROM ""TransactionOps"" WHERE ""Level"" >= {previousPeriod} AND ""Level"" < {currentPeriod}
                    UNION ALL
                    SELECT SUM(""BakerFee"") AS fees, SUM(COALESCE(""AllocationFee"", 0) + COALESCE(""StorageFee"", 0)) AS burn, 0 AS txs, 0 AS volume FROM ""OriginationOps"" WHERE ""Level"" >= {previousPeriod} AND ""Level"" < {currentPeriod}
                ) AS previous
                "));
            
            var currentBurned = (long) currentData.burned;
            var currentPaid = (long) (currentData.paid);
            var currentVolume = (long) (currentData.volume);
            var currentTxsCount = (long) (currentData.txs);
            
            return new TxsData
            {
                BurnedForMonth = currentBurned,
                BurnedDiff = CalculateDiff(currentBurned, (long) (prevData.burned)),
                PaidFeesForMonth = currentPaid,
                PaidDiff = CalculateDiff(currentPaid, (long) (prevData.paid)),
                TxsForMonth = currentTxsCount,
                TxsDiff = CalculateDiff(currentTxsCount, (long) prevData.txs),
                Volume = currentVolume,
                VolumeDiff = CalculateDiff(currentVolume, (long) (prevData.volume)),
                Chart = Stats?.TxsData?.Chart
            };
        }

        #region Charts

        private async Task GetTxChart()
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
                                                AND ""Level"" >= {Time.FindLevel(from, SearchMode.ExactOrHigher)}")).count;
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

            var newCallsAndBurned = await db.QueryFirstOrDefaultAsync($@"
                                    SELECT SUM(""burn"") AS burned, SUM(""count"") AS count  FROM
                                    (

                                        SELECT SUM(""StorageFee"") AS burn, COUNT(*) AS count FROM ""TransactionOps"" WHERE ""Level"" >= {currentPeriod} AND ""Entrypoint"" IS NOT NULL
                                        UNION ALL
                                        SELECT SUM(""StorageFee"") AS burn, 0 AS count FROM ""OriginationOps"" WHERE ""Level"" >= {currentPeriod}
                                    ) AS result
                                    ");

            var prevCallsAndBurned = await db.QueryFirstOrDefaultAsync($@"
                                    SELECT SUM(""burn"") AS burned, SUM(""count"") AS count  FROM
                                    (

                                        SELECT SUM(""StorageFee"") AS burn, COUNT(*) AS count FROM ""TransactionOps"" WHERE ""Level"" >= {previousMonth} AND ""Level"" < {currentPeriod}   AND ""Entrypoint"" IS NOT NULL
                                        UNION ALL
                                        SELECT SUM(""StorageFee"") AS burn, 0 AS count FROM ""OriginationOps"" WHERE ""Level"" >= {previousMonth} AND ""Level"" < {currentPeriod}
                                    ) AS result
                                    ");
            ;
            var transfers = (long) (await db.QueryFirstOrDefaultAsync($@"SELECT COUNT(*) AS count FROM ""TransactionOps"" WHERE ""Level"" >= {currentPeriod} AND   ""Entrypoint"" = 'transfer'")).count;
            var prevTransfers = (long) (await db.QueryFirstOrDefaultAsync($@"SELECT COUNT(*) AS count FROM ""TransactionOps"" WHERE
                                                         ""Level"" >= {previousMonth} AND ""Level"" < {currentPeriod} AND   ""Entrypoint"" = 'transfer'")).count;
            
            var contractsCount = await Accounts.GetContractsCount(new ContractKindParameter
            {
                In = new List<int> {1, 2},
            });

                        
            var burned = (long) newCallsAndBurned.burned;
            var prevBurned = (long) prevCallsAndBurned.burned;
            var newCalls = (long) newCallsAndBurned.count;
            var prevCalls = (long) prevCallsAndBurned.count;
            
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
            var votingPeriodsCount = 5;
            var levelsFromTheBeginning = -1;

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

            if (period.Kind == "promotion" || period.Kind == "exploration")
            {
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
            }

            return result;
        }

        private double CalculateDiff(long current, long previous)
        {
            return previous == 0 ? 0 : Math.Round(((((double) current - previous) / previous) * 100), 2);
        }
    }

























}