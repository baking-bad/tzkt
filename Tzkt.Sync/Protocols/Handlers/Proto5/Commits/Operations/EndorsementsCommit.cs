using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto5
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
                Level = block.Level,
                Timestamp = block.Timestamp,
                OpHash = op.Hash,
                Slots = content.Metadata.Slots.Count,
                Delegate = await Cache.GetDelegateAsync(content.Metadata.Delegate),
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

            sender.EndorsementsCount++;

            block.Operations |= Operations.Endorsements;
            block.Validations += Endorsement.Slots;

            var newDeactivationLevel = sender.Staked ? GracePeriod.Reset(Endorsement.Block) : GracePeriod.Init(Endorsement.Block);
            if (sender.DeactivationLevel < newDeactivationLevel)
            {
                if (sender.DeactivationLevel <= Endorsement.Level)
                    await UpdateDelegate(sender, true);

                Endorsement.ResetDeactivation = sender.DeactivationLevel;
                sender.DeactivationLevel = newDeactivationLevel;
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

            sender.EndorsementsCount--;

            if (Endorsement.ResetDeactivation != null)
            {
                if (Endorsement.ResetDeactivation <= Endorsement.Level)
                    await UpdateDelegate(sender, false);

                sender.DeactivationLevel = (int)Endorsement.ResetDeactivation;
            }
            #endregion

            Db.EndorsementOps.Remove(Endorsement);
        }

        async Task UpdateDelegate(Data.Models.Delegate delegat, bool staked)
        {
            delegat.Staked = staked;

            foreach (var delegator in await Db.Accounts.Where(x => x.DelegateId == delegat.Id).ToListAsync())
            {
                Cache.AddAccount(delegator);
                Db.TryAttach(delegator);

                delegator.Staked = staked;
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
