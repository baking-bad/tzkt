using System.Collections.Generic;
using System.Linq;
using Netezos.Encoding;

namespace Tzkt.Sync.Protocols.Proto13
{
    class BakingRightsCommit : Proto12.BakingRightsCommit
    {
        public BakingRightsCommit(ProtocolHandler protocol) : base(protocol) { }

        protected override Sampler GetSampler(IEnumerable<(int id, long stake)> selection, bool forceBase)
        {
            if (forceBase)
                return base.GetSampler(selection, false);

            var sorted = selection.OrderByDescending(x =>
            {
                var baker = Cache.Accounts.GetDelegate(x.id);
                return new byte[] { (byte)baker.PublicKey[0] }.Concat(Base58.Parse(baker.Address));
            }, new BytesComparer());

            return new Sampler(sorted.Select(x => x.id).ToArray(), sorted.Select(x => x.stake).ToArray());
        }
    }
}
