using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Tzkt.Data.Models;
using Tzkt.Sync.Services;

namespace Tzkt.Sync.Protocols.Proto12
{
    class Validator : IValidator
    {
        readonly CacheService Cache;
        JsonElement Block;
        Protocol Protocol;
        string Proposer;
        string Baker;
        int Level;
        int Cycle;

        public Validator(ProtocolHandler protocol) => Cache = protocol.Cache;

        public virtual async Task ValidateBlock(JsonElement block)
        {
            Block = block;
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

            Baker = metadata.RequiredString("baker");
            if (!Cache.Accounts.DelegateExists(Baker))
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

                if (periodKind != period.Kind)
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
            if (balanceUpdates.Any(x => x.RequiredString("kind") == "contract" && !Cache.Accounts.DelegateExists(x.RequiredString("contract"))))
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
                
                if (balanceUpdates.Count() > 4 && !Protocol.IsCycleEnd(Level))
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
                            case "preendorsement": ValidateEndorsement(content); break;
                            case "ballot": await ValidateBallot(content); break;
                            case "proposals": ValidateProposal(content); break;
                            //case "activate_account": await ValidateActivation(content); break;
                            //case "double_baking_evidence": ValidateDoubleBaking(content); break;
                            //case "double_endorsement_evidence": ValidateDoubleEndorsing(content); break;
                            //case "seed_nonce_revelation": await ValidateSeedNonceRevelation(content); break;
                            //case "delegation": await ValidateDelegation(content); break;
                            //case "origination": await ValidateOrigination(content); break;
                            //case "transaction": await ValidateTransaction(content); break;
                            //case "reveal": await ValidateReveal(content); break;
                            //case "register_global_constant": await ValidateRegisterConstant(content); break;
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

            if (!Cache.Accounts.DelegateExists(content.RequiredString("source")))
                throw new ValidationException("invalid proposal sender");
        }

        protected virtual async Task ValidateActivation(JsonElement content)
        {
            var account = content.RequiredString("pkh");

            if (await Cache.Accounts.ExistsAsync(account, AccountType.User) &&
                ((await Cache.Accounts.GetAsync(account)) as User).Activated == true)
                throw new ValidationException("account is already activated");

            if (content.Required("metadata").RequiredArray("balance_updates", 1)[0].RequiredString("contract") != account)
                throw new ValidationException("invalid activation balance updates");
        }

        protected virtual void ValidateDoubleBaking(JsonElement content)
        {
            if (content.Required("bh1").RequiredInt32("level") != content.Required("bh2").RequiredInt32("level"))
                throw new ValidationException("inconsistent double baking evidence");

            var balanceUpdates = ParseBalanceUpdates(content.Required("metadata").RequiredArray("balance_updates").EnumerateArray());
            if (balanceUpdates.Count > 0)
            {
                var rewardsUpdate = balanceUpdates.FirstOrDefault(x => x.Kind == BalanceUpdateKind.Rewards && x.Change > 0);
                var lostDepositsUpdate = balanceUpdates.FirstOrDefault(x => x.Kind == BalanceUpdateKind.Deposits && x.Change < 0);
                var lostRewardsUpdate = balanceUpdates.FirstOrDefault(x => x.Kind == BalanceUpdateKind.Rewards && x.Change < 0);
                var lostFeesUpdate = balanceUpdates.FirstOrDefault(x => x.Kind == BalanceUpdateKind.Fees && x.Change < 0);

                if (balanceUpdates.Count != (rewardsUpdate != null ? 1 : 0) + (lostDepositsUpdate != null ? 1 : 0)
                     + (lostRewardsUpdate != null ? 1 : 0) + (lostFeesUpdate != null ? 1 : 0))
                    throw new ValidationException("invalid double baking balance updates count");

                if (rewardsUpdate != null && rewardsUpdate.Account != Baker)
                    throw new ValidationException("invalid double baking reward recipient");

                if ((rewardsUpdate?.Change ?? 0) != -((lostDepositsUpdate?.Change ?? 0) + (lostFeesUpdate?.Change ?? 0)) / 2)
                    throw new ValidationException("invalid double baking reward amount");

                var offender = lostDepositsUpdate?.Account ?? lostRewardsUpdate?.Account ?? lostFeesUpdate?.Account;

                if (!Cache.Accounts.DelegateExists(offender))
                    throw new ValidationException("invalid double baking offender");

                if (lostDepositsUpdate != null && lostDepositsUpdate.Account != offender ||
                    lostRewardsUpdate != null && lostRewardsUpdate.Account != offender ||
                    lostFeesUpdate != null && lostFeesUpdate.Account != offender)
                    throw new ValidationException("invalid double baking offender updates");

                var accusedLevel = content.Required("bh1").RequiredInt32("level");
                var accusedCycle = Cache.Blocks.Get(accusedLevel).Cycle;
                if (lostDepositsUpdate != null && lostDepositsUpdate.Cycle != accusedCycle ||
                    lostRewardsUpdate != null && lostRewardsUpdate.Cycle != accusedCycle ||
                    lostFeesUpdate != null && lostFeesUpdate.Cycle != accusedCycle)
                    throw new ValidationException("invalid double baking freezer level");
            }
        }

