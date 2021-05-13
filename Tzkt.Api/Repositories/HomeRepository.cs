using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Dapper;
using Tzkt.Api.Models;
using Tzkt.Api.Models.Home;
using Tzkt.Api.Services.Cache;
using Tzkt.Api.Services.Metadata;

namespace Tzkt.Api.Repositories
{
    public class HomeRepository : DbConnection
    {
        #region static
        public static HomeData Stats;

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

        private readonly AccountMetadataService Metadata;

        private readonly AccountRepository AccountsRepo;
        private readonly BakingRightsRepository RightsRepo;
        private readonly BlockRepository BlocksRepo;
        private readonly OperationRepository OperationsRepo;
        private readonly QuotesRepository QuotesRepo;
        private readonly VotingRepository VotingRepo;

        private readonly ProtocolsCache Protocols;
        private readonly StateCache State;
        private readonly TimeCache Times;

        public HomeRepository(AccountMetadataService metadata, BakingRightsRepository rights, TimeCache times, BlockRepository blocks,
            VotingRepository voting, AccountRepository accounts, AccountsCache accountsCache, OperationRepository operations,
            ProtocolsCache protocols, StateCache state, QuotesRepository quotes, IConfiguration config) : base(config)
        {
            Metadata = metadata;
            RightsRepo = rights;
            Times = times;
            BlocksRepo = blocks;
            VotingRepo = voting;
            AccountsRepo = accounts;
            OperationsRepo = operations;
            Protocols = protocols;
            State = state;
            QuotesRepo = quotes;
        }

        public async Task UpdateStats()
        {
            BlocksTab = await GetBlocks();
            AccountsTab = await GetAccounts();
            BakersTab = await GetBakers();
            AssetsTab = await GetAssets();

            var statistics = await GetStatistics();
            Stats = new HomeData
            {
                DailyData = await GetDailyData(),
                CycleData = GetCycleData(),
                TxsData = await GetTxsData(),
                StakingData = await GetStakingData(statistics.TotalSupply),
                ContractsData = await GetContractsData(),
                MarketData = GetMarketData(statistics.TotalSupply, statistics.CirculatingSupply),
                GovernanceData = await GetGovernanceData(),
                AccountsData = await GetAccountsData(),
                AccountsChart = Stats?.AccountsChart,
                ContractsChart = Stats?.ContractsChart,
                MarketChart = Stats?.MarketChart,
                StakingChart = Stats?.StakingChart,
                TxsChart = Stats?.TxsChart
            };

            await UpdateTxChart();
            await UpdateCallsChart();
            await UpdateStakingChart();
            await UpdateAccountsChart();
            await UpdateMarketChart();
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
                .OrderByDescending(x => ((string)x[0])?.Length ?? 0)
                .ThenByDescending(x => (int)x[6])
                .Take(10)
                .ToArray();
        }
        #endregion

