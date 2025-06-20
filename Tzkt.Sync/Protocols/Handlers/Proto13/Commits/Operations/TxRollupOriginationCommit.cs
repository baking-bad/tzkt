﻿using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models;
using Tzkt.Data.Models.Base;

namespace Tzkt.Sync.Protocols.Proto13
{
    class TxRollupOriginationCommit(ProtocolHandler protocol) : ProtocolCommit(protocol)
    {
        public virtual async Task Apply(Block block, JsonElement op, JsonElement content)
        {
            #region init
            var sender = await Cache.Accounts.GetExistingAsync(content.RequiredString("source"));
            var senderDelegate = Cache.Accounts.GetDelegate(sender.DelegateId) ?? sender as Data.Models.Delegate;

            Db.TryAttach(sender);
            Db.TryAttach(senderDelegate);

            var result = content.Required("metadata").Required("operation_result");

            Rollup? rollup = null;
            if (result.RequiredString("status") == "applied")
            {
                var address = result.RequiredString("originated_rollup");
                var ghost = await Cache.Accounts.GetAsync(address);
                if (ghost != null)
                {
                    rollup = new Rollup
                    {
                        Id = ghost.Id,
                        FirstLevel = ghost.FirstLevel,
                        LastLevel = ghost.LastLevel,
                        Address = address,
                        Balance = 0,
                        Counter = 0,
                        CreatorId = sender.Id,
                        Staked = false,
                        Type = AccountType.Rollup,
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
                    rollup = new Rollup
                    {
                        Id = Cache.AppState.NextAccountId(),
                        FirstLevel = block.Level,
                        LastLevel = block.Level,
                        Address = address,
                        Balance = 0,
                        Counter = 0,
                        CreatorId = sender.Id,
                        Staked = false,
                        Type = AccountType.Rollup
                    };
                    Db.Rollups.Add(rollup);
                }
                Cache.Accounts.Add(rollup);
            }

            var origination = new TxRollupOriginationOperation
            {
                Id = Cache.AppState.NextOperationId(),
                Level = block.Level,
                Timestamp = block.Timestamp,
                OpHash = op.RequiredString("hash"),
                BakerFee = content.RequiredInt64("fee"),
                Counter = content.RequiredInt32("counter"),
                GasLimit = content.RequiredInt32("gas_limit"),
                StorageLimit = content.RequiredInt32("storage_limit"),
                SenderId = sender.Id,
                RollupId = rollup?.Id,
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
                AllocationFee = 4_000 * Context.Protocol.ByteCost
            };
            #endregion

            #region entities
            var blockBaker = Context.Proposer;
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

            sender.TxRollupOriginationCount++;
            if (rollup != null) rollup.TxRollupOriginationCount++;

            block.Operations |= Operations.TxRollupOrigination;
            block.Fees += origination.BakerFee;

            sender.Counter = origination.Counter;

            Cache.AppState.Get().TxRollupOriginationOpsCount++;
            #endregion

            #region apply result
            if (origination.Status == OperationStatus.Applied)
            {
                var burned = origination.AllocationFee ?? 0;
                Proto.Manager.Burn(burned);

                sender.Balance -= burned;
                if (senderDelegate != null)
                {
                    senderDelegate.StakingBalance -= burned;
                    if (senderDelegate.Id != sender.Id)
                        senderDelegate.DelegatedBalance -= burned;
                }

                sender.RollupsCount++;

                Cache.Statistics.Current.TotalBurned += burned;
            }
            #endregion

            Proto.Manager.Set(sender);
            Db.TxRollupOriginationOps.Add(origination);
            Context.TxRollupOriginationOps.Add(origination);
        }

        public virtual async Task Revert(Block block, TxRollupOriginationOperation origination)
        {
            #region entities
            var blockBaker = Context.Proposer;
            var sender = await Cache.Accounts.GetAsync(origination.SenderId);
            var senderDelegate = Cache.Accounts.GetDelegate(sender.DelegateId) ?? sender as Data.Models.Delegate;
            var rollup = await Cache.Accounts.GetAsync(origination.RollupId) as Rollup;

            Db.TryAttach(blockBaker);
            Db.TryAttach(sender);
            Db.TryAttach(senderDelegate);
            Db.TryAttach(rollup);
            #endregion

            #region revert result
            if (origination.Status == OperationStatus.Applied)
            {
                var spent = origination.AllocationFee ?? 0;

                sender.Balance += spent;
                if (senderDelegate != null)
                {
                    senderDelegate.StakingBalance += spent;
                    if (senderDelegate.Id != sender.Id)
                        senderDelegate.DelegatedBalance += spent;
                }

                sender.RollupsCount--;

                if (rollup!.TokenTransfersCount == 0 && rollup.TicketTransfersCount == 0)
                {
                    Db.Rollups.Remove(rollup);
                    Cache.Accounts.Remove(rollup);
                }
                else
                {
                    var ghost = new Account
                    {
                        Id = rollup.Id,
                        Address = rollup.Address,
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

            sender.TxRollupOriginationCount--;

            sender.Counter = origination.Counter - 1;
            (sender as User)!.Revealed = true;

            Cache.AppState.Get().TxRollupOriginationOpsCount--;
            #endregion

            Db.TxRollupOriginationOps.Remove(origination);
            Cache.AppState.ReleaseManagerCounter();
            Cache.AppState.ReleaseOperationId();
        }
    }
}
