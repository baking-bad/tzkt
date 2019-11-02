using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

using Tzkt.Data.Models;
using Tzkt.Data.Models.Base;

namespace Tzkt.Sync.Protocols.Proto2
{
    class DelegatesMigration : ProtocolCommit
    {
        DelegatesMigration(ProtocolHandler protocol) : base(protocol) { }

        public override async Task Apply()
        {
            var block = await Cache.GetCurrentBlockAsync();

            var weirdDelegates = (await Db.WeirdDelegations
                .AsNoTracking()
                .Include(x => x.Delegate)
                .Include(x => x.Origination)
                .ThenInclude(x => x.Contract)
                .Where(x =>
                    x.Delegate.Balance > 0 &&
                    x.Delegate.Type == AccountType.User &&
                    x.Origination.Status == OperationStatus.Applied &&
                    x.Origination.Contract.DelegateId == null)
                .Select(x => new { x.Origination.Contract, x.Delegate })
                .ToListAsync())
                .GroupBy(x => x.Delegate.Id);

            foreach (var weirds in weirdDelegates)
            {
                var delegat = await UpgradeUser(weirds.First().Delegate, block.Level);
                
                Db.DelegateChanges.Add(new DelegateChange
                {
                    Delegate = delegat,
                    Level = block.Level,
                    Type = DelegateChangeType.Activated
                });
                
                foreach (var weird in weirds)
                {
                    var delegator = weird.Contract;
                    if (delegator.DelegateId != null)
                        throw new Exception("migration error");

                    Db.TryAttach(delegator);
                    Cache.AddAccount(delegator);

                    delegator.Delegate = delegat;
                    delegator.DelegateId = delegat.Id;
                    delegator.DelegationLevel = block.Level;
                    delegator.Staked = true;

                    delegat.Delegators++;
                    delegat.StakingBalance += delegator.Balance;
                }
            }
        }

        public override async Task Revert()
        {
            var block = await Cache.GetCurrentBlockAsync();

            var delegates = await Db.Delegates
                .AsNoTracking()
                .Include(x => x.DelegateChanges)
                .Include(x => x.DelegatedAccounts)
                .Where(x => x.ActivationLevel == block.Level)
                .ToListAsync();

            foreach (var delegat in delegates)
            {
                foreach (var delegator in delegat.DelegatedAccounts.ToList())
                {
                    Db.TryAttach(delegator);
                    Cache.AddAccount(delegator);

                    delegator.Delegate = null;
                    delegator.DelegateId = null;
                    delegator.DelegationLevel = null;
                    delegator.Staked = false;

                    delegat.Delegators--;
                    delegat.StakingBalance -= delegator.Balance;
                }

                if (delegat.StakingBalance != delegat.Balance || delegat.Delegators > 0)
                    throw new Exception("migration error");

                Db.DelegateChanges.RemoveRange(delegat.DelegateChanges);
                DowngradeDelegate(delegat);
            }
        }

        async Task<Data.Models.Delegate> UpgradeUser(User user, int level)
        {
            var delegat = new Data.Models.Delegate
            {
                ActivationBlock = await Cache.GetBlockAsync(level),
                ActivationLevel = level,
                Activation = user.Activation,
                Address = user.Address,
                Balance = user.Balance,
                Counter = user.Counter,
                DeactivationBlock = null,
                DeactivationLevel = null,
                Delegate = null,
                DelegateId = null,
                DelegationLevel = null,
                Id = user.Id,
                Operations = user.Operations,
                OriginatedContracts = user.OriginatedContracts,
                PublicKey = user.PublicKey,
                ReceivedTransactions = user.ReceivedTransactions,
                SentReveals = user.SentReveals,
                SentDelegations = user.SentDelegations,
                SentOriginations = user.SentOriginations,
                SentTransactions = user.SentTransactions,
                Staked = true,
                StakingBalance = user.Balance,
                Type = AccountType.Delegate,
                WeirdDelegations = user.WeirdDelegations
            };

            Db.Entry(user).State = EntityState.Detached;
            Db.Entry(delegat).State = EntityState.Modified;
            Cache.AddAccount(delegat);

            return delegat;
        }

        void DowngradeDelegate(Data.Models.Delegate delegat)
        {
            var user = new User
            {
                Activation = delegat.Activation,
                Address = delegat.Address,
                Balance = delegat.Balance,
                Counter = delegat.Counter,
                Delegate = null,
                DelegateId = null,
                DelegationLevel = null,
                Id = delegat.Id,
                Operations = delegat.Operations,
                OriginatedContracts = delegat.OriginatedContracts,
                PublicKey = delegat.PublicKey,
                ReceivedTransactions = delegat.ReceivedTransactions,
                SentReveals = delegat.SentReveals,
                SentDelegations = delegat.SentDelegations,
                SentOriginations = delegat.SentOriginations,
                SentTransactions = delegat.SentTransactions,
                Staked = false,
                Type = AccountType.User,
                WeirdDelegations = delegat.WeirdDelegations
            };

            Db.Entry(delegat).State = EntityState.Detached;
            Db.Entry(user).State = EntityState.Modified;
            Cache.AddAccount(user);
        }

        #region static
        public static async Task<DelegatesMigration> Apply(ProtocolHandler proto)
        {
            var commit = new DelegatesMigration(proto);
            await commit.Apply();

            return commit;
        }

        public static async Task<DelegatesMigration> Revert(ProtocolHandler proto)
        {
            var commit = new DelegatesMigration(proto);
            await commit.Revert();

            return commit;
        }
        #endregion
    }
}
