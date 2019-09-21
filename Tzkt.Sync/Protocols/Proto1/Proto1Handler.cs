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
            var commits = new List<ICommit>();

            commits.Add(await new BlockCommit(Db, Cache).Init(json));
            commits.Add(await new EndorsementsCommit(Db, Cache).Init(json, (commits[0] as ICommit<Block>).Content));

            foreach (var commit in commits)
                await commit.Apply();

            await Db.SaveChangesAsync();
            return await Cache.State.GetAppStateAsync();
        }

        public virtual async Task<AppState> RevertLastBlock()
        {
            var block = await Cache.State.GetCurrentBlock();
            var endorsements = await Db.EndorsementOps.Where(x => x.Level == block.Level).ToListAsync();

            var commits = new List<ICommit>();

            commits.Add(await new BlockCommit(Db, Cache).Init(block));
            commits.Add(await new EndorsementsCommit(Db, Cache).Init(endorsements));

            foreach (var commit in commits)
                await commit.Revert();

            await Db.SaveChangesAsync();
            return await Cache.State.GetAppStateAsync();
        }
    }
}
