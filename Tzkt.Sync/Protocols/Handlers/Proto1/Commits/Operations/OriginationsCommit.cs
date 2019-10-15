using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

using Tzkt.Data.Models;
using Tzkt.Data.Models.Base;

namespace Tzkt.Sync.Protocols.Proto1
{
    class OriginationsCommit : ProtocolCommit
    {
        public List<OriginationOperation> Originations { get; private set; }
        public Protocol Protocol { get; private set; }

        public OriginationsCommit(ProtocolHandler protocol, List<ICommit> commits) : base(protocol, commits) { }

        public override async Task Init()
        {
            var block = await Cache.GetCurrentBlockAsync();
            block.Baker ??= (Data.Models.Delegate)await Cache.GetAccountAsync(block.BakerId);

            Protocol = await Cache.GetCurrentProtocolAsync();
            Originations = await Db.OriginationOps.Include(x => x.WeirdDelegation).Where(x => x.Level == block.Level).ToListAsync();
            foreach (var op in Originations)
            {
                op.Block = block;
                op.Sender ??= await Cache.GetAccountAsync(op.SenderId);
                op.Sender.Delegate ??= (Data.Models.Delegate)await Cache.GetAccountAsync(op.Sender.DelegateId);

                op.Contract ??= (Contract)await Cache.GetAccountAsync(op.ContractId);
                op.Delegate ??= (Data.Models.Delegate)await Cache.GetAccountAsync(op.DelegateId);
            }
        }

        public override async Task Init(IBlock block)
        {
            var rawBlock = block as RawBlock;
            var parsedBlock = FindCommit<BlockCommit>().Block;
            parsedBlock.Baker ??= (Data.Models.Delegate)await Cache.GetAccountAsync(parsedBlock.BakerId);

            Protocol = await Cache.GetProtocolAsync(block.Protocol);
            Originations = new List<OriginationOperation>();
            foreach (var op in rawBlock.Operations[3])
            {
                foreach (var content in op.Contents.Where(x => x is RawOriginationContent))
                {
                    var origination = content as RawOriginationContent;

                    var sender = await Cache.GetAccountAsync(origination.Source);
                    sender.Delegate ??= (Data.Models.Delegate)await Cache.GetAccountAsync(sender.DelegateId);

                    var originDelegate = await Cache.GetAccountAsync(origination.Delegate);
                    var delegat = originDelegate as Data.Models.Delegate;
                    // WTF: [level:635] - Tezos allows to set non-existent delegate.

                    var contract = origination.Metadata.Result.Status == "applied" ? 
                        new Contract
                        {
                            Address = origination.Metadata.Result.OriginatedContracts[0],
                            Balance = origination.Balance,
                            Counter = 0,
                            Delegate = delegat,
                            DelegationLevel = delegat != null ? (int?)parsedBlock.Level : null,
                            Manager = sender,
                            Operations = Operations.Originations,
                            Staked = delegat?.Staked ?? false,
                            Type = AccountType.Contract
                        }
                        : null;

                    var originationOp = new OriginationOperation
                    {
                        Block = parsedBlock,
                        Timestamp = parsedBlock.Timestamp,

                        OpHash = op.Hash,

                        Balance = origination.Balance,
                        BakerFee = origination.Fee,
                        Counter = origination.Counter,
                        GasLimit = origination.GasLimit,
                        StorageLimit = origination.StorageLimit,
                        Sender = sender,
                        Delegate = delegat,
                        Contract = contract,

                        Status = origination.Metadata.Result.Status switch
                        {
                            "applied" => OperationStatus.Applied,
                            _ => throw new NotImplementedException()
                        },
                        GasUsed = origination.Metadata.Result.ConsumedGas,
                        StorageUsed = origination.Metadata.Result.PaidStorageSizeDiff,
                        StorageFee = origination.Metadata.Result.PaidStorageSizeDiff * Protocol.ByteCost,
                        AllocationFee = Protocol.OriginationSize * Protocol.ByteCost
                    };

                    if (originDelegate != null && originDelegate.Type != AccountType.Delegate)
                    {
                        originationOp.WeirdDelegation = new WeirdDelegation
                        {
                            DelegateId = originDelegate.Id,
                            Level = block.Level,
                            Origination = originationOp
                        };
                    }

                    contract.Origination = originationOp;
                    Originations.Add(originationOp);
                }
            }
        }