        protected virtual void ValidateDoubleEndorsing(JsonElement content)
        {
            if (content.Required("op1").Required("operations").RequiredString("kind") != "endorsement" ||
                content.Required("op2").Required("operations").RequiredString("kind") != "endorsement")
                throw new ValidationException("inconsistent double endorsing evidence");

            if (content.Required("op1").Required("operations").RequiredInt32("level")
                != content.Required("op2").Required("operations").RequiredInt32("level"))
                throw new ValidationException("inconsistent double endorsing evidence");

            var balanceUpdates = ParseBalanceUpdates(content.Required("metadata").RequiredArray("balance_updates").EnumerateArray());
            if (balanceUpdates.Count > 0)
            {
                var rewardsUpdate = balanceUpdates.FirstOrDefault(x => x.Kind == BalanceUpdateKind.Rewards && x.Change > 0);
                var lostDepositsUpdate = balanceUpdates.FirstOrDefault(x => x.Kind == BalanceUpdateKind.Deposits && x.Change < 0);
                var lostRewardsUpdate = balanceUpdates.FirstOrDefault(x => x.Kind == BalanceUpdateKind.Rewards && x.Change < 0);
                var lostFeesUpdate = balanceUpdates.FirstOrDefault(x => x.Kind == BalanceUpdateKind.Fees && x.Change < 0);

                if (balanceUpdates.Count != (rewardsUpdate != null ? 1 : 0) + (lostDepositsUpdate != null ? 1 : 0)
                     + (lostRewardsUpdate != null ? 1 : 0) + (lostFeesUpdate != null ? 1 : 0))
                    throw new ValidationException("invalid double endorsing balance updates count");

                if (rewardsUpdate != null && rewardsUpdate.Account != Baker)
                    throw new ValidationException("invalid double endorsing reward recipient");

                if ((rewardsUpdate?.Change ?? 0) != -((lostDepositsUpdate?.Change ?? 0) + (lostFeesUpdate?.Change ?? 0)) / 2)
                    throw new ValidationException("invalid double endorsing reward amount");

                var offender = lostDepositsUpdate?.Account ?? lostRewardsUpdate?.Account ?? lostFeesUpdate?.Account;

                if (!Cache.Accounts.DelegateExists(offender))
                    throw new ValidationException("invalid double endorsing offender");

                if (lostDepositsUpdate != null && lostDepositsUpdate.Account != offender ||
                    lostRewardsUpdate != null && lostRewardsUpdate.Account != offender ||
                    lostFeesUpdate != null && lostFeesUpdate.Account != offender)
                    throw new ValidationException("invalid double endorsing offender updates");

                var accusedLevel = content.Required("op1").Required("operations").RequiredInt32("level");
                var accusedCycle = Cache.Blocks.Get(accusedLevel).Cycle;
                if (lostDepositsUpdate != null && lostDepositsUpdate.Cycle != accusedCycle ||
                    lostRewardsUpdate != null && lostRewardsUpdate.Cycle != accusedCycle ||
                    lostFeesUpdate != null && lostFeesUpdate.Cycle != accusedCycle)
                    throw new ValidationException("invalid double endorsing freezer level");
            }
        }

