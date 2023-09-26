using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto12
{
    class FreezerCommit : ProtocolCommit
    {
        public FreezerCommit(ProtocolHandler protocol) : base(protocol) { }

        public virtual void Apply(Block block, JsonElement rawBlock)
        {
            if (!block.Events.HasFlag(BlockEvents.CycleEnd))
                return;

            foreach (var update in rawBlock.Required("metadata").RequiredArray("balance_updates").EnumerateArray()
                .Where(x => x.RequiredString("origin") == "block" &&
                            x.RequiredString("kind") == "freezer" &&
                            x.RequiredString("category") == "deposits"))
            {
                var baker = Cache.Accounts.GetDelegate(update.RequiredString("delegate"));
                var freezerUpdate = new FreezerUpdate
                {
                    BakerId = baker.Id,
                    Cycle = block.Cycle,
                    Change = update.RequiredInt64("change")
                };

                Cache.Statistics.Current.TotalFrozen += freezerUpdate.Change;

                Db.FreezerUpdates.Add(freezerUpdate);
            }
        }

        public virtual async Task Revert(Block block)
        {
            if (!block.Events.HasFlag(BlockEvents.CycleEnd))
                return;

            foreach (var freezerUpdate in await Db.FreezerUpdates.Where(x => x.Cycle == block.Cycle).ToListAsync())
            {
                Db.FreezerUpdates.Remove(freezerUpdate);
            }
        }
    }
}
