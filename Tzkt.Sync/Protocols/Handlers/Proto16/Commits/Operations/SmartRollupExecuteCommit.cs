using System.Numerics;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Netezos.Contracts;
using Netezos.Encoding;
using Tzkt.Data.Models;
using Tzkt.Data.Models.Base;

namespace Tzkt.Sync.Protocols.Proto16
{
    class SmartRollupExecuteCommit(ProtocolHandler protocol) : ProtocolCommit(protocol)
    {
        public SmartRollupExecuteOperation Operation { get; private set; } = null!;
        public IEnumerable<TicketUpdates>? TicketUpdates { get; private set; }

        public virtual async Task Apply(Block block, JsonElement op, JsonElement content)
        {
            #region init
            var sender = await Cache.Accounts.GetExistingAsync(content.RequiredString("source"));
            var rollup = await Cache.Accounts.GetSmartRollupOrDefaultAsync(content.RequiredString("rollup"));
            var commitment = await Cache.SmartRollupCommitments.GetOrDefaultAsync(content.RequiredString("cemented_commitment"), rollup?.Id);

            var result = content.Required("metadata").Required("operation_result");

            var operation = new SmartRollupExecuteOperation
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
                SmartRollupId = rollup?.Id,
                CommitmentId = commitment?.Id,
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
                StorageUsed = result.OptionalInt32("paid_storage_size_diff") ?? 0,
                StorageFee = result.OptionalInt32("paid_storage_size_diff") > 0
                    ? result.OptionalInt32("paid_storage_size_diff") * Context.Protocol.ByteCost
                    : null,
                AllocationFee = null
            };
            #endregion

            #region entities
            Db.TryAttach(sender);
            Db.TryAttach(rollup);
            Db.TryAttach(commitment);
            #endregion

            #region apply operation
            PayFee(sender, operation.BakerFee);

            sender.SmartRollupExecuteCount++;
            if (rollup != null) rollup.SmartRollupExecuteCount++;

            block.Operations |= Operations.SmartRollupExecute;

            sender.Counter = operation.Counter;

            if (commitment != null)
                commitment.LastLevel = operation.Level;

            Cache.AppState.Get().SmartRollupExecuteOpsCount++;
            #endregion

            #region apply result
            if (operation.Status == OperationStatus.Applied)
            {
                var burned = operation.StorageFee ?? 0;
                Proto.Manager.Burn(burned);

                Spend(sender, burned);
                
                TicketUpdates = ParseTicketUpdates(result);

                if (commitment!.Status != SmartRollupCommitmentStatus.Executed)
                {
                    rollup!.ExecutedCommitments++;
                    commitment.Status = SmartRollupCommitmentStatus.Executed;
                }

                Cache.Statistics.Current.TotalBurned += burned;
            }
            #endregion

            Proto.Manager.Set(sender);
            Db.SmartRollupExecuteOps.Add(operation);
            Context.SmartRollupExecuteOps.Add(operation);
            Operation = operation;
        }

        public virtual async Task Revert(Block block, SmartRollupExecuteOperation operation)
        {
            #region entities
            var sender = await Cache.Accounts.GetAsync(operation.SenderId);
            var rollup = await Cache.Accounts.GetAsync(operation.SmartRollupId) as SmartRollup;
            var commitment = await Cache.SmartRollupCommitments.GetOrDefaultAsync(operation.CommitmentId);

            Db.TryAttach(sender);
            Db.TryAttach(rollup);
            Db.TryAttach(commitment);
            #endregion

            #region revert result
            if (operation.Status == OperationStatus.Applied)
            {
                RevertSpend(sender, operation.StorageFee ?? 0);

                var isFirstExecution = !await Db.SmartRollupExecuteOps
                    .AnyAsync(x => x.Status == OperationStatus.Applied && x.CommitmentId == operation.CommitmentId && x.Id < operation.Id);

                if (isFirstExecution)
                {
                    rollup!.ExecutedCommitments--;
                    commitment!.Status = SmartRollupCommitmentStatus.Cemented;
                }
            }
            #endregion

            #region revert operation
            RevertPayFee(sender, operation.BakerFee);

            sender.SmartRollupExecuteCount--;
            if (rollup != null) rollup.SmartRollupExecuteCount--;

            sender.Counter = operation.Counter - 1;
            (sender as User)!.Revealed = true;

            // commitment.LastLevel is not reverted

            Cache.AppState.Get().SmartRollupExecuteOpsCount--;
            #endregion

            Db.SmartRollupExecuteOps.Remove(operation);
            Cache.AppState.ReleaseManagerCounter();
            Cache.AppState.ReleaseOperationId();
        }

        protected virtual IEnumerable<TicketUpdates>? ParseTicketUpdates(JsonElement result)
        {
            if (!result.TryGetProperty("ticket_updates", out var ticketUpdates))
                return null;

            var res = new List<TicketUpdates>();
            foreach (var updates in ticketUpdates.RequiredArray().EnumerateArray())
            {
                var list = new List<TicketUpdate>();
                foreach (var update in updates.RequiredArray("updates").EnumerateArray())
                {
                    var amount = update.RequiredBigInteger("amount");
                    if (amount != BigInteger.Zero)
                    {
                        list.Add(new TicketUpdate
                        {
                            Account = update.RequiredString("account"),
                            Amount = amount
                        });
                    }
                }

                if (list.Count > 0)
                {
                    var ticketToken = updates.Required("ticket_token");
                    var type = ticketToken.RequiredMicheline("content_type");
                    var value = ticketToken.RequiredMicheline("content");
                    var rawType = type.ToBytes();

                    byte[] rawContent;
                    string? jsonContent;

                    try
                    {
                        var schema = Schema.Create((type as MichelinePrim)!);
                        rawContent = schema.Optimize(value).ToBytes();
                        jsonContent = schema.Humanize(value);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, "Failed to parse ticket content");
                        rawContent = value.ToBytes();
                        jsonContent = null;
                    }

                    res.Add(new TicketUpdates
                    {
                        Ticket = new TicketIdentity
                        {
                            Ticketer = ticketToken.RequiredString("ticketer"),
                            RawType = rawType,
                            RawContent = rawContent,
                            JsonContent = jsonContent,
                            TypeHash = Script.GetHash(rawType),
                            ContentHash = Script.GetHash(rawContent)
                        },
                        Updates = list
                    });
                }
            }

            return res.Count > 0 ? res : null;
        }
    }
}
