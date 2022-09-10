using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using Tzkt.Data;
using Tzkt.Data.Models;
using Tzkt.Sync.Protocols;
using Tzkt.Sync.Services;

namespace Tzkt.Sync
{
    public abstract class ProtocolHandler
    {
        public abstract IDiagnostics Diagnostics { get; }
        public abstract IValidator Validator { get; }
        public abstract IRpc Rpc { get; }

        public readonly TezosNode Node;
        public readonly TzktContext Db;
        public readonly CacheService Cache;
        public readonly QuotesService Quotes;
        public readonly IServiceProvider Services;
        public readonly TezosProtocolsConfig Config;
        public readonly ILogger Logger;
        public readonly ManagerContext Manager;
        
        public ProtocolHandler(TezosNode node, TzktContext db, CacheService cache, QuotesService quotes, IServiceProvider services, IConfiguration config, ILogger logger)
        {
            Node = node;
            Db = db;
            Cache = cache;
            Quotes = quotes;
            Services = services;
            Config = config.GetTezosProtocolsConfig();
            Logger = logger;
            Manager = new(this);
        }

        public virtual async Task<AppState> CommitBlock(int head)
        {
            var state = Cache.AppState.Get();
            Db.TryAttach(state);

            Logger.LogDebug($"Load block {state.Level + 1}");
            var block = await Rpc.GetBlockAsync(state.Level + 1);

            Logger.LogDebug("Begin DB transaction");
            using var tx = await Db.Database.BeginTransactionAsync();
            try
            {
                Logger.LogDebug("Warm up cache");
                await WarmUpCache(block);

                if (Config.Validation)
                {
                    Logger.LogDebug("Validate block");
                    await Validator.ValidateBlock(block);
                }

                Logger.LogDebug("Process block");
                await Commit(block);

                var nextProtocol = this;
                if (state.Protocol != state.NextProtocol)
                {
                    Logger.LogDebug($"Activate next protocol {state.NextProtocol.Substring(0, 8)}");
                    nextProtocol = Services.GetProtocolHandler(state.Level + 1, state.NextProtocol);
                    await nextProtocol.Activate(state, block);
                }

                Logger.LogDebug("Touch accounts");
                TouchAccounts();

                if (Config.Diagnostics)
                {
                    Logger.LogDebug("Diagnostics");
                    await nextProtocol.Diagnostics.Run(block);
                }

                Logger.LogDebug("Save changes");
                await Db.SaveChangesAsync();

                Logger.LogDebug("Save post-changes");
                await AfterCommit(block);

                Logger.LogDebug("Process quotes");
                await Quotes.Commit();

                if (state.Protocol != state.NextProtocol)
                {
                    Logger.LogDebug("Run post activation");
                    await nextProtocol.PostActivation(state);
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
            ClearCachedRelations();
            return Cache.AppState.Get();
        }
        
        public virtual async Task<AppState> RevertLastBlock(string predecessor)
        {
            Logger.LogDebug("Begin DB transaction");
            using var tx = await Db.Database.BeginTransactionAsync();
            try
            {
                var state = Cache.AppState.Get();
                Db.TryAttach(state);

                var nextProtocol = this;
                if (state.Protocol != state.NextProtocol)
                {
                    Logger.LogDebug("Run pre deactivation");
                    nextProtocol = Services.GetProtocolHandler(state.Level + 1, state.NextProtocol);
                    await nextProtocol.PreDeactivation(state);
                }

                Logger.LogDebug("Revert quotes");
                await Quotes.Revert();

                Logger.LogDebug("Revert post-changes");
                await BeforeRevert();

                if (state.Protocol != state.NextProtocol)
                {
                    Logger.LogDebug("Deactivate latest protocol");
                    await nextProtocol.Deactivate(state);
                }

                Logger.LogDebug("Revert block");
                await Revert();

                Logger.LogDebug("Touch accounts");
                ClearAccounts(state.Level + 1);

                if (Config.Diagnostics && state.Hash == predecessor)
                {
                    Logger.LogDebug("Diagnostics");
                    await Diagnostics.Run(state.Level);
                }

                Logger.LogDebug("Save changes");
                await Db.SaveChangesAsync();

                Logger.LogDebug("Commit DB transaction");
                await tx.CommitAsync();
            }
            catch (Exception)
            {
                await tx.RollbackAsync();
                throw;
            }

            Cache.Trim();
            ClearCachedRelations();
            return Cache.AppState.Get();
        }

        public virtual Task WarmUpCache(JsonElement block)
        {
            var accounts = new HashSet<string>(64);
            var operations = block.RequiredArray("operations", 4);

            foreach (var op in operations[2].RequiredArray().EnumerateArray())
            {
                var content = op.RequiredArray("contents", 1)[0];
                if (content.RequiredString("kind") == "activate_account")
                    accounts.Add(content.RequiredString("pkh"));
            }

            foreach (var op in operations[3].RequiredArray().EnumerateArray())
            {
                foreach (var content in op.RequiredArray("contents").EnumerateArray())
                {
                    accounts.Add(content.RequiredString("source"));
                    if (content.RequiredString("kind") == "transaction")
                    {
                        if (content.TryGetProperty("destination", out var dest))
                            accounts.Add(dest.GetString());

                        if (content.Required("metadata").TryGetProperty("internal_operation_results", out var internalResults))
                            foreach (var internalContent in internalResults.RequiredArray().EnumerateArray())
                            {
                                accounts.Add(internalContent.RequiredString("source"));
                                if (internalContent.RequiredString("kind") == "transaction")
                                {
                                    if (internalContent.TryGetProperty("destination", out var internalDest))
                                        accounts.Add(internalDest.GetString());
                                }
                            }
                    }
                }
            }

            return Cache.Accounts.LoadAsync(accounts);
        }

        public virtual Task PostActivation(AppState state) => Task.CompletedTask;

        public virtual Task PreDeactivation(AppState state) => Task.CompletedTask;

        public virtual Task Activate(AppState state, JsonElement block) => Task.CompletedTask;

        public virtual Task Deactivate(AppState state) => Task.CompletedTask;

        public virtual Task AfterCommit(JsonElement block) => Task.CompletedTask;

        public virtual Task BeforeRevert() => Task.CompletedTask;

        public abstract Task Commit(JsonElement block);

        public abstract Task Revert();

        void TouchAccounts()
        {
            var state = Cache.AppState.Get();
            var block = Db.ChangeTracker.Entries()
                .First(x => x.Entity is Block block && block.Level == state.Level).Entity as Block;

            foreach (var entry in Db.ChangeTracker.Entries().Where(x => x.Entity is Account))
            {
                var account = entry.Entity as Account;

                if (entry.State == EntityState.Modified)
                {
                    account.LastLevel = state.Level;
                }
                else if (entry.State == EntityState.Added)
                {
                    state.AccountsCount++;
                    if (account.FirstLevel == block.Level)
                        block.Events |= BlockEvents.NewAccounts;
                }
            }
        }

        void ClearAccounts(int level)
        {
            var state = Cache.AppState.Get();

            foreach (var entry in Db.ChangeTracker.Entries().Where(x => x.Entity is Account))
            {
                var account = entry.Entity as Account;

                if (entry.State == EntityState.Modified)
                    account.LastLevel = level;

                if (account.FirstLevel == level)
                {
                    Db.Accounts.Remove(account);
                    Cache.Accounts.Remove(account);
                    Cache.AppState.ReleaseAccountId();
                    state.AccountsCount--;
                }
            }
        }

        void ClearCachedRelations()
        {
            foreach (var entry in Db.ChangeTracker.Entries())
            {
                switch(entry.Entity)
                {
                    case Data.Models.Delegate delegat:
                        delegat.Delegate = null;
                        delegat.DelegatedAccounts = null;
                        delegat.FirstBlock = null;
                        delegat.Software = null;
                        break;
                    case User user:
                        user.Delegate = null;
                        user.FirstBlock = null;
                        break;
                    case Contract contract:
                        contract.Delegate = null;
                        contract.WeirdDelegate = null;
                        contract.Manager = null;
                        contract.Creator = null;
                        contract.FirstBlock = null;
                        break;
                    case Rollup rollup:
                        rollup.Delegate = null;
                        rollup.FirstBlock = null;
                        break;
                    case Account account:
                        account.Delegate = null;
                        account.FirstBlock = null;
                        break;
                    case Block b:
                        b.Activations = null;
                        b.Proposer = null;
                        b.Ballots = null;
                        b.CreatedAccounts = null;
                        b.Delegations = null;
                        b.DoubleBakings = null;
                        b.DoubleEndorsings = null;
                        b.DoublePreendorsings = null;
                        b.Endorsements = null;
                        b.Preendorsements = null;
                        b.Originations = null;
                        b.Proposals = null;
                        b.Protocol = null;
                        b.Reveals = null;
                        b.RegisterConstants = null;
                        b.SetDepositsLimits = null;
                        b.Revelation = null;
                        b.Revelations = null;
                        b.Transactions = null;
                        b.Migrations = null;
                        b.RevelationPenalties = null;
                        b.Software = null;
                        b.TxRollupOriginationOps = null;
                        b.TxRollupSubmitBatchOps = null;
                        b.TxRollupCommitOps = null;
                        b.TxRollupFinalizeCommitmentOps = null;
                        b.TxRollupRemoveCommitmentOps = null;
                        b.TxRollupReturnBondOps = null;
                        b.TxRollupRejectionOps = null;
                        b.TxRollupDispatchTicketsOps = null;
                        b.TransferTicketOps = null;
                        b.IncreasePaidStorageOps = null;
                        b.VdfRevelationOps = null;
                        break;
                }
            }
        }
    }
}
