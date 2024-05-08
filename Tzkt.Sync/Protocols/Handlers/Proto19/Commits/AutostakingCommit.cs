using System.Text.Json;

namespace Tzkt.Sync.Protocols.Proto19
{
    class AutostakingCommit : Proto18.AutostakingCommit
    {
        public AutostakingCommit(ProtocolHandler protocol) : base(protocol) { }

        protected override string GetFreezerBaker(JsonElement update)
        {
            return update.Required("staker").RequiredString("baker_own_stake");
        }
    }
}
