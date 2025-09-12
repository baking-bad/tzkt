using System.Numerics;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Netmavryk.Contracts;
using Netmavryk.Encoding;
using Mvkt.Data.Models;
using Mvkt.Data.Models.Base;

namespace Mvkt.Sync.Protocols.Proto18
{
    class SetDelegateParametersCommit : ProtocolCommit
    {
        #region static
        public static readonly string Entrypoint = "set_delegate_parameters";
        static readonly Schema Parameters = Schema.Create(Micheline.FromJson("""
            {
                "prim": "pair",
                "args": [
                    {
                        "prim": "int"
                    },
                    {
                        "prim": "int"
                    },
                    {
                        "prim": "unit"
                    }
                ]
            }
            """) as MichelinePrim);
        #endregion

        public SetDelegateParametersCommit(ProtocolHandler protocol) : base(protocol) { }

        public async Task Apply(Block block, JsonElement op, JsonElement content)
        {
            #region init
            var sender = await Cache.Accounts.GetAsync(content.RequiredString("source")) as User;
            var senderDelegate = sender as Data.Models.Delegate ?? Cache.Accounts.GetDelegate(sender.DelegateId);

            var result = content.Required("metadata").Required("operation_result");
            var status = result.RequiredString("status") switch
            {
                "applied" => OperationStatus.Applied,
                "backtracked" => OperationStatus.Backtracked,
                "failed" => OperationStatus.Failed,
                "skipped" => OperationStatus.Skipped,
                _ => throw new NotImplementedException()
            };

            var limit = BigInteger.Zero;
            var edge = BigInteger.Zero;
            try
            {
                var param = Parameters.Optimize(Micheline.FromJson(content.Required("parameters").Required("value")));
                limit = ((param as MichelinePrim).Args[0] as MichelineInt).Value;
                edge = (((param as MichelinePrim).Args[1] as MichelinePrim).Args[0] as MichelineInt).Value;
            }
            catch when (status != OperationStatus.Applied) { }

            var operation = new SetDelegateParametersOperation
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
                SenderId = sender.Id,
                ActivationCycle = block.Cycle + block.Protocol.DelegateParametersActivationDelay + 1,
                LimitOfStakingOverBaking = limit.TrimToInt64(),
                EdgeOfBakingOverStaking = (long)edge,
                Status = status,
                Errors = result.TryGetProperty("errors", out var errors)
                    ? OperationErrors.Parse(content, errors)
                    : null,
                GasUsed = (int)(((result.OptionalInt64("consumed_milligas") ?? 0) + 999) / 1000),
                AllocationFee = null,
                StorageFee = null,
                StorageUsed = 0
            };
            #endregion

            #region apply operation
            Db.TryAttach(sender);
            sender.Balance -= operation.BakerFee;
            sender.Counter = operation.Counter;
            sender.SetDelegateParametersOpsCount++;

            if (senderDelegate != null)
            {
                Db.TryAttach(senderDelegate);
                senderDelegate.StakingBalance -= operation.BakerFee;
                if (senderDelegate != sender)
                {
                    senderDelegate.DelegatedBalance -= operation.BakerFee;
                    senderDelegate.SetDelegateParametersOpsCount++;
                }
            }

            block.Proposer.Balance += operation.BakerFee;
            block.Proposer.StakingBalance += operation.BakerFee;

            block.Operations |= Operations.SetDelegateParameters;
            block.Fees += operation.BakerFee;

            Cache.AppState.Get().SetDelegateParametersOpsCount++;
            #endregion

            #region apply result
            if (operation.Status == OperationStatus.Applied)
            {
                Cache.AppState.Get().PendingDelegateParameters++;
            }
            #endregion

            Proto.Manager.Set(operation.Sender);
            Db.SetDelegateParametersOps.Add(operation);
        }

        public async Task Revert(Block block, SetDelegateParametersOperation operation)
        {
            var sender = await Cache.Accounts.GetAsync(operation.SenderId) as User;
            var senderDelegate = sender as Data.Models.Delegate ?? Cache.Accounts.GetDelegate(sender.DelegateId);

            Db.TryAttach(sender);
            Db.TryAttach(senderDelegate);

            #region revert result
            if (operation.Status == OperationStatus.Applied)
            {
                Cache.AppState.Get().PendingDelegateParameters--;
            }
            #endregion

            #region revert operation
            sender.Balance += operation.BakerFee;
            sender.Counter = operation.Counter - 1;
            sender.SetDelegateParametersOpsCount--;

            if (senderDelegate != null)
            {
                senderDelegate.StakingBalance += operation.BakerFee;
                if (senderDelegate != sender)
                {
                    senderDelegate.DelegatedBalance += operation.BakerFee;
                    senderDelegate.SetDelegateParametersOpsCount--;
                }
            }

            block.Proposer.Balance -= operation.BakerFee;
            block.Proposer.StakingBalance -= operation.BakerFee;

            Cache.AppState.Get().SetDelegateParametersOpsCount--;
            #endregion

            Db.SetDelegateParametersOps.Remove(operation);
            Cache.AppState.ReleaseManagerCounter();
            Cache.AppState.ReleaseOperationId();
        }

        public async Task ActivateStakingParameters(Block block)
        {
            if (!block.Events.HasFlag(BlockEvents.CycleBegin) || Cache.AppState.Get().PendingDelegateParameters == 0)
                return;

            var ops = await Db.SetDelegateParametersOps
                .AsNoTracking()
                .Where(x => x.ActivationCycle == block.Cycle && x.Status == OperationStatus.Applied)
                .ToListAsync();

            foreach (var op in ops)
            {
                var baker = Cache.Accounts.GetDelegate(op.SenderId);
                Db.TryAttach(baker);
                baker.EdgeOfBakingOverStaking = op.EdgeOfBakingOverStaking;
                baker.LimitOfStakingOverBaking = op.LimitOfStakingOverBaking;
                Cache.AppState.Get().PendingDelegateParameters--;
            }
        }

        public async Task DeactivateStakingParameters(Block block)
        {
            if (!block.Events.HasFlag(BlockEvents.CycleBegin))
                return;

            var ops = await Db.SetDelegateParametersOps
                .AsNoTracking()
                .Where(x => x.ActivationCycle == block.Cycle && x.Status == OperationStatus.Applied)
                .ToListAsync();

            foreach (var op in ops)
            {
                var baker = Cache.Accounts.GetDelegate(op.SenderId);

                var prevOp = await Db.SetDelegateParametersOps
                    .AsNoTracking()
                    .Where(x =>
                        x.SenderId == baker.Id &&
                        x.ActivationCycle < op.ActivationCycle &&
                        x.Status == OperationStatus.Applied)
                    .OrderByDescending(x => x.Id)
                    .FirstOrDefaultAsync();

                Db.TryAttach(baker);
                baker.EdgeOfBakingOverStaking = prevOp?.EdgeOfBakingOverStaking;
                baker.LimitOfStakingOverBaking = prevOp?.LimitOfStakingOverBaking;
                Cache.AppState.Get().PendingDelegateParameters++;
            }
        }
    }
}
