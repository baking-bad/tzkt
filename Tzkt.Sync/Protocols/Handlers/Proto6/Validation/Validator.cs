using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto6
{
    class Validator : Proto4.Validator
    {
        protected JsonElement Block;

        public Validator(ProtocolHandler protocol) : base(protocol) { }

        public override Task ValidateBlock(JsonElement block)
        {
            Block = block;
            return base.ValidateBlock(block);
        }

        // no block rewards if no endorsements
        protected override async Task ValidateBlockMetadata(JsonElement metadata)
        {
            Baker = metadata.RequiredString("baker");

            if (!Cache.Accounts.DelegateExists(Baker))
                throw new ValidationException($"non-existent block baker");

            await ValidateBlockVoting(metadata.RequiredString("voting_period_kind"));

            foreach (var baker in metadata.RequiredArray("deactivated").EnumerateArray())
                if (!Cache.Accounts.DelegateExists(baker.GetString()))
                    throw new ValidationException($"non-existent deactivated baker {baker}");

            var balanceUpdates = ParseBalanceUpdates(metadata.RequiredArray("balance_updates"));
            var rewardUpdates = Cycle < Protocol.NoRewardCycles || Block.RequiredArray("operations", 4)[0].Count() == 0 ? 2 : 3;

            ValidateBlockRewards(balanceUpdates.Take(rewardUpdates));
            ValidateCycleRewards(balanceUpdates.Skip(rewardUpdates));
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

            return Cycle < Protocol.NoRewardCycles ? 0 : (priority == 0 ? Protocol.BlockReward0 : Protocol.BlockReward1) * endorsements;
        }

        // new formula
        protected override long GetEndorsementReward(int slots)
        {
            var priority = Block
                .GetProperty("header")
                .RequiredInt32("priority");

            return Cycle < Protocol.NoRewardCycles ? 0 : (priority == 0 ? Protocol.EndorsementReward0 : Protocol.EndorsementReward1) * slots;
        }
    }
}
