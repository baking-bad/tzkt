using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using Npgsql;
using Tzkt.Data.Models;
using Tzkt.Sync.Utils;

namespace Tzkt.Sync.Protocols.Initiator
{
    class BootstrapCommit : ProtocolCommit
    {
        public IEnumerable<Commitment> Commitments { get; private set; }
        public List<Account> BootstrapedAccounts { get; private set; }
        public Block Block { get; private set; }

        BootstrapCommit(ProtocolHandler protocol) : base(protocol) { }

        public async Task Init(Block block, JsonElement rawBlock)
        {
            BootstrapedAccounts = new List<Account>(65);
            Block = block;

            var contracts = await Proto.Rpc.GetAllContractsAsync(block.Level);
            var delegates = new List<Data.Models.Delegate>(8);

            #region bootstrap delegates
            foreach (var data in contracts.EnumerateArray().Where(x => x[0].RequiredString() == x[1].OptionalString("delegate")))
            {
                var baker = new Data.Models.Delegate
                {
                    Id = Cache.AppState.NextAccountId(),
                    Address = data[0].RequiredString(),
                    Balance = data[1].RequiredInt64("balance"),
                    Counter = data[1].RequiredInt32("counter"),
                    PublicKey = data[1].RequiredString("manager"),
                    FirstLevel = block.Level,
                    LastLevel = block.Level,
                    ActivationLevel = 1,
                    DeactivationLevel = GracePeriod.Init(1, block.Protocol.BlocksPerCycle, block.Protocol.PreservedCycles),
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
            foreach (var data in contracts.EnumerateArray().Where(x => x[0].RequiredString()[0] == 't' && string.IsNullOrEmpty(x[1].OptionalString("delegate"))))
            {
                var user = new User
                {
                    Id = Cache.AppState.NextAccountId(),
                    Address = data[0].RequiredString(),
                    Balance = data[1].RequiredInt64("balance"),
                    Counter = data[1].RequiredInt32("counter"),
                    FirstLevel = block.Level,
                    LastLevel = block.Level,
                    Type = AccountType.User
                };
                Cache.Accounts.Add(user);
                BootstrapedAccounts.Add(user);
            }
            #endregion

            #region bootstrap contracts
            foreach (var data in contracts.EnumerateArray().Where(x => x[0].RequiredString()[0] == 'K'))
            {
                var manager = (User)await Cache.Accounts.GetAsync(data[1].RequiredString("manager"));
                manager.ContractsCount++;

                var contract = new Contract
                {
                    Id = Cache.AppState.NextAccountId(),
                    Address = data[0].RequiredString(),
                    Balance = data[1].RequiredInt64("balance"),
                    Counter = data[1].RequiredInt32("counter"),
                    FirstLevel = block.Level,
                    LastLevel = block.Level,
                    Spendable = false,
                    DelegationLevel = 1,
                    Delegate = Cache.Accounts.GetDelegate(data[1].OptionalString("delegate")),
                    Manager = manager,
                    Staked = !string.IsNullOrEmpty(data[1].OptionalString("delegate")),
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
            var protoParams = Bson.Parse(rawBlock.Required("header").Required("content").RequiredString("protocol_parameters").Substring(8));
            Commitments = protoParams["commitments"]?.Select(x => new Commitment
            {
                Address = x[0].Value<string>(),
                Balance = x[1].Value<long>()
            });
            #endregion
        }

        public async Task Init(Block block)
        {
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
                        Level = Block.Level,
                        Timestamp = Block.Timestamp,
                        Account = account,
                        Kind = MigrationKind.Bootstrap,
                        BalanceChange = account.Balance
                    });
                }

                state.MigrationOpsCount += BootstrapedAccounts.Count;
            }

            if (Commitments != null)
            {
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
            }

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
        public static async Task<BootstrapCommit> Apply(ProtocolHandler proto, Block block, JsonElement rawBlock)
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
