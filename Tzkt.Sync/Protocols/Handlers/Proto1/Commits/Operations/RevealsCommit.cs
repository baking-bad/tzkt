using System;
using System.Text.Json;
using System.Threading.Tasks;

using Tzkt.Data.Models;
using Tzkt.Data.Models.Base;

namespace Tzkt.Sync.Protocols.Proto1
{
    class RevealsCommit : ProtocolCommit
    {
        public RevealsCommit(ProtocolHandler protocol) : base(protocol) { }

        public virtual async Task Apply(Block block, JsonElement op, JsonElement content)
        {
            #region init
            var sender = await Cache.Accounts.GetAsync(content.RequiredString("source"));
            sender.Delegate ??= Cache.Accounts.GetDelegate(sender.DelegateId);

            var pubKey = content.RequiredString("public_key");
            var result = content.Required("metadata").Required("operation_result");
            var reveal = new RevealOperation
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
                GasUsed = GetConsumedGas(result)
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
            sender.Balance -= reveal.BakerFee;
            if (senderDelegate != null)
            {
                senderDelegate.StakingBalance -= reveal.BakerFee;
                if (senderDelegate.Id != sender.Id)
                    senderDelegate.DelegatedBalance -= reveal.BakerFee;
            }
            blockBaker.Balance += reveal.BakerFee;
            blockBaker.StakingBalance += reveal.BakerFee;

            sender.RevealsCount++;

            block.Operations |= Operations.Reveals;
            block.Fees += reveal.BakerFee;

            sender.Counter = reveal.Counter;
            #endregion

            #region apply result
            ApplyResult(reveal, pubKey);
            #endregion

            Proto.Manager.Set(reveal.Sender);
            Db.RevealOps.Add(reveal);
        }

        public virtual async Task Revert(Block block, RevealOperation reveal)
        {
            #region init
            reveal.Block ??= block;
            reveal.Block.Proposer ??= Cache.Accounts.GetDelegate(block.ProposerId);

            reveal.Sender ??= await Cache.Accounts.GetAsync(reveal.SenderId);
            reveal.Sender.Delegate ??= Cache.Accounts.GetDelegate(reveal.Sender.DelegateId);
            #endregion

            #region entities
            var blockBaker = block.Proposer;
            var sender = reveal.Sender;
            var senderDelegate = sender.Delegate ?? sender as Data.Models.Delegate;

            Db.TryAttach(blockBaker);
            Db.TryAttach(sender);
            Db.TryAttach(senderDelegate);
            #endregion

            #region revert result
            RevertResult(reveal);
            #endregion

            #region revert operation
            sender.Balance += reveal.BakerFee;
            if (senderDelegate != null)
            {
                senderDelegate.StakingBalance += reveal.BakerFee;
                if (senderDelegate.Id != sender.Id)
                    senderDelegate.DelegatedBalance += reveal.BakerFee;
            }
            blockBaker.Balance -= reveal.BakerFee;
            blockBaker.StakingBalance -= reveal.BakerFee;

            sender.RevealsCount--;

            sender.Counter = reveal.Counter - 1;
            #endregion

            Db.RevealOps.Remove(reveal);
            Cache.AppState.ReleaseManagerCounter();
            Cache.AppState.ReleaseOperationId();
        }

        protected virtual int GetConsumedGas(JsonElement result)
        {
            return result.OptionalInt32("consumed_gas") ?? 0;
        }

        protected virtual void ApplyResult(RevealOperation op, string pubKey)
        {
            if (op.Sender is User user)
            {
                user.PublicKey = pubKey;
                if (user.Balance > 0) user.Revealed = true;
            }
        }

        protected virtual void RevertResult(RevealOperation op)
        {
            if (op.Sender is User user)
            {
                if (user.RevealsCount == 1)
                    user.PublicKey = null;

                user.Revealed = false;
            }
        }
    }
}
