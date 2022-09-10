using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Netezos.Contracts;
using Netezos.Encoding;

using Tzkt.Data.Models;
using Tzkt.Data.Models.Base;

namespace Tzkt.Sync.Protocols.Proto1
{
    class OriginationsCommit : ProtocolCommit
    {
        public OriginationOperation Origination { get; private set; }
        public IEnumerable<BigMapDiff> BigMapDiffs { get; private set; }

        public OriginationsCommit(ProtocolHandler protocol) : base(protocol) { }

        public virtual async Task Apply(Block block, JsonElement op, JsonElement content)
        {
            #region init
            var sender = await Cache.Accounts.GetAsync(content.RequiredString("source"));
            sender.Delegate ??= Cache.Accounts.GetDelegate(sender.DelegateId);

            // WTF: [level:25054] - Manager and sender are not equal.
            var manager = await GetManager(content);
            var delegat = Cache.Accounts.GetDelegateOrDefault(content.OptionalString("delegate"));
            // WTF: [level:635] - Tezos allows to set non-existent delegate.

            Db.TryAttach(block.Proposer);
            Db.TryAttach(sender);
            Db.TryAttach(sender.Delegate);
            Db.TryAttach(manager);
            Db.TryAttach(delegat);

            var result = content.Required("metadata").Required("operation_result");

            Contract contract = null;
            if (result.RequiredString("status") == "applied")
            {
                var address = result.RequiredArray("originated_contracts", 1)[0].RequiredString();
                var ghost = await Cache.Accounts.GetAsync(address);
                if (ghost != null)
                {
                    contract = new Contract
                    {
                        Id = ghost.Id,
                        FirstLevel = ghost.FirstLevel,
                        LastLevel = ghost.LastLevel,
                        Address = address,
                        Balance = content.RequiredInt64("balance"),
                        Counter = 0,
                        Delegate = delegat,
                        DelegationLevel = delegat != null ? block.Level : null,
                        WeirdDelegate = await GetWeirdDelegate(content),
                        Creator = sender,
                        Manager = manager,
                        Staked = delegat?.Staked ?? false,
                        Type = AccountType.Contract,
                        Kind = GetContractKind(content),
                        Spendable = GetSpendable(content),
                        ActiveTokensCount = ghost.ActiveTokensCount,
                        TokenBalancesCount = ghost.TokenBalancesCount,
                        TokenTransfersCount = ghost.TokenTransfersCount
                    };
                    Db.Entry(ghost).State = EntityState.Detached;
                    Db.Entry(contract).State = EntityState.Modified;
                }
                else
                {
                    contract = new Contract
                    {
                        Id = Cache.AppState.NextAccountId(),
                        FirstLevel = block.Level,
                        LastLevel = block.Level,
                        Address = address,
                        Balance = content.RequiredInt64("balance"),
                        Counter = 0,
                        Delegate = delegat,
                        DelegationLevel = delegat != null ? block.Level : null,
                        WeirdDelegate = await GetWeirdDelegate(content),
                        Creator = sender,
                        Manager = manager,
                        Staked = delegat?.Staked ?? false,
                        Type = AccountType.Contract,
                        Kind = GetContractKind(content),
                        Spendable = GetSpendable(content)
                    };
                    Db.Contracts.Add(contract);
                }
                Cache.Accounts.Add(contract);
            }

            var origination = new OriginationOperation
            {
                Id = Cache.AppState.NextOperationId(),
                Block = block,
                Level = block.Level,
                Timestamp = block.Timestamp,
                OpHash = op.RequiredString("hash"),
                Balance = content.RequiredInt64("balance"),
                BakerFee = content.RequiredInt64("fee"),
                Counter = content.RequiredInt32("counter"),
                GasLimit = content.RequiredInt32("gas_limit"),
                StorageLimit = content.RequiredInt32("storage_limit"),
                Sender = sender,
                Manager = manager,
                Delegate = delegat,
                Contract = contract,
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
                StorageUsed = result.OptionalInt32("paid_storage_size_diff") ?? 0,
                StorageFee = result.OptionalInt32("paid_storage_size_diff") > 0
                    ? result.OptionalInt32("paid_storage_size_diff") * block.Protocol.ByteCost
                    : null,
                AllocationFee = block.Protocol.OriginationSize * block.Protocol.ByteCost
            };
            #endregion

            #region entities
            //var block = origination.Block;
            var blockBaker = block.Proposer;
            //var sender = origination.Sender;
            var senderDelegate = sender.Delegate ?? sender as Data.Models.Delegate;
            //var contract = origination.Contract;
            var contractDelegate = origination.Delegate;
            var contractManager = origination.Manager;

            //Db.TryAttach(block);
            //Db.TryAttach(blockBaker);
            //Db.TryAttach(sender);
            //Db.TryAttach(senderDelegate);
            //Db.TryAttach(contract);
            //Db.TryAttach(contractDelegate);
            //Db.TryAttach(contractManager);
            #endregion

            #region apply operation
            sender.Balance -= origination.BakerFee;
            if (senderDelegate != null)
            {
                senderDelegate.StakingBalance -= origination.BakerFee;
                if (senderDelegate.Id != sender.Id)
                    senderDelegate.DelegatedBalance -= origination.BakerFee;
            }
            blockBaker.Balance += origination.BakerFee;
            blockBaker.StakingBalance += origination.BakerFee;

            sender.OriginationsCount++;
            if (contractManager != null && contractManager != sender) contractManager.OriginationsCount++;
            if (contractDelegate != null && contractDelegate != sender && contractDelegate != contractManager) contractDelegate.OriginationsCount++;
            if (contract != null) contract.OriginationsCount++;

            block.Operations |= Operations.Originations;
            block.Fees += origination.BakerFee;

            sender.Counter = origination.Counter;
            #endregion

            #region apply result
            if (origination.Status == OperationStatus.Applied)
            {
                var burned = (origination.StorageFee ?? 0) + (origination.AllocationFee ?? 0);
                var spent = origination.Balance + burned;
                Proto.Manager.Burn(burned);
                
                sender.Balance -= spent;
                if (senderDelegate != null)
                {
                    senderDelegate.StakingBalance -= spent;
                    if (senderDelegate.Id != sender.Id)
                        senderDelegate.DelegatedBalance -= spent;
                }

                if (contractDelegate != null)
                {
                    contractDelegate.DelegatorsCount++;
                    contractDelegate.StakingBalance += contract.Balance;
                    contractDelegate.DelegatedBalance += contract.Balance;
                }

                sender.ContractsCount++;
                if (contractManager != null && contractManager != sender) contractManager.ContractsCount++;

                block.Events |= GetBlockEvents(contract);

                if (contract.Kind > ContractKind.DelegatorContract)
                {
                    var code = await ProcessCode(origination, Micheline.FromJson(content.Required("script").Required("code")));
                    var storage = Micheline.FromJson(content.Required("script").Required("storage"));

                    BigMapDiffs = ParseBigMapDiffs(origination, result, code, storage);
                    await ProcessScript(origination, content, code, storage);

                    origination.ContractCodeHash = contract.CodeHash;
                }
            }
            #endregion

            Proto.Manager.Set(origination.Sender);
            Db.OriginationOps.Add(origination);
            Origination = origination;
        }

