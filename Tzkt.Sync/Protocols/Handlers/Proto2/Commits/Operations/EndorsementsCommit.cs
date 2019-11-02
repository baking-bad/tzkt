using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto2
{
    class EndorsementsCommit : ProtocolCommit
    {
        public EndorsementOperation Endorsement { get; private set; }

        EndorsementsCommit(ProtocolHandler protocol) : base(protocol) { }

        public async Task Init(Block block, RawOperation op, RawEndorsementContent content)
        {
            Endorsement = new EndorsementOperation
            {
                Id = await Cache.NextCounterAsync(),
                Block = block,
                Timestamp = block.Timestamp,
                OpHash = op.Hash,
                Slots = content.Metadata.Slots.Count,
                Delegate = (Data.Models.Delegate)await Cache.GetAccountAsync(content.Metadata.Delegate),
                Reward = content.Metadata.BalanceUpdates.FirstOrDefault(x => x is RewardsUpdate)?.Change ?? 0
            };
        }

        public async Task Init(Block block, EndorsementOperation endorsement)
        {
            Endorsement = endorsement;

            Endorsement.Block ??= block;
            Endorsement.Block.Protocol ??= await Cache.GetProtocolAsync(block.ProtoCode);

            Endorsement.Delegate ??= (Data.Models.Delegate)await Cache.GetAccountAsync(endorsement.DelegateId);
        }

        public override async Task Apply()
        {
            #region entities
            var block = Endorsement.Block;
            var sender = Endorsement.Delegate;

            //Db.TryAttach(block);
            Db.TryAttach(sender);
            #endregion

            #region apply operation
            sender.Balance += Endorsement.Reward;
            sender.FrozenRewards += Endorsement.Reward;
            sender.FrozenDeposits += block.Protocol.EndorsementDeposit * Endorsement.Slots;

            sender.Operations |= Operations.Endorsements;
            block.Operations |= Operations.Endorsements;

            block.Validations++;

            if (!sender.Staked)
            {
                await ReactivateDelegate(sender);

                Endorsement.DelegateChange = new DelegateChange
                {
                    Delegate = sender,
                    Level = block.Level,
                    Type = DelegateChangeType.Reactivated
                };
            }
            #endregion

            Db.EndorsementOps.Add(Endorsement);
        }

        public override async Task Revert()
        {
            #region entities
            var block = Endorsement.Block;
            var sender = Endorsement.Delegate;

            //Db.TryAttach(block);
            Db.TryAttach(sender);
            #endregion

            #region apply operation
            sender.Balance -= Endorsement.Reward;
            sender.FrozenRewards -= Endorsement.Reward;
            sender.FrozenDeposits -= block.Protocol.EndorsementDeposit * Endorsement.Slots;

            if (!await Db.EndorsementOps.AnyAsync(x => x.DelegateId == sender.Id && x.Id < Endorsement.Id))
                sender.Operations &= ~Operations.Endorsements;
            #endregion

            if (Endorsement.DelegateChangeId != null)
            {
                var prevChange = await Db.DelegateChanges
                    .Where(x => x.DelegateId == sender.Id && x.Level < Endorsement.Level)
                    .OrderByDescending(x => x.Level)
                    .FirstOrDefaultAsync();

                if (prevChange.Type != DelegateChangeType.Deactivated)
                    throw new Exception("unexpected delegate change type");

                await DeactivateDelegate(sender, prevChange.Level);

                Db.DelegateChanges.Remove(new DelegateChange
                {
                    Id = (int)Endorsement.DelegateChangeId
                });
            }

            Db.EndorsementOps.Remove(Endorsement);
        }

        async Task DeactivateDelegate(Data.Models.Delegate delegat, int deactivationLevel)
        {
            delegat.DeactivationBlock = await Db.Blocks.FirstOrDefaultAsync(x => x.Level == deactivationLevel);
            delegat.DeactivationLevel = deactivationLevel;
            delegat.Staked = false;

            foreach (var delegator in await Db.Accounts.Where(x => x.DelegateId == delegat.Id).ToListAsync())
            {
                Cache.AddAccount(delegator);
                Db.TryAttach(delegator);

                delegator.Staked = false;
            }
        }

        async Task ReactivateDelegate(Data.Models.Delegate delegat)
        {
            delegat.DeactivationBlock = null;
            delegat.DeactivationLevel = null;
            delegat.Staked = true;

            foreach (var delegator in await Db.Accounts.Where(x => x.DelegateId == delegat.Id).ToListAsync())
            {
                Cache.AddAccount(delegator);
                Db.TryAttach(delegator);

                delegator.Staked = true;
            }
        }

        #region static
        public static async Task<EndorsementsCommit> Apply(ProtocolHandler proto, Block block, RawOperation op, RawEndorsementContent content)
        {
            var commit = new EndorsementsCommit(proto);
            await commit.Init(block, op, content);
            await commit.Apply();

            return commit;
        }

        public static async Task<EndorsementsCommit> Revert(ProtocolHandler proto, Block block, EndorsementOperation op)
        {
            var commit = new EndorsementsCommit(proto);
            await commit.Init(block, op);
            await commit.Revert();

            return commit;
        }
        #endregion
    }
}
