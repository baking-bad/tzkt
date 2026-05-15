using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using App.Metrics;
using Tzkt.Data;
using Tzkt.Data.Models;
using Tzkt.Sync.Protocols;
using Tzkt.Sync.Services;

namespace Tzkt.Sync
{
    public abstract class ProtocolHandler
    {
        public abstract IActivator Activator { get; }
        public abstract IMigrator Migrator { get; }
        public abstract IValidator Validator { get; }
        public abstract IRpc Rpc { get; }
        public abstract string VersionName { get; }
        public abstract int VersionNumber { get; }

        public readonly TezosNode Node;
        public readonly TzktContext Db;
        public readonly CacheService Cache;
        public readonly QuotesService Quotes;
        public readonly IServiceProvider Services;
        public readonly TezosProtocolsConfig Config;
        public readonly ILogger Logger;
        public readonly IMetrics Metrics;
        public readonly ManagerContext Manager;
        public BlockContext Context { get; private set; }

        public ProtocolHandler(TezosNode node, TzktContext db, CacheService cache, QuotesService quotes, IServiceProvider services, IConfiguration config, ILogger logger, IMetrics metrics)
        {
            Node = node;
            Db = db;
            Cache = cache;
            Quotes = quotes;
            Services = services;
            Config = config.GetTezosProtocolsConfig();
            Logger = logger;
            Metrics = metrics;
            Manager = new(this);
            Context = new();
        }

        public ProtocolHandler WithContext(BlockContext context)
        {
            Context = context;
            return this;
        }

        public virtual async Task<AppState> CommitNextBlock()
        {
            var state = Cache.AppState.Get();
            Db.TryAttach(state);

            JsonElement block;
            Logger.LogDebug("Load block {level}", state.Level + 1);
            using (Metrics.Measure.Timer.Time(MetricsRegistry.RpcResponseTime))
            {
                block = await Rpc.GetBlockAsync(state.Level + 1);
            }

            Logger.LogDebug("Begin DB transaction");
            using var tx = await Db.Database.BeginTransactionAsync();
            try
            {
                if (state.BlocksCount == 0)
                {
                    Logger.LogDebug("Activate context");
                    await Activator.ActivateContext(state, block);
                }

                if (Config.Validation)
                {
                    Logger.LogDebug("Validate block");
                    using (Metrics.Measure.Timer.Time(MetricsRegistry.ValidationTime))
                    {
                        await Validator.ValidateBlock(block);
                    }
                }

                Logger.LogDebug("Process block");
                using (Metrics.Measure.Timer.Time(MetricsRegistry.ProcessingTime))
                {
                    await Commit(block);
                }

                Logger.LogDebug("Touch accounts");
                TouchAccounts();

                Logger.LogDebug("Save changes");
                using (Metrics.Measure.Timer.Time(MetricsRegistry.SaveChangesTime))
                {
                    Context.Apply(Db);
                    await Db.SaveChangesAsync();
                }

                Logger.LogDebug("Process quotes");
                using (Metrics.Measure.Timer.Time(MetricsRegistry.QuotesProcessingTime))
                {
                    await Quotes.Commit();
                }

                if (state.Protocol != state.NextProtocol)
                {
                    Logger.LogDebug("Migrate to {hash}", state.NextProtocol);
                    var nextProtocol = Services.GetNextBlockHandler(state).WithContext(Context);
                    await nextProtocol.Migrator.MigrateContext(state);
                    await Db.SaveChangesAsync();
                }

                Logger.LogDebug("Commit DB transaction");
                await tx.CommitAsync();
            }
            catch (Exception)
            {
                await tx.RollbackAsync();
                throw;
            }

            Cache.Trim();
            return Cache.AppState.Get();
        }
        
