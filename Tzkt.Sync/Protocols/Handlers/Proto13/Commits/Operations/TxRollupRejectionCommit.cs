using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

using Tzkt.Data.Models;
using Tzkt.Data.Models.Base;

namespace Tzkt.Sync.Protocols.Proto13
{
    class TxRollupRejectionCommit : ProtocolCommit
    {
        public TxRollupRejectionOperation Operation { get; private set; }

        public TxRollupRejectionCommit(ProtocolHandler protocol) : base(protocol) { }

        public virtual async Task Apply(Block block, JsonElement op, JsonElement content)
        {
            #region init
            var sender = await Cache.Accounts.GetAsync(content.RequiredString("source"));
            var rollup = await Cache.Accounts.GetAsync(content.RequiredString("rollup"));

            var result = content.Required("metadata").Required("operation_result");
            var updates = result.RequiredArray("balance_updates").EnumerateArray();
            var reward = updates.FirstOrDefault(x => x.RequiredString("kind") == "minted");
            var loss = updates.FirstOrDefault(x => x.RequiredString("kind") == "burned");
            var freezer = updates.FirstOrDefault(x => x.RequiredString("kind") == "freezer");
            var committer = freezer.ValueKind == JsonValueKind.Undefined
                ? sender // if there is no balance update, we don't know who is the committer
                : await Cache.Accounts.GetAsync(freezer.RequiredString("contract"));

            var operation = new TxRollupRejectionOperation
            {
                Id = Cache.AppState.NextOperationId(),
                Block = block,
                Level = block.Level,
                Timestamp = block.Timestamp,
                OpHash = op.RequiredString("hash"),
                BakerFee = content.RequiredInt64("fee"),
                Counter = content.RequiredInt32("counter"),
                GasLimit = content.RequiredInt32("gas_limit"),
                StorageLimit = content.RequiredInt32("storage_limit"),
                Sender = sender,
                RollupId = rollup?.Id,
                CommitterId = committer.Id,
                Reward = reward.ValueKind == JsonValueKind.Undefined ? 0 : -reward.RequiredInt64("change"),
                Loss = loss.ValueKind == JsonValueKind.Undefined ? 0 : loss.RequiredInt64("change"),
                Status = result.RequiredString("status") switch
                {
                    "applied" => OperationStatus.Applied,
                    "backtracked" => OperationStatus.Backtracked,
                    "failed" => OperationStatus.Failed,
                    "skipped" => OperationStatus.Skipped,
                    _ => throw new NotImplementedException()
                },
                Errors = result.TryGetProperty("errors", out var errors)
                    ? OperationErrors.Parse(content, errors)
                    : null,
                GasUsed = (int)(((result.OptionalInt64("consumed_milligas") ?? 0) + 999) / 1000)
            };
            #endregion

            #region entities
            var blockBaker = block.Proposer;
            var senderDelegate = Cache.Accounts.GetDelegate(sender.DelegateId) ?? sender as Data.Models.Delegate;
            var committerDelegate = Cache.Accounts.GetDelegate(committer.DelegateId) ?? committer as Data.Models.Delegate;

            Db.TryAttach(blockBaker);
            Db.TryAttach(sender);
            Db.TryAttach(senderDelegate);
            Db.TryAttach(rollup);
            Db.TryAttach(committer);
            Db.TryAttach(committerDelegate);
            #endregion

            #region apply operation
            sender.Balance -= operation.BakerFee;
            if (senderDelegate != null)
            {
                senderDelegate.StakingBalance -= operation.BakerFee;
                if (senderDelegate.Id != sender.Id)
                    senderDelegate.DelegatedBalance -= operation.BakerFee;
            }
            blockBaker.Balance += operation.BakerFee;
            blockBaker.StakingBalance += operation.BakerFee;

            sender.TxRollupRejectionCount++;
            if (rollup != null) rollup.TxRollupRejectionCount++;
            if (sender.Id != committer.Id) committer.TxRollupRejectionCount++;

            block.Operations |= Operations.TxRollupRejection;
            block.Fees += operation.BakerFee;

            sender.Counter = operation.Counter;

            Cache.AppState.Get().TxRollupRejectionOpsCount++;
            #endregion

            #region apply result
            if (operation.Status == OperationStatus.Applied)
            {
                sender.Balance += operation.Reward;
                if (senderDelegate != null)
                {
                    senderDelegate.StakingBalance += operation.Reward;
                    if (senderDelegate.Id != sender.Id)
                        senderDelegate.DelegatedBalance += operation.Reward;
                }

                committer.Balance -= operation.Loss;
                if (committerDelegate != null)
                {
                    committerDelegate.StakingBalance -= operation.Loss;
                    if (committerDelegate.Id != committer.Id)
                        committerDelegate.DelegatedBalance -= operation.Loss;
                }

                if (sender.Id != committer.Id)
                {
                    Proto.Manager.Credit(operation.Reward);

                    if (committer.Balance == 0 && committer is User user && user.Revealed)
                    {
                        user.Counter = Cache.AppState.GetManagerCounter();
                        user.Revealed = false;
                    }
                }

                committer.RollupBonds -= operation.Loss;
                rollup.RollupBonds -= operation.Loss;
            }
            #endregion

            Proto.Manager.Set(operation.Sender);
            Db.TxRollupRejectionOps.Add(operation);
            Operation = operation;
        }

