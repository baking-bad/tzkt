using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Tzkt.Data.Models;
using Tzkt.Sync.Services;

namespace Tzkt.Sync.Protocols.Proto15
{
    class Validator : IValidator
    {
        readonly CacheService Cache;
        Protocol Protocol;
        string Proposer;
        string Producer;
        int Level;
        int Cycle;

        public Validator(ProtocolHandler protocol) => Cache = protocol.Cache;

        public virtual async Task ValidateBlock(JsonElement block)
        {
            Protocol = await Cache.Protocols.GetAsync(Cache.AppState.GetNextProtocol());

            if (block.RequiredString("chain_id") != Cache.AppState.GetChainId())
                throw new ValidationException("invalid chain");

            if (block.RequiredString("protocol") != Cache.AppState.GetNextProtocol())
                throw new ValidationException("invalid block protocol", true);

            ValidateBlockHeader(block.Required("header"));
            await ValidateBlockMetadata(block.Required("metadata"));
            await ValidateOperations(block.RequiredArray("operations", 4));
        }

        void ValidateBlockHeader(JsonElement header)
        {
            Level = header.RequiredInt32("level");
            if (Level != Cache.AppState.GetNextLevel())
                throw new ValidationException($"invalid block level", true);

            if (header.RequiredString("predecessor") != Cache.AppState.GetHead())
                throw new ValidationException($"invalid block predecessor", true);
        }

        async Task ValidateBlockMetadata(JsonElement metadata)
        {
            #region baking
            Proposer = metadata.RequiredString("proposer");
            if (!Cache.Accounts.DelegateExists(Proposer))
                throw new ValidationException($"non-existent block proposer");

            Producer = metadata.RequiredString("baker");
            if (!Cache.Accounts.DelegateExists(Producer))
                throw new ValidationException($"non-existent block baker");
            #endregion

            #region level info
            Cycle = metadata.Required("level_info").RequiredInt32("cycle");
            if (Cycle != Protocol.GetCycle(Level))
                throw new ValidationException($"invalid block cycle", true);
            #endregion

            #region voting info
            var periodInfo = metadata.Required("voting_period_info").Required("voting_period");
            var periodIndex = periodInfo.RequiredInt32("index");
            var periodKind = periodInfo.RequiredString("kind") switch
            {
                "proposal" => PeriodKind.Proposal,
                "exploration" => PeriodKind.Exploration,
                "cooldown" => PeriodKind.Testing,
                "promotion" => PeriodKind.Promotion,
                "adoption" => PeriodKind.Adoption,
                _ => throw new ValidationException("invalid voting period kind")
            };

            var period = await Cache.Periods.GetAsync(Cache.AppState.Get().VotingPeriod);
            if (Level > period.FirstLevel && Level < period.LastLevel)
            {
                if (periodIndex != period.Index)
                    throw new ValidationException("invalid voting period index");

                if (!Protocol.HasDictator && periodKind != period.Kind)
                    throw new ValidationException("unexpected voting period");
            }
            #endregion

            #region deactivation
            foreach (var baker in metadata.RequiredArray("deactivated").EnumerateArray())
                if (!Cache.Accounts.DelegateExists(baker.GetString()))
                    throw new ValidationException($"non-existent deactivated baker {baker}");
            #endregion

            #region balance updates
            var balanceUpdates = metadata.RequiredArray("balance_updates").EnumerateArray();
            if (balanceUpdates.Any(x => x.RequiredString("kind") == "contract" && x.RequiredString("origin") == "block" && !Cache.Accounts.DelegateExists(x.RequiredString("contract"))))
                throw new ValidationException("non-existent delegate in block balance updates");

            if (Cycle < Protocol.NoRewardCycles)
            {
                if (balanceUpdates.Any(x => x.RequiredString("kind") == "minted" && x.RequiredString("category") == "baking rewards"))
                    throw new ValidationException("unexpected block reward");
                
                if (balanceUpdates.Any(x => x.RequiredString("kind") == "minted" && x.RequiredString("category") == "baking bonuses"))
                    throw new ValidationException("unexpected block bonus");
            }
            else
            {
                if (balanceUpdates.Count(x => x.RequiredString("kind") == "minted" && x.RequiredString("category") == "baking rewards") != 1)
                    throw new ValidationException("invalid block reward");
                
                if (balanceUpdates.Count(x => x.RequiredString("kind") == "minted" && x.RequiredString("category") == "baking bonuses") > 1)
                    throw new ValidationException("invalid block bonus");
                
                if (balanceUpdates.Count(x => x.RequiredString("origin") == "block") > 5 && !Protocol.IsCycleEnd(Level))
                    throw new ValidationException("unexpected cycle rewards");
            }
            #endregion

            #region implicit operations
            foreach (var op in metadata.RequiredArray("implicit_operations_results").EnumerateArray())
            {
                var kind = op.RequiredString("kind");
                if (kind == "transaction")
                {
                    var subsidy = op.RequiredArray("balance_updates", 2).EnumerateArray()
                        .Where(x => x.RequiredString("kind") == "contract");

                    if (subsidy.Count() > 1)
                        throw new ValidationException("invalid subsidy");

                    if (subsidy.Any(x => x.RequiredString("origin") != "subsidy"))
                        throw new ValidationException("invalid subsidy origin");

                    if (subsidy.Any(x => x.RequiredString("contract") != Proto10.ProtoActivator.CpmmContract))
                        throw new ValidationException("invalid subsidy recepient");
                }
                else if (kind == "origination" && Level == Protocol.FirstLevel)
                {
                    var contract = op.RequiredArray("originated_contracts", 1)[0].RequiredString();
                    if (!await Cache.Accounts.ExistsAsync(contract, AccountType.Contract))
                        throw new ValidationException("unexpected implicit origination");
                }
                else
                {
                    throw new ValidationException("unexpected implicit operation kind");
                }
            }
            #endregion
        }

