using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

using Tzkt.Data.Models;
using Tzkt.Sync.Services;

namespace Tzkt.Sync.Protocols.Proto1
{
    class Validator : IValidator
    {
        protected readonly CacheService Cache;

        protected Protocol Protocol;
        protected Block LastBlock;
        protected string Baker;
        protected int Level;
        protected int Cycle;

        public Validator(ProtocolHandler protocol) => Cache = protocol.Cache;

        public virtual async Task ValidateBlock(JsonElement block)
        {
            var protocol = block.RequiredString("protocol");
            Protocol = await Cache.Protocols.GetAsync(protocol);

            if (protocol != Cache.AppState.GetNextProtocol())
                throw new ValidationException($"invalid block protocol", true);

            ValidateBlockHeader(block.Required("header"));
            await ValidateBlockMetadata(block.Required("metadata"));
            await ValidateOperations(block.RequiredArray("operations", 4));
        }

        protected virtual void ValidateBlockHeader(JsonElement header)
        {
            Level = header.RequiredInt32("level");
            Cycle = (Level - 1) /  Protocol.BlocksPerCycle;

            if (Level != Cache.AppState.GetNextLevel())
                throw new ValidationException($"invalid block level", true);

            if (header.RequiredString("predecessor") != Cache.AppState.GetHead())
                throw new ValidationException($"invalid block predecessor", true);
        }

        protected virtual async Task ValidateBlockMetadata(JsonElement metadata)
        {
            Baker = metadata.RequiredString("baker");

            if (!Cache.Accounts.DelegateExists(Baker))
                throw new ValidationException($"non-existent block baker");

            await ValidateBlockVoting(metadata.RequiredString("voting_period_kind"));

            foreach (var baker in metadata.RequiredArray("deactivated").EnumerateArray())
                if (!Cache.Accounts.DelegateExists(baker.GetString()))
                    throw new ValidationException($"non-existent deactivated baker {baker}");

            var balanceUpdates = ParseBalanceUpdates(metadata.RequiredArray("balance_updates"));
            ValidateBlockRewards(balanceUpdates.Take(Protocol.BlockReward0 > 0 ? 3 : 2));
            ValidateCycleRewards(balanceUpdates.Skip(Protocol.BlockReward0 > 0 ? 3 : 2));
        }

        protected virtual async Task ValidateBlockVoting(string periodKind)
        {
            var period = await Cache.Periods.CurrentAsync();
            var kind = periodKind switch
            {
                "proposal" => VotingPeriods.Proposal,
                "exploration" => VotingPeriods.Exploration,
                "testing" => VotingPeriods.Testing,
                "promotion" => VotingPeriods.Promotion,
                _ => throw new ValidationException("invalid voting period kind")
            };

            if (Level <= period.EndLevel)
            {
                if (period.Kind != kind)
                    throw new ValidationException("unexpected voting period");
            }
            else
            {
                if ((int)kind != ((int)period.Kind + 1) % 4)
                    throw new ValidationException("inconsistent voting period");
            }
        }

        protected virtual void ValidateBlockRewards(IEnumerable<BalanceUpdate> balanceUpdates)
        {
            if (balanceUpdates.Count() > 0)
            {
                var contractUpdate = balanceUpdates.FirstOrDefault(x => x.Kind == BalanceUpdateKind.Contract)
                    ?? throw new ValidationException("missed block contract balance update");

                var depostisUpdate = balanceUpdates.FirstOrDefault(x => x.Kind == BalanceUpdateKind.Deposits)
                    ?? throw new ValidationException("missed block freezer depostis update");

                if (contractUpdate.Account != Baker || contractUpdate.Change != -GetBlockDeposit())
                    throw new ValidationException("invalid block contract balance update");

                if (depostisUpdate.Account != Baker || depostisUpdate.Change != GetBlockDeposit())
                    throw new ValidationException("invalid block freezer depostis update");

                if (balanceUpdates.Count() == 3)
                {
                    var rewardsUpdate = balanceUpdates.FirstOrDefault(x => x.Kind == BalanceUpdateKind.Rewards)
                        ?? throw new ValidationException("missed block freezer rewards update");

                    if (rewardsUpdate.Account != Baker || rewardsUpdate.Change != GetBlockReward())
                        throw new ValidationException("invalid block freezer rewards update");
                }
            }
        }