        public virtual async Task Revert(Block block, TxRollupRejectionOperation operation)
        {
            #region init
            operation.Block ??= block;
            operation.Block.Protocol ??= await Cache.Protocols.GetAsync(block.ProtoCode);
            operation.Block.Proposer ??= Cache.Accounts.GetDelegate(block.ProposerId);

            operation.Sender = await Cache.Accounts.GetAsync(operation.SenderId);
            operation.Sender.Delegate ??= Cache.Accounts.GetDelegate(operation.Sender.DelegateId);
            #endregion

            #region entities
            var blockBaker = block.Proposer;
            var sender = operation.Sender;
            var senderDelegate = sender.Delegate ?? sender as Data.Models.Delegate;
            var rollup = await Cache.Accounts.GetAsync(operation.RollupId);
            var committer = await Cache.Accounts.GetAsync(operation.CommitterId);
            var committerDelegate = committer.Delegate ?? committer as Data.Models.Delegate;

            Db.TryAttach(blockBaker);
            Db.TryAttach(sender);
            Db.TryAttach(senderDelegate);
            Db.TryAttach(rollup);
            Db.TryAttach(committer);
            Db.TryAttach(committerDelegate);
            #endregion

            #region revert result
            if (operation.Status == OperationStatus.Applied)
            {
                sender.Balance -= operation.Reward;
                if (senderDelegate != null)
                {
                    senderDelegate.StakingBalance -= operation.Reward;
                    if (senderDelegate.Id != sender.Id)
                        senderDelegate.DelegatedBalance -= operation.Reward;
                }

                committer.Balance += operation.Loss;
                if (committerDelegate != null)
                {
                    committerDelegate.StakingBalance += operation.Loss;
                    if (committerDelegate.Id != committer.Id)
                        committerDelegate.DelegatedBalance += operation.Loss;
                }

                if (sender.Id != committer.Id)
                {
                    if (committer.Balance == operation.Loss && committer is User user && !user.Revealed)
                    {
                        user.Counter = await RestoreCounter(user, operation.Id);
                        user.Revealed = true;
                    }
                }

                committer.RollupBonds += operation.Loss;
                rollup.RollupBonds += operation.Loss;
            }
            #endregion

            #region revert operation
            sender.Balance += operation.BakerFee;
            if (senderDelegate != null)
            {
                senderDelegate.StakingBalance += operation.BakerFee;
                if (senderDelegate.Id != sender.Id)
                    senderDelegate.DelegatedBalance += operation.BakerFee;
            }
            blockBaker.Balance -= operation.BakerFee;
            blockBaker.StakingBalance -= operation.BakerFee;

            sender.TxRollupRejectionCount--;
            if (rollup != null) rollup.TxRollupRejectionCount--;
            if (committer.Id != sender.Id) committer.TxRollupRejectionCount--;

            sender.Counter = operation.Counter - 1;
            (sender as User).Revealed = true;

            Cache.AppState.Get().TxRollupRejectionOpsCount--;
            #endregion

            Db.TxRollupRejectionOps.Remove(operation);
            Cache.AppState.ReleaseManagerCounter();
            Cache.AppState.ReleaseOperationId();
        }

