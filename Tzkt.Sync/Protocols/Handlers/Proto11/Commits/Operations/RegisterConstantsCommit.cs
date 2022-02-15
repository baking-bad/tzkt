using Netezos.Encoding;
using System;
using System.Text.Json;
using System.Threading.Tasks;
using Tzkt.Data.Models;
using Tzkt.Data.Models.Base;

namespace Tzkt.Sync.Protocols.Proto11
{
    class RegisterConstantsCommit : ProtocolCommit
    {
        public RegisterConstantsCommit(ProtocolHandler protocol) : base(protocol) { }

        public virtual async Task Apply(Block block, JsonElement op, JsonElement content)
        {
            #region init
            var sender = (User)await Cache.Accounts.GetAsync(content.RequiredString("source"));
            sender.Delegate ??= Cache.Accounts.GetDelegate(sender.DelegateId);

            var result = content.Required("metadata").Required("operation_result");
            var registerConstant = new RegisterConstantOperation
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
                StorageFee = result.OptionalInt32("storage_size") > 0
                    ? result.OptionalInt32("storage_size") * block.Protocol.ByteCost
                    : null,
            };
            #endregion

            #region entities
            var blockBaker = block.Baker;
            var senderDelegate = sender.Delegate ?? sender as Data.Models.Delegate;

            Db.TryAttach(blockBaker);
            Db.TryAttach(sender);
            Db.TryAttach(senderDelegate);
            #endregion

            #region apply operation
            await Spend(sender, registerConstant.BakerFee);
            if (senderDelegate != null)
            {
                senderDelegate.StakingBalance -= registerConstant.BakerFee;
                if (senderDelegate.Id != sender.Id)
                    senderDelegate.DelegatedBalance -= registerConstant.BakerFee;
            }
            blockBaker.Balance += registerConstant.BakerFee;
            blockBaker.StakingBalance += registerConstant.BakerFee;

            sender.RegisterConstantsCount++;

            block.Operations |= Operations.RegisterConstant;
            block.Fees += registerConstant.BakerFee;

            sender.Counter = Math.Max(sender.Counter, registerConstant.Counter);
            #endregion

            #region apply result
            if (registerConstant.Status == OperationStatus.Applied)
            {
                await Spend(sender, registerConstant.StorageFee ?? 0);
                if (senderDelegate != null)
                {
                    senderDelegate.StakingBalance -= registerConstant.StorageFee ?? 0;
                    if (senderDelegate.Id != sender.Id)
                        senderDelegate.DelegatedBalance -= registerConstant.StorageFee ?? 0;
                }

                registerConstant.Address = result.RequiredString("global_address");
                registerConstant.Value = Micheline.FromJson(content.Required("value")).ToBytes();
                registerConstant.Refs = 0;

                Cache.AppState.Get().ConstantsCount++;
            }
            #endregion

            Db.RegisterConstantOps.Add(registerConstant);
        }

        public virtual async Task Revert(Block block, RegisterConstantOperation registerConstant)
        {
            #region init
            registerConstant.Block ??= block;
            registerConstant.Block.Baker ??= Cache.Accounts.GetDelegate(block.BakerId);

            registerConstant.Sender ??= await Cache.Accounts.GetAsync(registerConstant.SenderId);
            registerConstant.Sender.Delegate ??= Cache.Accounts.GetDelegate(registerConstant.Sender.DelegateId);
            #endregion

            #region entities
            var blockBaker = block.Baker;
            var sender = (User)registerConstant.Sender;
            var senderDelegate = sender.Delegate ?? sender as Data.Models.Delegate;

            Db.TryAttach(blockBaker);
            Db.TryAttach(sender);
            Db.TryAttach(senderDelegate);
            #endregion

            #region revert result
            if (registerConstant.Status == OperationStatus.Applied)
            {
                await Return(sender, registerConstant.StorageFee ?? 0);
                if (senderDelegate != null)
                {
                    senderDelegate.StakingBalance += registerConstant.StorageFee ?? 0;
                    if (senderDelegate.Id != sender.Id)
                        senderDelegate.DelegatedBalance += registerConstant.StorageFee ?? 0;
                }

                Cache.AppState.Get().ConstantsCount--;
            }
            #endregion

            #region revert operation
            await Return(sender, registerConstant.BakerFee, true);
            if (senderDelegate != null)
            {
                senderDelegate.StakingBalance += registerConstant.BakerFee;
                if (senderDelegate.Id != sender.Id)
                    senderDelegate.DelegatedBalance += registerConstant.BakerFee;
            }
            blockBaker.Balance -= registerConstant.BakerFee;
            blockBaker.StakingBalance -= registerConstant.BakerFee;

            sender.RegisterConstantsCount--;

            sender.Counter = Math.Min(sender.Counter, registerConstant.Counter - 1);
            #endregion

            Db.RegisterConstantOps.Remove(registerConstant);
            Cache.AppState.ReleaseManagerCounter();
        }
    }
}
