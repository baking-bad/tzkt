using System.Numerics;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models;
using Tzkt.Data.Models.Base;

namespace Tzkt.Sync.Protocols.Proto12
{
    class SetDepositsLimitCommit(ProtocolHandler protocol) : ProtocolCommit(protocol)
    {
        public virtual async Task Apply(Block block, JsonElement op, JsonElement content)
        {
            #region init
            var sender = (User)await Cache.Accounts.GetExistingAsync(content.RequiredString("source"));

            var result = content.Required("metadata").Required("operation_result");
            var limit = content.OptionalString("limit");

            var operation = new SetDepositsLimitOperation
            {
                Id = Cache.AppState.NextOperationId(),
                OpHash = op.RequiredString("hash"),
                Level = block.Level,
                Timestamp = block.Timestamp,
                BakerFee = content.RequiredInt64("fee"),
                Counter = content.RequiredInt32("counter"),
                GasLimit = content.RequiredInt32("gas_limit"),
                StorageLimit = content.RequiredInt32("storage_limit"),
                SenderId = sender.Id,
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

            #region apply operation
            Db.TryAttach(sender);
            PayFee(sender, operation.BakerFee);
            sender.Counter = operation.Counter;
            sender.SetDepositsLimitsCount++;

            block.Operations |= Operations.SetDepositsLimits;

            Cache.AppState.Get().SetDepositsLimitOpsCount++;
            #endregion

            #region apply result
            if (operation.Status == OperationStatus.Applied)
            {
                if (operation.Limit != null)
                {
                    (sender as Data.Models.Delegate)!.FrozenDepositLimit = operation.Limit > long.MaxValue / 100
                        ? long.MaxValue / 100
                        : (long)operation.Limit;
                }
                else
                {
                    (sender as Data.Models.Delegate)!.FrozenDepositLimit = null;
                }
                UpdateBakerPower((sender as Data.Models.Delegate)!);
            }
            #endregion

            Proto.Manager.Set(sender);
            Db.SetDepositsLimitOps.Add(operation);
            Context.SetDepositsLimitOps.Add(operation);
        }

        public virtual async Task Revert(Block block, SetDepositsLimitOperation op)
        {
            #region entities
            var sender = (User)await Cache.Accounts.GetAsync(op.SenderId);

            Db.TryAttach(sender);
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
                    (sender as Data.Models.Delegate)!.FrozenDepositLimit = prevOp.Limit > long.MaxValue / 100
                        ? long.MaxValue / 100
                        : (long)prevOp.Limit;
                }
                else
                {
                    (sender as Data.Models.Delegate)!.FrozenDepositLimit = null;
                }
                RevertBakerPower((sender as Data.Models.Delegate)!);
            }
            #endregion

            #region revert operation
            RevertPayFee(sender, op.BakerFee);
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
