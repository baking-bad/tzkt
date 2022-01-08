using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

using Tzkt.Data;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Services.Cache
{
    public class ProposalsCache
    {
        public const int MaxProposals = 32; //TODO: set limits in app settings

        static readonly Dictionary<int, Proposal> CachedById = new(37);
        static readonly Dictionary<(int, string), Proposal> CachedByKey = new(37);

        readonly TzktContext Db;

        public ProposalsCache(TzktContext db)
        {
            Db = db;
        }

        public void Reset()
        {
            CachedById.Clear();
            CachedByKey.Clear();
        }

        public void Add(Proposal proposal)
        {
            CheckSpace();
            CachedById[proposal.Id] = proposal;
            CachedByKey[(proposal.Epoch, proposal.Hash)] = proposal;
        }

        public async Task<Proposal> GetAsync(int id)
        {
            if (!CachedById.TryGetValue(id, out var proposal))
            {
                proposal = await Db.Proposals.FirstOrDefaultAsync(x => x.Id == id)
                    ?? throw new Exception($"Proposal #{id} doesn't exist");

                Add(proposal);
            }

            return proposal;
        }

        public async Task<Proposal> GetAsync(int epoch, string hash)
        {
            if (!CachedByKey.TryGetValue((epoch, hash), out var proposal))
            {
                proposal = await Db.Proposals.FirstOrDefaultAsync(x => x.Hash == hash && x.Epoch == epoch)
                    ?? throw new Exception($"Proposal {hash} from epoch {epoch} doesn't exist");

                Add(proposal);
            }

            return proposal;
        }

        public async Task<Proposal> GetOrDefaultAsync(int epoch, string hash)
        {
            if (!CachedByKey.TryGetValue((epoch, hash), out var proposal))
            {
                proposal = await Db.Proposals.FirstOrDefaultAsync(x => x.Hash == hash && x.Epoch == epoch);
                if (proposal != null) Add(proposal);
            }

            return proposal;
        }

        public async Task<Proposal> GetOrCreateAsync(int epoch, string hash, Func<Proposal> create)
        {
            if (!CachedByKey.TryGetValue((epoch, hash), out var proposal))
            {
                proposal = await Db.Proposals.FirstOrDefaultAsync(x => x.Hash == hash && x.Epoch == epoch)
                    ?? create();

                // just created proposal has id = 0 so CachedById will contain wrong key, but it's fine
                Add(proposal);
            }

            return proposal;
        }

        public void Remove(Proposal proposal)
        {
            CachedById.Remove(proposal.Id);
            CachedByKey.Remove((proposal.Epoch, proposal.Hash));
        }

        void CheckSpace()
        {
            if (CachedById.Count >= MaxProposals)
            {
                var oldest = CachedById.Values
                    .Take(MaxProposals / 4)
                    .ToList();

                foreach (var item in oldest)
                {
                    CachedById.Remove(item.Id);
                    CachedByKey.Remove((item.Epoch, item.Hash));
                }
            }
        }
    }
}
