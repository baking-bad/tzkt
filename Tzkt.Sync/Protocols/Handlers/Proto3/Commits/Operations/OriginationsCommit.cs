using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

using Tzkt.Data.Models;
using Tzkt.Data.Models.Base;

namespace Tzkt.Sync.Protocols.Proto3
{
    class OriginationsCommit : ProtocolCommit
    {
        public OriginationOperation Origination { get; private set; }

        OriginationsCommit(ProtocolHandler protocol) : base(protocol) { }

        public async Task Init(Block block, RawOperation op, RawOriginationContent content)
        {
            var sender = await Cache.Accounts.GetAsync(content.Source);
            sender.Delegate ??= Cache.Accounts.GetDelegate(sender.DelegateId);

            var manager = (User)await Cache.Accounts.GetAsync(content.Manager);
            var delegat = Cache.Accounts.GetDelegateOrDefault(content.Delegate);

            var contract = content.Metadata.Result.Status == "applied" ?
                new Contract
                {
                    Id = Cache.AppState.NextAccountId(),
                    Address = content.Metadata.Result.OriginatedContracts[0],
                    Balance = content.Balance,
                    Counter = 0,
                    Delegate = delegat,
                    DelegationLevel = delegat != null ? (int?)block.Level : null,
                    Creator = sender,
                    Manager = manager,
                    Staked = delegat?.Staked ?? false,
                    Type = AccountType.Contract,
                    Kind = content.Script == null ? ContractKind.DelegatorContract : ContractKind.SmartContract,
                    Spendable = content.Spendable == false ? content.Spendable : null
                }
                : null;

            Origination = new OriginationOperation
            {
                Id = Cache.AppState.NextOperationId(),
                Block = block,
                Level = block.Level,
                Timestamp = block.Timestamp,
                OpHash = op.Hash,
                Balance = content.Balance,
                BakerFee = content.Fee,
                Counter = content.Counter,
                GasLimit = content.GasLimit,
                StorageLimit = content.StorageLimit,
                Sender = sender,
                Manager = manager,
                Delegate = delegat,
                Contract = contract,
                Status = content.Metadata.Result.Status switch
                {
                    "applied" => OperationStatus.Applied,
                    "backtracked" => OperationStatus.Backtracked,
                    "failed" => OperationStatus.Failed,
                    "skipped" => OperationStatus.Skipped,
                    _ => throw new NotImplementedException()
                },
                Errors = OperationErrors.Parse(content.Metadata.Result.Errors),
                GasUsed = content.Metadata.Result.ConsumedGas,
                StorageUsed = content.Metadata.Result.PaidStorageSizeDiff,
                StorageFee = content.Metadata.Result.PaidStorageSizeDiff * block.Protocol.ByteCost,
                AllocationFee = block.Protocol.OriginationSize * block.Protocol.ByteCost
            };
        }

        public async Task Init(Block block, OriginationOperation origination)
        {
            Origination = origination;

            Origination.Block ??= block;
            Origination.Block.Protocol ??= await Cache.Protocols.GetAsync(block.ProtoCode);
            Origination.Block.Baker ??= Cache.Accounts.GetDelegate(block.BakerId);
            
            Origination.Sender = await Cache.Accounts.GetAsync(origination.SenderId);
            Origination.Sender.Delegate ??= Cache.Accounts.GetDelegate(origination.Sender.DelegateId);
            Origination.Contract ??= (Contract)await Cache.Accounts.GetAsync(origination.ContractId);
            Origination.Delegate ??= Cache.Accounts.GetDelegate(origination.DelegateId);
            Origination.Manager ??= (User)await Cache.Accounts.GetAsync(origination.ManagerId);
        }

