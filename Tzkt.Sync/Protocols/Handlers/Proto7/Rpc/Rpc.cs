using Tzkt.Sync.Services;

namespace Tzkt.Sync.Protocols.Proto7
{
    class Rpc : Proto6.Rpc
    {
        public Rpc(TezosNode node) : base(node) { }
    }
}
