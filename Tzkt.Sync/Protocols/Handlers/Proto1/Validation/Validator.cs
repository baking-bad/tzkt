using System.Text.Json;
using System.Threading.Tasks;
using Tzkt.Sync.Services;

namespace Tzkt.Sync.Protocols.Proto1
{
    class Validator : IValidator
    {
        protected readonly CacheService Cache;
        public Validator(ProtocolHandler protocol) => Cache = protocol.Cache;

        public Task ValidateBlock(JsonElement block)
        {
            if (block.RequiredString("chain_id") != Cache.AppState.GetChainId())
                throw new ValidationException("invalid chain");

            if (block.Required("header").RequiredInt32("level") != Cache.AppState.GetNextLevel())
                throw new ValidationException("invalid block level", true);

            if (block.Required("header").RequiredString("predecessor") != Cache.AppState.GetHead())
                throw new ValidationException("invalid block predecessor", true);

            if (block.RequiredString("protocol") != Cache.AppState.GetNextProtocol())
                throw new ValidationException("invalid block protocol", true);

            return Task.CompletedTask;
        }
    }
}
