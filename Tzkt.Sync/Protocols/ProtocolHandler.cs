using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        public abstract string Protocol { get; }
        public abstract IDiagnostics Diagnostics { get; }
        public abstract ISerializer Serializer { get; }
        public abstract IValidator Validator { get; }

        public readonly TezosNode Node;
        public readonly TzktContext Db;
        public readonly CacheService Cache;
        public readonly QuotesService Quotes;
        public readonly TezosProtocolsConfig Config;
        public readonly ILogger Logger;
        
        public ProtocolHandler(TezosNode node, TzktContext db, CacheService cache, QuotesService quotes, IConfiguration config, ILogger logger)
        {
            Node = node;
            Db = db;
            Cache = cache;
            Quotes = quotes;
            Config = config.GetTezosProtocolsConfig();
            Logger = logger;
        }

        public virtual async Task<AppState> CommitBlock(Stream stream, int head, DateTime sync)
        {
            using var tx = await Db.Database.BeginTransactionAsync();
            try
            {
                Logger.LogDebug("Deserializing block...");
                var rawBlock = await Serializer.DeserializeBlock(stream);
                
                Logger.LogDebug("Loading entities...");
                await LoadEntities(rawBlock);

                Logger.LogDebug("Loading constants...");
                await InitProtocol(rawBlock);

                if (Config.Validation)
                {
                    Logger.LogDebug("Validating block...");
                    rawBlock = await Validator.ValidateBlock(rawBlock);
                }

                Logger.LogDebug("Committing block...");
                await Commit(rawBlock);

                var state = Cache.AppState.Get();
                var protocolEnd = false;
                if (state.Protocol != state.NextProtocol)
                {
                    protocolEnd = true;
                    Logger.LogDebug("Migrating context...");
                    await Migration();
                }

                Logger.LogDebug("Touch accounts...");
                TouchAccounts(rawBlock.Level);

                if (Config.Diagnostics)
                {
                    Logger.LogDebug("Diagnostics...");
                    if (!protocolEnd)
                        await Diagnostics.Run(rawBlock.Level, rawBlock.OperationsCount);
                    else
                        await FindDiagnostics(state.NextProtocol).Run(rawBlock.Level, rawBlock.OperationsCount);
                }

                state.KnownHead = head;
                state.LastSync = sync;
 
                Logger.LogDebug("Saving...");
                await Db.SaveChangesAsync();

                await AfterCommit(rawBlock);

                await Quotes.Commit();

                await tx.CommitAsync();
            }
            catch (Exception)
            {
                await tx.RollbackAsync();
                throw;
            }

            ClearCachedRelations();

            return Cache.AppState.Get();
        }
        
        public virtual async Task<AppState> RevertLastBlock(string predecessor)
        {
            using var tx = await Db.Database.BeginTransactionAsync();
            try
            {
                await Quotes.Revert();

                await BeforeRevert();

                var state = Cache.AppState.Get();
                if (state.Protocol != state.NextProtocol)
                {
                    Logger.LogDebug("Migrating context...");
                    await CancelMigration();
                }

                Logger.LogDebug("Loading protocol...");
                await InitProtocol();

                Logger.LogDebug("Reverting...");
                await Revert();

                Logger.LogDebug("Clear accounts...");
                ClearAccounts(state.Level + 1);

                if (Config.Diagnostics && state.Hash == predecessor)
                {
                    Logger.LogDebug("Diagnostics...");
                    await Diagnostics.Run(state.Level);
                }

                Logger.LogDebug("Saving...");
                await Db.SaveChangesAsync();
                await tx.CommitAsync();
            }
            catch (Exception)
            {
                await tx.RollbackAsync();
                throw;
            }

            ClearCachedRelations();

            return Cache.AppState.Get();
        }

        public virtual void TouchAccounts(int level)
        {
            var state = Cache.AppState.Get();
            var block = Db.ChangeTracker.Entries()
                .First(x => x.Entity is Block block && block.Level == level).Entity as Block;

            foreach (var entry in Db.ChangeTracker.Entries().Where(x => x.Entity is Account))
            {
                var account = entry.Entity as Account;

                if (entry.State == EntityState.Modified)
                {
                    account.LastLevel = level;
                }
                else if (entry.State == EntityState.Added)
                {
                    state.AccountsCount++;
                    account.FirstLevel = level;
                    account.LastLevel = level;
                    block.Events |= BlockEvents.NewAccounts;
                }
            }
        }

        public virtual void ClearAccounts(int level)
        {
            var state = Cache.AppState.Get();

            foreach (var entry in Db.ChangeTracker.Entries().Where(x => x.Entity is Account))
            {
                var account = entry.Entity as Account;

                if (entry.State == EntityState.Modified)
                    account.LastLevel = level;
                
                if (account.FirstLevel == level)
                {
                    Db.Remove(account);
                    Cache.Accounts.Remove(account);
                    state.AccountsCount--;
                }
            }
        }

        public virtual Task LoadEntities(IBlock block) => Task.CompletedTask;

        public virtual Task Migration() => Task.CompletedTask;

        public virtual Task CancelMigration() => Task.CompletedTask;

        public abstract Task InitProtocol();

        public abstract Task InitProtocol(IBlock block);

        public abstract Task Commit(IBlock block);

        public virtual Task AfterCommit(IBlock block) => Task.CompletedTask;

        public virtual Task BeforeRevert() => Task.CompletedTask;

        public abstract Task Revert();

        IDiagnostics FindDiagnostics(string hash)
        {
            return hash switch
            {
                "PrihK96nBAFSxVL1GLJTVhu9YnzkMFiBeuJRPA8NwuZVZCE1L6i" => new Protocols.Genesis.Diagnostics(),
                "Ps9mPmXaRzmzk35gbAYNCAw6UXdE2qoABTHbN2oEEc1qM7CwT9P" => new Protocols.Initiator.Diagnostics(),
                "PtBMwNZT94N7gXKw4i273CKcSaBrrBnqnt3RATExNKr9KNX2USV" => new Protocols.Initiator.Diagnostics(),
                "PtYuensgYBb3G3x1hLLbCmcav8ue8Kyd2khADcL5LsT5R1hcXex" => new Protocols.Initiator.Diagnostics(),
                "PtCJ7pwoxe8JasnHY8YonnLYjcVHmhiARPJvqcC6VfHT5s8k8sY" => new Protocols.Proto1.Diagnostics(Db, Node),
                "PsYLVpVvgbLhAhoqAkMFUo6gudkJ9weNXhUYCiLDzcUpFpkk8Wt" => new Protocols.Proto2.Diagnostics(Db, Node),
                "PsddFKi32cMJ2qPjf43Qv5GDWLDPZb3T3bF6fLKiF5HtvHNU7aP" => new Protocols.Proto3.Diagnostics(Db, Node),
                "Pt24m4xiPbLDhVgVfABUjirbmda3yohdN82Sp9FeuAXJ4eV9otd" => new Protocols.Proto4.Diagnostics(Db, Node),
                "PsBabyM1eUXZseaJdmXFApDSBqj8YBfwELoxZHHW77EMcAbbwAS" => new Protocols.Proto5.Diagnostics(Db, Node),
                "PsBABY5HQTSkA4297zNHfsZNKtxULfL18y95qb3m53QJiXGmrbU" => new Protocols.Proto5.Diagnostics(Db, Node),
                "PsCARTHAGazKbHtnKfLzQg3kms52kSRpgnDY982a9oYsSXRLQEb" => new Protocols.Proto6.Diagnostics(Db, Node),
                _ => throw new NotImplementedException($"Diagnostics for the protocol {hash} hasn't been implemented yet")
            };
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
                    case Block b:
                        b.Activations = null;
                        b.Baker = null;
                        b.Ballots = null;
                        b.CreatedAccounts = null;
                        b.Delegations = null;
                        b.DoubleBakings = null;
                        b.DoubleEndorsings = null;
                        b.Endorsements = null;
                        b.Originations = null;
                        b.Proposals = null;
                        b.Protocol = null;
                        b.Reveals = null;
                        b.Revelation = null;
                        b.Revelations = null;
                        b.Transactions = null;
                        b.Migrations = null;
                        b.RevelationPenalties = null;
                        b.Software = null;
                        break;
                    case VotingPeriod period:
                        period.Epoch = null;
                        if (period is ExplorationPeriod exploration)
                            exploration.Proposal = null;
                        else if (period is PromotionPeriod promotion)
                            promotion.Proposal = null;
                        else if (period is TestingPeriod testing)
                            testing.Proposal = null;
                        break;
                    case Proposal proposal:
                        proposal.ExplorationPeriod = null;
                        proposal.Initiator = null;
                        proposal.PromotionPeriod = null;
                        proposal.ProposalPeriod = null;
                        proposal.TestingPeriod = null;
                        break;
                }
            }
        }
    }
}
