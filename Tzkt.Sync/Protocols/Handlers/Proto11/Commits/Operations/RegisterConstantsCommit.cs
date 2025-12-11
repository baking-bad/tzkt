using System.Text.Json;
using Netezos.Encoding;
using Tzkt.Data.Models;
using Tzkt.Data.Models.Base;

namespace Tzkt.Sync.Protocols.Proto11
{
    class RegisterConstantsCommit(ProtocolHandler protocol) : ProtocolCommit(protocol)
    {
        public virtual async Task Apply(Block block, JsonElement op, JsonElement content)
        {
            #region init
            var sender = (User)await Cache.Accounts.GetExistingAsync(content.RequiredString("source"));

            var result = content.Required("metadata").Required("operation_result");
            var registerConstant = new RegisterConstantOperation
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
                GasUsed = GetConsumedGas(result),
                StorageUsed = result.OptionalInt32("storage_size") ?? 0,
                StorageFee = result.OptionalInt32("storage_size") > 0
                    ? result.OptionalInt32("storage_size") * Context.Protocol.ByteCost
                    : null,
            };
            #endregion

            #region apply operation
            Db.TryAttach(sender);
            PayFee(sender, registerConstant.BakerFee);
            sender.Counter = registerConstant.Counter;
            sender.RegisterConstantsCount++;

            block.Operations |= Operations.RegisterConstant;

            Cache.AppState.Get().RegisterConstantOpsCount++;
            #endregion

            #region apply result
            if (registerConstant.Status == OperationStatus.Applied)
            {
                var burned = registerConstant.StorageFee ?? 0;
                Proto.Manager.Burn(burned);

                Spend(sender, burned);

                registerConstant.Address = result.RequiredString("global_address");
                registerConstant.Value = content.RequiredMicheline("value").ToBytes();
                registerConstant.Refs = 0;

                Cache.AppState.Get().ConstantsCount++;

                Cache.Statistics.Current.TotalBurned += burned;
            }
            #endregion

            Proto.Manager.Set(sender);
            Db.RegisterConstantOps.Add(registerConstant);
            Context.RegisterConstantOps.Add(registerConstant);
        }

        public virtual async Task Revert(Block block, RegisterConstantOperation registerConstant)
        {
            #region entities
            var sender = (User)await Cache.Accounts.GetAsync(registerConstant.SenderId);

            Db.TryAttach(sender);
            #endregion

            #region revert result
            if (registerConstant.Status == OperationStatus.Applied)
            {
                RevertSpend(sender, registerConstant.StorageFee ?? 0);

                Cache.AppState.Get().ConstantsCount--;
            }
            #endregion

            #region revert operation
            RevertPayFee(sender, registerConstant.BakerFee);
            sender.Counter = registerConstant.Counter - 1;
            sender.RegisterConstantsCount--;
            sender.Revealed = true;

            Cache.AppState.Get().RegisterConstantOpsCount--;
            #endregion

            Db.RegisterConstantOps.Remove(registerConstant);
            Cache.AppState.ReleaseManagerCounter();
            Cache.AppState.ReleaseOperationId();
        }

        protected virtual int GetConsumedGas(JsonElement result)
        {
            return result.OptionalInt32("consumed_gas") ?? 0;
        }
    }
}
