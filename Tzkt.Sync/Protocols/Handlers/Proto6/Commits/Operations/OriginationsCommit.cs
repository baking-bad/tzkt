using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

using Tzkt.Data.Models;
using Tzkt.Data.Models.Base;

namespace Tzkt.Sync.Protocols.Proto6
{
    class OriginationsCommit : ProtocolCommit
    {
        public OriginationOperation Origination { get; private set; }
        public TransactionOperation Parent { get; private set; }

        OriginationsCommit(ProtocolHandler protocol) : base(protocol) { }

        public async Task Init(Block block, RawOperation op, RawOriginationContent content)
        {
            var sender = await Cache.Accounts.GetAsync(content.Source);
            sender.Delegate ??= Cache.Accounts.GetDelegate(sender.DelegateId);

            var manager = ManagerTz.Test(content.Script.Code, content.Script.Storage)
                ? (User)await Cache.Accounts.GetAsync(ManagerTz.GetManager(content.Script.Storage))
                : null;

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
                    Kind = manager != null ? ContractKind.DelegatorContract : ContractKind.SmartContract
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
                    _ => throw new Exception($"Invalid status '{content.Metadata.Result.Status}'")
                },
                Errors = OperationErrors.Parse(content.Metadata.Result.Errors),
                GasUsed = content.Metadata.Result.ConsumedGas,
                StorageUsed = content.Metadata.Result.PaidStorageSizeDiff,
                StorageFee = content.Metadata.Result.PaidStorageSizeDiff * block.Protocol.ByteCost,
                AllocationFee = block.Protocol.OriginationSize * block.Protocol.ByteCost
            };
        }

        public async Task Init(Block block, TransactionOperation parent, RawInternalOriginationResult content)
        {
            var sender = await Cache.Accounts.GetAsync(content.Source)
                ?? block.Originations?.FirstOrDefault(x => x.Contract.Address == content.Source)?.Contract;
            
            sender.Delegate ??= Cache.Accounts.GetDelegate(sender.DelegateId);

            var manager = ManagerTz.Test(content.Script.Code, content.Script.Storage)
                ? (User)await Cache.Accounts.GetAsync(ManagerTz.GetManager(content.Script.Storage))
                : null;

            var delegat = Cache.Accounts.GetDelegateOrDefault(content.Delegate);

            var contract = content.Result.Status == "applied" ?
                new Contract
                {
                    Id = Cache.AppState.NextAccountId(),
                    Address = content.Result.OriginatedContracts[0],
                    Balance = content.Balance,
                    Counter = 0,
                    Delegate = delegat,
                    DelegationLevel = delegat != null ? (int?)block.Level : null,
                    Creator = sender,
                    Manager = manager,
                    Staked = delegat?.Staked ?? false,
                    Type = AccountType.Contract,
                    Kind = manager != null ? ContractKind.DelegatorContract : ContractKind.SmartContract
                }
                : null;

            Parent = parent;
            Origination = new OriginationOperation
            {
                Id = Cache.AppState.NextOperationId(),
                Initiator = parent.Sender,
                Block = parent.Block,
                Level = parent.Block.Level,
                Timestamp = parent.Timestamp,
                OpHash = parent.OpHash,
                Counter = parent.Counter,
                Nonce = content.Nonce,
                Balance = content.Balance,
                Sender = sender,
                Manager = manager,
                Delegate = delegat,
                Contract = contract,
                Status = content.Result.Status switch
                {
                    "applied" => OperationStatus.Applied,
                    "backtracked" => OperationStatus.Backtracked,
                    "failed" => OperationStatus.Failed,
                    "skipped" => OperationStatus.Skipped,
                    _ => throw new Exception($"Invalid status '{content.Result.Status}'")
                },
                Errors = OperationErrors.Parse(content.Result.Errors),
                GasUsed = content.Result.ConsumedGas,
                StorageUsed = content.Result.PaidStorageSizeDiff,
                StorageFee = content.Result.PaidStorageSizeDiff * block.Protocol.ByteCost,
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

            if (Origination.InitiatorId != null)
            {
                Origination.Initiator = await Cache.Accounts.GetAsync(origination.InitiatorId);
                Origination.Initiator.Delegate ??= Cache.Accounts.GetDelegate(origination.Initiator.DelegateId);
            }
        }

        public override async Task Apply()
        {
            if (Parent == null)
                await ApplyOrigination();
            else
                await ApplyInternalOrigination();
        }

        public override async Task Revert()
        {
            if (Origination.InitiatorId == null)
                await RevertOrigination();
            else
                await RevertInternalOrigination();
        }

        public async Task ApplyOrigination()
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
            if (contractManager != null && contractManager != sender) contractManager.OriginationsCount++;
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
                if (contractManager != null && contractManager != sender) contractManager.ContractsCount++;

                if (contract.Kind == ContractKind.SmartContract)
                    block.Events |= BlockEvents.SmartContracts;

                Db.Contracts.Add(contract);
            }
            #endregion

            Db.OriginationOps.Add(Origination);
        }

        public async Task ApplyInternalOrigination()
        {
            #region entities
            var block = Origination.Block;

            var parentTx = Parent;
            var parentSender = parentTx.Sender;
            var parentDelegate = parentSender.Delegate ?? parentSender as Data.Models.Delegate;

            var sender = Origination.Sender;
            var senderDelegate = sender.Delegate ?? sender as Data.Models.Delegate;

            var contract = Origination.Contract;
            var contractDelegate = Origination.Delegate;
            var contractManager = Origination.Manager;

            //Db.TryAttach(parentTx);
            //Db.TryAttach(parentSender);
            //Db.TryAttach(parentDelegate);

            Db.TryAttach(sender);
            Db.TryAttach(senderDelegate);

            Db.TryAttach(contract);
            Db.TryAttach(contractDelegate);
            Db.TryAttach(contractManager);
            #endregion

            #region apply operation
            parentTx.InternalOperations = (parentTx.InternalOperations ?? InternalOperations.None) | InternalOperations.Originations;

            sender.OriginationsCount++;
            if (contractManager != null && contractManager != sender) contractManager.OriginationsCount++;
            if (contractDelegate != null && contractDelegate != sender && contractDelegate != contractManager) contractDelegate.OriginationsCount++;
            if (parentSender != sender && parentSender != contractDelegate && parentSender != contractManager) parentSender.OriginationsCount++;
            if (contract != null) contract.OriginationsCount++;

            block.Operations |= Operations.Originations;
            #endregion

            #region apply result
            if (Origination.Status == OperationStatus.Applied)
            {
                await Spend(sender, Origination.Balance);

                if (senderDelegate != null)
                {
                    senderDelegate.StakingBalance -= Origination.Balance;
                }

                await Spend(parentSender,
                    (Origination.StorageFee ?? 0) +
                    (Origination.AllocationFee ?? 0));

                if (parentDelegate != null)
                {
                    parentDelegate.StakingBalance -= Origination.StorageFee ?? 0;
                    parentDelegate.StakingBalance -= Origination.AllocationFee ?? 0;
                }

                if (contractDelegate != null)
                {
                    contractDelegate.DelegatorsCount++;
                    contractDelegate.StakingBalance += contract.Balance;
                }

                sender.ContractsCount++;
                if (contractManager != null && contractManager != sender) contractManager.ContractsCount++;

                if (contract.Kind == ContractKind.SmartContract)
                    block.Events |= BlockEvents.SmartContracts;

                Db.Contracts.Add(contract);
            }
            #endregion

            Db.OriginationOps.Add(Origination);
        }

        public async Task RevertOrigination()
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
                if (contractManager != null && contractManager != sender) contractManager.ContractsCount--;

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
            if (contractManager != null && contractManager != sender) contractManager.OriginationsCount--;
            if (contractDelegate != null && contractDelegate != sender && contractDelegate != contractManager) contractDelegate.OriginationsCount--;

            sender.Counter = Math.Min(sender.Counter, Origination.Counter - 1);
            #endregion

            Db.OriginationOps.Remove(Origination);
            Cache.AppState.ReleaseManagerCounter();
        }

        public async Task RevertInternalOrigination()
        {
            #region entities
            var parentSender = Origination.Initiator;
            var parentDelegate = parentSender.Delegate ?? parentSender as Data.Models.Delegate;

            var sender = Origination.Sender;
            var senderDelegate = sender.Delegate ?? sender as Data.Models.Delegate;

            var contract = Origination.Contract;
            var contractDelegate = Origination.Delegate;
            var contractManager = Origination.Manager;

            //Db.TryAttach(parentTx);
            //Db.TryAttach(parentSender);
            //Db.TryAttach(parentDelegate);

            Db.TryAttach(sender);
            Db.TryAttach(senderDelegate);

            Db.TryAttach(contract);
            Db.TryAttach(contractDelegate);
            Db.TryAttach(contractManager);
            #endregion

            #region revert result
            if (Origination.Status == OperationStatus.Applied)
            {
                await Return(sender, Origination.Balance);

                if (senderDelegate != null)
                {
                    senderDelegate.StakingBalance += Origination.Balance;
                }

                await Return(parentSender,
                    (Origination.StorageFee ?? 0) +
                    (Origination.AllocationFee ?? 0));

                if (parentDelegate != null)
                {
                    parentDelegate.StakingBalance += Origination.StorageFee ?? 0;
                    parentDelegate.StakingBalance += Origination.AllocationFee ?? 0;
                }

                if (contractDelegate != null)
                {
                    contractDelegate.DelegatorsCount--;
                    contractDelegate.StakingBalance -= contract.Balance;
                }

                sender.ContractsCount--;
                if (contractManager != null && contractManager != sender) contractManager.ContractsCount--;

                Db.Contracts.Remove(contract);
                Cache.Accounts.Remove(contract);
            }
            #endregion

            #region revert operation
            sender.OriginationsCount--;
            if (contractManager != null && contractManager != sender) contractManager.OriginationsCount--;
            if (contractDelegate != null && contractDelegate != sender && contractDelegate != contractManager) contractDelegate.OriginationsCount--;
            if (parentSender != sender && parentSender != contractDelegate && parentSender != contractManager) parentSender.OriginationsCount--;
            #endregion

            Db.OriginationOps.Remove(Origination);
        }

        #region static
        public static async Task<OriginationsCommit> Apply(ProtocolHandler proto, Block block, RawOperation op, RawOriginationContent content)
        {
            var commit = new OriginationsCommit(proto);
            await commit.Init(block, op, content);
            await commit.Apply();

            return commit;
        }

        public static async Task<OriginationsCommit> Apply(ProtocolHandler proto, Block block, TransactionOperation parent, RawInternalOriginationResult content)
        {
            var commit = new OriginationsCommit(proto);
            await commit.Init(block, parent, content);
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