        protected virtual async Task ValidateSeedNonceRevelation(JsonElement content)
        {
            var level = content.RequiredInt32("level");
            var proto = await Cache.Protocols.FindByLevelAsync(level);

            if (level % proto.BlocksPerCommitment != 0)
                throw new ValidationException("invalid seed nonce revelation level");

            var balanceUpdate = content.Required("metadata").RequiredArray("balance_updates", 1)[0];

            if (balanceUpdate.RequiredString("delegate") != Baker)
                throw new ValidationException("invalid seed nonce revelation baker");

            if (balanceUpdate.RequiredString("category") != "rewards")
                throw new ValidationException("invalid seed nonce revelation balance update type");

            if (balanceUpdate.RequiredInt64("change") != Protocol.RevelationReward)
                throw new ValidationException("invalid seed nonce revelation balance update amount");
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
                ParseBalanceUpdates(content.Required("metadata").RequiredArray("balance_updates").EnumerateArray()),
                source,
                content.RequiredInt64("fee"));
        }

        protected virtual void ValidateInternalDelegation(JsonElement content, string initiator)
        {
            var delegat = content.OptionalString("delegate");

            if (content.Required("result").RequiredString("status") == "applied" && delegat != null)
                if (!Cache.Accounts.DelegateExists(delegat))
                    throw new ValidationException("unknown delegate account");
        }

        protected virtual async Task ValidateOrigination(JsonElement content)
        {
            var source = content.RequiredString("source");
            var delegat = content.OptionalString("delegate");
            var metadata = content.Required("metadata");
            var result = metadata.Required("operation_result");

            if (!await Cache.Accounts.ExistsAsync(source))
                throw new ValidationException("unknown source account");

            if (result.RequiredString("status") == "applied" && delegat != null)
                if (!Cache.Accounts.DelegateExists(delegat))
                    throw new ValidationException("unknown delegate account");

            ValidateFeeBalanceUpdates(
                ParseBalanceUpdates(metadata.RequiredArray("balance_updates").EnumerateArray()),
                source,
                content.RequiredInt64("fee"));

            if (result.TryGetProperty("balance_updates", out var resultUpdates))
                ValidateTransferBalanceUpdates(
                    ParseBalanceUpdates(resultUpdates.EnumerateArray()),
                    source,
                    result.RequiredArray("originated_contracts", 1)[0].RequiredString(),
                    content.RequiredInt64("balance"),
                    (result.OptionalInt32("paid_storage_size_diff") ?? 0) * Protocol.ByteCost,
                    Protocol.OriginationSize * Protocol.ByteCost);
        }

