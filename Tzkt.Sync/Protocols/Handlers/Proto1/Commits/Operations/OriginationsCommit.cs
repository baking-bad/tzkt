using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Netezos.Contracts;
using Netezos.Encoding;

using Tzkt.Data.Models;
using Tzkt.Data.Models.Base;

namespace Tzkt.Sync.Protocols.Proto1
{
    class OriginationsCommit(ProtocolHandler protocol) : ProtocolCommit(protocol)
    {
        public OriginationOperation Origination { get; private set; } = null!;
        public IEnumerable<BigMapDiff>? BigMapDiffs { get; private set; }
        public Contract? Contract { get; private set; }

        public virtual async Task Apply(Block block, JsonElement op, JsonElement content)
        {
            #region init
            var sender = await Cache.Accounts.GetExistingAsync(content.RequiredString("source"));
            var senderDelegate = sender.DelegateId is int senderDelegateId
                ? await Cache.Accounts.GetAsync(senderDelegateId) as Data.Models.Delegate
                : sender as Data.Models.Delegate;
            var contractDelegate = content.OptionalString("delegate") is string _delegateAddress
                ? await Cache.Accounts.GetOrCreateAsync(_delegateAddress)
                : null;

            var result = content.Required("metadata").Required("operation_result");

            var origination = new OriginationOperation
            {
                Id = Cache.AppState.NextOperationId(),
                Level = block.Level,
                Timestamp = block.Timestamp,
                OpHash = op.RequiredString("hash"),
                Balance = content.RequiredInt64("balance"),
                BakerFee = content.RequiredInt64("fee"),
                Counter = content.RequiredInt32("counter"),
                GasLimit = content.RequiredInt32("gas_limit"),
                StorageLimit = content.RequiredInt32("storage_limit"),
                SenderId = sender.Id,
                DelegateId = contractDelegate?.Id,
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
                    ? result.OptionalInt32("paid_storage_size_diff") * Context.Protocol.ByteCost
                    : null,
                AllocationFee = Context.Protocol.OriginationSize * Context.Protocol.ByteCost
            };
            #endregion

            #region apply operation
            Db.TryAttach(sender);
            sender.LastLevel = origination.Level;
            sender.Balance -= origination.BakerFee;
            sender.Counter = origination.Counter;
            sender.OriginationsCount++;

            if (senderDelegate != null)
            {
                Db.TryAttach(senderDelegate);
                senderDelegate.LastLevel = origination.Level;
                senderDelegate.StakingBalance -= origination.BakerFee;
                if (senderDelegate != sender)
                    senderDelegate.DelegatedBalance -= origination.BakerFee;
            }

            if (contractDelegate != null)
            {
                Db.TryAttach(contractDelegate);
                contractDelegate.LastLevel = block.Level;
                if (contractDelegate != sender)
                    contractDelegate.OriginationsCount++;
            }

            Context.Proposer.Balance += origination.BakerFee;
            Context.Proposer.StakingBalance += origination.BakerFee;

            Context.Block.Operations |= Operations.Originations;
            Context.Block.Fees += origination.BakerFee;

            Cache.AppState.Get().OriginationOpsCount++;
            #endregion

            #region apply result
            if (origination.Status == OperationStatus.Applied)
            {
                var burned = (origination.StorageFee ?? 0) + (origination.AllocationFee ?? 0);
                var spent = origination.Balance + burned;
                Proto.Manager.Burn(burned);

                sender.Balance -= spent;
                sender.ContractsCount++;

                if (senderDelegate != null)
                {
                    senderDelegate.StakingBalance -= spent;
                    if (senderDelegate != sender)
                        senderDelegate.DelegatedBalance -= spent;
                }

                var _contractDelegate = contractDelegate as Data.Models.Delegate;
                if (_contractDelegate != null)
                {
                    _contractDelegate.DelegatorsCount++;
                    _contractDelegate.StakingBalance += origination.Balance;
                    _contractDelegate.DelegatedBalance += origination.Balance;
                }

                Contract? contract;
                var contractAddress = result.RequiredArray("originated_contracts", 1)[0].RequiredString();
                var ghost = await Cache.Accounts.GetAsync(contractAddress);
                if (ghost != null)
                {
                    contract = new Contract
                    {
                        Id = ghost.Id,
                        FirstLevel = ghost.FirstLevel,
                        LastLevel = origination.Level,
                        Address = contractAddress,
                        Balance = origination.Balance,
                        DelegateId = _contractDelegate?.Id,
                        DelegationLevel = _contractDelegate != null ? origination.Level : null,
                        CreatorId = sender.Id,
                        Staked = _contractDelegate?.Staked ?? false,
                        Type = AccountType.Contract,
                        Kind = ContractKind.SmartContract,
                        OriginationsCount = 1,
                        ActiveTokensCount = ghost.ActiveTokensCount,
                        TokenBalancesCount = ghost.TokenBalancesCount,
                        TokenTransfersCount = ghost.TokenTransfersCount,
                        ActiveTicketsCount = ghost.ActiveTicketsCount,
                        TicketBalancesCount = ghost.TicketBalancesCount,
                        TicketTransfersCount = ghost.TicketTransfersCount
                    };
                    Db.Entry(ghost).State = EntityState.Detached;
                    Db.Entry(contract).State = EntityState.Modified;
                }
                else
                {
                    contract = new Contract
                    {
                        Id = Cache.AppState.NextAccountId(),
                        FirstLevel = origination.Level,
                        LastLevel = origination.Level,
                        Address = contractAddress,
                        Balance = origination.Balance,
                        DelegateId = _contractDelegate?.Id,
                        DelegationLevel = _contractDelegate != null ? origination.Level : null,
                        CreatorId = sender.Id,
                        Staked = _contractDelegate?.Staked ?? false,
                        Type = AccountType.Contract,
                        Kind = ContractKind.SmartContract,
                        OriginationsCount = 1
                    };
                    Db.Contracts.Add(contract);
                }
                Cache.Accounts.Add(contract);
                origination.ContractId = contract.Id;
                Contract = contract;

                var code = await ExpandCode(contract, GetCode(content));
                var storage = GetStorage(content);

                BigMapDiffs = ParseBigMapDiffs(origination, result, code, storage);
                await ProcessScript(origination, contract, code, storage);

                Cache.Statistics.Current.TotalBurned += burned;
            }
            #endregion

            Proto.Manager.Set(sender);
            Db.OriginationOps.Add(origination);
            Context.OriginationOps.Add(origination);
            Origination = origination;
        }

