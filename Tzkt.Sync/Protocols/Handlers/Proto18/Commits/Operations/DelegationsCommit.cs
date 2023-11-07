using System.Numerics;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto18
{
    class DelegationsCommit : Proto14.DelegationsCommit
    {
        public DelegationsCommit(ProtocolHandler protocol) : base(protocol) { }

        protected override void Unstake(Account sender, Data.Models.Delegate baker, DelegationOperation op)
        {
            if (baker == null || sender is not User user || user.StakedBalance == 0)
                return;

            var unstakedAmount = (long)((BigInteger)baker.ExternalStakedBalance * user.StakedPseudotokens / baker.IssuedPseudotokens);
            op.UnstakedPseudotokens = user.StakedPseudotokens;
            op.UnstakedBalance = user.StakedBalance;
            op.UnstakedRewards = unstakedAmount - user.StakedBalance; // rewards withdrawn
            
            user.Balance += op.UnstakedRewards.Value;

            baker.IssuedPseudotokens -= user.StakedPseudotokens;
            baker.TotalStakedBalance -= unstakedAmount;
            baker.ExternalStakedBalance -= unstakedAmount;
            baker.ExternalUnstakedBalance += unstakedAmount;
            baker.DelegatedBalance += unstakedAmount;

            user.StakedPseudotokens = 0;
            user.StakedBalance = 0;
            user.UnstakedBalance += unstakedAmount;

            if (user.UnstakedBalance > 0)
            {
                if (user.UnstakedBakerId == null)
                    user.UnstakedBakerId = baker.Id;
                else if (user.UnstakedBakerId != baker.Id)
                    throw new Exception("Multiple unstaked bakers are not expected");
            }

            Cache.Statistics.Current.TotalFrozen -= unstakedAmount;

            #region temporary diagnostics
            var remoteSender = Proto.Node.GetAsync($"chains/main/blocks/{op.Level}/context/raw/json/contracts/index/{user.Address}").Result;

            if ((remoteSender.OptionalInt64("staking_pseudotokens") ?? 0) != user.StakedPseudotokens)
                throw new Exception("Wrong sender.StakedPseudotokens");

            var remoteDelegate = Proto.Node.GetAsync($"chains/main/blocks/{op.Level}/context/raw/json/contracts/index/{baker.Address}").Result;

            if ((remoteDelegate.OptionalInt64("frozen_deposits_pseudotokens") ?? 0) != baker.IssuedPseudotokens)
                throw new Exception("Wrong senderDelegate.IssuedPseudotokens");
            #endregion
        }

        protected override void RevertUnstake(Account sender, Data.Models.Delegate baker, DelegationOperation op)
        {
            if (op.UnstakedPseudotokens == null)
                return;

            var user = sender as User;
            user.Balance -= op.UnstakedRewards.Value;

            baker.IssuedPseudotokens += op.UnstakedPseudotokens.Value;
            baker.TotalStakedBalance += op.UnstakedBalance.Value + op.UnstakedRewards.Value;
            baker.ExternalStakedBalance += op.UnstakedBalance.Value + op.UnstakedRewards.Value;
            baker.ExternalUnstakedBalance -= op.UnstakedBalance.Value + op.UnstakedRewards.Value;
            baker.DelegatedBalance -= op.UnstakedBalance.Value + op.UnstakedRewards.Value;

            user.StakedPseudotokens = op.UnstakedPseudotokens.Value;
            user.StakedBalance = op.UnstakedBalance.Value;
            user.UnstakedBalance -= op.UnstakedBalance.Value + op.UnstakedRewards.Value;

            if (user.UnstakedBalance == 0)
                user.UnstakedBakerId = null;

            #region temporary diagnostics
            var remoteSender = Proto.Node.GetAsync($"chains/main/blocks/{op.Level - 1}/context/raw/json/contracts/index/{user.Address}").Result;

            if ((remoteSender.OptionalInt64("staking_pseudotokens") ?? 0) != user.StakedPseudotokens)
                throw new Exception("Wrong sender.StakedPseudotokens");

            var remoteDelegate = Proto.Node.GetAsync($"chains/main/blocks/{op.Level - 1}/context/raw/json/contracts/index/{baker.Address}").Result;

            if ((remoteDelegate.OptionalInt64("frozen_deposits_pseudotokens") ?? 0) != baker.IssuedPseudotokens)
                throw new Exception("Wrong senderDelegate.IssuedPseudotokens");
            #endregion
        }
    }
}