        public virtual async Task ApplyInternal(Block block, TransactionOperation parent, JsonElement content)
        {
            #region init
            var sender = await Cache.Accounts.GetAsync(content.RequiredString("source"))
                ?? block.Originations?.FirstOrDefault(x => x.Contract.Address == content.RequiredString("source"))?.Contract
                    ?? throw new ValidationException("Origination source address doesn't exist");

            sender.Delegate ??= Cache.Accounts.GetDelegate(sender.DelegateId);

            // WTF: [level:25054] - Manager and sender are not equal.
            var manager = await GetManager(content);
            var delegat = Cache.Accounts.GetDelegateOrDefault(content.OptionalString("delegate"));
            // WTF: [level:635] - Tezos allows to set non-existent delegate.

            Db.TryAttach(sender);
            Db.TryAttach(sender.Delegate);
            Db.TryAttach(manager);
            Db.TryAttach(delegat);

            var result = content.Required("result");

            Contract contract = null;
            if (result.RequiredString("status") == "applied")
            {
                var address = result.RequiredArray("originated_contracts", 1)[0].RequiredString();
                var ghost = await Cache.Accounts.GetAsync(address);
                if (ghost != null)
                {
                    contract = new Contract
                    {
                        Id = ghost.Id,
                        FirstLevel = ghost.FirstLevel,
                        LastLevel = ghost.LastLevel,
                        Address = address,
                        Balance = content.RequiredInt64("balance"),
                        Counter = 0,
                        Delegate = delegat,
                        DelegationLevel = delegat != null ? block.Level : null,
                        WeirdDelegate = await GetWeirdDelegate(content),
                        Creator = sender,
                        Manager = manager,
                        Staked = delegat?.Staked ?? false,
                        Type = AccountType.Contract,
                        Kind = GetContractKind(content),
                        Spendable = GetSpendable(content),
                        ActiveTokensCount = ghost.ActiveTokensCount,
                        TokenBalancesCount = ghost.TokenBalancesCount,
                        TokenTransfersCount = ghost.TokenTransfersCount
                    };
                    Db.Entry(ghost).State = EntityState.Detached;
                    Db.Entry(contract).State = EntityState.Modified;
                }
                else
                {
                    contract = new Contract
                    {
                        Id = Cache.AppState.NextAccountId(),
                        FirstLevel = block.Level,
                        LastLevel = block.Level,
                        Address = address,
                        Balance = content.RequiredInt64("balance"),
                        Counter = 0,
                        Delegate = delegat,
                        DelegationLevel = delegat != null ? (int?)block.Level : null,
                        WeirdDelegate = await GetWeirdDelegate(content),
                        Creator = sender,
                        Manager = manager,
                        Staked = delegat?.Staked ?? false,
                        Type = AccountType.Contract,
                        Kind = GetContractKind(content),
                        Spendable = GetSpendable(content)
                    };
                    Db.Contracts.Add(contract);
                }
                Cache.Accounts.Add(contract);
            }

            var origination = new OriginationOperation
            {
                Id = Cache.AppState.NextOperationId(),
                Initiator = parent.Sender,
                Block = parent.Block,
                Level = parent.Block.Level,
                Timestamp = parent.Timestamp,
                OpHash = parent.OpHash,
                Counter = parent.Counter,
                Nonce = content.RequiredInt32("nonce"),
                Balance = content.RequiredInt64("balance"),
                Sender = sender,
                SenderCodeHash = (sender as Contract)?.CodeHash,
                Manager = manager,
                Delegate = delegat,
                Contract = contract,
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
                StorageUsed = result.OptionalInt32("paid_storage_size_diff") ?? 0,
                StorageFee = result.OptionalInt32("paid_storage_size_diff") > 0
                    ? result.OptionalInt32("paid_storage_size_diff") * block.Protocol.ByteCost
                    : null,
                AllocationFee = block.Protocol.OriginationSize * block.Protocol.ByteCost
            };
            #endregion

            #region entities
            var parentTx = parent;
            var parentSender = parentTx.Sender;
            var parentDelegate = parentSender.Delegate ?? parentSender as Data.Models.Delegate;

            var senderDelegate = sender.Delegate ?? sender as Data.Models.Delegate;

            var contractDelegate = origination.Delegate;
            var contractManager = origination.Manager;

            //Db.TryAttach(parentTx);
            //Db.TryAttach(parentSender);
            //Db.TryAttach(parentDelegate);

            //Db.TryAttach(sender);
            //Db.TryAttach(senderDelegate);

            //Db.TryAttach(contract);
            //Db.TryAttach(contractDelegate);
            //Db.TryAttach(contractManager);
            #endregion

            #region apply operation
            parentTx.InternalOperations = (short?)((parentTx.InternalOperations ?? 0) + 1);
            parentTx.InternalOriginations = (short?)((parentTx.InternalOriginations ?? 0) + 1);

            sender.OriginationsCount++;
            if (contractManager != null && contractManager != sender) contractManager.OriginationsCount++;
            if (contractDelegate != null && contractDelegate != sender && contractDelegate != contractManager) contractDelegate.OriginationsCount++;
            if (parentSender != sender && parentSender != contractDelegate && parentSender != contractManager) parentSender.OriginationsCount++;
            if (contract != null) contract.OriginationsCount++;

            block.Operations |= Operations.Originations;
            #endregion

            #region apply result
            if (origination.Status == OperationStatus.Applied)
            {
                var burned = (origination.StorageFee ?? 0) + (origination.AllocationFee ?? 0);
                Proto.Manager.Burn(burned);
                
                parentSender.Balance -= burned;
                if (parentDelegate != null)
                {
                    parentDelegate.StakingBalance -= burned;
                    if (parentDelegate.Id != parentSender.Id)
                        parentDelegate.DelegatedBalance -= burned;
                }

                sender.Balance -= origination.Balance;
                if (senderDelegate != null)
                {
                    senderDelegate.StakingBalance -= origination.Balance;
                    if (senderDelegate.Id != sender.Id)
                        senderDelegate.DelegatedBalance -= origination.Balance;
                }

                if (contractDelegate != null)
                {
                    contractDelegate.DelegatorsCount++;
                    contractDelegate.StakingBalance += contract.Balance;
                    contractDelegate.DelegatedBalance += contract.Balance;
                }

                sender.ContractsCount++;
                if (contractManager != null && contractManager != sender) contractManager.ContractsCount++;

                block.Events |= GetBlockEvents(contract);

                if (contract.Kind > ContractKind.DelegatorContract)
                {
                    var code = await ProcessCode(origination, Micheline.FromJson(content.Required("script").Required("code")));
                    var storage = Micheline.FromJson(content.Required("script").Required("storage"));

                    BigMapDiffs = ParseBigMapDiffs(origination, result, code, storage);
                    await ProcessScript(origination, content, code, storage);

                    origination.ContractCodeHash = contract.CodeHash;
                }
            }
            #endregion

            Db.OriginationOps.Add(origination);
            Origination = origination;
        }

