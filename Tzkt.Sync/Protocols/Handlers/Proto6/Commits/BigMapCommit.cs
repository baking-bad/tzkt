using Netezos.Contracts;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto6
{
    class BigMapCommit : Proto1.BigMapCommit
    {
        public BigMapCommit(ProtocolHandler protocol) : base(protocol) { }

        protected override BigMapTag GetTags(Contract contract, TreeView node)
        {
            var tags = base.GetTags(contract, node);

            // custom handler for tzBTC
            if (contract.Address == "KT1PWx2mnDueood7fEmfbBDKx1D9BAnnXitn")
                tags |= BigMapTag.Ledger7;

            return tags;
        }
    }
}
