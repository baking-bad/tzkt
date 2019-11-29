using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Tzkt.Data.Models;
using Tzkt.Sync.Services;

namespace Tzkt.Sync.Protocols.Proto2
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
            Protocol = await Cache.GetProtocolAsync(block.Protocol);
            Cycle = (block.Level - 1) / Protocol.BlocksPerCycle;

            if (!(block is RawBlock rawBlock))
                throw new ValidationException("invalid raw block type");

            if (rawBlock.Level != (await Cache.GetCurrentBlockAsync()).Level + 1)
                throw new ValidationException($"invalid block level", true);

            if (rawBlock.Predecessor != (await Cache.GetCurrentBlockAsync()).Hash)
                throw new ValidationException($"Invalid block predecessor", true);

            if (rawBlock.Protocol != (await Cache.GetAppStateAsync()).NextProtocol)
                throw new ValidationException($"invalid block protocol", true);

            if (!await Cache.AccountExistsAsync(rawBlock.Metadata.Baker, AccountType.Delegate))
                throw new ValidationException($"invalid block baker '{rawBlock.Metadata.Baker}'");

            var period = await Cache.GetCurrentVotingPeriodAsync();
            var kind = rawBlock.Metadata.VotingPeriod switch
            {
                "proposal" => VotingPeriods.Proposal,
                "exploration" => VotingPeriods.Exploration,
                "testing" => VotingPeriods.Testing,
                "promotion" => VotingPeriods.Promotion,
                _ => throw new ValidationException("invalid voting period kind")
            };

            if (block.Level <= period.EndLevel)
            {
                if (period.Kind != kind)
                    throw new ValidationException("unexpected voting period");
            }
            else
            {
                if ((int)kind != (int)period.Kind + 1 && kind != VotingPeriods.Proposal)
                    throw new ValidationException("inconsistent voting period");
            }

            foreach (var baker in rawBlock.Metadata.Deactivated)
            {
                if (!await Cache.AccountExistsAsync(baker, AccountType.Delegate))
                    throw new ValidationException($"invalid deactivated baker {baker}");
            }

            if (rawBlock.Metadata.BalanceUpdates.Count > 0)
            {
                var blockUpdates = rawBlock.Metadata.BalanceUpdates.Take(Cycle < 7 ? 2 : 3);

                var contractUpdate = blockUpdates.FirstOrDefault(x => x is ContractUpdate) as ContractUpdate
                    ?? throw new ValidationException("invalid block contract balance updates");

                var depostisUpdate = blockUpdates.FirstOrDefault(x => x is DepositsUpdate) as DepositsUpdate
                    ?? throw new ValidationException("invalid block depostis balance updates");

                if (contractUpdate.Contract != rawBlock.Metadata.Baker ||
                    contractUpdate.Change != -Protocol.BlockDeposit)
                    throw new ValidationException("invalid block contract update");

                if (depostisUpdate.Delegate != rawBlock.Metadata.Baker ||
                    depostisUpdate.Change != Protocol.BlockDeposit)
                    throw new ValidationException("invalid block depostis update");

                if (Cycle >= 7)
                {
                    var rewardsUpdate = blockUpdates.FirstOrDefault(x => x is RewardsUpdate) as RewardsUpdate
                        ?? throw new ValidationException("invalid block rewards updates");

                    if (rewardsUpdate.Delegate != rawBlock.Metadata.Baker ||
                        rewardsUpdate.Change != Protocol.BlockReward)
                        throw new ValidationException("invalid block rewards update");
                }
            }

            if (rawBlock.Metadata.BalanceUpdates.Count > (Cycle < 7 ? 2 : 3))
            {
                if (rawBlock.Level % Protocol.BlocksPerCycle != 0)
                    throw new ValidationException("unexpected freezer updates");

                foreach (var update in rawBlock.Metadata.BalanceUpdates.Skip(Cycle < 7 ? 2 : 3))
                {
                    if (update is ContractUpdate contractUpdate &&
                        !await Cache.AccountExistsAsync(contractUpdate.Contract, AccountType.Delegate))
                        throw new ValidationException($"unknown delegate {contractUpdate.Contract}");

                    if (update is FreezerUpdate freezerUpdate)
                    {
                        if (!await Cache.AccountExistsAsync(freezerUpdate.Delegate, AccountType.Delegate))
                            throw new ValidationException($"unknown delegate {freezerUpdate.Delegate}");

                        if (freezerUpdate.Level != Cycle - 5 && freezerUpdate.Level != Cycle - 1)
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
                        else if (content is RawDoubleBakingEvidenceContent db)
                            await ValidateDoubleBaking(db, rawBlock);
                    }
                }

            return block;
        }

        protected async Task ValidateActivation(RawActivationContent activation)
        {
            if (await Cache.AccountExistsAsync(activation.Address, AccountType.User) &&
                ((await Cache.GetAccountAsync(activation.Address)) as User).Activated == true)
                throw new ValidationException("account is already activated");

            if ((activation.Metadata.BalanceUpdates[0] as ContractUpdate)?.Contract != activation.Address)
                throw new ValidationException($"invalid activation balance updates");
        }

        protected async Task ValidateDelegation(RawDelegationContent delegation, RawBlock rawBlock)
        {
            if (!await Cache.AccountExistsAsync(delegation.Source))
                throw new ValidationException("unknown source account");

            ValidateFeeBalanceUpdates(
                delegation.Metadata.BalanceUpdates,
                rawBlock.Metadata.Baker,
                delegation.Source,
                delegation.Fee,
                rawBlock.Metadata.LevelInfo.Cycle);

            if (delegation.Metadata.Result.Status == "applied" && delegation.Delegate != null)
            {
                if (delegation.Source != delegation.Delegate && !await Cache.AccountExistsAsync(delegation.Delegate, AccountType.Delegate))
                    throw new ValidationException("unknown delegate account");
            }
        }

        protected async Task ValidateEndorsement(RawEndorsementContent endorsement, RawBlock rawBlock)
        {
            var lastBlock = await Cache.GetCurrentBlockAsync();

            if (endorsement.Level != lastBlock.Level)
                throw new ValidationException("invalid endorsed block level");

            if (!await Cache.AccountExistsAsync(endorsement.Metadata.Delegate, AccountType.Delegate))
                throw new ValidationException("invalid endorsement delegate");

            if (endorsement.Metadata.BalanceUpdates.Count != 0 && endorsement.Metadata.BalanceUpdates.Count != (Cycle < 7 ? 2 : 3))
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

                if (Cycle >= 7)
                {
                    var rewardsUpdate = endorsement.Metadata.BalanceUpdates.FirstOrDefault(x => x is RewardsUpdate) as RewardsUpdate
                        ?? throw new ValidationException("invalidendorsement rewards updates");

                    if (rewardsUpdate.Delegate != endorsement.Metadata.Delegate ||
                        rewardsUpdate.Change != GetEndorsementReward(endorsement.Metadata.Slots.Count, lastBlock.Priority))
                        throw new ValidationException("invalid endorsement rewards update");
                }
            }
        }

        protected async Task ValidateNonceRevelation(RawNonceRevelationContent revelation, RawBlock rawBlock)
        {
            if (revelation.Level % Protocol.BlocksPerCommitment != 0)
                throw new ValidationException("invalid seed nonce revelation level");

            if (revelation.Metadata.BalanceUpdates.Count != 1)
                throw new ValidationException("invalid seed nonce revelation balance updates count");

            if (!(revelation.Metadata.BalanceUpdates[0] is RewardsUpdate))
                throw new ValidationException("invalid seed nonce revelation balance update type");

            if (revelation.Metadata.BalanceUpdates[0].Change != Protocol.RevelationReward)
                throw new ValidationException("invalid seed nonce revelation balance update amount");

            if (!await Cache.AccountExistsAsync(revelation.Metadata.BalanceUpdates[0].Target, AccountType.Delegate) ||
                revelation.Metadata.BalanceUpdates[0].Target != rawBlock.Metadata.Baker)
                throw new ValidationException("invalid seed nonce revelation baker");
        }

        protected async Task ValidateOrigination(RawOriginationContent origination, RawBlock rawBlock)
        {
            if (!await Cache.AccountExistsAsync(origination.Source))
                throw new ValidationException("unknown source account");

            if (!await Cache.AccountExistsAsync(origination.Manager))
                throw new ValidationException("unknown manager account");

            if (origination.Metadata.Result.Status == "applied" && origination.Delegate != null)
            {
                if (!await Cache.AccountExistsAsync(origination.Delegate, AccountType.Delegate))
                    throw new ValidationException("unknown delegate");
            }

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
            if (!await Cache.AccountExistsAsync(reveal.Source))
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
            if (!await Cache.AccountExistsAsync(transaction.Source))
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

                    if (!await Cache.AccountExistsAsync(internalTransaction.Source, AccountType.Contract))
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

        protected async Task ValidateDoubleBaking(RawDoubleBakingEvidenceContent db, RawBlock rawBlock)
        {
            if (db.Block1.Level != db.Block2.Level)
                throw new ValidationException("inconsistent double baking levels");

            var rewardUpdate = db.Metadata.BalanceUpdates.FirstOrDefault(x => x.Change > 0) as RewardsUpdate
                ?? throw new ValidationException("double baking reward is missed");

            if (rewardUpdate.Delegate != rawBlock.Metadata.Baker)
                throw new ValidationException("invalid double baking reward recipient");

            var lostDepositsUpdate = db.Metadata.BalanceUpdates.FirstOrDefault(x => x is DepositsUpdate &&  x.Change < 0) as DepositsUpdate;
            var lostRewardsUpdate = db.Metadata.BalanceUpdates.FirstOrDefault(x => x is RewardsUpdate &&  x.Change < 0) as RewardsUpdate;
            var lostFeesUpdate = db.Metadata.BalanceUpdates.FirstOrDefault(x => x is FeesUpdate &&  x.Change < 0) as FeesUpdate;

            var offender = lostDepositsUpdate?.Delegate ?? lostRewardsUpdate?.Delegate ?? lostFeesUpdate?.Delegate;
            if (!await Cache.AccountExistsAsync(offender, AccountType.Delegate))
                throw new ValidationException("invalid double baking offender");

            if ((lostDepositsUpdate?.Delegate ?? offender) != offender ||
                (lostRewardsUpdate?.Delegate ?? offender) != offender ||
                (lostFeesUpdate?.Delegate ?? offender) != offender)
                throw new ValidationException("invalid double baking offender updates");

            if (rewardUpdate.Change != -((lostDepositsUpdate?.Change ?? 0) + (lostFeesUpdate?.Change ?? 0)) / 2)
                throw new ValidationException("invalid double baking reward amount");

            var accusedCycle = (db.Block1.Level - 1) / Protocol.BlocksPerCycle;
            if ((lostDepositsUpdate?.Level ?? accusedCycle) != accusedCycle ||
                (lostRewardsUpdate?.Level ?? accusedCycle) != accusedCycle ||
                (lostFeesUpdate?.Level ?? accusedCycle) != accusedCycle)
                throw new ValidationException("invalid double baking freezer level");
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
            => slots * (long)(Protocol.EndorsementReward / (priority + 1.0));
    }
}