        public virtual async Task<AppState> RevertLastBlock()
        {
            Logger.LogDebug("Begin DB transaction");
            using var tx = await Db.Database.BeginTransactionAsync();
            try
            {
                var state = Cache.AppState.Get();
                Db.TryAttach(state);

                Logger.LogDebug("Init block context");
                await InitContext(state);

                if (state.Protocol != state.NextProtocol)
                {
                    Logger.LogDebug("Revert migration to {hash}", state.NextProtocol);
                    var nextProtocol = Services.GetNextBlockHandler(state).WithContext(Context);
                    await nextProtocol.Migrator.RevertContext(state);
                    await Db.SaveChangesAsync();
                }

                Logger.LogDebug("Revert quotes");
                using (Metrics.Measure.Timer.Time(MetricsRegistry.RevertQuotesProcessingTime))
                {
                    await Quotes.Revert();
                }

                Logger.LogDebug("Revert block");
                using (Metrics.Measure.Timer.Time(MetricsRegistry.RevertProcessingTime))
                {
                    await Revert();
                }

                if (state.BlocksCount != 0)
                {
                    Logger.LogDebug("Touch accounts");
                    ClearAccounts(state.Level + 1);
                }
                else
                {
                    Logger.LogDebug("Deactivate context");
                    await Activator.DeactivateContext(state);
                }

                Logger.LogDebug("Save changes");
                using (Metrics.Measure.Timer.Time(MetricsRegistry.RevertSaveChangesTime))
                {
                    await Context.Revert(Db);
                    await Db.SaveChangesAsync();
                }

                Logger.LogDebug("Commit DB transaction");
                await tx.CommitAsync();
            }
            catch (Exception)
            {
                await tx.RollbackAsync();
                throw;
            }

            Cache.Trim();
            return Cache.AppState.Get();
        }

        public abstract Task Commit(JsonElement block);

        public abstract Task Revert();

        async Task InitContext(AppState state)
        {
            var currBlock = Cache.Blocks.Get(state.Level);
            Context.Block = currBlock;
            Context.Protocol = await Cache.Protocols.GetAsync(currBlock.ProtoCode);

            if (currBlock.Operations.HasFlag(Operations.Originations))
                Context.OriginationOps = await Db.OriginationOps.AsNoTracking().Where(x => x.Level == currBlock.Level).ToListAsync();

            if (currBlock.Operations.HasFlag(Operations.Transactions))
                Context.TransactionOps = await Db.TransactionOps.AsNoTracking().Where(x => x.Level == currBlock.Level).ToListAsync();

            if (currBlock.Operations.HasFlag(Operations.Reveals))
                Context.RevealOps = await Db.RevealOps.AsNoTracking().Where(x => x.Level == currBlock.Level).ToListAsync();

            if (currBlock.Operations.HasFlag(Operations.RegisterConstant))
                Context.RegisterConstantOps = await Db.RegisterConstantOps.AsNoTracking().Where(x => x.Level == currBlock.Level).ToListAsync();

            if (currBlock.Operations.HasFlag(Operations.IncreasePaidStorage))
                Context.IncreasePaidStorageOps = await Db.IncreasePaidStorageOps.AsNoTracking().Where(x => x.Level == currBlock.Level).ToListAsync();

            if (currBlock.Operations.HasFlag(Operations.TransferTicket))
                Context.TransferTicketOps = await Db.TransferTicketOps.AsNoTracking().Where(x => x.Level == currBlock.Level).ToListAsync();

            if (currBlock.Operations.HasFlag(Operations.Migrations))
                Context.MigrationOps = await Db.MigrationOps.AsNoTracking().Where(x => x.Level == currBlock.Level).ToListAsync();

            if (currBlock.Events.HasFlag(BlockEvents.NewAccounts))
            {
                var createdAccounts = await Db.Accounts
                    .Where(x => x.FirstLevel == currBlock.Level)
                    .ToListAsync();

                foreach (var account in createdAccounts)
                    Cache.Accounts.Add(account);
            }
        }

        void TouchAccounts()
        {
            var state = Cache.AppState.Get();
            var block = (Db.ChangeTracker.Entries()
                .First(x => x.Entity is Block block && block.Level == state.Level).Entity as Block)!;

            foreach (var entry in Db.ChangeTracker.Entries().Where(x => x.Entity is Account).ToList())
            {
                var account = (entry.Entity as Account)!;

                if (entry.State == EntityState.Modified)
                {
                    account.LastLevel = state.Level;
                }
                else if (entry.State == EntityState.Added)
                {
                    if (account.FirstLevel == block.Level)
                        block.Events |= BlockEvents.NewAccounts;
                }
            }
        }

        void ClearAccounts(int level)
        {
            var state = Cache.AppState.Get();

            foreach (var entry in Db.ChangeTracker.Entries().Where(x => x.Entity is Account).ToList())
            {
                var account = (entry.Entity as Account)!;

                if (entry.State == EntityState.Modified)
                    account.LastLevel = level;

                if (account.FirstLevel == level)
                {
                    Db.Accounts.Remove(account);
                    Cache.Accounts.Remove(account);
                    Cache.AppState.ReleaseAccountId();
                }
            }
        }
    }
}
