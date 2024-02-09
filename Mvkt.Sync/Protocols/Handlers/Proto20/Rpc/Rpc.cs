using Mvkt.Sync.Services;

namespace Mvkt.Sync.Protocols.Proto20
{
    class Rpc : Proto19.Rpc
    {
        public Rpc(TezosNode node) : base(node) { }
    }
}
