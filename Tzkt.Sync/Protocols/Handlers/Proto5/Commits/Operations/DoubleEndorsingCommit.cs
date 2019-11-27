using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto5
{
    class DoubleEndorsingCommit : ProtocolCommit
    {
        public DoubleEndorsingOperation DoubleEndorsing { get; private set; }

        DoubleEndorsingCommit(ProtocolHandler protocol) : base(protocol) { }

        public async Task Init(Block block, RawOperation op, RawDoubleEndorsingEvidenceContent content)
        {
            DoubleEndorsing = new DoubleEndorsingOperation
            {
                Id = await Cache.NextCounterAsync(),
                Block = block,
                Level = block.Level,
                Timestamp = block.Timestamp,
                OpHash = op.Hash,

                AccusedLevel = content.Op1.Operations.Level,
                Accuser = block.Baker,
                Offender = await Cache.GetDelegateAsync(content.Metadata.BalanceUpdates.First(x => x.Change < 0).Target),
                
                AccuserReward = content.Metadata.BalanceUpdates.Where(x => x.Change > 0).Sum(x => x.Change),
                OffenderLostDeposit = content.Metadata.BalanceUpdates.Where(x => x.Change < 0 && x is DepositsUpdate).Sum(x => -x.Change),
                OffenderLostReward = content.Metadata.BalanceUpdates.Where(x => x.Change < 0 && x is RewardsUpdate).Sum(x => -x.Change),
                OffenderLostFee = content.Metadata.BalanceUpdates.Where(x => x.Change < 0 && x is FeesUpdate).Sum(x => -x.Change)
            };
        }

        public async Task Init(Block block, DoubleEndorsingOperation doubleEndorsing)
        {
            DoubleEndorsing = doubleEndorsing;

            DoubleEndorsing.Block ??= block;
            DoubleEndorsing.Block.Baker ??= (Data.Models.Delegate)await Cache.GetAccountAsync(block.BakerId);

            DoubleEndorsing.Accuser ??= (Data.Models.Delegate)await Cache.GetAccountAsync(doubleEndorsing.AccuserId);
            DoubleEndorsing.Offender ??= (Data.Models.Delegate)await Cache.GetAccountAsync(doubleEndorsing.OffenderId);
        }

        public override Task Apply()
        {
            #region entities
            var block = DoubleEndorsing.Block;
            var accuser = DoubleEndorsing.Accuser;
            var offender = DoubleEndorsing.Offender;

            //Db.TryAttach(block);
            Db.TryAttach(accuser);
            Db.TryAttach(offender);
            #endregion

            #region apply operation
            accuser.Balance += DoubleEndorsing.AccuserReward;
            accuser.FrozenRewards += DoubleEndorsing.AccuserReward;

            offender.Balance -= DoubleEndorsing.OffenderLostDeposit;
            offender.FrozenDeposits -= DoubleEndorsing.OffenderLostDeposit;
            offender.StakingBalance -= DoubleEndorsing.OffenderLostDeposit;

            offender.Balance -= DoubleEndorsing.OffenderLostReward;
            offender.FrozenRewards -= DoubleEndorsing.OffenderLostReward;

            offender.Balance -= DoubleEndorsing.OffenderLostFee;
            offender.FrozenFees -= DoubleEndorsing.OffenderLostFee;
            offender.StakingBalance -= DoubleEndorsing.OffenderLostFee;

            accuser.DoubleEndorsingCount++;
            if (offender != accuser) offender.DoubleEndorsingCount++;

            block.Operations |= Operations.DoubleEndorsings;
            #endregion

            Db.DoubleEndorsingOps.Add(DoubleEndorsing);

            return Task.CompletedTask;
        }

        public override Task Revert()
        {
            #region entities
            var block = DoubleEndorsing.Block;
            var accuser = DoubleEndorsing.Accuser;
            var offender = DoubleEndorsing.Offender;

            //Db.TryAttach(block);
            Db.TryAttach(accuser);
            Db.TryAttach(offender);
            #endregion

            #region apply operation
            accuser.Balance -= DoubleEndorsing.AccuserReward;
            accuser.FrozenRewards -= DoubleEndorsing.AccuserReward;

            offender.Balance += DoubleEndorsing.OffenderLostDeposit;
            offender.FrozenDeposits += DoubleEndorsing.OffenderLostDeposit;
            offender.StakingBalance += DoubleEndorsing.OffenderLostDeposit;

            offender.Balance += DoubleEndorsing.OffenderLostReward;
            offender.FrozenRewards += DoubleEndorsing.OffenderLostReward;

            offender.Balance += DoubleEndorsing.OffenderLostFee;
            offender.FrozenFees += DoubleEndorsing.OffenderLostFee;
            offender.StakingBalance += DoubleEndorsing.OffenderLostFee;

            accuser.DoubleEndorsingCount--;
            if (offender != accuser) offender.DoubleEndorsingCount--;
            #endregion

            Db.DoubleEndorsingOps.Remove(DoubleEndorsing);

            return Task.CompletedTask;
        }

        #region static
        public static async Task<DoubleEndorsingCommit> Apply(ProtocolHandler proto, Block block, RawOperation op, RawDoubleEndorsingEvidenceContent content)
        {
            var commit = new DoubleEndorsingCommit(proto);
            await commit.Init(block, op, content);
            await commit.Apply();

            return commit;
        }

        public static async Task<DoubleEndorsingCommit> Revert(ProtocolHandler proto, Block block, DoubleEndorsingOperation op)
        {
            var commit = new DoubleEndorsingCommit(proto);
            await commit.Init(block, op);
            await commit.Revert();

            return commit;
        }
        #endregion
    }
}