        public virtual async Task Revert(Block block, OriginationOperation origination)
        {
            #region init
            origination.Block ??= block;
            origination.Block.Protocol ??= await Cache.Protocols.GetAsync(block.ProtoCode);
            origination.Block.Proposer ??= Cache.Accounts.GetDelegate(block.ProposerId);
            
            origination.Sender ??= await Cache.Accounts.GetAsync(origination.SenderId);
            origination.Sender.Delegate ??= Cache.Accounts.GetDelegate(origination.Sender.DelegateId);
            origination.Contract ??= (Contract)await Cache.Accounts.GetAsync(origination.ContractId);
            origination.Delegate ??= Cache.Accounts.GetDelegate(origination.DelegateId);
            origination.Manager ??= (User)await Cache.Accounts.GetAsync(origination.ManagerId);
            #endregion

            #region entities
            //var block = origination.Block;
            var blockBaker = block.Proposer;
            var sender = origination.Sender;
            var senderDelegate = sender.Delegate ?? sender as Data.Models.Delegate;
            var contract = origination.Contract;
            var contractDelegate = origination.Delegate;
            var contractManager = origination.Manager;

            //Db.TryAttach(block);
            Db.TryAttach(blockBaker);
            Db.TryAttach(sender);
            Db.TryAttach(senderDelegate);
            Db.TryAttach(contract);
            Db.TryAttach(contractDelegate);
            Db.TryAttach(contractManager);
            #endregion

            #region revert result
            if (origination.Status == OperationStatus.Applied)
            {
                var spent = origination.Balance + (origination.StorageFee ?? 0) + (origination.AllocationFee ?? 0);

                sender.Balance += spent;
                if (senderDelegate != null)
                {
                    senderDelegate.StakingBalance += spent;
                    if (senderDelegate.Id != sender.Id)
                        senderDelegate.DelegatedBalance += spent;
                }

                if (contractDelegate != null)
                {
                    contractDelegate.DelegatorsCount--;
                    contractDelegate.StakingBalance -= contract.Balance;
                    contractDelegate.DelegatedBalance -= contract.Balance;
                }

                sender.ContractsCount--;
                if (contractManager != null && contractManager != sender) contractManager.ContractsCount--;

                if (contract.Kind > ContractKind.DelegatorContract)
                    await RevertScript(origination);

                if (contract.TokenTransfersCount == 0)
                {
                    Db.Contracts.Remove(contract);
                    Cache.Accounts.Remove(contract);
                }
                else
                {
                    var ghost = new Account
                    {
                        Id = contract.Id,
                        Address = contract.Address,
                        FirstBlock = contract.FirstBlock,
                        FirstLevel = contract.FirstLevel,
                        LastLevel = contract.LastLevel,
                        ActiveTokensCount = contract.ActiveTokensCount,
                        TokenBalancesCount = contract.TokenBalancesCount,
                        TokenTransfersCount = contract.TokenTransfersCount,
                        Type = AccountType.Ghost,
                    };

                    Db.Entry(contract).State = EntityState.Detached;
                    Db.Entry(ghost).State = EntityState.Modified;
                    Cache.Accounts.Add(ghost);
                }
            }
            #endregion

            #region revert operation
            sender.Balance += origination.BakerFee;
            if (senderDelegate != null)
            {
                senderDelegate.StakingBalance += origination.BakerFee;
                if (senderDelegate.Id != sender.Id)
                    senderDelegate.DelegatedBalance += origination.BakerFee;
            }
            blockBaker.Balance -= origination.BakerFee;
            blockBaker.StakingBalance -= origination.BakerFee;

            sender.OriginationsCount--;
            if (contractManager != null && contractManager != sender) contractManager.OriginationsCount--;
            if (contractDelegate != null && contractDelegate != sender && contractDelegate != contractManager) contractDelegate.OriginationsCount--;

            sender.Counter = origination.Counter - 1;
            (sender as User).Revealed = true;
            #endregion

            Db.OriginationOps.Remove(origination);
            Cache.AppState.ReleaseManagerCounter();
            Cache.AppState.ReleaseOperationId();
        }

