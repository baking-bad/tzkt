using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto12
{
    class Diagnostics : Proto5.Diagnostics
    {
        public Diagnostics(ProtocolHandler handler) : base(handler) { }

        protected override async Task TestDelegate(int level, Data.Models.Delegate delegat, Protocol proto)
        {
            var remote = await Rpc.GetDelegateAsync(level, delegat.Address);

            if (remote.RequiredInt64("full_balance") != delegat.Balance)
                throw new Exception($"Diagnostics failed: wrong balance {delegat.Address}");

            if (remote.RequiredInt64("current_frozen_deposits") != delegat.FrozenDeposit)
                throw new Exception($"Diagnostics failed: wrong frozen deposits {delegat.Address}");

            if (remote.RequiredInt64("staking_balance") != delegat.StakingBalance)
                throw new Exception($"Diagnostics failed: wrong staking balance {delegat.Address}");

            if (remote.RequiredInt64("delegated_balance") != delegat.DelegatedBalance)
                throw new Exception($"Diagnostics failed: wrong delegated balance {delegat.Address}");

            if (remote.RequiredBool("deactivated") != !delegat.Staked)
                throw new Exception($"Diagnostics failed: wrong deactivation state {delegat.Address}");

            var deactivationCycle = (delegat.DeactivationLevel - 1) >= proto.FirstLevel
                ? proto.GetCycle(delegat.DeactivationLevel - 1)
                : (await Cache.Blocks.GetAsync(delegat.DeactivationLevel - 1)).Cycle;

            if (remote.RequiredInt32("grace_period") != deactivationCycle)
                throw new Exception($"Diagnostics failed: wrong grace period {delegat.Address}");

            TestDelegatorsCount(remote, delegat);
        }

        protected override async Task TestRights(AppState state, int cycle)
        {
                if (state.Chain == "mainnet" && cycle < 13)
                    return;

                var itha = await Db.Protocols.Where(x => x.Hash == "Psithaca2MLRFYargivpo7YvUr7wUDqyxrdhC5CQq78mRvimz6A").FirstOrDefaultAsync();
                var migration = (itha?.FirstCycle ?? 0) - 1;
                
                var protocols = await Db.Protocols.OrderByDescending(x => x.Code).ToListAsync();
                var proto = protocols.First(x => x.FirstCycle <= cycle);

                var fBr = await Db.BakingRights.CountAsync(x =>
                    x.Cycle == cycle &&
                    x.Type == BakingRightType.Baking &&
                    x.Status == BakingRightStatus.Future);

                if (fBr > 0)
                    throw new Exception($"There are {fBr} future baking rights for cycle {cycle}");

                var sBr = await Db.BakingRights.CountAsync(x =>
                    x.Cycle == cycle &&
                    x.Type == BakingRightType.Baking &&
                    x.Status == BakingRightStatus.Realized);

                var reproposedBlocks = await Db.Blocks.CountAsync(x =>
                    x.Cycle == cycle &&
                    x.ProposerId != x.ProducerId);

                if (sBr != proto.BlocksPerCycle + reproposedBlocks && cycle > 0 ||
                    cycle == 0 && sBr != proto.BlocksPerCycle + reproposedBlocks - 1)
                    throw new Exception($"Wrong successfull baking rights {sBr} for cycle {cycle}, should be {proto.BlocksPerCycle + reproposedBlocks} (or -1)");

                var allBr = await Db.BakingRights.CountAsync(x =>
                    x.Cycle == cycle &&
                    x.Type == BakingRightType.Baking);

                var allPr = await Db.Blocks
                    .Where(x => x.Cycle == cycle)
                    .SumAsync(x => x.BlockRound + 1);

                if (allBr != allPr && cycle > 0 || cycle == 0 && allBr != allPr - 1)
                    throw new Exception($"Wrong total number of baking rights {allBr} for cycle {cycle}, should be {allPr} (or -1)");

                var fEr = await Db.BakingRights.CountAsync(x =>
                    x.Cycle == cycle &&
                    x.Type == BakingRightType.Endorsing &&
                    x.Status == BakingRightStatus.Future);

                if (fEr > 0)
                    throw new Exception($"There are {fEr} future endorsing rights for cycle {cycle}");

                var sEr = await Db.BakingRights.Where(x =>
                    x.Cycle == cycle &&
                    x.Type == BakingRightType.Endorsing &&
                    x.Status == BakingRightStatus.Realized).SumAsync(x => x.Slots);

                var allVal = await Db.Blocks
                    .Where(x => x.Cycle == cycle)
                    .SumAsync(x => x.Validations);

                if (sEr != allVal)
                    throw new Exception($"Wrong successfull slots {sEr} for cycle {cycle}, should be {allVal}");

                if (cycle != migration && !(state.Chain == "mainnet" && cycle == 387))
                {
                    var mEr = await Db.BakingRights.Where(x =>
                        x.Cycle == cycle &&
                        x.Type == BakingRightType.Endorsing &&
                        x.Status == BakingRightStatus.Missed)
                        .SumAsync(x => x.Slots);

                    var totalSlots = (proto.BlocksPerCycle - (cycle == 0 ? 1 : 0)) * proto.EndorsersPerBlock;
                    if (mEr != totalSlots - sEr)
                        throw new Exception($"Wrong missed slots {mEr} for cycle {cycle}, should be {totalSlots - sEr}");
                }
        }
    }
}
