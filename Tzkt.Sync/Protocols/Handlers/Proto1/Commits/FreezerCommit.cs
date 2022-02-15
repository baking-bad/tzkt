using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto1
{
    class FreezerCommit : ProtocolCommit
    {
        public IEnumerable<JsonElement> FreezerUpdates { get; private set; }

        public FreezerCommit(ProtocolHandler protocol) : base(protocol) { }

        public virtual Task Apply(Block block, JsonElement rawBlock)
        {
            if (block.Events.HasFlag(BlockEvents.CycleEnd))
            {
                FreezerUpdates = GetFreezerUpdates(block, rawBlock);
            }

            if (FreezerUpdates == null) return Task.CompletedTask;

            foreach (var update in FreezerUpdates)
            {
                #region entities
                var delegat = Cache.Accounts.GetDelegate(update.RequiredString("delegate"));

                Db.TryAttach(delegat);
                #endregion

                var change = update.RequiredInt64("change");
                switch (update.RequiredString("category")[0])
                {
                    case 'd':
                        break;
                    case 'r':
                        delegat.StakingBalance -= change;
                        break;
                    case 'f':
                        break;
                    default:
                        throw new Exception("unexpected freezer balance update type");
                }
            }

            return Task.CompletedTask;
        }

        public virtual async Task Revert(Block block)
        {
            if (block.Events.HasFlag(BlockEvents.CycleEnd))
            {
                var rawBlock = await Proto.Rpc.GetBlockAsync(block.Level);
                FreezerUpdates = GetFreezerUpdates(block, rawBlock);
            }

            if (FreezerUpdates == null) return;

            foreach (var update in FreezerUpdates)
            {
                #region entities
                var delegat = Cache.Accounts.GetDelegate(update.RequiredString("delegate"));

                Db.TryAttach(delegat);
                #endregion

                var change = update.RequiredInt64("change");
                switch (update.RequiredString("category")[0])
                {
                    case 'd':
                        break;
                    case 'r':
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
                            GetFreezerCycle(x) == block.Cycle - block.Protocol.PreservedCycles);
        }
    }
}