        public virtual async Task RevertInternal(Block block, OriginationOperation origination)
        {
            #region init
            origination.Block ??= block;
            origination.Block.Protocol ??= await Cache.Protocols.GetAsync(block.ProtoCode);
            origination.Block.Proposer ??= Cache.Accounts.GetDelegate(block.ProposerId);

            origination.Sender ??= await Cache.Accounts.GetAsync(origination.SenderId);
            origination.Sender.Delegate ??= Cache.Accounts.GetDelegate(origination.Sender.DelegateId);
            origination.Contract ??= (Contract)await Cache.Accounts.GetAsync(origination.ContractId);
            origination.Delegate ??= Cache.Accounts.GetDelegate(origination.DelegateId);
            origination.Manager ??= (User)await Cache.Accounts.GetAsync(origination.ManagerId);

            origination.Initiator = await Cache.Accounts.GetAsync(origination.InitiatorId);
            origination.Initiator.Delegate ??= Cache.Accounts.GetDelegate(origination.Initiator.DelegateId);
            #endregion

            #region entities
            var parentSender = origination.Initiator;
            var parentDelegate = parentSender.Delegate ?? parentSender as Data.Models.Delegate;

            var sender = origination.Sender;
            var senderDelegate = sender.Delegate ?? sender as Data.Models.Delegate;

            var contract = origination.Contract;
            var contractDelegate = origination.Delegate;
            var contractManager = origination.Manager;

            //Db.TryAttach(parentTx);
            Db.TryAttach(parentSender);
            Db.TryAttach(parentDelegate);

            Db.TryAttach(sender);
            Db.TryAttach(senderDelegate);

            Db.TryAttach(contract);
            Db.TryAttach(contractDelegate);
            Db.TryAttach(contractManager);
            #endregion

            #region revert result
            if (origination.Status == OperationStatus.Applied)
            {
                sender.Balance += origination.Balance;
                if (senderDelegate != null)
                {
                    senderDelegate.StakingBalance += origination.Balance;
                    if (senderDelegate.Id != sender.Id)
                        senderDelegate.DelegatedBalance += origination.Balance;
                }

                var spent = (origination.StorageFee ?? 0) + (origination.AllocationFee ?? 0);

                parentSender.Balance += spent;
                if (parentDelegate != null)
                {
                    parentDelegate.StakingBalance += spent;
                    if (parentDelegate.Id != parentSender.Id)
                        parentDelegate.DelegatedBalance += spent;
                }

                if (contractDelegate != null)
                {
                    contractDelegate.DelegatorsCount--;
                    contractDelegate.StakingBalance -= contract.Balance;
                    contractDelegate.DelegatedBalance -= contract.Balance;
                }

                sender.ContractsCount--;
                if (contractManager != null && contractManager != sender) contractManager.ContractsCount--;

                if (contract.Kind > ContractKind.DelegatorContract)
                    await RevertScript(origination);

                if (contract.TokenTransfersCount == 0)
                {
                    Db.Contracts.Remove(contract);
                    Cache.Accounts.Remove(contract);
                }
                else
                {
                    var ghost = new Account
                    {
                        Id = contract.Id,
                        Address = contract.Address,
                        FirstBlock = contract.FirstBlock,
                        FirstLevel = contract.FirstLevel,
                        LastLevel = contract.LastLevel,
                        ActiveTokensCount = contract.ActiveTokensCount,
                        TokenBalancesCount = contract.TokenBalancesCount,
                        TokenTransfersCount = contract.TokenTransfersCount,
                        Type = AccountType.Ghost,
                    };

                    Db.Entry(contract).State = EntityState.Detached;
                    Db.Entry(ghost).State = EntityState.Modified;
                    Cache.Accounts.Add(ghost);
                }
            }
            #endregion

            #region revert operation
            sender.OriginationsCount--;
            if (contractManager != null && contractManager != sender) contractManager.OriginationsCount--;
            if (contractDelegate != null && contractDelegate != sender && contractDelegate != contractManager) contractDelegate.OriginationsCount--;
            if (parentSender != sender && parentSender != contractDelegate && parentSender != contractManager) parentSender.OriginationsCount--;
            #endregion

            Db.OriginationOps.Remove(origination);
            Cache.AppState.ReleaseOperationId();
        }

