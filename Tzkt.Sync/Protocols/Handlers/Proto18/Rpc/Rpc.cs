using Tzkt.Sync.Services;

namespace Tzkt.Sync.Protocols.Proto18
{
    class Rpc : Proto16.Rpc
    {
        public Rpc(TezosNode node) : base(node) { }
    }
}