        #region cards
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
                CirculatingSupply = row.TotalBootstrapped + row.TotalActivated + row.TotalCreated
                                  - row.TotalBurned - row.TotalVested - row.TotalFrozen,
            };
        }

        private async Task<DailyData> GetDailyData()
        {
            var period = 24 * 60 * 60 / Protocols.Current.TimeBetweenBlocks; //day
            var currPeriod = State.Current.Level - period;
            var prevPeriod = currPeriod - period;

            using var db = GetConnection();

            var txs = await db.QueryFirstOrDefaultAsync(
                $@"SELECT SUM(""Amount"")::bigint AS volume, COUNT(*)::integer AS count FROM ""TransactionOps"" WHERE ""Status"" = 1 AND ""Level"" >= {currPeriod}");
            var prevTxs = await db.QueryFirstOrDefaultAsync(
                $@"SELECT SUM(""Amount"")::bigint AS volume, COUNT(*)::integer AS count FROM ""TransactionOps"" WHERE ""Status"" = 1 AND ""Level"" >= {prevPeriod} AND ""Level"" < {currPeriod}");

            var calls = await db.QueryFirstOrDefaultAsync(
                $@"SELECT COUNT(*)::integer AS count FROM ""TransactionOps"" WHERE ""Status"" = 1  AND ""Entrypoint"" IS NOT NULL AND ""Level"" >= {currPeriod}");
            var prevCalls = await db.QueryFirstOrDefaultAsync(
                $@"SELECT COUNT(*)::integer AS count FROM ""TransactionOps"" WHERE ""Status"" = 1  AND ""Entrypoint"" IS NOT NULL AND ""Level"" >= {prevPeriod} AND ""Level"" < {currPeriod}");

            var accounts = await db.QueryFirstOrDefaultAsync(
                $@"SELECT COUNT(*)::integer AS count FROM ""Accounts"" WHERE ""FirstLevel"" >= {currPeriod}");
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

        private async Task<TxsData> GetTxsData()
        {
            var period = 43_200; //month
            var currPeriod = State.Current.Level - period;
            var prevPeriod = currPeriod - period;

            using var db = GetConnection();

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

            var txs = await db.QueryFirstOrDefaultAsync(
                $@"SELECT COUNT(*)::integer AS count, SUM(""Amount"")::bigint AS volume FROM ""TransactionOps"" WHERE ""Status"" = 1 AND ""Level"" >= {currPeriod}");
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

        private async Task<StakingData> GetStakingData(long totalSupply)
        {
            var protocol = Protocols.Current;
            using var db = GetConnection();

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

        private async Task<ContractsData> GetContractsData()
        {
            var period = 43_200; //month
            var currPeriod = State.Current.Level - period;
            var prevPeriod = currPeriod - period;

            using var db = GetConnection();

            var fees = await db.QueryFirstOrDefaultAsync($@"
                SELECT SUM(burn)::bigint AS burned FROM
                (
                    SELECT SUM(""StorageFee"")::bigint AS burn FROM ""TransactionOps"" WHERE ""Level"" >= {currPeriod}
                    UNION ALL
                    SELECT SUM(""StorageFee"")::bigint AS burn FROM ""OriginationOps"" WHERE ""Level"" >= {currPeriod}
                ) AS result");
            var prevFees = await db.QueryFirstOrDefaultAsync($@"
                SELECT SUM(burn)::bigint AS burned FROM
                (
                    SELECT SUM(""StorageFee"")::bigint AS burn FROM ""TransactionOps"" WHERE ""Level"" >= {prevPeriod} AND ""Level"" < {currPeriod}
                    UNION ALL
                    SELECT SUM(""StorageFee"")::bigint AS burn FROM ""OriginationOps"" WHERE ""Level"" >= {prevPeriod} AND ""Level"" < {currPeriod}
                ) AS result");

            var calls = await db.QueryFirstOrDefaultAsync(
                $@"SELECT COUNT(*)::integer AS count FROM ""TransactionOps"" WHERE ""Status"" = 1 AND ""Entrypoint"" IS NOT NULL AND ""Level"" >= {currPeriod}");
            var prevCalls = await db.QueryFirstOrDefaultAsync(
                $@"SELECT COUNT(*)::integer AS count FROM ""TransactionOps"" WHERE ""Status"" = 1 AND ""Entrypoint"" IS NOT NULL AND ""Level"" >= {prevPeriod} AND ""Level"" < {currPeriod}");

            var transfers = await db.QueryFirstOrDefaultAsync(
                $@"SELECT COUNT(*)::integer AS count FROM ""TransactionOps"" WHERE ""Status"" = 1 AND ""Entrypoint"" = 'transfer' AND ""Level"" >= {currPeriod}");
            var prevTransfers = await db.QueryFirstOrDefaultAsync(
                $@"SELECT COUNT(*)::integer AS count FROM ""TransactionOps"" WHERE ""Status"" = 1 AND ""Entrypoint"" = 'transfer' AND ""Level"" >= {prevPeriod} AND ""Level"" < {currPeriod}");

            var contractsCount = await AccountsRepo.GetContractsCount(new ContractKindParameter
            {
                In = new List<int> { 1, 2 },
            });

            return new ContractsData
            {
                TotalContracts = contractsCount,
                Calls = calls.count,
                CallsDiff = Diff(calls.count, prevCalls.count),
                Burned = fees.burned,
                BurnedDiff = Diff(fees.burned, prevFees.burned),
                Transfers = transfers.count,
                TransfersDiff = Diff(transfers.count, prevTransfers.count)
            };
        }

        private async Task<AccountsData> GetAccountsData()
        {
            var period = 43_200; //month
            var currPeriod = State.Current.Level - period;

            using var db = GetConnection();

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
            return new MarketData
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
                Proposal = proposal.Hash,
                Protocol = proposal.Metadata?.Alias,
                Period = period.Kind,
                PeriodEndTime = period.EndTime,
                EpochEndTime = State.Current.Timestamp.AddMinutes(epoch.FirstLevel + (Protocols.Current.BlocksPerVoting * 5) - State.Current.Level),
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
        private async Task UpdateCallsChart()
        {
            if (Stats.ContractsChart == null)
            {

                var result = new List<ChartPoint>();
                var initialDate = new DateTime(DateTime.UtcNow.AddMonths(-11).Year, DateTime.UtcNow.AddMonths(-11).Month, 1, 0, 0, 0, 0);

                var totalNumber = await GetCallsCountForPeriod(DateTime.MinValue, initialDate);
                for (var i = 11; i >= 0; i--)
                {
                    var start = new DateTime(DateTime.UtcNow.AddMonths(-i).Year, DateTime.UtcNow.AddMonths(-i).Month, 1, 0, 0, 0, 0);

                    totalNumber += await GetCallsCountForPeriod(start, start.AddMonths(1));
                    result.Add(new ChartPoint
                    {
                        Date = start,
                        Value = totalNumber
                    });
                }

                Stats.ContractsChart = result;
                return;
            }

            var currentMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, 0);

            if (Stats.ContractsChart[^1].Date.Month < DateTime.UtcNow.Month)
            {

                Stats.ContractsChart[^1].Value = Stats.ContractsChart[^2].Value + await GetCallsCountForPeriod(currentMonth.AddMonths(-1), currentMonth);
                Stats.ContractsChart.RemoveAt(0);
                Stats.ContractsChart.Add(new ChartPoint
                {
                    Value = Stats.ContractsChart[^1].Value + await GetCallsCountForPeriod(currentMonth, currentMonth.AddMonths(1)),
                    Date = currentMonth.AddMonths(1)
                });

                return;
            }

            Stats.ContractsChart[^1].Value = Stats.ContractsChart[^2].Value + await GetCallsCountForPeriod(currentMonth, currentMonth.AddMonths(1));
        }

        private async Task UpdateStakingChart()
        {
            var currentMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, 0);

            if (Stats.StakingChart == null)
            {

                var result = new List<ChartPoint>();
                var initialDate = new DateTime(DateTime.UtcNow.AddMonths(-11).Year, DateTime.UtcNow.AddMonths(-11).Month, 1, 0, 0, 0, 0);

                var totalNumber = await GetTotalStakingForPeriod(initialDate);
                for (var i = 11; i > 0; i--)
                {
                    var start = new DateTime(DateTime.UtcNow.AddMonths(-i).Year, DateTime.UtcNow.AddMonths(-i).Month, 1, 0, 0, 0, 0);

                    result.Add(new ChartPoint
                    {
                        Date = start,
                        Value = await GetTotalStakingForPeriod(start.AddMonths(1))
                    });
                }

                result.Add(new ChartPoint
                {
                    Date = currentMonth,
                    Value = Stats.StakingData.TotalStaking
                });

                Stats.StakingChart = result;
                return;
            }


            if (Stats.StakingChart[^1].Date.Month < DateTime.UtcNow.Month)
            {

                Stats.StakingChart[^1].Value = await GetTotalStakingForPeriod(currentMonth);
                Stats.StakingChart.RemoveAt(0);
                Stats.StakingChart.Add(new ChartPoint
                {
                    Value = Stats.StakingData.TotalStaking,
                    Date = currentMonth.AddMonths(1)
                });

                return;
            }

            Stats.StakingChart[^1].Value = Stats.StakingData.TotalStaking;
        }

        private async Task UpdateAccountsChart()
        {
            var currentMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, 0);

            if (Stats.AccountsChart == null)
            {
                var result = new List<ChartPoint>();
                var initialDate = new DateTime(DateTime.UtcNow.AddMonths(-11).Year, DateTime.UtcNow.AddMonths(-11).Month, 1, 0, 0, 0, 0);

                var totalNumber = await GetTotalAccountsForPeriod(initialDate);
                for (var i = 11; i > 0; i--)
                {
                    var start = new DateTime(DateTime.UtcNow.AddMonths(-i).Year, DateTime.UtcNow.AddMonths(-i).Month, 1, 0, 0, 0, 0);

                    result.Add(new ChartPoint
                    {
                        Date = start,
                        Value = await GetTotalAccountsForPeriod(start.AddMonths(1))
                    });
                }

                result.Add(new ChartPoint
                {
                    Date = currentMonth,
                    Value = Stats.AccountsData.TotalAccounts
                });

                Stats.AccountsChart = result;
                return;
            }


            if (Stats.AccountsChart[^1].Date.Month < DateTime.UtcNow.Month)
            {

                Stats.AccountsChart[^1].Value = await GetTotalAccountsForPeriod(currentMonth);
                Stats.AccountsChart.RemoveAt(0);
                Stats.AccountsChart.Add(new ChartPoint
                {
                    Value = Stats.AccountsData.TotalAccounts,
                    Date = currentMonth.AddMonths(1)
                });

                return;
            }

            Stats.AccountsChart[^1].Value = Stats.AccountsData.TotalAccounts;
        }

        private async Task UpdateTxChart()
        {
            //Get data for a year if there's no data
            if (Stats.TxsChart == null)
            {

                var result = new List<ChartPoint>();
                var initialDate = new DateTime(DateTime.UtcNow.AddMonths(-11).Year, DateTime.UtcNow.AddMonths(-11).Month, 1, 0, 0, 0, 0);

                long totalNumber = await GetTxCountForPeriod(DateTime.MinValue, initialDate);
                for (var i = 11; i >= 0; i--)
                {
                    var start = new DateTime(DateTime.UtcNow.AddMonths(-i).Year, DateTime.UtcNow.AddMonths(-i).Month, 1, 0, 0, 0, 0);

                    totalNumber += await GetTxCountForPeriod(start, start.AddMonths(1));
                    result.Add(new ChartPoint
                    {
                        Date = start,
                        Value = totalNumber
                    });
                }

                Stats.TxsChart = result;
                return;
            }

            var currentMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, 0);

            if (Stats.TxsChart[^1].Date.Month < DateTime.UtcNow.Month)
            {
                Stats.TxsChart[^1].Value = Stats.TxsChart[^2].Value + await GetTxCountForPeriod(currentMonth.AddMonths(-1), currentMonth);
                Stats.TxsChart.RemoveAt(0);
                Stats.TxsChart.Add(new ChartPoint
                {
                    Value = Stats.TxsChart[^1].Value + await GetTxCountForPeriod(currentMonth, currentMonth.AddMonths(1)),
                    Date = currentMonth.AddMonths(1)
                });
                return;
            }

            Stats.TxsChart[^1].Value = Stats.TxsChart[^2].Value + await GetTxCountForPeriod(currentMonth, currentMonth.AddMonths(1));
        }

        private async Task UpdateMarketChart()
        {
            var quotes = await QuotesRepo.Get(new Int32Parameter { Gt = State.Current.Level - 50 }, null, null, null, 50);
            Stats.MarketChart = quotes.Select(x => new ChartPoint<double> { Date = x.Timestamp, Value = x.Usd }).ToList();
        }

        private async Task<long> GetTxCountForPeriod(DateTime from, DateTime to)
        {
            using var db = GetConnection();
            return (await db.QueryFirstOrDefaultAsync($@"SELECT COUNT(*) AS count FROM ""TransactionOps"" 
                                                WHERE ""Level"" < {Times.FindLevel(to, SearchMode.ExactOrLower)}
                                                AND ""Level"" >= {Times.FindLevel(from, SearchMode.ExactOrHigher)}
                                                AND ""Status"" = 1")).count;
        }

        private async Task<long> GetCallsCountForPeriod(DateTime from, DateTime to)
        {
            using var db = GetConnection();
            return (await db.QueryFirstOrDefaultAsync($@"SELECT COUNT(*) AS count FROM ""TransactionOps"" 
                                                WHERE ""Level"" < {Times.FindLevel(to, SearchMode.ExactOrLower)}
                                                AND ""Level"" >= {Times.FindLevel(from, SearchMode.ExactOrHigher)}
                                                AND ""Status"" = 1
                                                AND ""Entrypoint"" IS NOT NULL")).count;
        }

        private async Task<long> GetTotalStakingForPeriod(DateTime to)
        {
            var cycle = Times.FindLevel(to, SearchMode.ExactOrHigher) / 4096 + 1;
            using var db = GetConnection();
            return (long)(await db.QueryFirstOrDefaultAsync($@"SELECT SUM(""StakingBalance"") AS sum FROM ""BakerCycles"" WHERE ""Cycle"" = {cycle}")).sum;
        }

        private async Task<long> GetTotalAccountsForPeriod(DateTime to)
        {
            using var db = GetConnection();
            return (long)(await db.QueryFirstOrDefaultAsync($@"SELECT COUNT(*) AS count FROM ""Accounts"" WHERE ""FirstLevel"" <= {Times.FindLevel(to, SearchMode.ExactOrLower)}")).count;
        }
        #endregion

        private static double Diff(long current, long previous)
        {
            return previous == 0 ? 0 : Math.Round(100.0 * (current - previous) / previous, 2);
        }
    }
}