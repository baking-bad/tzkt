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
        #region constants
        protected virtual int EndorsementDeposit => 0;
        protected virtual int EndorsementReward => 0;
        #endregion

        public List<EndorsementOperation> Endorsements { get; protected set; }

        public EndorsementsCommit(ProtocolHandler protocol, List<ICommit> commits) : base(protocol, commits) { }

        public override async Task Init(IBlock block)
        {
            var rawBlock = block as RawBlock;
            var parsedBlock = FindCommit<BlockCommit>().Block;

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
                        Delegate = (Data.Models.Delegate)await Accounts.GetAccountAsync(endorsement.Metadata.Delegate),
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
                #region balances
                endorsement.Delegate.Balance += endorsement.Reward;
                endorsement.Delegate.FrozenRewards += endorsement.Reward;
                endorsement.Delegate.FrozenDeposits += EndorsementDeposit * endorsement.Slots;
                #endregion

                #region counters
                endorsement.Delegate.Operations |= Operations.Endorsements;
                endorsement.Block.Operations |= Operations.Endorsements;
                endorsement.Block.Validations++;
                #endregion

                Db.Delegates.Update(endorsement.Delegate);
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
                #region balances
                var baker = (Data.Models.Delegate)await Accounts.GetAccountAsync(endorsement.DelegateId);
                baker.Balance -= endorsement.Reward;
                baker.FrozenRewards -= endorsement.Reward;
                baker.FrozenDeposits -= EndorsementDeposit * endorsement.Slots;
                #endregion

                #region counters
                if (!await Db.EndorsementOps.AnyAsync(x => x.DelegateId == baker.Id && x.Id != endorsement.Id))
                    baker.Operations &= ~Operations.Endorsements;
                #endregion

                Db.Delegates.Update(baker);
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

        public static Task<EndorsementsCommit> Create(ProtocolHandler protocol, List<ICommit> commits, List<EndorsementOperation> endorsements)
        {
            var commit = new EndorsementsCommit(protocol, commits) { Endorsements = endorsements };
            return Task.FromResult(commit);
        }
        #endregion
    }
}
