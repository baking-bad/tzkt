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

        public async Task Init(Block block, RawBlock rawBlock)
        {
            if (block.Events.HasFlag(BlockEvents.CycleEnd))
            {
                Protocol = await Cache.GetProtocolAsync(rawBlock.Protocol);
                BalanceUpdates = rawBlock.Metadata.BalanceUpdates.Skip(Protocol.BlockReward0 > 0 ? 3 : 2);
            }
        }

        public async Task Init(Block block)
        {
            if (block.Events.HasFlag(BlockEvents.CycleEnd))
            {
                var stream = await Proto.Node.GetBlockAsync(block.Level);
                var rawBlock = (RawBlock)await (Proto.Serializer as Serializer).DeserializeBlock(stream);

                Protocol = await Cache.GetProtocolAsync(rawBlock.Protocol);
                BalanceUpdates = rawBlock.Metadata.BalanceUpdates.Skip(Protocol.BlockReward0 > 0 ? 3 : 2);
            }
        }

        public override async Task Apply()
        {
            if (BalanceUpdates == null) return;

            foreach (var update in BalanceUpdates)
            {
                #region entities
                var delegat = await Cache.GetDelegateAsync(update.Target);

                Db.TryAttach(delegat);
                #endregion

                if (update is DepositsUpdate depositsFreezer)
                {
                    delegat.FrozenDeposits += depositsFreezer.Change;
                }
                else if (update is RewardsUpdate rewardsFreezer)
                {
                    delegat.FrozenRewards += rewardsFreezer.Change;
                    delegat.StakingBalance -= rewardsFreezer.Change;
                }
                else if (update is FeesUpdate feesFreezer)
                {
                    delegat.FrozenFees += feesFreezer.Change;
                }
            }
        }

        public async override Task Revert()
        {
            if (BalanceUpdates == null) return;

            foreach (var update in BalanceUpdates)
            {
                #region entities
                var delegat = await Cache.GetDelegateAsync(update.Target);

                Db.TryAttach(delegat);
                #endregion

                if (update is DepositsUpdate depositsFreezer)
                {
                    delegat.FrozenDeposits -= depositsFreezer.Change;
                }
                else if (update is RewardsUpdate rewardsFreezer)
                {
                    delegat.FrozenRewards -= rewardsFreezer.Change;
                    delegat.StakingBalance += rewardsFreezer.Change;
                }
                else if (update is FeesUpdate feesFreezer)
                {
                    delegat.FrozenFees -= feesFreezer.Change;
                }
            }
        }

        #region static
        public static async Task<FreezerCommit> Apply(ProtocolHandler proto, Block block, RawBlock rawBlock)
        {
            var commit = new FreezerCommit(proto);
            await commit.Init(block, rawBlock);
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
