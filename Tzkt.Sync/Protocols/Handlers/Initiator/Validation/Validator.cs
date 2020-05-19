using System;
using System.Threading.Tasks;
using Tzkt.Sync.Services;

namespace Tzkt.Sync.Protocols.Initiator
{
    class Validator : IValidator
    {
        readonly CacheService Cache;

        public Validator(ProtocolHandler protocol)
        {
            Cache = protocol.Cache;
        }

        public Task<IBlock> ValidateBlock(IBlock block)
        {
            if (!(block is RawBlock rawBlock))
                throw new ArgumentException("invalid type of the block to validate");

            if (rawBlock.Level != Cache.AppState.GetNextLevel())
                throw new ValidationException($"Invalid block level", true);

            if (rawBlock.Predecessor != Cache.AppState.GetHead())
                throw new ValidationException($"Invalid block predecessor", true);

            if (rawBlock.Protocol != Cache.AppState.GetNextProtocol())
                throw new ValidationException($"Invalid block protocol", true);

            if (rawBlock.Level != 1)
                throw new ValidationException("initiator block is allowed only at level 1");

            return Task.FromResult(block);
        }
    }
}
