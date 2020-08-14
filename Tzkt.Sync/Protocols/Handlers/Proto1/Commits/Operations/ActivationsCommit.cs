using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto1
{
    class ActivationsCommit : ProtocolCommit
    {
        public ActivationOperation Activation { get; private set; }
        public Commitment Commitment { get; private set; }

        ActivationsCommit(ProtocolHandler protocol) : base(protocol) { }

        public async Task Init(Block block, RawOperation op, RawActivationContent content)
        {
            Activation = new ActivationOperation
            {
                Id = Cache.AppState.NextOperationId(),
                Block = block,
                Level = block.Level,
                Timestamp = block.Timestamp,
                OpHash = op.Hash,
                Account = (User)await Cache.Accounts.GetAsync(content.Address),
                Balance = content.Metadata.BalanceUpdates[0].Change
            };

            var btz = Blind.GetBlindedAddress(content.Address, content.Secret);
            Commitment = await Db.Commitments.FirstAsync(x => x.Address == btz);
        }

        public async Task Init(Block block, ActivationOperation activation)
        {
            Activation = activation;
            Activation.Block ??= block;
            Activation.Account ??= (User)await Cache.Accounts.GetAsync(activation.AccountId);

            Commitment = await Db.Commitments.FirstAsync(x => x.AccountId == activation.AccountId);
        }

        public override Task Apply()
        {
            #region entities
            var block = Activation.Block;
            var sender = Activation.Account;

            //Db.TryAttach(block);
            Db.TryAttach(sender);
            #endregion

            #region apply operation
            sender.Balance += Activation.Balance;

            sender.Activated = true;

            block.Operations |= Operations.Activations;

            Commitment.AccountId = sender.Id;
            Commitment.Level = block.Level;
            #endregion

            Db.ActivationOps.Add(Activation);

            return Task.CompletedTask;
        }

        public override Task Revert()
        {
            #region entities
            //var block = Activation.Block;
            var sender = Activation.Account;

            //Db.TryAttach(block);
            Db.TryAttach(sender);
            #endregion

            #region revert operation
            sender.Balance -= Activation.Balance;

            sender.Activated = null;

            Commitment.AccountId = null;
            Commitment.Level = null;
            #endregion

            Db.ActivationOps.Remove(Activation);

            return Task.CompletedTask;
        }

        #region static
        public static async Task<ActivationsCommit> Apply(ProtocolHandler proto, Block block, RawOperation op, RawActivationContent content)
        {
            var commit = new ActivationsCommit(proto);
            await commit.Init(block, op, content);
            await commit.Apply();

            return commit;
        }

        public static async Task<ActivationsCommit> Revert(ProtocolHandler proto, Block block, ActivationOperation op)
        {
            var commit = new ActivationsCommit(proto);
            await commit.Init(block, op);
            await commit.Revert();

            return commit;
        }
        #endregion
    }
}
