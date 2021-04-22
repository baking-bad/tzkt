using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Configuration;
using Org.BouncyCastle.Utilities;
using Tzkt.Api.Models;
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
            //TODO Reconsider all operations only for applied
            //TODO All queries to string
            //TODO All Now to UTCNow
            Tabs = await GetTabsData();
            var cycleInfo = await GetCycles();
            var statistics = await GetStatistics();
//TODO Don't create new objects
            Stats = new HomeData
            {
                HeaderData = await GetHeaderData(),
                CycleData = GetCurrentCycleData(cycleInfo.Index),
                TxsData = await GetTxsData(),
                StakingData = GetStakingData(cycleInfo, statistics),
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
            Stats.CycleData = GetCurrentCycleData(await GetCycleIndex());
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

        public async Task<Cycle> GetCycles()
        {
            //TODO Get rid of it with v1.5 State
            var sql = new SqlBuilder($@"SELECT * FROM ""Cycles""")
                .Filter("Index", State.Current.CyclesCount - Protocols.Current.PreservedCycles - 1);

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            return rows.Select(row => new Cycle
            {
                Index = row.Index,
                //TODO From accounts
                TotalBakers = row.TotalBakers,
                //TODO From accounts

                TotalStaking = row.TotalStaking,
            }).FirstOrDefault();
        }

        public async Task<int> GetCycleIndex()
        {
            //TODO Get rid of it with v1.5 State
            var sql = new SqlBuilder($@"SELECT ""Index"" FROM ""Cycles""")
                .Filter("Index", State.Current.CyclesCount - Protocols.Current.PreservedCycles - 1);

            using var db = GetConnection();
            var row = await db.QueryFirstOrDefaultAsync(sql.Query, sql.Params);

            return row.Index;
        }

        public async Task<Statistics> GetStatistics()
        {
            var sql = new SqlBuilder($@"SELECT * FROM ""Statistics"" WHERE ""Level"" = {State.Current.Level}");

            using var db = GetConnection();
            var row = await db.QueryFirstOrDefaultAsync(sql.Query, sql.Params);

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
            var period = 1440 * 60 / Protocols.Current.TimeBetweenBlocks; //day
            var currentPeriod = State.Current.Level - period;
            var previousPeriod = currentPeriod - period;
            
            var txsAndVolumeQuery = new SqlBuilder($@"SELECT SUM(""Amount"") AS volume, COUNT(*) AS txs FROM ""TransactionOps"" WHERE ""Level"" >= {currentPeriod}");
            var prevTxsAndVolumeQuery = new SqlBuilder($@"SELECT SUM(""Amount"") AS volume, COUNT(*) AS txs FROM ""TransactionOps"" WHERE ""Level"" >= {previousPeriod} AND ""Level"" < {currentPeriod}");

            var callsQuery = new SqlBuilder($@"SELECT COUNT(*) AS count FROM ""TransactionOps"" WHERE ""Level"" >= {currentPeriod} AND   ""Entrypoint"" IS NOT NULL");
            var prevCallsQuery = new SqlBuilder($@"SELECT COUNT(*) AS count FROM ""TransactionOps"" WHERE ""Level"" >= {previousPeriod} AND ""Level"" < {currentPeriod} AND ""Entrypoint"" IS NOT NULL");
            
            var accountsQuery = new SqlBuilder($@"SELECT COUNT(*) AS count FROM ""Accounts"" WHERE ""FirstLevel"" >= {currentPeriod}");            
            var prevAccountsQuery = new SqlBuilder($@"SELECT COUNT(*) AS count FROM ""Accounts"" WHERE ""FirstLevel"" >= {previousPeriod} AND ""FirstLevel"" < {currentPeriod}");            

            using var db = GetConnection();

            var txsAndVolume = await db.QueryFirstOrDefaultAsync(txsAndVolumeQuery.Query, txsAndVolumeQuery.Params);
            var calls = (long) (await db.QueryFirstOrDefaultAsync(callsQuery.Query, callsQuery.Params)).count;
            var accounts = (long) (await db.QueryFirstOrDefaultAsync(accountsQuery.Query, accountsQuery.Params)).count;

            var prevTxsAndVolume = await db.QueryFirstOrDefaultAsync(prevTxsAndVolumeQuery.Query, prevTxsAndVolumeQuery.Params);
            var prevCalls = (long) (await db.QueryFirstOrDefaultAsync(prevCallsQuery.Query, prevCallsQuery.Params)).count;
            var prevAccounts = (long) (await db.QueryFirstOrDefaultAsync(prevAccountsQuery.Query, prevAccountsQuery.Params)).count;
            
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
            
            var feesQuery = new SqlBuilder($@"
                SELECT SUM(""fees"") AS paid, SUM(""burn"") AS burned  FROM
                (
                    SELECT SUM(""BakerFee"") AS fees, 0 AS burn FROM ""DelegationOps"" WHERE ""Level"" >= {currentPeriod}
                    UNION ALL
                    SELECT SUM(""BakerFee"") AS fees, 0 AS burn FROM ""RevealOps"" WHERE ""Level"" >= {currentPeriod}
                    UNION ALL
                    SELECT SUM(""BakerFee"") AS fees, SUM(COALESCE(""AllocationFee"", 0) + COALESCE(""StorageFee"", 0)) AS burn FROM ""TransactionOps"" WHERE ""Level"" >= {currentPeriod}
                    UNION ALL
                    SELECT SUM(""BakerFee"") AS fees, SUM(COALESCE(""AllocationFee"", 0) + COALESCE(""StorageFee"", 0)) AS burn FROM ""OriginationOps"" WHERE ""Level"" >= {currentPeriod}
                ) AS current
                ");
            
            var prevFeesQuery = new SqlBuilder($@"
                SELECT SUM(""fees"") AS paid, SUM(""burn"") AS burned  FROM
                (
                    SELECT SUM(""BakerFee"") AS fees, 0 AS burn FROM ""DelegationOps"" WHERE ""Level"" >= {previousPeriod} AND ""Level"" < {currentPeriod}
                    UNION ALL
                    SELECT SUM(""BakerFee"") AS fees, 0 AS burn FROM ""RevealOps"" WHERE ""Level"" >= {previousPeriod} AND ""Level"" < {currentPeriod}
                    UNION ALL
                    SELECT SUM(""BakerFee"") AS fees, SUM(COALESCE(""AllocationFee"", 0) + COALESCE(""StorageFee"", 0)) AS burn FROM ""TransactionOps"" WHERE ""Level"" >= {previousPeriod} AND ""Level"" < {currentPeriod}
                    UNION ALL
                    SELECT SUM(""BakerFee"") AS fees, SUM(COALESCE(""AllocationFee"", 0) + COALESCE(""StorageFee"", 0)) AS burn FROM ""OriginationOps"" WHERE ""Level"" >= {previousPeriod} AND ""Level"" < {currentPeriod}
                ) AS previous
                ");
//TODO to fees query
            var txsQuery = new SqlBuilder($@"SELECT COUNT(*) AS txs, SUM(""Amount"") AS volume FROM ""TransactionOps"" WHERE ""Level"" >= {currentPeriod}");
            var prevTxsQuery = new SqlBuilder($@"SELECT COUNT(*) AS txs, SUM(""Amount"") AS volume FROM ""TransactionOps"" WHERE ""Level"" >= {previousPeriod} AND ""Level"" < {currentPeriod}");
            
            using var db = GetConnection();
            
            var fees = (await db.QueryFirstOrDefaultAsync(feesQuery.Query, feesQuery.Params));
            var prevFees = (await db.QueryFirstOrDefaultAsync(prevFeesQuery.Query, prevFeesQuery.Params));
            
            var txsAndVolume = (await db.QueryFirstOrDefaultAsync(txsQuery.Query, txsQuery.Params));
            var prevTxsAndVolume = (await db.QueryFirstOrDefaultAsync(prevTxsQuery.Query, prevTxsQuery.Params));
            
            var currentBurned = (long) (fees.burned);
            var currentPaid = (long) (fees.paid);
            var currentVolume = (long) (txsAndVolume.volume);
            var currentTxsCount = txsAndVolume.txs;
            
            return new TxsData
            {
                BurnedForMonth = currentBurned,
                BurnedDiff = CalculateDiff(currentBurned, (long) (prevFees.burned)),
                PaidFeesForMonth = currentPaid,
                PaidDiff = CalculateDiff(currentPaid, (long) (prevFees.paid)),
                TxsForMonth = currentTxsCount,
                TxsDiff = CalculateDiff(currentTxsCount, prevTxsAndVolume.txs),
                Volume = currentVolume,
                VolumeDiff = CalculateDiff(currentVolume, (long) (prevTxsAndVolume.volume)),
                Chart = Stats?.TxsData?.Chart
            };
        }

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
            var txsQuery = new SqlBuilder($@"SELECT COUNT(*) AS count FROM ""TransactionOps"" 
                                                WHERE ""Level"" < {Time.FindLevel(to, SearchMode.ExactOrLower)}
                                                AND ""Level"" >= {Time.FindLevel(from, SearchMode.ExactOrHigher)}");
            using var db = GetConnection();

            return (await db.QueryFirstOrDefaultAsync(txsQuery.Query, txsQuery.Params)).count;
        }

        private CycleData GetCurrentCycleData(int index)
        {
            return new CycleData
            {
                CurrentCycle = index,
                FirstLevel = index * Protocols.Current.BlocksPerCycle + 1,
                LastLevel = (index + 1) * Protocols.Current.BlocksPerCycle,
                CycleEndDate = Time[(index + 1) * Protocols.Current.BlocksPerCycle],
                Progress = (State.Current.Level - index * Protocols.Current.BlocksPerCycle + 1) * 100 / Protocols.Current.BlocksPerCycle
            };
        }

        private StakingData GetStakingData(Cycle cycleInfo, Statistics statistics)
        {
            const int blocksPerYear = 60 * 24 * 365;
            var protocol = Protocols.Current;
            var maxBlockReward = protocol.EndorsersPerBlock * (protocol.BlockReward0 + protocol.EndorsementReward0); //microtez
            var tokensPerRoll = protocol.TokensPerRoll;
            var totalRewardsPerYear = (long) maxBlockReward * blocksPerYear;
            
            return new StakingData
            {
                TotalStaking = cycleInfo.TotalStaking,
                StakingPercentage = (int) (cycleInfo.TotalStaking * 100 / statistics.TotalSupply),
                //TODO ROI depends on rolls instead of staking.
                AvgRoi = Math.Round((decimal) totalRewardsPerYear * 100 / cycleInfo.TotalStaking, 2),
                Inflation = Math.Round((decimal) totalRewardsPerYear * 100 / statistics.TotalSupply, 2),
                //TODO Get from accounts
                BakersCount = cycleInfo.TotalBakers
            };
        }

        private async Task<ContractsData> GetContractsData()
        {
            //TODO Used storage KB with diff maybe someday

            var period = 43200; //month
            
            var day = State.Current.Level - 1440;
            var currentPeriod = State.Current.Level - period;

            var previousMonth = currentPeriod - period;
            //TODO To fees query
            var txsQuery = new SqlBuilder($@"SELECT COUNT(*) AS count FROM ""TransactionOps"" WHERE ""Level"" >= {currentPeriod} AND   ""Entrypoint"" IS NOT NULL");
            var previousTxsQuery = new SqlBuilder($@"SELECT COUNT(*) AS count FROM ""TransactionOps"" WHERE ""Level"" >= {previousMonth} AND ""Level"" < {currentPeriod} AND ""Entrypoint"" IS NOT NULL");
            var feesQuery = new SqlBuilder($@"
                                    SELECT SUM(""burn"") AS burned  FROM
                                    (

                                        SELECT SUM(""StorageFee"") AS burn FROM ""TransactionOps"" WHERE ""Level"" >= {currentPeriod}
                                        UNION ALL
                                        SELECT SUM(""StorageFee"") AS burn FROM ""OriginationOps"" WHERE ""Level"" >= {currentPeriod}
                                    ) AS result
                                    ");
            var prevFeesQuery = new SqlBuilder($@"
                                    SELECT SUM(""burn"") AS burned  FROM
                                    (

                                        SELECT SUM(""StorageFee"") AS burn FROM ""TransactionOps"" WHERE ""Level"" >= {previousMonth} AND ""Level"" < {currentPeriod}
                                        UNION ALL
                                        SELECT SUM(""StorageFee"") AS burn FROM ""OriginationOps"" WHERE ""Level"" >= {previousMonth} AND ""Level"" < {currentPeriod}
                                    ) AS result
                                    ");
            //TODO add kind = asset
            var transfersQuery = new SqlBuilder($@"SELECT COUNT(*) AS count FROM ""TransactionOps"" WHERE ""Level"" >= {currentPeriod} AND   ""Entrypoint"" = 'transfer'");
            var prevTransfersQuery = new SqlBuilder($@"SELECT COUNT(*) AS count FROM ""TransactionOps"" WHERE
                                                         ""Level"" >= {previousMonth} AND ""Level"" < {currentPeriod} AND   ""Entrypoint"" = 'transfer'");


            using var db = GetConnection();

            var newCalls = (long) (await db.QueryFirstOrDefaultAsync(txsQuery.Query, txsQuery.Params)).count;

            var prevCalls = (long) (await db.QueryFirstOrDefaultAsync(previousTxsQuery.Query, previousTxsQuery.Params)).count;
            
            var burned = (long) (await db.QueryFirstOrDefaultAsync(feesQuery.Query, feesQuery.Params)).burned;

            var prevBurned = (long) (await db.QueryFirstOrDefaultAsync(prevFeesQuery.Query, prevFeesQuery.Params)).burned;

            var transfers = (long) (await db.QueryFirstOrDefaultAsync(transfersQuery.Query, transfersQuery.Params)).count;

            var prevTransfers = (long) (await db.QueryFirstOrDefaultAsync(prevTransfersQuery.Query, prevTransfersQuery.Params)).count;

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
    
    //TODO All models to directory
    public class HomeData
    {
        public HeaderData HeaderData { get; set; }
        public CycleData CycleData { get; set; }
        public TxsData TxsData { get; set; }
        public StakingData StakingData { get; set; }
        public ContractsData ContractsData { get; set; }
        public MarketData MarketData { get; set; }
        public GovernanceData GovernanceData { get; set; }
    }

    public class HeaderData
    {
        public long Volume { get; set; }
        public double VolumeDiff { get; set; }
        public long TxsCount { get; set; }
        public double TxsDiff { get; set; }
        public long ContractCalls { get; set; }
        public double CallsDiff { get; set; }
        public long NewAccounts { get; set; }
        public double NewAccountsDiff { get; set; }
    }
    

    public class TxsData
    {
        //TODO Collect 12 month amount of total txs with DateTime
        public List<ChartPoint> Chart { get; set; }
        public long TxsForMonth { get; set; }
        public double TxsDiff { get; set; }
        
        public long Volume { get; set; }
        public double VolumeDiff { get; set; }

        public long PaidFeesForMonth { get; set; }
        public double PaidDiff { get; set; }

        public long BurnedForMonth { get; set; }
        public double BurnedDiff { get; set; }
    }



    public class CycleData
    {
        public int CurrentCycle { get; set; }
        public int FirstLevel { get; set; }
        public int LastLevel { get; set; }
        public int Progress { get; set; }
        public DateTime CycleEndDate { get; set; }
    }

    public class StakingData
    {
        //TODO Collect 12 month amount of total staking
        public List<ChartPoint> Chart { get; set; }

        public long TotalStaking { get; set; }
        public int StakingPercentage { get; set; }
        public decimal AvgRoi { get; set; }
        public decimal Inflation { get; set; }
        public int BakersCount { get; set; }
    }

    public class ContractsData
    {
        public List<ChartPoint> Chart { get; set; }
        public long TotalContracts { get; set; }
        public long NewCalls { get; set; }
        public double CallsDiff { get; set; }
        
        public long Transfers { get; set; }
        public double TransfersDiff { get; set; }
        
        public long Burned { get; set; }
        public double BurnedDiff { get; set; }
    }

    public class MarketData
    {
        public long TotalSupply { get; set; }
        public long CirculationSupply { get; set; }
        public Quote Quote { get; set; }
        public IEnumerable<Quote> PriceData { get; set; }
    }

    public class Tabs
    {
        //TODO Oncoming blocks
        public object[][] Blocks { get; set; }
        public object[][] Accounts { get; set; }
        public object[][] Assets { get; set; }
        public IEnumerable<Delegate> Bakers { get; set; }
    }

    public class BlockData
    {
        public int Level { get; set; }
        
        public string Hash { get; set; }
        
        public int Priority { get; set; }
        public DateTime Timestamp { get; set; }
        public int Validations { get; set; }
        public long Reward { get; set; }
        public long Fees { get; set; }
        public Alias Baker { get; set; }

    }
    
    public class GovernanceData
    {
        public string Proposal { get; set; }
        public string CurrentPeriod { get; set; }
        public DateTime PeriodEnds { get; set; }
        public DateTime ProtocolWillBeApplied { get; set; }
        public string Hash { get; set; }
        public double? Supermajority { get; set; }
        public double? InFavor { get; set; }
        
        public double? Quorum { get; set; }
        public double? Participation { get; set; }
    }

    public class AccountsData
    {
        public long TotalAccounts { get; set; }
        public List<ChartPoint> Chart { get; set; }
        public long FundedAccounts { get; set; }
        public long ActiveAccounts { get; set; }
        public long PublicAccounts { get; set; }
     }

    public class ChartPoint
    {
        public DateTime Month { get; set; }
        public long Value { get; set; }
    }

    public enum TabTypes
    {
        Blocks,
        Accounts,
        Bakers,
        Assets
    }
}