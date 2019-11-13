using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

using Tzkt.Data.Models;
using Tzkt.Data.Models.Base;

namespace Tzkt.Sync.Protocols.Proto2
{
    class RevealsCommit : ProtocolCommit
    {
        public RevealOperation Reveal { get; private set; }
        public string PubKey { get; private set; }

        RevealsCommit(ProtocolHandler protocol) : base(protocol) { }

        public async Task Init(Block block, RawOperation op, RawRevealContent content)
        {
            var id = await Cache.NextCounterAsync();

            var sender = await Cache.GetAccountAsync(content.Source);
            sender.Delegate ??= (Data.Models.Delegate)await Cache.GetAccountAsync(sender.DelegateId);

            PubKey = content.PublicKey;
            Reveal = new RevealOperation
            {
                Id = id,
                OpHash = op.Hash,
                Block = block,
                Level = block.Level,
                Timestamp = block.Timestamp,
                BakerFee = content.Fee,
                Counter = content.Counter,
                GasLimit = content.GasLimit,
                StorageLimit = content.StorageLimit,
                Sender = sender,
                Status = content.Metadata.Result.Status switch
                {
                    "applied" => OperationStatus.Applied,
                    "backtracked" => OperationStatus.Backtracked,
                    _ => throw new NotImplementedException()
                }
            };
        }

        public async Task Init(Block block, RevealOperation reveal)
        {
            Reveal = reveal;

            Reveal.Block ??= block;
            Reveal.Block.Baker ??= (Data.Models.Delegate)await Cache.GetAccountAsync(block.BakerId);

            Reveal.Sender = await Cache.GetAccountAsync(reveal.SenderId);
            Reveal.Sender.Delegate ??= (Data.Models.Delegate)await Cache.GetAccountAsync(reveal.Sender.DelegateId);
        }

        public override Task Apply()
        {
            #region entities
            var block = Reveal.Block;
            var blockBaker = block.Baker;

            var sender = Reveal.Sender;
            var senderDelegate = sender.Delegate ?? sender as Data.Models.Delegate;

            //Db.TryAttach(block);
            Db.TryAttach(blockBaker);

            Db.TryAttach(sender);
            Db.TryAttach(senderDelegate);
            #endregion

            #region apply operation
            sender.Balance -= Reveal.BakerFee;
            if (senderDelegate != null) senderDelegate.StakingBalance -= Reveal.BakerFee;
            blockBaker.FrozenFees += Reveal.BakerFee;
            blockBaker.Balance += Reveal.BakerFee;
            blockBaker.StakingBalance += Reveal.BakerFee;

            sender.Operations |= Operations.Reveals;
            block.Operations |= Operations.Reveals;

            sender.Counter = Math.Max(sender.Counter, Reveal.Counter);
            #endregion

            #region apply result
            if (Reveal.Status == OperationStatus.Applied || Reveal.Status == OperationStatus.Backtracked)
            {
                if (sender is User user)
                    user.PublicKey = PubKey;
            }
            #endregion

            Db.RevealOps.Add(Reveal);

            return Task.CompletedTask;
        }

        public override async Task Revert()
        {
            #region entities
            var block = Reveal.Block;
            var blockBaker = block.Baker;

            var sender = Reveal.Sender;
            var senderDelegate = sender.Delegate ?? sender as Data.Models.Delegate;

            //Db.TryAttach(block);
            Db.TryAttach(blockBaker);

            Db.TryAttach(sender);
            Db.TryAttach(senderDelegate);
            #endregion

            #region revert result
            if (Reveal.Status == OperationStatus.Applied || Reveal.Status == OperationStatus.Backtracked)
            {
                if (sender is User user)
                    user.PublicKey = null;
            }
            #endregion

            #region revert operation
            sender.Balance += Reveal.BakerFee;
            if (senderDelegate != null) senderDelegate.StakingBalance += Reveal.BakerFee;
            blockBaker.FrozenFees -= Reveal.BakerFee;
            blockBaker.Balance -= Reveal.BakerFee;
            blockBaker.StakingBalance -= Reveal.BakerFee;

            if (!await Db.RevealOps.AnyAsync(x => x.SenderId == sender.Id && x.Id < Reveal.Id))
                sender.Operations &= ~Operations.Reveals;

            sender.Counter = Math.Min(sender.Counter, Reveal.Counter - 1);
            #endregion

            Db.RevealOps.Remove(Reveal);
            await Cache.ReleaseCounterAsync(true);
        }

        #region static
        public static async Task<RevealsCommit> Apply(ProtocolHandler proto, Block block, RawOperation op, RawRevealContent content)
        {
            var commit = new RevealsCommit(proto);
            await commit.Init(block, op, content);
            await commit.Apply();

            return commit;
        }

        public static async Task<RevealsCommit> Revert(ProtocolHandler proto, Block block, RevealOperation op)
        {
            var commit = new RevealsCommit(proto);
            await commit.Init(block, op);
            await commit.Revert();

            return commit;
        }
        #endregion
    }
}
