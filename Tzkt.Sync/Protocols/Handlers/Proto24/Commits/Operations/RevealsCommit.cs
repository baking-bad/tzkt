using System.Text.Json;
using Tzkt.Data.Models;
using Tzkt.Data.Models.Base;

namespace Tzkt.Sync.Protocols.Proto24
{
    class RevealsCommit(ProtocolHandler protocol) : ProtocolCommit(protocol)
    {
        public virtual async Task Apply(Block block, JsonElement op, JsonElement content)
        {
            #region init
            var sender = await Cache.Accounts.GetExistingAsync(content.RequiredString("source"));

            var pubKey = content.RequiredString("public_key");
            var metadata = content.Required("metadata");
            var result = metadata.Required("operation_result");

            var refund = metadata
                .OptionalArray("balance_updates")?
                .EnumerateArray()
                .FirstOrDefault(x =>
                    x.RequiredString("kind") == "accumulator" &&
                    x.RequiredString("category") == "block fees" &&
                    x.RequiredInt64("change") < 0)
                ?? default;

            var bakerFee = content.RequiredInt64("fee")
                + (refund.ValueKind != JsonValueKind.Undefined ? refund.RequiredInt64("change") : 0);

            var reveal = new RevealOperation
            {
                Id = Cache.AppState.NextOperationId(),
                OpHash = op.RequiredString("hash"),
                Level = block.Level,
                Timestamp = block.Timestamp,
                BakerFee = bakerFee,
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
                GasUsed = (int)(((result.OptionalInt64("consumed_milligas") ?? 0) + 999) / 1000)
            };
            #endregion

            #region apply operation
            Db.TryAttach(sender);
            PayFee(sender, reveal.BakerFee);
            sender.Counter = reveal.Counter;
            sender.RevealsCount++;

            block.Operations |= Operations.Reveals;

            Cache.AppState.Get().RevealOpsCount++;
            #endregion

            #region apply result
            if (reveal.Status == OperationStatus.Applied)
            {
                if (sender is User user)
                {
                    user.PublicKey = pubKey;
                    if (user.Balance > 0) user.Revealed = true;
                }
            }
            #endregion

            Proto.Manager.Set(sender);
            Db.RevealOps.Add(reveal);
            Context.RevealOps.Add(reveal);
        }

        public virtual async Task Revert(Block block, RevealOperation reveal)
        {
            #region entities
            var sender = await Cache.Accounts.GetAsync(reveal.SenderId);

            Db.TryAttach(sender);
            #endregion

            #region revert result
            if (reveal.Status == OperationStatus.Applied)
            {
                if (sender is User user)
                {
                    if (user.RevealsCount == 1)
                        user.PublicKey = null;

                    user.Revealed = false;
                }
            }
            #endregion

            #region revert operation
            RevertPayFee(sender, reveal.BakerFee);
            sender.Counter = reveal.Counter - 1;
            sender.RevealsCount--;

            Cache.AppState.Get().RevealOpsCount--;
            #endregion

            Db.RevealOps.Remove(reveal);
            Cache.AppState.ReleaseManagerCounter();
            Cache.AppState.ReleaseOperationId();
        }
    }
}
