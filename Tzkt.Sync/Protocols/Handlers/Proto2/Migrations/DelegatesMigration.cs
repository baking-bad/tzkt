using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

using Tzkt.Data.Models;
using Tzkt.Sync.Protocols.Proto2.Migrations;

namespace Tzkt.Sync.Protocols.Proto2
{
    class DelegatesMigration : ProtocolCommit
    {
        DelegatesMigration(ProtocolHandler protocol) : base(protocol) { }

        public override async Task Apply()
        {
            var block = await Cache.GetCurrentBlockAsync();

            using var stream = await Proto.Node.GetDelegatesAsync(block.Level);
            var remoteDelegates = await JsonSerializer.DeserializeAsync<List<string>>(stream, SerializerOptions.Default);

            foreach (var delegateAddress in remoteDelegates)
            {
                var account = await Cache.GetAccountAsync(delegateAddress);
                if (account is Data.Models.Delegate) continue;
                
                var delegat = await UpgradeUser(account as User, block.Level);

                using var delegateStream = await Proto.Node.GetDelegateAsync(block.Level, delegateAddress);
                var remoteDelegate = await JsonSerializer.DeserializeAsync<RemoteDelegate>(delegateStream, SerializerOptions.Default);

                foreach (var delegatorAddress in remoteDelegate.Delegators)
                {
                    var delegator = await Cache.GetAccountAsync(delegatorAddress);

                    if (delegator.DelegateId != null)
                        throw new Exception("migration error");

                    Db.TryAttach(delegator);

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
                var cachedDelegate = (Data.Models.Delegate)await Cache.GetAccountAsync(delegat);

                foreach (var delegator in delegat.DelegatedAccounts.ToList())
                {
                    var cachedDelegator = await Cache.GetAccountAsync(delegator);
                    Db.TryAttach(cachedDelegator);

                    cachedDelegator.Delegate = null;
                    cachedDelegator.DelegateId = null;
                    cachedDelegator.DelegationLevel = null;
                    cachedDelegator.Staked = false;

                    cachedDelegate.Delegators--;
                    cachedDelegate.StakingBalance -= delegator.Balance;
                }

                if (cachedDelegate.StakingBalance != cachedDelegate.Balance || cachedDelegate.Delegators > 0)
                    throw new Exception("migration error");

                DowngradeDelegate(cachedDelegate);
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
                Type = AccountType.Delegate
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
                Type = AccountType.User
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
