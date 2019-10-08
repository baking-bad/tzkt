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
        #region constants
        protected virtual int BlocksPerCycle => 4096;
        protected virtual int ByteCost => 1000;
        protected virtual int OriginationCost => 257_000;
        protected virtual int EndorsementDeposit => 0;
        protected virtual int EndorsementReward => 0;
        #endregion

        readonly AccountsCache Accounts;
        readonly StateCache State;

        public Validator(ProtocolHandler protocol)
        {
            Accounts = protocol.Accounts;
            State = protocol.State;
        }

        public async Task<IBlock> ValidateBlock(IBlock block)
        {
            if (!(block is Proto1.RawBlock rawBlock))
                throw new ValidationException("invalid raw block type");

            if (rawBlock.Level != (await State.GetCurrentBlock()).Level + 1)
                throw new ValidationException($"Invalid block level", true);

            if (rawBlock.Protocol != (await State.GetAppStateAsync()).NextProtocol)
                throw new ValidationException($"Invalid block protocol", true);

            if (!await Accounts.ExistsAsync(rawBlock.Metadata.Baker, AccountType.Delegate))
                throw new ValidationException($"Invalid block baker '{rawBlock.Metadata.Baker}'");

            foreach (var baker in rawBlock.Metadata.Deactivated)
            {
                if (!await Accounts.ExistsAsync(baker, AccountType.Delegate))
                    throw new ValidationException($"Invalid deactivated baker {baker}");
            }

            if (rawBlock.Metadata.BalanceUpdates.Count > 2)
            {
                if (rawBlock.Level % BlocksPerCycle != 0)
                    throw new ValidationException("Unexpected freezer updates");

                throw new NotImplementedException();
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
                            await ValidateNonceRevelation(revelation);
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
            if (await Accounts.ExistsAsync(activation.Address, AccountType.User))
                throw new ValidationException("account is already activated");

            if ((activation.Metadata.BalanceUpdates[0] as ContractUpdate)?.Contract != activation.Address)
                throw new ValidationException($"invalid activation balance updates");
        }

        protected async Task ValidateDelegation(RawDelegationContent delegation, RawBlock rawBlock)
        {
            if (!await Accounts.ExistsAsync(delegation.Source))
                throw new ValidationException("unknown source account");

            ValidateFeeBalanceUpdates(
                delegation.Metadata.BalanceUpdates,
                rawBlock.Metadata.Baker,
                delegation.Source,
                delegation.Fee,
                rawBlock.Metadata.LevelInfo.Cycle);

            if (delegation.Delegate != null)
            {
                if (delegation.Source != delegation.Delegate && !await Accounts.ExistsAsync(delegation.Delegate, AccountType.Delegate))
                    throw new ValidationException("unknown delegate account");

                var delegatAccount = await Accounts.GetAccountAsync(delegation.Delegate);
                if (delegation.Source == delegation.Delegate && delegatAccount is User)
                    throw new NotImplementedException();
            }
        }

        protected async Task ValidateEndorsement(RawEndorsementContent endorsement, RawBlock rawBlock)
        {
            var lastBlock = await State.GetCurrentBlock();

            if (endorsement.Level != lastBlock.Level)
                throw new ValidationException("invalid endorsed block level");

            if (!await Accounts.ExistsAsync(endorsement.Metadata.Delegate, AccountType.Delegate))
                throw new ValidationException("invalid endorsement delegate");

            if (endorsement.Metadata.BalanceUpdates.Count != 0 && endorsement.Metadata.BalanceUpdates.Count != 3)
                throw new ValidationException("invalid endorsement balance updates count");

            if (endorsement.Metadata.BalanceUpdates.Count > 0)
            {
                var contractUpdate = endorsement.Metadata.BalanceUpdates.FirstOrDefault(x => x is ContractUpdate) as ContractUpdate
                    ?? throw new ValidationException("invalid delegation fee balance updates");

                var depostisUpdate = endorsement.Metadata.BalanceUpdates.FirstOrDefault(x => x is DepositsUpdate) as DepositsUpdate
                    ?? throw new ValidationException("invalid delegation fee balance updates");

                var rewardsUpdate = endorsement.Metadata.BalanceUpdates.FirstOrDefault(x => x is FeesUpdate) as FeesUpdate
                    ?? throw new ValidationException("invalid delegation fee balance updates");

                if (contractUpdate.Contract != endorsement.Metadata.Delegate ||
                    contractUpdate.Change != endorsement.Metadata.Slots.Count * EndorsementDeposit)
                    throw new ValidationException("invalid endorsement contract update");

                if (depostisUpdate.Delegate != endorsement.Metadata.Delegate ||
                    depostisUpdate.Change != endorsement.Metadata.Slots.Count * EndorsementDeposit)
                    throw new ValidationException("invalid endorsement depostis update");

                if (rewardsUpdate.Delegate != endorsement.Metadata.Delegate ||
                    rewardsUpdate.Change != GetEndorsementReward(endorsement.Metadata.Slots.Count, lastBlock.Priority))
                    throw new ValidationException("invalid endorsement depostis update");
            }
        }

        protected Task ValidateNonceRevelation(RawNonceRevelationContent revelation)
        {
            throw new NotImplementedException();
        }

        protected Task ValidateOrigination(RawOriginationContent origination, RawBlock rawBlock)
        {
            throw new NotImplementedException();
        }

        protected async Task ValidateReveal(RawRevealContent reveal, RawBlock rawBlock)
        {
            if (!await Accounts.ExistsAsync(reveal.Source))
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
            if (!await Accounts.ExistsAsync(transaction.Source))
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
                    transaction.Metadata.Result.PaidStorageSizeDiff * ByteCost);

            if (transaction.Metadata.InternalResults?.Count > 0)
            {
                foreach (var internalContent in transaction.Metadata.InternalResults.Where(x => x is RawInternalTransactionResult))
                {
                    var internalTransaction = internalContent as RawInternalTransactionResult;

                    if (!await Accounts.ExistsAsync(internalTransaction.Source, AccountType.Contract))
                        throw new ValidationException("unknown source contract");

                    if (transaction.Metadata.Result.BalanceUpdates != null)
                        ValidateTransferBalanceUpdates(
                        internalTransaction.Result.BalanceUpdates,
                        internalTransaction.Source,
                        internalTransaction.Destination,
                        internalTransaction.Amount,
                        internalTransaction.Result.PaidStorageSizeDiff * ByteCost,
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
            => (long)Math.Round(slots * EndorsementReward / (priority + 1.0));
    }
}
