using Mvkt.Sync.Services;

namespace Mvkt.Sync.Protocols.Initiator
{
    sealed class Rpc : Proto1.Rpc
    {
        public Rpc(MavrykNode node) : base(node) { }
    }
}