        protected virtual void ValidateCycleRewards(IEnumerable<BalanceUpdate> balanceUpdates)
        {
            if (balanceUpdates.Count() > 0)
            {
                if (Level % Protocol.BlocksPerCycle != 0)
                    throw new ValidationException("unexpected cycle rewards");

                foreach (var update in balanceUpdates)
                {
                    if (!Cache.Accounts.DelegateExists(update.Account))
                        throw new ValidationException($"unknown delegate {update.Account}");

                    if (update.Kind != BalanceUpdateKind.Contract)
                        if (update.Cycle != Cycle - Protocol.PreservedCycles && update.Cycle != Cycle - 1)
                            throw new ValidationException("invalid freezer updates cycle");
                }
            }
        }

        protected virtual async Task ValidateOperations(JsonElement operations)
        {
            foreach (var group in operations.EnumerateArray())
                foreach (var op in group.RequiredArray().EnumerateArray())
                {
                    if (op.RequiredString("hash").Length == 0)
                        throw new ValidationException("invalid operation hash");

                    foreach (var content in op.RequiredArray("contents").EnumerateArray())
                    {
                        switch (content.RequiredString("kind"))
                        {
                            case "endorsement": ValidateEndorsement(content); break;
                            case "ballot": await ValidateBallot(content); break;
                            case "proposals": await ValidateProposal(content); break;
                            case "activate_account": await ValidateActivation(content); break;
                            case "double_baking_evidence": ValidateDoubleBaking(content); break;
                            case "double_endorsement_evidence": ValidateDoubleEndorsing(content); break;
                            case "seed_nonce_revelation": ValidateSeedNonceRevelation(content); break;
                            case "delegation": await ValidateDelegation(content); break;
                            case "origination": await ValidateOrigination(content); break;
                            case "transaction": await ValidateTransaction(content); break;
                            case "reveal": await ValidateReveal(content); break;
                            default:
                                throw new ValidationException("invalid operation content kind");
                        }
                    }
                }
        }

        protected virtual void ValidateEndorsement(JsonElement content)
        {
            if (content.RequiredInt32("level") != Cache.AppState.GetLevel())
                throw new ValidationException("invalid endorsed block level");

            var metadata = content.Required("metadata");
            var delegat = metadata.RequiredString("delegate");
            var slots = metadata.RequiredArray("slots").Count();

            var balanceUpdates = ParseBalanceUpdates(metadata.RequiredArray("balance_updates"));

            if (!Cache.Accounts.DelegateExists(delegat))
                throw new ValidationException($"unknown endorsement delegate {delegat}");

            if (balanceUpdates.Count > 0)
            {
                if (balanceUpdates.Count != (Protocol.BlockReward0 > 0 ? 3 : 2)) // TODO: depend on no_reward_cycles
                    throw new ValidationException("invalid endorsement balance updates count");

                var contractUpdate = balanceUpdates.FirstOrDefault(x => x.Kind == BalanceUpdateKind.Contract)
                    ?? throw new ValidationException("missed endorsement contract balance update");

                var depostisUpdate = balanceUpdates.FirstOrDefault(x => x.Kind == BalanceUpdateKind.Deposits)
                    ?? throw new ValidationException("missed endorsement freezer depostis update");

                if (contractUpdate.Account != delegat || contractUpdate.Change != -GetEndorsementDeposit(slots))
                    throw new ValidationException("invalid endorsement contract balance update");

                if (depostisUpdate.Account != delegat || depostisUpdate.Change != GetEndorsementDeposit(slots))
                    throw new ValidationException("invalid endorsement freezer depostis update");

                if (balanceUpdates.Count > 2)
                {
                    var rewardsUpdate = balanceUpdates.FirstOrDefault(x => x.Kind == BalanceUpdateKind.Rewards)
                        ?? throw new ValidationException("missed block freezer rewards update");

                    if (rewardsUpdate.Account != delegat || rewardsUpdate.Change != GetEndorsementReward(slots))
                        throw new ValidationException("invalid block freezer rewards update");
                }
            }
        }

