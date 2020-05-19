using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto4
{
    class DoubleBakingCommit : ProtocolCommit
    {
        public DoubleBakingOperation DoubleBaking { get; private set; }

        DoubleBakingCommit(ProtocolHandler protocol) : base(protocol) { }

        public Task Init(Block block, RawOperation op, RawDoubleBakingEvidenceContent content)
        {
            DoubleBaking = new DoubleBakingOperation
            {
                Id = Cache.AppState.NextOperationId(),
                Block = block,
                Level = block.Level,
                Timestamp = block.Timestamp,
                OpHash = op.Hash,

                AccusedLevel = content.Block1.Level,
                Accuser = block.Baker,
                Offender = Cache.Accounts.GetDelegate(content.Metadata.BalanceUpdates.First(x => x.Change < 0).Target),
                
                AccuserReward = content.Metadata.BalanceUpdates.Where(x => x.Change > 0).Sum(x => x.Change),
                OffenderLostDeposit = content.Metadata.BalanceUpdates.Where(x => x.Change < 0 && x is DepositsUpdate).Sum(x => -x.Change),
                OffenderLostReward = content.Metadata.BalanceUpdates.Where(x => x.Change < 0 && x is RewardsUpdate).Sum(x => -x.Change),
                OffenderLostFee = content.Metadata.BalanceUpdates.Where(x => x.Change < 0 && x is FeesUpdate).Sum(x => -x.Change)
            };

            return Task.CompletedTask;
        }

        public Task Init(Block block, DoubleBakingOperation doubleBaking)
        {
            DoubleBaking = doubleBaking;

            DoubleBaking.Block ??= block;
            DoubleBaking.Block.Baker ??= Cache.Accounts.GetDelegate(block.BakerId);

            DoubleBaking.Accuser ??= Cache.Accounts.GetDelegate(doubleBaking.AccuserId);
            DoubleBaking.Offender ??= Cache.Accounts.GetDelegate(doubleBaking.OffenderId);

            return Task.CompletedTask;
        }

        public override Task Apply()
        {
            #region entities
            var block = DoubleBaking.Block;
            var accuser = DoubleBaking.Accuser;
            var offender = DoubleBaking.Offender;

            //Db.TryAttach(block);
            Db.TryAttach(accuser);
            Db.TryAttach(offender);
            #endregion

            #region apply operation
            accuser.Balance += DoubleBaking.AccuserReward;
            accuser.FrozenRewards += DoubleBaking.AccuserReward;

            offender.Balance -= DoubleBaking.OffenderLostDeposit;
            offender.FrozenDeposits -= DoubleBaking.OffenderLostDeposit;
            offender.StakingBalance -= DoubleBaking.OffenderLostDeposit;

            offender.Balance -= DoubleBaking.OffenderLostReward;
            offender.FrozenRewards -= DoubleBaking.OffenderLostReward;

            offender.Balance -= DoubleBaking.OffenderLostFee;
            offender.FrozenFees -= DoubleBaking.OffenderLostFee;
            offender.StakingBalance -= DoubleBaking.OffenderLostFee;

            accuser.DoubleBakingCount++;
            if (offender != accuser) offender.DoubleBakingCount++;

            block.Operations |= Operations.DoubleBakings;
            #endregion

            Db.DoubleBakingOps.Add(DoubleBaking);

            return Task.CompletedTask;
        }

        public override Task Revert()
        {
            #region entities
            var block = DoubleBaking.Block;
            var accuser = DoubleBaking.Accuser;
            var offender = DoubleBaking.Offender;

            //Db.TryAttach(block);
            Db.TryAttach(accuser);
            Db.TryAttach(offender);
            #endregion

            #region apply operation
            accuser.Balance -= DoubleBaking.AccuserReward;
            accuser.FrozenRewards -= DoubleBaking.AccuserReward;

            offender.Balance += DoubleBaking.OffenderLostDeposit;
            offender.FrozenDeposits += DoubleBaking.OffenderLostDeposit;
            offender.StakingBalance += DoubleBaking.OffenderLostDeposit;

            offender.Balance += DoubleBaking.OffenderLostReward;
            offender.FrozenRewards += DoubleBaking.OffenderLostReward;

            offender.Balance += DoubleBaking.OffenderLostFee;
            offender.FrozenFees += DoubleBaking.OffenderLostFee;
            offender.StakingBalance += DoubleBaking.OffenderLostFee;

            accuser.DoubleBakingCount--;
            if (offender != accuser) offender.DoubleBakingCount--;
            #endregion

            Db.DoubleBakingOps.Remove(DoubleBaking);

            return Task.CompletedTask;
        }

        #region static
        public static async Task<DoubleBakingCommit> Apply(ProtocolHandler proto, Block block, RawOperation op, RawDoubleBakingEvidenceContent content)
        {
            var commit = new DoubleBakingCommit(proto);
            await commit.Init(block, op, content);
            await commit.Apply();

            return commit;
        }

        public static async Task<DoubleBakingCommit> Revert(ProtocolHandler proto, Block block, DoubleBakingOperation op)
        {
            var commit = new DoubleBakingCommit(proto);
            await commit.Init(block, op);
            await commit.Revert();

            return commit;
        }
        #endregion
    }
}
