using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
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
        public abstract ISerializer Serializer { get; }
        public abstract IValidator Validator { get; }

        public readonly TezosNode Node;
        public readonly TzktContext Db;
        public readonly CacheService Cache;
        public readonly ILogger Logger;

        public ProtocolHandler(TezosNode node, TzktContext db, CacheService cache, ILogger logger)
        {
            Node = node;
            Db = db;
            Cache = cache;
            Logger = logger;
        }

        public virtual async Task<AppState> ApplyBlock(Stream stream)
        {
            Logger.LogDebug("Deserializing block...");
            var rawBlock = await Serializer.DeserializeBlock(stream);

            Logger.LogDebug("Validating block...");
            rawBlock = await Validator.ValidateBlock(rawBlock);

            Logger.LogDebug("Init commits...");
            var commits = await GetCommits(rawBlock);

            Logger.LogDebug("Applying commits...");
            foreach (var commit in commits)
                await commit.Apply();

            Logger.LogDebug("Saving...");
            await Db.SaveChangesAsync();

            ClearCachedRelations();

            return await Cache.GetAppStateAsync();
        }
        
        public virtual async Task<AppState> RevertLastBlock()
        {
            Logger.LogDebug("Init reverts...");
            var commits = await GetReverts();

            Logger.LogDebug("Applying reverts...");
            foreach (var commit in commits)
                await commit.Revert();

            Logger.LogDebug("Saving...");
            await Db.SaveChangesAsync();

            ClearCachedRelations();

            return await Cache.GetAppStateAsync();
        }

        public abstract Task<List<ICommit>> GetCommits(IBlock block);

        public abstract Task<List<ICommit>> GetReverts();

        void ClearCachedRelations()
        {
            foreach (var entry in Db.ChangeTracker.Entries())
            {
                if (entry.Entity is Delegate delegat)
                {
                    delegat.Activation = null;
                    delegat.ActivationBlock = null;
                    delegat.BakedBlocks = null;
                    delegat.Ballots = null;
                    delegat.DeactivationBlock = null;
                    delegat.Delegate = null;
                    delegat.DelegatedAccounts = null;
                    delegat.DelegatedOriginations = null;
                    delegat.Endorsements = null;
                    delegat.ManagedContracts = null;
                    delegat.ManagedOriginations = null;
                    delegat.OriginatedContracts = null;
                    delegat.Proposals = null;
                    delegat.PushedProposals = null;
                    delegat.ReceivedDelegations = null;
                    delegat.ReceivedDoubleBakingAccusations = null;
                    delegat.ReceivedDoubleEndorsingAccusations = null;
                    delegat.ReceivedTransactions = null;
                    delegat.Reveal = null;
                    delegat.Revelations = null;
                    delegat.SentDelegations = null;
                    delegat.SentDoubleBakingAccusations = null;
                    delegat.SentDoubleEndorsingAccusations = null;
                    delegat.SentOriginations = null;
                    delegat.SentTransactions = null;
                }
                else if (entry.Entity is User user)
                {
                    user.Activation = null;
                    user.Delegate = null;
                    user.ManagedContracts = null;
                    user.ManagedOriginations = null;
                    user.OriginatedContracts = null;
                    user.ReceivedTransactions = null;
                    user.Reveal = null;
                    user.SentDelegations = null;
                    user.SentOriginations = null;
                    user.SentTransactions = null;
                }
                else if (entry.Entity is Contract contract)
                {
                    contract.Delegate = null;
                    contract.Manager = null;
                    contract.OriginatedContracts = null;
                    contract.Origination = null;
                    contract.Originator = null;
                    contract.ReceivedTransactions = null;
                    contract.Reveal = null;
                    contract.SentDelegations = null;
                    contract.SentOriginations = null;
                    contract.SentTransactions = null;
                }
                else if (entry.Entity is Block b)
                {
                    b.ActivatedDelegates = null;
                    b.Activations = null;
                    b.Baker = null;
                    b.Ballots = null;
                    b.DeactivatedDelegates = null;
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
                }
                else if (entry.Entity is Protocol p)
                {
                    p.Blocks = null;
                }
            }
        }
    }
}
