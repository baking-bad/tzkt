using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto2
{
    class ProtoActivator : Proto1.ProtoActivator
    {
        public ProtoActivator(ProtocolHandler proto) : base(proto) { }

        // Activate weird delegates

        protected override async Task MigrateContext(AppState state)
        {
            var block = await Cache.Blocks.CurrentAsync();
            var protocol = await Cache.Protocols.GetAsync(block.ProtoCode);

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

            var activatedDelegates = new Dictionary<int, Data.Models.Delegate>(weirdDelegates.Count());

            foreach (var weirds in weirdDelegates)
            {
                var delegat = await UpgradeUser(weirds.First().Delegate, block.Level, protocol);
                activatedDelegates.Add(delegat.Id, delegat);

                delegat.MigrationsCount++;
                block.Operations |= Operations.Migrations;
                Db.MigrationOps.Add(new MigrationOperation
                {
                    Id = Cache.AppState.NextOperationId(),
                    Block = block,
                    Level = block.Level,
                    Timestamp = block.Timestamp,
                    Account = delegat,
                    Kind = MigrationKind.ActivateDelegate
                });
                state.MigrationOpsCount++;

                foreach (var weird in weirds)
                {
                    var delegator = weird.Contract;
                    if (delegator.DelegateId != null)
                        throw new Exception("migration error");

                    delegator.WeirdDelegate = null;

                    Db.TryAttach(delegator);
                    Cache.Accounts.Add(delegator);

                    delegator.WeirdDelegate = delegat;
                    delegator.Delegate = delegat;
                    delegator.DelegateId = delegat.Id;
                    delegator.DelegationLevel = delegator.FirstLevel;
                    delegator.Staked = true;

                    delegat.DelegatorsCount++;
                    delegat.StakingBalance += delegator.Balance;
                    delegat.DelegatedBalance += delegator.Balance;
                }
            }

            var ids = activatedDelegates.Keys.ToList();
            var weirdOriginations = await Db.OriginationOps
                .AsNoTracking()
                .Where(x => x.Contract != null && ids.Contains((int)x.Contract.WeirdDelegateId))
                .Select(x => new
                {
                    x.Contract.WeirdDelegateId,
                    Origination = x
                })
                .ToListAsync();

            foreach (var op in weirdOriginations)
            {
                var delegat = activatedDelegates[(int)op.WeirdDelegateId];

                Db.TryAttach(op.Origination);
                op.Origination.Delegate = delegat;
                if (delegat.Id != op.Origination.SenderId && delegat.Id != op.Origination.ManagerId) delegat.OriginationsCount++;
            }
        }

        protected override async Task RevertContext(AppState state)
        {
            var block = await Cache.Blocks.CurrentAsync();

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
                    Cache.Accounts.Add(delegator);

                    (delegator as Contract).WeirdDelegate = null;
                    delegator.Delegate = null;
                    delegator.DelegateId = null;
                    delegator.DelegationLevel = null;
                    delegator.Staked = false;

                    delegat.DelegatorsCount--;
                    delegat.StakingBalance -= delegator.Balance;
                    delegat.DelegatedBalance -= delegator.Balance;
                }

                if (delegat.StakingBalance != delegat.Balance || delegat.DelegatorsCount > 0)
                    throw new Exception("migration error");

                var user = DowngradeDelegate(delegat);
                user.MigrationsCount--;

                foreach (var delegator in delegators)
                    (delegator as Contract).WeirdDelegate = user;
            }

            var migrationOps = await Db.MigrationOps
                .AsNoTracking()
                .Where(x => x.Kind == MigrationKind.ActivateDelegate)
                .ToListAsync();

            Db.MigrationOps.RemoveRange(migrationOps);
            Cache.AppState.ReleaseOperationId(migrationOps.Count);

            state.MigrationOpsCount -= migrationOps.Count;

            var ids = delegates.Select(x => x.Id).ToList();
            var weirdOriginations = await Db.OriginationOps
                .AsNoTracking()
                .Where(x => x.Contract != null && ids.Contains((int)x.Contract.WeirdDelegateId))
                .Select(x => new
                {
                    x.Contract.WeirdDelegateId,
                    Origination = x
                })
                .ToListAsync();

            foreach (var op in weirdOriginations)
            {
                var delegat = delegates.First(x => x.Id == op.WeirdDelegateId);

                Db.TryAttach(op.Origination);
                op.Origination.DelegateId = null;
                if (delegat.Id != op.Origination.SenderId && delegat.Id != op.Origination.ManagerId) delegat.OriginationsCount--;
            }
        }

        Task<Data.Models.Delegate> UpgradeUser(User user, int level, Protocol proto)
        {
            var delegat = new Data.Models.Delegate
            {
                ActivationLevel = level,
                Address = user.Address,
                FirstLevel = user.FirstLevel,
                LastLevel = user.LastLevel,
                Balance = user.Balance,
                Counter = user.Counter,
                DeactivationLevel = GracePeriod.Init(level, proto),
                Delegate = null,
                DelegateId = null,
                DelegationLevel = null,
                Id = user.Id,
                Activated = user.Activated,
                DelegationsCount = user.DelegationsCount,
                OriginationsCount = user.OriginationsCount,
                TransactionsCount = user.TransactionsCount,
                RevealsCount = user.RevealsCount,
                RegisterConstantsCount = user.RegisterConstantsCount,
                SetDepositsLimitsCount = user.SetDepositsLimitsCount,
                ContractsCount = user.ContractsCount,
                MigrationsCount = user.MigrationsCount,
                PublicKey = user.PublicKey,
                Revealed = user.Revealed,
                Staked = true,
                StakingBalance = user.Balance,
                DelegatedBalance = 0,
                Type = AccountType.Delegate,
                ActiveTokensCount = user.ActiveTokensCount,
                TokenBalancesCount = user.TokenBalancesCount,
                TokenTransfersCount = user.TokenTransfersCount,
            };

            Db.Entry(user).State = EntityState.Detached;
            Db.Entry(delegat).State = EntityState.Modified;
            Cache.Accounts.Add(delegat);

            return Task.FromResult(delegat);
        }

        User DowngradeDelegate(Data.Models.Delegate delegat)
        {
            var user = new User
            {
                Address = delegat.Address,
                FirstLevel = delegat.FirstLevel,
                LastLevel = delegat.LastLevel,
                Balance = delegat.Balance,
                Counter = delegat.Counter,
                Delegate = null,
                DelegateId = null,
                DelegationLevel = null,
                Id = delegat.Id,
                Activated = delegat.Activated,
                DelegationsCount = delegat.DelegationsCount,
                OriginationsCount = delegat.OriginationsCount,
                TransactionsCount = delegat.TransactionsCount,
                RevealsCount = delegat.RevealsCount,
                RegisterConstantsCount = delegat.RegisterConstantsCount,
                SetDepositsLimitsCount = delegat.SetDepositsLimitsCount,
                ContractsCount = delegat.ContractsCount,
                MigrationsCount = delegat.MigrationsCount,
                PublicKey = delegat.PublicKey,
                Revealed = delegat.Revealed,
                Staked = false,
                Type = AccountType.User,
                ActiveTokensCount = delegat.ActiveTokensCount,
                TokenBalancesCount = delegat.TokenBalancesCount,
                TokenTransfersCount = delegat.TokenTransfersCount,
            };

            Db.Entry(delegat).State = EntityState.Detached;
            Db.Entry(user).State = EntityState.Modified;
            Cache.Accounts.Add(user);

            return user;
        }
    }
}
