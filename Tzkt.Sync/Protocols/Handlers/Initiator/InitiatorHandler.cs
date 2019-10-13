using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

using Tzkt.Data;
using Tzkt.Sync.Services;
using Tzkt.Sync.Protocols.Initiator;

namespace Tzkt.Sync.Protocols
{
    class InitiatorHandler : ProtocolHandler
    {
        public override string Protocol => "Initiator";
        public override ISerializer Serializer { get; }
        public override IValidator Validator { get; }

        public InitiatorHandler(TezosNode node, TzktContext db, CacheService cache, ILogger<InitiatorHandler> logger)
            : base(node, db, cache, logger)
        {
            Serializer = new Serializer();
            Validator = new Validator(this);
        }

        public override async Task<List<ICommit>> GetCommits(IBlock block)
        {
            var rawBlock = block as RawBlock;

            var commits = new List<ICommit>();
            commits.Add(await ProtoCommit.Create(this, commits, rawBlock));
            commits.Add(await BootstrapCommit.Create(this, commits, rawBlock));
            commits.Add(await BlockCommit.Create(this, commits, rawBlock));
            commits.Add(await VotingCommit.Create(this, commits, rawBlock));
            commits.Add(await StateCommit.Create(this, commits, rawBlock));

            return commits;
        }

        public override async Task<List<ICommit>> GetReverts()
        {
            var commits = new List<ICommit>();
            commits.Add(await VotingCommit.Create(this, commits));
            commits.Add(await BlockCommit.Create(this, commits));
            commits.Add(await BootstrapCommit.Create(this, commits));
            commits.Add(await ProtoCommit.Create(this, commits));
            commits.Add(await StateCommit.Create(this, commits));

            return commits;
        }

        /*private async Task InitCycle(int cycle)
        {
            #region init rights
            var rights = await Task.WhenAll(
                Node.GetBakingRightsAsync(1, cycle, 1),
                Node.GetEndorsingRightsAsync(1, cycle));

            var bakingRights = rights[0]
                .Select(x => new BakingRight
                {
                    Baker = GetContract(x["delegate"].String()),
                    Level = x["level"].Int32(),
                    Priority = x["priority"].Int32()
                });

            var endorsingRights = rights[1]
                .Select(x => new EndorsingRight
                {
                    Baker = GetContract(x["delegate"].String()),
                    Level = x["level"].Int32(),
                    Slots = x["slots"].Count()
                });

            Db.BakingRights.AddRange(bakingRights);
            Db.EndorsingRights.AddRange(endorsingRights);
            #endregion

            #region init cycle
            var cycleObj = new Cycle
            {
                Index = cycle,
                Snapshot = 1,
            };
            Db.Cycles.Add(cycleObj);
            #endregion

            #region init snapshots
            var snapshots = Contracts.Values
                .Where(x => x.Staked)
                .Select(x => new BalanceSnapshot
                {
                    Balance = x.Balance,
                    Address = x,
                    Delegate = GetContract(x.Delegate?.Address ?? x.Address),
                    Level = cycleObj.Snapshot
                });
            #endregion

            #region init delegators
            var delegators = snapshots
                .Where(x => x.Contract.Kind != ContractKind.Baker)
                .Select(x => new DelegatorSnapshot
                {
                    Baker = x.Delegate,
                    Balance = x.Balance,
                    Delegator = x.Contract,
                    Cycle = cycle
                });
            Db.DelegatorSnapshots.AddRange(delegators);
            #endregion

            #region init bakers
            var bakers = snapshots
                .Where(x => x.Contract.Kind == ContractKind.Baker)
                .Select(x => new BakingCycle
                {
                    Baker = x.Contract,
                    Balance = x.Balance,
                    Cycle = cycle,
                    StakingBalance = snapshots
                        .Where(s => s.Delegate == x.Contract)
                        .Sum(s => s.Balance),
                    Blocks = bakingRights
                        .Count(r => r.Priority == 0 && r.Baker == x.Contract),
                    Endorsements = endorsingRights
                        .Where(r => r.Baker == x.Contract)
                        .DefaultIfEmpty(new EndorsingRight())
                        .Sum(r => r.Slots)
                });
            Db.BakerCycles.AddRange(bakers);
            #endregion
        }*/
        /*private async Task ClearCycle(int cycle)
        {
            Db.BakingRights.RemoveRange(
                await Db.BakingRights.Where(x => (x.Level - 1) / 4096 == cycle).ToListAsync());

            Db.EndorsingRights.RemoveRange(
                await Db.EndorsingRights.Where(x => (x.Level - 1) / 4096 == cycle).ToListAsync());

            Db.Cycles.Remove(
                await Db.Cycles.FirstAsync(x => x.Index == cycle));

            Db.DelegatorSnapshots.RemoveRange(
                await Db.DelegatorSnapshots.Where(x => x.Cycle == cycle).ToListAsync());

            Db.BakerCycles.RemoveRange(
                await Db.BakerCycles.Where(x => x.Cycle == cycle).ToListAsync());
        }*/
    }
}
