using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Tzkt.Data;
using Tzkt.Data.Models;
using Tzkt.Data.Models.Base;
using Tzkt.Sync.Services.Cache;

namespace Tzkt.Sync.Protocols.Proto16
{
    class SmartRollupPublishCommit : ProtocolCommit
    {
        public SmartRollupPublishCommit(ProtocolHandler protocol) : base(protocol) { }

        public virtual async Task Apply(Block block, JsonElement op, JsonElement content)
        {
            #region init
            var result = content.Required("metadata").Required("operation_result");
            var commitmentHash = result.OptionalString("staked_hash");
            var bond = result.OptionalArray("balance_updates")?.EnumerateArray()
                .FirstOrDefault(x => x.RequiredString("kind") == "contract") ?? default;

            var sender = await Cache.Accounts.GetAsync(content.RequiredString("source"));
            sender.Delegate ??= Cache.Accounts.GetDelegate(sender.DelegateId);
            var rollup = await Cache.Accounts.GetSmartRollupOrDefaultAsync(content.RequiredString("rollup"));
            var commitment = await Cache.SmartRollupCommitments.GetOrDefaultAsync(commitmentHash, rollup?.Id);
            
            var operation = new SmartRollupPublishOperation
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
                Bond = bond.ValueKind == JsonValueKind.Undefined ? 0 : -bond.RequiredInt64("change"),
                Flags = SmartRollupPublishFlags.None,
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
                StorageUsed = 0,
                StorageFee = null,
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

            sender.SmartRollupPublishCount++;
            if (rollup != null) rollup.SmartRollupPublishCount++;

            block.Operations |= Operations.SmartRollupPublish;
            block.Fees += operation.BakerFee;

            sender.Counter = operation.Counter;

            if (commitment != null)
                commitment.LastLevel = operation.Level;

            Cache.AppState.Get().SmartRollupPublishOpsCount++;
            #endregion

            #region apply result
            if (operation.Status == OperationStatus.Applied)
            {
                if (operation.Bond != 0)
                {
                    sender.SmartRollupBonds += operation.Bond;
                    rollup.SmartRollupBonds += operation.Bond;

                    var uniqueStakers = (await Db.SmartRollupPublishOps.AsNoTracking()
                        .Where(x => x.SmartRollupId == rollup.Id && x.BondStatus != null)
                        .Select(x => x.SenderId)
                        .Distinct()
                        .ToListAsync())
                        .ToHashSet();

                    if (block.SmartRollupPublishOps != null)
                        foreach (var publish in block.SmartRollupPublishOps.Where(x => x.SmartRollupId == rollup.Id))
                            uniqueStakers.Add(publish.SenderId);

                    uniqueStakers.Add(operation.SenderId);

                    rollup.TotalStakers = uniqueStakers.Count;
                    rollup.ActiveStakers++;

                    operation.BondStatus = SmartRollupBondStatus.Active;

                    Cache.Statistics.Current.TotalSmartRollupBonds += operation.Bond;
                }

                if (commitment == null)
                {
                    var commitmentEl = content.Required("commitment");
                    commitment = new SmartRollupCommitment
                    {
                        Id = Cache.AppState.NextSmartRollupCommitmentId(),
                        SmartRollupId = rollup.Id,
                        InitiatorId = operation.SenderId,
                        FirstLevel = operation.Level,
                        LastLevel = operation.Level,
                        InboxLevel = commitmentEl.RequiredInt32("inbox_level"),
                        State = commitmentEl.RequiredString("compressed_state"),
                        Ticks = commitmentEl.RequiredInt64("number_of_ticks"),
                        Hash = commitmentHash,
                        Stakers = 1,
                        ActiveStakers = 1,
                        Successors = 0,
                        Status = SmartRollupCommitmentStatus.Pending
                    };
                    Cache.SmartRollupCommitments.Add(commitment);
                    Db.SmartRollupCommitments.Add(commitment);

                    var predecessorHash = commitmentEl.RequiredString("predecessor");
                    if (predecessorHash != rollup.GenesisCommitment)
                    {
                        var predecessor = await Cache.SmartRollupCommitments.GetAsync(predecessorHash, rollup.Id);
                        Db.TryAttach(predecessor);
                        predecessor.Successors++;
                        predecessor.LastLevel = operation.Level;

                        commitment.PredecessorId = predecessor.Id;
                    }

                    rollup.PendingCommitments++;

                    operation.CommitmentId = commitment.Id;
                    operation.Flags = SmartRollupPublishFlags.AddStaker;
                    Cache.SmartRollupStakes.Add(commitment);
                }
                else
                {
                    var stake = await Cache.SmartRollupStakes.GetAsync(commitment, sender.Id);
                    if (stake == null)
                    {
                        commitment.Stakers++;
                        commitment.ActiveStakers++;
                        operation.Flags = SmartRollupPublishFlags.AddStaker;
                        Cache.SmartRollupStakes.Set(commitment.Id, sender.Id, 1);
                    }
                    else if (stake == 0)
                    {
                        commitment.ActiveStakers++;
                        operation.Flags = SmartRollupPublishFlags.ReactivateStaker;
                        Cache.SmartRollupStakes.Set(commitment.Id, sender.Id, 1);
                    }
                    if (commitment.Status == SmartRollupCommitmentStatus.Refuted)
                    {
                        rollup.PendingCommitments++;
                        rollup.RefutedCommitments--;

                        commitment.Status = SmartRollupCommitmentStatus.Pending;
                        if (commitment.Successors > 0)
                        {
                            var cnt = await UpdateSuccessorsStatus(Db, Cache.SmartRollupCommitments, commitment, SmartRollupCommitmentStatus.Pending);
                            rollup.PendingCommitments += cnt;
                            rollup.OrphanCommitments -= cnt;
                        }
                        operation.Flags |= SmartRollupPublishFlags.ReactivateBranch;
                    }
                }
            }
            #endregion

            Proto.Manager.Set(operation.Sender);
            Db.SmartRollupPublishOps.Add(operation);
        }

