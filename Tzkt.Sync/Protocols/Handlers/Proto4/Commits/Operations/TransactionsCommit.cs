using System.Threading.Tasks;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto4
{
    class TransactionsCommit : Proto3.TransactionsCommit
    {
        public TransactionsCommit(ProtocolHandler protocol) : base(protocol) { }

        protected override Task ResetGracePeriod(TransactionOperation transaction) => Task.CompletedTask;
    }
}
