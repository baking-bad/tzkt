using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
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

            var result = content.Required("metadata").Required("operation_result");

            var contract = result.RequiredString("status") == "applied" ?
                new Contract
                {
                    Id = Cache.AppState.NextAccountId(),
                    Address = result.RequiredArray("originated_contracts", 1)[0].RequiredString(),
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
                }
                : null;

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
                    ? OperationErrors.Parse(errors)
                    : null,
                GasUsed = result.OptionalInt32("consumed_gas") ?? 0,
                StorageUsed = result.OptionalInt32("paid_storage_size_diff") ?? 0,
                StorageFee = (result.OptionalInt32("paid_storage_size_diff") ?? 0) * block.Protocol.ByteCost,
                AllocationFee = block.Protocol.OriginationSize * block.Protocol.ByteCost
            };
            #endregion

            #region entities
            //var block = origination.Block;
            var blockBaker = block.Baker;
            //var sender = origination.Sender;
            var senderDelegate = sender.Delegate ?? sender as Data.Models.Delegate;
            //var contract = origination.Contract;
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

            #region apply operation
            await Spend(sender, origination.BakerFee);
            if (senderDelegate != null) senderDelegate.StakingBalance -= origination.BakerFee;
            blockBaker.FrozenFees += origination.BakerFee;
            blockBaker.Balance += origination.BakerFee;
            blockBaker.StakingBalance += origination.BakerFee;

            sender.OriginationsCount++;
            if (contractManager != null && contractManager != sender) contractManager.OriginationsCount++;
            if (contractDelegate != null && contractDelegate != sender && contractDelegate != contractManager) contractDelegate.OriginationsCount++;
            if (contract != null) contract.OriginationsCount++;

            block.Operations |= Operations.Originations;
            block.Fees += origination.BakerFee;

            sender.Counter = Math.Max(sender.Counter, origination.Counter);
            #endregion

            #region apply result
            if (origination.Status == OperationStatus.Applied)
            {
                await Spend(sender,
                    origination.Balance +
                    (origination.StorageFee ?? 0) +
                    (origination.AllocationFee ?? 0));

                if (senderDelegate != null)
                {
                    senderDelegate.StakingBalance -= origination.Balance;
                    senderDelegate.StakingBalance -= origination.StorageFee ?? 0;
                    senderDelegate.StakingBalance -= origination.AllocationFee ?? 0;
                }

                if (contractDelegate != null)
                {
                    contractDelegate.DelegatorsCount++;
                    contractDelegate.StakingBalance += contract.Balance;
                }

                sender.ContractsCount++;
                if (contractManager != null && contractManager != sender) contractManager.ContractsCount++;

                block.Events |= GetBlockEvents(contract);

                Db.Contracts.Add(contract);

                if (contract.Kind > ContractKind.DelegatorContract)
                {
                    var code = Micheline.FromJson(content.Required("script").Required("code")) as MichelineArray;
                    var storage = Micheline.FromJson(content.Required("script").Required("storage"));

                    BigMapDiffs = ParseBigMapDiffs(origination, result, code, storage);
                    ProcessScript(origination, content, code, storage);
                }
            }
            #endregion

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

            var result = content.Required("result");

            var contract = result.RequiredString("status") == "applied" ?
                new Contract
                {
                    Id = Cache.AppState.NextAccountId(),
                    Address = result.RequiredArray("originated_contracts", 1)[0].RequiredString(),
                    Balance = content.RequiredInt64("balance"),
                    Counter = 0,
                    Delegate = delegat,
                    DelegationLevel = delegat != null ? (int?)block.Level : null,
                    WeirdDelegate = await GetWeirdDelegate(content),
                    Creator = sender,
                    Manager = manager,
                    Staked = delegat?.Staked ?? false,
                    Type = AccountType.Contract,
                    Kind = manager == null && content.TryGetProperty("script", out var _) ? ContractKind.SmartContract : ContractKind.DelegatorContract,
                    Spendable = GetSpendable(content)
                }
                : null;

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
                    ? OperationErrors.Parse(errors)
                    : null,
                GasUsed = result.OptionalInt32("consumed_gas") ?? 0,
                StorageUsed = result.OptionalInt32("paid_storage_size_diff") ?? 0,
                StorageFee = (result.OptionalInt32("paid_storage_size_diff") ?? 0) * block.Protocol.ByteCost,
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

            Db.TryAttach(sender);
            Db.TryAttach(senderDelegate);

            Db.TryAttach(contract);
            Db.TryAttach(contractDelegate);
            Db.TryAttach(contractManager);
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
                await Spend(sender, origination.Balance);

                if (senderDelegate != null)
                {
                    senderDelegate.StakingBalance -= origination.Balance;
                }

                await Spend(parentSender,
                    (origination.StorageFee ?? 0) +
                    (origination.AllocationFee ?? 0));

                if (parentDelegate != null)
                {
                    parentDelegate.StakingBalance -= origination.StorageFee ?? 0;
                    parentDelegate.StakingBalance -= origination.AllocationFee ?? 0;
                }

                if (contractDelegate != null)
                {
                    contractDelegate.DelegatorsCount++;
                    contractDelegate.StakingBalance += contract.Balance;
                }

                sender.ContractsCount++;
                if (contractManager != null && contractManager != sender) contractManager.ContractsCount++;

                block.Events |= GetBlockEvents(contract);

                Db.Contracts.Add(contract);

                if (contract.Kind > ContractKind.DelegatorContract)
                {
                    var code = Micheline.FromJson(content.Required("script").Required("code")) as MichelineArray;
                    var storage = Micheline.FromJson(content.Required("script").Required("storage"));

                    BigMapDiffs = ParseBigMapDiffs(origination, result, code, storage);
                    ProcessScript(origination, content, code, storage);
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
            origination.Block.Baker ??= Cache.Accounts.GetDelegate(block.BakerId);
            
            origination.Sender ??= await Cache.Accounts.GetAsync(origination.SenderId);
            origination.Sender.Delegate ??= Cache.Accounts.GetDelegate(origination.Sender.DelegateId);
            origination.Contract ??= (Contract)await Cache.Accounts.GetAsync(origination.ContractId);
            origination.Delegate ??= Cache.Accounts.GetDelegate(origination.DelegateId);
            origination.Manager ??= (User)await Cache.Accounts.GetAsync(origination.ManagerId);
            #endregion

            #region entities
            //var block = origination.Block;
            var blockBaker = block.Baker;
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
                await Return(sender,
                    origination.Balance +
                    (origination.StorageFee ?? 0) +
                    (origination.AllocationFee ?? 0));

                if (senderDelegate != null)
                {
                    senderDelegate.StakingBalance += origination.Balance;
                    senderDelegate.StakingBalance += origination.StorageFee ?? 0;
                    senderDelegate.StakingBalance += origination.AllocationFee ?? 0;
                }

                if (contractDelegate != null)
                {
                    contractDelegate.DelegatorsCount--;
                    contractDelegate.StakingBalance -= contract.Balance;
                }

                sender.ContractsCount--;
                if (contractManager != null && contractManager != sender) contractManager.ContractsCount--;

                if (contract.Kind > ContractKind.DelegatorContract)
                    await RevertScript(origination);

                Db.Contracts.Remove(contract);
                Cache.Accounts.Remove(contract);
            }
            #endregion

            #region revert operation
            await Return(sender, origination.BakerFee);
            if (senderDelegate != null) senderDelegate.StakingBalance += origination.BakerFee;
            blockBaker.FrozenFees -= origination.BakerFee;
            blockBaker.Balance -= origination.BakerFee;
            blockBaker.StakingBalance -= origination.BakerFee;

            sender.OriginationsCount--;
            if (contractManager != null && contractManager != sender) contractManager.OriginationsCount--;
            if (contractDelegate != null && contractDelegate != sender && contractDelegate != contractManager) contractDelegate.OriginationsCount--;

            sender.Counter = Math.Min(sender.Counter, origination.Counter - 1);
            #endregion

            Db.OriginationOps.Remove(origination);
            Cache.AppState.ReleaseManagerCounter();
        }

        public virtual async Task RevertInternal(Block block, OriginationOperation origination)
        {
            #region init
            origination.Block ??= block;
            origination.Block.Protocol ??= await Cache.Protocols.GetAsync(block.ProtoCode);
            origination.Block.Baker ??= Cache.Accounts.GetDelegate(block.BakerId);

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
                await Return(sender, origination.Balance);

                if (senderDelegate != null)
                {
                    senderDelegate.StakingBalance += origination.Balance;
                }

                await Return(parentSender,
                    (origination.StorageFee ?? 0) +
                    (origination.AllocationFee ?? 0));

                if (parentDelegate != null)
                {
                    parentDelegate.StakingBalance += origination.StorageFee ?? 0;
                    parentDelegate.StakingBalance += origination.AllocationFee ?? 0;
                }

                if (contractDelegate != null)
                {
                    contractDelegate.DelegatorsCount--;
                    contractDelegate.StakingBalance -= contract.Balance;
                }

                sender.ContractsCount--;
                if (contractManager != null && contractManager != sender) contractManager.ContractsCount--;

                if (contract.Kind > ContractKind.DelegatorContract)
                    await RevertScript(origination);

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

            Db.OriginationOps.Remove(origination);
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

        protected void ProcessScript(OriginationOperation origination, JsonElement content, MichelineArray code, IMicheline storageValue)
        {
            var contract = origination.Contract;
            var micheParameter = code.First(x => x is MichelinePrim p && p.Prim == PrimType.parameter);
            var micheStorage = code.First(x => x is MichelinePrim p && p.Prim == PrimType.storage);
            var micheCode = code.First(x => x is MichelinePrim p && p.Prim == PrimType.code);
            var script = new Script
            {
                Id = Cache.AppState.NextScriptId(),
                Level = origination.Level,
                ContractId = contract.Id,
                OriginationId = origination.Id,
                ParameterSchema = micheParameter.ToBytes(),
                StorageSchema = micheStorage.ToBytes(),
                CodeSchema = micheCode.ToBytes(),
                Current = true
            };

            var typeSchema = script.ParameterSchema.Concat(script.StorageSchema);
            var fullSchema = typeSchema.Concat(script.CodeSchema);
            contract.TypeHash = script.TypeHash = Script.GetHash(typeSchema);
            contract.CodeHash = script.CodeHash = Script.GetHash(fullSchema);

            contract.Tzips = Tzip.None;
            if (script.Schema.IsFA1())
            {
                if (script.Schema.IsFA12())
                    contract.Tzips |= Tzip.FA12;

                contract.Tzips |= Tzip.FA1;
                contract.Kind = ContractKind.Asset;
            }
            if (script.Schema.IsFA2())
            {
                contract.Tzips |= Tzip.FA2;
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

            Db.Scripts.Remove(new Script { Id = (int)origination.ScriptId });
            Cache.Schemas.Remove(contract);

            var storage = await Cache.Storages.GetAsync(contract);
            Db.Storages.Remove(storage);
            Cache.Storages.Remove(contract);
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