        protected virtual async Task<User> GetWeirdDelegate(JsonElement content)
        {
            var originDelegate = await Cache.Accounts.GetAsync(content.OptionalString("delegate"));
            return originDelegate?.Type == AccountType.User ? (User)originDelegate : null;
        }

        protected virtual async Task<User> GetManager(JsonElement content)
        {
            // WTF: [level: 130] - Different nodes return different manager prop name.
            return (User)await Cache.Accounts.GetAsync(content.OptionalString("managerPubkey") ?? content.OptionalString("manager_pubkey"));
        }

        protected virtual ContractKind GetContractKind(JsonElement content)
        {
            return content.TryGetProperty("script", out var _)
                ? ContractKind.SmartContract
                : ContractKind.DelegatorContract;
        }

        protected virtual bool? GetSpendable(JsonElement content)
        {
            return content.TryGetProperty("spendable", out var s) && s.ValueKind == JsonValueKind.False ? (bool?)false : null;
        }

        protected virtual BlockEvents GetBlockEvents(Contract contract)
        {
            return contract.Kind == ContractKind.SmartContract
                ? BlockEvents.SmartContracts
                : BlockEvents.None;
        }

        protected virtual int GetConsumedGas(JsonElement result)
        {
            return result.OptionalInt32("consumed_gas") ?? 0;
        }

