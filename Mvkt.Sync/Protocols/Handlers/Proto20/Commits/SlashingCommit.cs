using System.Text.Json;

namespace Mvkt.Sync.Protocols.Proto20
{
    class SlashingCommit : Proto19.SlashingCommit
    {
        public SlashingCommit(ProtocolHandler protocol) : base(protocol) { }

        protected override string GetFreezerBaker(JsonElement update)
        {
            return update.Required("staker").OptionalString("baker_own_stake")
                ?? update.Required("staker").RequiredString("delegate");
        }

        protected override bool IsOwnStake(JsonElement update)
        {
            return update.Required("staker").TryGetProperty("baker_own_stake", out _);
        }
    }
}
