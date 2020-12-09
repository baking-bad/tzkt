using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto5
{
    class Validator : Proto4.Validator
    {
        protected JsonElement Block;

        public Validator(ProtocolHandler protocol) : base(protocol) { }

        // global block
        public override Task ValidateBlock(JsonElement block)
        {
            Block = block;
            return base.ValidateBlock(block);
        }

        // new formula
        protected override long GetBlockReward()
        {
            var priority = Block
                .GetProperty("header")
                .RequiredInt32("priority");

            var endorsements = Block
                .GetProperty("operations")[0]
                .EnumerateArray()
                .Select(x => x.GetProperty("contents")[0].GetProperty("metadata").GetProperty("slots").Count())
                .Sum();

            return Cycle < Protocol.NoRewardCycles ? 0 : (Protocol.BlockReward0 * (8 + 2 * endorsements / Protocol.EndorsersPerBlock) / 10 / (priority + 1));
        }

        // new formula
        protected override long GetEndorsementReward(int slots)
        {
            var priority = Block
                .GetProperty("header")
                .RequiredInt32("priority");

            return Cycle < Protocol.NoRewardCycles ? 0 : (slots * (long)(Protocol.EndorsementReward0 / (priority + 1.0)));
        }
    }
}