        protected async Task<MichelineArray> ProcessCode(OriginationOperation origination, IMicheline code)
        {
            if (code is not MichelineArray array)
            {
                var contract = origination.Contract;
                var constants = await Constants.Find(Db, new[] { code });
                if (constants.Count > 0)
                {
                    contract.Tags |= ContractTags.Constants;
                    foreach (var constant in constants)
                    {
                        Db.TryAttach(constant);
                        constant.Refs++;
                    }
                    var dict = constants.ToDictionary(x => x.Address, x => Micheline.FromBytes(x.Value));
                    array = Constants.Expand(code, dict) as MichelineArray
                        ?? throw new Exception("Contract code should be an array or constant");
                }
                else
                {
                    throw new Exception("Contract code should be an array or constant");
                }
            }
            return array;
        }

        protected async Task ProcessScript(OriginationOperation origination, JsonElement content, MichelineArray code, IMicheline storageValue)
        {
            var contract = origination.Contract;
            var micheParameter = code.First(x => x is MichelinePrim p && p.Prim == PrimType.parameter);
            var micheStorage = code.First(x => x is MichelinePrim p && p.Prim == PrimType.storage);
            var micheCode = code.First(x => x is MichelinePrim p && p.Prim == PrimType.code);
            var micheViews = code.Where(x => x is MichelinePrim p && p.Prim == PrimType.view);

            #region process constants
            var constants = await Constants.Find(Db, code);
            if (constants.Count > 0)
            {
                contract.Tags |= ContractTags.Constants;
                foreach (var constant in constants)
                {
                    Db.TryAttach(constant);
                    constant.Refs++;
                }
                var dict = constants.ToDictionary(x => x.Address, x => Micheline.FromBytes(x.Value));
                micheParameter = Constants.Expand(micheParameter, dict);
                micheStorage = Constants.Expand(micheStorage, dict);
                foreach (MichelinePrim view in micheViews)
                {
                    view.Args[1] = Constants.Expand(view.Args[1], dict);
                    view.Args[2] = Constants.Expand(view.Args[2], dict);
                }
            }
            #endregion

            var script = new Script
            {
                Id = Cache.AppState.NextScriptId(),
                Level = origination.Level,
                ContractId = contract.Id,
                OriginationId = origination.Id,
                ParameterSchema = micheParameter.ToBytes(),
                StorageSchema = micheStorage.ToBytes(),
                CodeSchema = micheCode.ToBytes(),
                Views = micheViews.Any()
                    ? micheViews.Select(x => x.ToBytes()).ToArray()
                    : null,
                Current = true
            };

            var viewsBytes = script.Views?
                .OrderBy(x => x, new BytesComparer())
                .SelectMany(x => x)
                .ToArray()
                ?? Array.Empty<byte>();
            var typeSchema = script.ParameterSchema.Concat(script.StorageSchema).Concat(viewsBytes);
            var fullSchema = typeSchema.Concat(script.CodeSchema);
            contract.TypeHash = script.TypeHash = Script.GetHash(typeSchema);
            contract.CodeHash = script.CodeHash = Script.GetHash(fullSchema);

            if (script.Schema.IsFA1())
            {
                if (script.Schema.IsFA12())
                    contract.Tags |= ContractTags.FA12;

                contract.Tags |= ContractTags.FA1;
                contract.Kind = ContractKind.Asset;
            }
            if (script.Schema.IsFA2())
            {
                contract.Tags |= ContractTags.FA2;
                contract.Kind = ContractKind.Asset;
            }

            if (BigMapDiffs != null)
            {
                var ind = 0;
                var ptrs = BigMapDiffs.Where(x => x.Action <= BigMapDiffAction.Copy && x.Ptr >= 0).Select(x => x.Ptr).ToList();
                var view = script.Schema.Storage.Schema.ToTreeView(storageValue);

                foreach (var bigmap in view.Nodes().Where(x => x.Schema.Prim == PrimType.big_map))
                    storageValue = storageValue.Replace(bigmap.Value, new MichelineInt(ptrs[^++ind]));
            }

            var storage = new Storage
            {
                Id = Cache.AppState.NextStorageId(),
                Level = origination.Level,
                ContractId = contract.Id,
                OriginationId = origination.Id,
                RawValue = script.Schema.OptimizeStorage(storageValue, false).ToBytes(),
                JsonValue = script.Schema.HumanizeStorage(storageValue),
                Current = true
            };

            Db.Scripts.Add(script);
            Cache.Schemas.Add(contract, script.Schema);

            Db.Storages.Add(storage);
            Cache.Storages.Add(contract, storage);

            origination.Script = script;
            origination.Storage = storage;
        }

