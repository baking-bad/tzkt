using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Tzkt.Data;
using Tzkt.Data.Models;
using Tzkt.Sync.Services;
using Tzkt.Sync.Protocols.Proto1;

namespace Tzkt.Sync.Protocols
{
    class Proto1Handler : ProtocolHandler
    {
        public override string Protocol => "Proto 1";
        public override ISerializer Serializer { get; }
        public override IValidator Validator { get; }

        public Proto1Handler(TezosNode node, TzktContext db, CacheService cache, ILogger<Proto1Handler> logger)
            : base(node, db, cache, logger)
        {
            Serializer = new Serializer();
            Validator = new Validator(this);
        }

        public override async Task<List<ICommit>> GetCommits(IBlock block)
        {
            var rawBlock = block as RawBlock;
            var commits = new List<ICommit>();

            commits.Add(await BlockCommit.Create(this, commits, rawBlock));
            commits.Add(await DeactivationCommit.Create(this, commits, rawBlock));
            commits.Add(await FreezerCommit.Create(this, commits, rawBlock));
            commits.Add(await VotingCommit.Create(this, commits, rawBlock));

            commits.Add(await ActivationsCommit.Create(this, commits, rawBlock));
            commits.Add(await RevealsCommit.Create(this, commits, rawBlock));
            commits.Add(await DelegationsCommit.Create(this, commits, rawBlock));
            commits.Add(await OriginationsCommit.Create(this, commits, rawBlock));
            commits.Add(await TransactionsCommit.Create(this, commits, rawBlock));

            commits.Add(await EndorsementsCommit.Create(this, commits, rawBlock));
            commits.Add(await NonceRevelationsCommit.Create(this, commits, rawBlock));

            commits.Add(await StateCommit.Create(this, commits, rawBlock));

            return commits;
        }

        public override async Task<List<ICommit>> GetCommits(Block block)
        {
            #region prepare entities
            var deactivations = await Db.Delegates.Where(x => x.DeactivationLevel == block.Level).ToListAsync();
            var freezer = new List<IBalanceUpdate>();
            var voting = await Db.VotingPeriods.Include(x => x.Epoch).Where(x => x.StartLevel == block.Level).FirstOrDefaultAsync();

            var activations = await Db.ActivationOps.Where(x => x.Level == block.Level).ToListAsync();
            var delegations = await Db.DelegationOps.Include(x => x.Parent).Where(x => x.Level == block.Level).ToListAsync();
            var originations = await Db.OriginationOps.Include(x => x.Parent).Where(x => x.Level == block.Level).ToListAsync();
            var transactions = await Db.TransactionOps.Include(x => x.Parent).Where(x => x.Level == block.Level).ToListAsync();
            var reveals = await Db.RevealOps.Where(x => x.Level == block.Level).ToListAsync();

            var endorsements = await Db.EndorsementOps.Where(x => x.Level == block.Level).ToListAsync();
            var nonceRevelations = await Db.NonceRevelationOps.Where(x => x.Level == block.Level).ToListAsync();
            #endregion

            var commits = new List<ICommit>();

            commits.Add(await NonceRevelationsCommit.Create(this, commits, nonceRevelations));
            commits.Add(await EndorsementsCommit.Create(this, commits, endorsements));

            commits.Add(await TransactionsCommit.Create(this, commits, transactions));
            commits.Add(await OriginationsCommit.Create(this, commits, originations));
            commits.Add(await DelegationsCommit.Create(this, commits, delegations));
            commits.Add(await RevealsCommit.Create(this, commits, reveals));
            commits.Add(await ActivationsCommit.Create(this, commits, activations));

            commits.Add(await VotingCommit.Create(this, commits, voting));
            commits.Add(await FreezerCommit.Create(this, commits, freezer));
            commits.Add(await DeactivationCommit.Create(this, commits, deactivations));
            commits.Add(await BlockCommit.Create(this, commits, block));

            commits.Add(await StateCommit.Create(this, commits));

            return commits;
        }
    }
}
