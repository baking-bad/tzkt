using System;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models;
using Tzkt.Data.Models.Base;

namespace Tzkt.Sync.Protocols.Proto12
{
    class SetDepositsLimitCommit : ProtocolCommit
    {
        public SetDepositsLimitCommit(ProtocolHandler protocol) : base(protocol) { }

        public virtual async Task Apply(Block block, JsonElement op, JsonElement content)
        {
            #region init
            var sender = (User)await Cache.Accounts.GetAsync(content.RequiredString("source"));
            sender.Delegate ??= Cache.Accounts.GetDelegate(sender.DelegateId);

            var result = content.Required("metadata").Required("operation_result");
            var limit = content.OptionalString("limit");

            var operation = new SetDepositsLimitOperation
            {
                Id = Cache.AppState.NextOperationId(),
                OpHash = op.RequiredString("hash"),
                Block = block,
                Level = block.Level,
                Timestamp = block.Timestamp,
                BakerFee = content.RequiredInt64("fee"),
                Counter = content.RequiredInt32("counter"),
                GasLimit = content.RequiredInt32("gas_limit"),
                StorageLimit = content.RequiredInt32("storage_limit"),
                Sender = sender,
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
                GasUsed = (int)(((result.OptionalInt64("consumed_milligas") ?? 0) + 999) / 1000),
                Limit = limit == null ? null : BigInteger.Parse(limit)
            };
            #endregion

            #region entities
            var blockBaker = block.Proposer;
            var senderDelegate = sender.Delegate ?? sender as Data.Models.Delegate;

            Db.TryAttach(blockBaker);
            Db.TryAttach(sender);
            Db.TryAttach(senderDelegate);
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

            sender.SetDepositsLimitsCount++;
            sender.Counter = operation.Counter;

            block.Operations |= Operations.SetDepositsLimits;
            block.Fees += operation.BakerFee;

            Cache.AppState.Get().SetDepositsLimitOpsCount++;
            #endregion

            #region apply result
            if (operation.Status == OperationStatus.Applied)
            {
                if (operation.Limit != null)
                {
                    (sender as Data.Models.Delegate).FrozenDepositLimit = operation.Limit > long.MaxValue / 100
                        ? long.MaxValue / 100
                        : (long)operation.Limit;
                }
                else
                {
                    (sender as Data.Models.Delegate).FrozenDepositLimit = null;
                }
            }
            #endregion

            Proto.Manager.Set(operation.Sender);
            Db.SetDepositsLimitOps.Add(operation);
        }

        public virtual async Task Revert(Block block, SetDepositsLimitOperation op)
        {
            #region init
            op.Block ??= block;
            op.Block.Proposer ??= Cache.Accounts.GetDelegate(block.ProposerId);

            op.Sender ??= await Cache.Accounts.GetAsync(op.SenderId);
            op.Sender.Delegate ??= Cache.Accounts.GetDelegate(op.Sender.DelegateId);
            #endregion

            #region entities
            var blockBaker = block.Proposer;
            var sender = (User)op.Sender;
            var senderDelegate = sender.Delegate ?? sender as Data.Models.Delegate;

            Db.TryAttach(blockBaker);
            Db.TryAttach(sender);
            Db.TryAttach(senderDelegate);
            #endregion

            #region revert result
            if (op.Status == OperationStatus.Applied)
            {
                var prevOp = await Db.SetDepositsLimitOps
                    .AsNoTracking()
                    .OrderByDescending(x => x.Id)
                    .FirstOrDefaultAsync(x => x.SenderId == op.SenderId && x.Status == OperationStatus.Applied && x.Id < op.Id);
                
                if (prevOp?.Limit != null)
                {
                    (sender as Data.Models.Delegate).FrozenDepositLimit = prevOp.Limit > long.MaxValue / 100
                        ? long.MaxValue / 100
                        : (long)prevOp.Limit;
                }
                else
                {
                    (sender as Data.Models.Delegate).FrozenDepositLimit = null;
                }
            }
            #endregion

            #region revert operation
            sender.Balance += op.BakerFee;
            if (senderDelegate != null)
            {
                senderDelegate.StakingBalance += op.BakerFee;
                if (senderDelegate.Id != sender.Id)
                    senderDelegate.DelegatedBalance += op.BakerFee;
            }

            blockBaker.Balance -= op.BakerFee;
            blockBaker.StakingBalance -= op.BakerFee;

            sender.SetDepositsLimitsCount--;
            sender.Counter = op.Counter - 1;
            sender.Revealed = true;

            Cache.AppState.Get().SetDepositsLimitOpsCount--;
            #endregion

            Db.SetDepositsLimitOps.Remove(op);
            Cache.AppState.ReleaseManagerCounter();
            Cache.AppState.ReleaseOperationId();
        }
    }
}
