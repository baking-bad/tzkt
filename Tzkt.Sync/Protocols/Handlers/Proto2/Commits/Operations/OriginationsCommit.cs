using System.Text.Json;
using System.Threading.Tasks;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto2
{
    class OriginationsCommit : Proto1.OriginationsCommit
    {
        public OriginationsCommit(ProtocolHandler protocol) : base(protocol) { }

        protected override Task<User> GetWeirdDelegate(JsonElement content) => Task.FromResult<User>(null);
    }
}
