using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
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
            List<Data.Models.Delegate> delegates = null;
            if (block.Events.HasFlag(BlockEvents.Deactivations))
            {
                var deactivated = rawBlock
                    .Required("metadata")
                    .RequiredArray("deactivated")
                    .EnumerateArray()
                    .Select(x => x.RequiredString())
                    .ToList();

                delegates = await Db.Delegates
                    .Include(x => x.DelegatedAccounts)
                    .Where(x => x.Staked && deactivated.Contains(x.Address))
                    .ToListAsync();
            }
            else if (block.Events.HasFlag(BlockEvents.CycleBegin))
            {
                delegates = await Db.Delegates
                    .Include(x => x.DelegatedAccounts)
                    .Where(x => x.Staked && x.DeactivationLevel == block.Level)
                    .ToListAsync();
            }
            #endregion

            if (delegates == null) return;

            foreach (var delegat in delegates)
            {
                Cache.Accounts.Add(delegat);
                Db.TryAttach(delegat);

                delegat.DeactivationLevel = block.Level;
                delegat.Staked = false;

                foreach (var delegator in delegat.DelegatedAccounts)
                {
                    Cache.Accounts.Add(delegator);
                    Db.TryAttach(delegator);

                    delegator.Staked = false;
                }
            }
        }

        public virtual async Task Revert(Block block)
        {
            #region init
            List<Data.Models.Delegate> delegates = null;
            if (block.Events.HasFlag(BlockEvents.Deactivations) || block.Events.HasFlag(BlockEvents.CycleBegin))
            {
                delegates = await Db.Delegates
                    .Include(x => x.DelegatedAccounts)
                    .Where(x => x.DeactivationLevel == block.Level)
                    .ToListAsync();
            }
            #endregion

            if (delegates == null) return;

            foreach (var delegat in delegates)
            {
                Cache.Accounts.Add(delegat);
                Db.TryAttach(delegat);

                delegat.DeactivationLevel = block.Events.HasFlag(BlockEvents.CycleEnd)
                    ? block.Level + 1
                    : block.Level;
                delegat.Staked = true;

                foreach (var delegator in delegat.DelegatedAccounts)
                {
                    Cache.Accounts.Add(delegator);
                    Db.TryAttach(delegator);

                    delegator.Staked = true;
                }
            }
        }
    }
}