        protected virtual async Task ValidateBallot(JsonElement content)
        {
            var period = await Cache.Periods.CurrentAsync();
            if (period.EndLevel == Cache.AppState.GetLevel()) return; // Voting period is not added yet
            var proposal = await Cache.Proposals.GetAsync((period as ExplorationPeriod)?.ProposalId ?? (period as PromotionPeriod).ProposalId);
            
            if (content.RequiredString("proposal") != proposal.Hash)
                throw new ValidationException("invalid ballot proposal");

            if (!Cache.Accounts.DelegateExists(content.RequiredString("source")))
                throw new ValidationException("invalid ballot sender");

            if (content.RequiredInt32("period") != period.Code)
                throw new ValidationException("invalid ballot voting period");
        }

        protected virtual async Task ValidateProposal(JsonElement content)
        {
            var period = await Cache.Periods.CurrentAsync();
            if (period.EndLevel == Cache.AppState.GetLevel()) return; // Voting period is not added yet

            if (!Cache.Accounts.DelegateExists(content.RequiredString("source")))
                throw new ValidationException("invalid proposal sender");

            if (content.RequiredInt32("period") != period.Code)
                throw new ValidationException("invalid proposal voting period");
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

            var balanceUpdates = ParseBalanceUpdates(content.Required("metadata").RequiredArray("balance_updates"));
            if (balanceUpdates.Count > 0)
            {
                var rewardsUpdate = balanceUpdates.FirstOrDefault(x => x.Kind == BalanceUpdateKind.Rewards && x.Change > 0);
                var lostDepositsUpdate = balanceUpdates.FirstOrDefault(x => x.Kind == BalanceUpdateKind.Deposits && x.Change < 0);
                var lostRewardsUpdate = balanceUpdates.FirstOrDefault(x => x.Kind == BalanceUpdateKind.Rewards && x.Change < 0);
                var lostFeesUpdate = balanceUpdates.FirstOrDefault(x => x.Kind == BalanceUpdateKind.Fees && x.Change < 0);

                if (balanceUpdates.Count !=  (rewardsUpdate != null ? 1 : 0) + (lostDepositsUpdate != null ? 1 : 0)
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

                var accusedCycle = (content.Required("bh1").RequiredInt32("level") - 1) / Protocol.BlocksPerCycle;
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

            var balanceUpdates = ParseBalanceUpdates(content.Required("metadata").RequiredArray("balance_updates"));
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

                var accusedCycle = (content.Required("op1").Required("operations").RequiredInt32("level") - 1) / Protocol.BlocksPerCycle;
                if (lostDepositsUpdate != null && lostDepositsUpdate.Cycle != accusedCycle ||
                    lostRewardsUpdate != null && lostRewardsUpdate.Cycle != accusedCycle ||
                    lostFeesUpdate != null && lostFeesUpdate.Cycle != accusedCycle)
                    throw new ValidationException("invalid double endorsing freezer level");
            }
        }

        protected virtual void ValidateSeedNonceRevelation(JsonElement content)
        {
            if (content.RequiredInt32("level") % Protocol.BlocksPerCommitment != 0)
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
            var delegat = content.RequiredString("delegate");

            if (!await Cache.Accounts.ExistsAsync(source, AccountType.Contract))
                throw new ValidationException("unknown source contract");

            if (content.Required("operation_result").RequiredString("status") == "applied" && delegat != null)
                if (!Cache.Accounts.DelegateExists(delegat))
                    throw new ValidationException("unknown delegate account");

            ValidateFeeBalanceUpdates(
                ParseBalanceUpdates(content.Required("metadata").RequiredArray("balance_updates")),
                source,
                content.RequiredInt64("fee"));
        }

        protected virtual async Task ValidateInternalDelegation(JsonElement content, string initiator)
        {
            var source = content.RequiredString("source");
            var delegat = content.RequiredString("delegate");

            if (!await Cache.Accounts.ExistsAsync(source, AccountType.Contract))
                throw new ValidationException("unknown source contract");

            if (content.Required("result").RequiredString("status") == "applied" && delegat != null)
                if (!Cache.Accounts.DelegateExists(delegat))
                    throw new ValidationException("unknown delegate account");
        }

        protected virtual async Task ValidateOrigination(JsonElement content)
        {
            var source = content.RequiredString("source");
            var delegat = content.RequiredString("delegate");
            var metadata = content.Required("metadata");
            var result = metadata.Required("operation_result");

            if (!await Cache.Accounts.ExistsAsync(source, AccountType.Contract))
                throw new ValidationException("unknown source contract");

            //if (result.RequiredString("status") == "applied" && delegat != null)
            //    if (!Cache.Accounts.DelegateExists(delegat))
            //        throw new ValidationException("unknown delegate account");

            ValidateFeeBalanceUpdates(
                ParseBalanceUpdates(metadata.RequiredArray("balance_updates")),
                source,
                content.RequiredInt64("fee"));

            if (result.TryGetProperty("balance_updates", out var resultUpdates))
                ValidateTransferBalanceUpdates(
                    ParseBalanceUpdates(resultUpdates),
                    source,
                    result.RequiredArray("originated_contracts", 1)[0].RequiredString(),
                    content.RequiredInt64("balance"),
                    ((result.OptionalInt32("paid_storage_size_diff") ?? 0) + Protocol.OriginationSize) * Protocol.ByteCost,
                     0);
        }

        protected virtual async Task ValidateInternalOrigination(JsonElement content, string initiator)
        {
            var source = content.RequiredString("source");
            var delegat = content.RequiredString("delegate");
            var result = content.Required("result");

            if (!await Cache.Accounts.ExistsAsync(source, AccountType.Contract))
                throw new ValidationException("unknown source contract");

            //if (result.RequiredString("status") == "applied" && delegat != null)
            //    if (!Cache.Accounts.DelegateExists(delegat))
            //        throw new ValidationException("unknown delegate account");

            if (result.TryGetProperty("balance_updates", out var resultUpdates))
                ValidateTransferBalanceUpdates(
                    ParseBalanceUpdates(resultUpdates.RequiredArray()),
                    source,
                    result.RequiredArray("originated_contracts", 1)[0].RequiredString(),
                    content.RequiredInt64("balance"),
                    ((result.OptionalInt32("paid_storage_size_diff") ?? 0) + Protocol.OriginationSize) * Protocol.ByteCost,
                    0,
                    initiator);
        }

        protected virtual async Task ValidateTransaction(JsonElement content)
        {
            var source = content.RequiredString("source");

            if (!await Cache.Accounts.ExistsAsync(source))
                throw new ValidationException("unknown source account");

            var metadata = content.Required("metadata");

            ValidateFeeBalanceUpdates(
                ParseBalanceUpdates(metadata.RequiredArray("balance_updates")),
                source,
                content.RequiredInt64("fee"));

            var result = metadata.Required("operation_result");

            if (result.TryGetProperty("balance_updates", out var resultUpdates))
                ValidateTransferBalanceUpdates(
                    ParseBalanceUpdates(resultUpdates.RequiredArray()),
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
                        case "delegation": await ValidateInternalDelegation(internalContent, source); break;
                        case "origination": await ValidateInternalOrigination(internalContent, source); break;
                        case "transaction": await ValidateInternalTransaction(internalContent, source); break;
                        default:
                            throw new ValidationException("invalid internal operation kind");
                    }
                }
            }
        }

