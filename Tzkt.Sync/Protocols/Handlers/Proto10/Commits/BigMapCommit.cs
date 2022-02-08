using Netezos.Contracts;
using Netezos.Encoding;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto10
{
    class BigMapCommit : Proto1.BigMapCommit
    {
        public BigMapCommit(ProtocolHandler protocol) : base(protocol) { }

        protected override BigMapTag GetTags(Contract contract, TreeView node)
        {
            var tags = base.GetTags(contract, node);

            // custom handler for QUIPU
            if (contract.Address == "KT193D4vozYnhGJQVtw7CoxxqphqUEEwK6Vb" &&
                (node.Value as MichelineInt).Value == 12043) // %account_info
                tags |= BigMapTag.Ledger11;

            return tags;
        }
    }
}