        public override async Task Apply()
        {
            #region entities
            var block = Origination.Block;
            var blockBaker = block.Baker;

            var sender = Origination.Sender;
            var senderDelegate = sender.Delegate ?? sender as Data.Models.Delegate;

            var contract = Origination.Contract;
            var contractDelegate = Origination.Delegate;
            var contractManager = Origination.Manager;

            //Db.TryAttach(block);
            Db.TryAttach(blockBaker);

            Db.TryAttach(sender);
            Db.TryAttach(senderDelegate);

            Db.TryAttach(contract);
            Db.TryAttach(contractDelegate);
            Db.TryAttach(contractManager);
            #endregion

            #region apply operation
            await Spend(sender, Origination.BakerFee);
            if (senderDelegate != null) senderDelegate.StakingBalance -= Origination.BakerFee;
            blockBaker.FrozenFees += Origination.BakerFee;
            blockBaker.Balance += Origination.BakerFee;
            blockBaker.StakingBalance += Origination.BakerFee;

            sender.OriginationsCount++;
            if (contractManager != sender) contractManager.OriginationsCount++;
            if (contractDelegate != null && contractDelegate != sender && contractDelegate != contractManager) contractDelegate.OriginationsCount++;
            if (contract != null) contract.OriginationsCount++;

            block.Operations |= Operations.Originations;
            block.Fees += Origination.BakerFee;

            sender.Counter = Math.Max(sender.Counter, Origination.Counter);
            #endregion

            #region apply result
            if (Origination.Status == OperationStatus.Applied)
            {
                await Spend(sender,
                    Origination.Balance +
                    (Origination.StorageFee ?? 0) +
                    (Origination.AllocationFee ?? 0));

                if (senderDelegate != null)
                {
                    senderDelegate.StakingBalance -= Origination.Balance;
                    senderDelegate.StakingBalance -= Origination.StorageFee ?? 0;
                    senderDelegate.StakingBalance -= Origination.AllocationFee ?? 0;
                }

                if (contractDelegate != null)
                {
                    contractDelegate.DelegatorsCount++;
                    contractDelegate.StakingBalance += contract.Balance;
                }

                sender.ContractsCount++;
                if (contractManager != sender) contractManager.ContractsCount++;

                if (contract.Kind == ContractKind.SmartContract)
                    block.Events |= BlockEvents.SmartContracts;

                Db.Contracts.Add(contract);
            }
            #endregion

            Db.OriginationOps.Add(Origination);
        }

        public override async Task Revert()
        {
            #region entities
            var block = Origination.Block;
            var blockBaker = block.Baker;

            var sender = Origination.Sender;
            var senderDelegate = sender.Delegate ?? sender as Data.Models.Delegate;

            var contract = Origination.Contract;
            var contractDelegate = Origination.Delegate;
            var contractManager = Origination.Manager;

            //Db.TryAttach(block);
            Db.TryAttach(blockBaker);

            Db.TryAttach(sender);
            Db.TryAttach(senderDelegate);

            Db.TryAttach(contract);
            Db.TryAttach(contractDelegate);
            Db.TryAttach(contractManager);
            #endregion

            #region revert result
            if (Origination.Status == OperationStatus.Applied)
            {
                await Return(sender,
                    Origination.Balance +
                    (Origination.StorageFee ?? 0) +
                    (Origination.AllocationFee ?? 0));

                if (senderDelegate != null)
                {
                    senderDelegate.StakingBalance += Origination.Balance;
                    senderDelegate.StakingBalance += Origination.StorageFee ?? 0;
                    senderDelegate.StakingBalance += Origination.AllocationFee ?? 0;
                }

                if (contractDelegate != null)
                {
                    contractDelegate.DelegatorsCount--;
                    contractDelegate.StakingBalance -= contract.Balance;
                }

                sender.ContractsCount--;
                if (contractManager != sender) contractManager.ContractsCount--;

                Db.Contracts.Remove(contract);
                Cache.Accounts.Remove(contract);
            }
            #endregion

            #region revert operation
            await Return(sender, Origination.BakerFee);
            if (senderDelegate != null) senderDelegate.StakingBalance += Origination.BakerFee;
            blockBaker.FrozenFees -= Origination.BakerFee;
            blockBaker.Balance -= Origination.BakerFee;
            blockBaker.StakingBalance -= Origination.BakerFee;

            sender.OriginationsCount--;
            if (contractManager != sender) contractManager.OriginationsCount--;
            if (contractDelegate != null && contractDelegate != sender && contractDelegate != contractManager) contractDelegate.OriginationsCount--;

            sender.Counter = Math.Min(sender.Counter, Origination.Counter - 1);
            #endregion

            Db.OriginationOps.Remove(Origination);
            Cache.AppState.ReleaseManagerCounter();
        }

        #region static
        public static async Task<OriginationsCommit> Apply(ProtocolHandler proto, Block block, RawOperation op, RawOriginationContent content)
        {
            var commit = new OriginationsCommit(proto);
            await commit.Init(block, op, content);
            await commit.Apply();

            return commit;
        }

        public static async Task<OriginationsCommit> Revert(ProtocolHandler proto, Block block, OriginationOperation op)
        {
            var commit = new OriginationsCommit(proto);
            await commit.Init(block, op);
            await commit.Revert();

            return commit;
        }
        #endregion
    }
}
