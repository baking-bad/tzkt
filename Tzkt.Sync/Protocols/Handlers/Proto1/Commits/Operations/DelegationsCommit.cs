using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Tzkt.Data.Models;
using Tzkt.Data.Models.Base;

namespace Tzkt.Sync.Protocols.Proto1
{
    class DelegationsCommit : ProtocolCommit
    {
        public List<DelegationOperation> Delegations { get; protected set; }

        public DelegationsCommit(ProtocolHandler protocol, List<ICommit> commits) : base(protocol, commits) { }

        public override async Task Init(IBlock block)
        {
            var rawBlock = block as RawBlock;
            var parsedBlock = FindCommit<BlockCommit>().Block;

            Delegations = new List<DelegationOperation>();
            foreach (var op in rawBlock.Operations[3])
            {
                foreach (var content in op.Contents.Where(x => x is RawDelegationContent))
                {
                    var delegation = content as RawDelegationContent;

                    Delegations.Add(new DelegationOperation
                    {
                        Block = parsedBlock,
                        Timestamp = parsedBlock.Timestamp, 

                        OpHash = op.Hash,

                        BakerFee = delegation.Fee,
                        Counter = delegation.Counter,
                        GasLimit = delegation.GasLimit,
                        StorageLimit = delegation.StorageLimit,
                        Sender = await Accounts.GetAccountAsync(delegation.Source),
                        Delegate = delegation.Delegate != null
                           ? (Data.Models.Delegate)await Accounts.GetAccountAsync(delegation.Delegate)
                           : null,

                        Status = delegation.Metadata.Result.Status switch
                        {
                            "applied" => OperationStatus.Applied,
                            _ => throw new NotImplementedException()
                        }
                    });
                }
            }
        }

        public override Task Apply()
        {
            foreach (var delegation in Delegations)
            {
                throw new NotImplementedException();
            }

            return Task.CompletedTask;
        }

        public override Task Revert()
        {
            foreach (var delegation in Delegations)
            {
                throw new NotImplementedException();
            }

            return Task.CompletedTask;
        }

        #region static
        public static async Task<DelegationsCommit> Create(ProtocolHandler protocol, List<ICommit> commits, RawBlock rawBlock)
        {
            var commit = new DelegationsCommit(protocol, commits);
            await commit.Init(rawBlock);
            return commit;
        }

        public static Task<DelegationsCommit> Create(ProtocolHandler protocol, List<ICommit> commits, List<DelegationOperation> delegations)
        {
            var commit = new DelegationsCommit(protocol, commits) { Delegations = delegations };
            return Task.FromResult(commit);
        }
        #endregion
    }
}
