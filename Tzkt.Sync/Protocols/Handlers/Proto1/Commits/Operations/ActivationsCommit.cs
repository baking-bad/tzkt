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

                        Account = (User)await Accounts.GetAccountAsync(activation.Address),
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
                #region balances
                activation.Account.Balance = activation.Balance;
                #endregion

                #region counters
                activation.Account.Operations |= Operations.Activations;
                activation.Block.Operations |= Operations.Activations;
                #endregion

                if (Db.Entry(activation.Account).State != EntityState.Added)
                    Db.Accounts.Update(activation.Account);

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
                var account = await Accounts.GetAccountAsync(activation.AccountId);

                Db.Accounts.Remove(account);
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

        public static Task<ActivationsCommit> Create(ProtocolHandler protocol, List<ICommit> commits, List<ActivationOperation> activations)
        {
            var commit = new ActivationsCommit(protocol, commits) { Activations = activations };
            return Task.FromResult(commit);
        }
        #endregion
    }
}
