using System.Text.Json;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols
{
    public class ManagerContext(ProtocolHandler proto)
    {
        readonly ProtocolHandler Proto = proto;
        Account? Account = null;
        long Change = 0;

        public void Init(JsonElement operation)
        {
            Proto.Cache.AppState.IncreaseManagerCounter(operation.RequiredArray("contents").Count());
            Account = null;
            Change = 0;
        }

        public void Set(Account account)
        {
            Account = account;
        }

        public void Credit(long amount)
        {
            Change += amount;
        }

        public void Burn(long amount)
        {
            Change -= amount;
        }

        public void Reset()
        {
            if (Account?.Type == AccountType.User && (Account.Balance == 0 || Account.Balance - Change == 0))
            {
                (Account as User)!.Counter = Proto.Cache.AppState.GetManagerCounter();
                (Account as User)!.Revealed = false;
            }
        }
    }
}