        public virtual async Task Revert(Block block, SmartRollupPublishOperation operation)
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
                if (operation.Bond != 0)
                {
                    sender.SmartRollupBonds -= operation.Bond;
                    rollup.SmartRollupBonds -= operation.Bond;

                    var uniqueStakers = await Db.SmartRollupPublishOps.AsNoTracking()
                        .Where(x => x.SmartRollupId == rollup.Id && x.BondStatus != null && x.Id < operation.Id)
                        .Select(x => x.SenderId)
                        .Distinct()
                        .CountAsync();

                    rollup.TotalStakers = uniqueStakers;
                    rollup.ActiveStakers--;
                }

                if (commitment.Stakers == 1 && operation.Flags.HasFlag(SmartRollupPublishFlags.AddStaker))
                {
                    rollup.PendingCommitments--;

                    if (commitment.PredecessorId != null)
                    {
                        var predecessor = await Cache.SmartRollupCommitments.GetAsync((int)commitment.PredecessorId);
                        Db.TryAttach(predecessor);
                        predecessor.Successors--;
                        // predecessor.LastLevel is not reverted
                    }

                    Cache.AppState.ReleaseSmartRollupCommitmentId();
                    Cache.SmartRollupStakes.Remove(commitment.Id);
                    Cache.SmartRollupCommitments.Remove(commitment);
                    Db.SmartRollupCommitments.Remove(commitment);
                }
                else if (operation.Flags is SmartRollupPublishFlags change)
                {
                    if (change.HasFlag(SmartRollupPublishFlags.AddStaker))
                    {
                        commitment.Stakers--;
                        commitment.ActiveStakers--;
                        Cache.SmartRollupStakes.Remove(commitment.Id, operation.SenderId);
                    }
                    else if (change.HasFlag(SmartRollupPublishFlags.ReactivateStaker))
                    {
                        commitment.ActiveStakers--;
                        Cache.SmartRollupStakes.Set(commitment.Id, operation.SenderId, 0);
                    }
                    if (change.HasFlag(SmartRollupPublishFlags.ReactivateBranch))
                    {
                        rollup.PendingCommitments--;
                        rollup.RefutedCommitments++;

                        commitment.Status = SmartRollupCommitmentStatus.Refuted;
                        if (commitment.Successors > 0)
                        {
                            var cnt = await UpdateSuccessorsStatus(Db, Cache.SmartRollupCommitments, commitment, SmartRollupCommitmentStatus.Orphan);
                            rollup.PendingCommitments -= cnt;
                            rollup.OrphanCommitments += cnt;
                        }
                    }
                }
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

            sender.SmartRollupPublishCount--;
            if (rollup != null) rollup.SmartRollupPublishCount--;

            sender.Counter = operation.Counter - 1;
            (sender as User).Revealed = true;

            // commitment.LastLevel is not reverted

            Cache.AppState.Get().SmartRollupPublishOpsCount--;
            #endregion

            Db.SmartRollupPublishOps.Remove(operation);
            Cache.AppState.ReleaseManagerCounter();
            Cache.AppState.ReleaseOperationId();
        }

        internal static async Task<int> UpdateSuccessorsStatus(
            TzktContext db,
            SmartRollupCommitmentCache cache,
            SmartRollupCommitment commitment,
            SmartRollupCommitmentStatus status)
        {
            var cnt = 0;
            var stack = new Stack<SmartRollupCommitment>();
            stack.Push(commitment);

            while (stack.TryPop(out var c))
            {
                if (c.Successors > 0)
                {
                    var ids = await db.SmartRollupCommitments
                        .AsNoTracking()
                        .Where(x => x.PredecessorId == c.Id)
                        .Select(x => x.Id)
                        .ToListAsync();

                    foreach (var id in ids)
                    {
                        var successor = await cache.GetAsync(id);
                        db.TryAttach(successor);
                        successor.Status = status;
                        stack.Push(successor);
                        cnt++;
                    }
                }
            }

            return cnt;
        }
    }
}
