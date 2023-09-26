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

            var unstakedAmount = (long)((BigInteger)baker.ExternalStakedBalance * user.StakingPseudotokens / baker.StakingPseudotokens);
            op.UnstakedPseudotokens = user.StakingPseudotokens;
            op.UnstakedBalance = user.StakedBalance;
            op.UnstakedRewards = unstakedAmount - user.StakedBalance; // rewards withdrawn
            
            user.Balance += op.UnstakedRewards.Value;
            baker.DelegatedBalance += op.UnstakedRewards.Value;

            baker.StakingPseudotokens -= user.StakingPseudotokens;
            baker.TotalStakedBalance -= unstakedAmount;
            baker.ExternalStakedBalance -= unstakedAmount;
            baker.ExternalUnstakedBalance += unstakedAmount;

            user.StakingPseudotokens = 0;
            user.StakedBalance = 0;
            user.UnstakedBalance += unstakedAmount;

            Cache.Statistics.Current.TotalFrozen -= unstakedAmount;
        }

        protected override void RevertUnstake(Account sender, Data.Models.Delegate baker, DelegationOperation op)
        {
            if (op.UnstakedPseudotokens == null)
                return;

            var user = sender as User;
            user.Balance -= op.UnstakedRewards.Value;
            baker.DelegatedBalance -= op.UnstakedRewards.Value;

            baker.StakingPseudotokens += op.UnstakedPseudotokens.Value;
            baker.TotalStakedBalance += op.UnstakedBalance.Value + op.UnstakedRewards.Value;
            baker.ExternalStakedBalance += op.UnstakedBalance.Value + op.UnstakedRewards.Value;
            baker.ExternalUnstakedBalance -= op.UnstakedBalance.Value + op.UnstakedRewards.Value;

            user.StakingPseudotokens = op.UnstakedPseudotokens.Value;
            user.StakedBalance = op.UnstakedBalance.Value;
            user.UnstakedBalance -= op.UnstakedBalance.Value + op.UnstakedRewards.Value;
        }
    }
}
