using Tzkt.Sync.Services;

namespace Tzkt.Sync.Protocols.Proto16
{
    class Rpc : Proto12.Rpc
    {
        public Rpc(TezosNode node) : base(node) { }
    }
}
