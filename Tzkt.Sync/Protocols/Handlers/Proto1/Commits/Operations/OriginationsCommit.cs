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
            // WTF: [level:25054] - Manager and sender are not equal.
            var manager = await GetManager(content);
            // WTF: [level:635] - Tezos allows to set non-existent delegate.
            var delegat = Cache.Accounts.GetDelegateOrDefault(content.OptionalString("delegate"));

            Db.TryAttach(sender);
            Db.TryAttach(manager);
            Db.TryAttach(delegat);

            var result = content.Required("metadata").Required("operation_result");

            Contract? contract = null;
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
                        DelegateId = delegat?.Id,
                        DelegationLevel = delegat != null ? block.Level : null,
                        WeirdDelegateId = (await GetWeirdDelegate(content))?.Id,
                        CreatorId = sender.Id,
                        ManagerId = manager?.Id,
                        Staked = delegat?.Staked ?? false,
                        Type = AccountType.Contract,
                        Kind = GetContractKind(content),
                        Spendable = GetSpendable(content),
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
                        FirstLevel = block.Level,
                        LastLevel = block.Level,
                        Address = address,
                        Balance = content.RequiredInt64("balance"),
                        Counter = 0,
                        DelegateId = delegat?.Id,
                        DelegationLevel = delegat != null ? block.Level : null,
                        WeirdDelegateId = (await GetWeirdDelegate(content))?.Id,
                        CreatorId = sender.Id,
                        ManagerId = manager?.Id,
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
                Level = block.Level,
                Timestamp = block.Timestamp,
                OpHash = op.RequiredString("hash"),
                Balance = content.RequiredInt64("balance"),
                BakerFee = content.RequiredInt64("fee"),
                Counter = content.RequiredInt32("counter"),
                GasLimit = content.RequiredInt32("gas_limit"),
                StorageLimit = content.RequiredInt32("storage_limit"),
                SenderId = sender.Id,
                ManagerId = manager?.Id,
                DelegateId = delegat?.Id,
                ContractId = contract?.Id,
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

            #region entities
            var blockBaker = Context.Proposer;
            var senderDelegate = Cache.Accounts.GetDelegate(sender.DelegateId) ?? sender as Data.Models.Delegate;
            //var contract = origination.Contract;
            var contractDelegate = delegat;
            var contractManager = manager;

            Db.TryAttach(blockBaker);
            Db.TryAttach(senderDelegate);
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

            Cache.AppState.Get().OriginationOpsCount++;
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
                    contractDelegate.StakingBalance += contract!.Balance;
                    contractDelegate.DelegatedBalance += contract.Balance;
                }

                sender.ContractsCount++;
                if (contractManager != null && contractManager != sender) contractManager.ContractsCount++;

                block.Events |= GetBlockEvents(contract!);

                if (contract!.Kind > ContractKind.DelegatorContract)
                {
                    var code = await ProcessCode(contract, Micheline.FromJson(content.Required("script").Required("code"))!);
                    var storage = Micheline.FromJson(content.Required("script").Required("storage"))!;

                    BigMapDiffs = ParseBigMapDiffs(origination, result, code, storage);
                    await ProcessScript(origination, contract, code, storage);

                    origination.ContractCodeHash = contract.CodeHash;
                }

                Cache.Statistics.Current.TotalBurned += burned;
            }
            #endregion

            Proto.Manager.Set(sender);
            Db.OriginationOps.Add(origination);
            Context.OriginationOps.Add(origination);
            Origination = origination;
            Contract = contract;
        }

        public virtual async Task ApplyInternal(Block block, ManagerOperation parent, JsonElement content)
        {
            #region init
            var sender = await Cache.Accounts.GetExistingAsync(content.RequiredString("source"));

            // WTF: [level:25054] - Manager and sender are not equal.
            var manager = await GetManager(content);
            var delegat = Cache.Accounts.GetDelegateOrDefault(content.OptionalString("delegate"));
            // WTF: [level:635] - Tezos allows to set non-existent delegate.

            Db.TryAttach(sender);
            Db.TryAttach(manager);
            Db.TryAttach(delegat);

            var result = content.Required("result");

            Contract? contract = null;
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
                        DelegateId = delegat?.Id,
                        DelegationLevel = delegat != null ? block.Level : null,
                        WeirdDelegateId = (await GetWeirdDelegate(content))?.Id,
                        CreatorId = sender.Id,
                        ManagerId = manager?.Id,
                        Staked = delegat?.Staked ?? false,
                        Type = AccountType.Contract,
                        Kind = GetContractKind(content),
                        Spendable = GetSpendable(content),
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
                        FirstLevel = block.Level,
                        LastLevel = block.Level,
                        Address = address,
                        Balance = content.RequiredInt64("balance"),
                        Counter = 0,
                        DelegateId = delegat?.Id,
                        DelegationLevel = delegat != null ? (int?)block.Level : null,
                        WeirdDelegateId = (await GetWeirdDelegate(content))?.Id,
                        CreatorId = sender.Id,
                        ManagerId = manager?.Id,
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
                InitiatorId = parent.SenderId,
                Level = parent.Level,
                Timestamp = parent.Timestamp,
                OpHash = parent.OpHash,
                Counter = parent.Counter,
                Nonce = content.RequiredInt32("nonce"),
                Balance = content.RequiredInt64("balance"),
                SenderId = sender.Id,
                SenderCodeHash = (sender as Contract)?.CodeHash,
                ManagerId = manager?.Id,
                DelegateId = delegat?.Id,
                ContractId = contract?.Id,
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

            #region entities
            var parentSender = await Cache.Accounts.GetAsync(parent.SenderId);
            var parentDelegate = Cache.Accounts.GetDelegate(parentSender.DelegateId) ?? parentSender as Data.Models.Delegate;

            var senderDelegate = Cache.Accounts.GetDelegate(sender.DelegateId) ?? sender as Data.Models.Delegate;

            var contractDelegate = delegat;
            var contractManager = manager;

            //Db.TryAttach(parentSender);
            Db.TryAttach(parentDelegate);

            //Db.TryAttach(sender);
            Db.TryAttach(senderDelegate);

            //Db.TryAttach(contract);
            //Db.TryAttach(contractDelegate);
            //Db.TryAttach(contractManager);
            #endregion

            #region apply operation
            if (parent is TransactionOperation parentTx)
            {
                parentTx.InternalOperations = (short?)((parentTx.InternalOperations ?? 0) + 1);
                parentTx.InternalOriginations = (short?)((parentTx.InternalOriginations ?? 0) + 1);
            }

            sender.OriginationsCount++;
            if (contractManager != null && contractManager != sender) contractManager.OriginationsCount++;
            if (contractDelegate != null && contractDelegate != sender && contractDelegate != contractManager) contractDelegate.OriginationsCount++;
            if (parentSender != sender && parentSender != contractDelegate && parentSender != contractManager) parentSender.OriginationsCount++;
            if (contract != null) contract.OriginationsCount++;

            block.Operations |= Operations.Originations;

            Cache.AppState.Get().OriginationOpsCount++;
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
                    contractDelegate.StakingBalance += contract!.Balance;
                    contractDelegate.DelegatedBalance += contract.Balance;
                }

                sender.ContractsCount++;
                if (contractManager != null && contractManager != sender) contractManager.ContractsCount++;

                block.Events |= GetBlockEvents(contract!);

                if (contract!.Kind > ContractKind.DelegatorContract)
                {
                    var code = await ProcessCode(contract, Micheline.FromJson(content.Required("script").Required("code"))!);
                    var storage = Micheline.FromJson(content.Required("script").Required("storage"))!;

                    BigMapDiffs = ParseBigMapDiffs(origination, result, code, storage);
                    await ProcessScript(origination, contract, code, storage);

                    origination.ContractCodeHash = contract.CodeHash;
                }

                Cache.Statistics.Current.TotalBurned += burned;
            }
            #endregion

            Db.OriginationOps.Add(origination);
            Context.OriginationOps.Add(origination);
            Origination = origination;
            Contract = contract;
        }

        public virtual async Task Revert(Block block, OriginationOperation origination)
        {
            #region entities
            var blockBaker = Context.Proposer;
            var sender = await Cache.Accounts.GetAsync(origination.SenderId);
            var senderDelegate = Cache.Accounts.GetDelegate(sender.DelegateId) ?? sender as Data.Models.Delegate;
            var contract = await Cache.Accounts.GetAsync(origination.ContractId) as Contract;
            var contractDelegate = Cache.Accounts.GetDelegate(origination.DelegateId);
            var contractManager = await Cache.Accounts.GetAsync(origination.ManagerId) as User;

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
                    contractDelegate.StakingBalance -= contract!.Balance;
                    contractDelegate.DelegatedBalance -= contract.Balance;
                }

                sender.ContractsCount--;
                if (contractManager != null && contractManager != sender) contractManager.ContractsCount--;

                if (contract!.Kind > ContractKind.DelegatorContract)
                    await RevertScript(origination, contract);

                if (contract.TokenTransfersCount == 0 && contract.TicketTransfersCount == 0)
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
                        FirstLevel = contract.FirstLevel,
                        LastLevel = contract.LastLevel,
                        ActiveTokensCount = contract.ActiveTokensCount,
                        TokenBalancesCount = contract.TokenBalancesCount,
                        TokenTransfersCount = contract.TokenTransfersCount,
                        ActiveTicketsCount = contract.ActiveTicketsCount,
                        TicketBalancesCount = contract.TicketBalancesCount,
                        TicketTransfersCount = contract.TicketTransfersCount,
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
            if (sender is User user) user.Revealed = true;

            Cache.AppState.Get().OriginationOpsCount--;
            #endregion

            Db.OriginationOps.Remove(origination);
            Cache.AppState.ReleaseManagerCounter();
            Cache.AppState.ReleaseOperationId();
        }

        public virtual async Task RevertInternal(Block block, OriginationOperation origination)
        {
            #region entities
            var parentSender = await Cache.Accounts.GetAsync(origination.InitiatorId!.Value);
            var parentDelegate = Cache.Accounts.GetDelegate(parentSender.DelegateId) ?? parentSender as Data.Models.Delegate;

            var sender = await Cache.Accounts.GetAsync(origination.SenderId);
            var senderDelegate = Cache.Accounts.GetDelegate(sender.DelegateId) ?? sender as Data.Models.Delegate;

            var contract = await Cache.Accounts.GetAsync(origination.ContractId) as Contract;
            var contractDelegate = Cache.Accounts.GetDelegate(origination.DelegateId);
            var contractManager = await Cache.Accounts.GetAsync(origination.ManagerId) as User;

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
                    contractDelegate.StakingBalance -= contract!.Balance;
                    contractDelegate.DelegatedBalance -= contract.Balance;
                }

                sender.ContractsCount--;
                if (contractManager != null && contractManager != sender) contractManager.ContractsCount--;

                if (contract!.Kind > ContractKind.DelegatorContract)
                    await RevertScript(origination, contract);

                if (contract.TokenTransfersCount == 0 && contract.TicketTransfersCount == 0)
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
                        FirstLevel = contract.FirstLevel,
                        LastLevel = contract.LastLevel,
                        ActiveTokensCount = contract.ActiveTokensCount,
                        TokenBalancesCount = contract.TokenBalancesCount,
                        TokenTransfersCount = contract.TokenTransfersCount,
                        ActiveTicketsCount = contract.ActiveTicketsCount,
                        TicketBalancesCount = contract.TicketBalancesCount,
                        TicketTransfersCount = contract.TicketTransfersCount,
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

            Cache.AppState.Get().OriginationOpsCount--;
            #endregion

            Db.OriginationOps.Remove(origination);
            Cache.AppState.ReleaseOperationId();
        }

        protected virtual async Task<User?> GetWeirdDelegate(JsonElement content)
        {
            var originDelegate = await Cache.Accounts.GetAsync(content.OptionalString("delegate"));
            return originDelegate?.Type == AccountType.User ? (User)originDelegate : null;
        }

        protected virtual async Task<User?> GetManager(JsonElement content)
        {
            // WTF: [level: 130] - Different nodes return different manager prop name.
            return await Cache.Accounts.GetAsync(content.OptionalString("managerPubkey") ?? content.OptionalString("manager_pubkey")) as User;
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

        protected async Task<MichelineArray> ProcessCode(Contract contract, IMicheline code)
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

            var storage = await Cache.Storages.GetKnownAsync(contract);
            Db.Storages.Remove(storage);
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
