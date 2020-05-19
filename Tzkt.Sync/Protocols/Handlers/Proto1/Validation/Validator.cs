using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Tzkt.Data.Models;
using Tzkt.Sync.Services;

namespace Tzkt.Sync.Protocols.Proto1
{
    class Validator : IValidator
    {
        readonly CacheService Cache;
        Protocol Protocol;
        int Cycle;

        public Validator(ProtocolHandler protocol)
        {
            Cache = protocol.Cache;
        }

        public async Task<IBlock> ValidateBlock(IBlock block)
        {
            Protocol = await Cache.Protocols.GetAsync(block.Protocol);
            Cycle = (block.Level - 1) / Protocol.BlocksPerCycle;

            if (!(block is Proto1.RawBlock rawBlock))
                throw new ValidationException("invalid raw block type");

            if (rawBlock.Level != Cache.AppState.GetNextLevel())
                throw new ValidationException($"invalid block level", true);

            if (rawBlock.Predecessor != Cache.AppState.GetHead())
                throw new ValidationException($"Invalid block predecessor", true);

            if (rawBlock.Protocol != Cache.AppState.GetNextProtocol())
                throw new ValidationException($"invalid block protocol", true);

            if (!Cache.Accounts.DelegateExists(rawBlock.Metadata.Baker))
                throw new ValidationException($"invalid block baker '{rawBlock.Metadata.Baker}'");

            foreach (var baker in rawBlock.Metadata.Deactivated)
            {
                if (!Cache.Accounts.DelegateExists(baker))
                    throw new ValidationException($"invalid deactivated baker {baker}");
            }

            if (rawBlock.Metadata.BalanceUpdates.Count > 0)
            {
                var contractUpdate = rawBlock.Metadata.BalanceUpdates.FirstOrDefault(x => x is ContractUpdate) as ContractUpdate
                    ?? throw new ValidationException("invalid block contract balance updates");

                var depostisUpdate = rawBlock.Metadata.BalanceUpdates.FirstOrDefault(x => x is DepositsUpdate) as DepositsUpdate
                    ?? throw new ValidationException("invalid block depostis balance updates");

                if (contractUpdate.Contract != rawBlock.Metadata.Baker ||
                    contractUpdate.Change != -Protocol.BlockDeposit)
                    throw new ValidationException("invalid block contract update");

                if (depostisUpdate.Delegate != rawBlock.Metadata.Baker ||
                    depostisUpdate.Change != Protocol.BlockDeposit)
                    throw new ValidationException("invalid block depostis update");

                if (Cycle >= (Protocol.PreservedCycles + 2))
                {
                    var rewardsUpdate = rawBlock.Metadata.BalanceUpdates.FirstOrDefault(x => x is RewardsUpdate) as RewardsUpdate
                        ?? throw new ValidationException("invalid block rewards updates");

                    if (rewardsUpdate.Delegate != rawBlock.Metadata.Baker ||
                        rewardsUpdate.Change != Protocol.BlockReward0)
                        throw new ValidationException("invalid block rewards update");
                }
            }

            if (rawBlock.Metadata.BalanceUpdates.Count > (Protocol.BlockReward0 > 0 ? 3 : 2))
            {
                if (rawBlock.Level % Protocol.BlocksPerCycle != 0)
                    throw new ValidationException("unexpected freezer updates");

                foreach (var update in rawBlock.Metadata.BalanceUpdates.Skip(Protocol.BlockReward0 > 0 ? 3 : 2))
                {
                    if (update is ContractUpdate contractUpdate &&
                        !Cache.Accounts.DelegateExists(contractUpdate.Contract))
                        throw new ValidationException($"unknown delegate {contractUpdate.Contract}");

                    if (update is FreezerUpdate freezerUpdate)
                    {
                        if (!Cache.Accounts.DelegateExists(freezerUpdate.Delegate))
                            throw new ValidationException($"unknown delegate {freezerUpdate.Delegate}");

                        if (freezerUpdate.Level != Cycle - Protocol.PreservedCycles && freezerUpdate.Level != Cycle - 1)
                            throw new ValidationException("invalid freezer updates cycle");
                    }
                }
            }

            foreach (var opGroup in rawBlock.Operations)
                foreach (var op in opGroup)
                {
                    if (String.IsNullOrEmpty(op.Hash))
                        throw new ValidationException("invalid operation hash");

                    foreach (var content in op.Contents)
                    {
                        if (content is RawEndorsementContent endorsement)
                            await ValidateEndorsement(endorsement, rawBlock);
                        else if (content is RawTransactionContent transaction)
                            await ValidateTransaction(transaction, rawBlock);
                        else if (content is RawNonceRevelationContent revelation)
                            await ValidateNonceRevelation(revelation, rawBlock);
                        else if (content is RawOriginationContent origination)
                            await ValidateOrigination(origination, rawBlock);
                        else if (content is RawDelegationContent delegation)
                            await ValidateDelegation(delegation, rawBlock);
                        else if (content is RawActivationContent activation)
                            await ValidateActivation(activation);
                        else if (content is RawRevealContent reveal)
                            await ValidateReveal(reveal, rawBlock);
                    }
                }

            return block;
        }

