using System.Text.Json;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto1
{
    class FreezerCommit : ProtocolCommit
    {
        public FreezerCommit(ProtocolHandler protocol) : base(protocol) { }

        public void Apply(Block block, JsonElement rawBlock)
        {
            if (!block.Events.HasFlag(BlockEvents.CycleEnd))
                return;

            foreach (var update in GetFreezerUpdates(block, rawBlock))
            {
                var change = update.RequiredInt64("change");
                switch (update.RequiredString("category")[0])
                {
                    case 'd':
                        break;
                    case 'r':
                        var delegat = Cache.Accounts.GetDelegate(update.RequiredString("delegate"));
                        Db.TryAttach(delegat);
                        delegat.StakingBalance -= change;
                        break;
                    case 'f':
                        break;
                    default:
                        throw new Exception("unexpected freezer balance update type");
                }

                Cache.Statistics.Current.TotalFrozen += change;
            }

            return;
        }

        public async Task Revert(Block block)
        {
            if (!block.Events.HasFlag(BlockEvents.CycleEnd))
                return;

            var rawBlock = await Proto.Rpc.GetBlockAsync(block.Level);

            foreach (var update in GetFreezerUpdates(block, rawBlock))
            {
                var change = update.RequiredInt64("change");
                switch (update.RequiredString("category")[0])
                {
                    case 'd':
                        break;
                    case 'r':
                        var delegat = Cache.Accounts.GetDelegate(update.RequiredString("delegate"));
                        Db.TryAttach(delegat);
                        delegat.StakingBalance += change;
                        break;
                    case 'f':
                        break;
                    default:
                        throw new Exception("unexpected freezer balance update type");
                }
            }
        }

        protected virtual int GetFreezerCycle(JsonElement el) => el.RequiredInt32("level");

        protected virtual IEnumerable<JsonElement> GetFreezerUpdates(Block block, JsonElement rawBlock)
        {
            return rawBlock
                .Required("metadata")
                .Required("balance_updates")
                .EnumerateArray()
                .Where(x => x.RequiredString("kind")[0] == 'f' &&
                            x.RequiredInt64("change") < 0 &&
                            GetFreezerCycle(x) == block.Cycle - block.Protocol.ConsensusRightsDelay);
        }
    }
}