        protected virtual async Task ValidateOperations(JsonElement operations)
        {
            foreach (var opg in operations.EnumerateArray())
            {
                foreach (var op in opg.RequiredArray().EnumerateArray())
                {
                    foreach (var content in op.RequiredArray("contents").EnumerateArray())
                    {
                        switch (content.RequiredString("kind"))
                        {
                            case "endorsement": ValidateEndorsement(content); break;
                            case "preendorsement": ValidatePreendorsement(content); break;
                            case "ballot": await ValidateBallot(content); break;
                            case "proposals": ValidateProposal(content); break;
                            case "activate_account": await ValidateActivation(content); break;
                            case "double_baking_evidence": ValidateDoubleBaking(content); break;
                            case "double_endorsement_evidence": ValidateDoubleBaking(content); break;
                            case "double_preendorsement_evidence": ValidateDoubleBaking(content); break;
                            case "seed_nonce_revelation": await ValidateSeedNonceRevelation(content); break;
                            case "vdf_revelation": ValidateVdfRevelation(content); break;
                            case "drain_delegate": ValidateDrainDelegate(content); break;
                            case "delegation": await ValidateDelegation(content); break;
                            case "origination": await ValidateOrigination(content); break;
                            case "transaction": await ValidateTransaction(content); break;
                            case "reveal": await ValidateReveal(content); break;
                            case "register_global_constant": await ValidateRegisterConstant(content); break;
                            case "set_deposits_limit": await ValidateSetDepositsLimit(content); break;
                            case "increase_paid_storage": await ValidateIncreasePaidStorage(content); break;
                            case "update_consensus_key": await ValidateUpdateConsensusKey(content); break;
                            case "tx_rollup_origination": await ValidateTxRollupOrigination(content); break;
                            case "tx_rollup_submit_batch": await ValidateTxRollupSubmitBatch(content); break; 
                            case "tx_rollup_commit": await ValidateTxRollupCommit(content); break; 
                            case "tx_rollup_finalize_commitment": await ValidateTxRollupFinalizeCommitment(content); break; 
                            case "tx_rollup_remove_commitment": await ValidateTxRollupRemoveCommitment(content); break; 
                            case "tx_rollup_return_bond": await ValidateTxRollupReturnBond(content); break; 
                            case "tx_rollup_rejection": await ValidateTxRollupRejection(content); break; 
                            case "tx_rollup_dispatch_tickets": await ValidateTxRollupDispatchTickets(content); break; 
                            case "transfer_ticket": await ValidateTransferTicket(content); break; 
                            default:
                                throw new ValidationException("invalid operation content kind");
                        }
                    }
                }
            }
        }

