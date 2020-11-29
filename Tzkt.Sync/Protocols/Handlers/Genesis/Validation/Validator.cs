using System.Text.Json;
using System.Threading.Tasks;
using Tzkt.Sync.Services;

namespace Tzkt.Sync.Protocols.Genesis
{
    class Validator : IValidator
    {
        readonly CacheService Cache;

        public Validator(ProtocolHandler protocol)
        {
            Cache = protocol.Cache;
        }

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
