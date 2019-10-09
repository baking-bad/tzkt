using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

using Tzkt.Data;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Services
{
    public class AccountManager
    {
        #region cache
        const int MaxSize = 8192;
        static readonly Dictionary<string, Account> Accounts = new Dictionary<string, Account>(MaxSize);
        #endregion

        readonly TzktContext Db;
        readonly StateManager State;

        public AccountManager(TzktContext db, StateManager state)
        {
            Db = db;
            State = state;
        }

        public void AddAccount(Account account)
        {
            Accounts[account.Address] = account;
            Db.Accounts.Add(account);
        }

        public async Task<bool> ExistsAsync(string address, AccountType? type = null)
        {
            if (String.IsNullOrEmpty(address))
                return false;

            if (Accounts.ContainsKey(address))
                return Accounts[address].Type == type || type == null;

            var account = type switch
            {
                AccountType.User => await Db.Users.FirstOrDefaultAsync(x => x.Address == address),
                AccountType.Delegate => await Db.Delegates.FirstOrDefaultAsync(x => x.Address == address),
                AccountType.Contract => await Db.Contracts.FirstOrDefaultAsync(x => x.Address == address),
                _ => await Db.Accounts.FirstOrDefaultAsync(x => x.Address == address)
            };

            if (account != null)
            {
                Accounts[address] = account;
                return true;
            }

            return false;
        }

        public async Task<Account> GetAccountAsync(int id)
        {
            var account = Accounts.Values.FirstOrDefault(x => x.Id == id);
            if (account == null)
            {
                if (Accounts.Count >= MaxSize)
                    foreach (var key in Accounts.Where(x => x.Value.Type != AccountType.Delegate).Select(x => x.Key).Take(MaxSize / 8).ToList())
                        Accounts.Remove(key);

                account = await Db.Accounts.FirstOrDefaultAsync(x => x.Id == id)
                    ?? throw new Exception($"Account #{id} doesn't exist");

                Accounts[account.Address] = account;
            }

            return account;
        }

        public async Task<Account> GetAccountAsync(string address)
        {
            if (!Accounts.ContainsKey(address))
            {
                if (Accounts.Count >= MaxSize)
                    foreach (var key in Accounts.Where(x => x.Value.Type != AccountType.Delegate).Select(x => x.Key).Take(MaxSize / 8).ToList())
                        Accounts.Remove(key);

                var account = await Db.Accounts.FirstOrDefaultAsync(x => x.Address == address);

                if (account == null)
                {
                    if (address[0] != 't')
                        throw new Exception($"Contract {address} doesn't exist");

                    account = new User
                    {
                        Address = address,
                        Counter = await State.GetCounter(),
                        Type = AccountType.User,
                    };

                    Db.Accounts.Add(account);
                }

                Accounts[address] = account;
            }

            return Accounts[address];
        }

        public void Clear(bool bakers = false)
        {
            if (!bakers)
            {
                foreach (var address in Accounts.Where(x => x.Value.Type != AccountType.Delegate).Select(x => x.Key).ToList())
                    Accounts.Remove(address);
            }
            else
            {
                Accounts.Clear();
            }
        }
    }
}
