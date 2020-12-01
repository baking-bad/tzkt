using Tzkt.Data;

namespace Tzkt.Sync.Protocols.Proto3
{
    class Diagnostics : Proto1.Diagnostics
    {
        public Diagnostics(TzktContext db, IRpc rpc) : base(db, rpc) { }
    }
}
