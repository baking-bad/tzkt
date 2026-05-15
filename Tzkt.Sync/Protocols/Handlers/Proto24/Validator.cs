using System.Text.Json;
using Tzkt.Sync.Services;

namespace Tzkt.Sync.Protocols.Proto24
{
    class Validator(ProtocolHandler protocol) : IValidator
    {
        readonly CacheService Cache = protocol.Cache;

        public virtual async Task ValidateBlock(JsonElement block)
        {
            if (block.RequiredString("chain_id") != Cache.AppState.GetChainId())
                throw new ValidationException("invalid chain");

            if (block.RequiredString("protocol") != Cache.AppState.GetNextProtocol())
                throw new ValidationException("invalid block protocol", true);

            var header = block.Required("header");

            if (header.RequiredInt32("level") != Cache.AppState.GetNextLevel())
                throw new ValidationException($"invalid block level", true);

            if (header.RequiredString("predecessor") != Cache.AppState.GetHead() && Cache.AppState.GetHead() != string.Empty)
                throw new ValidationException($"invalid block predecessor", true);
        }
    }
}
