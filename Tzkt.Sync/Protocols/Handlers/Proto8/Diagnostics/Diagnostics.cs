using Tzkt.Data;

namespace Tzkt.Sync.Protocols.Proto8
{
    class Diagnostics : Proto5.Diagnostics
    {
        public Diagnostics(TzktContext db, IRpc rpc) : base(db, rpc) { }
    }
}