        protected async Task ValidateActivation(RawActivationContent activation)
        {
            if (await Cache.Accounts.ExistsAsync(activation.Address, AccountType.User) &&
                ((await Cache.Accounts.GetAsync(activation.Address)) as User).Activated == true)
                throw new ValidationException("account is already activated");

            if ((activation.Metadata.BalanceUpdates[0] as ContractUpdate)?.Contract != activation.Address)
                throw new ValidationException($"invalid activation balance updates");
        }

        protected async Task ValidateDelegation(RawDelegationContent delegation, RawBlock rawBlock)
        {
            if (!await Cache.Accounts.ExistsAsync(delegation.Source))
                throw new ValidationException("unknown source account");

            ValidateFeeBalanceUpdates(
                delegation.Metadata.BalanceUpdates,
                rawBlock.Metadata.Baker,
                delegation.Source,
                delegation.Fee,
                rawBlock.Metadata.LevelInfo.Cycle);

            if (delegation.Metadata.Result.Status == "applied" && delegation.Delegate != null)
            {
                if (delegation.Source != delegation.Delegate && !Cache.Accounts.DelegateExists(delegation.Delegate))
                    throw new ValidationException("unknown delegate account");
            }
        }

        protected async Task ValidateEndorsement(RawEndorsementContent endorsement, RawBlock rawBlock)
        {
            var lastBlock = await Cache.Blocks.CurrentAsync();

            if (endorsement.Level != lastBlock.Level)
                throw new ValidationException("invalid endorsed block level");

            if (!Cache.Accounts.DelegateExists(endorsement.Metadata.Delegate))
                throw new ValidationException("invalid endorsement delegate");

            if (endorsement.Metadata.BalanceUpdates.Count != 0 && endorsement.Metadata.BalanceUpdates.Count != (Protocol.BlockReward0 > 0 ? 3 : 2))
                throw new ValidationException("invalid endorsement balance updates count");

            if (endorsement.Metadata.BalanceUpdates.Count > 0)
            {
                var contractUpdate = endorsement.Metadata.BalanceUpdates.FirstOrDefault(x => x is ContractUpdate) as ContractUpdate
                    ?? throw new ValidationException("invalid endorsement contract balance updates");

                var depostisUpdate = endorsement.Metadata.BalanceUpdates.FirstOrDefault(x => x is DepositsUpdate) as DepositsUpdate
                    ?? throw new ValidationException("invalid endorsement depostis balance updates");

                if (contractUpdate.Contract != endorsement.Metadata.Delegate ||
                    contractUpdate.Change != -endorsement.Metadata.Slots.Count * Protocol.EndorsementDeposit)
                    throw new ValidationException("invalid endorsement contract update");

                if (depostisUpdate.Delegate != endorsement.Metadata.Delegate ||
                    depostisUpdate.Change != endorsement.Metadata.Slots.Count * Protocol.EndorsementDeposit)
                    throw new ValidationException("invalid endorsement depostis update");

                if (Cycle >= (Protocol.PreservedCycles + 2))
                {
                    var rewardsUpdate = endorsement.Metadata.BalanceUpdates.FirstOrDefault(x => x is RewardsUpdate) as RewardsUpdate
                        ?? throw new ValidationException("invalidendorsement rewards updates");

                    if (rewardsUpdate.Delegate != endorsement.Metadata.Delegate ||
                        rewardsUpdate.Change != GetEndorsementReward(endorsement.Metadata.Slots.Count, lastBlock.Priority))
                        throw new ValidationException("invalid endorsement rewards update");
                }
            }
        }

        protected Task ValidateNonceRevelation(RawNonceRevelationContent revelation, RawBlock rawBlock)
        {
            if (revelation.Level % Protocol.BlocksPerCommitment != 0)
                throw new ValidationException("invalid seed nonce revelation level");

            if (revelation.Metadata.BalanceUpdates.Count != 1)
                throw new ValidationException("invalid seed nonce revelation balance updates count");

            if (!(revelation.Metadata.BalanceUpdates[0] is RewardsUpdate))
                throw new ValidationException("invalid seed nonce revelation balance update type");

            if (revelation.Metadata.BalanceUpdates[0].Change != Protocol.RevelationReward)
                throw new ValidationException("invalid seed nonce revelation balance update amount");

            if (!Cache.Accounts.DelegateExists(revelation.Metadata.BalanceUpdates[0].Target) ||
                revelation.Metadata.BalanceUpdates[0].Target != rawBlock.Metadata.Baker)
                throw new ValidationException("invalid seed nonce revelation baker");

            return Task.CompletedTask;
        }

