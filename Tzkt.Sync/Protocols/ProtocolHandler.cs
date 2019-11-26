using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

using Tzkt.Data;
using Tzkt.Data.Models;
using Tzkt.Sync.Protocols;
using Tzkt.Sync.Services;

namespace Tzkt.Sync
{
    public abstract class ProtocolHandler
    {
        public abstract string Protocol { get; }
        public abstract ISerializer Serializer { get; }
        public abstract IValidator Validator { get; }

        public readonly TezosNode Node;
        public readonly TzktContext Db;
        public readonly CacheService Cache;
        public readonly ILogger Logger;
        
        readonly DiagnosticService Diagnostics;

        public ProtocolHandler(TezosNode node, TzktContext db, CacheService cache, DiagnosticService diagnostics, ILogger logger)
        {
            Node = node;
            Db = db;
            Cache = cache;
            Diagnostics = diagnostics;
            Logger = logger;
        }

        public virtual async Task<AppState> ApplyBlock(Stream stream)
        {
            Logger.LogDebug("Deserializing block...");
            var rawBlock = await Serializer.DeserializeBlock(stream);

            Logger.LogDebug("Loading entities...");
            await LoadEntities(rawBlock);

            Logger.LogDebug("Loading constants...");
            await InitProtocol(rawBlock);

            Logger.LogDebug("Validating block...");
            rawBlock = await Validator.ValidateBlock(rawBlock);

            Logger.LogDebug("Committing block...");
            await Commit(rawBlock);

            var state = await Cache.GetAppStateAsync();
            if (state.Protocol != state.NextProtocol)
            {
                Logger.LogDebug("Migrating context...");
                await Migration();
            }

            Logger.LogDebug("Touch accounts...");
            TouchAccounts(rawBlock.Level);

            Logger.LogDebug("Diagnostics...");
            await Diagnostics.Run(rawBlock.Level, rawBlock.OperationsCount);

            Logger.LogDebug("Saving...");
            await Db.SaveChangesAsync();

            ClearCachedRelations();

            return await Cache.GetAppStateAsync();
        }
        
        public virtual async Task<AppState> RevertLastBlock()
        {
            var state = await Cache.GetAppStateAsync();
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

            Logger.LogDebug("Diagnostics...");
            await Diagnostics.Run((await Cache.GetAppStateAsync()).Level);

            Logger.LogDebug("Saving...");
            await Db.SaveChangesAsync();

            ClearCachedRelations();
            
            return await Cache.GetAppStateAsync();
        }

        public virtual void TouchAccounts(int level)
        {
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
                    account.FirstLevel = level;
                    account.LastLevel = level;
                    block.Events |= BlockEvents.NewAccounts;
                }
            }
        }

        public virtual void ClearAccounts(int level)
        {
            foreach (var entry in Db.ChangeTracker.Entries().Where(x => x.Entity is Account))
            {
                var account = entry.Entity as Account;

                if (entry.State == EntityState.Modified)
                    account.LastLevel = level;
                
                if (account.FirstLevel == level)
                {
                    Db.Remove(account);
                    Cache.RemoveAccount(account);
                }
            }
        }

        public virtual Task LoadEntities(IBlock block) => Task.CompletedTask;

        public virtual Task Migration() => Task.CompletedTask;

        public virtual Task CancelMigration() => Task.CompletedTask;

        public abstract Task InitProtocol();

        public abstract Task InitProtocol(IBlock block);

        public abstract Task Commit(IBlock block);

        public abstract Task Revert();

        void ClearCachedRelations()
        {
            foreach (var entry in Db.ChangeTracker.Entries())
            {
                switch(entry.Entity)
                {
                    case Delegate delegat:
                        delegat.Delegate = null;
                        delegat.DelegatedAccounts = null;
                        delegat.FirstBlock = null;
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
