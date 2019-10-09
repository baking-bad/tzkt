using System;
using System.Threading.Tasks;
using Tzkt.Sync.Services;

namespace Tzkt.Sync.Protocols.Genesis
{
    class Validator : IValidator
    {
        readonly StateManager State;

        public Validator(ProtocolHandler protocol)
        {
            State = protocol.State;
        }

        public async Task<IBlock> ValidateBlock(IBlock block)
        {
            if (!(block is RawBlock rawBlock))
                throw new ArgumentException("invalid type of the block to validate");

            if (rawBlock.Level != (await State.GetAppStateAsync()).Level + 1)
                throw new ValidationException("invalid block level", true);

            if (rawBlock.Level != 0)
                throw new ValidationException("genesis block is allowed only at level 0");

            return rawBlock;
        }
    }
}
