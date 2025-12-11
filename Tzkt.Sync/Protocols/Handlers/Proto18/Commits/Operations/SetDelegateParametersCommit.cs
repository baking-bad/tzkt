using System.Numerics;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Netezos.Contracts;
using Netezos.Encoding;
using Tzkt.Data.Models;
using Tzkt.Data.Models.Base;

namespace Tzkt.Sync.Protocols.Proto18
{
    class SetDelegateParametersCommit(ProtocolHandler protocol) : ProtocolCommit(protocol)
    {
        #region static
        public static readonly string Entrypoint = "set_delegate_parameters";
        static readonly Schema Parameters = Schema.Create(new MichelinePrim
        {
            Prim = PrimType.pair,
            Args =
            [
                new MichelinePrim { Prim = PrimType.@int },
                new MichelinePrim { Prim = PrimType.@int },
                new MichelinePrim { Prim = PrimType.unit },
            ]
        });
        #endregion

        public async Task Apply(Block block, JsonElement op, JsonElement content)
        {
            #region init
            var sender = (await Cache.Accounts.GetExistingAsync(content.RequiredString("source")) as User)!;

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
                var param = Parameters.Optimize(content.Required("parameters").RequiredMicheline("value"));
                limit = ((param as MichelinePrim)!.Args![0] as MichelineInt)!.Value;
                edge = (((param as MichelinePrim)!.Args![1] as MichelinePrim)!.Args![0] as MichelineInt)!.Value;
            }
            catch when (status != OperationStatus.Applied) { }

            var operation = new SetDelegateParametersOperation
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
                ActivationCycle = block.Cycle + Context.Protocol.DelegateParametersActivationDelay + 1,
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
            PayFee(sender, operation.BakerFee);
            sender.Counter = operation.Counter;
            sender.SetDelegateParametersOpsCount++;

            block.Operations |= Operations.SetDelegateParameters;

            Cache.AppState.Get().SetDelegateParametersOpsCount++;
            #endregion

            #region apply result
            if (operation.Status == OperationStatus.Applied)
            {
                Cache.AppState.Get().PendingDelegateParameters++;
            }
            #endregion

            Proto.Manager.Set(sender);
            Db.SetDelegateParametersOps.Add(operation);
            Context.SetDelegateParametersOps.Add(operation);
        }

        public async Task Revert(Block block, SetDelegateParametersOperation operation)
        {
            var sender = (await Cache.Accounts.GetAsync(operation.SenderId) as User)!;
            Db.TryAttach(sender);

            #region revert result
            if (operation.Status == OperationStatus.Applied)
            {
                Cache.AppState.Get().PendingDelegateParameters--;
            }
            #endregion

            #region revert operation
            RevertPayFee(sender, operation.BakerFee);
            sender.Counter = operation.Counter - 1;
            sender.SetDelegateParametersOpsCount--;

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
                UpdateBakerPower(baker);
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
                UpdateBakerPower(baker);
                Cache.AppState.Get().PendingDelegateParameters++;
            }
        }
    }
}
