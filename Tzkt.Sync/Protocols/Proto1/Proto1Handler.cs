using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;

using Tzkt.Data;
using Tzkt.Data.Models;
using Tzkt.Sync.Services;
using Tzkt.Sync.Protocols.Proto1;

namespace Tzkt.Sync.Protocols
{
    public class Proto1Handler : IProtocolHandler
    {
        public string Protocol => "Proto 1";

        protected readonly TzktContext Db;
        protected readonly CacheService Cache;
        protected readonly TezosNode Node;

        public Proto1Handler(TzktContext db, CacheService cache, TezosNode node)
        {
            Db = db;
            Cache = cache;
            Node = node;
        }

        public virtual async Task<AppState> ApplyBlock(JObject json)
        {
            #region init commits
            var commits = new List<ICommit>();

            commits.Add(await new BlockCommit(Db, Cache).Init(json));
            commits.Add(await new DeactivationCommit(Db, Cache).Init(json));
            commits.Add(await new FreezerCommit(Db, Cache).Init(json));
            commits.Add(await new VotingCommit(Db, Cache).Init(json));

            commits.Add(await new ActivationsCommit(Db, Cache).Init(json, (commits[0] as ICommit<Block>).Content));
            commits.Add(await new RevealsCommit(Db, Cache).Init(json, (commits[0] as ICommit<Block>).Content));
            commits.Add(await new DelegationsCommit(Db, Cache).Init(json, (commits[0] as ICommit<Block>).Content));
            commits.Add(await new OriginationsCommit(Db, Cache).Init(json, (commits[0] as ICommit<Block>).Content));
            commits.Add(await new TransactionsCommit(Db, Cache).Init(json, (commits[0] as ICommit<Block>).Content));

            commits.Add(await new EndorsementsCommit(Db, Cache).Init(json, (commits[0] as ICommit<Block>).Content));
            commits.Add(await new DoubleBakingsCommit(Db, Cache).Init(json, (commits[0] as ICommit<Block>).Content));
            commits.Add(await new DoubleEndorsingsCommit(Db, Cache).Init(json, (commits[0] as ICommit<Block>).Content));
            commits.Add(await new NonceRevelationsCommit(Db, Cache).Init(json, (commits[0] as ICommit<Block>).Content));

            commits.Add(await new BallotsCommit(Db, Cache).Init(json, (commits[0] as ICommit<Block>).Content));
            commits.Add(await new ProposalsCommit(Db, Cache).Init(json, (commits[0] as ICommit<Block>).Content));
            #endregion

            foreach (var commit in commits)
                await commit.Apply();

            await Db.SaveChangesAsync();
            return await Cache.State.GetAppStateAsync();
        }

        public virtual async Task<AppState> RevertLastBlock()
        {
            #region prepare entities
            var block = await Cache.State.GetCurrentBlock();
            var deactivations = await Db.Delegates.Where(x => x.DeactivationLevel == block.Level).ToListAsync();
            var freezer = new List<IBalanceUpdate>();
            var voting = await Db.VotingPeriods.Include(x => x.Epoch).Where(x => x.StartLevel == block.Level).FirstOrDefaultAsync();

            var activations = await Db.ActivationOps.Where(x => x.Level == block.Level).ToListAsync();
            var delegations = await Db.DelegationOps.Where(x => x.Level == block.Level).ToListAsync();
            var originations = await Db.OriginationOps.Where(x => x.Level == block.Level).ToListAsync();
            var transactions = await Db.TransactionOps.Where(x => x.Level == block.Level).ToListAsync();
            var reveals = await Db.RevealOps.Where(x => x.Level == block.Level).ToListAsync();

            var endorsements = await Db.EndorsementOps.Where(x => x.Level == block.Level).ToListAsync();
            var doubleBakings = await Db.DoubleBakingOps.Where(x => x.Level == block.Level).ToListAsync();
            var doubleEndorsings = await Db.DoubleEndorsingOps.Where(x => x.Level == block.Level).ToListAsync();
            var nonceRevelations = await Db.NonceRevelationOps.Where(x => x.Level == block.Level).ToListAsync();

            var ballots = await Db.BallotOps.Where(x => x.Level == block.Level).ToListAsync();
            var proposals = await Db.ProposalOps.Where(x => x.Level == block.Level).ToListAsync();
            #endregion

            #region init commits
            var commits = new List<ICommit>();

            commits.Add(await new ProposalsCommit(Db, Cache).Init(proposals));
            commits.Add(await new BallotsCommit(Db, Cache).Init(ballots));

            commits.Add(await new NonceRevelationsCommit(Db, Cache).Init(nonceRevelations));
            commits.Add(await new DoubleEndorsingsCommit(Db, Cache).Init(doubleEndorsings));
            commits.Add(await new DoubleBakingsCommit(Db, Cache).Init(doubleBakings));
            commits.Add(await new EndorsementsCommit(Db, Cache).Init(endorsements));

            commits.Add(await new TransactionsCommit(Db, Cache).Init(transactions));
            commits.Add(await new OriginationsCommit(Db, Cache).Init(originations));
            commits.Add(await new DelegationsCommit(Db, Cache).Init(delegations));
            commits.Add(await new RevealsCommit(Db, Cache).Init(reveals));
            commits.Add(await new ActivationsCommit(Db, Cache).Init(activations));

            commits.Add(await new VotingCommit(Db, Cache).Init(voting));
            commits.Add(await new FreezerCommit(Db, Cache).Init(freezer));
            commits.Add(await new DeactivationCommit(Db, Cache).Init(deactivations));
            commits.Add(await new BlockCommit(Db, Cache).Init(block));
            #endregion

            foreach (var commit in commits)
                await commit.Revert();

            await Db.SaveChangesAsync();
            return await Cache.State.GetAppStateAsync();
        }
    }
}
