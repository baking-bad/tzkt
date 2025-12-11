using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto2
{
    class DeactivationCommit : ProtocolCommit
    {
        public DeactivationCommit(ProtocolHandler protocol) : base(protocol) { }

        public virtual async Task Apply(Block block, JsonElement rawBlock)
        {
            #region init
            List<Data.Models.Delegate>? bakers = null;
            if (block.Events.HasFlag(BlockEvents.Deactivations))
            {
                var deactivated = rawBlock
                    .Required("metadata")
                    .RequiredArray("deactivated")
                    .EnumerateArray()
                    .Select(x => x.RequiredString())
                    .ToHashSet();

                bakers = [..Cache.Accounts.GetDelegates().Where(x => x.Staked && deactivated.Contains(x.Address))];
            }
            else if (block.Events.HasFlag(BlockEvents.CycleBegin))
            {
                bakers = [..Cache.Accounts.GetDelegates().Where(x => x.Staked && x.DeactivationLevel == block.Level)];
            }
            #endregion

            if (bakers == null) return;

            foreach (var baker in bakers)
            {
                Db.TryAttach(baker);
                baker.DeactivationLevel = block.Level;
                await DeactivateBaker(baker);
            }
        }

        public virtual async Task Revert(Block block)
        {
            #region init
            List<Data.Models.Delegate>? bakers = null;
            if (block.Events.HasFlag(BlockEvents.Deactivations) || block.Events.HasFlag(BlockEvents.CycleBegin))
            {
                bakers = [..Cache.Accounts.GetDelegates().Where(x => x.DeactivationLevel == block.Level)];
            }
            #endregion

            if (bakers == null) return;

            foreach (var baker in bakers)
            {
                Db.TryAttach(baker);
                baker.DeactivationLevel = block.Events.HasFlag(BlockEvents.CycleEnd) ? block.Level + 1 : block.Level;
                await ActivateBaker(baker);
            }
        }
    }
}
