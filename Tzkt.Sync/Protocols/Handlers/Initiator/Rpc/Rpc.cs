using Tzkt.Sync.Services;

namespace Tzkt.Sync.Protocols.Initiator
{
    sealed class Rpc : Genesis.Rpc
    {
        public Rpc(TezosNode node) : base(node) { }
    }
}