        protected async Task RevertScript(OriginationOperation origination)
        {
            var contract = origination.Contract;

            #region process constants
            if (contract.Tags.HasFlag(ContractTags.Constants))
            {
                var script = await Db.Scripts
                    .AsNoTracking()
                    .Where(x => x.ContractId == contract.Id && x.Current)
                    .Select(x => new { x.ParameterSchema, x.StorageSchema, x.CodeSchema, x.Views })
                    .FirstAsync();

                var code = new MichelineArray
                {
                    Micheline.FromBytes(script.ParameterSchema),
                    Micheline.FromBytes(script.StorageSchema),
                    Micheline.FromBytes(script.CodeSchema)
                };
                if (script.Views != null)
                    foreach (var bytes in script.Views)
                        code.Add(Micheline.FromBytes(bytes));

                // TODO: we're actually missing constants in parameter and storage,
                // as they were expanded, so refs may be reverted inaccurately.
                var constants = await Constants.Find(Db, code);
                foreach (var constant in constants)
                {
                    Db.TryAttach(constant);
                    constant.Refs--;
                }
            }
            #endregion

            Db.Scripts.Remove(new Script { Id = (int)origination.ScriptId });
            Cache.Schemas.Remove(contract);
            Cache.AppState.ReleaseScriptId();

            var storage = await Cache.Storages.GetAsync(contract);
            Db.Storages.Remove(storage);
            Cache.Storages.Remove(contract);
            Cache.AppState.ReleaseStorageId();
        }

        protected virtual IEnumerable<BigMapDiff> ParseBigMapDiffs(OriginationOperation origination, JsonElement result, MichelineArray code, IMicheline storage)
        {
            List<BigMapDiff> res = null;

            var micheStorage = code.First(x => x is MichelinePrim p && p.Prim == PrimType.storage) as MichelinePrim;
            var schema = new StorageSchema(micheStorage);
            var tree = schema.Schema.ToTreeView(storage);
            var bigmap = tree.Nodes().FirstOrDefault(x => x.Schema.Prim == PrimType.big_map);

            if (bigmap != null)
            {
                res = new List<BigMapDiff>
                {
                    new AllocDiff { Ptr = origination.Contract.Id }
                };
                if (bigmap.Value is MichelineArray items && items.Count > 0)
                {
                    foreach (var item in items)
                    {
                        var key = (item as MichelinePrim).Args[0];
                        var value = (item as MichelinePrim).Args[1];
                        res.Add(new UpdateDiff
                        {
                            Ptr = res[0].Ptr,
                            Key = key,
                            Value = value,
                            KeyHash = (bigmap.Schema as BigMapSchema).GetKeyHash(key)
                        });
                    }
                }
            }

            return res;
        }
    }
}
