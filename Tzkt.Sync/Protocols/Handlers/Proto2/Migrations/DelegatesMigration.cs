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
            var protocol = await Cache.GetProtocolAsync(block.ProtoCode);

            var weirdDelegates = (await Db.Contracts
                .AsNoTracking()
                .Include(x => x.WeirdDelegate)
                .Where(x =>
                    x.DelegateId == null &&
                    x.WeirdDelegateId != null &&
                    x.WeirdDelegate.Balance > 0)
                .Select(x => new 
                {
                    Contract = x,
                    Delegate = x.WeirdDelegate
                })
                .ToListAsync())
                .GroupBy(x => x.Delegate.Id);

            foreach (var weirds in weirdDelegates)
            {
                var delegat = await UpgradeUser(weirds.First().Delegate, block.Level, protocol);
                
                foreach (var weird in weirds)
                {
                    var delegator = weird.Contract;
                    if (delegator.DelegateId != null)
                        throw new Exception("migration error");

                    delegator.WeirdDelegate = null;

                    Db.TryAttach(delegator);
                    Cache.AddAccount(delegator);

                    delegator.WeirdDelegate = delegat;
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
                .Include(x => x.DelegatedAccounts)
                .Where(x => x.ActivationLevel == block.Level)
                .ToListAsync();

            foreach (var delegat in delegates)
            {
                var delegators = delegat.DelegatedAccounts.ToList();
                foreach (var delegator in delegators)
                {
                    Db.TryAttach(delegator);
                    Cache.AddAccount(delegator);

                    (delegator as Contract).WeirdDelegate = null;
                    delegator.Delegate = null;
                    delegator.DelegateId = null;
                    delegator.DelegationLevel = null;
                    delegator.Staked = false;

                    delegat.Delegators--;
                    delegat.StakingBalance -= delegator.Balance;
                }

                if (delegat.StakingBalance != delegat.Balance || delegat.Delegators > 0)
                    throw new Exception("migration error");

                var user = DowngradeDelegate(delegat);

                foreach (var delegator in delegators)
                    (delegator as Contract).WeirdDelegate = user;
            }
        }

        Task<Data.Models.Delegate> UpgradeUser(User user, int level, Protocol proto)
        {
            var delegat = new Data.Models.Delegate
            {
                ActivationLevel = level,
                Activation = user.Activation,
                Address = user.Address,
                AirDrop = user.AirDrop,
                FirstLevel = user.FirstLevel,
                LastLevel = user.LastLevel,
                Balance = user.Balance,
                Counter = user.Counter,
                DeactivationLevel = GracePeriod.Init(level, proto.BlocksPerCycle, proto.PreserverCycles),
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
            };

            Db.Entry(user).State = EntityState.Detached;
            Db.Entry(delegat).State = EntityState.Modified;
            Cache.AddAccount(delegat);

            return Task.FromResult(delegat);
        }

        User DowngradeDelegate(Data.Models.Delegate delegat)
        {
            var user = new User
            {
                Activation = delegat.Activation,
                Address = delegat.Address,
                AirDrop = delegat.AirDrop,
                FirstLevel = delegat.FirstLevel,
                LastLevel = delegat.LastLevel,
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
            };

            Db.Entry(delegat).State = EntityState.Detached;
            Db.Entry(user).State = EntityState.Modified;
            Cache.AddAccount(user);

            return user;
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
