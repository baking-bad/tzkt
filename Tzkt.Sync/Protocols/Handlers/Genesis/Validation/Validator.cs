using System.Text.Json;
using Tzkt.Sync.Services;

namespace Tzkt.Sync.Protocols.Genesis
{
    class Validator(ProtocolHandler protocol) : IValidator
    {
        readonly CacheService Cache = protocol.Cache;

        public Task ValidateBlock(JsonElement block)
        {
            if (block.Required("header").RequiredInt32("level") != Cache.AppState.GetNextLevel())
                throw new ValidationException("invalid block level", true);

            if (block.Required("header").RequiredInt32("level") != 0)
                throw new ValidationException("genesis block is allowed only at level 0", true);

            return Task.CompletedTask;
        }
    }
}
