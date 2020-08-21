using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Tzkt.Data.Models;
using Tzkt.Sync.Utils;

namespace Tzkt.Sync.Protocols.Initiator
{
    class BootstrapCommit : ProtocolCommit
    {
        public int Level { get; private set; }
        public DateTime Timestamp { get; private set; }
        public IEnumerable<Commitment> Commitments { get; private set; }
        public List<Account> BootstrapedAccounts { get; private set; }
        public Block Block { get; private set; }

        BootstrapCommit(ProtocolHandler protocol) : base(protocol) { }

        public async Task Init(Block block, RawBlock rawBlock)
        {
            var protocol = await Cache.Protocols.GetAsync(rawBlock.Protocol);
            BootstrapedAccounts = new List<Account>(65);
            Timestamp = rawBlock.Header.Timestamp;
            Level = rawBlock.Level;
            Block = block;

            var stream = await Proto.Node.GetContractsAsync(level: 1);
            var contracts = await (Proto.Serializer as Serializer).DeserializeContracts(stream);
            var delegates = new List<Data.Models.Delegate>(8);

            #region bootstrap delegates
            foreach (var data in contracts.Where(x => x.Delegate == x.Address))
            {
                var baker = new Data.Models.Delegate
                {
                    Id = Cache.AppState.NextAccountId(),
                    Address = data.Address,
                    FirstLevel = rawBlock.Level,
                    LastLevel = rawBlock.Level,
                    ActivationLevel = 1,
                    DeactivationLevel = GracePeriod.Init(1, protocol.BlocksPerCycle, protocol.PreservedCycles),
                    Balance = data.Balance,
                    Counter = data.Counter,
                    PublicKey = data.Manager,
                    Staked = true,
                    Revealed = true,
                    Type = AccountType.Delegate
                };
                Cache.Accounts.Add(baker);
                BootstrapedAccounts.Add(baker);
                delegates.Add(baker);
            }
            #endregion

            #region bootstrap users
            foreach (var data in contracts.Where(x => x.Address[0] == 't' && String.IsNullOrEmpty(x.Delegate)))
            {
                var user = new User
                {
                    Id = Cache.AppState.NextAccountId(),
                    Address = data.Address,
                    FirstLevel = rawBlock.Level,
                    LastLevel = rawBlock.Level,
                    Balance = data.Balance,
                    Counter = data.Counter,
                    Type = AccountType.User
                };
                Cache.Accounts.Add(user);
                BootstrapedAccounts.Add(user);
            }
            #endregion

            #region bootstrap contracts
            foreach (var data in contracts.Where(x => x.Address[0] == 'K'))
            {
                var manager = (User)await Cache.Accounts.GetAsync(data.Manager);
                manager.ContractsCount++;

                var contract = new Contract
                {
                    Id = Cache.AppState.NextAccountId(),
                    Address = data.Address,
                    FirstLevel = rawBlock.Level,
                    LastLevel = rawBlock.Level,
                    Balance = data.Balance,
                    Counter = data.Counter,
                    Spendable = false,
                    DelegationLevel = 1,
                    Delegate = Cache.Accounts.GetDelegate(data.Delegate),
                    Manager = manager,
                    Staked = !String.IsNullOrEmpty(data.Delegate),
                    Type = AccountType.Contract,
                    Kind = ContractKind.SmartContract,
                };

                Cache.Accounts.Add(contract);
                BootstrapedAccounts.Add(contract);
            }
            #endregion

            #region stats
            foreach (var baker in delegates)
            {
                var delegators = BootstrapedAccounts.Where(x => x.Delegate == baker);

                baker.DelegatorsCount = delegators.Count();
                baker.StakingBalance = baker.Balance
                    + (baker.DelegatorsCount > 0 ? delegators.Sum(x => x.Balance) : 0);
            }
            #endregion

            #region parameters
            var protoParams = Bson.Parse<RawProtoParameters>(rawBlock.Header.Content.Parameters.Substring(8));
            Commitments = protoParams.Commitments.Select(x => new Commitment
            {
                Address = x[0],
                Balance = long.Parse(x[1])
            });
            #endregion
        }

        public async Task Init(Block block)
        {
            Level = block.Level;
            Timestamp = block.Timestamp;
            BootstrapedAccounts = await Db.Accounts.Where(x => x.Counter == 0).ToListAsync();
        }

        public override Task Apply()
        {
            var state = Cache.AppState.Get();

            if (BootstrapedAccounts.Count > 0)
            {
                Db.Accounts.AddRange(BootstrapedAccounts);
                
                Block.Operations |= Operations.Migrations;
                if (BootstrapedAccounts.Any(x => x.Type == AccountType.Contract))
                    Block.Events |= BlockEvents.SmartContracts;

                foreach (var account in BootstrapedAccounts)
                {
                    account.MigrationsCount++;
                    Db.MigrationOps.Add(new MigrationOperation
                    {
                        Id = Cache.AppState.NextOperationId(),
                        Block = Block,
                        Level = Level,
                        Timestamp = Timestamp,
                        Account = account,
                        Kind = MigrationKind.Bootstrap,
                        BalanceChange = account.Balance
                    });
                }

                state.MigrationOpsCount += BootstrapedAccounts.Count;
            }

            var conn = Db.Database.GetDbConnection() as NpgsqlConnection;
            using var writer = conn.BeginBinaryImport(@"COPY ""Commitments"" (""Balance"", ""Address"") FROM STDIN (FORMAT BINARY)");

            foreach (var commitment in Commitments)
            {
                writer.StartRow();
                writer.Write(commitment.Balance);
                writer.Write(commitment.Address);

                state.CommitmentsCount++;
            }

            writer.Complete();

            return Task.CompletedTask;
        }

        public override async Task Revert()
        {
            await Db.Database.ExecuteSqlRawAsync(@"DELETE FROM ""Commitments""");
            Db.Accounts.RemoveRange(BootstrapedAccounts);
            Cache.Accounts.Remove(BootstrapedAccounts);

            var migrationOps = await Db.MigrationOps
                .AsNoTracking()
                .Where(x => x.Kind == MigrationKind.Bootstrap)
                .ToListAsync();

            Db.MigrationOps.RemoveRange(migrationOps);

            var state = Cache.AppState.Get();
            Db.TryAttach(state);
            state.MigrationOpsCount -= migrationOps.Count;
            state.CommitmentsCount = 0;
        }

        #region static
        public static async Task<BootstrapCommit> Apply(ProtocolHandler proto, Block block, RawBlock rawBlock)
        {
            var commit = new BootstrapCommit(proto);
            await commit.Init(block, rawBlock);
            await commit.Apply();

            return commit;
        }

        public static async Task<BootstrapCommit> Revert(ProtocolHandler proto, Block block)
        {
            var commit = new BootstrapCommit(proto);
            await commit.Init(block);
            await commit.Revert();

            return commit;
        }
        #endregion
    }
}
