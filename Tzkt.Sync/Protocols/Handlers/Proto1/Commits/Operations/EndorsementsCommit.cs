﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto1
{
    class EndorsementsCommit : ProtocolCommit
    {
        public EndorsementOperation Endorsement { get; private set; }

        EndorsementsCommit(ProtocolHandler protocol) : base(protocol) { }

        public async Task Init(Block block, RawOperation op, RawEndorsementContent content)
        {
            Endorsement = new EndorsementOperation
            {
                Id = await Cache.NextCounterAsync(),
                Block = block,
                Timestamp = block.Timestamp,
                OpHash = op.Hash,
                Slots = content.Metadata.Slots.Count,
                Delegate = (Data.Models.Delegate)await Cache.GetAccountAsync(content.Metadata.Delegate),
                Reward = content.Metadata.BalanceUpdates.FirstOrDefault(x => x is RewardsUpdate)?.Change ?? 0
            };
        }

        public async Task Init(Block block, EndorsementOperation endorsement)
        {
            Endorsement = endorsement;

            Endorsement.Block ??= block;
            Endorsement.Block.Protocol ??= await Cache.GetProtocolAsync(block.ProtoCode);

            Endorsement.Delegate ??= (Data.Models.Delegate)await Cache.GetAccountAsync(endorsement.DelegateId);
        }

        public override Task Apply()
        {
            #region entities
            var block = Endorsement.Block;
            var sender = Endorsement.Delegate;

            //Db.TryAttach(block);
            Db.TryAttach(sender);
            #endregion

            #region apply operation
            sender.Balance += Endorsement.Reward;
            sender.FrozenRewards += Endorsement.Reward;
            sender.FrozenDeposits += 1_000_000 * ((Endorsement.Block.Level - 1) / block.Protocol.BlocksPerCycle) * Endorsement.Slots;

            sender.Operations |= Operations.Endorsements;
            block.Operations |= Operations.Endorsements;

            block.Validations++;
            #endregion

            Db.EndorsementOps.Add(Endorsement);
            return Task.CompletedTask;
        }

        public override async Task Revert()
        {
            #region entities
            var block = Endorsement.Block;
            var sender = Endorsement.Delegate;

            //Db.TryAttach(block);
            Db.TryAttach(sender);
            #endregion

            #region apply operation
            sender.Balance -= Endorsement.Reward;
            sender.FrozenRewards -= Endorsement.Reward;
            sender.FrozenDeposits -= 1_000_000 * ((Endorsement.Block.Level - 1) / block.Protocol.BlocksPerCycle) * Endorsement.Slots;

            if (!await Db.EndorsementOps.AnyAsync(x => x.DelegateId == sender.Id && x.Id < Endorsement.Id))
                sender.Operations &= ~Operations.Endorsements;
            #endregion

            Db.EndorsementOps.Remove(Endorsement);
        }

        #region static
        public static async Task<EndorsementsCommit> Apply(ProtocolHandler proto, Block block, RawOperation op, RawEndorsementContent content)
        {
            var commit = new EndorsementsCommit(proto);
            await commit.Init(block, op, content);
            await commit.Apply();

            return commit;
        }

        public static async Task<EndorsementsCommit> Revert(ProtocolHandler proto, Block block, EndorsementOperation op)
        {
            var commit = new EndorsementsCommit(proto);
            await commit.Init(block, op);
            await commit.Revert();

            return commit;
        }
        #endregion
    }
}