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
        public List<ActivationOperation> Activations { get; protected set; }

        public ActivationsCommit(ProtocolHandler protocol, List<ICommit> commits) : base(protocol, commits) { }

        public override async Task Init()
        {
            var block = await Cache.GetCurrentBlockAsync();
            Activations = await Db.ActivationOps.Where(x => x.Level == block.Level).ToListAsync();
            foreach (var op in Activations)
            {
                op.Account ??= (User)await Cache.GetAccountAsync(op.AccountId);
                op.Block = block;
            }
        }

        public override async Task Init(IBlock block)
        {
            var rawBlock = block as RawBlock;
            var parsedBlock = FindCommit<BlockCommit>().Block;

            Activations = new List<ActivationOperation>();
            foreach (var op in rawBlock.Operations[2])
            {
                foreach (var content in op.Contents.Where(x => x is RawActivationContent))
                {
                    var activation = content as RawActivationContent;

                    Activations.Add(new ActivationOperation
                    {
                        Block = parsedBlock,
                        Timestamp = parsedBlock.Timestamp,
                        OpHash = op.Hash,
                        Account = (User)await Cache.GetAccountAsync(activation.Address),
                        Balance = activation.Metadata.BalanceUpdates[0].Change
                    });
                }
            }
        }

        public override Task Apply()
        {
            if (Activations == null)
                throw new Exception("Commit is not initialized");

            foreach (var activation in Activations)
            {
                #region entities
                var block = activation.Block;
                var sender = activation.Account;

                //Db.TryAttach(block);
                Db.TryAttach(sender);
                #endregion

                #region apply operation
                sender.Balance += activation.Balance;

                sender.Operations |= Operations.Activations;
                block.Operations |= Operations.Activations;
                #endregion

                Db.ActivationOps.Add(activation);
            }

            return Task.CompletedTask;
        }

        public override async Task Revert()
        {
            if (Activations == null)
                throw new Exception("Commit is not initialized");

            foreach (var activation in Activations)
            {
                #region entities
                var sender = activation.Account;

                Db.TryAttach(sender);
                #endregion

                #region revert operation
                sender.Balance -= activation.Balance;

                if (!await Db.ActivationOps.AnyAsync(x => x.AccountId == sender.Id && x.Level < activation.Level))
                    sender.Operations &= ~Operations.Activations;
                #endregion

                if (sender.Operations == Operations.None && sender.Counter > 0)
                {
                    Db.Accounts.Remove(sender);
                    Cache.RemoveAccount(sender);
                }

                Db.ActivationOps.Remove(activation);
            }
        }

        #region static
        public static async Task<ActivationsCommit> Create(ProtocolHandler protocol, List<ICommit> commits, RawBlock rawBlock)
        {
            var commit = new ActivationsCommit(protocol, commits);
            await commit.Init(rawBlock);
            return commit;
        }

        public static async Task<ActivationsCommit> Create(ProtocolHandler protocol, List<ICommit> commits)
        {
            var commit = new ActivationsCommit(protocol, commits);
            await commit.Init();
            return commit;
        }
        #endregion
    }
}