        protected async Task ValidateOrigination(RawOriginationContent origination, RawBlock rawBlock)
        {
            if (!await Cache.Accounts.ExistsAsync(origination.Source))
                throw new ValidationException("unknown source account");

            ValidateFeeBalanceUpdates(
                origination.Metadata.BalanceUpdates,
                rawBlock.Metadata.Baker,
                origination.Source,
                origination.Fee,
                rawBlock.Metadata.LevelInfo.Cycle);

            if (origination.Metadata.Result.BalanceUpdates != null)
                ValidateTransferBalanceUpdates(
                    origination.Metadata.Result.BalanceUpdates,
                    origination.Source,
                    origination.Metadata.Result.OriginatedContracts[0],
                    origination.Balance,
                    (origination.Metadata.Result.PaidStorageSizeDiff + Protocol.OriginationSize) * Protocol.ByteCost);
        }

        protected async Task ValidateReveal(RawRevealContent reveal, RawBlock rawBlock)
        {
            if (!await Cache.Accounts.ExistsAsync(reveal.Source))
                throw new ValidationException("unknown source account");

            ValidateFeeBalanceUpdates(
                reveal.Metadata.BalanceUpdates,
                rawBlock.Metadata.Baker,
                reveal.Source,
                reveal.Fee,
                rawBlock.Metadata.LevelInfo.Cycle);
        }

        protected async Task ValidateTransaction(RawTransactionContent transaction, RawBlock rawBlock)
        {
            if (!await Cache.Accounts.ExistsAsync(transaction.Source))
                throw new ValidationException("unknown source account");

            ValidateFeeBalanceUpdates(
                transaction.Metadata.BalanceUpdates,
                rawBlock.Metadata.Baker,
                transaction.Source,
                transaction.Fee,
                rawBlock.Metadata.LevelInfo.Cycle);

            if (transaction.Metadata.Result.BalanceUpdates != null)
                ValidateTransferBalanceUpdates(
                    transaction.Metadata.Result.BalanceUpdates,
                    transaction.Source,
                    transaction.Destination,
                    transaction.Amount,
                    transaction.Metadata.Result.PaidStorageSizeDiff * Protocol.ByteCost);

            if (transaction.Metadata.InternalResults?.Count > 0)
            {
                foreach (var internalContent in transaction.Metadata.InternalResults.Where(x => x is RawInternalTransactionResult))
                {
                    var internalTransaction = internalContent as RawInternalTransactionResult;

                    if (!await Cache.Accounts.ExistsAsync(internalTransaction.Source, AccountType.Contract))
                        throw new ValidationException("unknown source contract");

                    if (internalTransaction.Result.BalanceUpdates != null)
                        ValidateTransferBalanceUpdates(
                            internalTransaction.Result.BalanceUpdates,
                            internalTransaction.Source,
                            internalTransaction.Destination,
                            internalTransaction.Amount,
                            internalTransaction.Result.PaidStorageSizeDiff * Protocol.ByteCost,
                            transaction.Source);
                }
            }
        }

        void ValidateFeeBalanceUpdates(List<IBalanceUpdate> updates, string baker, string sender, long fee, int cycle)
        {
            if (updates.Count != (fee != 0 ? 2 : 0))
                throw new ValidationException($"invalid fee balance updates count");

            if (updates.Count > 0)
            {
                if (!updates.Any(x => x is ContractUpdate update && update.Change == -fee && update.Contract == sender))
                    throw new ValidationException("invalid fee balance updates");

                if (!updates.Any(x => x is FeesUpdate update && update.Change == fee && update.Delegate == baker && update.Level == cycle))
                    throw new ValidationException("invalid fee balance updates");
            }
        }

        void ValidateTransferBalanceUpdates(List<IBalanceUpdate> updates, string sender, string receiver, long amount, long storageFee, string parent = null)
        {
            if (updates.Count != (amount != 0 ? 2 : 0) + (storageFee != 0 ? 1 : 0))
                throw new ValidationException($"invalid transfer balance updates count");

            if (amount > 0)
            {
                if (!updates.Any(x => x is ContractUpdate update && update.Change == -amount && update.Contract == sender))
                    throw new ValidationException("invalid transfer balance updates");

                if (!updates.Any(x => x is ContractUpdate update && update.Change == amount && update.Contract == receiver))
                    throw new ValidationException("invalid transfer balance updates");
            }

            if (storageFee > 0)
            {
                if (!updates.Any(x => x is ContractUpdate update && update.Change == -storageFee && update.Contract == (parent ?? sender)))
                    throw new ValidationException("invalid transfer balance updates");
            }
        }

        long GetEndorsementReward(int slots, int priority)
            => slots * (long)(Protocol.EndorsementReward0 / (priority + 1.0));
    }
}