        protected virtual async Task ValidateInternalTransaction(JsonElement content, string initiator)
        {
            var source = content.RequiredString("source");
            var result = content.Required("result");

            if (!await Cache.Accounts.ExistsAsync(source))
                throw new ValidationException("unknown source account");

            if (result.TryGetProperty("balance_updates", out var resultUpdates))
                ValidateTransferBalanceUpdates(
                    ParseBalanceUpdates(resultUpdates.RequiredArray()),
                    source,
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
                ParseBalanceUpdates(content.Required("metadata").RequiredArray("balance_updates")),
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

        protected virtual long GetBlockDeposit()
        {
            // TODO: depend on ramp_up_cycles
            return Protocol.BlockDeposit;
        }

        protected virtual long GetBlockReward()
        {
            // TODO: depend on no_reward_cycles
            return Protocol.BlockReward0;
        }

        protected virtual long GetEndorsementDeposit(int slots)
        {
            // TODO: depend on ramp_up_cycles
            return slots * Protocol.EndorsementDeposit;
        }

        protected virtual long GetEndorsementReward(int slots)
        {
            // TODO: depend on no_reward_cycles
            LastBlock ??= Cache.Blocks.Current();
            return slots * (long)(Protocol.EndorsementReward0 / (LastBlock.Priority + 1.0));
        }

        protected virtual List<BalanceUpdate> ParseBalanceUpdates(JsonElement updates)
        {
            var res = new List<BalanceUpdate>(4);
            foreach (var update in updates.EnumerateArray())
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
                        Cycle = update.RequiredInt32("level"),
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
        }
    }
}
