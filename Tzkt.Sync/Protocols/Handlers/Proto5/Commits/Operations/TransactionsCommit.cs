using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto5
{
    class TransactionsCommit : Proto4.TransactionsCommit
    {
        public TransactionsCommit(ProtocolHandler protocol) : base(protocol) { }

        protected override BlockEvents GetBlockEvents(Account target)
        {
            return target is Contract c
                ? c.Kind == ContractKind.DelegatorContract
                    ? BlockEvents.DelegatorContracts
                    : BlockEvents.SmartContracts
                : BlockEvents.None;
        }
    }
}
