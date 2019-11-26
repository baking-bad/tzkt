using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

using Tzkt.Data.Models;
using Tzkt.Data.Models.Base;

namespace Tzkt.Sync.Protocols.Proto5
{
    class OriginationsCommit : ProtocolCommit
    {
        public OriginationOperation Origination { get; private set; }

        OriginationsCommit(ProtocolHandler protocol) : base(protocol) { }

        public async Task Init(Block block, RawOperation op, RawOriginationContent content)
        {
            var sender = await Cache.GetAccountAsync(content.Source);
            sender.Delegate ??= (Data.Models.Delegate)await Cache.GetAccountAsync(sender.DelegateId);

            var delegat = await Cache.GetDelegateOrDefaultAsync(content.Delegate);

            var contract = content.Metadata.Result.Status == "applied" ?
                new Contract
                {
                    Address = content.Metadata.Result.OriginatedContracts[0],
                    Balance = content.Balance,
                    Counter = 0,
                    Delegate = delegat,
                    DelegationLevel = delegat != null ? (int?)block.Level : null,
                    Creator = sender,
                    Staked = delegat?.Staked ?? false,
                    Type = AccountType.Contract,
                    Kind = content.Script == null ? ContractKind.DelegatorContract : ContractKind.SmartContract
                }
                : null;

            Origination = new OriginationOperation
            {
                Id = await Cache.NextCounterAsync(),
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
            var sender = await Cache.GetAccountAsync(content.Source);
            sender.Delegate ??= (Data.Models.Delegate)await Cache.GetAccountAsync(sender.DelegateId);

            var delegat = await Cache.GetDelegateOrDefaultAsync(content.Delegate);

            var contract = content.Result.Status == "applied" ?
                new Contract
                {
                    Address = content.Result.OriginatedContracts[0],
                    Balance = content.Balance,
                    Counter = 0,
                    Delegate = delegat,
                    DelegationLevel = delegat != null ? (int?)block.Level : null,
                    Creator = sender,
                    Staked = delegat?.Staked ?? false,
                    Type = AccountType.Contract,
                    Kind = ContractKind.SmartContract
                }
                : null;

            Origination = new OriginationOperation
            {
                Id = await Cache.NextCounterAsync(),
                Parent = parent,
                Block = parent.Block,
                Level = parent.Block.Level,
                Timestamp = parent.Timestamp,
                OpHash = parent.OpHash,
                Counter = parent.Counter,
                Nonce = content.Nonce,
                Balance = content.Balance,
                Sender = sender,
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
            Origination.Block.Protocol ??= await Cache.GetProtocolAsync(block.ProtoCode);
            Origination.Block.Baker ??= (Data.Models.Delegate)await Cache.GetAccountAsync(block.BakerId);
            
            Origination.Sender = await Cache.GetAccountAsync(origination.SenderId);
            Origination.Sender.Delegate ??= (Data.Models.Delegate)await Cache.GetAccountAsync(origination.Sender.DelegateId);
            Origination.Contract ??= (Contract)await Cache.GetAccountAsync(origination.ContractId);
            Origination.Delegate ??= (Data.Models.Delegate)await Cache.GetAccountAsync(origination.DelegateId);
        }

        public override async Task Apply()
        {
            if (Origination.Parent == null)
                await ApplyOrigination();
            else
                await ApplyInternalOrigination();
        }

        public override async Task Revert()
        {
            if (Origination.ParentId == null)
                await RevertOrigination();
            else
                await RevertInternalOrigination();
        }

        public Task ApplyOrigination()
        {
            #region entities
            var block = Origination.Block;
            var blockBaker = block.Baker;

            var sender = Origination.Sender;
            var senderDelegate = sender.Delegate ?? sender as Data.Models.Delegate;

            var contract = Origination.Contract;
            var contractDelegate = Origination.Delegate;

            //Db.TryAttach(block);
            Db.TryAttach(blockBaker);

            Db.TryAttach(sender);
            Db.TryAttach(senderDelegate);

            Db.TryAttach(contract);
            Db.TryAttach(contractDelegate);
            #endregion

            #region apply operation
            sender.Balance -= Origination.BakerFee;
            if (senderDelegate != null) senderDelegate.StakingBalance -= Origination.BakerFee;
            blockBaker.FrozenFees += Origination.BakerFee;
            blockBaker.Balance += Origination.BakerFee;
            blockBaker.StakingBalance += Origination.BakerFee;

            sender.OriginationsCount++;
            contract.OriginationsCount++;
            if (contractDelegate != null) contractDelegate.OriginationsCount++;

            block.Operations |= Operations.Originations;

            sender.Counter = Math.Max(sender.Counter, Origination.Counter);
            #endregion

            #region apply result
            if (Origination.Status == OperationStatus.Applied)
            {
                sender.Balance -= Origination.Balance;
                sender.Balance -= Origination.StorageFee ?? 0;
                sender.Balance -= Origination.AllocationFee ?? 0;

                if (senderDelegate != null)
                {
                    senderDelegate.StakingBalance -= Origination.Balance;
                    senderDelegate.StakingBalance -= Origination.StorageFee ?? 0;
                    senderDelegate.StakingBalance -= Origination.AllocationFee ?? 0;
                }

                if (contractDelegate != null)
                {
                    contractDelegate.Delegators++;
                    contractDelegate.StakingBalance += contract.Balance;
                }

                sender.Contracts++;

                Db.Contracts.Add(contract);
            }
            #endregion

            Db.OriginationOps.Add(Origination);

            return Task.CompletedTask;
        }

        public Task ApplyInternalOrigination()
        {
            #region entities
            var block = Origination.Block;

            var parentTx = Origination.Parent;
            var parentSender = parentTx.Sender;
            var parentDelegate = parentSender.Delegate ?? parentSender as Data.Models.Delegate;

            var sender = Origination.Sender;
            var senderDelegate = sender.Delegate ?? sender as Data.Models.Delegate;

            var contract = Origination.Contract;
            var contractDelegate = Origination.Delegate;

            //Db.TryAttach(parentTx);
            //Db.TryAttach(parentSender);
            //Db.TryAttach(parentDelegate);

            Db.TryAttach(sender);
            Db.TryAttach(senderDelegate);

            Db.TryAttach(contract);
            Db.TryAttach(contractDelegate);
            #endregion

            #region apply operation
            parentTx.InternalOperations = (parentTx.InternalOperations ?? InternalOperations.None) | InternalOperations.Originations;

            sender.OriginationsCount++;
            contract.OriginationsCount++;
            if (contractDelegate != null) contractDelegate.OriginationsCount++;

            block.Operations |= Operations.Originations;
            #endregion

            #region apply result
            if (Origination.Status == OperationStatus.Applied)
            {
                sender.Balance -= Origination.Balance;

                if (senderDelegate != null)
                {
                    senderDelegate.StakingBalance -= Origination.Balance;
                }

                parentSender.Balance -= Origination.StorageFee ?? 0;
                parentSender.Balance -= Origination.AllocationFee ?? 0;

                if (parentDelegate != null)
                {
                    parentDelegate.StakingBalance -= Origination.StorageFee ?? 0;
                    parentDelegate.StakingBalance -= Origination.AllocationFee ?? 0;
                }

                if (contractDelegate != null)
                {
                    contractDelegate.Delegators++;
                    contractDelegate.StakingBalance += contract.Balance;
                }

                sender.Contracts++;

                Db.Contracts.Add(contract);
            }
            #endregion

            Db.OriginationOps.Add(Origination);

            return Task.CompletedTask;
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

            //Db.TryAttach(block);
            Db.TryAttach(blockBaker);

            Db.TryAttach(sender);
            Db.TryAttach(senderDelegate);

            Db.TryAttach(contract);
            Db.TryAttach(contractDelegate);
            #endregion

            #region revert result
            if (Origination.Status == OperationStatus.Applied)
            {
                sender.Balance += Origination.Balance;
                sender.Balance += Origination.StorageFee ?? 0;
                sender.Balance += Origination.AllocationFee ?? 0;

                if (senderDelegate != null)
                {
                    senderDelegate.StakingBalance += Origination.Balance;
                    senderDelegate.StakingBalance += Origination.StorageFee ?? 0;
                    senderDelegate.StakingBalance += Origination.AllocationFee ?? 0;
                }

                if (contractDelegate != null)
                {
                    contractDelegate.Delegators--;
                    contractDelegate.StakingBalance -= contract.Balance;
                }

                sender.Contracts--;

                Db.Contracts.Remove(contract);
                Cache.RemoveAccount(contract);
            }
            #endregion

            #region revert operation
            sender.Balance += Origination.BakerFee;
            if (senderDelegate != null) senderDelegate.StakingBalance += Origination.BakerFee;
            blockBaker.FrozenFees -= Origination.BakerFee;
            blockBaker.Balance -= Origination.BakerFee;
            blockBaker.StakingBalance -= Origination.BakerFee;

            sender.OriginationsCount--;
            if (contractDelegate != null) contractDelegate.OriginationsCount--;

            sender.Counter = Math.Min(sender.Counter, Origination.Counter - 1);
            #endregion

            Db.OriginationOps.Remove(Origination);
            await Cache.ReleaseCounterAsync(true);
        }

        public async Task RevertInternalOrigination()
        {
            #region entities
            var parentTx = Origination.Parent;
            var parentSender = parentTx.Sender;
            var parentDelegate = parentSender.Delegate ?? parentSender as Data.Models.Delegate;

            var sender = Origination.Sender;
            var senderDelegate = sender.Delegate ?? sender as Data.Models.Delegate;

            var contract = Origination.Contract;
            var contractDelegate = Origination.Delegate;

            //Db.TryAttach(parentTx);
            //Db.TryAttach(parentSender);
            //Db.TryAttach(parentDelegate);

            Db.TryAttach(sender);
            Db.TryAttach(senderDelegate);

            Db.TryAttach(contract);
            Db.TryAttach(contractDelegate);
            #endregion

            #region revert result
            if (Origination.Status == OperationStatus.Applied)
            {
                sender.Balance += Origination.Balance;

                if (senderDelegate != null)
                {
                    senderDelegate.StakingBalance += Origination.Balance;
                }

                parentSender.Balance += Origination.StorageFee ?? 0;
                parentSender.Balance += Origination.AllocationFee ?? 0;

                if (parentDelegate != null)
                {
                    parentDelegate.StakingBalance += Origination.StorageFee ?? 0;
                    parentDelegate.StakingBalance += Origination.AllocationFee ?? 0;
                }

                if (contractDelegate != null)
                {
                    contractDelegate.Delegators--;
                    contractDelegate.StakingBalance -= contract.Balance;
                }

                sender.Contracts--;

                Db.Contracts.Remove(contract);
                Cache.RemoveAccount(contract);
            }
            #endregion

            #region revert operation
            sender.OriginationsCount--;
            if (contractDelegate != null) contractDelegate.OriginationsCount--;
            #endregion

            Db.OriginationOps.Remove(Origination);
            await Cache.ReleaseCounterAsync(true);
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
