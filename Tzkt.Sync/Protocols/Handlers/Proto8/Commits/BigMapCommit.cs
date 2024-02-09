using Netmavryk.Contracts;
using Netmavryk.Encoding;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto8
{
    class BigMapCommit : Proto1.BigMapCommit
    {
        public BigMapCommit(ProtocolHandler protocol) : base(protocol) { }

        protected override BigMapTag GetTags(Contract contract, TreeView node)
        {
            var tags = base.GetTags(contract, node);

            // custom handler for Mavryk Domains
            if (contract.Address == "KT1GBZmSxmnKJXGMdMLbugPfLyUPmuLSMwKS" &&
                (node.Value as MichelineInt).Value == 1264) // %records
                tags |= BigMapTag.Ledger12;

            return tags;
        }
    }
}