        public virtual async Task ApplyInternal(Block block, ManagerOperation parent, JsonElement content)
        {
            #region init
            var initiator = await Cache.Accounts.GetAsync(parent.SenderId);
            var initiatorDelegate = initiator.DelegateId is int initiatorDelegateId
                ? await Cache.Accounts.GetAsync(initiatorDelegateId) as Data.Models.Delegate
                : initiator as Data.Models.Delegate;
            var sender = await Cache.Accounts.GetExistingAsync(content.RequiredString("source"));
            var senderDelegate = sender.DelegateId is int senderDelegateId
                ? await Cache.Accounts.GetAsync(senderDelegateId) as Data.Models.Delegate
                : null;
            var contractDelegate = content.OptionalString("delegate") is string _delegateAddress
                ? await Cache.Accounts.GetOrCreateAsync(_delegateAddress)
                : null;

            var result = content.Required("result");

            var origination = new OriginationOperation
            {
                Id = Cache.AppState.NextOperationId(),
                InitiatorId = parent.SenderId,
                Level = parent.Level,
                Timestamp = parent.Timestamp,
                OpHash = parent.OpHash,
                Counter = parent.Counter,
                Nonce = content.RequiredInt32("nonce"),
                Balance = content.RequiredInt64("balance"),
                SenderId = sender.Id,
                SenderCodeHash = (sender as Contract)?.CodeHash,
                DelegateId = contractDelegate?.Id,
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
                    ? result.OptionalInt32("paid_storage_size_diff") * Context.Protocol.ByteCost
                    : null,
                AllocationFee = Context.Protocol.OriginationSize * Context.Protocol.ByteCost
            };
            #endregion

            #region apply operation
            if (parent is TransactionOperation parentTx)
            {
                parentTx.InternalOperations = (short?)((parentTx.InternalOperations ?? 0) + 1);
                parentTx.InternalOriginations = (short?)((parentTx.InternalOriginations ?? 0) + 1);
            }

            Db.TryAttach(sender);
            sender.LastLevel = block.Level;
            sender.OriginationsCount++;

            if (contractDelegate != null)
            {
                Db.TryAttach(contractDelegate);
                contractDelegate.LastLevel = block.Level;
                if (contractDelegate != sender)
                    contractDelegate.OriginationsCount++;
            }

            if (initiator != sender && initiator != contractDelegate)
            {
                initiator.OriginationsCount++;
            }

            block.Operations |= Operations.Originations;

            Cache.AppState.Get().OriginationOpsCount++;
            #endregion

            #region apply result
            if (origination.Status == OperationStatus.Applied)
            {
                var burned = (origination.StorageFee ?? 0) + (origination.AllocationFee ?? 0);
                Proto.Manager.Burn(burned);

                initiator.Balance -= burned;

                if (initiatorDelegate != null)
                {
                    initiatorDelegate.StakingBalance -= burned;
                    if (initiatorDelegate != initiator)
                        initiatorDelegate.DelegatedBalance -= burned;
                }

                sender.Balance -= origination.Balance;
                sender.ContractsCount++;

                if (senderDelegate != null)
                {
                    senderDelegate.StakingBalance -= origination.Balance;
                    if (senderDelegate != sender)
                        senderDelegate.DelegatedBalance -= origination.Balance;
                }

                var _contractDelegate = contractDelegate as Data.Models.Delegate;
                if (_contractDelegate != null)
                {
                    _contractDelegate.DelegatorsCount++;
                    _contractDelegate.StakingBalance += origination.Balance;
                    _contractDelegate.DelegatedBalance += origination.Balance;
                }

                Contract? contract;
                var contractAddress = result.RequiredArray("originated_contracts", 1)[0].RequiredString();
                var ghost = await Cache.Accounts.GetAsync(contractAddress);
                if (ghost != null)
                {
                    contract = new Contract
                    {
                        Id = ghost.Id,
                        FirstLevel = ghost.FirstLevel,
                        LastLevel = origination.Level,
                        Address = contractAddress,
                        Balance = content.RequiredInt64("balance"),
                        Counter = 0,
                        DelegateId = _contractDelegate?.Id,
                        DelegationLevel = _contractDelegate != null ? origination.Level : null,
                        CreatorId = sender.Id,
                        Staked = _contractDelegate?.Staked ?? false,
                        Type = AccountType.Contract,
                        Kind = ContractKind.SmartContract,
                        OriginationsCount = 1,
                        ActiveTokensCount = ghost.ActiveTokensCount,
                        TokenBalancesCount = ghost.TokenBalancesCount,
                        TokenTransfersCount = ghost.TokenTransfersCount,
                        ActiveTicketsCount = ghost.ActiveTicketsCount,
                        TicketBalancesCount = ghost.TicketBalancesCount,
                        TicketTransfersCount = ghost.TicketTransfersCount
                    };
                    Db.Entry(ghost).State = EntityState.Detached;
                    Db.Entry(contract).State = EntityState.Modified;
                }
                else
                {
                    contract = new Contract
                    {
                        Id = Cache.AppState.NextAccountId(),
                        FirstLevel = origination.Level,
                        LastLevel = origination.Level,
                        Address = contractAddress,
                        Balance = content.RequiredInt64("balance"),
                        Counter = 0,
                        DelegateId = _contractDelegate?.Id,
                        DelegationLevel = _contractDelegate != null ? origination.Level : null,
                        CreatorId = sender.Id,
                        Staked = _contractDelegate?.Staked ?? false,
                        Type = AccountType.Contract,
                        Kind = ContractKind.SmartContract,
                        OriginationsCount = 1
                    };
                    Db.Contracts.Add(contract);
                }
                Cache.Accounts.Add(contract);
                origination.ContractId = contract.Id;
                Contract = contract;

                var code = await ExpandCode(contract, GetCode(content));
                var storage = GetStorage(content);

                BigMapDiffs = ParseBigMapDiffs(origination, result, code, storage);
                await ProcessScript(origination, contract, code, storage);

                Cache.Statistics.Current.TotalBurned += burned;
            }
            #endregion

            Db.OriginationOps.Add(origination);
            Context.OriginationOps.Add(origination);
            Origination = origination;
        }

