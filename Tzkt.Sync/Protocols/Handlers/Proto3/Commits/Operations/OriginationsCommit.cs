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
            var sender = await Cache.GetAccountAsync(content.Source);
            sender.Delegate ??= (Data.Models.Delegate)await Cache.GetAccountAsync(sender.DelegateId);

            var delegat = await Cache.GetAccountAsync(content.Delegate) as Data.Models.Delegate;

            var contract = content.Metadata.Result.Status == "applied" ?
                        new Contract
                        {
                            Address = content.Metadata.Result.OriginatedContracts[0],
                            Balance = content.Balance,
                            Counter = 0,
                            Delegate = delegat,
                            DelegationLevel = delegat != null ? (int?)block.Level : null,
                            Manager = sender,
                            Operations = Operations.None,
                            Staked = delegat?.Staked ?? false,
                            Type = AccountType.Contract
                        }
                        : null;

            Origination = new OriginationOperation
            {
                Id = await Cache.NextCounterAsync(),
                Block = block,
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
                    _ => throw new NotImplementedException()
                },
                GasUsed = content.Metadata.Result.ConsumedGas,
                StorageUsed = content.Metadata.Result.PaidStorageSizeDiff,
                StorageFee = content.Metadata.Result.PaidStorageSizeDiff * block.Protocol.ByteCost,
                AllocationFee = block.Protocol.OriginationSize * block.Protocol.ByteCost
            };

            if (contract != null)
                contract.Origination = Origination;
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

        public override Task Apply()
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

            sender.Operations |= Operations.Originations;
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

                Db.Contracts.Add(contract);
            }
            #endregion

            Db.OriginationOps.Add(Origination);

            return Task.CompletedTask;
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

            if (!await Db.OriginationOps.AnyAsync(x => x.SenderId == sender.Id && x.Id < Origination.Id))
                sender.Operations &= ~Operations.Originations;

            sender.Counter = Math.Min(sender.Counter, Origination.Counter - 1);
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
