using System.Text.Json;
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
            Db.TryAttach(sender);

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
                        Index = ghost.Index,
                        FirstLevel = ghost.FirstLevel,
                        LastLevel = ghost.LastLevel,
                        Address = address,
                        Counter = 0,
                        CreatorId = sender.Id,
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
                        Counter = 0,
                        CreatorId = sender.Id,
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

            #region apply operation
            PayFee(sender, origination.BakerFee);

            sender.TxRollupOriginationCount++;
            if (rollup != null) rollup.TxRollupOriginationCount++;

            block.Operations |= Operations.TxRollupOrigination;

            sender.Counter = origination.Counter;

            Cache.AppState.Get().TxRollupOriginationOpsCount++;
            #endregion

            #region apply result
            if (origination.Status == OperationStatus.Applied)
            {
                var burned = origination.AllocationFee ?? 0;
                Proto.Manager.Burn(burned);

                Spend(sender, burned);

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
            var sender = await Cache.Accounts.GetAsync(origination.SenderId);
            var rollup = await Cache.Accounts.GetAsync(origination.RollupId) as Rollup;

            Db.TryAttach(sender);
            Db.TryAttach(rollup);
            #endregion

            #region revert result
            if (origination.Status == OperationStatus.Applied)
            {
                RevertSpend(sender, origination.AllocationFee ?? 0);

                sender.RollupsCount--;

                if (rollup!.TokenTransfersCount == 0 && rollup.TicketTransfersCount == 0 && rollup.Index is null)
                {
                    Db.Rollups.Remove(rollup);
                    Cache.Accounts.Remove(rollup);
                }
                else
                {
                    var ghost = new Account
                    {
                        Id = rollup.Id,
                        Index = rollup.Index,
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
            RevertPayFee(sender, origination.BakerFee);

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
