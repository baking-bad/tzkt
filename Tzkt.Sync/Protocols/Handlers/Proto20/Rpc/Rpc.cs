using Tzkt.Sync.Services;

namespace Tzkt.Sync.Protocols.Proto20
{
    class Rpc : Proto19.Rpc
    {
        public Rpc(TezosNode node) : base(node) { }
    }
}
