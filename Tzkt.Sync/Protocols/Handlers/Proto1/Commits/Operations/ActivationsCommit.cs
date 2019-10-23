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

        ActivationsCommit(ProtocolHandler protocol) : base(protocol) { }

        public async Task Init(Block block, RawOperation op, RawActivationContent content)
        {
            Activation = new ActivationOperation
            {
                Id = await Cache.NextCounterAsync(),
                Block = block,
                Timestamp = block.Timestamp,
                OpHash = op.Hash,
                Account = (User)await Cache.GetAccountAsync(content.Address),
                Balance = content.Metadata.BalanceUpdates[0].Change
            };
        }

        public async Task Init(Block block, ActivationOperation activation)
        {
            Activation = activation;
            Activation.Block ??= block;
            Activation.Account ??= (User)await Cache.GetAccountAsync(activation.AccountId);
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

            sender.Operations |= Operations.Activations;
            block.Operations |= Operations.Activations;
            #endregion

            Db.ActivationOps.Add(Activation);

            return Task.CompletedTask;
        }

        public override async Task Revert()
        {
            #region entities
            //var block = Activation.Block;
            var sender = Activation.Account;

            //Db.TryAttach(block);
            Db.TryAttach(sender);
            #endregion

            #region revert operation
            sender.Balance -= Activation.Balance;

            if (!await Db.ActivationOps.AnyAsync(x => x.AccountId == sender.Id && x.Id < Activation.Id))
                sender.Operations &= ~Operations.Activations;
            #endregion

            if (sender.Operations == Operations.None && sender.Counter > 0)
            {
                Db.Accounts.Remove(sender);
                Cache.RemoveAccount(sender);
            }

            Db.ActivationOps.Remove(Activation);
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
