using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Netezos.Contracts;
using Netezos.Encoding;
using Tzkt.Data.Models;
using Tzkt.Data.Models.Base;

namespace Tzkt.Sync.Protocols.Proto14
{
    class ContractEventCommit(ProtocolHandler protocol) : ProtocolCommit(protocol)
    {
        public virtual async Task Apply(Block block, JsonElement content)
        {
            #region init
            var contract = (await Cache.Accounts.GetExistingAsync(content.RequiredString("source")) as Contract)!;
            var parentTx = Context.TransactionOps.OrderByDescending(x => x.Id).FirstOrDefault(x => x.TargetId == contract.Id)
                ?? throw new Exception("Event parent transaction not found");

            var result = content.Required("result");
            if (parentTx.Status != OperationStatus.Applied || result.RequiredString("status") != "applied")
                return;

            var consumedGas = (int)(((result.OptionalInt64("consumed_milligas") ?? 0) + 999) / 1000);

            var contractEvent = new ContractEvent
            {
                Id = Cache.AppState.NextEventId(),
                Level = block.Level,
                ContractId = contract.Id,
                ContractCodeHash = contract.CodeHash,
                TransactionId = parentTx.Id,
                Tag = content.OptionalString("tag")
            };

            try
            {
                var type = (content.RequiredMicheline("type") as MichelinePrim)!;
                var schema = Schema.Create(type);
                contractEvent.Type = type.ToBytes();

                var rawPayload = content.OptionalMicheline("payload") ?? new MichelinePrim { Prim = PrimType.Unit };

                contractEvent.JsonPayload = schema.Humanize(rawPayload);
                contractEvent.RawPayload = schema.Optimize(rawPayload).ToBytes();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to process event payload");
            }
            #endregion

            #region apply
            parentTx.GasUsed += consumedGas;
            parentTx.EventsCount = (parentTx.EventsCount ?? 0) + 1;
            contract.EventsCount++;
            Cache.AppState.Get().EventsCount++;
            block.Events |= BlockEvents.Events;
            #endregion

            Db.Events.Add(contractEvent);
        }

        public virtual async Task Revert(Block block)
        {
            if (!block.Events.HasFlag(BlockEvents.Events))
                return;

            var events = await Db.Events
                .AsNoTracking()
                .Where(x => x.Level == block.Level)
                .ToListAsync();

            foreach (var contractEvent in events)
            {
                var contract = (await Cache.Accounts.GetAsync(contractEvent.ContractId) as Contract)!;
                Db.TryAttach(contract);
                contract.EventsCount--;

                Cache.AppState.Get().EventsCount--;
            }

            Cache.AppState.ReleaseEventId(events.Count);

            await Db.Database.ExecuteSqlRawAsync("""
                DELETE FROM "Events"
                WHERE "Level" = {0}
                """, block.Level);
                
        }
    }
}
