using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models;
using Tzkt.Data.Models.Base;

namespace Tzkt.Sync.Protocols.Proto2
{
    class ProtoActivator(ProtocolHandler proto) : Proto1.ProtoActivator(proto)
    {
        protected override async Task MigrateContext(AppState state)
        {
            var block = await Cache.Blocks.CurrentAsync();
            var protocol = await Cache.Protocols.GetAsync(block.ProtoCode);
            
            var weirdDelegates = (await Db.OriginationOps
                .AsNoTracking()
                .Join(Db.Users, x => x.DelegateId, x => x.Id, (op, delegat) => new { op, delegat })
                .Join(Db.Accounts, x => x.op.ContractId, x => x.Id, (opDelegat, contract) => new { opDelegat.op, opDelegat.delegat, contract })
                .Where(x =>
                    x.op.Status == OperationStatus.Applied &&
                    x.op.DelegateId != null &&
                    x.delegat.Type != AccountType.Delegate &&
                    x.delegat.Balance > 0 &&
                    x.contract.DelegateId == null)
                .Select(x => new
                {
                    Contract = x.contract,
                    WeirdDelegate = x.delegat
                })
                .ToListAsync())
                .GroupBy(x => x.WeirdDelegate.Id);

            var activatedDelegates = new Dictionary<int, Data.Models.Delegate>(weirdDelegates.Count());

            Db.TryAttach(block);
            Db.TryAttach(state);

            foreach (var weirds in weirdDelegates)
            {
                var delegat = RegisterBaker(weirds.First().WeirdDelegate, protocol);
                activatedDelegates.Add(delegat.Id, delegat);

                delegat.MigrationsCount++;
                delegat.LastLevel = block.Level;

                block.Operations |= Operations.Migrations;

                var migration = new MigrationOperation
                {
                    Id = Cache.AppState.NextOperationId(),
                    Level = block.Level,
                    Timestamp = block.Timestamp,
                    AccountId = delegat.Id,
                    Kind = MigrationKind.ActivateDelegate
                };
                Db.MigrationOps.Add(migration);
                Context.MigrationOps.Add(migration);
                
                state.MigrationOpsCount++;

                foreach (var weird in weirds)
                {
                    var delegator = weird.Contract;
                    if (delegator.DelegateId != null)
                        throw new Exception("migration error");

                    Db.TryAttach(delegator);
                    Cache.Accounts.Add(delegator);

                    Delegate(delegator, delegat, delegator.FirstLevel);
                }
            }
        }

        protected override async Task RevertContext(AppState state)
        {
            var block = await Cache.Blocks.CurrentAsync();

            var delegates = await Db.Delegates
                .AsNoTracking()
                .GroupJoin(Db.Accounts, x => x.Id, x => x.DelegateId, (baker, delegators) => new { baker, delegators })
                .Where(x => x.baker.ActivationLevel == block.Level)
                .ToListAsync();

            foreach (var row in delegates)
            {
                foreach (var delegator in row.delegators)
                {
                    Db.TryAttach(delegator);
                    Cache.Accounts.Add(delegator);

                    Undelegate(delegator, row.baker);
                }

                if (row.baker.ExternalDelegatedBalance != 0 || row.baker.DelegatorsCount > 0)
                    throw new Exception("migration error");

                var user = UnregisterBaker(row.baker);
                user.MigrationsCount--;
            }

            var migrationOps = await Db.MigrationOps
                .AsNoTracking()
                .Where(x => x.Kind == MigrationKind.ActivateDelegate)
                .ToListAsync();

            Db.MigrationOps.RemoveRange(migrationOps);
            Cache.AppState.ReleaseOperationId(migrationOps.Count);

            state.MigrationOpsCount -= migrationOps.Count;
        }
    }
}
