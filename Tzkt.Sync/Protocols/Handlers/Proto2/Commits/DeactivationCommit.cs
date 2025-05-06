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
            IEnumerable<(Data.Models.Delegate, IEnumerable<Account>)>? delegates = null;
            if (block.Events.HasFlag(BlockEvents.Deactivations))
            {
                var deactivated = rawBlock
                    .Required("metadata")
                    .RequiredArray("deactivated")
                    .EnumerateArray()
                    .Select(x => x.RequiredString())
                    .ToList();

                delegates = (await Db.Delegates
                    .GroupJoin(Db.Accounts, x => x.Id, x => x.DelegateId, (baker, delegators) => new { baker, delegators })
                    .Where(x => x.baker.Staked && deactivated.Contains(x.baker.Address))
                    .ToListAsync())
                    .Select(x => (x.baker, x.delegators));
            }
            else if (block.Events.HasFlag(BlockEvents.CycleBegin))
            {
                delegates = (await Db.Delegates
                    .GroupJoin(Db.Accounts, x => x.Id, x => x.DelegateId, (baker, delegators) => new { baker, delegators })
                    .Where(x => x.baker.Staked && x.baker.DeactivationLevel == block.Level)
                    .ToListAsync())
                    .Select(x => (x.baker, x.delegators));
            }
            #endregion

            if (delegates == null) return;

            foreach (var (delegat, delegators) in delegates)
            {
                Cache.Accounts.Add(delegat);
                Db.TryAttach(delegat);

                delegat.DeactivationLevel = block.Level;
                delegat.Staked = false;

                foreach (var delegator in delegators)
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
            IEnumerable<(Data.Models.Delegate, IEnumerable<Account>)>? delegates = null;
            if (block.Events.HasFlag(BlockEvents.Deactivations) || block.Events.HasFlag(BlockEvents.CycleBegin))
            {
                delegates = (await Db.Delegates
                    .GroupJoin(Db.Accounts, x => x.Id, x => x.DelegateId, (baker, delegators) => new { baker, delegators })
                    .Where(x => x.baker.DeactivationLevel == block.Level)
                    .ToListAsync())
                    .Select(x => (x.baker, x.delegators));
            }
            #endregion

            if (delegates == null) return;

            foreach (var (delegat, delegators) in delegates)
            {
                Cache.Accounts.Add(delegat);
                Db.TryAttach(delegat);

                delegat.DeactivationLevel = block.Events.HasFlag(BlockEvents.CycleEnd)
                    ? block.Level + 1
                    : block.Level;
                delegat.Staked = true;

                foreach (var delegator in delegators)
                {
                    Cache.Accounts.Add(delegator);
                    Db.TryAttach(delegator);

                    delegator.Staked = true;
                }
            }
        }
    }
}
