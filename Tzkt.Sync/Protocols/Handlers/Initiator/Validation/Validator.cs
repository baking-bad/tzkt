using System;
using System.Threading.Tasks;
using Tzkt.Sync.Services;

namespace Tzkt.Sync.Protocols.Initiator
{
    class Validator : IValidator
    {
        readonly StateCache State;

        public Validator(ProtocolHandler protocol)
        {
            State = protocol.State;
        }

        public async Task<IBlock> ValidateBlock(IBlock block)
        {
            if (!(block is RawBlock rawBlock))
                throw new ArgumentException("invalid type of the block to validate");

            if (rawBlock.Level != (await State.GetCurrentBlock()).Level + 1)
                throw new ValidationException($"Invalid block level", true);

            if (rawBlock.Predecessor != (await State.GetCurrentBlock()).Hash)
                throw new ValidationException($"Invalid block predecessor", true);

            if (rawBlock.Protocol != (await State.GetAppStateAsync()).NextProtocol)
                throw new ValidationException($"Invalid block protocol", true);

            if (rawBlock.Level != 1)
                throw new ValidationException("initiator block is allowed only at level 1");

            return rawBlock;
        }
    }
}
