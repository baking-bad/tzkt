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
            var sender = Cache.Accounts.GetDelegate(content.RequiredString("source"));
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
                GasUsed = result.OptionalInt32("consumed_gas") ?? 0,
                StorageUsed = result.OptionalInt32("storage_size") ?? 0,
                Limit = limit == null ? null : BigInteger.Parse(limit)
            };
            #endregion

            #region entities
            var blockBaker = block.Proposer;
            Db.TryAttach(blockBaker);
            Db.TryAttach(sender);
            #endregion

            #region apply operation
            await Spend(sender, operation.BakerFee);
            sender.StakingBalance -= operation.BakerFee;
            
            blockBaker.Balance += operation.BakerFee;
            blockBaker.StakingBalance += operation.BakerFee;

            sender.SetDepositsLimitsCount++;
            sender.Counter = Math.Max(sender.Counter, operation.Counter);

            block.Operations |= Operations.SetDepositsLimits;
            block.Fees += operation.BakerFee;

            Cache.AppState.Get().SetDepositsLimitOpsCount++;
            #endregion

            #region apply result
            if (operation.Status == OperationStatus.Applied)
            {
                if (operation.Limit != null)
                {
                    sender.FrozenDepositLimit = operation.Limit > long.MaxValue
                        ? long.MaxValue
                        : (long)operation.Limit;
                }
                else
                {
                    sender.FrozenDepositLimit = null;
                }
            }
            #endregion

            Db.SetDepositsLimitOps.Add(operation);
        }

        public virtual async Task Revert(Block block, SetDepositsLimitOperation op)
        {
            #region init
            op.Block ??= block;
            op.Block.Proposer ??= Cache.Accounts.GetDelegate(block.ProposerId);
            op.Sender ??= await Cache.Accounts.GetAsync(op.SenderId);
            #endregion

            #region entities
            var blockBaker = block.Proposer;
            var sender = (Data.Models.Delegate)op.Sender;
            Db.TryAttach(blockBaker);
            Db.TryAttach(sender);
            #endregion

            #region revert result
            if (op.Status == OperationStatus.Applied)
            {
                var prevOp = await Db.SetDepositsLimitOps
                    .AsNoTracking()
                    .OrderByDescending(x => x.Id)
                    .FirstOrDefaultAsync(x => x.SenderId == op.SenderId && x.Id < op.Id);
                
                if (prevOp != null)
                {
                    sender.FrozenDepositLimit = prevOp.Limit > long.MaxValue
                        ? long.MaxValue
                        : (long)prevOp.Limit;
                }
                else
                {
                    sender.FrozenDepositLimit = null;
                }
            }
            #endregion

            #region revert operation
            await Return(sender, op.BakerFee);
            sender.StakingBalance += op.BakerFee;

            blockBaker.Balance -= op.BakerFee;
            blockBaker.StakingBalance -= op.BakerFee;

            sender.SetDepositsLimitsCount--;
            sender.Counter = Math.Min(sender.Counter, op.Counter - 1);

            Cache.AppState.Get().SetDepositsLimitOpsCount--;
            #endregion

            Db.SetDepositsLimitOps.Remove(op);
            Cache.AppState.ReleaseManagerCounter();
        }
    }
}