        protected virtual void ValidateEndorsement(JsonElement content)
        {
            if (content.RequiredInt32("level") != Cache.AppState.GetLevel())
                throw new ValidationException("invalid endorsement level");

            if (!Cache.Accounts.DelegateExists(content.Required("metadata").RequiredString("delegate")))
                throw new ValidationException("unknown endorsement delegate");
        }

        protected virtual void ValidatePreendorsement(JsonElement content)
        {
            if (content.RequiredInt32("level") != Cache.AppState.GetLevel() + 1)
                throw new ValidationException("invalid preendorsement level");

            if (!Cache.Accounts.DelegateExists(content.Required("metadata").RequiredString("delegate")))
                throw new ValidationException("unknown endorsement delegate");
        }

        protected virtual async Task ValidateBallot(JsonElement content)
        {
            var periodIndex = content.RequiredInt32("period");

            if (Cache.AppState.Get().VotingPeriod != periodIndex)
                throw new ValidationException("invalid ballot voting period");

            var proposal = await Cache.Proposals.GetOrDefaultAsync(Cache.AppState.Get().VotingEpoch, content.RequiredString("proposal"));
            if (proposal?.Status != ProposalStatus.Active)
                throw new ValidationException("invalid ballot proposal");

            if (!Cache.Accounts.DelegateExists(content.RequiredString("source")))
                throw new ValidationException("invalid ballot sender");
        }

        protected virtual void ValidateProposal(JsonElement content)
        {
            var periodIndex = content.RequiredInt32("period");

            if (Cache.AppState.Get().VotingPeriod != periodIndex)
                throw new ValidationException("invalid proposal voting period");

            var source = content.RequiredString("source");
            if (Protocol.Dictator != source && !Cache.Accounts.DelegateExists(source))
                throw new ValidationException("invalid proposal sender");
        }

        protected virtual async Task ValidateActivation(JsonElement content)
        {
            var account = content.RequiredString("pkh");

            if (await Cache.Accounts.ExistsAsync(account, AccountType.User) &&
                ((await Cache.Accounts.GetAsync(account)) as User).Activated == true)
                throw new ValidationException("account is already activated");

            if (content.Required("metadata").RequiredArray("balance_updates", 2)[1].RequiredString("contract") != account)
                throw new ValidationException("invalid activation balance updates");
        }

        protected virtual void ValidateDoubleBaking(JsonElement content)
        {
            var balanceUpdates = content.Required("metadata").RequiredArray("balance_updates").EnumerateArray();
            
            var offenders = balanceUpdates.Where(x => x.RequiredString("kind") == "freezer" && x.RequiredString("category") == "deposits");
            if (offenders.Any())
            {
                if (offenders.Count() > 1)
                    throw new ValidationException("invalid double baking offender updates");

                if (!Cache.Accounts.DelegateExists(offenders.First().RequiredString("delegate")))
                    throw new ValidationException("invalid double baking offender");
            }

            var accusers = balanceUpdates.Where(x => x.RequiredString("kind") == "contract");
            if (accusers.Any())
            {
                if (accusers.Count() > 1)
                    throw new ValidationException("invalid double baking accuser updates");

                var accuserAddress = accusers.First().RequiredString("contract");
                if (!Cache.Accounts.DelegateExists(accuserAddress) || accuserAddress != Proposer)
                    throw new ValidationException("invalid double baking accuser");
            }
        }

