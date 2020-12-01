using Tzkt.Sync.Services;

namespace Tzkt.Sync.Protocols.Proto4
{
    class Rpc : Proto1.Rpc
    {
        public Rpc(TezosNode node) : base(node) { }
    }
}
