using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto1
{
    class NonceRevelationsCommit : ProtocolCommit
    {
        public List<NonceRevelationOperation> Revelations { get; private set; }
        public Protocol Protocol { get; private set; }

        public NonceRevelationsCommit(ProtocolHandler protocol, List<ICommit> commits) : base(protocol, commits) { }

        public override async Task Init()
        {
            var block = await Cache.GetCurrentBlockAsync();
            Protocol = await Cache.GetCurrentProtocolAsync();
            Revelations = await Db.NonceRevelationOps.Include(x => x.RevealedBlock).Where(x => x.Level == block.Level).ToListAsync();
            foreach (var op in Revelations)
            {
                op.Block = block;
                op.Baker ??= (Data.Models.Delegate)await Cache.GetAccountAsync(op.BakerId);
            }
        }

        public override async Task Init(IBlock block)
        {
            var rawBlock = block as RawBlock;
            var parsedBlock = FindCommit<BlockCommit>().Block;
            parsedBlock.Baker ??= (Data.Models.Delegate)await Cache.GetAccountAsync(parsedBlock.BakerId);

            Protocol = await Cache.GetProtocolAsync(block.Protocol);
            Revelations = new List<NonceRevelationOperation>();
            foreach (var op in rawBlock.Operations[2])
            {
                foreach (var content in op.Contents.Where(x => x is RawNonceRevelationContent))
                {
                    var revelation = content as RawNonceRevelationContent;
                    var revealedBlock = await Db.Blocks.FirstAsync(x => x.Level == revelation.Level);

                    Revelations.Add(new NonceRevelationOperation
                    {
                        Baker = parsedBlock.Baker,
                        Block = parsedBlock,
                        Timestamp = parsedBlock.Timestamp,
                        OpHash = op.Hash,
                        RevealedBlock = revealedBlock,
                        RevealedLevel = revealedBlock.Level
                    });
                }
            }
        }

        public override Task Apply()
        {
            if (Revelations == null)
                throw new Exception("Commit is not initialized");

            foreach (var revelation in Revelations)
            {
                #region entities
                var block = revelation.Block;
                var blockBaker = block.Baker;
                var revealedBlock = revelation.RevealedBlock;

                //Db.TryAttach(block);
                Db.TryAttach(blockBaker);
                Db.TryAttach(revealedBlock);
                #endregion

                #region apply operation
                blockBaker.Balance += Protocol.RevelationReward;
                blockBaker.FrozenRewards += Protocol.RevelationReward;

                blockBaker.Operations |= Operations.Revelations;
                block.Operations |= Operations.Revelations;

                revealedBlock.Revelation = revelation;
                #endregion

                Db.NonceRevelationOps.Add(revelation);
            }

            return Task.CompletedTask;
        }

        public override async Task Revert()
        {
            if (Revelations == null)
                throw new Exception("Commit is not initialized");

            foreach (var revelation in Revelations)
            {
                #region entities
                var block = revelation.Block;
                var blockBaker = block.Baker;
                var revealedBlock = revelation.RevealedBlock;

                //Db.TryAttach(block);
                Db.TryAttach(blockBaker);
                Db.TryAttach(revealedBlock);
                #endregion

                #region apply operation
                blockBaker.Balance -= Protocol.RevelationReward;
                blockBaker.FrozenRewards -= Protocol.RevelationReward;

                if (!await Db.NonceRevelationOps.AnyAsync(x => x.BakerId == blockBaker.Id && x.Level < revelation.Level))
                    blockBaker.Operations &= ~Operations.Revelations;

                revealedBlock.Revelation = null;
                revealedBlock.RevelationId = null;
                #endregion

                Db.NonceRevelationOps.Remove(revelation);
            }
        }

        #region static
        public static async Task<NonceRevelationsCommit> Create(ProtocolHandler protocol, List<ICommit> commits, RawBlock rawBlock)
        {
            var commit = new NonceRevelationsCommit(protocol, commits);
            await commit.Init(rawBlock);
            return commit;
        }

        public static async Task<NonceRevelationsCommit> Create(ProtocolHandler protocol, List<ICommit> commits)
        {
            var commit = new NonceRevelationsCommit(protocol, commits);
            await commit.Init();
            return commit;
        }
        #endregion
    }
}
