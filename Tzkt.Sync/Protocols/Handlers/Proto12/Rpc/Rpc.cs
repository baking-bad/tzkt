using Tzkt.Sync.Services;

namespace Tzkt.Sync.Protocols.Proto12
{
    class Rpc : Proto6.Rpc
    {
        public Rpc(TezosNode node) : base(node) { }
    }
}
