using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Netmavryk.Encoding;
using Tzkt.Data.Models;
using Tzkt.Data.Models.Base;

namespace Tzkt.Sync.Protocols.Proto16
{
    class SmartRollupOriginateCommit : ProtocolCommit
    {
        public SmartRollupOriginateCommit(ProtocolHandler protocol) : base(protocol) { }

        public virtual async Task Apply(Block block, JsonElement op, JsonElement content)
        {
            #region init
            var sender = await Cache.Accounts.GetAsync(content.RequiredString("source"));
            sender.Delegate ??= Cache.Accounts.GetDelegate(sender.DelegateId);

            Db.TryAttach(block.Proposer);
            Db.TryAttach(sender);
            Db.TryAttach(sender.Delegate);

            var pvmKind = content.RequiredString("pvm_kind") switch
            {
                "arith" => PvmKind.Arith,
                "wasm_2_0_0" => PvmKind.Wasm,
                _ => throw new NotImplementedException()
            };
            var result = content.Required("metadata").Required("operation_result");

            SmartRollup rollup = null;
            if (result.RequiredString("status") == "applied")
            {
                var address = result.RequiredString("address");
                var ghost = await Cache.Accounts.GetAsync(address);
                if (ghost != null)
                {
                    rollup = new()
                    {
                        Id = ghost.Id,
                        FirstLevel = ghost.FirstLevel,
                        LastLevel = ghost.LastLevel,
                        Address = address,
                        Balance = 0,
                        Counter = 0,
                        CreatorId = sender.Id,
                        Staked = false,
                        Type = AccountType.SmartRollup,
                        PvmKind = pvmKind,
                        GenesisCommitment = result.RequiredString("genesis_commitment_hash"),
                        LastCommitment = result.RequiredString("genesis_commitment_hash"),
                        InboxLevel = 0,
                        TotalStakers = 0,
                        ActiveStakers = 0,
                        ExecutedCommitments = 0,
                        CementedCommitments = 0,
                        PendingCommitments = 0,
                        RefutedCommitments = 0,
                        OrphanCommitments = 0,
                        SmartRollupBonds = 0,
                        ActiveTokensCount = ghost.ActiveTokensCount,
                        TokenBalancesCount = ghost.TokenBalancesCount,
                        TokenTransfersCount = ghost.TokenTransfersCount,
                        ActiveTicketsCount = ghost.ActiveTicketsCount,
                        TicketBalancesCount = ghost.TicketBalancesCount,
                        TicketTransfersCount = ghost.TicketTransfersCount
                    };
                    Db.Entry(ghost).State = EntityState.Detached;
                    Db.Entry(rollup).State = EntityState.Modified;
                }
                else
                {
                    rollup = new()
                    {
                        Id = Cache.AppState.NextAccountId(),
                        FirstLevel = block.Level,
                        LastLevel = block.Level,
                        Address = address,
                        Balance = 0,
                        Counter = 0,
                        CreatorId = sender.Id,
                        Staked = false,
                        Type = AccountType.SmartRollup,
                        PvmKind = pvmKind,
                        GenesisCommitment = result.RequiredString("genesis_commitment_hash"),
                        LastCommitment = result.RequiredString("genesis_commitment_hash"),
                        InboxLevel = 0,
                        TotalStakers = 0,
                        ActiveStakers = 0,
                        ExecutedCommitments = 0,
                        CementedCommitments = 0,
                        PendingCommitments = 0,
                        RefutedCommitments = 0,
                        OrphanCommitments = 0,
                        SmartRollupBonds = 0
                    };
                    Db.SmartRollups.Add(rollup);
                }
                Cache.Accounts.Add(rollup);
            }

            var operation = new SmartRollupOriginateOperation
            {
                Id = Cache.AppState.NextOperationId(),
                Block = block,
                Level = block.Level,
                Timestamp = block.Timestamp,
                OpHash = op.RequiredString("hash"),
                BakerFee = content.RequiredInt64("fee"),
                Counter = content.RequiredInt32("counter"),
                GasLimit = content.RequiredInt32("gas_limit"),
                StorageLimit = content.RequiredInt32("storage_limit"),
                SenderId = sender.Id,
                Sender = sender,
                PvmKind = pvmKind,
                Kernel = Hex.Parse(content.RequiredString("kernel")),
                GenesisCommitment = result.OptionalString("genesis_commitment_hash"),
                SmartRollupId = rollup?.Id,
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
                GasUsed = (int)(((result.OptionalInt64("consumed_milligas") ?? 0) + 999) / 1000),
                StorageUsed = result.OptionalInt32("size") ?? 0,
                StorageFee = result.OptionalInt32("size") > 0
                    ? result.OptionalInt32("size") * block.Protocol.ByteCost
                    : null,
                AllocationFee = null
            };

            try
            {
                operation.ParameterType = Micheline.FromJson(content.Required("parameters_ty")).ToBytes();
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Failed to parse smart rollup parameter type");
            }
            #endregion

            #region entities
            var blockBaker = block.Proposer;
            var senderDelegate = sender.Delegate ?? sender as Data.Models.Delegate;
            #endregion

            #region apply operation
            sender.Balance -= operation.BakerFee;
            if (senderDelegate != null)
            {
                senderDelegate.StakingBalance -= operation.BakerFee;
                if (senderDelegate.Id != sender.Id)
                    senderDelegate.DelegatedBalance -= operation.BakerFee;
            }
            blockBaker.Balance += operation.BakerFee;
            blockBaker.StakingBalance += operation.BakerFee;

            sender.SmartRollupOriginateCount++;
            if (rollup != null) rollup.SmartRollupOriginateCount++;

            block.Operations |= Operations.SmartRollupOriginate;
            block.Fees += operation.BakerFee;

            sender.Counter = operation.Counter;

            Cache.AppState.Get().SmartRollupOriginateOpsCount++;
            #endregion

            #region apply result
            if (operation.Status == OperationStatus.Applied)
            {
                var burned = operation.StorageFee ?? 0;
                Proto.Manager.Burn(burned);

                sender.Balance -= burned;
                if (senderDelegate != null)
                {
                    senderDelegate.StakingBalance -= burned;
                    if (senderDelegate.Id != sender.Id)
                        senderDelegate.DelegatedBalance -= burned;
                }

                sender.SmartRollupsCount++;

                Cache.Statistics.Current.TotalBurned += burned;
            }
            #endregion

            Proto.Manager.Set(operation.Sender);
            Db.SmartRollupOriginateOps.Add(operation);
        }