        public override Task Apply()
        {
            foreach (var origination in Originations)
            {
                #region entities
                var block = origination.Block;
                var blockBaker = block.Baker;

                var sender = origination.Sender;
                var senderDelegate = sender.Delegate ?? sender as Data.Models.Delegate;

                var contract = origination.Contract;
                var contractDelegate = origination.Delegate;

                //Db.TryAttach(block);
                Db.TryAttach(blockBaker);

                Db.TryAttach(sender);
                Db.TryAttach(senderDelegate);

                Db.TryAttach(contract);
                Db.TryAttach(contractDelegate);
                #endregion

                #region apply operation
                sender.Balance -= origination.BakerFee;
                if (senderDelegate != null) senderDelegate.StakingBalance -= origination.BakerFee;
                blockBaker.FrozenFees += origination.BakerFee;
                blockBaker.Balance += origination.BakerFee;
                blockBaker.StakingBalance += origination.BakerFee;

                sender.Operations |= Operations.Originations;
                contract.Operations |= Operations.Originations;
                block.Operations |= Operations.Originations;

                sender.Counter = Math.Max(sender.Counter, origination.Counter);
                #endregion

                #region apply result
                if (origination.Status == OperationStatus.Applied)
                {
                    sender.Balance -= origination.Balance;
                    sender.Balance -= origination.StorageFee ?? 0;
                    sender.Balance -= origination.AllocationFee ?? 0;

                    if (senderDelegate != null)
                    {
                        senderDelegate.StakingBalance -= origination.Balance;
                        senderDelegate.StakingBalance -= origination.StorageFee ?? 0;
                        senderDelegate.StakingBalance -= origination.AllocationFee ?? 0;
                    }

                    if (contractDelegate != null)
                    {
                        contractDelegate.Delegators++;
                        contractDelegate.StakingBalance += contract.Balance;
                    }
                }
                #endregion

                Db.Contracts.Add(contract);
                Db.OriginationOps.Add(origination);
            }

            return Task.CompletedTask;
        }

        public override async Task Revert()
        {
            foreach (var origination in Originations)
            {
                #region entities
                var block = origination.Block;
                var blockBaker = block.Baker;

                var sender = origination.Sender;
                var senderDelegate = sender.Delegate ?? sender as Data.Models.Delegate;

                var contract = origination.Contract;
                var contractDelegate = origination.Delegate;

                //Db.TryAttach(block);
                Db.TryAttach(blockBaker);

                Db.TryAttach(sender);
                Db.TryAttach(senderDelegate);

                Db.TryAttach(contract);
                Db.TryAttach(contractDelegate);
                #endregion

                #region revert result
                if (origination.Status == OperationStatus.Applied)
                {
                    sender.Balance += origination.Balance;
                    sender.Balance += origination.StorageFee ?? 0;
                    sender.Balance += origination.AllocationFee ?? 0;

                    if (senderDelegate != null)
                    {
                        senderDelegate.StakingBalance += origination.Balance;
                        senderDelegate.StakingBalance += origination.StorageFee ?? 0;
                        senderDelegate.StakingBalance += origination.AllocationFee ?? 0;
                    }

                    if (contractDelegate != null)
                    {
                        contractDelegate.Delegators--;
                        contractDelegate.StakingBalance -= contract.Balance;
                    }
                }
                #endregion

                #region revert operation
                sender.Balance += origination.BakerFee;
                if (senderDelegate != null) senderDelegate.StakingBalance += origination.BakerFee;
                blockBaker.FrozenFees -= origination.BakerFee;
                blockBaker.Balance -= origination.BakerFee;
                blockBaker.StakingBalance -= origination.BakerFee;

                if (!await Db.OriginationOps.AnyAsync(x => x.SenderId == sender.Id && x.Level < origination.Level))
                    sender.Operations &= ~Operations.Originations;

                sender.Counter = Math.Min(sender.Counter, origination.Counter - 1);
                #endregion

                Db.Contracts.Remove(contract);
                Cache.RemoveAccount(contract);

                Db.OriginationOps.Remove(origination);
            }
        }

        #region static
        public static async Task<OriginationsCommit> Create(ProtocolHandler protocol, List<ICommit> commits, RawBlock rawBlock)
        {
            var commit = new OriginationsCommit(protocol, commits);
            await commit.Init(rawBlock);
            return commit;
        }

        public static async Task<OriginationsCommit> Create(ProtocolHandler protocol, List<ICommit> commits)
        {
            var commit = new OriginationsCommit(protocol, commits);
            await commit.Init();
            return commit;
        }
        #endregion
    }
}
