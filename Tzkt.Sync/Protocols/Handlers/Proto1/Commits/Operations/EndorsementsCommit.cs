using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto1
{
    class EndorsementsCommit : ProtocolCommit
    {
        public List<EndorsementOperation> Endorsements { get; protected set; }
        public Protocol Protocol { get; private set; }

        public EndorsementsCommit(ProtocolHandler protocol, List<ICommit> commits) : base(protocol, commits) { }

        public override async Task Init()
        {
            var block = await Cache.GetCurrentBlockAsync();

            Protocol = await Cache.GetCurrentProtocolAsync();
            Endorsements = await Db.EndorsementOps.Where(x => x.Level == block.Level).ToListAsync();
            foreach (var op in Endorsements)
            {
                op.Block = block;
                op.Delegate = (Data.Models.Delegate)await Cache.GetAccountAsync(op.DelegateId);
            }
        }

        public override async Task Init(IBlock block)
        {
            var rawBlock = block as RawBlock;
            var parsedBlock = FindCommit<BlockCommit>().Block;

            Protocol = await Cache.GetProtocolAsync(block.Protocol);
            Endorsements = new List<EndorsementOperation>();
            foreach (var op in rawBlock.Operations[0])
            {
                foreach (var content in op.Contents.Where(x => x is RawEndorsementContent))
                {
                    var endorsement = content as RawEndorsementContent;

                    Endorsements.Add(new EndorsementOperation
                    {
                        Block = parsedBlock,
                        Timestamp = parsedBlock.Timestamp,
                        OpHash = op.Hash,
                        Slots = endorsement.Metadata.Slots.Count,
                        Delegate = (Data.Models.Delegate)await Cache.GetAccountAsync(endorsement.Metadata.Delegate),
                        Reward = endorsement.Metadata.BalanceUpdates.FirstOrDefault(x => x is RewardsUpdate)?.Change ?? 0
                    });
                }
            }
        }

        public override Task Apply()
        {
            if (Endorsements == null)
                throw new Exception("Commit is not initialized");

            foreach (var endorsement in Endorsements)
            {
                #region entities
                var block = endorsement.Block;
                var sender = endorsement.Delegate;

                //Db.TryAttach(block);
                Db.TryAttach(sender);
                #endregion

                #region apply operation
                sender.Balance += endorsement.Reward;
                sender.FrozenRewards += endorsement.Reward;
                sender.FrozenDeposits += Protocol.EndorsementDeposit * endorsement.Slots;

                sender.Operations |= Operations.Endorsements;
                block.Operations |= Operations.Endorsements;

                block.Validations++;
                #endregion

                Db.EndorsementOps.Add(endorsement);
            }
            return Task.CompletedTask;
        }

        public override async Task Revert()
        {
            if (Endorsements == null)
                throw new Exception("Commit is not initialized");

            foreach (var endorsement in Endorsements)
            {
                #region entities
                var block = endorsement.Block;
                var sender = endorsement.Delegate;

                //Db.TryAttach(block);
                Db.TryAttach(sender);
                #endregion

                #region apply operation
                sender.Balance -= endorsement.Reward;
                sender.FrozenRewards -= endorsement.Reward;
                sender.FrozenDeposits -= Protocol.EndorsementDeposit * endorsement.Slots;

                if (!await Db.EndorsementOps.AnyAsync(x => x.DelegateId == sender.Id && x.Level < endorsement.Level))
                    sender.Operations &= ~Operations.Endorsements;
                #endregion

                Db.EndorsementOps.Remove(endorsement);
            }
        }

        #region static
        public static async Task<EndorsementsCommit> Create(ProtocolHandler protocol, List<ICommit> commits, RawBlock rawBlock)
        {
            var commit = new EndorsementsCommit(protocol, commits);
            await commit.Init(rawBlock);
            return commit;
        }

        public static async Task<EndorsementsCommit> Create(ProtocolHandler protocol, List<ICommit> commits)
        {
            var commit = new EndorsementsCommit(protocol, commits);
            await commit.Init();
            return commit;
        }
        #endregion
    }
}
