using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Netezos.Keys;
using Tzkt.Data.Models;
using Tzkt.Data.Models.Base;

namespace Tzkt.Sync.Protocols.Proto15
{
    class UpdateSecondaryKeyCommit(ProtocolHandler protocol) : ProtocolCommit(protocol)
    {
        public virtual async Task Apply(Block block, JsonElement op, JsonElement content)
        {
            #region init
            var sender = await Cache.Accounts.GetExistingAsync(content.RequiredString("source"));

            var pubKey = content.RequiredString("pk");
            var pubKeyHash = PubKey.FromBase58(pubKey).Address;
            var result = content.Required("metadata").Required("operation_result");
            var operation = new UpdateSecondaryKeyOperation
            {
                Id = Cache.AppState.NextOperationId(),
                OpHash = op.RequiredString("hash"),
                Level = block.Level,
                Timestamp = block.Timestamp,
                BakerFee = content.RequiredInt64("fee"),
                Counter = content.RequiredInt32("counter"),
                GasLimit = content.RequiredInt32("gas_limit"),
                StorageLimit = content.RequiredInt32("storage_limit"),
                SenderId = sender.Id,
                KeyType = content.RequiredString("kind") switch
                {
                    "update_consensus_key" => SecondaryKeyType.Consensus,
                    "update_companion_key" => SecondaryKeyType.Companion,
                    _ => throw new NotImplementedException()
                },
                ActivationCycle = block.Cycle + Context.Protocol.ConsensusRightsDelay + 1,
                PublicKey = pubKey,
                PublicKeyHash = pubKeyHash,
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
                GasUsed = (int)(((result.OptionalInt64("consumed_milligas") ?? 0) + 999) / 1000)
            };
            #endregion

            #region apply operation
            Db.TryAttach(sender);
            PayFee(sender, operation.BakerFee);
            sender.Counter = operation.Counter;
            sender.UpdateSecondaryKeyCount++;

            block.Operations |= Operations.UpdateSecondaryKey;

            Cache.AppState.Get().UpdateSecondaryKeyOpsCount++;
            #endregion

            #region apply result
            if (operation.Status == OperationStatus.Applied)
            {
                Cache.AppState.Get().PendingSecondaryKeys++;
            }
            #endregion

            Proto.Manager.Set(sender);
            Db.UpdateSecondaryKeyOps.Add(operation);
            Context.UpdateSecondaryKeyOps.Add(operation);
        }

        public virtual async Task Revert(Block block, UpdateSecondaryKeyOperation operation)
        {
            var sender = await Cache.Accounts.GetAsync(operation.SenderId);
            Db.TryAttach(sender);

            #region revert result
            if (operation.Status == OperationStatus.Applied)
            {
                Cache.AppState.Get().PendingSecondaryKeys--;
            }
            #endregion

            #region revert operation
            RevertPayFee(sender, operation.BakerFee);
            sender.Counter = operation.Counter - 1;
            sender.UpdateSecondaryKeyCount--;

            Cache.AppState.Get().UpdateSecondaryKeyOpsCount--;
            #endregion

            Db.UpdateSecondaryKeyOps.Remove(operation);
            Cache.AppState.ReleaseManagerCounter();
            Cache.AppState.ReleaseOperationId();
        }

        public async Task ActivateSecondaryKeys(Block block)
        {
            if (!block.Events.HasFlag(BlockEvents.CycleBegin) || Cache.AppState.Get().PendingSecondaryKeys == 0)
                return;

            var ops = await Db.UpdateSecondaryKeyOps
                .AsNoTracking()
                .Where(x => x.ActivationCycle == block.Cycle && x.Status == OperationStatus.Applied)
                .ToListAsync();

            foreach (var op in ops.OrderBy(x => x.Id))
            {
                var baker = Cache.Accounts.GetDelegate(op.SenderId);
                Db.TryAttach(baker);
                if (op.KeyType == SecondaryKeyType.Consensus)
                    baker.ConsensusAddress = op.PublicKeyHash;
                else
                    baker.CompanionAddress = op.PublicKeyHash;
                Cache.AppState.Get().PendingSecondaryKeys--;
            }
        }

        public async Task DeactivateSecondaryKeys(Block block)
        {
            if (!block.Events.HasFlag(BlockEvents.CycleBegin))
                return;

            var ops = await Db.UpdateSecondaryKeyOps
                .AsNoTracking()
                .Where(x => x.ActivationCycle == block.Cycle && x.Status == OperationStatus.Applied)
                .ToListAsync();

            foreach (var op in ops.OrderByDescending(x => x.Id))
            {
                var baker = Cache.Accounts.GetDelegate(op.SenderId);
                Db.TryAttach(baker);

                var prevOp = await Db.UpdateSecondaryKeyOps
                    .AsNoTracking()
                    .Where(x =>
                        x.SenderId == baker.Id &&
                        x.KeyType == op.KeyType &&
                        x.ActivationCycle < op.ActivationCycle &&
                        x.Status == OperationStatus.Applied)
                    .OrderByDescending(x => x.Id)
                    .FirstOrDefaultAsync();

                if (op.KeyType == SecondaryKeyType.Consensus)
                    baker.ConsensusAddress = prevOp?.PublicKeyHash;
                else
                    baker.CompanionAddress = prevOp?.PublicKeyHash;

                Cache.AppState.Get().PendingSecondaryKeys++;
            }
        }
    }
}
