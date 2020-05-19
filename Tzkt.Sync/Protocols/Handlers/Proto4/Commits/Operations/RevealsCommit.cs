using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

using Tzkt.Data.Models;
using Tzkt.Data.Models.Base;

namespace Tzkt.Sync.Protocols.Proto4
{
    class RevealsCommit : ProtocolCommit
    {
        public RevealOperation Reveal { get; private set; }
        public string PubKey { get; private set; }

        RevealsCommit(ProtocolHandler protocol) : base(protocol) { }

        public async Task Init(Block block, RawOperation op, RawRevealContent content)
        {
            var id = Cache.AppState.NextOperationId();

            var sender = await Cache.Accounts.GetAsync(content.Source);
            sender.Delegate ??= Cache.Accounts.GetDelegate(sender.DelegateId);

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
                    "failed" => OperationStatus.Failed,
                    _ => throw new NotImplementedException()
                },
                Errors = OperationErrors.Parse(content.Metadata.Result.Errors),
                GasUsed = content.Metadata.Result.ConsumedGas
            };
        }

        public async Task Init(Block block, RevealOperation reveal)
        {
            Reveal = reveal;

            Reveal.Block ??= block;
            Reveal.Block.Baker ??= Cache.Accounts.GetDelegate(block.BakerId);

            Reveal.Sender = await Cache.Accounts.GetAsync(reveal.SenderId);
            Reveal.Sender.Delegate ??= Cache.Accounts.GetDelegate(reveal.Sender.DelegateId);
        }

        public override async Task Apply()
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
            await Spend(sender, Reveal.BakerFee);
            if (senderDelegate != null) senderDelegate.StakingBalance -= Reveal.BakerFee;
            blockBaker.FrozenFees += Reveal.BakerFee;
            blockBaker.Balance += Reveal.BakerFee;
            blockBaker.StakingBalance += Reveal.BakerFee;

            sender.RevealsCount++;

            block.Operations |= Operations.Reveals;
            block.Fees += Reveal.BakerFee;

            sender.Counter = Math.Max(sender.Counter, Reveal.Counter);
            #endregion

            #region apply result
            if (sender is User user)
            {
                user.PublicKey = PubKey;
                if (user.Balance > 0) user.Revealed = true;
            }
            #endregion

            Db.RevealOps.Add(Reveal);
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
            if (sender is User user)
            {
                if (sender.RevealsCount == 1)
                    user.PublicKey = null;

                user.Revealed = false;
            }
            #endregion

            #region revert operation
            await Return(sender, Reveal.BakerFee, true);
            if (senderDelegate != null) senderDelegate.StakingBalance += Reveal.BakerFee;
            blockBaker.FrozenFees -= Reveal.BakerFee;
            blockBaker.Balance -= Reveal.BakerFee;
            blockBaker.StakingBalance -= Reveal.BakerFee;

            sender.RevealsCount--;

            sender.Counter = Math.Min(sender.Counter, Reveal.Counter - 1);
            #endregion

            Db.RevealOps.Remove(Reveal);
            Cache.AppState.ReleaseManagerCounter();
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