        public virtual async Task Revert(Block block, SmartRollupOriginateOperation operation)
        {
            #region init
            operation.Block ??= block;
            operation.Block.Protocol ??= await Cache.Protocols.GetAsync(block.ProtoCode);
            operation.Block.Proposer ??= Cache.Accounts.GetDelegate(block.ProposerId);

            operation.Sender ??= await Cache.Accounts.GetAsync(operation.SenderId);
            operation.Sender.Delegate ??= Cache.Accounts.GetDelegate(operation.Sender.DelegateId);
            #endregion

            #region entities
            var blockBaker = block.Proposer;
            var sender = operation.Sender;
            var senderDelegate = sender.Delegate ?? sender as Data.Models.Delegate;
            var rollup = await Cache.Accounts.GetAsync(operation.SmartRollupId) as SmartRollup;

            Db.TryAttach(blockBaker);
            Db.TryAttach(sender);
            Db.TryAttach(senderDelegate);
            Db.TryAttach(rollup);
            #endregion

            #region revert result
            if (operation.Status == OperationStatus.Applied)
            {
                var spent = operation.StorageFee ?? 0;

                sender.Balance += spent;
                if (senderDelegate != null)
                {
                    senderDelegate.StakingBalance += spent;
                    if (senderDelegate.Id != sender.Id)
                        senderDelegate.DelegatedBalance += spent;
                }

                sender.SmartRollupsCount--;

                if (rollup.TokenTransfersCount == 0 && rollup.TicketTransfersCount == 0)
                {
                    Db.SmartRollups.Remove(rollup);
                    Cache.Accounts.Remove(rollup);
                }
                else
                {
                    var ghost = new Account
                    {
                        Id = rollup.Id,
                        Address = rollup.Address,
                        FirstBlock = rollup.FirstBlock,
                        FirstLevel = rollup.FirstLevel,
                        LastLevel = rollup.LastLevel,
                        ActiveTokensCount = rollup.ActiveTokensCount,
                        TokenBalancesCount = rollup.TokenBalancesCount,
                        TokenTransfersCount = rollup.TokenTransfersCount,
                        ActiveTicketsCount = rollup.ActiveTicketsCount,
                        TicketBalancesCount = rollup.TicketBalancesCount,
                        TicketTransfersCount = rollup.TicketTransfersCount,
                        Type = AccountType.Ghost,
                    };

                    Db.Entry(rollup).State = EntityState.Detached;
                    Db.Entry(ghost).State = EntityState.Modified;
                    Cache.Accounts.Add(ghost);
                }

                Cache.Schemas.Remove(rollup);
            }
            #endregion

            #region revert operation
            sender.Balance += operation.BakerFee;
            if (senderDelegate != null)
            {
                senderDelegate.StakingBalance += operation.BakerFee;
                if (senderDelegate.Id != sender.Id)
                    senderDelegate.DelegatedBalance += operation.BakerFee;
            }
            blockBaker.Balance -= operation.BakerFee;
            blockBaker.StakingBalance -= operation.BakerFee;

            sender.SmartRollupOriginateCount--;

            sender.Counter = operation.Counter - 1;
            (sender as User).Revealed = true;

            Cache.AppState.Get().SmartRollupOriginateOpsCount--;
            #endregion

            Db.SmartRollupOriginateOps.Remove(operation);
            Cache.AppState.ReleaseManagerCounter();
            Cache.AppState.ReleaseOperationId();
        }
    }
}