        async Task<int> RestoreCounter(User user, long opId)
        {
            var counter = 0;

            if (user.DelegationsCount > 0)
            {
                var opCounter = await Db.DelegationOps
                    .AsNoTracking()
                    .Where(x => x.SenderId == user.Id && x.Id < opId)
                    .OrderByDescending(x => x.Id)
                    .Select(x => x.Counter)
                    .FirstAsync();
                counter = Math.Max(counter, opCounter);
            }

            if (user.OriginationsCount > 0)
            {
                var opCounter = await Db.OriginationOps
                    .AsNoTracking()
                    .Where(x => x.SenderId == user.Id && x.Id < opId)
                    .OrderByDescending(x => x.Id)
                    .Select(x => x.Counter)
                    .FirstAsync();
                counter = Math.Max(counter, opCounter);
            }

            if (user.TransactionsCount > 0)
            {
                var opCounter = await Db.TransactionOps
                    .AsNoTracking()
                    .Where(x => x.SenderId == user.Id && x.Id < opId)
                    .OrderByDescending(x => x.Id)
                    .Select(x => x.Counter)
                    .FirstAsync();
                counter = Math.Max(counter, opCounter);
            }

            if (user.RevealsCount > 0)
            {
                var opCounter = await Db.RevealOps
                    .AsNoTracking()
                    .Where(x => x.SenderId == user.Id && x.Id < opId)
                    .OrderByDescending(x => x.Id)
                    .Select(x => x.Counter)
                    .FirstAsync();
                counter = Math.Max(counter, opCounter);
            }

            if (user.RegisterConstantsCount > 0)
            {
                var opCounter = await Db.RegisterConstantOps
                    .AsNoTracking()
                    .Where(x => x.SenderId == user.Id && x.Id < opId)
                    .OrderByDescending(x => x.Id)
                    .Select(x => x.Counter)
                    .FirstAsync();
                counter = Math.Max(counter, opCounter);
            }

            if (user.SetDepositsLimitsCount > 0)
            {
                var opCounter = await Db.SetDepositsLimitOps
                    .AsNoTracking()
                    .Where(x => x.SenderId == user.Id && x.Id < opId)
                    .OrderByDescending(x => x.Id)
                    .Select(x => x.Counter)
                    .FirstAsync();
                counter = Math.Max(counter, opCounter);
            }

            if (user.TransferTicketCount > 0)
            {
                var opCounter = await Db.TransferTicketOps
                    .AsNoTracking()
                    .Where(x => x.SenderId == user.Id && x.Id < opId)
                    .OrderByDescending(x => x.Id)
                    .Select(x => x.Counter)
                    .FirstAsync();
                counter = Math.Max(counter, opCounter);
            }

            if (user.TxRollupCommitCount > 0)
            {
                var opCounter = await Db.TxRollupCommitOps
                    .AsNoTracking()
                    .Where(x => x.SenderId == user.Id && x.Id < opId)
                    .OrderByDescending(x => x.Id)
                    .Select(x => x.Counter)
                    .FirstAsync();
                counter = Math.Max(counter, opCounter);
            }

            if (user.TxRollupDispatchTicketsCount > 0)
            {
                var opCounter = await Db.TxRollupDispatchTicketsOps
                    .AsNoTracking()
                    .Where(x => x.SenderId == user.Id && x.Id < opId)
                    .OrderByDescending(x => x.Id)
                    .Select(x => x.Counter)
                    .FirstAsync();
                counter = Math.Max(counter, opCounter);
            }

            if (user.TxRollupFinalizeCommitmentCount > 0)
            {
                var opCounter = await Db.TxRollupFinalizeCommitmentOps
                    .AsNoTracking()
                    .Where(x => x.SenderId == user.Id && x.Id < opId)
                    .OrderByDescending(x => x.Id)
                    .Select(x => x.Counter)
                    .FirstAsync();
                counter = Math.Max(counter, opCounter);
            }

            if (user.TxRollupOriginationCount > 0)
            {
                var opCounter = await Db.TxRollupOriginationOps
                    .AsNoTracking()
                    .Where(x => x.SenderId == user.Id && x.Id < opId)
                    .OrderByDescending(x => x.Id)
                    .Select(x => x.Counter)
                    .FirstAsync();
                counter = Math.Max(counter, opCounter);
            }

            if (user.TxRollupRejectionCount > 0)
            {
                var opCounter = await Db.TxRollupRejectionOps
                    .AsNoTracking()
                    .Where(x => x.SenderId == user.Id && x.Id < opId)
                    .OrderByDescending(x => x.Id)
                    .Select(x => x.Counter)
                    .FirstAsync();
                counter = Math.Max(counter, opCounter);
            }

            if (user.TxRollupRemoveCommitmentCount > 0)
            {
                var opCounter = await Db.TxRollupRemoveCommitmentOps
                    .AsNoTracking()
                    .Where(x => x.SenderId == user.Id && x.Id < opId)
                    .OrderByDescending(x => x.Id)
                    .Select(x => x.Counter)
                    .FirstAsync();
                counter = Math.Max(counter, opCounter);
            }

            if (user.TxRollupReturnBondCount > 0)
            {
                var opCounter = await Db.TxRollupReturnBondOps
                    .AsNoTracking()
                    .Where(x => x.SenderId == user.Id && x.Id < opId)
                    .OrderByDescending(x => x.Id)
                    .Select(x => x.Counter)
                    .FirstAsync();
                counter = Math.Max(counter, opCounter);
            }

            if (user.TxRollupSubmitBatchCount > 0)
            {
                var opCounter = await Db.TxRollupSubmitBatchOps
                    .AsNoTracking()
                    .Where(x => x.SenderId == user.Id && x.Id < opId)
                    .OrderByDescending(x => x.Id)
                    .Select(x => x.Counter)
                    .FirstAsync();
                counter = Math.Max(counter, opCounter);
            }

            return counter;
        }
    }
}