        protected virtual async Task ValidateSeedNonceRevelation(JsonElement content)
        {
            var level = content.RequiredInt32("level");
            var proto = await Cache.Protocols.FindByLevelAsync(level);

            if (level % proto.BlocksPerCommitment != 0)
                throw new ValidationException("invalid seed nonce revelation level");

            var balanceUpdate = content.Required("metadata").RequiredArray("balance_updates", 2)[1];

            if (balanceUpdate.RequiredString("kind") != "contract")
                throw new ValidationException("invalid seed nonce revelation balance update kind");

            if (balanceUpdate.RequiredString("contract") != Proposer)
                throw new ValidationException("invalid seed nonce revelation baker");

            if (balanceUpdate.RequiredInt64("change") != Protocol.RevelationReward)
                throw new ValidationException("invalid seed nonce revelation balance update amount");
        }
        
        protected virtual void ValidateVdfRevelation(JsonElement content)
        {
            var balanceUpdate = content.Required("metadata").RequiredArray("balance_updates").EnumerateArray()
                .SingleOrDefault(x => x.RequiredString("kind") == "contract");

            if (balanceUpdate.RequiredString("kind") != "contract")
                throw new ValidationException("invalid vdf revelation balance update kind");

            if (balanceUpdate.ValueKind != JsonValueKind.Undefined && balanceUpdate.RequiredString("contract") != Proposer)
                throw new ValidationException("invalid vdf revelation baker");
        }

        protected virtual void ValidateDrainDelegate(JsonElement content)
        {
            var drainedBaker = content.RequiredString("delegate");
            var balanceUpdates = content.Required("metadata").RequiredArray("balance_updates").EnumerateArray();
            
            if (!Cache.Accounts.DelegateExists(drainedBaker))
                throw new ValidationException("unknown drained delegate");

            if (balanceUpdates.Count() % 2 != 0)
                throw new ValidationException("invalid drain balance updates count");

            if (balanceUpdates.Where(x => x.RequiredInt64("change") < 0).Any(x => x.RequiredString("contract") != drainedBaker))
                throw new ValidationException("invalid drain balance updates");
        }

        protected virtual async Task ValidateDelegation(JsonElement content)
        {
            var source = content.RequiredString("source");
            var delegat = content.OptionalString("delegate");

            if (!await Cache.Accounts.ExistsAsync(source))
                throw new ValidationException("unknown source account");

            if (content.Required("metadata").Required("operation_result").RequiredString("status") == "applied" && delegat != null)
                if (source != delegat && !Cache.Accounts.DelegateExists(delegat))
                    throw new ValidationException("unknown delegate account");

            ValidateFeeBalanceUpdates(
                content.Required("metadata").OptionalArray("balance_updates")?.EnumerateArray() ?? Enumerable.Empty<JsonElement>(),
                source,
                content.RequiredInt64("fee"));
        }

        protected virtual void ValidateInternalDelegation(JsonElement content, string initiator)
        {
            //var delegat = content.OptionalString("delegate");

            //if (content.Required("result").RequiredString("status") == "applied" && delegat != null)
            //    if (!Cache.Accounts.DelegateExists(delegat))
            //        throw new ValidationException("unknown delegate account");
        }

        protected virtual async Task ValidateOrigination(JsonElement content)
        {
            var source = content.RequiredString("source");
            var delegat = content.OptionalString("delegate");
            var metadata = content.Required("metadata");
            var result = metadata.Required("operation_result");
            var applied = result.RequiredString("status") == "applied";

            if (!await Cache.Accounts.ExistsAsync(source))
                throw new ValidationException("unknown source account");

            if (applied && delegat != null)
                if (!Cache.Accounts.DelegateExists(delegat))
                    throw new ValidationException("unknown delegate account");

            ValidateFeeBalanceUpdates(
                metadata.OptionalArray("balance_updates")?.EnumerateArray() ?? Enumerable.Empty<JsonElement>(),
                source,
                content.RequiredInt64("fee"));

            if (applied && result.TryGetProperty("balance_updates", out var resultUpdates))
                ValidateTransferBalanceUpdates(
                    resultUpdates.EnumerateArray(),
                    source,
                    result.RequiredArray("originated_contracts", 1)[0].RequiredString(),
                    content.RequiredInt64("balance"),
                    (result.OptionalInt32("paid_storage_size_diff") ?? 0) * Protocol.ByteCost,
                    Protocol.OriginationSize * Protocol.ByteCost);
        }

