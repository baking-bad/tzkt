using Tzkt.Data;

namespace Tzkt.Sync.Protocols.Proto10
{
    class Diagnostics : Proto5.Diagnostics
    {
        public Diagnostics(TzktContext db, IRpc rpc) : base(db, rpc) { }
    }
}
