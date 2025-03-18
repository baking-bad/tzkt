using Microsoft.EntityFrameworkCore;
using Tzkt.Data;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Services.Cache
{
    public class ProposalsCache(TzktContext db)
    {
        const int MaxCount = 32; //TODO: set limits in app settings
        const int TargetCount = MaxCount * 3 / 4;

        static readonly Dictionary<int, Proposal> CachedById = new(37);
        static readonly Dictionary<(int, string), Proposal> CachedByHash = new(37);

        readonly TzktContext Db = db;

        public void Reset()
        {
            CachedById.Clear();
            CachedByHash.Clear();
        }

        public void Trim()
        {
            if (CachedById.Count > MaxCount)
            {
                var toRemove = CachedById.Values
                    .OrderBy(x => x.LastPeriod)
                    .Take(CachedById.Count - TargetCount)
                    .ToList();

                foreach (var item in toRemove)
                    Remove(item);
            }
        }

        public void Add(Proposal proposal)
        {
            CachedById[proposal.Id] = proposal;
            CachedByHash[(proposal.Epoch, proposal.Hash)] = proposal;
        }

        public void Remove(Proposal proposal)
        {
            CachedById.Remove(proposal.Id);
            CachedByHash.Remove((proposal.Epoch, proposal.Hash));
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
            if (!CachedByHash.TryGetValue((epoch, hash), out var proposal))
            {
                proposal = await Db.Proposals.FirstOrDefaultAsync(x => x.Hash == hash && x.Epoch == epoch)
                    ?? throw new Exception($"Proposal {hash} from epoch {epoch} doesn't exist");

                Add(proposal);
            }

            return proposal;
        }

        public async Task<Proposal?> GetOrDefaultAsync(int epoch, string hash)
        {
            if (!CachedByHash.TryGetValue((epoch, hash), out var proposal))
            {
                proposal = await Db.Proposals.FirstOrDefaultAsync(x => x.Hash == hash && x.Epoch == epoch);
                if (proposal != null) Add(proposal);
            }

            return proposal;
        }

        public async Task<Proposal> GetOrCreateAsync(int epoch, string hash, Func<Proposal> create)
        {
            if (!CachedByHash.TryGetValue((epoch, hash), out var proposal))
            {
                proposal = await Db.Proposals.FirstOrDefaultAsync(x => x.Hash == hash && x.Epoch == epoch)
                    ?? create();

                Add(proposal);
            }

            return proposal;
        }
    }
}
