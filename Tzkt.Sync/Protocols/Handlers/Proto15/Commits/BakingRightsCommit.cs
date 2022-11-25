using System.Collections.Generic;
using System.Linq;
using Netezos.Encoding;

namespace Tzkt.Sync.Protocols.Proto15
{
    class BakingRightsCommit : Proto14.BakingRightsCommit
    {
        public BakingRightsCommit(ProtocolHandler protocol) : base(protocol) { }

        protected override Sampler GetSampler(IEnumerable<(int id, long stake)> selection, bool forceBase)
        {
            if (forceBase)
            {
                var sorted = selection
                    .OrderByDescending(x => x.stake)
                    .ThenByDescending(x =>
                    {
                        var baker = Cache.Accounts.GetDelegate(x.id);
                        return new byte[] { (byte)baker.PublicKey[0] }.Concat(Base58.Parse(baker.Address));
                    }, new BytesComparer());

                return new Sampler(sorted.Select(x => x.id).ToArray(), sorted.Select(x => x.stake).ToArray());
            }

            return base.GetSampler(selection, forceBase);
        }
    }
}