        protected virtual void ValidateInternalOrigination(JsonElement content, string initiator)
        {
            //var delegat = content.OptionalString("delegate");
            var result = content.Required("result");
            var applied = result.RequiredString("status") == "applied";

            //if (applied && delegat != null)
            //    if (!Cache.Accounts.DelegateExists(delegat))
            //        throw new ValidationException("unknown delegate account");

            if (applied && result.TryGetProperty("balance_updates", out var resultUpdates))
                ValidateTransferBalanceUpdates(
                    resultUpdates.RequiredArray().EnumerateArray(),
                    content.RequiredString("source"),
                    result.RequiredArray("originated_contracts", 1)[0].RequiredString(),
                    content.RequiredInt64("balance"),
                    (result.OptionalInt32("paid_storage_size_diff") ?? 0) * Protocol.ByteCost,
                    Protocol.OriginationSize * Protocol.ByteCost,
                    initiator);
        }

        protected virtual async Task ValidateTransaction(JsonElement content)
        {
            var source = content.RequiredString("source");

            if (!await Cache.Accounts.ExistsAsync(source))
                throw new ValidationException("unknown source account");

            var metadata = content.Required("metadata");

            ValidateFeeBalanceUpdates(
                metadata.OptionalArray("balance_updates")?.EnumerateArray() ?? Enumerable.Empty<JsonElement>(),
                source,
                content.RequiredInt64("fee"));

            var result = metadata.Required("operation_result");
            var applied = result.RequiredString("status") == "applied";

            if (applied && result.TryGetProperty("balance_updates", out var resultUpdates))
                ValidateTransferBalanceUpdates(
                    resultUpdates.RequiredArray().EnumerateArray(),
                    source,
                    content.RequiredString("destination"),
                    content.RequiredInt64("amount"),
                    (result.OptionalInt32("paid_storage_size_diff") ?? 0) * Protocol.ByteCost,
                    (result.OptionalBool("allocated_destination_contract") ?? false) ? Protocol.OriginationSize * Protocol.ByteCost : 0);

            if (metadata.TryGetProperty("internal_operation_results", out var internalResults))
            {
                foreach (var internalContent in internalResults.RequiredArray().EnumerateArray())
                {
                    switch (internalContent.RequiredString("kind"))
                    {
                        case "delegation": ValidateInternalDelegation(internalContent, source); break;
                        case "origination": ValidateInternalOrigination(internalContent, source); break;
                        case "transaction": ValidateInternalTransaction(internalContent, source); break;
                        case "event": break;
                        default:
                            throw new ValidationException("invalid internal operation kind");
                    }
                }
            }
        }

        protected virtual void ValidateInternalTransaction(JsonElement content, string initiator)
        {
            var result = content.Required("result");
            var applied = result.RequiredString("status") == "applied";

            if (applied && result.TryGetProperty("balance_updates", out var resultUpdates))
                ValidateTransferBalanceUpdates(
                    resultUpdates.RequiredArray().EnumerateArray(),
                    content.RequiredString("source"),
                    content.RequiredString("destination"),
                    content.RequiredInt64("amount"),
                    (result.OptionalInt32("paid_storage_size_diff") ?? 0) * Protocol.ByteCost,
                    (result.OptionalBool("allocated_destination_contract") ?? false) ? Protocol.OriginationSize * Protocol.ByteCost : 0,
                    initiator);
        }

        protected virtual async Task ValidateReveal(JsonElement content)
        {
            var source = content.RequiredString("source");

            if (!await Cache.Accounts.ExistsAsync(source))
                throw new ValidationException("unknown source account");

            ValidateFeeBalanceUpdates(
                content.Required("metadata").OptionalArray("balance_updates")?.EnumerateArray() ?? Enumerable.Empty<JsonElement>(),
                source,
                content.RequiredInt64("fee"));
        }

