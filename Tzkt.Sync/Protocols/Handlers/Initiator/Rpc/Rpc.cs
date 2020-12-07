using Tzkt.Sync.Services;

namespace Tzkt.Sync.Protocols.Initiator
{
    sealed class Rpc : Proto1.Rpc
    {
        public Rpc(TezosNode node) : base(node) { }
    }
}
