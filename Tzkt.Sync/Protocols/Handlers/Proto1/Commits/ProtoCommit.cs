using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto1
{
    class ProtoCommit : ProtocolCommit
    {
        public Protocol Protocol { get; private set; }
        public Protocol NextProtocol { get; private set; }

        public ProtoCommit(ProtocolHandler protocol, List<ICommit> commits) : base(protocol, commits) { }

        public override async Task Init()
        {
            var state = await Cache.GetAppStateAsync();

            Protocol = await Cache.GetProtocolAsync(state.Protocol);
            NextProtocol = await Cache.GetProtocolAsync(state.NextProtocol);
        }

        public override async Task Init(IBlock block)
        {
            var rawBlock = block as RawBlock;

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

        public override Task Apply()
        {
            if (Protocol == null)
                throw new Exception("Commit is not initialized");

            Db.TryAttach(Protocol);
            Db.TryAttach(NextProtocol);
            Protocol.Weight++;

            return Task.CompletedTask;
        }

        public override Task Revert()
        {
            if (Protocol == null)
                throw new Exception("Commit is not initialized");

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
        public static async Task<ProtoCommit> Create(ProtocolHandler protocol, List<ICommit> commits, RawBlock rawBlock)
        {
            var commit = new ProtoCommit(protocol, commits);
            await commit.Init(rawBlock);
            return commit;
        }

        public static async Task<ProtoCommit> Create(ProtocolHandler protocol, List<ICommit> commits)
        {
            var commit = new ProtoCommit(protocol, commits);
            await commit.Init();
            return commit;
        }
        #endregion
    }
}
