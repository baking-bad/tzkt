using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Initiator
{
    class ProtoCommit : ProtocolCommit
    {
        public Protocol Protocol { get; private set; }
        public Protocol NextProtocol { get; private set; }

        ProtoCommit(ProtocolHandler protocol) : base(protocol) { }

        public async Task Init(RawBlock rawBlock)
        {
            Protocol = await Cache.GetProtocolAsync(rawBlock.Metadata.Protocol);
            NextProtocol = await Cache.GetProtocolAsync(rawBlock.Metadata.NextProtocol);

            if (Protocol.Id != NextProtocol.Id)
            {
                var stream = await Proto.Node.GetConstantsAsync(rawBlock.Level);
                var rawConst = await (Proto.Serializer as Serializer).DeserializeConstants(stream);

                NextProtocol.BlockDeposit = rawConst.BlockDeposit;
                NextProtocol.BlockReward = rawConst.BlockReward;
                NextProtocol.BlocksPerCommitment = rawConst.BlocksPerCommitment;
                NextProtocol.BlocksPerCycle = rawConst.BlocksPerCycle;
                NextProtocol.BlocksPerSnapshot = rawConst.BlocksPerSnapshot;
                NextProtocol.BlocksPerVoting = rawConst.BlocksPerVoting;
                NextProtocol.ByteCost = rawConst.ByteCost;
                NextProtocol.EndorsementDeposit = rawConst.EndorsementDeposit;
                NextProtocol.EndorsementReward = rawConst.EndorsementReward;
                NextProtocol.EndorsersPerBlock = rawConst.EndorsersPerBlock;
                NextProtocol.HardBlockGasLimit = rawConst.HardBlockGasLimit;
                NextProtocol.HardOperationGasLimit = rawConst.HardOperationGasLimit;
                NextProtocol.HardOperationStorageLimit = rawConst.HardOperationStorageLimit;
                NextProtocol.OriginationSize = rawConst.OriginationBurn / rawConst.ByteCost;
                NextProtocol.PreserverCycles = rawConst.PreserverCycles;
                NextProtocol.RevelationReward = rawConst.RevelationReward;
                NextProtocol.TimeBetweenBlocks = rawConst.TimeBetweenBlocks[0];
                NextProtocol.TokensPerRoll = rawConst.TokensPerRoll;
            }
        }

        public async Task Init(Block block)
        {
            var state = await Cache.GetAppStateAsync();

            Protocol = await Cache.GetProtocolAsync(state.Protocol);
            NextProtocol = await Cache.GetProtocolAsync(state.NextProtocol);
        }

        public override Task Apply()
        {
            Db.TryAttach(Protocol);
            Db.TryAttach(NextProtocol);
            Protocol.Weight++;

            return Task.CompletedTask;
        }

        public override Task Revert()
        {
            Db.TryAttach(Protocol);
            Db.TryAttach(NextProtocol);

            if (NextProtocol.Weight == 0)
            {
                Db.Protocols.Remove(NextProtocol);
                Cache.RemoveProtocol(NextProtocol);
            }

            Protocol.Weight--;

            return Task.CompletedTask;
        }

        #region static
        public static async Task<ProtoCommit> Apply(ProtocolHandler proto, RawBlock rawBlock)
        {
            var commit = new ProtoCommit(proto);
            await commit.Init(rawBlock);
            await commit.Apply();

            return commit;
        }

        public static async Task<ProtoCommit> Revert(ProtocolHandler proto, Block block)
        {
            var commit = new ProtoCommit(proto);
            await commit.Init(block);
            await commit.Revert();

            return commit;
        }
        #endregion
    }
}