        public virtual async Task Revert(Block block, OriginationOperation origination)
        {
            #region init
            var sender = await Cache.Accounts.GetAsync(origination.SenderId);
            var senderDelegate = sender.DelegateId is int senderDelegateId
                ? await Cache.Accounts.GetAsync(senderDelegateId) as Data.Models.Delegate
                : sender as Data.Models.Delegate;
            var contractDelegate = origination.DelegateId is int delegateId
                ? await Cache.Accounts.GetAsync(delegateId)
                : null;
            var contract = origination.ContractId is int contractId
                ? await Cache.Accounts.GetAsync(contractId) as Contract
                : null;

            Db.TryAttach(sender);
            Db.TryAttach(senderDelegate);
            Db.TryAttach(contractDelegate);
            Db.TryAttach(contract);
            Db.TryAttach(Context.Proposer);
            #endregion

            #region revert result
            if (origination.Status == OperationStatus.Applied)
            {
                var spent = origination.Balance + (origination.StorageFee ?? 0) + (origination.AllocationFee ?? 0);

                sender.Balance += spent;
                sender.ContractsCount--;

                if (senderDelegate != null)
                {
                    senderDelegate.StakingBalance += spent;
                    if (senderDelegate != sender)
                        senderDelegate.DelegatedBalance += spent;
                }

                if (contractDelegate is Data.Models.Delegate _contractDelegate)
                {
                    _contractDelegate.DelegatorsCount--;
                    _contractDelegate.StakingBalance -= origination.Balance;
                    _contractDelegate.DelegatedBalance -= origination.Balance;
                }

                await RevertScript(origination, contract!);

                contract!.OriginationsCount--;
                if (contract.TokenTransfersCount == 0 && contract.TicketTransfersCount == 0)
                {
                    Db.Accounts.Remove(contract);
                    Cache.Accounts.Remove(contract);
                }
                else
                {
                    var ghost = new Account
                    {
                        Id = contract.Id,
                        Address = contract.Address,
                        FirstLevel = contract.FirstLevel,
                        LastLevel = origination.Level,
                        Type = AccountType.Ghost,
                        ActiveTokensCount = contract.ActiveTokensCount,
                        TokenBalancesCount = contract.TokenBalancesCount,
                        TokenTransfersCount = contract.TokenTransfersCount,
                        ActiveTicketsCount = contract.ActiveTicketsCount,
                        TicketBalancesCount = contract.TicketBalancesCount,
                        TicketTransfersCount = contract.TicketTransfersCount,
                    };

                    Db.Entry(contract).State = EntityState.Detached;
                    Db.Entry(ghost).State = EntityState.Modified;
                    Cache.Accounts.Add(ghost);
                }
            }
            #endregion

            #region revert operation
            sender.LastLevel = block.Level;
            sender.Balance += origination.BakerFee;
            sender.Counter = origination.Counter - 1;
            if (sender is User user) user.Revealed = true;
            sender.OriginationsCount--;

            if (senderDelegate != null)
            {
                senderDelegate.LastLevel = block.Level;
                senderDelegate.StakingBalance += origination.BakerFee;
                if (senderDelegate != sender)
                    senderDelegate.DelegatedBalance += origination.BakerFee;
            }

            if (contractDelegate != null)
            {
                contractDelegate.LastLevel = block.Level;
                if (contractDelegate != sender)
                    contractDelegate.OriginationsCount--;
            }

            Context.Proposer.Balance -= origination.BakerFee;
            Context.Proposer.StakingBalance -= origination.BakerFee;

            Cache.AppState.Get().OriginationOpsCount--;
            #endregion

            Db.OriginationOps.Remove(origination);
            Cache.AppState.ReleaseManagerCounter();
            Cache.AppState.ReleaseOperationId();
        }

