﻿using System.Text.Json;
using Tzkt.Data.Models;
using Tzkt.Data.Models.Base;

namespace Tzkt.Sync.Protocols.Proto13
{
    class TxRollupReturnBondCommit(ProtocolHandler protocol) : ProtocolCommit(protocol)
    {
        public virtual async Task Apply(Block block, JsonElement op, JsonElement content)
        {
            #region init
            var sender = await Cache.Accounts.GetExistingAsync(content.RequiredString("source"));
            var rollup = await Cache.Accounts.GetAsync(content.RequiredString("rollup"));

            var result = content.Required("metadata").Required("operation_result");
            var bond = result.RequiredArray("balance_updates").EnumerateArray()
                .FirstOrDefault(x => x.RequiredString("kind") == "contract");

            var operation = new TxRollupReturnBondOperation
            {
                Id = Cache.AppState.NextOperationId(),
                Level = block.Level,
                Timestamp = block.Timestamp,
                OpHash = op.RequiredString("hash"),
                BakerFee = content.RequiredInt64("fee"),
                Counter = content.RequiredInt32("counter"),
                GasLimit = content.RequiredInt32("gas_limit"),
                StorageLimit = content.RequiredInt32("storage_limit"),
                SenderId = sender.Id,
                RollupId = rollup?.Id,
                Bond = bond.ValueKind == JsonValueKind.Undefined ? 0 : bond.RequiredInt64("change"),
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
            var blockBaker = Context.Proposer;
            var senderDelegate = Cache.Accounts.GetDelegate(sender.DelegateId) ?? sender as Data.Models.Delegate;

            Db.TryAttach(blockBaker);
            Db.TryAttach(sender);
            Db.TryAttach(senderDelegate);
            Db.TryAttach(rollup);
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

            sender.TxRollupReturnBondCount++;
            if (rollup != null) rollup.TxRollupReturnBondCount++;

            block.Operations |= Operations.TxRollupReturnBond;
            block.Fees += operation.BakerFee;

            sender.Counter = operation.Counter;

            Cache.AppState.Get().TxRollupReturnBondOpsCount++;
            #endregion

            #region apply result
            if (operation.Status == OperationStatus.Applied)
            {
                sender.RollupBonds -= operation.Bond;
                rollup!.RollupBonds -= operation.Bond;

                Cache.Statistics.Current.TotalRollupBonds -= operation.Bond;
            }
            #endregion

            Proto.Manager.Set(sender);
            Db.TxRollupReturnBondOps.Add(operation);
            Context.TxRollupReturnBondOps.Add(operation);
        }

        public virtual async Task Revert(Block block, TxRollupReturnBondOperation operation)
        {
            #region entities
            var blockBaker = Context.Proposer;
            var sender = await Cache.Accounts.GetAsync(operation.SenderId);
            var senderDelegate = Cache.Accounts.GetDelegate(sender.DelegateId) ?? sender as Data.Models.Delegate;
            var rollup = await Cache.Accounts.GetAsync(operation.RollupId);

            Db.TryAttach(blockBaker);
            Db.TryAttach(sender);
            Db.TryAttach(senderDelegate);
            Db.TryAttach(rollup);
            #endregion

            #region revert result
            if (operation.Status == OperationStatus.Applied)
            {
                sender.RollupBonds += operation.Bond;
                rollup!.RollupBonds += operation.Bond;
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

            sender.TxRollupReturnBondCount--;
            if (rollup != null) rollup.TxRollupReturnBondCount--;

            sender.Counter = operation.Counter - 1;
            (sender as User)!.Revealed = true;

            Cache.AppState.Get().TxRollupReturnBondOpsCount--;
            #endregion

            Db.TxRollupReturnBondOps.Remove(operation);
            Cache.AppState.ReleaseManagerCounter();
            Cache.AppState.ReleaseOperationId();
        }
    }
}