        protected virtual void ValidateInternalOrigination(JsonElement content, string initiator)
        {
            var delegat = content.OptionalString("delegate");
            var result = content.Required("result");

            if (result.RequiredString("status") == "applied" && delegat != null)
                if (!Cache.Accounts.DelegateExists(delegat))
                    throw new ValidationException("unknown delegate account");

            if (result.TryGetProperty("balance_updates", out var resultUpdates))
                ValidateTransferBalanceUpdates(
                    ParseBalanceUpdates(resultUpdates.RequiredArray().EnumerateArray()),
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
                ParseBalanceUpdates(metadata.RequiredArray("balance_updates").EnumerateArray()),
                source,
                content.RequiredInt64("fee"));

            var result = metadata.Required("operation_result");

            if (result.TryGetProperty("balance_updates", out var resultUpdates))
                ValidateTransferBalanceUpdates(
                    ParseBalanceUpdates(resultUpdates.RequiredArray().EnumerateArray()),
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
                        default:
                            throw new ValidationException("invalid internal operation kind");
                    }
                }
            }
        }

        protected virtual void ValidateInternalTransaction(JsonElement content, string initiator)
        {
            var result = content.Required("result");

            if (result.TryGetProperty("balance_updates", out var resultUpdates))
                ValidateTransferBalanceUpdates(
                    ParseBalanceUpdates(resultUpdates.RequiredArray().EnumerateArray()),
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
                ParseBalanceUpdates(content.Required("metadata").RequiredArray("balance_updates").EnumerateArray()),
                source,
                content.RequiredInt64("fee"));
        }

        protected virtual async Task ValidateRegisterConstant(JsonElement content)
        {
            var source = content.RequiredString("source");

            if (!await Cache.Accounts.ExistsAsync(source))
                throw new ValidationException("unknown source account");

            ValidateFeeBalanceUpdates(
                ParseBalanceUpdates(content.Required("metadata").RequiredArray("balance_updates").EnumerateArray()),
                source,
                content.RequiredInt64("fee"));
        }

        protected virtual void ValidateFeeBalanceUpdates(List<BalanceUpdate> balanceUpdates, string sender, long fee)
        {
            if (balanceUpdates.Count != (fee != 0 ? 2 : 0))
                throw new ValidationException("invalid fee balance updates count");

            if (fee != 0)
            {
                if (balanceUpdates[0].Kind != BalanceUpdateKind.Contract ||
                    balanceUpdates[0].Change != -fee ||
                    balanceUpdates[0].Account != sender)
                    throw new ValidationException("invalid fee contract balance update");

                if (balanceUpdates[1].Kind != BalanceUpdateKind.Fees ||
                    balanceUpdates[1].Change != fee ||
                    balanceUpdates[1].Account != Baker ||
                    balanceUpdates[1].Cycle != Cycle)
                    throw new ValidationException("invalid fee freezer fees update");
            }
        }

        protected virtual void ValidateTransferBalanceUpdates(List<BalanceUpdate> balanceUpdates, string sender, string target, long amount, long storageFee, long allocationFee, string initiator = null)
        {
            if (balanceUpdates.Count != (amount != 0 ? 2 : 0) + (storageFee != 0 ? 1 : 0) + (allocationFee != 0 ? 1 : 0))
                throw new ValidationException("invalid transfer balance updates count");

            if (amount > 0)
            {
                if (!balanceUpdates.Any(x =>
                    x.Kind == BalanceUpdateKind.Contract &&
                    x.Change == -amount &&
                    x.Account == sender))
                    throw new ValidationException("invalid transfer balance updates");

                if (!balanceUpdates.Any(x =>
                    x.Kind == BalanceUpdateKind.Contract &&
                    x.Change == amount &&
                    x.Account == target))
                    throw new ValidationException("invalid transfer balance updates");
            }

            if (storageFee > 0)
            {
                if (!balanceUpdates.Any(x =>
                    x.Kind == BalanceUpdateKind.Contract &&
                    x.Change == -storageFee &&
                    x.Account == (initiator ?? sender)))
                    throw new ValidationException("invalid transfer balance updates");
            }

            if (allocationFee > 0)
            {
                if (!balanceUpdates.Any(x =>
                    x.Kind == BalanceUpdateKind.Contract &&
                    x.Change == -allocationFee &&
                    x.Account == (initiator ?? sender)))
                    throw new ValidationException("invalid transfer balance updates");
            }
        }

        protected virtual List<BalanceUpdate> ParseBalanceUpdates(IEnumerable<JsonElement> updates)
        {
            var res = new List<BalanceUpdate>(4);
            foreach (var update in updates)
            {
                res.Add(update.RequiredString("kind") switch
                {
                    "contract" => new BalanceUpdate
                    {
                        Kind = BalanceUpdateKind.Contract,
                        Account = update.RequiredString("contract"),
                        Change = update.RequiredInt64("change")
                    },
                    "freezer" => new BalanceUpdate
                    {
                        Kind = update.RequiredString("category") switch
                        {
                            "deposits" => BalanceUpdateKind.Deposits,
                            "rewards" => BalanceUpdateKind.Rewards,
                            "fees" => BalanceUpdateKind.Fees,
                            _ => throw new ValidationException("invalid freezer category")
                        },
                        Account = update.RequiredString("delegate"),
                        Change = update.RequiredInt64("change"),
                        Cycle = update.RequiredInt32("cycle"),
                    },
                    _ => throw new ValidationException("invalid balance update kind")
                });
            }
            return res;
        }

        protected enum BalanceUpdateKind
        {
            Contract,
            Deposits,
            Rewards,
            Fees
        }

        protected class BalanceUpdate
        {
            public BalanceUpdateKind Kind { get; set; }
            public string Account { get; set; }
            public long Change { get; set; }
            public int Cycle { get; set; }

            public bool IsBlockUpdate =>
                Kind == BalanceUpdateKind.Contract && Change < 0 ||
                Kind != BalanceUpdateKind.Contract && Change > 0;

            public bool IsCycleUpdate =>
                Kind == BalanceUpdateKind.Contract && Change > 0 ||
                Kind != BalanceUpdateKind.Contract && Change < 0;
        }
    }
}