        protected virtual async Task ValidateRegisterConstant(JsonElement content)
        {
            var source = content.RequiredString("source");

            if (!await Cache.Accounts.ExistsAsync(source))
                throw new ValidationException("unknown source account");

            ValidateFeeBalanceUpdates(
                content.Required("metadata").OptionalArray("balance_updates")?.EnumerateArray() ?? Enumerable.Empty<JsonElement>(),
                source,
                content.RequiredInt64("fee"));
        }

        protected virtual async Task ValidateSetDepositsLimit(JsonElement content)
        {
            var source = content.RequiredString("source");

            if (!await Cache.Accounts.ExistsAsync(source))
                throw new ValidationException("unknown source account");

            ValidateFeeBalanceUpdates(
                content.Required("metadata").OptionalArray("balance_updates")?.EnumerateArray() ?? Enumerable.Empty<JsonElement>(),
                source,
                content.RequiredInt64("fee"));
        }
        
        protected virtual async Task ValidateIncreasePaidStorage(JsonElement content)
        {
            var source = content.RequiredString("source");

            if (!await Cache.Accounts.ExistsAsync(source))
                throw new ValidationException("unknown source account");

            ValidateFeeBalanceUpdates(
                content.Required("metadata").OptionalArray("balance_updates")?.EnumerateArray() ?? Enumerable.Empty<JsonElement>(),
                source,
                content.RequiredInt64("fee"));
        }

        protected virtual async Task ValidateUpdateConsensusKey(JsonElement content)
        {
            var source = content.RequiredString("source");

            if (!await Cache.Accounts.ExistsAsync(source))
                throw new ValidationException("unknown source account");

            ValidateFeeBalanceUpdates(
                content.Required("metadata").OptionalArray("balance_updates")?.EnumerateArray() ?? Enumerable.Empty<JsonElement>(),
                source,
                content.RequiredInt64("fee"));
        }

        protected virtual async Task ValidateTxRollupOrigination(JsonElement content)
        {
            var source = content.RequiredString("source");

            if (!await Cache.Accounts.ExistsAsync(source))
                throw new ValidationException("unknown source account");

            ValidateFeeBalanceUpdates(
                content.Required("metadata").OptionalArray("balance_updates")?.EnumerateArray() ?? Enumerable.Empty<JsonElement>(),
                source,
                content.RequiredInt64("fee"));

            var result = content.Required("metadata").Required("operation_result");
            var applied = result.RequiredString("status") == "applied";

            if (applied && result.TryGetProperty("balance_updates", out var resultUpdates))
                ValidateTransferBalanceUpdates(
                    resultUpdates.EnumerateArray(),
                    source,
                    null,
                    0,
                    0,
                    Protocol.TxRollupOriginationSize * Protocol.ByteCost);
        }

        protected virtual async Task ValidateTxRollupSubmitBatch(JsonElement content)
        {
            var source = content.RequiredString("source");

            if (!await Cache.Accounts.ExistsAsync(source))
                throw new ValidationException("unknown source account");

            ValidateFeeBalanceUpdates(
                content.Required("metadata").OptionalArray("balance_updates")?.EnumerateArray() ?? Enumerable.Empty<JsonElement>(),
                source,
                content.RequiredInt64("fee"));

            var result = content.Required("metadata").Required("operation_result");
            var applied = result.RequiredString("status") == "applied";

            if (applied && result.TryGetProperty("balance_updates", out var resultUpdates))
                ValidateTransferBalanceUpdates(
                    resultUpdates.EnumerateArray(),
                    source,
                    null,
                    0,
                    (result.OptionalInt32("paid_storage_size_diff") ?? 0) * Protocol.ByteCost,
                    0);
        }

