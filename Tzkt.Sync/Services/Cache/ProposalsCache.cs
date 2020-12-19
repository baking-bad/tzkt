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

        static readonly Dictionary<int, Proposal> CachedById = new Dictionary<int, Proposal>(37);
        static readonly Dictionary<string, Proposal> CachedByHash = new Dictionary<string, Proposal>(37);

        readonly TzktContext Db;

        public ProposalsCache(TzktContext db)
        {
            Db = db;
        }

        public void Reset()
        {
            CachedById.Clear();
            CachedByHash.Clear();
        }

        public void Add(Proposal proposal)
        {
            CheckSpace();
            CachedById[proposal.Id] = proposal;
            CachedByHash[proposal.Hash] = proposal;
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

        public async Task<Proposal> GetAsync(string hash)
        {
            if (!CachedByHash.TryGetValue(hash, out var proposal))
            {
                proposal = await Db.Proposals.FirstOrDefaultAsync(x => x.Hash == hash)
                    ?? throw new Exception($"Proposal {hash} doesn't exist");

                Add(proposal);
            }

            return proposal;
        }

        public async Task<Proposal> GetOrDefaultAsync(string hash)
        {
            if (!CachedByHash.TryGetValue(hash, out var proposal))
            {
                proposal = await Db.Proposals.FirstOrDefaultAsync(x => x.Hash == hash);
                if (proposal != null) Add(proposal);
            }

            return proposal;
        }

        public async Task<Proposal> GetOrCreateAsync(string hash, Func<Proposal> create)
        {
            if (!CachedByHash.TryGetValue(hash, out var proposal))
            {
                proposal = await Db.Proposals.FirstOrDefaultAsync(x => x.Hash == hash)
                    ?? create();

                // just created proposal has id = 0 so CachedById will contain wrong key, but it's fine
                Add(proposal);
            }

            return proposal;
        }

        public void Remove(Proposal proposal)
        {
            CachedById.Remove(proposal.Id);
            CachedByHash.Remove(proposal.Hash);
        }

        void CheckSpace()
        {
            if (CachedById.Count >= MaxProposals)
            {
                var oldest = CachedById.Values
                    .Take(MaxProposals / 4);

                foreach (var id in oldest.Select(x => x.Id).ToList())
                    CachedById.Remove(id);

                foreach (var hash in oldest.Select(x => x.Hash).ToList())
                    CachedByHash.Remove(hash);
            }
        }
    }
}
