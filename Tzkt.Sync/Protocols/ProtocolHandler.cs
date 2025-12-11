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
        public abstract IDiagnostics Diagnostics { get; }
        public abstract IHelpers Helpers { get; }
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
        public readonly InboxContext Inbox;
        public BlockContext Context { get; private set; }

        bool _ForceDiagnostics = false;

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
            Inbox = new();
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
                Logger.LogDebug("Warm up cache");
                using (Metrics.Measure.Timer.Time(MetricsRegistry.CacheWarmUpTime))
                {
                    await WarmUpCache(block);
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

                var nextProtocol = this;
                if (state.Protocol != state.NextProtocol)
                    nextProtocol = Services.GetProtocolHandler(state.Level + 1, state.NextProtocol).WithContext(Context);

                Logger.LogDebug("Save changes");
                using (Metrics.Measure.Timer.Time(MetricsRegistry.SaveChangesTime))
                {
                    if (Config.Diagnostics || _ForceDiagnostics)
                        nextProtocol.Diagnostics.TrackChanges();
                    Context.Apply(Db);
                    await Db.SaveChangesAsync();
                }

                Logger.LogDebug("Save post-changes");
                using (Metrics.Measure.Timer.Time(MetricsRegistry.PostProcessingTime))
                {
                    await AfterCommit(block);
                    if (Config.Diagnostics || _ForceDiagnostics)
                        nextProtocol.Diagnostics.TrackChanges();
                    await Db.SaveChangesAsync();
                }

                Logger.LogDebug("Process quotes");
                using (Metrics.Measure.Timer.Time(MetricsRegistry.QuotesProcessingTime))
                {
                    await Quotes.Commit();
                }

                if (state.Protocol != state.NextProtocol)
                {
                    Logger.LogDebug("Activate protocol {hash}", state.NextProtocol);
                    await nextProtocol.Activate(state, block);
                    if (Config.Diagnostics || _ForceDiagnostics)
                        nextProtocol.Diagnostics.TrackChanges();
                    await Db.SaveChangesAsync();
                }

                if (Config.Diagnostics || _ForceDiagnostics)
                {
                    Logger.LogDebug("Diagnostics");
                    using (Metrics.Measure.Timer.Time(MetricsRegistry.DiagnosticsTime))
                    {
                        await nextProtocol.Diagnostics.Run(block);
                    }
                    _ForceDiagnostics = false;
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
        
        public virtual async Task<AppState> RevertLastBlock(string predecessor)
        {
            Logger.LogDebug("Begin DB transaction");
            using var tx = await Db.Database.BeginTransactionAsync();
            try
            {
                var state = Cache.AppState.Get();
                Db.TryAttach(state);

                Logger.LogDebug("Init block context");
                await InitContext(state);
                Db.TryAttach(Context.Proposer);

                var nextProtocol = this;
                if (state.Protocol != state.NextProtocol)
                {
                    nextProtocol = Services.GetProtocolHandler(state.Level + 1, state.NextProtocol);

                    Logger.LogDebug("Deactivate protocol {hash}", state.NextProtocol);
                    await nextProtocol.Deactivate(state);

                    nextProtocol.Diagnostics.TrackChanges();
                    await Db.SaveChangesAsync();
                }

                Logger.LogDebug("Revert quotes");
                using (Metrics.Measure.Timer.Time(MetricsRegistry.RevertQuotesProcessingTime))
                {
                    await Quotes.Revert();
                }

                Logger.LogDebug("Revert post-changes");
                using (Metrics.Measure.Timer.Time(MetricsRegistry.RevertPostProcessingTime))
                {
                    await BeforeRevert();

                    nextProtocol.Diagnostics.TrackChanges();
                    await Db.SaveChangesAsync();
                }

                Logger.LogDebug("Revert block");
                using (Metrics.Measure.Timer.Time(MetricsRegistry.RevertProcessingTime))
                {
                    await Revert();
                }

                Logger.LogDebug("Touch accounts");
                ClearAccounts(state.Level + 1);

                Logger.LogDebug("Save changes");
                using (Metrics.Measure.Timer.Time(MetricsRegistry.RevertSaveChangesTime))
                {
                    nextProtocol.Diagnostics.TrackChanges();
                    await Context.Revert(Db);
                    await Db.SaveChangesAsync();
                }

                if ((Config.Diagnostics || _ForceDiagnostics) && state.Hash == predecessor)
                {
                    Logger.LogDebug("Diagnostics");
                    using (Metrics.Measure.Timer.Time(MetricsRegistry.RevertDiagnosticsTime))
                    {
                        await Diagnostics.Run(state.Level);
                    }
                    _ForceDiagnostics = false;
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

        public virtual async Task WarmUpCache(JsonElement block)
        {
            var accounts = new HashSet<string>(64);
            var contracts = new HashSet<string>(64);
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
                        if (content.OptionalString("destination") is string dest)
                        {
                            accounts.Add(dest);
                            if (dest[0] == 'K')
                                contracts.Add(dest);
                        }

                        if (content.Required("metadata").TryGetProperty("internal_operation_results", out var internalResults))
                            foreach (var internalContent in internalResults.RequiredArray().EnumerateArray())
                            {
                                accounts.Add(internalContent.RequiredString("source"));
                                if (internalContent.RequiredString("kind") == "transaction")
                                {
                                    if (internalContent.OptionalString("destination") is string internalDest)
                                    {
                                        accounts.Add(internalDest);
                                        if (internalDest[0] == 'K')
                                            contracts.Add(internalDest);
                                    }
                                }
                            }
                    }
                }
            }

            if (accounts.Count != 0)
            {
                await Cache.Accounts.LoadAsync(accounts);
            }
            if (contracts.Count != 0)
            {
                var contractIds = new List<int>();
                foreach (var contract in contracts)
                    if (Cache.Accounts.TryGetCached(contract, out var _contract))
                        contractIds.Add(_contract.Id);

                if (contractIds.Count != 0)
                {
                    await Cache.Storages.PreloadAsync(contractIds);
                    await Cache.Schemas.PreloadAsync(contractIds);
                }
            }
        }

        public virtual Task Activate(AppState state, JsonElement block) => Task.CompletedTask;

        public virtual Task Deactivate(AppState state) => Task.CompletedTask;

        public virtual Task AfterCommit(JsonElement block) => Task.CompletedTask;

        public virtual Task BeforeRevert() => Task.CompletedTask;

        public abstract Task Commit(JsonElement block);

        public abstract Task Revert();

        public void ForceDiagnostics() => _ForceDiagnostics = true;

        async Task InitContext(AppState state)
        {
            var currBlock = Cache.Blocks.Get(state.Level);
            Context.Block = currBlock;
            Context.Proposer = Cache.Accounts.GetDelegate(currBlock.ProposerId!.Value);
            Context.Protocol = await Cache.Protocols.GetAsync(currBlock.ProtoCode);

            if (currBlock.Operations.HasFlag(Operations.Attestations))
                Context.AttestationOps = await Db.AttestationOps.AsNoTracking().Where(x => x.Level == currBlock.Level).ToListAsync();

            if (currBlock.Operations.HasFlag(Operations.Preattestations))
                Context.PreattestationOps = await Db.PreattestationOps.AsNoTracking().Where(x => x.Level == currBlock.Level).ToListAsync();

            if (currBlock.Operations.HasFlag(Operations.Proposals))
                Context.ProposalOps = await Db.ProposalOps.AsNoTracking().Where(x => x.Level == currBlock.Level).ToListAsync();

            if (currBlock.Operations.HasFlag(Operations.Ballots))
                Context.BallotOps = await Db.BallotOps.AsNoTracking().Where(x => x.Level == currBlock.Level).ToListAsync();

            if (currBlock.Operations.HasFlag(Operations.Activations))
                Context.ActivationOps = await Db.ActivationOps.AsNoTracking().Where(x => x.Level == currBlock.Level).ToListAsync();

            if (currBlock.Operations.HasFlag(Operations.DalEntrapmentEvidence))
                Context.DalEntrapmentEvidenceOps = await Db.DalEntrapmentEvidenceOps.AsNoTracking().Where(x => x.Level == currBlock.Level).ToListAsync();

            if (currBlock.Operations.HasFlag(Operations.DoubleBakings))
                Context.DoubleBakingOps = await Db.DoubleBakingOps.AsNoTracking().Where(x => x.Level == currBlock.Level).ToListAsync();

            if (currBlock.Operations.HasFlag(Operations.DoubleConsensus))
                Context.DoubleConsensusOps = await Db.DoubleConsensusOps.AsNoTracking().Where(x => x.Level == currBlock.Level).ToListAsync();

            if (currBlock.Operations.HasFlag(Operations.Revelations))
                Context.NonceRevelationOps = await Db.NonceRevelationOps.AsNoTracking().Where(x => x.Level == currBlock.Level).ToListAsync();

            if (currBlock.Operations.HasFlag(Operations.VdfRevelation))
                Context.VdfRevelationOps = await Db.VdfRevelationOps.AsNoTracking().Where(x => x.Level == currBlock.Level).ToListAsync();

            if (currBlock.Operations.HasFlag(Operations.DrainDelegate))
                Context.DrainDelegateOps = await Db.DrainDelegateOps.AsNoTracking().Where(x => x.Level == currBlock.Level).ToListAsync();

            if (currBlock.Operations.HasFlag(Operations.Delegations))
                Context.DelegationOps = await Db.DelegationOps.AsNoTracking().Where(x => x.Level == currBlock.Level).ToListAsync();

            if (currBlock.Operations.HasFlag(Operations.Originations))
                Context.OriginationOps = await Db.OriginationOps.AsNoTracking().Where(x => x.Level == currBlock.Level).ToListAsync();

            if (currBlock.Operations.HasFlag(Operations.Transactions))
                Context.TransactionOps = await Db.TransactionOps.AsNoTracking().Where(x => x.Level == currBlock.Level).ToListAsync();

            if (currBlock.Operations.HasFlag(Operations.Reveals))
                Context.RevealOps = await Db.RevealOps.AsNoTracking().Where(x => x.Level == currBlock.Level).ToListAsync();

            if (currBlock.Operations.HasFlag(Operations.RegisterConstant))
                Context.RegisterConstantOps = await Db.RegisterConstantOps.AsNoTracking().Where(x => x.Level == currBlock.Level).ToListAsync();

            if (currBlock.Operations.HasFlag(Operations.SetDepositsLimits))
                Context.SetDepositsLimitOps = await Db.SetDepositsLimitOps.AsNoTracking().Where(x => x.Level == currBlock.Level).ToListAsync();

            if (currBlock.Operations.HasFlag(Operations.IncreasePaidStorage))
                Context.IncreasePaidStorageOps = await Db.IncreasePaidStorageOps.AsNoTracking().Where(x => x.Level == currBlock.Level).ToListAsync();

            if (currBlock.Operations.HasFlag(Operations.UpdateSecondaryKey))
                Context.UpdateSecondaryKeyOps = await Db.UpdateSecondaryKeyOps.AsNoTracking().Where(x => x.Level == currBlock.Level).ToListAsync();

            if (currBlock.Operations.HasFlag(Operations.TransferTicket))
                Context.TransferTicketOps = await Db.TransferTicketOps.AsNoTracking().Where(x => x.Level == currBlock.Level).ToListAsync();

            if (currBlock.Operations.HasFlag(Operations.SetDelegateParameters))
                Context.SetDelegateParametersOps = await Db.SetDelegateParametersOps.AsNoTracking().Where(x => x.Level == currBlock.Level).ToListAsync();

            if (currBlock.Operations.HasFlag(Operations.DalPublishCommitment))
                Context.DalPublishCommitmentOps = await Db.DalPublishCommitmentOps.AsNoTracking().Where(x => x.Level == currBlock.Level).ToListAsync();

            if (currBlock.Operations.HasFlag(Operations.Staking))
                Context.StakingOps = await Db.StakingOps.AsNoTracking().Where(x => x.Level == currBlock.Level).ToListAsync();

            if (currBlock.Operations.HasFlag(Operations.TxRollupOrigination))
                Context.TxRollupOriginationOps = await Db.TxRollupOriginationOps.AsNoTracking().Where(x => x.Level == currBlock.Level).ToListAsync();

            if (currBlock.Operations.HasFlag(Operations.TxRollupSubmitBatch))
                Context.TxRollupSubmitBatchOps = await Db.TxRollupSubmitBatchOps.AsNoTracking().Where(x => x.Level == currBlock.Level).ToListAsync();

            if (currBlock.Operations.HasFlag(Operations.TxRollupCommit))
                Context.TxRollupCommitOps = await Db.TxRollupCommitOps.AsNoTracking().Where(x => x.Level == currBlock.Level).ToListAsync();

            if (currBlock.Operations.HasFlag(Operations.TxRollupFinalizeCommitment))
                Context.TxRollupFinalizeCommitmentOps = await Db.TxRollupFinalizeCommitmentOps.AsNoTracking().Where(x => x.Level == currBlock.Level).ToListAsync();

            if (currBlock.Operations.HasFlag(Operations.TxRollupRemoveCommitment))
                Context.TxRollupRemoveCommitmentOps = await Db.TxRollupRemoveCommitmentOps.AsNoTracking().Where(x => x.Level == currBlock.Level).ToListAsync();

            if (currBlock.Operations.HasFlag(Operations.TxRollupReturnBond))
                Context.TxRollupReturnBondOps = await Db.TxRollupReturnBondOps.AsNoTracking().Where(x => x.Level == currBlock.Level).ToListAsync();

            if (currBlock.Operations.HasFlag(Operations.TxRollupRejection))
                Context.TxRollupRejectionOps = await Db.TxRollupRejectionOps.AsNoTracking().Where(x => x.Level == currBlock.Level).ToListAsync();

            if (currBlock.Operations.HasFlag(Operations.TxRollupDispatchTickets))
                Context.TxRollupDispatchTicketsOps = await Db.TxRollupDispatchTicketsOps.AsNoTracking().Where(x => x.Level == currBlock.Level).ToListAsync();

            if (currBlock.Operations.HasFlag(Operations.SmartRollupAddMessages))
                Context.SmartRollupAddMessagesOps = await Db.SmartRollupAddMessagesOps.AsNoTracking().Where(x => x.Level == currBlock.Level).ToListAsync();

            if (currBlock.Operations.HasFlag(Operations.SmartRollupCement))
                Context.SmartRollupCementOps = await Db.SmartRollupCementOps.AsNoTracking().Where(x => x.Level == currBlock.Level).ToListAsync();

            if (currBlock.Operations.HasFlag(Operations.SmartRollupExecute))
                Context.SmartRollupExecuteOps = await Db.SmartRollupExecuteOps.AsNoTracking().Where(x => x.Level == currBlock.Level).ToListAsync();

            if (currBlock.Operations.HasFlag(Operations.SmartRollupOriginate))
                Context.SmartRollupOriginateOps = await Db.SmartRollupOriginateOps.AsNoTracking().Where(x => x.Level == currBlock.Level).ToListAsync();

            if (currBlock.Operations.HasFlag(Operations.SmartRollupPublish))
                Context.SmartRollupPublishOps = await Db.SmartRollupPublishOps.AsNoTracking().Where(x => x.Level == currBlock.Level).ToListAsync();

            if (currBlock.Operations.HasFlag(Operations.SmartRollupRecoverBond))
                Context.SmartRollupRecoverBondOps = await Db.SmartRollupRecoverBondOps.AsNoTracking().Where(x => x.Level == currBlock.Level).ToListAsync();

            if (currBlock.Operations.HasFlag(Operations.SmartRollupRefute))
                Context.SmartRollupRefuteOps = await Db.SmartRollupRefuteOps.AsNoTracking().Where(x => x.Level == currBlock.Level).ToListAsync();

            if (currBlock.Operations.HasFlag(Operations.Migrations))
                Context.MigrationOps = await Db.MigrationOps.AsNoTracking().Where(x => x.Level == currBlock.Level).ToListAsync();

            if (currBlock.Operations.HasFlag(Operations.RevelationPenalty))
                Context.RevelationPenaltyOps = await Db.RevelationPenaltyOps.AsNoTracking().Where(x => x.Level == currBlock.Level).ToListAsync();

            if (currBlock.Operations.HasFlag(Operations.AttestationRewards))
                Context.AttestationRewardOps = await Db.AttestationRewardOps.AsNoTracking().Where(x => x.Level == currBlock.Level).ToListAsync();

            if (currBlock.Operations.HasFlag(Operations.DalAttestationReward))
                Context.DalAttestationRewardOps = await Db.DalAttestationRewardOps.AsNoTracking().Where(x => x.Level == currBlock.Level).ToListAsync();

            if (currBlock.Operations.HasFlag(Operations.Autostaking))
                Context.AutostakingOps = await Db.AutostakingOps.AsNoTracking().Where(x => x.Level == currBlock.Level).ToListAsync();

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