        public virtual async Task RevertInternal(Block block, OriginationOperation origination)
        {
            #region init
            var initiator = await Cache.Accounts.GetAsync(origination.InitiatorId!.Value);
            var initiatorDelegate = initiator.DelegateId is int initiatorDelegateId
                ? await Cache.Accounts.GetAsync(initiatorDelegateId) as Data.Models.Delegate
                : initiator as Data.Models.Delegate;
            var sender = await Cache.Accounts.GetAsync(origination.SenderId);
            var senderDelegate = sender.DelegateId is int senderDelegateId
                ? await Cache.Accounts.GetAsync(senderDelegateId) as Data.Models.Delegate
                : null;
            var contractDelegate = origination.DelegateId is int delegateId
                ? await Cache.Accounts.GetAsync(delegateId)
                : null;
            var contract = origination.ContractId is int contractId
                ? await Cache.Accounts.GetAsync(contractId) as Contract
                : null;

            Db.TryAttach(initiator);
            Db.TryAttach(initiatorDelegate);
            Db.TryAttach(sender);
            Db.TryAttach(senderDelegate);
            Db.TryAttach(contractDelegate);
            Db.TryAttach(contract);
            #endregion

            #region revert result
            if (origination.Status == OperationStatus.Applied)
            {
                var spent = (origination.StorageFee ?? 0) + (origination.AllocationFee ?? 0);

                initiator.Balance += spent;

                if (initiatorDelegate != null)
                {
                    initiatorDelegate.StakingBalance += spent;
                    if (initiatorDelegate != initiator)
                        initiatorDelegate.DelegatedBalance += spent;
                }

                sender.Balance += origination.Balance;
                sender.ContractsCount--;

                if (senderDelegate != null)
                {
                    senderDelegate.StakingBalance += origination.Balance;
                    if (senderDelegate != sender)
                        senderDelegate.DelegatedBalance += origination.Balance;
                }

                if (contractDelegate is Data.Models.Delegate _contractDelegate)
                {
                    _contractDelegate.DelegatorsCount--;
                    _contractDelegate.StakingBalance -= contract!.Balance;
                    _contractDelegate.DelegatedBalance -= contract.Balance;
                }

                await RevertScript(origination, contract!);

                contract!.OriginationsCount--;
                if (contract.TokenTransfersCount == 0 && contract.TicketTransfersCount == 0)
                {
                    Db.Accounts.Remove(contract);
                    Cache.Accounts.Remove(contract);
                }
                else
                {
                    var ghost = new Account
                    {
                        Id = contract.Id,
                        Address = contract.Address,
                        FirstLevel = contract.FirstLevel,
                        LastLevel = origination.Level,
                        Type = AccountType.Ghost,
                        ActiveTokensCount = contract.ActiveTokensCount,
                        TokenBalancesCount = contract.TokenBalancesCount,
                        TokenTransfersCount = contract.TokenTransfersCount,
                        ActiveTicketsCount = contract.ActiveTicketsCount,
                        TicketBalancesCount = contract.TicketBalancesCount,
                        TicketTransfersCount = contract.TicketTransfersCount,
                    };

                    Db.Entry(contract).State = EntityState.Detached;
                    Db.Entry(ghost).State = EntityState.Modified;
                    Cache.Accounts.Add(ghost);
                }
            }
            #endregion

            #region revert operation
            sender.LastLevel = block.Level;
            sender.OriginationsCount--;

            if (contractDelegate != null)
            {
                contractDelegate.LastLevel = block.Level;
                if (contractDelegate != sender)
                    contractDelegate.OriginationsCount--;
            }

            if (initiator != sender && initiator != contractDelegate)
            {
                initiator.OriginationsCount--;
            }

            Cache.AppState.Get().OriginationOpsCount--;
            #endregion

            Db.OriginationOps.Remove(origination);
            Cache.AppState.ReleaseOperationId();
        }