        protected virtual async Task ValidateTxRollupCommit(JsonElement content)
        {
            var source = content.RequiredString("source");

            if (!await Cache.Accounts.ExistsAsync(source))
                throw new ValidationException("unknown source account");

            ValidateFeeBalanceUpdates(
                content.Required("metadata").OptionalArray("balance_updates")?.EnumerateArray() ?? Enumerable.Empty<JsonElement>(),
                source,
                content.RequiredInt64("fee"));

            var result = content.Required("metadata").Required("operation_result");
            if (result.TryGetProperty("balance_updates", out var updates) && updates.Count() != 0)
            {
                if (updates.Count() != 2)
                    throw new ValidationException("unexpected number of rollup bonds balance updates");

                if (!updates.EnumerateArray().Any(x =>
                    x.RequiredString("kind") == "contract" &&
                    x.RequiredString("contract") == source))
                    throw new ValidationException("invalid transfer balance updates");

                if (!updates.EnumerateArray().Any(x =>
                    x.RequiredString("kind") == "freezer" &&
                    x.RequiredString("category") == "bonds" &&
                    x.RequiredString("contract") == source))
                    throw new ValidationException("invalid transfer balance updates");

                if (updates[0].RequiredInt64("change") != -updates[1].RequiredInt64("change"))
                    throw new ValidationException("inconsistent change of rollup bonds balance updates");
            }
        }

        protected virtual async Task ValidateTxRollupFinalizeCommitment(JsonElement content)
        {
            var source = content.RequiredString("source");

            if (!await Cache.Accounts.ExistsAsync(source))
                throw new ValidationException("unknown source account");

            ValidateFeeBalanceUpdates(
                content.Required("metadata").OptionalArray("balance_updates")?.EnumerateArray() ?? Enumerable.Empty<JsonElement>(),
                source,
                content.RequiredInt64("fee"));

            var result = content.Required("metadata").Required("operation_result");
            if (result.TryGetProperty("balance_updates", out var updates) && updates.Count() != 0)
                throw new ValidationException("unexpected balance updates");
        }

        protected virtual async Task ValidateTxRollupRemoveCommitment(JsonElement content)
        {
            var source = content.RequiredString("source");

            if (!await Cache.Accounts.ExistsAsync(source))
                throw new ValidationException("unknown source account");

            ValidateFeeBalanceUpdates(
                content.Required("metadata").OptionalArray("balance_updates")?.EnumerateArray() ?? Enumerable.Empty<JsonElement>(),
                source,
                content.RequiredInt64("fee"));

            var result = content.Required("metadata").Required("operation_result");
            if (result.TryGetProperty("balance_updates", out var updates) && updates.Count() != 0)
                throw new ValidationException("unexpected balance updates");
        }

        protected virtual async Task ValidateTxRollupReturnBond(JsonElement content)
        {
            var source = content.RequiredString("source");

            if (!await Cache.Accounts.ExistsAsync(source))
                throw new ValidationException("unknown source account");

            ValidateFeeBalanceUpdates(
                content.Required("metadata").OptionalArray("balance_updates")?.EnumerateArray() ?? Enumerable.Empty<JsonElement>(),
                source,
                content.RequiredInt64("fee"));

            var result = content.Required("metadata").Required("operation_result");
            if (result.TryGetProperty("balance_updates", out var updates) && updates.Count() != 0)
            {
                if (updates.Count() != 2)
                    throw new ValidationException("unexpected number of rollup bonds balance updates");

                if (!updates.EnumerateArray().Any(x =>
                    x.RequiredString("kind") == "contract" &&
                    x.RequiredString("contract") == source))
                    throw new ValidationException("invalid transfer balance updates");

                if (!updates.EnumerateArray().Any(x =>
                    x.RequiredString("kind") == "freezer" &&
                    x.RequiredString("category") == "bonds" &&
                    x.RequiredString("contract") == source))
                    throw new ValidationException("invalid transfer balance updates");

                if (updates[0].RequiredInt64("change") != -updates[1].RequiredInt64("change"))
                    throw new ValidationException("inconsistent change of rollup bonds balance updates");
            }
        }

        protected virtual async Task ValidateTxRollupRejection(JsonElement content)
        {
            var source = content.RequiredString("source");

            if (!await Cache.Accounts.ExistsAsync(source))
                throw new ValidationException("unknown source account");

            ValidateFeeBalanceUpdates(
                content.Required("metadata").OptionalArray("balance_updates")?.EnumerateArray() ?? Enumerable.Empty<JsonElement>(),
                source,
                content.RequiredInt64("fee"));
        }

