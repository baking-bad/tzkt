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

        public async Task<IBlock> ValidateBlock(IBlock block)
        {
            if (!(block is RawBlock rawBlock))
                throw new ArgumentException("invalid type of the block to validate");

            if (rawBlock.Level != (await Cache.GetCurrentBlockAsync()).Level + 1)
                throw new ValidationException($"Invalid block level", true);

            if (rawBlock.Predecessor != (await Cache.GetCurrentBlockAsync()).Hash)
                throw new ValidationException($"Invalid block predecessor", true);

            if (rawBlock.Protocol != (await Cache.GetAppStateAsync()).NextProtocol)
                throw new ValidationException($"Invalid block protocol", true);

            if (rawBlock.Level != 1)
                throw new ValidationException("initiator block is allowed only at level 1");

            return rawBlock;
        }
    }
}