        protected virtual int GetConsumedGas(JsonElement result)
        {
            return result.OptionalInt32("consumed_gas") ?? 0;
        }

        protected virtual IMicheline GetCode(JsonElement content)
        {
            return content.TryGetProperty("script", out var script)
                ? Micheline.FromJson(script.Required("code"))!
                // WTF: Before Proto5 some contracts had no code nor storage
                : Micheline.FromBytes(Script.ManagerTzBytes);
        }

        protected virtual IMicheline GetStorage(JsonElement content)
        {
            return content.TryGetProperty("script", out var script)
                ? Micheline.FromJson(script.Required("storage"))!
                // WTF: Different nodes return different manager prop name.
                : new MichelineString(content.OptionalString("managerPubkey") ?? content.RequiredString("manager_pubkey"));
        }

        protected async Task<MichelineArray> ExpandCode(Contract contract, IMicheline code)
        {
            if (code is not MichelineArray array)
            {
                var constants = await Constants.Find(Db, [code]);
                if (constants.Count > 0)
                {
                    contract.Tags |= ContractTags.Constants;
                    foreach (var constant in constants)
                    {
                        Db.TryAttach(constant);
                        constant.Refs++;
                    }
                    var dict = constants.ToDictionary(x => x.Address!, x => Micheline.FromBytes(x.Value!));
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

        protected async Task ProcessScript(OriginationOperation origination, Contract contract, MichelineArray code, IMicheline storageValue)
        {
            #region expand top-level constants
            var constants = await Constants.Find(Db, code);
            if (constants.Count > 0)
            {
                var depth = 0;
                while (code.Any(x => x is MichelinePrim prim && prim.Prim == PrimType.constant) && depth++ <= 10_000)
                {
                    for (int i = 0; i < code.Count; i++)
                    {
                        if (code[i] is MichelinePrim prim && prim.Prim == PrimType.constant)
                        {
                            code[i] = Micheline.FromBytes(constants.First(x => x.Address == (prim.Args![0] as MichelineString)!.Value).Value!);
                        }
                    }
                }
            }
            #endregion

            var micheParameter = code.First(x => x is MichelinePrim p && p.Prim == PrimType.parameter);
            var micheStorage = code.First(x => x is MichelinePrim p && p.Prim == PrimType.storage);
            var micheCode = code.First(x => x is MichelinePrim p && p.Prim == PrimType.code);
            var micheViews = code.Where(x => x is MichelinePrim p && p.Prim == PrimType.view);

            #region process constants
            if (constants.Count > 0)
            {
                contract.Tags |= ContractTags.Constants;
                foreach (var constant in constants)
                {
                    Db.TryAttach(constant);
                    constant.Refs++;
                }
                var dict = constants.ToDictionary(x => x.Address!, x => Micheline.FromBytes(x.Value!));
                micheParameter = Constants.Expand(micheParameter, dict);
                micheStorage = Constants.Expand(micheStorage, dict);
                foreach (var view in micheViews.Select(x => (x as MichelinePrim)!))
                {
                    view.Args![1] = Constants.Expand(view.Args[1], dict);
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
                ?? [];
            var typeSchema = script.ParameterSchema.Concat(script.StorageSchema).Concat(viewsBytes);
            var fullSchema = typeSchema.Concat(script.CodeSchema);
            contract.TypeHash = script.TypeHash = Script.GetHash(typeSchema);
            origination.ContractCodeHash = contract.CodeHash = script.CodeHash = Script.GetHash(fullSchema);

            if ((storageValue.Type == MichelineType.String || storageValue.Type == MichelineType.Bytes) &&
                code.ToBytes().IsEqual(Script.ManagerTzBytes))
            {
                contract.Kind = ContractKind.DelegatorContract;
            }
            else
            {
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

            origination.ScriptId = script.Id;
            origination.StorageId = storage.Id;
        }

        protected async Task RevertScript(OriginationOperation origination, Contract contract)
        {
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

            Db.Scripts.Remove(new Script
            {
                Id = origination.ScriptId!.Value,
                ParameterSchema = [],
                StorageSchema = [],
                CodeSchema = [],
                Level = 0,
                ContractId = 0,
            });
            Cache.Schemas.Remove(contract);
            Cache.AppState.ReleaseScriptId();

            Db.Storages.Remove(new Storage
            {
                Id = origination.StorageId!.Value,
                RawValue = [],
                JsonValue = string.Empty,
                Level = 0,
                ContractId = 0,
            });
            Cache.Storages.Remove(contract);
            Cache.AppState.ReleaseStorageId();
        }

        protected virtual IEnumerable<BigMapDiff>? ParseBigMapDiffs(OriginationOperation origination, JsonElement result, MichelineArray code, IMicheline storage)
        {
            List<BigMapDiff>? res = null;

            var micheStorage = (code.First(x => x is MichelinePrim p && p.Prim == PrimType.storage) as MichelinePrim)!;
            var schema = new StorageSchema(micheStorage);
            var tree = schema.Schema.ToTreeView(storage);
            var bigmap = tree.Nodes().FirstOrDefault(x => x.Schema.Prim == PrimType.big_map);

            if (bigmap != null)
            {
                res =
                [
                    new AllocDiff { Ptr = origination.ContractId!.Value }
                ];
                if (bigmap.Value is MichelineArray items && items.Count > 0)
                {
                    foreach (var item in items)
                    {
                        var key = (item as MichelinePrim)!.Args![0];
                        var value = (item as MichelinePrim)!.Args![1];
                        res.Add(new UpdateDiff
                        {
                            Ptr = res[0].Ptr,
                            Key = key,
                            Value = value,
                            KeyHash = (bigmap.Schema as BigMapSchema)!.GetKeyHash(key)
                        });
                    }
                }
            }

            return res;
        }
    }
}