        protected virtual async Task ValidateTxRollupDispatchTickets(JsonElement content)
        {
            var source = content.RequiredString("source");

            if (!await Cache.Accounts.ExistsAsync(source))
                throw new ValidationException("unknown source account");

            ValidateFeeBalanceUpdates(
                content.Required("metadata").OptionalArray("balance_updates")?.EnumerateArray() ?? Enumerable.Empty<JsonElement>(),
                source,
                content.RequiredInt64("fee"));
        }

        protected virtual async Task ValidateTransferTicket(JsonElement content)
        {
            var source = content.RequiredString("source");

            if (!await Cache.Accounts.ExistsAsync(source))
                throw new ValidationException("unknown source account");

            ValidateFeeBalanceUpdates(
                content.Required("metadata").OptionalArray("balance_updates")?.EnumerateArray() ?? Enumerable.Empty<JsonElement>(),
                source,
                content.RequiredInt64("fee"));
        }

        protected virtual void ValidateFeeBalanceUpdates(IEnumerable<JsonElement> balanceUpdates, string sender, long fee)
        {
            if (fee != 0)
            {
                if (balanceUpdates.Count() != 2)
                    throw new ValidationException("invalid fee balance updates count");

                var first = balanceUpdates.First();
                var last = balanceUpdates.Last();

                if (first.RequiredString("kind") != "contract" ||
                    first.RequiredString("contract") != sender ||
                    first.RequiredInt64("change") != -fee)
                    throw new ValidationException("invalid fee contract update");

                if (last.RequiredString("kind") != "accumulator" ||
                    last.RequiredString("category") != "block fees" ||
                    last.RequiredInt64("change") != fee)
                    throw new ValidationException("invalid fee accumulator update");
            }
            else
            {
                if (balanceUpdates.Any())
                    throw new ValidationException("invalid fee balance updates count");
            }
        }

        protected virtual void ValidateTransferBalanceUpdates(IEnumerable<JsonElement> balanceUpdates, string sender, string target, long amount, long storageFee, long allocationFee, string initiator = null)
        {
            if (balanceUpdates.Count() != (amount != 0 ? 2 : 0) + (storageFee != 0 ? 2 : 0) + (allocationFee != 0 ? 2 : 0))
                throw new ValidationException("invalid transfer balance updates count");

            if (amount > 0)
            {
                if (!balanceUpdates.Any(x =>
                    x.RequiredString("kind") == "contract" &&
                    x.RequiredInt64("change") == -amount &&
                    x.RequiredString("contract") == sender))
                    throw new ValidationException("invalid transfer balance updates");

                if (!balanceUpdates.Any(x =>
                    x.RequiredString("kind") == "contract" &&
                    x.RequiredInt64("change") == amount &&
                    x.RequiredString("contract") == target))
                    throw new ValidationException("invalid transfer balance updates");
            }

            if (storageFee > 0)
            {
                if (!balanceUpdates.Any(x =>
                    x.RequiredString("kind") == "contract" &&
                    x.RequiredInt64("change") == -storageFee &&
                    x.RequiredString("contract") == (initiator ?? sender)))
                    throw new ValidationException("invalid storage fee balance updates");

                if (!balanceUpdates.Any(x =>
                    x.RequiredString("kind") == "burned" &&
                    x.RequiredString("category") == "storage fees" &&
                    x.RequiredInt64("change") == storageFee))
                    throw new ValidationException("invalid storage fee balance updates");
            }

            if (allocationFee > 0)
            {
                if (!balanceUpdates.Any(x =>
                    x.RequiredString("kind") == "contract" &&
                    x.RequiredInt64("change") == -allocationFee &&
                    x.RequiredString("contract") == (initiator ?? sender)))
                    throw new ValidationException("invalid allocation fee balance updates");

                if (!balanceUpdates.Any(x =>
                    x.RequiredString("kind") == "burned" &&
                    x.RequiredString("category") == "storage fees" &&
                    x.RequiredInt64("change") == allocationFee))
                    throw new ValidationException("invalid allocation fee balance updates");
            }
        }
    }
}
