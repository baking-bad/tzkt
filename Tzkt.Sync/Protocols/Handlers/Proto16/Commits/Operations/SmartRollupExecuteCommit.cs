using System.Numerics;
using System.Text.Json;
using Netmavryk.Contracts;
using Netmavryk.Encoding;
using Tzkt.Data.Models;
using Tzkt.Data.Models.Base;

namespace Tzkt.Sync.Protocols.Proto16
{
    class SmartRollupExecuteCommit : ProtocolCommit
    {
        public SmartRollupExecuteOperation Operation { get; private set; }
        public IEnumerable<TicketUpdates> TicketUpdates { get; private set; }

        public SmartRollupExecuteCommit(ProtocolHandler protocol) : base(protocol) { }

        public virtual async Task Apply(Block block, JsonElement op, JsonElement content)
        {
            #region init
            var sender = await Cache.Accounts.GetAsync(content.RequiredString("source"));
            sender.Delegate ??= Cache.Accounts.GetDelegate(sender.DelegateId);
            var rollup = await Cache.Accounts.GetSmartRollupOrDefaultAsync(content.RequiredString("rollup"));
            var commitment = await Cache.SmartRollupCommitments.GetOrDefaultAsync(content.RequiredString("cemented_commitment"), rollup?.Id);

            var result = content.Required("metadata").Required("operation_result");

            var operation = new SmartRollupExecuteOperation
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
                    ? result.OptionalInt32("paid_storage_size_diff") * block.Protocol.ByteCost
                    : null,
                AllocationFee = null
            };
            #endregion

            #region entities
            var blockBaker = block.Proposer;
            var senderDelegate = sender.Delegate ?? sender as Data.Models.Delegate;

            Db.TryAttach(block.Proposer);
            Db.TryAttach(sender);
            Db.TryAttach(sender.Delegate);
            Db.TryAttach(rollup);
            Db.TryAttach(commitment);
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

            sender.SmartRollupExecuteCount++;
            if (rollup != null) rollup.SmartRollupExecuteCount++;

            block.Operations |= Operations.SmartRollupExecute;
            block.Fees += operation.BakerFee;

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

                sender.Balance -= burned;
                if (senderDelegate != null)
                {
                    senderDelegate.StakingBalance -= burned;
                    if (senderDelegate.Id != sender.Id)
                        senderDelegate.DelegatedBalance -= burned;
                }
                
                TicketUpdates = ParseTicketUpdates(result);

                rollup.ExecutedCommitments++;

                commitment.Status = SmartRollupCommitmentStatus.Executed;

                Cache.Statistics.Current.TotalBurned += burned;
            }
            #endregion

            Proto.Manager.Set(operation.Sender);
            Db.SmartRollupExecuteOps.Add(operation);
            Operation = operation;
        }

        public virtual async Task Revert(Block block, SmartRollupExecuteOperation operation)
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
            var commitment = await Cache.SmartRollupCommitments.GetOrDefaultAsync(operation.CommitmentId);

            Db.TryAttach(blockBaker);
            Db.TryAttach(sender);
            Db.TryAttach(senderDelegate);
            Db.TryAttach(rollup);
            Db.TryAttach(commitment);
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

                rollup.ExecutedCommitments--;

                commitment.Status = SmartRollupCommitmentStatus.Cemented;
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

            sender.SmartRollupExecuteCount--;
            if (rollup != null) rollup.SmartRollupExecuteCount--;

            sender.Counter = operation.Counter - 1;
            (sender as User).Revealed = true;

            // commitment.LastLevel is not reverted

            Cache.AppState.Get().SmartRollupExecuteOpsCount--;
            #endregion

            Db.SmartRollupExecuteOps.Remove(operation);
            Cache.AppState.ReleaseManagerCounter();
            Cache.AppState.ReleaseOperationId();
        }

        protected virtual IEnumerable<TicketUpdates> ParseTicketUpdates(JsonElement result)
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
                    var type = Micheline.FromJson(ticketToken.Required("content_type"));
                    var value = Micheline.FromJson(ticketToken.Required("content"));
                    var rawType = type.ToBytes();

                    byte[] rawContent;
                    string jsonContent;

                    try
                    {
                        var schema = Schema.Create(type as MichelinePrim);
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
