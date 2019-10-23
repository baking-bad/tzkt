using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto1
{
    class FreezerCommit : ProtocolCommit
    {
        public IEnumerable<IBalanceUpdate> BalanceUpdates { get; private set; }
        public Protocol Protocol { get; private set; }

        FreezerCommit(ProtocolHandler protocol) : base(protocol) { }

        public async Task Init(RawBlock rawBlock)
        {
            Protocol = await Cache.GetProtocolAsync(rawBlock.Protocol);
            var cycle = (rawBlock.Level - 1) / Protocol.BlocksPerCycle;
            BalanceUpdates = rawBlock.Metadata.BalanceUpdates.Skip(cycle < 7 ? 2 : 3);
        }

        public async Task Init(Block block)
        {
            var stream = await Proto.Node.GetBlockAsync(block.Level);
            var rawBlock = (RawBlock)await (Proto.Serializer as Serializer).DeserializeBlock(stream);

            Protocol = await Cache.GetProtocolAsync(rawBlock.Protocol);
            var cycle = (rawBlock.Level - 1) / Protocol.BlocksPerCycle;
            BalanceUpdates = rawBlock.Metadata.BalanceUpdates.Skip(cycle < 7 ? 2 : 3);
        }

        public override async Task Apply()
        {
            foreach (var update in BalanceUpdates)
            {
                #region entities
                var delegat = (Data.Models.Delegate)await Cache.GetAccountAsync(update.Target);

                Db.TryAttach(delegat);
                #endregion

                if (update is DepositsUpdate depositsFreezer)
                {
                    delegat.FrozenDeposits += depositsFreezer.Change;
                }
                else if (update is RewardsUpdate rewardsFreezer)
                {
                    delegat.FrozenRewards += rewardsFreezer.Change;
                }
                else if (update is FeesUpdate feesFreezer)
                {
                    delegat.FrozenFees += feesFreezer.Change;
                }
            }
        }

        public async override Task Revert()
        {
            foreach (var update in BalanceUpdates)
            {
                #region entities
                var delegat = (Data.Models.Delegate)await Cache.GetAccountAsync(update.Target);

                Db.TryAttach(delegat);
                #endregion

                if (update is DepositsUpdate depositsFreezer)
                {
                    delegat.FrozenDeposits -= depositsFreezer.Change;
                }
                else if (update is RewardsUpdate rewardsFreezer)
                {
                    delegat.FrozenRewards -= rewardsFreezer.Change;
                }
                else if (update is FeesUpdate feesFreezer)
                {
                    delegat.FrozenFees -= feesFreezer.Change;
                }
            }
        }

        #region static
        public static async Task<FreezerCommit> Apply(ProtocolHandler proto, RawBlock rawBlock)
        {
            var commit = new FreezerCommit(proto);
            await commit.Init(rawBlock);
            await commit.Apply();

            return commit;
        }

        public static async Task<FreezerCommit> Revert(ProtocolHandler proto, Block block)
        {
            var commit = new FreezerCommit(proto);
            await commit.Init(block);
            await commit.Revert();

            return commit;
        }
        #endregion
    }
}
